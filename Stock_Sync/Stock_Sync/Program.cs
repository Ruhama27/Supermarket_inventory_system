using System;
using System.Threading;
using System.Windows.Forms;

namespace Stock_Sync
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Set up global unhandled exception handlers for production reliability
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += GlobalThreadExceptionHandler;
            AppDomain.CurrentDomain.UnhandledException += GlobalDomainExceptionHandler;

            try
            {
                Logger.LogInfo("Stock Sync application starting up.");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new LoginForm());
            }
            catch (Exception ex)
            {
                Logger.LogError("Fatal application startup crash", ex);
                MessageBox.Show($"A fatal error occurred:\n{ex.Message}\n\nDetails have been logged to the application log.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Logger.LogInfo("Stock Sync application shut down.");
            }
        }

        private static void GlobalThreadExceptionHandler(object sender, ThreadExceptionEventArgs e)
        {
            Logger.LogError("Unhandled UI Thread Exception", e.Exception);
            MessageBox.Show($"An unexpected error occurred: {e.Exception.Message}\n\nPlease check logs for more details.", "System Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private static void GlobalDomainExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                Logger.LogError("Unhandled AppDomain Exception", ex);
            }
        }
    }
}
