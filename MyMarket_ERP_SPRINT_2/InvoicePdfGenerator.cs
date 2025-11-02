using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace MyMarket_ERP
{
    public sealed class InvoicePdfGenerator
    {
        private const float PageWidth = 595f;
        private const float PageHeight = 842f;
        private const float MarginLeft = 40f;
        private const float MarginRight = 40f;
        private const float MarginTop = 40f;
        private const float MarginBottom = 40f;
        private static readonly float[] ColumnWidths = { 28f, 70f, 220f, 60f, 70f, 75f };
        private static readonly string[] ColumnHeaders = { "#", "Código", "Descripción", "Cantidad", "V. Unitario", "Total" };

        public byte[] Generate(InvoiceDocumentData data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            var model = InvoiceDocumentModelBuilder.Build(data);
            string content = BuildContent(model);
            return SimplePdfBuilder.Build(content);
        }

        private static string BuildContent(InvoiceDocumentViewModel model)
        {
            var sb = new StringBuilder();
            sb.AppendLine("0 0 0 rg");
            sb.AppendLine("0 0 0 RG");

            float y = PageHeight - MarginTop - 20f;

            AddText(sb, MarginLeft, y, 24, "MyMarket");
            y -= 28f;

            AddText(sb, MarginLeft, y, 14, $"Factura electrónica de venta No. {model.Data.Number}");
            y -= 20f;

            string issued = model.Data.IssuedAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);
            AddText(sb, MarginLeft, y, 10, $"Fecha de emisión: {issued}");
            y -= 14f;

            string paymentMethod = FormatOrDefault(model.Data.PaymentMethod, "Sin método");
            string paymentStatus = FormatOrDefault(model.Data.PaymentStatus, "Sin estado");
            AddText(sb, MarginLeft, y, 10, $"Método de pago: {paymentMethod}    Estado: {paymentStatus}");
            y -= 14f;

            if (!string.IsNullOrWhiteSpace(model.Data.TrackingId))
            {
                AddText(sb, MarginLeft, y, 10, $"Tracking: {model.Data.TrackingId}");
                y -= 14f;
            }

            y -= 10f;
            AddText(sb, MarginLeft, y, 12, "Información del cliente");
            y -= 16f;

            string customerName = FormatOrDefault(model.Data.CustomerName, "Consumidor final");
            string customerEmail = FormatOrDefault(model.Data.CustomerEmail, "No registrado");
            string customerDocument = FormatOrDefault(model.Data.CustomerDocument, "No disponible");
            string sender = FormatOrDefault(model.Data.Sender, "Caja principal");

            AddText(sb, MarginLeft, y, 10, $"Cliente: {customerName}");
            y -= 14f;
            AddText(sb, MarginLeft, y, 10, $"Correo: {customerEmail}");
            y -= 14f;
            AddText(sb, MarginLeft, y, 10, $"Identificación: {customerDocument}");
            y -= 14f;
            AddText(sb, MarginLeft, y, 10, $"Emisor: {sender}");
            y -= 22f;

            y = DrawItemsTable(sb, model, y);
            y -= 18f;

            y = DrawTotals(sb, model, y);
            y -= 12f;

            y = DrawTotalsInWords(sb, model, y);
            y -= 24f;

            DrawQrSection(sb, model);
            DrawFooter(sb);

            return sb.ToString();
        }

        private static float DrawItemsTable(StringBuilder sb, InvoiceDocumentViewModel model, float startY)
        {
            var summaries = model.Summaries;
            float[] positions = BuildColumnPositions();
            float tableLeft = positions[0];
            float tableRight = positions[^1];
            float headerY = startY;
            float rowHeight = 16f;

            DrawLine(sb, tableLeft, headerY + 6f, tableRight, headerY + 6f, 1f);
            for (int i = 0; i < ColumnHeaders.Length; i++)
            {
                AddText(sb, positions[i] + 2f, headerY, 10, ColumnHeaders[i]);
            }

            float currentY = headerY - rowHeight;
            DrawLine(sb, tableLeft, currentY + 4f, tableRight, currentY + 4f, 0.7f);

            if (summaries.Count == 0)
            {
                AddText(sb, tableLeft + 4f, currentY, 10, "Sin ítems registrados");
                currentY -= rowHeight;
            }
            else
            {
                foreach (var summary in summaries)
                {
                    AddText(sb, positions[0] + 2f, currentY, 10, summary.Item.LineNumber.ToString(CultureInfo.InvariantCulture));
                    AddText(sb, positions[1] + 2f, currentY, 10, TrimForPdf(summary.Item.Code, 16));
                    AddText(sb, positions[2] + 2f, currentY, 10, TrimForPdf(summary.Item.Description, 48));
                    AddText(sb, positions[3] + 2f, currentY, 10, summary.Item.Quantity.ToString(CultureInfo.InvariantCulture));
                    AddText(sb, positions[4] + 2f, currentY, 10, FormatCurrency(summary.Item.UnitPrice));
                    decimal lineTotal = summary.Item.LineTotal + summary.TaxAmount;
                    AddText(sb, positions[5] + 2f, currentY, 10, FormatCurrency(lineTotal));

                    currentY -= rowHeight;
                    DrawLine(sb, tableLeft, currentY + 4f, tableRight, currentY + 4f, 0.3f);
                }
            }

            return currentY;
        }

        private static float DrawTotals(StringBuilder sb, InvoiceDocumentViewModel model, float startY)
        {
            float totalsX = PageWidth - MarginRight - 180f;
            float y = startY;

            AddText(sb, totalsX, y, 12, "Totales");
            y -= 16f;
            AddText(sb, totalsX, y, 10, $"Subtotal: {FormatCurrency(model.Data.Subtotal)}");
            y -= 14f;
            AddText(sb, totalsX, y, 10, $"IVA: {FormatCurrency(model.Data.Tax)}");
            y -= 14f;
            AddText(sb, totalsX, y, 11, $"Total: {FormatCurrency(model.Data.Total)}");
            y -= 18f;

            return Math.Min(startY - 60f, y);
        }

        private static float DrawTotalsInWords(StringBuilder sb, InvoiceDocumentViewModel model, float startY)
        {
            float y = startY;
            AddText(sb, MarginLeft, y, 12, "Valor en letras");
            y -= 16f;

            var lines = WrapText(model.TotalsInWords, 80);
            foreach (var line in lines)
            {
                AddText(sb, MarginLeft, y, 10, line);
                y -= 14f;
            }

            return y;
        }

        private static void DrawQrSection(StringBuilder sb, InvoiceDocumentViewModel model)
        {
            float qrSize = 120f;
            float qrLeft = MarginLeft;
            float qrBottom = MarginBottom + 60f;

            if (model.QrMatrix != null)
            {
                DrawQrMatrix(sb, model.QrMatrix, qrLeft, qrBottom, qrSize);
            }
            else
            {
                float placeholderTop = qrBottom + qrSize - 12f;
                AddText(sb, qrLeft + 12f, placeholderTop, 10, "QR no disponible");
            }

            float infoX = qrLeft + qrSize + 18f;
            float infoY = qrBottom + qrSize - 6f;

            AddText(sb, infoX, infoY, 12, "Firma digital");
            infoY -= 16f;
            string signatureHash = FormatOrDefault(model.Data.SignatureHash, "No disponible");
            AddText(sb, infoX, infoY, 10, $"Hash: {signatureHash}");
            infoY -= 14f;
            string tracking = FormatOrDefault(model.Data.TrackingId, "No registrado");
            AddText(sb, infoX, infoY, 10, $"Tracking: {tracking}");
            infoY -= 14f;
            AddText(sb, infoX, infoY, 10, "Documento generado por MyMarket ERP");
        }

        private static void DrawFooter(StringBuilder sb)
        {
            AddText(sb, MarginLeft, MarginBottom + 32f, 9, "Documento electrónico generado automáticamente. No requiere firma manuscrita.");
        }

        private static float[] BuildColumnPositions()
        {
            var positions = new float[ColumnWidths.Length + 1];
            positions[0] = MarginLeft;
            for (int i = 0; i < ColumnWidths.Length; i++)
            {
                positions[i + 1] = positions[i] + ColumnWidths[i];
            }
            return positions;
        }

        private static void AddText(StringBuilder sb, float x, float y, int fontSize, string text)
        {
            sb.AppendLine("BT");
            sb.AppendLine($"/F1 {fontSize} Tf");
            sb.AppendLine($"1 0 0 1 {FormatFloat(x)} {FormatFloat(y)} Tm");
            sb.AppendLine($"({Escape(text)}) Tj");
            sb.AppendLine("ET");
        }

        private static void DrawLine(StringBuilder sb, float x1, float y1, float x2, float y2, float width)
        {
            sb.AppendLine($"{FormatFloat(width)} w");
            sb.AppendLine($"{FormatFloat(x1)} {FormatFloat(y1)} m");
            sb.AppendLine($"{FormatFloat(x2)} {FormatFloat(y2)} l");
            sb.AppendLine("S");
        }

        private static void DrawQrMatrix(StringBuilder sb, bool[,] matrix, float left, float bottom, float size)
        {
            int modules = matrix.GetLength(0);
            float moduleSize = size / modules;

            sb.AppendLine("1 1 1 rg");
            sb.AppendLine($"{FormatFloat(left)} {FormatFloat(bottom)} {FormatFloat(size)} {FormatFloat(size)} re");
            sb.AppendLine("f");
            sb.AppendLine("0 0 0 rg");

            for (int y = 0; y < modules; y++)
            {
                for (int x = 0; x < modules; x++)
                {
                    if (!matrix[y, x])
                        continue;
                    float px = left + x * moduleSize;
                    float py = bottom + (modules - 1 - y) * moduleSize;
                    sb.AppendLine($"{FormatFloat(px)} {FormatFloat(py)} {FormatFloat(moduleSize)} {FormatFloat(moduleSize)} re");
                    sb.AppendLine("f");
                }
            }

            sb.AppendLine("0 0 0 rg");
        }

        private static string FormatCurrency(decimal value) => "$" + InvoiceDocumentModelBuilder.FormatNumber(value);

        private static string FormatOrDefault(string? value, string fallback)
            => string.IsNullOrWhiteSpace(value) ? fallback : value;

        private static string TrimForPdf(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            return value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";
        }

        private static IEnumerable<string> WrapText(string text, int maxCharacters)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<string>();

            var words = text.Split(' ');
            var lines = new List<string>();
            var current = new StringBuilder();

            foreach (var word in words)
            {
                if (current.Length + word.Length + 1 > maxCharacters)
                {
                    if (current.Length > 0)
                    {
                        lines.Add(current.ToString().Trim());
                        current.Clear();
                    }
                }

                current.Append(word);
                current.Append(' ');
            }

            if (current.Length > 0)
                lines.Add(current.ToString().Trim());

            return lines;
        }

        private static string FormatFloat(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            return text
                .Replace("\\", "\\\\")
                .Replace("(", "\\(")
                .Replace(")", "\\)");
        }

        private static class SimplePdfBuilder
        {
            public static byte[] Build(string content)
            {
                var encoding = Encoding.Latin1;
                byte[] contentBytes = encoding.GetBytes(content);
                string contentObject = $"<< /Length {contentBytes.Length} >>\nstream\n{content}\nendstream";

                var objects = new[]
                {
                    "<< /Type /Catalog /Pages 2 0 R >>",
                    "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                    "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
                    "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
                    contentObject
                };

                using var ms = new MemoryStream();
                using (var writer = new StreamWriter(ms, encoding, leaveOpen: true))
                {
                    writer.NewLine = "\n";
                    writer.WriteLine("%PDF-1.4");

                    var offsets = new long[objects.Length + 1];
                    offsets[0] = 0;

                    for (int i = 0; i < objects.Length; i++)
                    {
                        writer.Flush();
                        offsets[i + 1] = ms.Position;
                        writer.WriteLine($"{i + 1} 0 obj");
                        writer.WriteLine(objects[i]);
                        writer.WriteLine("endobj");
                    }

                    writer.Flush();
                    long xrefPosition = ms.Position;

                    writer.WriteLine("xref");
                    writer.WriteLine($"0 {objects.Length + 1}");
                    writer.WriteLine("0000000000 65535 f ");
                    for (int i = 1; i <= objects.Length; i++)
                    {
                        writer.WriteLine($"{offsets[i]:D10} 00000 n ");
                    }

                    writer.WriteLine("trailer");
                    writer.WriteLine($"<< /Size {objects.Length + 1} /Root 1 0 R >>");
                    writer.WriteLine("startxref");
                    writer.WriteLine(xrefPosition);
                    writer.WriteLine("%%EOF");
                }

                return ms.ToArray();
            }
        }
    }
}
