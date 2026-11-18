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
    /// Lightweight embedded HTTP micro-server enabling store owners and admins to view
    /// live sales, orders, and inventory alerts on their mobile phone over Wi-Fi.
    /// Strictly restricted to Admin accounts only — staff accounts cannot access.
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
                    else if (rawPath.StartsWith("/api/data"))
                    {
                        if (!ValidateToken(tokenParam, out _))
                        {
                            string err = "{\"error\":\"Unauthorized\",\"message\":\"Admin authentication required. Staff cannot access.\"}";
                            await SendResponseAsync(stream, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(err), 401, "Unauthorized");
                            return;
                        }

                        string json = GetDashboardJson();
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
                        string json = GetDashboardJson();
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

            // Check Authorization header
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

            // Check JSON body
            if (!string.IsNullOrWhiteSpace(body) && body.Contains("\"token\""))
            {
                return ExtractJsonString(body, "token");
            }

            return null;
        }

        private static string ExtractParam(string rawPath, string body, string paramName)
        {
            // 1. Check Query String
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

            // 2. Check JSON Body
            if (!string.IsNullOrWhiteSpace(body))
            {
                string jsonVal = ExtractJsonString(body, paramName);
                if (!string.IsNullOrEmpty(jsonVal)) return jsonVal;

                // Form URL encoded body
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

        private static string GetDashboardJson()
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

            // Recent Orders (Top 8)
            sb.Append("\"recentOrders\":[");
            try
            {
                DataTable dt = Report.FetchOrderHistory();
                if (dt != null && dt.Rows.Count > 0)
                {
                    int count = Math.Min(dt.Rows.Count, 8);
                    for (int i = 0; i < count; i++)
                    {
                        var row = dt.Rows[i];
                        string orderId = row["Order ID"]?.ToString() ?? "";
                        string name = row["Order Name"]?.ToString() ?? "";
                        string valStr = row["Stock Value"]?.ToString() ?? "0";
                        decimal val = decimal.TryParse(valStr, out decimal d) ? d : 0m;
                        string payment = row["Payment"]?.ToString() ?? "Cash";

                        sb.Append("{");
                        sb.Append($"\"id\":\"{EscapeJson(orderId)}\",");
                        sb.Append($"\"name\":\"{EscapeJson(name)}\",");
                        sb.Append($"\"amount\":\"{EscapeJson(LocalizationManager.FormatMoney(val))}\",");
                        sb.Append($"\"payment\":\"{EscapeJson(payment)}\"");
                        sb.Append("}");
                        if (i < count - 1) sb.Append(",");
                    }
                }
            }
            catch { }
            sb.Append("],");

            // Low Stock Items
            sb.Append("\"lowStock\":[");
            try
            {
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string query = @"
                        SELECT TOP 8 p.PName, SUM(s.quantity) AS TotalQty, MAX(s.minimum_stock) AS MinStock
                        FROM Products p 
                        INNER JOIN Stocks s ON p.ID = s.barcode_id 
                        WHERE p.IsActive = 1 AND s.IsActive = 1
                        GROUP BY p.PName
                        HAVING SUM(s.quantity) < MAX(s.minimum_stock)
                        ORDER BY TotalQty ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        bool first = true;
                        while (r.Read())
                        {
                            if (!first) sb.Append(",");
                            first = false;
                            sb.Append("{");
                            sb.Append($"\"name\":\"{EscapeJson(r["PName"].ToString())}\",");
                            sb.Append($"\"qty\":{Convert.ToInt32(r["TotalQty"])},");
                            sb.Append($"\"min\":{Convert.ToInt32(r["MinStock"])}");
                            sb.Append("}");
                        }
                    }
                }
            }
            catch { }
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
    <title>Stock-Sync Admin Live</title>
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
            padding-bottom: 50px;
            min-height: 100vh;
        }

        /* Top Navigation Header */
        .topbar {
            background: #FFFFFF;
            padding: 14px 18px;
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
            width: 38px;
            height: 38px;
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
        .admin-tag {
            background: #FEF3C7;
            color: #D97706;
            font-size: 10.5px;
            font-weight: 800;
            padding: 4px 8px;
            border-radius: 6px;
            letter-spacing: 0.5px;
        }
        .btn-pill {
            background: #F1F5F9;
            color: #334155;
            border: none;
            padding: 6px 12px;
            border-radius: 20px;
            font-size: 12px;
            font-weight: 700;
            cursor: pointer;
        }
        .btn-logout {
            background: #FEE2E2;
            color: #DC2626;
            border: none;
            padding: 6px 12px;
            border-radius: 20px;
            font-size: 12px;
            font-weight: 700;
            cursor: pointer;
        }

        /* Container */
        .container {
            padding: 16px;
            max-width: 600px;
            margin: 0 auto;
        }

        /* Login Screen Card */
        .login-card {
            background: #FFFFFF;
            border-radius: 20px;
            padding: 32px 24px;
            margin-top: 40px;
            box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.05), 0 8px 10px -6px rgba(0, 0, 0, 0.01);
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
            font-size: 12px;
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
            transition: border-color 0.2s;
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
            transition: background 0.2s, transform 0.1s;
            margin-top: 8px;
        }
        .btn-submit:active {
            transform: scale(0.98);
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
        .login-footer {
            margin-top: 24px;
            font-size: 11.5px;
            color: #94A3B8;
            line-height: 1.4;
        }

        /* Dashboard View Elements */
        .time-strip {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 14px;
            padding: 0 4px;
        }
        .time-pill {
            font-size: 12px;
            font-weight: 700;
            color: #64748B;
        }
        .clock-pill {
            font-size: 12px;
            font-weight: 800;
            color: #FF5B26;
            background: #FFF1EB;
            padding: 4px 10px;
            border-radius: 20px;
        }

        .sales-hero {
            background: linear-gradient(135deg, #FF5B26 0%, #FF8A00 100%);
            border-radius: 18px;
            padding: 22px;
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
            font-size: 32px;
            font-weight: 800;
            letter-spacing: -0.5px;
        }
        .sales-hero-sub {
            font-size: 11.5px;
            opacity: 0.85;
            margin-top: 6px;
        }

        .metric-grid {
            display: grid;
            grid-template-columns: repeat(3, 1fr);
            gap: 10px;
            margin-bottom: 16px;
        }
        .metric-card {
            background: #FFFFFF;
            border-radius: 14px;
            padding: 14px 10px;
            text-align: center;
            border: 1px solid #E2E8F0;
            box-shadow: 0 2px 6px rgba(0,0,0,0.02);
        }
        .metric-val {
            font-size: 20px;
            font-weight: 800;
            color: #0F172A;
        }
        .metric-label {
            font-size: 10.5px;
            font-weight: 700;
            color: #64748B;
            margin-top: 4px;
            text-transform: uppercase;
        }
        .alert-val {
            color: #EF4444;
        }

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
        .order-row {
            display: flex;
            align-items: center;
            justify-content: space-between;
            padding: 10px 0;
            border-bottom: 1px solid #F8FAFC;
        }
        .order-row:last-child {
            border-bottom: none;
        }
        .order-id {
            font-size: 11.5px;
            font-weight: 800;
            color: #FF5B26;
        }
        .order-name {
            font-size: 12px;
            color: #475569;
            font-weight: 600;
        }
        .order-price {
            font-size: 13px;
            font-weight: 800;
            color: #0F172A;
            text-align: right;
        }
        .alert-item {
            display: flex;
            align-items: center;
            justify-content: space-between;
            padding: 8px 0;
            border-bottom: 1px solid #F8FAFC;
            font-size: 12px;
        }
        .alert-item:last-child {
            border-bottom: none;
        }
        .alert-name {
            font-weight: 700;
            color: #334155;
        }
        .alert-badge {
            background: #FEE2E2;
            color: #EF4444;
            font-size: 11px;
            font-weight: 800;
            padding: 3px 8px;
            border-radius: 6px;
        }

        .refresh-bar {
            text-align: center;
            margin-top: 10px;
        }
        .refresh-btn {
            background: #FFFFFF;
            border: 1px solid #CBD5E1;
            color: #475569;
            font-size: 12px;
            font-weight: 700;
            padding: 8px 18px;
            border-radius: 20px;
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
            <span class=""admin-tag"">👑 ADMIN</span>
            <button class=""btn-pill"" id=""btnLang"" onclick=""toggleLang()"">🌐 አማርኛ</button>
            <button class=""btn-logout"" id=""btnLogout"" style=""display:none;"" onclick=""doLogout()"">🚪 Exit</button>
        </div>
    </div>

    <div class=""container"">

        <!-- 1. ADMIN LOGIN VIEW -->
        <div id=""viewLogin"" class=""login-card"">
            <div class=""login-badge"">👑</div>
            <div class=""security-badge"">🔒 Admin Access Only</div>
            <h2 class=""login-title"" id=""txtLoginTitle"">Stock-Sync Admin Live</h2>
            <p class=""login-sub"" id=""txtLoginSub"">Sign in with your Admin credentials to monitor supermarket sales and inventory.</p>

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

            <div class=""login-footer"">
                ⚠️ Staff accounts cannot access this dashboard.<br/>
                Ethiopian Birr (ETB) sales and stock telemetry are encrypted.
            </div>
        </div>

        <!-- 2. ADMIN DASHBOARD VIEW (Shown once authenticated) -->
        <div id=""viewDashboard"" style=""display:none;"">
            <div class=""time-strip"">
                <div class=""time-pill"" id=""valDate"">--</div>
                <div class=""clock-pill"" id=""valTime"">--:--</div>
            </div>

            <!-- Revenue Hero Card -->
            <div class=""sales-hero"">
                <div class=""sales-hero-label"" id=""lblSalesTitle"">Today's Revenue</div>
                <div class=""sales-hero-val"" id=""valSales"">0.00 ETB</div>
                <div class=""sales-hero-sub"">Live updates from checkout counters</div>
            </div>

            <!-- 3 Mini KPIs -->
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
            </div>

            <!-- Recent Invoices List -->
            <div class=""section-card"">
                <div class=""section-header"">
                    <div class=""section-title"" id=""lblRecentTitle"">🧾 Recent Checkouts</div>
                </div>
                <div id=""ordersList"">
                    <div style=""text-align:center; color:#94A3B8; font-size:12px; padding:10px;"">Loading recent sales...</div>
                </div>
            </div>

            <!-- Low Stock Warnings -->
            <div class=""section-card"">
                <div class=""section-header"">
                    <div class=""section-title"" id=""lblWarningTitle"">⚠️ Low Stock Alerts</div>
                </div>
                <div id=""stockList"">
                    <div style=""text-align:center; color:#94A3B8; font-size:12px; padding:10px;"">Checking inventory...</div>
                </div>
            </div>

            <div class=""refresh-bar"">
                <button class=""refresh-btn"" onclick=""fetchData()"">🔄 Refresh Now</button>
            </div>
        </div>

    </div>

    <script>
        const TOKEN_KEY = 'stock_sync_admin_token';
        let isAmharic = false;

        function getActiveToken() {
            return localStorage.getItem(TOKEN_KEY) || '';
        }

        function showDashboard() {
            document.getElementById('viewLogin').style.display = 'none';
            document.getElementById('viewDashboard').style.display = 'block';
            document.getElementById('btnLogout').style.display = 'inline-block';
            fetchData();
        }

        function showLogin(errMsg) {
            document.getElementById('viewLogin').style.display = 'block';
            document.getElementById('viewDashboard').style.display = 'none';
            document.getElementById('btnLogout').style.display = 'none';
            const errBox = document.getElementById('loginError');
            if (errMsg) {
                errBox.innerText = errMsg;
                errBox.style.display = 'block';
            } else {
                errBox.style.display = 'none';
            }
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
                    showDashboard();
                } else {
                    showLogin(data.message || 'Login failed. Only Admins can access this dashboard.');
                }
            } catch (ex) {
                showLogin('Server connection error. Ensure the computer is running.');
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
                    showLogin('Session expired or unauthorized. Please log in as Admin.');
                    return;
                }

                const data = await res.json();
                isAmharic = data.isAmharic;

                document.getElementById('valSales').innerText = data.todaySales;
                document.getElementById('valOrders').innerText = data.todayOrders;
                document.getElementById('valProducts').innerText = data.totalProducts;
                document.getElementById('valLowStock').innerText = data.lowStockCount;
                document.getElementById('valTime').innerText = data.time;
                document.getElementById('valDate').innerText = data.date;

                document.getElementById('btnLang').innerText = isAmharic ? '🌐 English' : '🌐 አማርኛ';

                // Localized labels
                document.getElementById('lblSalesTitle').innerText = isAmharic ? 'የዛሬ ሽያጭ' : 'Today\'s Revenue';
                document.getElementById('lblOrdersTitle').innerText = isAmharic ? 'ትዕዛዞች' : 'Invoices';
                document.getElementById('lblProductsTitle').innerText = isAmharic ? 'ምርቶች' : 'Products';
                document.getElementById('lblAlertsTitle').innerText = isAmharic ? 'ያለቀ ክምችት' : 'Low Stock';
                document.getElementById('lblRecentTitle').innerText = isAmharic ? '🧾 የቅርብ ጊዜ ሽያጮች' : '🧾 Recent Checkouts';
                document.getElementById('lblWarningTitle').innerText = isAmharic ? '⚠️ ክምችት ያነሰባቸው ምርቶች' : '⚠️ Low Stock Alerts';

                // Recent Orders
                const oList = document.getElementById('ordersList');
                if (data.recentOrders && data.recentOrders.length > 0) {
                    oList.innerHTML = data.recentOrders.map(o => `
                        <div class=""order-row"">
                            <div>
                                <div class=""order-id"">${o.id}</div>
                                <div class=""order-name"">${o.name} • ${o.payment}</div>
                            </div>
                            <div class=""order-price"">${o.amount}</div>
                        </div>
                    `).join('');
                } else {
                    oList.innerHTML = '<div style=""text-align:center; color:#94A3B8; font-size:12px; padding:8px;"">' + 
                        (isAmharic ? 'ዛሬ ምንም ሽያጭ የለም' : 'No orders today yet') + '</div>';
                }

                // Low Stock
                const sList = document.getElementById('stockList');
                if (data.lowStock && data.lowStock.length > 0) {
                    sList.innerHTML = data.lowStock.map(s => `
                        <div class=""alert-item"">
                            <span class=""alert-name"">${s.name}</span>
                            <span class=""alert-badge"">${s.qty} ` + (isAmharic ? 'ቀሪ' : 'left') + ` (Min: ${s.min})</span>
                        </div>
                    `).join('');
                } else {
                    sList.innerHTML = '<div style=""text-align:center; color:#10B981; font-size:12px; padding:8px; font-weight:700;"">✅ ' + 
                        (isAmharic ? 'ሁሉም ክምችት በጥሩ ደረጃ ላይ ነው!' : 'All stock levels healthy!') + '</div>';
                }
            } catch(e) {
                console.error(e);
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
                document.getElementById('txtLoginTitle').innerText = isAmharic ? 'ስቶክ-ሲንክ የአድሚን መዳረሻ' : 'Stock-Sync Admin Live';
                document.getElementById('txtLoginSub').innerText = isAmharic 
                    ? 'የሱፐርማርኬት ሽያጭ እና ክምችት በስልክዎ ለመከታተል የአድሚን መለያዎን ያስገቡ።' 
                    : 'Sign in with your Admin credentials to monitor supermarket sales and inventory.';
                document.getElementById('lblUserTag').innerText = isAmharic ? 'የአድሚን መለያ ስም' : 'Admin Username';
                document.getElementById('lblPassTag').innerText = isAmharic ? 'የይለፍ ቃል' : 'Admin Password';
                document.getElementById('btnSubmitLogin').innerText = isAmharic ? '🔐 እንደ አድሚን ይግቡ' : '🔐 Sign In as Admin';
            }
        }

        // Check if already authenticated on page load
        (async function init() {
            const token = getActiveToken();
            if (token) {
                try {
                    const res = await fetch('/api/verify?token=' + encodeURIComponent(token));
                    const data = await res.json();
                    if (data.valid) {
                        showDashboard();
                        return;
                    }
                } catch(e) {}
            }
            showLogin();
        })();

        // Auto refresh every 15 seconds when active
        setInterval(() => {
            if (getActiveToken() && document.getElementById('viewDashboard').style.display === 'block') {
                fetchData();
            }
        }, 15000);
    </script>
</body>
</html>";
        }
    }
}
