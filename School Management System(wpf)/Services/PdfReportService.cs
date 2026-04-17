using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace School_Management_System.Services
{
    internal static class PdfReportService
    {
        public static void SaveDataTableAsPdf(DataTable table, string title, string filePath, IEnumerable<string>? headerLines = null)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            var lines = BuildLines(table, title, headerLines);
            var pages = Paginate(lines, maxLinesPerPage: 46).ToList();
            if (pages.Count == 0)
            {
                pages.Add(new List<string> { title, string.Empty, "No data available." });
            }

            var objects = new List<PdfObject>();
            var pageObjectIds = new List<int>();
            var contentObjectIds = new List<int>();

            var catalogId = 1;
            var pagesId = 2;
            var fontId = 3;
            var nextId = 4;

            foreach (var page in pages)
            {
                var content = BuildContentStream(page);
                var contentId = nextId++;
                var pageId = nextId++;

                contentObjectIds.Add(contentId);
                pageObjectIds.Add(pageId);

                objects.Add(new PdfObject(contentId, $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream"));
                objects.Add(new PdfObject(
                    pageId,
                    $"<< /Type /Page /Parent {pagesId} 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 {fontId} 0 R >> >> /Contents {contentId} 0 R >>"));
            }

            var kids = string.Join(" ", pageObjectIds.Select(id => $"{id} 0 R"));
            objects.Add(new PdfObject(fontId, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"));
            objects.Add(new PdfObject(pagesId, $"<< /Type /Pages /Count {pageObjectIds.Count} /Kids [{kids}] >>"));
            objects.Add(new PdfObject(catalogId, $"<< /Type /Catalog /Pages {pagesId} 0 R >>"));

            var ordered = objects.OrderBy(x => x.Id).ToList();
            WritePdf(filePath, ordered, catalogId);
        }

        private static List<string> BuildLines(DataTable table, string title, IEnumerable<string>? headerLines)
        {
            var lines = new List<string>();
            if (headerLines != null)
            {
                lines.AddRange(headerLines.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
            }

            lines.Add(title);
            lines.Add($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            lines.Add(string.Empty);

            var headers = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            lines.Add(JoinColumns(headers));
            lines.Add(new string('-', 150));

            foreach (DataRow row in table.Rows)
            {
                var values = table.Columns.Cast<DataColumn>()
                    .Select(c => (row[c]?.ToString() ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim())
                    .ToList();
                lines.Add(JoinColumns(values));
            }

            return lines;
        }

        private static string JoinColumns(List<string> values)
        {
            var sanitized = values.Select(value =>
            {
                var compact = value;
                if (compact.Length > 28)
                {
                    compact = compact.Substring(0, 28) + "...";
                }

                return compact.PadRight(31);
            });

            return string.Join(" ", sanitized).TrimEnd();
        }

        private static IEnumerable<List<string>> Paginate(List<string> lines, int maxLinesPerPage)
        {
            for (var i = 0; i < lines.Count; i += maxLinesPerPage)
            {
                yield return lines.Skip(i).Take(maxLinesPerPage).ToList();
            }
        }

        private static string BuildContentStream(List<string> lines)
        {
            var sb = new StringBuilder();
            sb.AppendLine("BT");
            sb.AppendLine("/F1 10 Tf");
            sb.AppendLine("50 800 Td");

            for (var i = 0; i < lines.Count; i++)
            {
                var escaped = EscapePdfText(lines[i]);
                if (i == 0)
                {
                    sb.AppendLine($"({escaped}) Tj");
                }
                else
                {
                    sb.AppendLine("0 -16 Td");
                    sb.AppendLine($"({escaped}) Tj");
                }
            }

            sb.AppendLine("ET");
            return sb.ToString().TrimEnd();
        }

        private static string EscapePdfText(string input)
        {
            return (input ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("(", "\\(")
                .Replace(")", "\\)");
        }

        private static void WritePdf(string filePath, List<PdfObject> objects, int catalogId)
        {
            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream, Encoding.ASCII);

            writer.WriteLine("%PDF-1.4");
            writer.Flush();

            var offsets = new Dictionary<int, long> { [0] = 0 };

            foreach (var obj in objects)
            {
                writer.Flush();
                offsets[obj.Id] = stream.Position;
                writer.WriteLine($"{obj.Id} 0 obj");
                writer.WriteLine(obj.Body);
                writer.WriteLine("endobj");
            }

            writer.Flush();
            var xrefPosition = stream.Position;
            var maxId = objects.Max(x => x.Id);

            writer.WriteLine("xref");
            writer.WriteLine($"0 {maxId + 1}");
            writer.WriteLine("0000000000 65535 f ");

            for (var i = 1; i <= maxId; i++)
            {
                offsets.TryGetValue(i, out var offset);
                writer.WriteLine($"{offset:0000000000} 00000 n ");
            }

            writer.WriteLine("trailer");
            writer.WriteLine($"<< /Size {maxId + 1} /Root {catalogId} 0 R >>");
            writer.WriteLine("startxref");
            writer.WriteLine(xrefPosition);
            writer.Write("%%EOF");
        }

        private sealed class PdfObject
        {
            public PdfObject(int id, string body)
            {
                Id = id;
                Body = body;
            }

            public int Id { get; }
            public string Body { get; }
        }
    }
}
