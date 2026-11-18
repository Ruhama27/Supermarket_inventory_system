using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Stock_Sync
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
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

                // Start mobile telemetry server immediately so Admin phone access is always active,
                // even before anyone logs into the desktop interface or if the PC is left unattended.
                try
                {
                    MobileServer.Start();
                }
                catch (Exception ex)
                {
                    Logger.LogError("Failed to start MobileServer on startup", ex);
                }

                // Check for background/headless service mode switch
                bool isBackground = args != null && args.Length > 0 && 
                    (args[0].Equals("--server", StringComparison.OrdinalIgnoreCase) || 
                     args[0].Equals("--background", StringComparison.OrdinalIgnoreCase) ||
                     args[0].Equals("-b", StringComparison.OrdinalIgnoreCase));

                if (isBackground)
                {
                    Logger.LogInfo("Running in headless background server mode with System Tray icon.");
                    Application.Run(new BackgroundTrayContext());
                }
                else
                {
                    Application.Run(new LoginForm());
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Fatal application startup crash", ex);
                MessageBox.Show($"A fatal error occurred:\n{ex.Message}\n\nDetails have been logged to the application log.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                MobileServer.Stop();
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

    /// <summary>
    /// Lightweight System Tray context allowing the Mobile Server to run silently
    /// in the Windows background without opening any desktop window.
    /// </summary>
    internal class BackgroundTrayContext : ApplicationContext
    {
        private NotifyIcon trayIcon;

        public BackgroundTrayContext()
        {
            ContextMenuStrip menu = new ContextMenuStrip();

            var miHeader = new ToolStripMenuItem("📱 Stock-Sync Mobile Server (Active)") { Enabled = false, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            
            var miCopyUrl = new ToolStripMenuItem("📋 Copy Phone URL", null, (s, e) =>
            {
                try
                {
                    Clipboard.SetText(MobileServer.GetMobileUrl());
                    MessageBox.Show("Phone URL copied to clipboard:\n" + MobileServer.GetMobileUrl(), "Mobile Access", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch { }
            });

            var miBrowser = new ToolStripMenuItem("🌐 Open in Web Browser", null, (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = MobileServer.GetLocalhostUrl(),
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not open browser: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            });

            var miOpenDesktop = new ToolStripMenuItem("🖥️ Open Desktop Application", null, (s, e) =>
            {
                LoginForm login = new LoginForm();
                login.Show();
            });

            var miExit = new ToolStripMenuItem("❌ Exit Server", null, (s, e) =>
            {
                trayIcon.Visible = false;
                MobileServer.Stop();
                Application.Exit();
            });

            menu.Items.Add(miHeader);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(miCopyUrl);
            menu.Items.Add(miBrowser);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(miOpenDesktop);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(miExit);

            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                ContextMenuStrip = menu,
                Text = "Stock-Sync Mobile Server (Port " + MobileServer.Port + ")",
                Visible = true
            };

            trayIcon.DoubleClick += (s, e) =>
            {
                LoginForm login = new LoginForm();
                login.Show();
            };

            trayIcon.ShowBalloonTip(3000, "Stock-Sync Mobile Server",
                $"Running on {MobileServer.GetMobileUrl()}\nAdmin phone access active.", ToolTipIcon.Info);
        }
    }
}
