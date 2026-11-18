using System;
using System.Collections.Generic;

namespace Stock_Sync
{
    public enum AppLanguage
    {
        English,
        Amharic
    }

    public static class LocalizationManager
    {
        public static AppLanguage CurrentLanguage { get; private set; } = AppLanguage.English;

        public static event Action OnLanguageChanged;

        public static void SetLanguage(AppLanguage lang)
        {
            if (CurrentLanguage != lang)
            {
                CurrentLanguage = lang;
                OnLanguageChanged?.Invoke();
            }
        }

        public static void ToggleLanguage()
        {
            SetLanguage(CurrentLanguage == AppLanguage.English ? AppLanguage.Amharic : AppLanguage.English);
        }

        public static string T(string key)
        {
            if (CurrentLanguage == AppLanguage.Amharic && amharicTranslations.TryGetValue(key, out string amValue))
            {
                return amValue;
            }

            if (englishTranslations.TryGetValue(key, out string enValue))
            {
                return enValue;
            }

            return key;
        }

        /// <summary>
        /// Formats numeric currency values in Ethiopian Birr (ETB / ብር) according to active language.
        /// </summary>
        public static string FormatMoney(decimal amount)
        {
            if (CurrentLanguage == AppLanguage.Amharic)
            {
                return $"{amount:N2} ብር";
            }
            return $"{amount:N2} ETB";
        }

        public static string FormatMoney(double amount)
        {
            return FormatMoney((decimal)amount);
        }

        // English Translations
        private static readonly Dictionary<string, string> englishTranslations = new Dictionary<string, string>
        {
            // Navigation
            { "NavOrders", "📦  Orders Screen" },
            { "NavPos", "🛒  Point of Sale" },
            { "NavProducts", "📑  Products" },
            { "NavStock", "📥  Stock Batches" },
            { "NavDashboard", "📊  Dashboard Analytics" },
            { "NavReports", "📈  Sales Reports" },
            { "NavAlerts", "🔔  Alerts & Notices" },
            { "NavLogout", "⏻   Logout" },

            // Roles & User
            { "AdminRole", "System Admin" },
            { "StaffRole", "Staff Cashier" },

            // Header
            { "TitleOrders", "Orders Screen" },
            { "BreadcrumbOrders", "Order Management  >  Order History" },
            { "TitlePos", "Point of Sale" },
            { "BreadcrumbPos", "Sales Checkout  >  Live POS" },
            { "TitleProducts", "Products Catalog" },
            { "BreadcrumbProducts", "Inventory  >  Product Management" },
            { "TitleStock", "Stock Management" },
            { "BreadcrumbStock", "Inventory  >  Stock Batches" },
            { "TitleDashboard", "Analytics Dashboard" },
            { "BreadcrumbDashboard", "Executive Overview  >  Sales Analytics" },
            { "TitleReports", "Sales Reports" },
            { "BreadcrumbReports", "Accounting  >  Daily Invoices" },
            { "TitleAlerts", "System Alerts" },
            { "BreadcrumbAlerts", "Notifications  >  Low Stock & Expirations" },

            // Orders Screen
            { "OrderHistory", "Order History" },
            { "AllOrders", "All Orders" },
            { "Completed", "Completed" },
            { "Pending", "Pending / Other" },
            { "ExportCsv", "Export to CSV" },
            { "ColSNo", "S No" },
            { "ColOrderId", "Order ID" },
            { "ColOrderName", "Order Name" },
            { "ColStockValue", "Stock Value (Birr)" },
            { "ColQuantity", "Quantity" },
            { "ColPayment", "Payment" },
            { "ColStatus", "Status" },

            // POS
            { "ScanPrompt", "Scan Barcode or Enter Product Code:" },
            { "ClearCart", "Clear Cart" },
            { "PaymentSummary", "Payment Summary" },
            { "Subtotal", "Subtotal:" },
            { "Tax", "Tax / VAT (0%):" },
            { "TotalPayable", "Total Payable:" },
            { "CashReceived", "Cash Received (Birr):" },
            { "ChangeDue", "Change Due:" },
            { "CompleteSale", "✔   Complete Sale" },
            { "PrintReceipt", "🖨   Print Receipt" },
            { "CurrencyUnit", "Birr" },
            { "CurrencyCode", "ETB" },

            // Products
            { "ManageProduct", "Manage Product" },
            { "Barcode", "Barcode:" },
            { "ProductName", "Product Name:" },
            { "Category", "Category:" },
            { "Status", "Status:" },
            { "AddNewProduct", "Add New Product" },
            { "UpdateSelected", "Update Selected" },
            { "DeactivateProduct", "Deactivate Product" },
            { "Search", "🔍 Search" },

            // Stock
            { "ManageStockBatch", "Manage Stock Batch" },
            { "ProductId", "Product ID:" },
            { "BatchCode", "Batch Code:" },
            { "MinThreshold", "Min Threshold:" },
            { "CostPrice", "Cost Price (Birr):" },
            { "SellingPrice", "Selling Price (Birr):" },
            { "ArrivalDate", "Arrival Date:" },
            { "HasExpiryDate", "Has Expiry Date:" },
            { "SaveStockBatch", "Save Stock Batch" },

            // Dashboard KPIs
            { "KpiTodaySales", "Today's Sales" },
            { "KpiTodayOrders", "Today's Invoices" },
            { "KpiActiveProducts", "Active Products" },
            { "KpiLowStock", "Low Stock Alerts" },

            // Reports
            { "SelectDate", "Select Date:" },
            { "GenerateReport", "Generate Report" },

            // Alerts
            { "LowStockAlerts", "⚠ Low Stock Warnings" },
            { "ExpiredBatches", "⌛ Expired Batches" },
            { "NeverSoldItems", "📦 Never Sold Items" },

            // Grid Columns
            { "ColId", "ID" },
            { "ColName", "Product Name" },
            { "ColCategory", "Category" },
            { "ColBatchCode", "Batch Code" },
            { "ColCostPrice", "Cost Price (Birr)" },
            { "ColSellingPrice", "Selling Price (Birr)" },
            { "ColArrivalDate", "Arrival Date" },
            { "ColExpiryDate", "Expiry Date" },
            { "ColPrice", "Price (Birr)" },
            { "ColTotal", "Total (Birr)" },
            { "ColCashReceived", "Cash Received (Birr)" },
            { "ColChangeReturned", "Change Returned (Birr)" },
            { "ColCashier", "Cashier" },
            { "ColDate", "Date" },
            { "ColMinStock", "Min Stock" },

            // Messages & Prompts
            { "MsgEmptyCart", "Please scan at least one product before checking out." },
            { "MsgEnterCash", "Please enter the cash received from the customer." },
            { "MsgInsufficientCash", "Cash received is less than total due." },
            { "MsgSaleCompleted", "Sale #{0} completed successfully!\nChange Due: {1}\n\nPrint receipt now?" },
            { "MsgCheckoutSuccessTitle", "Checkout Completed" },
            { "MsgProdAdded", "Product added successfully!" },
            { "MsgProdUpdated", "Product updated successfully!" },
            { "MsgProdDeactivated", "Product deactivated successfully." },
            { "MsgStockAdded", "Stock batch added successfully!" },
            { "MsgSuccessTitle", "Success" },
            { "MsgWarningTitle", "Warning" },
            { "MsgInputErrorTitle", "Input Error" },
            { "MsgPaymentErrorTitle", "Payment Error" },

            // Login
            { "SignInTitle", "Sign In Start Your Session" },
            { "SignInAdmin", "Sign In As Admin" },
            { "SignInStaff", "Sign In As Staff" },
            { "Email", "Email" },
            { "Username", "Username" },
            { "EnterUsernamePassword", "Please enter both username and password." },
            { "Password", "Password" },
            { "Or", "- OR -" }
        };

        // Amharic (አማርኛ) Translations
        private static readonly Dictionary<string, string> amharicTranslations = new Dictionary<string, string>
        {
            // Navigation
            { "NavOrders", "📦  የትዕዛዞች ገጽ" },
            { "NavPos", "🛒  የሽያጭ ቦታ (POS)" },
            { "NavProducts", "📑  ዕቃዎች / ምርቶች" },
            { "NavStock", "📥  የመጋዘን ክምችት" },
            { "NavDashboard", "📊  ዳሽቦርድ እና ትንታኔ" },
            { "NavReports", "📈  የሽያጭ ሪፖርቶች" },
            { "NavAlerts", "🔔  ማስጠንቀቂያዎች" },
            { "NavLogout", "⏻   ውጣ" },

            // Roles & User
            { "AdminRole", "ሲስተም አስተዳዳሪ" },
            { "StaffRole", "ገንዘብ ተቀባይ / ሠራተኛ" },

            // Header
            { "TitleOrders", "የትዕዛዞች ገጽ" },
            { "BreadcrumbOrders", "የትዕዛዝ አስተዳደር  >  የትዕዛዝ ታሪክ" },
            { "TitlePos", "የቀጥታ ሽያጭ ማከናወኛ (POS)" },
            { "BreadcrumbPos", "ሽያጭ  >  የቀጥታ ሽያጭ" },
            { "TitleProducts", "የዕቃዎች ዝርዝር ማውጫ" },
            { "BreadcrumbProducts", "ክምችት  >  የዕቃዎች አስተዳደር" },
            { "TitleStock", "የመጋዘን ክምችት አስተዳደር" },
            { "BreadcrumbStock", "ክምችት  >  የክምችት ባቾች" },
            { "TitleDashboard", "የትንታኔ ዳሽቦርድ" },
            { "BreadcrumbDashboard", "አጠቃላይ እይታ  >  የሽያጭ ትንታኔ" },
            { "TitleReports", "የሽያጭ ሪፖርቶች" },
            { "BreadcrumbReports", "ሒሳብ  >  የቀን ደረሰኞች" },
            { "TitleAlerts", "የሲስተም ማስጠንቀቂያዎች" },
            { "BreadcrumbAlerts", "ማሳወቂያዎች  >  የሚያልቁ እና ጊዜያቸው ያለፈባቸው" },

            // Orders Screen
            { "OrderHistory", "የትዕዛዝ ታሪክ" },
            { "AllOrders", "ሁሉም ትዕዛዞች" },
            { "Completed", "የተጠናቀቁ" },
            { "Pending", "በመጠባበቅ ላይ" },
            { "ExportCsv", "ወደ CSV ላክ" },
            { "ColSNo", "ተ.ቁ" },
            { "ColOrderId", "የትዕዛዝ ቁጥር" },
            { "ColOrderName", "የዕቃው ስም" },
            { "ColStockValue", "ጠቅላላ ዋጋ (ብር)" },
            { "ColQuantity", "ብዛት" },
            { "ColPayment", "የክፍያ ዘዴ" },
            { "ColStatus", "ሁኔታ" },

            // POS
            { "ScanPrompt", "ባርኮድ ይቃኙ ወይም የዕቃውን ቁጥር ያስገቡ:" },
            { "ClearCart", "ቅርጫቱን አጽዳ" },
            { "PaymentSummary", "የክፍያ ማጠቃለያ" },
            { "Subtotal", "ድምር:" },
            { "Tax", "ታክስ / ቫት (0%):" },
            { "TotalPayable", "ጠቅላላ ክፍያ:" },
            { "CashReceived", "የተቀበሉት ጥሬ ገንዘብ (ብር):" },
            { "ChangeDue", "መልስ:" },
            { "CompleteSale", "✔   ሽያጩን አጠናቅቅ" },
            { "PrintReceipt", "🖨   ደረሰኝ አትም" },
            { "CurrencyUnit", "ብር" },
            { "CurrencyCode", "ብር" },

            // Products
            { "ManageProduct", "ዕቃዎችን ማስተዳደር" },
            { "Barcode", "ባርኮድ:" },
            { "ProductName", "የዕቃው ስም:" },
            { "Category", "ምድብ:" },
            { "Status", "ሁኔታ:" },
            { "AddNewProduct", "አዲስ ዕቃ መዝግብ" },
            { "UpdateSelected", "የተመረጠውን አሻሽል" },
            { "DeactivateProduct", "ዕቃውን አግድ / ሰርዝ" },
            { "Search", "🔍 ፈልግ" },

            // Stock
            { "ManageStockBatch", "የክምችት ባች ማስተዳደሪያ" },
            { "ProductId", "የዕቃው መለያ (ID):" },
            { "BatchCode", "የባች ኮድ:" },
            { "MinThreshold", "ዝቅተኛ ገደብ:" },
            { "CostPrice", "የግዢ ዋጋ (ብር):" },
            { "SellingPrice", "የመሸጫ ዋጋ (ብር):" },
            { "ArrivalDate", "የደረሰበት ቀን:" },
            { "HasExpiryDate", "የሚያበቃበት ቀን አለው:" },
            { "SaveStockBatch", "ክምችቱን መዝግብ" },

            // Dashboard KPIs
            { "KpiTodaySales", "የዛሬ ሽያጭ" },
            { "KpiTodayOrders", "የዛሬ ደረሰኞች" },
            { "KpiActiveProducts", "ንቁ ዕቃዎች" },
            { "KpiLowStock", "የሚያልቁ ዕቃዎች ማስጠንቀቂያ" },

            // Reports
            { "SelectDate", "ቀን ይምረጡ:" },
            { "GenerateReport", "ሪፖርት አውጣ" },

            // Alerts
            { "LowStockAlerts", "⚠ ዝቅተኛ ክምችት ማስጠንቀቂያ" },
            { "ExpiredBatches", "⌛ ጊዜያቸው ያለፈባቸው ባቾች" },
            { "NeverSoldItems", "📦 ፈጽሞ ያልተሸጡ ዕቃዎች" },

            // Grid Columns
            { "ColId", "መለያ ቁጥር" },
            { "ColName", "የዕቃው ስም" },
            { "ColCategory", "ምድብ" },
            { "ColBatchCode", "የባች ኮድ" },
            { "ColCostPrice", "የግዢ ዋጋ (ብር)" },
            { "ColSellingPrice", "የመሸጫ ዋጋ (ብር)" },
            { "ColArrivalDate", "የደረሰበት ቀን" },
            { "ColExpiryDate", "የሚያበቃበት ቀን" },
            { "ColPrice", "ዋጋ (ብር)" },
            { "ColTotal", "ድምር (ብር)" },
            { "ColCashReceived", "የተቀበሉት ጥሬ ገንዘብ (ብር)" },
            { "ColChangeReturned", "የተመለሰ መልስ (ብር)" },
            { "ColCashier", "ገንዘብ ተቀባይ" },
            { "ColDate", "ቀን" },
            { "ColMinStock", "ዝቅተኛ ክምችት" },

            // Messages & Prompts
            { "MsgEmptyCart", "እባክዎ ከመክፈልዎ በፊት ቢያንስ አንድ ዕቃ ይቃኙ።" },
            { "MsgEnterCash", "እባክዎ ከደንበኛው የተቀበሉትን ጥሬ ገንዘብ ያስገቡ።" },
            { "MsgInsufficientCash", "የተቀበሉት ገንዘብ ከሚፈለገው ጠቅላላ ክፍያ ያነሰ ነው።" },
            { "MsgSaleCompleted", "ሽያጭ #{0} በተሳካ ሁኔታ ተጠናቋል!\nመልስ: {1}\n\nደረሰኝ አሁን ይታተም?" },
            { "MsgCheckoutSuccessTitle", "ሽያጩ ተጠናቋል" },
            { "MsgProdAdded", "ዕቃው በተሳካ ሁኔታ ተመዝግቧል!" },
            { "MsgProdUpdated", "ዕቃው በተሳካ ሁኔታ ተሻሽሏል!" },
            { "MsgProdDeactivated", "ዕቃው በተሳካ ሁኔታ ተሰርዟል/ታግዷል!" },
            { "MsgStockAdded", "የክምችት ባች በተሳካ ሁኔታ ተመዝግቧል!" },
            { "MsgSuccessTitle", "ስኬታማ" },
            { "MsgWarningTitle", "ማስጠንቀቂያ" },
            { "MsgInputErrorTitle", "የግቤት ስህተት" },
            { "MsgPaymentErrorTitle", "የክፍያ ስህተት" },

            // Login
            { "SignInTitle", "መለያዎን ያስገቡ" },
            { "SignInAdmin", "እንደ አስተዳዳሪ ግባ" },
            { "SignInStaff", "እንደ ሠራተኛ ግባ" },
            { "Email", "ኢሜይል" },
            { "Username", "የተጠቃሚ ስም" },
            { "EnterUsernamePassword", "እባክዎ የተጠቃሚ ስም እና የይለፍ ቃል ያስገቡ።" },
            { "Password", "የይለፍ ቃል" },
            { "Or", "- ወይም -" }
        };
    }
}
