using System;
using System.IO;
using System.Reflection;

namespace Stock_Sync
{
    public static class Logger
    {
        private static readonly object lockObj = new object();
        private static readonly string baseDir;
        private static readonly string logDirectory;
        private static readonly string logFilePath;

        static Logger()
        {
            try
            {
                baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory;
                logDirectory = Path.Combine(baseDir, "logs");
                logFilePath = Path.Combine(logDirectory, "stock_sync.log");

                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
            }
            catch
            {
                // Ignore initialization failures
            }
        }

        public static void LogInfo(string message)
        {
            WriteEntry("INFO", message);
        }

        public static void LogWarning(string message)
        {
            WriteEntry("WARN", message);
        }

        public static void LogError(string message, Exception ex = null)
        {
            string detail = ex != null ? $"{message} | Exception: {ex.GetType().Name}: {ex.Message}\nStack: {ex.StackTrace}" : message;
            WriteEntry("ERROR", detail);
        }

        private static void WriteEntry(string level, string message)
        {
            lock (lockObj)
            {
                try
                {
                    if (string.IsNullOrEmpty(logFilePath)) return;

                    string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}";
                    File.AppendAllText(logFilePath, entry);
                }
                catch
                {
                    // Fail silently to prevent logging crashes from breaking business logic
                }
            }
        }
    }
}
