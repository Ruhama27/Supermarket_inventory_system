using System;
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
    /// </summary>
    public static class MobileServer
    {
        private static TcpListener listener;
        private static CancellationTokenSource cts;
        private static int activePort = 5050;
        private static bool isRunning = false;

        public static bool IsRunning => isRunning;
        public static int Port => activePort;

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
                // Fallback to DNS enumeration
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

            // Try preferred port, or increment if busy
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
                    byte[] buffer = new byte[4096];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (bytesRead <= 0) return;

                    string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    string firstLine = request.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)[0];
                    string[] parts = firstLine.Split(' ');

                    string method = parts.Length > 0 ? parts[0] : "GET";
                    string path = parts.Length > 1 ? parts[1] : "/";

                    if (path.StartsWith("/api/data"))
                    {
                        string json = GetDashboardJson();
                        await SendResponseAsync(stream, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(json));
                    }
                    else if (path.StartsWith("/api/toggle-lang"))
                    {
                        LocalizationManager.ToggleLanguage();
                        string json = GetDashboardJson();
                        await SendResponseAsync(stream, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(json));
                    }
                    else
                    {
                        // Serve Mobile Single-Page Dashboard
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

        private static async Task SendResponseAsync(NetworkStream stream, string contentType, byte[] body)
        {
            string headers = "HTTP/1.1 200 OK\r\n" +
                             $"Content-Type: {contentType}\r\n" +
                             $"Content-Length: {body.Length}\r\n" +
                             "Access-Control-Allow-Origin: *\r\n" +
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
            background: #F6F8FA;
            color: #1E293B;
            padding-bottom: 50px;
        }
        /* Top Navigation Header */
        .topbar {
            background: #FFFFFF;
            padding: 16px 20px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            border-bottom: 1px solid #EBEFF3;
            position: sticky;
            top: 0;
            z-index: 100;
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
            font-size: 16px;
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
            box-shadow: 0 0 0 3px rgba(16, 185, 129, 0.2);
            animation: pulse 2s infinite;
        }
        @keyframes pulse {
            0% { transform: scale(0.95); box-shadow: 0 0 0 0 rgba(16, 185, 129, 0.7); }
            70% { transform: scale(1); box-shadow: 0 0 0 6px rgba(16, 185, 129, 0); }
            100% { transform: scale(0.95); box-shadow: 0 0 0 0 rgba(16, 185, 129, 0); }
        }
        .lang-btn {
            background: #FFF0EB;
            color: #FF5B26;
            border: none;
            padding: 8px 12px;
            border-radius: 20px;
            font-weight: 700;
            font-size: 12px;
            cursor: pointer;
        }
        /* Container */
        .container {
            padding: 18px 16px;
            max-width: 500px;
            margin: 0 auto;
        }
        /* Highlight KPI Card (Coral) */
        .hero-card {
            background: linear-gradient(135deg, #FF5B26 0%, #FA602D 100%);
            border-radius: 20px;
            padding: 22px 20px;
            color: white;
            box-shadow: 0 10px 24px rgba(255, 91, 38, 0.25);
            margin-bottom: 14px;
            position: relative;
            overflow: hidden;
        }
        .hero-card::after {
            content: '💰';
            position: absolute;
            right: 14px;
            top: 14px;
            font-size: 40px;
            opacity: 0.22;
        }
        .hero-title {
            font-size: 12px;
            text-transform: uppercase;
            letter-spacing: 0.8px;
            font-weight: 700;
            opacity: 0.9;
            margin-bottom: 6px;
        }
        .hero-val {
            font-size: 28px;
            font-weight: 800;
            letter-spacing: -0.5px;
            margin-bottom: 8px;
        }
        .hero-time {
            font-size: 11px;
            opacity: 0.8;
        }
        /* Metric Grid */
        .grid {
            display: grid;
            grid-template-columns: repeat(3, 1fr);
            gap: 10px;
            margin-bottom: 20px;
        }
        .metric-card {
            background: #FFFFFF;
            border-radius: 16px;
            padding: 14px 12px;
            border: 1px solid #EBEFF3;
            text-align: center;
            box-shadow: 0 2px 6px rgba(0,0,0,0.02);
        }
        .metric-val {
            font-size: 20px;
            font-weight: 800;
            color: #0F172A;
            margin-bottom: 4px;
        }
        .metric-label {
            font-size: 11px;
            font-weight: 600;
            color: #64748B;
        }
        .alert-val { color: #EF4444; }
        /* Section Cards */
        .section-card {
            background: #FFFFFF;
            border-radius: 18px;
            border: 1px solid #EBEFF3;
            padding: 18px;
            margin-bottom: 16px;
            box-shadow: 0 2px 8px rgba(0,0,0,0.02);
        }
        .section-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 14px;
        }
        .section-title {
            font-size: 14px;
            font-weight: 700;
            color: #0F172A;
            display: flex;
            align-items: center;
            gap: 6px;
        }
        /* List Items */
        .order-row {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 10px 0;
            border-bottom: 1px solid #F1F5F9;
        }
        .order-row:last-child { border-bottom: none; }
        .order-id { font-size: 12px; font-weight: 700; color: #FF5B26; }
        .order-name { font-size: 11px; color: #64748B; max-width: 170px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
        .order-price { font-size: 13px; font-weight: 700; color: #0F172A; text-align: right; }
        .alert-item {
            display: flex;
            align-items: center;
            justify-content: space-between;
            padding: 10px 12px;
            background: #FFF5F2;
            border-radius: 12px;
            margin-bottom: 8px;
        }
        .alert-item:last-child { margin-bottom: 0; }
        .alert-name { font-size: 12px; font-weight: 700; color: #C2410C; }
        .alert-badge {
            background: #FFE4DC;
            color: #C2410C;
            padding: 3px 8px;
            border-radius: 8px;
            font-size: 11px;
            font-weight: 800;
        }
        /* Bottom Refresh Bar */
        .refresh-bar {
            text-align: center;
            margin-top: 14px;
        }
        .refresh-btn {
            background: #FFFFFF;
            border: 1px solid #E2E8F0;
            padding: 10px 20px;
            border-radius: 24px;
            font-size: 12px;
            font-weight: 700;
            color: #475569;
            cursor: pointer;
            box-shadow: 0 2px 6px rgba(0,0,0,0.03);
        }
    </style>
</head>
<body>
    <div class=""topbar"">
        <div class=""brand"">
            <div class=""brand-badge"">📦</div>
            <div class=""brand-info"">
                <h1>Stock-Sync</h1>
                <span><i class=""pulse""></i> <span id=""lblStatus"">Store Live</span></span>
            </div>
        </div>
        <button class=""lang-btn"" id=""btnLang"" onclick=""toggleLang()"">🌐 አማርኛ</button>
    </div>

    <div class=""container"">
        <!-- Hero Today's Sales Card -->
        <div class=""hero-card"">
            <div class=""hero-title"" id=""lblSalesTitle"">Today's Sales / የዛሬ ሽያጭ</div>
            <div class=""hero-val"" id=""valSales"">ETB 0.00</div>
            <div class=""hero-time"">🕒 <span id=""valTime"">--:--</span> • <span id=""valDate"">Today</span></div>
        </div>

        <!-- 3 Top Metric Cards -->
        <div class=""grid"">
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

    <script>
        async function fetchData() {
            try {
                const res = await fetch('/api/data?t=' + Date.now());
                const data = await res.json();

                document.getElementById('valSales').innerText = data.todaySales;
                document.getElementById('valOrders').innerText = data.todayOrders;
                document.getElementById('valProducts').innerText = data.totalProducts;
                document.getElementById('valLowStock').innerText = data.lowStockCount;
                document.getElementById('valTime').innerText = data.time;
                document.getElementById('valDate').innerText = data.date;

                document.getElementById('btnLang').innerText = data.isAmharic ? '🌐 English' : '🌐 አማርኛ';

                // Recent Orders
                const oList = document.getElementById('ordersList');
                if (data.recentOrders && data.recentOrders.length > 0) {
                    oList.innerHTML = data.recentOrders.map(o => `
                        <div class=""order-row"">
                            <div>
                                <div class=""order-id"">${o.id}</div>
                                <div class=""order-name"">${o.name}</div>
                            </div>
                            <div class=""order-price"">${o.amount}</div>
                        </div>
                    `).join('');
                } else {
                    oList.innerHTML = '<div style=""text-align:center; color:#94A3B8; font-size:12px; padding:8px;"">No orders today yet</div>';
                }

                // Low Stock
                const sList = document.getElementById('stockList');
                if (data.lowStock && data.lowStock.length > 0) {
                    sList.innerHTML = data.lowStock.map(s => `
                        <div class=""alert-item"">
                            <span class=""alert-name"">${s.name}</span>
                            <span class=""alert-badge"">${s.qty} left (Min: ${s.min})</span>
                        </div>
                    `).join('');
                } else {
                    sList.innerHTML = '<div style=""text-align:center; color:#10B981; font-size:12px; padding:8px; font-weight:700;"">✅ All stock levels healthy!</div>';
                }
            } catch(e) {
                console.error(e);
            }
        }

        async function toggleLang() {
            await fetch('/api/toggle-lang');
            fetchData();
        }

        // Fetch on load and auto-refresh every 15s
        fetchData();
        setInterval(fetchData, 15000);
    </script>
</body>
</html>";
        }
    }
}
