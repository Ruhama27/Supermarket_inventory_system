using System;
using System.Windows.Forms;

namespace Stock_Sync
{
    internal static class Buttons
    {
        private static void NavigateTo(Form currentForm, Form nextForm)
        {
            try
            {
                nextForm.StartPosition = FormStartPosition.Manual;
                nextForm.Location = currentForm.Location;
                nextForm.Size = currentForm.Size;
                nextForm.WindowState = currentForm.WindowState;

                nextForm.FormClosed += (s, args) =>
                {
                    // If no forms remain open, terminate process cleanly
                    if (Application.OpenForms.Count == 0)
                    {
                        Application.Exit();
                    }
                };

                nextForm.Show();
                currentForm.Dispose(); // Releases memory and GDI handles
            }
            catch (Exception ex)
            {
                Logger.LogError("Error during form navigation", ex);
            }
        }

        public static void OpenStockForm(Form currentForm)
        {
            NavigateTo(currentForm, new StockForm());
        }

        public static void OpenProductForm(Form currentForm)
        {
            NavigateTo(currentForm, new ProductsForm());
        }

        public static void OpenBillForm(Form currentForm)
        {
            NavigateTo(currentForm, new BillForm());
        }

        public static void OpenChartForm(Form currentForm)
        {
            NavigateTo(currentForm, new ChartsFormcs());
        }

        public static void OpenReportForm(Form currentForm)
        {
            NavigateTo(currentForm, new ReportForm());
        }

        public static void OpenNotificationForm(Form currentForm)
        {
            NavigateTo(currentForm, new NotificationForm());
        }

        public static void OpenLoginForm(Form currentForm)
        {
            Session.Clear();
            LoginForm login = new LoginForm();
            login.StartPosition = FormStartPosition.CenterScreen;
            login.FormClosed += (s, args) => Application.Exit();
            login.Show();
            currentForm.Dispose();
        }
    }
}
