using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace School_Management_System.Services
{
    internal static class CsvExportService
    {
        public static string? SaveDataTable(DataTable table, string defaultFileName)
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = defaultFileName
            };

            if (dialog.ShowDialog() != DialogResult.OK) return null;

            var sb = new StringBuilder();
            var headers = table.Columns.Cast<DataColumn>().Select(c => Escape(c.ColumnName));
            sb.AppendLine(string.Join(",", headers));

            foreach (DataRow row in table.Rows)
            {
                var values = table.Columns.Cast<DataColumn>().Select(c => Escape(row[c]?.ToString() ?? string.Empty));
                sb.AppendLine(string.Join(",", values));
            }

            File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
            MessageBox.Show("Export completed.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return dialog.FileName;
        }

        private static string Escape(string value)
        {
            var escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }
    }
}
