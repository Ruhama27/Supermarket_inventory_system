using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stock_Sync
{
    /// <summary>
    /// Lightweight embedded HTTP micro-server enabling store owners and admins to:
    /// 1. Monitor live sales and revenue in Ethiopian Birr.
    /// 2. View remaining stock and restock finished/low items directly from mobile phone.
    /// 3. Track staff shifts — see who is on shift, who processed which orders, and who did what.
    /// Strictly restricted to Admin accounts — staff accounts are blocked.
    /// </summary>
    public static class MobileServer
    {
        private class AdminSession
        {
            public string Username { get; set; }
            public DateTime Expiry { get; set; }
        }

        private static TcpListener listener;
        private static CancellationTokenSource cts;
        private static int activePort = 5050;
        private static bool isRunning = false;
        private static readonly ConcurrentDictionary<string, AdminSession> activeSessions = new ConcurrentDictionary<string, AdminSession>();

        public static bool IsRunning => isRunning;
        public static int Port => activePort;

        /// <summary>
        /// Validates whether a token belongs to an active, authenticated Admin session.
        /// </summary>
        public static bool ValidateToken(string token, out string username)
        {
            username = null;
            if (string.IsNullOrWhiteSpace(token)) return false;

            if (activeSessions.TryGetValue(token, out AdminSession session))
            {
                if (DateTime.UtcNow < session.Expiry)
                {
                    username = session.Username;
                    return true;
                }
                else
                {
                    activeSessions.TryRemove(token, out _);
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the primary local IPv4 address of this machine (e.g., 192.168.1.105).
        /// </summary>
        public static string GetLocalIpAddress()
        {
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    if (endPoint != null)
                    {
                        return endPoint.Address.ToString();
                    }
                }
            }
            catch
            {
                try
                {
                    var host = Dns.GetHostEntry(Dns.GetHostName());
                    foreach (var ip in host.AddressList)
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                        {
                            return ip.ToString();
                        }
                    }
                }
                catch { }
            }

            return "127.0.0.1";
        }

        public static string GetMobileUrl()
        {
            return $"http://{GetLocalIpAddress()}:{activePort}";
        }

        public static string GetLocalhostUrl()
        {
            return $"http://localhost:{activePort}";
        }

        /// <summary>
        /// Starts the embedded mobile dashboard server in the background.
        /// </summary>
        public static void Start(int preferredPort = 5050)
        {
            if (isRunning) return;

            activePort = preferredPort;
            cts = new CancellationTokenSource();

            for (int p = preferredPort; p < preferredPort + 10; p++)
            {
                try
                {
                    listener = new TcpListener(IPAddress.Any, p);
                    listener.Start();
                    activePort = p;
                    isRunning = true;
                    Logger.LogInfo($"Mobile Dashboard server started on port {activePort} ({GetMobileUrl()})");
                    break;
                }
                catch (SocketException)
                {
                    listener = null;
                }
            }

            if (!isRunning)
            {
                Logger.LogError("Failed to bind Mobile Dashboard server on ports 5050-5060.", null);
                return;
            }

            Task.Run(() => AcceptClientsAsync(cts.Token));
        }

        /// <summary>
        /// Stops the mobile server gracefully.
        /// </summary>
        public static void Stop()
        {
            if (!isRunning) return;

            try
            {
                isRunning = false;
                cts?.Cancel();
                listener?.Stop();
                listener = null;
                Logger.LogInfo("Mobile Dashboard server stopped.");
            }
            catch (Exception ex)
            {
                Logger.LogError("Error stopping Mobile Dashboard server", ex);
            }
        }

        private static async Task AcceptClientsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && isRunning)
            {
                try
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(client, token));
                }
                catch
                {
                    if (token.IsCancellationRequested || !isRunning) break;
                }
            }
        }

        private static async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            {
                try
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (bytesRead <= 0) return;

                    string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    string firstLine = request.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)[0];
                    string[] parts = firstLine.Split(' ');

                    string method = parts.Length > 0 ? parts[0].ToUpperInvariant() : "GET";
                    string rawPath = parts.Length > 1 ? parts[1] : "/";

                    if (method == "OPTIONS")
                    {
                        await SendResponseAsync(stream, "text/plain", new byte[0], 200, "OK");
                        return;
                    }

                    int bodyIndex = request.IndexOf("\r\n\r\n");
                    string requestBody = bodyIndex >= 0 ? request.Substring(bodyIndex + 4) : "";

                    string tokenParam = ExtractToken(rawPath, request, requestBody);

                    if (rawPath.StartsWith("/api/login"))
                    {
                        await HandleLoginAsync(stream, rawPath, requestBody);
                    }
                    else if (rawPath.StartsWith("/api/verify"))
                    {
                        bool valid = ValidateToken(tokenParam, out string uname);
                        string res = $"{{\"valid\":{(valid ? "true" : "false")},\"username\":\"{EscapeJson(uname ?? "")}\"}}";
                        await SendResponseAsync(stream, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(res));
                    }
                    else if (rawPath.StartsWith("/api/logout"))
                    {
                        if (!string.IsNullOrEmpty(tokenParam))
                        {
                            activeSessions.TryRemove(tokenParam, out _);
                        }
                        string res = "{\"success\":true}";
                        await SendResponseAsync(stream, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(res));
                    }
                    else if (rawPath.StartsWith("/api/restock"))
                    {
                        if (!ValidateToken(tokenParam, out string adminUser))
                        {
                            string err = "{\"success\":false,\"message\":\"Admin authentication required to restock.\"}";
                            await SendResponseAsync(stream, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(err), 401, "Unauthorized");
                            return;
                        }

                        await HandleRestockAsync(stream, rawPath, requestBody, adminUser);
                    }
                    else if (rawPath.StartsWith("/api/data"))
                    {
                        if (!ValidateToken(tokenParam, out _))
                        {
                            string err = "{\"error\":\"Unauthorized\",\"message\":\"Admin authentication required. Staff cannot access.\"}";
                            await SendResponseAsync(stream, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(err), 401, "Unauthorized");
                            return;
                        }

                        string json = GetComprehensiveDashboardJson();
                        await SendResponseAsync(stream, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(json));
                    }
                    else if (rawPath.StartsWith("/api/toggle-lang"))
                    {
                        if (!ValidateToken(tokenParam, out _))
                        {
                            string err = "{\"error\":\"Unauthorized\",\"message\":\"Admin authentication required.\"}";
                            await SendResponseAsync(stream, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(err), 401, "Unauthorized");
                            return;
                        }

                        LocalizationManager.ToggleLanguage();
                        string json = GetComprehensiveDashboardJson();
                        await SendResponseAsync(stream, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(json));
                    }
                    else
                    {
                        string html = GenerateMobileHtml();
                        await SendResponseAsync(stream, "text/html; charset=utf-8", Encoding.UTF8.GetBytes(html));
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Error handling mobile client request: {ex.Message}");
                }
            }
        }

        private static async Task HandleLoginAsync(NetworkStream stream, string rawPath, string requestBody)
        {
            string username = ExtractParam(rawPath, requestBody, "username") ?? ExtractParam(rawPath, requestBody, "u");
            string password = ExtractParam(rawPath, requestBody, "password") ?? ExtractParam(rawPath, requestBody, "p");

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                string err = "{\"success\":false,\"message\":\"Please enter both Admin username and password.\"}";
                await SendResponseAsync(stream, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(err), 400, "Bad Request");
                return;
            }

            var user = Login.Authenticate(username.Trim(), password);

            if (user != null && user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                string token = Guid.NewGuid().ToString("N");
                activeSessions[token] = new AdminSession
                {
                    Username = user.Username,
                    Expiry = DateTime.UtcNow.AddDays(7)
                };

                Logger.LogInfo($"Mobile Dashboard Admin login successful: {user.Username}");
                string res = $"{{\"success\":true,\"token\":\"{token}\",\"username\":\"{EscapeJson(user.Username)}\"}}";
                await SendResponseAsync(stream, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(res));
            }
            else if (user != null && user.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogWarning($"Mobile Dashboard login rejected for Staff account: {user.Username}");
                string err = "{\"success\":false,\"message\":\"Access Denied: Staff accounts cannot access the mobile dashboard. Only Admins are authorized.\"}";
                await SendResponseAsync(stream, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(err), 403, "Forbidden");
            }
            else
            {
                Logger.LogWarning($"Mobile Dashboard login failed for username: {username}");
                string err = "{\"success\":false,\"message\":\"Invalid Admin credentials. Please check your username and password.\"}";
                await SendResponseAsync(stream, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(err), 401, "Unauthorized");
            }
        }

        private static async Task HandleRestockAsync(NetworkStream stream, string rawPath, string requestBody, string adminUser)
        {
            string barcodeStr = ExtractParam(rawPath, requestBody, "barcode") ?? ExtractParam(rawPath, requestBody, "id");
            string qtyStr = ExtractParam(rawPath, requestBody, "qty") ?? ExtractParam(rawPath, requestBody, "quantity");

            if (string.IsNullOrEmpty(barcodeStr) || !int.TryParse(qtyStr, out int addQty) || addQty <= 0)
            {
                string err = "{\"success\":false,\"message\":\"Valid product barcode and restock quantity are required.\"}";
                await SendResponseAsync(stream, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(err), 400, "Bad Request");
                return;
            }

            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();

                    // Find product
                    string findQuery = @"
                        SELECT p.ID, p.PName, ISNULL(SUM(s.quantity), 0) AS CurrentQty
                        FROM Products p
                        LEFT JOIN Stocks s ON p.ID = s.barcode_id AND s.IsActive = 1
                        WHERE (CAST(p.Barcode AS NVARCHAR) = @bcode OR CAST(p.ID AS NVARCHAR) = @bcode) AND p.IsActive = 1
                        GROUP BY p.ID, p.PName";

                    int productId = 0;
                    string productName = "";
                    int currentQty = 0;

                    using (SqlCommand cmd = new SqlCommand(findQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@bcode", barcodeStr);
                        using (SqlDataReader r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                productId = Convert.ToInt32(r["ID"]);
                                productName = r["PName"].ToString();
                                currentQty = Convert.ToInt32(r["CurrentQty"]);
                            }
                        }
                    }

                    if (productId == 0)
                    {
                        string err = "{\"success\":false,\"message\":\"Product not found.\"}";
                        await SendResponseAsync(stream, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(err), 404, "Not Found");
                        return;
                    }

                    // Update existing stock record or insert new batch
                    string updateStock = @"
                        IF EXISTS (SELECT 1 FROM Stocks WHERE barcode_id = @pid AND IsActive = 1)
                        BEGIN
                            UPDATE TOP (1) Stocks
                            SET quantity = quantity + @qty
                            WHERE barcode_id = @pid AND IsActive = 1
                        END
                        ELSE
                        BEGIN
                            INSERT INTO Stocks (barcode_id, stock_code, quantity, minimum_stock, cp_unit, sp_unit, arrival_date)
                            VALUES (@pid, 'RESTOCK-MOB', @qty, 5, 0.00, 0.00, CAST(GETDATE() AS DATE))
                        END";

                    using (SqlCommand cmd = new SqlCommand(updateStock, conn))
                    {
                        cmd.Parameters.AddWithValue("@pid", productId);
                        cmd.Parameters.AddWithValue("@qty", addQty);
                        cmd.ExecuteNonQuery();
                    }

                    int newTotal = currentQty + addQty;
                    ShiftManager.LogActivity($"Admin '{adminUser}' restocked +{addQty} units for '{productName}' via Phone (New Total: {newTotal})");
                    Logger.LogInfo($"Mobile Restock: Admin {adminUser} added {addQty} units to product #{productId} ({productName})");

                    string res = $"{{\"success\":true,\"message\":\"Successfully restocked +{addQty} units for {EscapeJson(productName)}!\",\"newQty\":{newTotal}}}";
                    await SendResponseAsync(stream, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(res));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error processing mobile restock", ex);
                string err = $"{{\"success\":false,\"message\":\"Restock error: {EscapeJson(ex.Message)}\"}}";
                await SendResponseAsync(stream, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(err), 500, "Internal Server Error");
            }
        }

        private static string ExtractToken(string rawPath, string rawRequest, string body)
        {
            int qIdx = rawPath.IndexOf('?');
            if (qIdx >= 0)
            {
                string qs = rawPath.Substring(qIdx + 1);
                var pairs = qs.Split('&');
                foreach (var p in pairs)
                {
                    var kv = p.Split('=');
                    if (kv.Length >= 2 && kv[0].Trim().Equals("token", StringComparison.OrdinalIgnoreCase))
                    {
                        return Uri.UnescapeDataString(kv[1].Trim());
                    }
                }
            }

            var lines = rawRequest.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                if (line.StartsWith("Authorization:", StringComparison.OrdinalIgnoreCase))
                {
                    string val = line.Substring("Authorization:".Length).Trim();
                    if (val.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        return val.Substring("Bearer ".Length).Trim();
                    }
                    return val;
                }
            }

            if (!string.IsNullOrWhiteSpace(body) && body.Contains("\"token\""))
            {
                return ExtractJsonString(body, "token");
            }

            return null;
        }

        private static string ExtractParam(string rawPath, string body, string paramName)
        {
            int qIdx = rawPath.IndexOf('?');
            if (qIdx >= 0)
            {
                string qs = rawPath.Substring(qIdx + 1);
                var pairs = qs.Split('&');
                foreach (var p in pairs)
                {
                    var kv = p.Split('=');
                    if (kv.Length >= 2 && kv[0].Trim().Equals(paramName, StringComparison.OrdinalIgnoreCase))
                    {
                        return Uri.UnescapeDataString(kv[1].Replace("+", " ").Trim());
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(body))
            {
                string jsonVal = ExtractJsonString(body, paramName);
                if (!string.IsNullOrEmpty(jsonVal)) return jsonVal;

                var pairs = body.Split('&');
                foreach (var p in pairs)
                {
                    var kv = p.Split('=');
                    if (kv.Length >= 2 && kv[0].Trim().Equals(paramName, StringComparison.OrdinalIgnoreCase))
                    {
                        return Uri.UnescapeDataString(kv[1].Replace("+", " ").Trim());
                    }
                }
            }

            return null;
        }

        private static string ExtractJsonString(string json, string key)
        {
            try
            {
                string searchKey = $"\"{key}\"";
                int idx = json.IndexOf(searchKey, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) return null;

                int colon = json.IndexOf(':', idx + searchKey.Length);
                if (colon < 0) return null;

                int startQuote = json.IndexOf('\"', colon + 1);
                if (startQuote < 0) return null;

                int endQuote = json.IndexOf('\"', startQuote + 1);
                if (endQuote < 0) return null;

                return json.Substring(startQuote + 1, endQuote - startQuote - 1);
            }
            catch
            {
                return null;
            }
        }

        private static async Task SendResponseAsync(NetworkStream stream, string contentType, byte[] body, int statusCode = 200, string statusText = "OK")
        {
            string headers = $"HTTP/1.1 {statusCode} {statusText}\r\n" +
                             $"Content-Type: {contentType}\r\n" +
                             $"Content-Length: {body.Length}\r\n" +
                             "Access-Control-Allow-Origin: *\r\n" +
                             "Access-Control-Allow-Methods: GET, POST, OPTIONS\r\n" +
                             "Access-Control-Allow-Headers: Content-Type, Authorization\r\n" +
                             "Connection: close\r\n\r\n";

            byte[] headerBytes = Encoding.UTF8.GetBytes(headers);
            await stream.WriteAsync(headerBytes, 0, headerBytes.Length);
            await stream.WriteAsync(body, 0, body.Length);
            await stream.FlushAsync();
        }

        /// <summary>
        /// Gathers full telemetry: Today's sales, items sold, complete inventory with remaining quantities,
        /// finished products (0 stock), active staff on duty, shifts, and "who does what" timeline.
        /// </summary>
        private static string GetComprehensiveDashboardJson()
        {
            var kpis = Report.GetDashboardKpis();
            bool isAmh = LocalizationManager.CurrentLanguage == AppLanguage.Amharic;

            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"todaySales\":\"{EscapeJson(LocalizationManager.FormatMoney(kpis.TodaySales))}\",");
            sb.Append($"\"todayOrders\":{kpis.TodayOrders},");
            sb.Append($"\"totalProducts\":{kpis.TotalProducts},");
            sb.Append($"\"lowStockCount\":{kpis.LowStockItems},");
            sb.Append($"\"time\":\"{DateTime.Now:hh:mm tt}\",");
            sb.Append($"\"date\":\"{DateTime.Now:dd MMM yyyy}\",");
            sb.Append($"\"isAmharic\":{(isAmh ? "true" : "false")},");

            // 1. Active Staff on Duty
            var activeShifts = ShiftManager.GetActiveShifts();
            sb.Append("\"activeCashiers\":[");
            for (int i = 0; i < activeShifts.Count; i++)
            {
                var s = activeShifts[i];
                sb.Append("{");
                sb.Append($"\"shiftId\":{s.ShiftId},");
                sb.Append($"\"name\":\"{EscapeJson(s.Username)}\",");
                sb.Append($"\"role\":\"{EscapeJson(s.Role)}\",");
                sb.Append($"\"startTime\":\"{s.ShiftStart:hh:mm tt}\",");
                sb.Append($"\"salesCount\":{s.TotalSalesCount},");
                sb.Append($"\"salesTotal\":\"{EscapeJson(LocalizationManager.FormatMoney(s.TotalSalesAmount))}\"");
                sb.Append("}");
                if (i < activeShifts.Count - 1) sb.Append(",");
            }
            sb.Append("],");

            // 2. Recent Orders (What was sold & by whom)
            sb.Append("\"recentOrders\":[");
            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string q = @"
                    SELECT TOP 12 
                        o.OrderID, 
                        o.Date, 
                        o.TotalPayable, 
                        ISNULL(o.PaymentMethod, 'Cash') AS Payment,
                        ISNULL(o.CashierName, 'Cashier') AS Cashier,
                        (SELECT COUNT(*) FROM OrderDetails od WHERE od.OrderID = o.OrderID) AS ItemCount
                    FROM Orders o
                    ORDER BY o.OrderID DESC";

                    using (SqlCommand cmd = new SqlCommand(q, conn))
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        bool first = true;
                        while (r.Read())
                        {
                            if (!first) sb.Append(",");
                            first = false;
                            sb.Append("{");
                            sb.Append($"\"id\":\"#{r["OrderID"]}\",");
                            sb.Append($"\"cashier\":\"{EscapeJson(r["Cashier"].ToString())}\",");
                            sb.Append($"\"amount\":\"{EscapeJson(LocalizationManager.FormatMoney(Convert.ToDecimal(r["TotalPayable"])))}\",");
                            sb.Append($"\"payment\":\"{EscapeJson(r["Payment"].ToString())}\",");
                            sb.Append($"\"items\":{Convert.ToInt32(r["ItemCount"])},");
                            DateTime dt = Convert.ToDateTime(r["Date"]);
                            sb.Append($"\"time\":\"{dt:hh:mm tt}\"");
                            sb.Append("}");
                        }
                    }
                }
            }
            catch { }
            sb.Append("],");

            // 3. Complete Inventory (What remains & what is finished)
            sb.Append("\"inventory\":[");
            int outOfStockCount = 0;
            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            p.ID,
                            p.Barcode, 
                            p.PName, 
                            p.Category,
                            ISNULL(SUM(s.quantity), 0) AS CurrentQty,
                            ISNULL(MAX(s.minimum_stock), 5) AS MinStock,
                            ISNULL(MAX(s.sp_unit), 0.00) AS SellingPrice
                        FROM Products p
                        LEFT JOIN Stocks s ON p.ID = s.barcode_id AND s.IsActive = 1
                        WHERE p.IsActive = 1
                        GROUP BY p.ID, p.Barcode, p.PName, p.Category
                        ORDER BY CurrentQty ASC, p.PName ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        bool first = true;
                        while (r.Read())
                        {
                            if (!first) sb.Append(",");
                            first = false;

                            int qty = Convert.ToInt32(r["CurrentQty"]);
                            int min = Convert.ToInt32(r["MinStock"]);
                            decimal price = Convert.ToDecimal(r["SellingPrice"]);

                            string status = "in_stock";
                            if (qty == 0)
                            {
                                status = "finished";
                                outOfStockCount++;
                            }
                            else if (qty < min)
                            {
                                status = "low";
                            }

                            sb.Append("{");
                            sb.Append($"\"id\":{r["ID"]},");
                            sb.Append($"\"barcode\":\"{r["Barcode"]}\",");
                            sb.Append($"\"name\":\"{EscapeJson(r["PName"].ToString())}\",");
                            sb.Append($"\"category\":\"{EscapeJson(r["Category"].ToString())}\",");
                            sb.Append($"\"qty\":{qty},");
                            sb.Append($"\"min\":{min},");
                            sb.Append($"\"price\":\"{EscapeJson(LocalizationManager.FormatMoney(price))}\",");
                            sb.Append($"\"status\":\"{status}\"");
                            sb.Append("}");
                        }
                    }
                }
            }
            catch { }
            sb.Append("],");

            sb.Append($"\"outOfStockCount\":{outOfStockCount},");

            // 4. Staff Activity Feed ("Who does what")
            var activities = ShiftManager.GetRecentActivities(15);
            sb.Append("\"activities\":[");
            for (int i = 0; i < activities.Count; i++)
            {
                var a = activities[i];
                sb.Append("{");
                sb.Append($"\"msg\":\"{EscapeJson(a.Message)}\",");
                sb.Append($"\"time\":\"{a.ActionTime:hh:mm tt}\"");
                sb.Append("}");
                if (i < activities.Count - 1) sb.Append(",");
            }
            sb.Append("],");

            // 5. Recent Staff Shifts
            var shifts = ShiftManager.GetRecentShifts(8);
            sb.Append("\"shifts\":[");
            for (int i = 0; i < shifts.Count; i++)
            {
                var s = shifts[i];
                sb.Append("{");
                sb.Append($"\"id\":{s.ShiftId},");
                sb.Append($"\"name\":\"{EscapeJson(s.Username)}\",");
                sb.Append($"\"role\":\"{EscapeJson(s.Role)}\",");
                sb.Append($"\"start\":\"{s.ShiftStart:dd MMM, hh:mm tt}\",");
                sb.Append($"\"end\":\"{(s.ShiftEnd.HasValue ? s.ShiftEnd.Value.ToString("hh:mm tt") : "Active")}\",");
                sb.Append($"\"salesCount\":{s.TotalSalesCount},");
                sb.Append($"\"salesAmount\":\"{EscapeJson(LocalizationManager.FormatMoney(s.TotalSalesAmount))}\",");
                sb.Append($"\"status\":\"{s.Status}\"");
                sb.Append("}");
                if (i < shifts.Count - 1) sb.Append(",");
            }
            sb.Append("]}");

            return sb.ToString();
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ");
        }

        private static string GenerateMobileHtml()
        {
            return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no"">
    <title>Stock-Sync Admin Control</title>
    <meta name=""theme-color"" content=""#FF5B26"">
    <meta name=""apple-mobile-web-app-capable"" content=""yes"">
    <meta name=""apple-mobile-web-app-status-bar-style"" content=""black-translucent"">
    <link rel=""preconnect"" href=""https://fonts.googleapis.com"">
    <link href=""https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;600;700;800&display=swap"" rel=""stylesheet"">
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; -webkit-tap-highlight-color: transparent; }
        body {
            font-family: 'Plus Jakarta Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: #F8FAFC;
            color: #0F172A;
            padding-bottom: 85px;
            min-height: 100vh;
        }

        /* Top Navigation Header */
        .topbar {
            background: #FFFFFF;
            padding: 12px 16px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            border-bottom: 1px solid #E2E8F0;
            position: sticky;
            top: 0;
            z-index: 100;
            box-shadow: 0 1px 3px rgba(0,0,0,0.03);
        }
        .brand {
            display: flex;
            align-items: center;
            gap: 10px;
        }
        .brand-badge {
            width: 36px;
            height: 36px;
            background: #FF5B26;
            color: white;
            border-radius: 10px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 18px;
            box-shadow: 0 4px 12px rgba(255, 91, 38, 0.25);
        }
        .brand-info h1 {
            font-size: 15px;
            font-weight: 800;
            color: #0F172A;
            line-height: 1.1;
        }
        .brand-info span {
            font-size: 11px;
            color: #64748B;
            display: flex;
            align-items: center;
            gap: 4px;
        }
        .pulse {
            width: 7px;
            height: 7px;
            background: #10B981;
            border-radius: 50%;
            display: inline-block;
            box-shadow: 0 0 0 2px rgba(16, 185, 129, 0.2);
            animation: pulse-ring 2s infinite;
        }
        @keyframes pulse-ring {
            0% { box-shadow: 0 0 0 0 rgba(16, 185, 129, 0.5); }
            70% { box-shadow: 0 0 0 6px rgba(16, 185, 129, 0); }
            100% { box-shadow: 0 0 0 0 rgba(16, 185, 129, 0); }
        }

        .header-actions {
            display: flex;
            align-items: center;
            gap: 8px;
        }
        .btn-pill {
            background: #F1F5F9;
            color: #334155;
            border: none;
            padding: 6px 12px;
            border-radius: 20px;
            font-size: 11.5px;
            font-weight: 700;
            cursor: pointer;
        }
        .btn-logout {
            background: #FEE2E2;
            color: #DC2626;
            border: none;
            padding: 6px 11px;
            border-radius: 20px;
            font-size: 11.5px;
            font-weight: 700;
            cursor: pointer;
        }

        /* Container */
        .container {
            padding: 16px;
            max-width: 600px;
            margin: 0 auto;
        }

        /* Login Card */
        .login-card {
            background: #FFFFFF;
            border-radius: 20px;
            padding: 32px 24px;
            margin-top: 30px;
            box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.05);
            border: 1px solid #E2E8F0;
            text-align: center;
        }
        .login-badge {
            width: 58px;
            height: 58px;
            background: #FF5B26;
            color: white;
            border-radius: 16px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 28px;
            margin: 0 auto 16px auto;
            box-shadow: 0 8px 20px rgba(255, 91, 38, 0.3);
        }
        .login-title {
            font-size: 20px;
            font-weight: 800;
            color: #0F172A;
            margin-bottom: 6px;
        }
        .login-sub {
            font-size: 13px;
            color: #64748B;
            margin-bottom: 24px;
            line-height: 1.4;
        }
        .security-badge {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            background: #EFF6FF;
            color: #2563EB;
            font-size: 11.5px;
            font-weight: 700;
            padding: 5px 12px;
            border-radius: 20px;
            margin-bottom: 20px;
        }
        .form-group {
            text-align: left;
            margin-bottom: 16px;
        }
        .form-group label {
            display: block;
            font-size: 11.5px;
            font-weight: 700;
            color: #475569;
            margin-bottom: 6px;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }
        .form-input {
            width: 100%;
            padding: 12px 14px;
            font-size: 15px;
            border: 1.5px solid #CBD5E1;
            border-radius: 10px;
            font-family: inherit;
            color: #0F172A;
            background: #FFFFFF;
        }
        .form-input:focus {
            outline: none;
            border-color: #FF5B26;
            box-shadow: 0 0 0 3px rgba(255, 91, 38, 0.15);
        }
        .btn-submit {
            width: 100%;
            background: #FF5B26;
            color: white;
            border: none;
            padding: 13px;
            border-radius: 10px;
            font-size: 15px;
            font-weight: 800;
            cursor: pointer;
            box-shadow: 0 4px 14px rgba(255, 91, 38, 0.35);
            margin-top: 8px;
        }
        .error-box {
            display: none;
            background: #FEF2F2;
            color: #DC2626;
            border: 1px solid #FCA5A5;
            padding: 10px 14px;
            border-radius: 8px;
            font-size: 12.5px;
            font-weight: 600;
            margin-bottom: 16px;
            text-align: left;
        }

        /* Sales Hero */
        .sales-hero {
            background: linear-gradient(135deg, #FF5B26 0%, #FF8A00 100%);
            border-radius: 18px;
            padding: 20px;
            color: white;
            box-shadow: 0 10px 25px rgba(255, 91, 38, 0.3);
            margin-bottom: 16px;
            position: relative;
            overflow: hidden;
        }
        .sales-hero::after {
            content: 'ETB';
            position: absolute;
            right: -10px;
            bottom: -20px;
            font-size: 80px;
            font-weight: 900;
            color: rgba(255, 255, 255, 0.08);
            pointer-events: none;
        }
        .sales-hero-label {
            font-size: 12px;
            font-weight: 700;
            text-transform: uppercase;
            letter-spacing: 0.8px;
            opacity: 0.9;
            margin-bottom: 6px;
        }
        .sales-hero-val {
            font-size: 30px;
            font-weight: 800;
        }
        .sales-hero-sub {
            font-size: 11.5px;
            opacity: 0.85;
            margin-top: 6px;
        }

        .metric-grid {
            display: grid;
            grid-template-columns: repeat(4, 1fr);
            gap: 8px;
            margin-bottom: 16px;
        }
        .metric-card {
            background: #FFFFFF;
            border-radius: 12px;
            padding: 12px 6px;
            text-align: center;
            border: 1px solid #E2E8F0;
        }
        .metric-val {
            font-size: 18px;
            font-weight: 800;
            color: #0F172A;
        }
        .metric-label {
            font-size: 9.5px;
            font-weight: 700;
            color: #64748B;
            margin-top: 4px;
            text-transform: uppercase;
        }
        .alert-val { color: #F59E0B; }
        .danger-val { color: #EF4444; }

        .section-card {
            background: #FFFFFF;
            border-radius: 16px;
            padding: 16px;
            border: 1px solid #E2E8F0;
            margin-bottom: 16px;
            box-shadow: 0 2px 6px rgba(0,0,0,0.02);
        }
        .section-header {
            display: flex;
            align-items: center;
            justify-content: space-between;
            margin-bottom: 12px;
            border-bottom: 1px solid #F1F5F9;
            padding-bottom: 8px;
        }
        .section-title {
            font-size: 13.5px;
            font-weight: 800;
            color: #1E293B;
        }

        /* Order Rows */
        .order-row {
            display: flex;
            align-items: center;
            justify-content: space-between;
            padding: 10px 0;
            border-bottom: 1px solid #F8FAFC;
        }
        .order-row:last-child { border-bottom: none; }
        .order-id { font-size: 12px; font-weight: 800; color: #FF5B26; }
        .order-name { font-size: 12px; color: #475569; font-weight: 600; margin-top: 2px; }
        .order-price { font-size: 13.5px; font-weight: 800; color: #0F172A; text-align: right; }

        /* Inventory Items */
        .filter-strip {
            display: flex;
            gap: 6px;
            margin-bottom: 12px;
            overflow-x: auto;
            padding-bottom: 4px;
        }
        .filter-chip {
            background: #F1F5F9;
            color: #475569;
            border: none;
            padding: 6px 12px;
            border-radius: 20px;
            font-size: 11.5px;
            font-weight: 700;
            cursor: pointer;
            white-space: nowrap;
        }
        .filter-chip.active {
            background: #FF5B26;
            color: white;
        }
        .search-box {
            width: 100%;
            padding: 10px 14px;
            font-size: 13.5px;
            border: 1px solid #CBD5E1;
            border-radius: 10px;
            margin-bottom: 12px;
            font-family: inherit;
        }
        .inv-card {
            background: #FFFFFF;
            border-radius: 12px;
            padding: 12px 14px;
            border: 1px solid #E2E8F0;
            margin-bottom: 10px;
            display: flex;
            align-items: center;
            justify-content: space-between;
        }
        .inv-card.finished { border-left: 4px solid #EF4444; }
        .inv-card.low { border-left: 4px solid #F59E0B; }
        .inv-card.in_stock { border-left: 4px solid #10B981; }

        .inv-name { font-size: 13px; font-weight: 800; color: #1E293B; }
        .inv-meta { font-size: 11px; color: #64748B; margin-top: 3px; }
        .stock-badge {
            display: inline-block;
            padding: 3px 8px;
            border-radius: 6px;
            font-size: 11px;
            font-weight: 800;
            margin-top: 4px;
        }
        .stock-badge.finished { background: #FEE2E2; color: #DC2626; }
        .stock-badge.low { background: #FEF3C7; color: #D97706; }
        .stock-badge.in_stock { background: #D1FAE5; color: #059669; }

        .btn-restock {
            background: #FF5B26;
            color: white;
            border: none;
            padding: 7px 12px;
            border-radius: 8px;
            font-size: 11.5px;
            font-weight: 800;
            cursor: pointer;
            white-space: nowrap;
            box-shadow: 0 2px 6px rgba(255, 91, 38, 0.25);
        }

        /* Staff Shift Cards */
        .shift-badge-active {
            background: #D1FAE5;
            color: #065F46;
            font-size: 11px;
            font-weight: 800;
            padding: 4px 8px;
            border-radius: 6px;
        }
        .activity-row {
            padding: 9px 0;
            border-bottom: 1px solid #F1F5F9;
            font-size: 12px;
        }
        .activity-row:last-child { border-bottom: none; }
        .activity-time { font-size: 10.5px; color: #94A3B8; font-weight: 700; }
        .activity-msg { font-weight: 600; color: #334155; margin-top: 2px; }

        /* Bottom Tab Bar */
        .bottom-nav {
            position: fixed;
            bottom: 0;
            left: 0;
            right: 0;
            background: #FFFFFF;
            border-top: 1px solid #E2E8F0;
            display: flex;
            justify-content: space-around;
            padding: 8px 0;
            z-index: 99;
            box-shadow: 0 -2px 10px rgba(0,0,0,0.03);
        }
        .nav-item {
            display: flex;
            flex-direction: column;
            align-items: center;
            font-size: 11px;
            font-weight: 700;
            color: #64748B;
            background: none;
            border: none;
            cursor: pointer;
            gap: 3px;
        }
        .nav-item.active {
            color: #FF5B26;
        }
        .nav-icon { font-size: 18px; }

        /* Restock Modal */
        .modal-overlay {
            display: none;
            position: fixed;
            top: 0; left: 0; right: 0; bottom: 0;
            background: rgba(15, 23, 42, 0.6);
            backdrop-filter: blur(4px);
            z-index: 200;
            align-items: center;
            justify-content: center;
            padding: 16px;
        }
        .modal-card {
            background: white;
            border-radius: 20px;
            padding: 24px;
            width: 100%;
            max-width: 400px;
            box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1);
        }
        .modal-title { font-size: 16px; font-weight: 800; color: #0F172A; margin-bottom: 4px; }
        .modal-sub { font-size: 12px; color: #64748B; margin-bottom: 16px; }
        .qty-presets { display: flex; gap: 8px; margin: 10px 0 16px 0; }
        .preset-btn {
            flex: 1;
            padding: 8px;
            background: #F1F5F9;
            border: 1px solid #CBD5E1;
            border-radius: 8px;
            font-weight: 800;
            font-size: 12px;
            color: #334155;
            cursor: pointer;
        }
    </style>
</head>
<body>

    <!-- TOP BAR -->
    <div class=""topbar"">
        <div class=""brand"">
            <div class=""brand-badge"">🛒</div>
            <div class=""brand-info"">
                <h1>Stock-Sync</h1>
                <span><i class=""pulse""></i> Live Admin</span>
            </div>
        </div>
        <div class=""header-actions"">
            <button class=""btn-pill"" id=""btnLang"" onclick=""toggleLang()"">🌐 አማርኛ</button>
            <button class=""btn-logout"" id=""btnLogout"" style=""display:none;"" onclick=""doLogout()"">🚪 Exit</button>
        </div>
    </div>

    <!-- MAIN CONTAINER -->
    <div class=""container"">

        <!-- 1. ADMIN LOGIN VIEW -->
        <div id=""viewLogin"" class=""login-card"">
            <div class=""login-badge"">👑</div>
            <div class=""security-badge"">🔒 Admin Access Only</div>
            <h2 class=""login-title"" id=""txtLoginTitle"">Stock-Sync Admin Control</h2>
            <p class=""login-sub"" id=""txtLoginSub"">Sign in with your Admin credentials to monitor sales, restock products, and review staff shifts.</p>

            <div id=""loginError"" class=""error-box""></div>

            <form onsubmit=""doLogin(event)"">
                <div class=""form-group"">
                    <label id=""lblUserTag"">Admin Username</label>
                    <input type=""text"" id=""txtUsername"" class=""form-input"" placeholder=""e.g. admin"" autocomplete=""username"" required />
                </div>
                <div class=""form-group"">
                    <label id=""lblPassTag"">Admin Password</label>
                    <input type=""password"" id=""txtPassword"" class=""form-input"" placeholder=""Enter password"" autocomplete=""current-password"" required />
                </div>
                <button type=""submit"" id=""btnSubmitLogin"" class=""btn-submit"">🔐 Sign In as Admin</button>
            </form>

            <div style=""margin-top:20px; font-size:11.5px; color:#94A3B8;"">
                ⚠️ Staff accounts cannot access this portal. Admin credentials required.
            </div>
        </div>

        <!-- 2. TAB 1: SALES & TELEMETRY -->
        <div id=""tabSales"" style=""display:none;"">
            <div style=""display:flex; justify-content:space-between; margin-bottom:12px; font-size:12px; font-weight:700; color:#64748B;"">
                <span id=""valDate"">--</span>
                <span style=""color:#FF5B26; background:#FFF1EB; padding:3px 10px; border-radius:12px;"" id=""valTime"">--:--</span>
            </div>

            <!-- Revenue Hero Card -->
            <div class=""sales-hero"">
                <div class=""sales-hero-label"" id=""lblSalesTitle"">Today's Revenue</div>
                <div class=""sales-hero-val"" id=""valSales"">0.00 ETB</div>
                <div class=""sales-hero-sub"">Live retail checkouts • በብር የተገኘ ጠቅላላ ገቢ</div>
            </div>

            <!-- 4 Mini KPIs -->
            <div class=""metric-grid"">
                <div class=""metric-card"">
                    <div class=""metric-val"" id=""valOrders"">0</div>
                    <div class=""metric-label"" id=""lblOrdersTitle"">Invoices</div>
                </div>
                <div class=""metric-card"">
                    <div class=""metric-val"" id=""valProducts"">0</div>
                    <div class=""metric-label"" id=""lblProductsTitle"">Products</div>
                </div>
                <div class=""metric-card"">
                    <div class=""metric-val alert-val"" id=""valLowStock"">0</div>
                    <div class=""metric-label"" id=""lblAlertsTitle"">Low Stock</div>
                </div>
                <div class=""metric-card"">
                    <div class=""metric-val danger-val"" id=""valOutOfStock"">0</div>
                    <div class=""metric-label"" id=""lblFinishedTitle"">Finished</div>
                </div>
            </div>

            <!-- Active Cashiers on Duty -->
            <div class=""section-card"">
                <div class=""section-header"">
                    <div class=""section-title"" id=""lblStaffOnDuty"">🟢 Staff Currently on Duty</div>
                </div>
                <div id=""activeStaffList"">
                    <div style=""text-align:center; color:#94A3B8; font-size:12px; padding:10px;"">Checking staff status...</div>
                </div>
            </div>

            <!-- Recent Checkouts (Who sold what) -->
            <div class=""section-card"">
                <div class=""section-header"">
                    <div class=""section-title"" id=""lblRecentTitle"">🧾 Recent Checkouts (What was Sold)</div>
                </div>
                <div id=""ordersList"">
                    <div style=""text-align:center; color:#94A3B8; font-size:12px; padding:10px;"">Loading recent checkouts...</div>
                </div>
            </div>
        </div>

        <!-- 3. TAB 2: INVENTORY & RESTOCK (See what remains & restock finished) -->
        <div id=""tabInventory"" style=""display:none;"">
            <div class=""section-header"" style=""margin-bottom:8px;"">
                <div class=""section-title"" id=""lblInvTitle"">📦 Remaining Stock & 1-Tap Restock</div>
            </div>

            <!-- Filter Chips -->
            <div class=""filter-strip"">
                <button class=""filter-chip active"" onclick=""setInvFilter('all', this)"" id=""fChipAll"">All Items</button>
                <button class=""filter-chip"" onclick=""setInvFilter('finished', this)"" id=""fChipFinished"">🚨 Finished (0 Qty)</button>
                <button class=""filter-chip"" onclick=""setInvFilter('low', this)"" id=""fChipLow"">⚠️ Low Stock (< Min)</button>
            </div>

            <input type=""text"" id=""invSearch"" class=""search-box"" placeholder=""🔍 Search product name or barcode..."" oninput=""renderInventory()"" />

            <!-- Product Items Feed -->
            <div id=""inventoryFeed"">
                <div style=""text-align:center; color:#94A3B8; font-size:12px; padding:14px;"">Loading store catalog...</div>
            </div>
        </div>

        <!-- 4. TAB 3: STAFF SHIFTS & ACTIVITY (Who was on shift & who does what) -->
        <div id=""tabShifts"" style=""display:none;"">
            <!-- Active Shift Card -->
            <div class=""section-card"">
                <div class=""section-header"">
                    <div class=""section-title"" id=""lblActiveShiftsTitle"">⏱️ Active Cashier Shifts</div>
                </div>
                <div id=""shiftsActiveFeed""></div>
            </div>

            <!-- Who Does What (Live Action Feed) -->
            <div class=""section-card"">
                <div class=""section-header"">
                    <div class=""section-title"" id=""lblActivityTitle"">⚡ Who Did What (Live Audit)</div>
                </div>
                <div id=""activityFeed"">
                    <div style=""text-align:center; color:#94A3B8; font-size:12px; padding:10px;"">Loading store audit trail...</div>
                </div>
            </div>

            <!-- Past Shifts History -->
            <div class=""section-card"">
                <div class=""section-header"">
                    <div class=""section-title"" id=""lblHistoryShiftsTitle"">📋 Shift History & Performance</div>
                </div>
                <div id=""shiftsHistoryFeed""></div>
            </div>
        </div>

    </div>

    <!-- BOTTOM TAB BAR (Visible when logged in) -->
    <div id=""bottomNav"" class=""bottom-nav"" style=""display:none;"">
        <button class=""nav-item active"" onclick=""switchTab('sales', this)"">
            <span class=""nav-icon"">📊</span>
            <span id=""navSales"">Sales</span>
        </button>
        <button class=""nav-item"" onclick=""switchTab('inventory', this)"">
            <span class=""nav-icon"">📦</span>
            <span id=""navInv"">Inventory & Restock</span>
        </button>
        <button class=""nav-item"" onclick=""switchTab('shifts', this)"">
            <span class=""nav-icon"">👥</span>
            <span id=""navShifts"">Staff Shifts</span>
        </button>
    </div>

    <!-- RESTOCK MODAL (Popup on phone) -->
    <div id=""modalRestock"" class=""modal-overlay"">
        <div class=""modal-card"">
            <div class=""modal-title"" id=""restockProdName"">Restock Product</div>
            <div class=""modal-sub"" id=""restockProdSub"">Barcode: -- • Current: 0 units</div>

            <div class=""form-group"">
                <label id=""lblRestockAddQty"">Quantity to Add</label>
                <input type=""number"" id=""txtRestockQty"" class=""form-input"" value=""50"" min=""1"" />
            </div>

            <div class=""qty-presets"">
                <button class=""preset-btn"" onclick=""setRestockAdd(10)"">+10</button>
                <button class=""preset-btn"" onclick=""setRestockAdd(25)"">+25</button>
                <button class=""preset-btn"" onclick=""setRestockAdd(50)"">+50</button>
                <button class=""preset-btn"" onclick=""setRestockAdd(100)"">+100</button>
            </div>

            <div style=""display:flex; gap:10px; margin-top:14px;"">
                <button style=""flex:1; background:#E2E8F0; color:#334155; border:none; padding:12px; border-radius:10px; font-weight:700;"" onclick=""closeRestockModal()"">Cancel</button>
                <button style=""flex:2; background:#FF5B26; color:white; border:none; padding:12px; border-radius:10px; font-weight:800;"" id=""btnConfirmRestock"" onclick=""submitRestock()"">✅ Confirm Restock</button>
            </div>
        </div>
    </div>

    <script>
        const TOKEN_KEY = 'stock_sync_admin_token';
        let currentTab = 'sales';
        let invFilter = 'all';
        let globalData = null;
        let selectedProductForRestock = null;
        let isAmharic = false;

        function getActiveToken() {
            return localStorage.getItem(TOKEN_KEY) || '';
        }

        function showApp() {
            document.getElementById('viewLogin').style.display = 'none';
            document.getElementById('bottomNav').style.display = 'flex';
            document.getElementById('btnLogout').style.display = 'inline-block';
            switchTab(currentTab);
            fetchData();
        }

        function showLogin(errMsg) {
            document.getElementById('viewLogin').style.display = 'block';
            document.getElementById('tabSales').style.display = 'none';
            document.getElementById('tabInventory').style.display = 'none';
            document.getElementById('tabShifts').style.display = 'none';
            document.getElementById('bottomNav').style.display = 'none';
            document.getElementById('btnLogout').style.display = 'none';

            const errBox = document.getElementById('loginError');
            if (errMsg) {
                errBox.innerText = errMsg;
                errBox.style.display = 'block';
            } else {
                errBox.style.display = 'none';
            }
        }

        function switchTab(tab, btnElem) {
            currentTab = tab;
            document.getElementById('tabSales').style.display = (tab === 'sales' ? 'block' : 'none');
            document.getElementById('tabInventory').style.display = (tab === 'inventory' ? 'block' : 'none');
            document.getElementById('tabShifts').style.display = (tab === 'shifts' ? 'block' : 'none');

            document.querySelectorAll('.nav-item').forEach(el => el.classList.remove('active'));
            if (btnElem) {
                btnElem.classList.add('active');
            } else {
                const idx = (tab === 'sales' ? 0 : (tab === 'inventory' ? 1 : 2));
                const items = document.querySelectorAll('.nav-item');
                if (items[idx]) items[idx].classList.add('active');
            }

            if (tab === 'inventory') renderInventory();
        }

        async function doLogin(event) {
            event.preventDefault();
            const errBox = document.getElementById('loginError');
            errBox.style.display = 'none';

            const u = document.getElementById('txtUsername').value.trim();
            const p = document.getElementById('txtPassword').value.trim();

            if (!u || !p) return;

            const btn = document.getElementById('btnSubmitLogin');
            btn.disabled = true;
            btn.innerText = 'Verifying...';

            try {
                const res = await fetch('/api/login', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ username: u, password: p })
                });

                const data = await res.json();
                if (res.ok && data.success && data.token) {
                    localStorage.setItem(TOKEN_KEY, data.token);
                    document.getElementById('txtPassword').value = '';
                    showApp();
                } else {
                    showLogin(data.message || 'Login failed. Only Admins can access this portal.');
                }
            } catch (ex) {
                showLogin('Server connection error. Ensure store PC is turned on.');
            } finally {
                btn.disabled = false;
                btn.innerText = '🔐 Sign In as Admin';
            }
        }

        async function doLogout() {
            const token = getActiveToken();
            if (token) {
                try {
                    await fetch('/api/logout?token=' + encodeURIComponent(token));
                } catch(e) {}
            }
            localStorage.removeItem(TOKEN_KEY);
            showLogin();
        }

        async function fetchData() {
            const token = getActiveToken();
            if (!token) {
                showLogin();
                return;
            }

            try {
                const res = await fetch('/api/data?token=' + encodeURIComponent(token) + '&t=' + Date.now());
                if (res.status === 401 || res.status === 403) {
                    localStorage.removeItem(TOKEN_KEY);
                    showLogin('Session expired. Please log in as Admin.');
                    return;
                }

                const data = await res.json();
                globalData = data;
                isAmharic = data.isAmharic;

                // Tab 1: Sales
                document.getElementById('valSales').innerText = data.todaySales;
                document.getElementById('valOrders').innerText = data.todayOrders;
                document.getElementById('valProducts').innerText = data.totalProducts;
                document.getElementById('valLowStock').innerText = data.lowStockCount;
                document.getElementById('valOutOfStock').innerText = data.outOfStockCount || 0;
                document.getElementById('valTime').innerText = data.time;
                document.getElementById('valDate').innerText = data.date;

                document.getElementById('btnLang').innerText = isAmharic ? '🌐 English' : '🌐 አማርኛ';

                // Localized labels
                document.getElementById('lblSalesTitle').innerText = isAmharic ? 'የዛሬ ሽያጭ' : 'Today\'s Revenue';
                document.getElementById('lblOrdersTitle').innerText = isAmharic ? 'ትዕዛዞች' : 'Invoices';
                document.getElementById('lblProductsTitle').innerText = isAmharic ? 'ምርቶች' : 'Products';
                document.getElementById('lblAlertsTitle').innerText = isAmharic ? 'ያነሰ' : 'Low Stock';
                document.getElementById('lblFinishedTitle').innerText = isAmharic ? 'ያለቀ' : 'Finished';
                document.getElementById('lblRecentTitle').innerText = isAmharic ? '🧾 የቅርብ ጊዜ ሽያጮች (የተሸጡት)' : '🧾 Recent Checkouts (What was Sold)';
                document.getElementById('lblStaffOnDuty').innerText = isAmharic ? '🟢 በስራ ላይ ያሉ ሰራተኞች' : '🟢 Staff Currently on Duty';
                document.getElementById('lblInvTitle').innerText = isAmharic ? '📦 የቀረ ክምችት እና መሙላት' : '📦 Remaining Stock & 1-Tap Restock';
                document.getElementById('lblActiveShiftsTitle').innerText = isAmharic ? '⏱️ አሁን በስራ ላይ ያሉ ፈረቃዎች' : '⏱️ Active Cashier Shifts';
                document.getElementById('lblActivityTitle').innerText = isAmharic ? '⚡ የቀጥታ እንቅስቃሴዎች (ማን ምን አደረገ)' : '⚡ Who Did What (Live Audit)';
                document.getElementById('lblHistoryShiftsTitle').innerText = isAmharic ? '📋 ያለፉ ፈረቃዎች ታሪክ' : '📋 Shift History & Performance';

                document.getElementById('navSales').innerText = isAmharic ? 'ሽያጭ' : 'Sales';
                document.getElementById('navInv').innerText = isAmharic ? 'ክምችት & መሙላት' : 'Inventory & Restock';
                document.getElementById('navShifts').innerText = isAmharic ? 'ፈረቃ & ሰራተኞች' : 'Staff Shifts';

                // Active Staff on Duty
                const sStaff = document.getElementById('activeStaffList');
                if (data.activeCashiers && data.activeCashiers.length > 0) {
                    sStaff.innerHTML = data.activeCashiers.map(c => `
                        <div class=""order-row"">
                            <div>
                                <div style=""font-weight:800; font-size:13px; color:#0F172A;"">👤 ${c.name} <span class=""shift-badge-active"">${c.role}</span></div>
                                <div style=""font-size:11px; color:#64748B; margin-top:2px;"">Shift started: ${c.startTime} • ${c.salesCount} sales</div>
                            </div>
                            <div style=""text-align:right;"">
                                <div style=""font-weight:800; font-size:13px; color:#10B981;"">${c.salesTotal}</div>
                                <div style=""font-size:10.5px; color:#10B981; font-weight:700;"">● On POS</div>
                            </div>
                        </div>
                    `).join('');
                } else {
                    sStaff.innerHTML = '<div style=""text-align:center; color:#94A3B8; font-size:12px; padding:10px;"">' + 
                        (isAmharic ? 'አሁን በስራ ላይ ያለ ሰራተኛ የለም (POS አልተከፈተም)' : 'No staff currently active on POS') + '</div>';
                }

                // Recent Orders
                const oList = document.getElementById('ordersList');
                if (data.recentOrders && data.recentOrders.length > 0) {
                    oList.innerHTML = data.recentOrders.map(o => `
                        <div class=""order-row"">
                            <div>
                                <div class=""order-id"">${o.id} • ${o.time}</div>
                                <div class=""order-name"">Cashier: <b>${o.cashier}</b> • ${o.payment} (${o.items} items)</div>
                            </div>
                            <div class=""order-price"">${o.amount}</div>
                        </div>
                    `).join('');
                } else {
                    oList.innerHTML = '<div style=""text-align:center; color:#94A3B8; font-size:12px; padding:10px;"">' + 
                        (isAmharic ? 'ዛሬ ምንም ሽያጭ የለም' : 'No sales recorded today yet') + '</div>';
                }

                // Tab 3: Activities Feed
                const aFeed = document.getElementById('activityFeed');
                if (data.activities && data.activities.length > 0) {
                    aFeed.innerHTML = data.activities.map(a => `
                        <div class=""activity-row"">
                            <div class=""activity-time"">🕒 ${a.time}</div>
                            <div class=""activity-msg"">${a.msg}</div>
                        </div>
                    `).join('');
                } else {
                    aFeed.innerHTML = '<div style=""text-align:center; color:#94A3B8; font-size:12px; padding:10px;"">No audit activities yet</div>';
                }

                // Tab 3: Active & History Shifts
                const shActive = document.getElementById('shiftsActiveFeed');
                const shHistory = document.getElementById('shiftsHistoryFeed');

                if (data.shifts && data.shifts.length > 0) {
                    const activeOnly = data.shifts.filter(s => s.status === 'Active');
                    const historyOnly = data.shifts.filter(s => s.status !== 'Active');

                    shActive.innerHTML = activeOnly.length > 0 ? activeOnly.map(s => `
                        <div class=""order-row"">
                            <div>
                                <div style=""font-weight:800; font-size:13px;"">👤 ${s.name} (${s.role})</div>
                                <div style=""font-size:11px; color:#64748B;"">Started: ${s.start}</div>
                            </div>
                            <div style=""text-align:right;"">
                                <div style=""font-weight:800; color:#0F172A;"">${s.salesAmount}</div>
                                <div style=""font-size:11px; color:#64748B;"">${s.salesCount} orders</div>
                            </div>
                        </div>
                    `).join('') : '<div style=""text-align:center; color:#94A3B8; font-size:12px; padding:8px;"">No active shifts right now</div>';

                    shHistory.innerHTML = historyOnly.length > 0 ? historyOnly.map(s => `
                        <div class=""order-row"">
                            <div>
                                <div style=""font-weight:800; font-size:12.5px;"">👤 ${s.name} (${s.role})</div>
                                <div style=""font-size:11px; color:#64748B;"">${s.start} → ${s.end}</div>
                            </div>
                            <div style=""text-align:right;"">
                                <div style=""font-weight:800; color:#0F172A;"">${s.salesAmount}</div>
                                <div style=""font-size:11px; color:#10B981; font-weight:700;"">Completed</div>
                            </div>
                        </div>
                    `).join('') : '<div style=""text-align:center; color:#94A3B8; font-size:12px; padding:8px;"">No past shifts recorded</div>';
                }

                // Refresh Inventory if on Tab 2
                if (currentTab === 'inventory') {
                    renderInventory();
                }

            } catch(e) {
                console.error(e);
            }
        }

        function setInvFilter(filter, btn) {
            invFilter = filter;
            document.querySelectorAll('.filter-chip').forEach(b => b.classList.remove('active'));
            if (btn) btn.classList.add('active');
            renderInventory();
        }

        function renderInventory() {
            if (!globalData || !globalData.inventory) return;

            const q = (document.getElementById('invSearch').value || '').toLowerCase();
            let items = globalData.inventory;

            if (invFilter === 'finished') {
                items = items.filter(i => i.qty === 0);
            } else if (invFilter === 'low') {
                items = items.filter(i => i.qty > 0 && i.qty < i.min);
            }

            if (q) {
                items = items.filter(i => i.name.toLowerCase().includes(q) || i.barcode.includes(q));
            }

            const feed = document.getElementById('inventoryFeed');
            if (items.length === 0) {
                feed.innerHTML = '<div style=""text-align:center; color:#94A3B8; font-size:12px; padding:20px;"">' + 
                    (isAmharic ? 'ምንም ምርት አልተገኘም' : 'No matching products found') + '</div>';
                return;
            }

            feed.innerHTML = items.map(p => {
                let badgeClass = p.status;
                let badgeText = (p.qty === 0 ? (isAmharic ? '🚨 ያለቀ (0 ቀሪ)' : '🚨 Finished (0 left)') : 
                                (p.qty < p.min ? `${p.qty} ` + (isAmharic ? 'ቀሪ (ያነሰ)' : 'left (Low)') : 
                                `${p.qty} ` + (isAmharic ? 'በክምችት አለ' : 'in stock')));

                return `
                <div class=""inv-card ${p.status}"">
                    <div>
                        <div class=""inv-name"">${p.name}</div>
                        <div class=""inv-meta"">Barcode: <b>${p.barcode}</b> • Price: <b>${p.price}</b></div>
                        <span class=""stock-badge ${badgeClass}"">${badgeText}</span>
                    </div>
                    <div>
                        <button class=""btn-restock"" data-b=""${p.barcode}"" data-n=""${encodeURIComponent(p.name)}"" data-q=""${p.qty}"" onclick=""handleRestockBtn(this)"">
                            ➕ ${isAmharic ? 'ሙላ' : 'Restock'}
                        </button>
                    </div>
                </div>`;
            }).join('');
        }

        function handleRestockBtn(btn) {
            const b = btn.getAttribute('data-b');
            const n = decodeURIComponent(btn.getAttribute('data-n'));
            const q = parseInt(btn.getAttribute('data-q'));
            openRestockModal(b, n, q);
        }

        function openRestockModal(barcode, name, currentQty) {
            selectedProductForRestock = { barcode, name, currentQty };
            document.getElementById('restockProdName').innerText = (isAmharic ? 'ክምችት መሙላት፦ ' : 'Restock: ') + name;
            document.getElementById('restockProdSub').innerText = `Barcode: ${barcode} • ` + (isAmharic ? `አሁን ያለው፦ ${currentQty} ፍሬ` : `Current: ${currentQty} units`);
            document.getElementById('txtRestockQty').value = (currentQty === 0 ? '50' : '25');
            document.getElementById('modalRestock').style.display = 'flex';
        }

        function closeRestockModal() {
            document.getElementById('modalRestock').style.display = 'none';
            selectedProductForRestock = null;
        }

        function setRestockAdd(qty) {
            document.getElementById('txtRestockQty').value = qty;
        }

        async function submitRestock() {
            if (!selectedProductForRestock) return;

            const token = getActiveToken();
            const addQty = parseInt(document.getElementById('txtRestockQty').value);

            if (!addQty || addQty <= 0) {
                alert('Please enter a valid quantity.');
                return;
            }

            const btn = document.getElementById('btnConfirmRestock');
            btn.disabled = true;
            btn.innerText = 'Saving...';

            try {
                const res = await fetch('/api/restock', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        token: token,
                        barcode: selectedProductForRestock.barcode,
                        qty: addQty
                    })
                });

                const data = await res.json();
                if (data.success) {
                    closeRestockModal();
                    await fetchData();
                } else {
                    alert('Error: ' + data.message);
                }
            } catch(e) {
                alert('Connection error. Check store PC.');
            } finally {
                btn.disabled = false;
                btn.innerText = '✅ Confirm Restock';
            }
        }

        async function toggleLang() {
            const token = getActiveToken();
            if (token) {
                await fetch('/api/toggle-lang?token=' + encodeURIComponent(token));
                fetchData();
            } else {
                isAmharic = !isAmharic;
                document.getElementById('btnLang').innerText = isAmharic ? '🌐 English' : '🌐 አማርኛ';
                document.getElementById('txtLoginTitle').innerText = isAmharic ? 'ስቶክ-ሲንክ የአድሚን መዳረሻ' : 'Stock-Sync Admin Control';
                document.getElementById('txtLoginSub').innerText = isAmharic 
                    ? 'ሽያጭ ለመከታተል፣ ምርቶችን ለመሙላት እና የሰራተኞችን ፈረቃ ለመቆጣጠር የአድሚን መለያዎን ያስገቡ።' 
                    : 'Sign in with your Admin credentials to monitor sales, restock products, and review staff shifts.';
                document.getElementById('lblUserTag').innerText = isAmharic ? 'የአድሚን መለያ ስም' : 'Admin Username';
                document.getElementById('lblPassTag').innerText = isAmharic ? 'የይለፍ ቃል' : 'Admin Password';
                document.getElementById('btnSubmitLogin').innerText = isAmharic ? '🔐 እንደ አድሚን ይግቡ' : '🔐 Sign In as Admin';
            }
        }

        // Initialize on load
        (async function init() {
            const token = getActiveToken();
            if (token) {
                try {
                    const res = await fetch('/api/verify?token=' + encodeURIComponent(token));
                    const data = await res.json();
                    if (data.valid) {
                        showApp();
                        return;
                    }
                } catch(e) {}
            }
            showLogin();
        })();

        // Auto-refresh every 15s when page is active
        setInterval(() => {
            if (getActiveToken() && document.getElementById('viewLogin').style.display === 'none') {
                fetchData();
            }
        }, 15000);
    </script>
</body>
</html>";
        }
    }
}
