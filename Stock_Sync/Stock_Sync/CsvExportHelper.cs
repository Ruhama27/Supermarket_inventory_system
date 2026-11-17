using System;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Stock_Sync
{
    public static class CsvExportHelper
    {
        public static bool ExportToCsv(DataTable table, string defaultFilename)
        {
            if (table == null || table.Rows.Count == 0)
            {
                MessageBox.Show("No data available to export.", "Export Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
                sfd.FileName = defaultFilename;
                sfd.Title = "Save Exported CSV";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        StringBuilder sb = new StringBuilder();

                        // Write Header
                        for (int i = 0; i < table.Columns.Count; i++)
                        {
                            sb.Append(EscapeCsv(table.Columns[i].ColumnName));
                            if (i < table.Columns.Count - 1) sb.Append(",");
                        }
                        sb.AppendLine();

                        // Write Rows
                        foreach (DataRow row in table.Rows)
                        {
                            for (int i = 0; i < table.Columns.Count; i++)
                            {
                                sb.Append(EscapeCsv(row[i]?.ToString() ?? ""));
                                if (i < table.Columns.Count - 1) sb.Append(",");
                            }
                            sb.AppendLine();
                        }

                        File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                        MessageBox.Show($"Data exported successfully to:\n{sfd.FileName}", "Export Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Failed to export data to CSV", ex);
                        MessageBox.Show($"Failed to export data: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }
            }

            return false;
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }
    }
}
