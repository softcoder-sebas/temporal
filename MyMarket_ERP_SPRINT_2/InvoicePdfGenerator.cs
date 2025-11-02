using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
//SRE
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
        // Encabezados sin tildes; V.Unitario y TOTAL ajustados
        private static readonly string[] ColumnHeaders = { "#", "Codigo", "Descripcion", "Cantidad", "V.Unitario", "TOTAL" };

        // Heuristics for measuring text width (approximate)
        private const float AvgCharWidthFactor = 0.55f; // average char width relative to font size
        private const float CellPadding = 12f; // total horizontal padding inside a cell (left + right)

        private const float HeaderFontSize = 9f;
        private const float BodyFontSize = 10f;
        private const float MinBodyFontSize = 7f;
        private const float RowLineHeightFactor = 1.2f; // multiplier for font size to compute row height

        private enum TextAlign { Left, Right, Center }

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

            AddText(sb, MarginLeft, y, 24f, "MyMarket");
            y -= 28f;

            AddText(sb, MarginLeft, y, 14f, $"Factura electronica de venta No. {model.Data.Number}");
            y -= 20f;

            string issued = model.Data.IssuedAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);
            AddText(sb, MarginLeft, y, 10f, $"Fecha de emision: {issued}");
            y -= 14f;

            string paymentMethod = FormatOrDefault(model.Data.PaymentMethod, "Sin metodo");
            string paymentStatus = FormatOrDefault(model.Data.PaymentStatus, "Sin estado");
            AddText(sb, MarginLeft, y, 10f, $"Metodo de pago: {paymentMethod}    Estado: {paymentStatus}");
            y -= 14f;

            if (!string.IsNullOrWhiteSpace(model.Data.TrackingId))
            {
                AddText(sb, MarginLeft, y, 10f, $"Tracking: {model.Data.TrackingId}");
                y -= 14f;
            }

            y -= 10f;
            AddText(sb, MarginLeft, y, 12f, "Informacion del cliente");
            y -= 16f;

            string customerName = FormatOrDefault(model.Data.CustomerName, "Consumidor final");
            string customerEmail = FormatOrDefault(model.Data.CustomerEmail, "No registrado");
            string customerDocument = FormatOrDefault(model.Data.CustomerDocument, "No disponible");
            string sender = FormatOrDefault(model.Data.Sender, "Caja principal");

            AddText(sb, MarginLeft, y, 10f, $"Cliente: {customerName}");
            y -= 14f;
            AddText(sb, MarginLeft, y, 10f, $"Correo: {customerEmail}");
            y -= 14f;
            AddText(sb, MarginLeft, y, 10f, $"Identificacion: {customerDocument}");
            y -= 14f;
            AddText(sb, MarginLeft, y, 10f, $"Emisor: {sender}");
            y -= 22f;

            y = DrawItemsTable(sb, model, y);
            y -= 18f;

            y = DrawTotals(sb, model, y);
            y -= 12f;

            y = DrawTotalsInWords(sb, model, y);
            y -= 24f;

            // QR / signature / footer removed per request

            return sb.ToString();
        }

        private static float DrawItemsTable(StringBuilder sb, InvoiceDocumentViewModel model, float startY)
        {
            var summaries = model.Summaries;
            float[] columnWidths = ComputeColumnWidths(model);
            float[] positions = BuildColumnPositions(columnWidths);
            float tableLeft = positions[0];
            float tableRight = positions[^1];
            float headerY = startY;
            float defaultRowHeight = 18f;

            // Draw table outer border (top)
            DrawLine(sb, tableLeft - 2f, headerY + 8f, tableRight + 2f, headerY + 8f, 1f);

            // Header background (blue)
            float headerHeight = 20f;
            float headerTop = headerY + 6f;
            float headerBottom = headerTop - headerHeight;
            sb.AppendLine("0.16 0.50 0.92 rg"); // blue fill
            sb.AppendLine($"{FormatFloat(tableLeft - 2f)} {FormatFloat(headerBottom)} {FormatFloat(tableRight - tableLeft + 4f)} {FormatFloat(headerHeight)} re");
            sb.AppendLine("f");
            sb.AppendLine("0 0 0 rg");

            // Header text in white, vertically centered in header box
            float headerTextY = headerBottom + headerHeight / 2f + 2f; // center
            sb.AppendLine("1 1 1 rg");
            for (int i = 0; i < ColumnHeaders.Length; i++)
            {
                float colLeft = positions[i];
                float colRight = positions[i + 1];
                float colCenter = (colLeft + colRight) / 2f;
                // center header labels horizontally in each column
                AddText(sb, colCenter, headerTextY, HeaderFontSize, ColumnHeaders[i], TextAlign.Center);
            }
            sb.AppendLine("0 0 0 rg");

            // draw vertical separators in header to align with columns
            for (int i = 1; i < positions.Length - 1; i++)
            {
                float x = positions[i];
                DrawLine(sb, x, headerTop + 2f, x, headerBottom - 2f, 0.8f);
            }

            // Header bottom line
            DrawLine(sb, tableLeft - 2f, headerBottom, tableRight + 2f, headerBottom, 0.8f);

            // Prepare to draw rows: set top coordinate for the first row area
            float rowTop = headerBottom - 4f; // top Y of first row box
            float lastRowBottom = rowTop;

            if (summaries.Count == 0)
            {
                float cellTop = rowTop;
                float cellBottom = cellTop - defaultRowHeight;
                AddText(sb, tableLeft + 6f, cellBottom + defaultRowHeight / 2f + 2f, BodyFontSize, "Sin items registrados");
                lastRowBottom = cellBottom;
            }
            else
            {
                bool alternate = false;
                foreach (var summary in summaries)
                {
                    float cellTop = rowTop;

                    // Per-column: try fit text by reducing font until it fits, otherwise trim
                    var texts = new string[ColumnHeaders.Length];
                    var usedFonts = new float[ColumnHeaders.Length];
                    texts[0] = summary.Item.LineNumber.ToString(CultureInfo.InvariantCulture);
                    texts[1] = summary.Item.Code ?? string.Empty;
                    texts[2] = summary.Item.Description ?? string.Empty;
                    texts[3] = summary.Item.Quantity.ToString(CultureInfo.InvariantCulture);
                    texts[4] = FormatCurrency(summary.Item.UnitPrice);
                    decimal lineTotal = summary.Item.LineTotal + summary.TaxAmount;
                    texts[5] = FormatCurrency(lineTotal);

                    float maxColumnHeight = 0f;
                    for (int col = 0; col < ColumnHeaders.Length; col++)
                    {
                        float maxWidth = columnWidths[col];
                        // Available width inside cell (subtract small padding)
                        float avail = Math.Max(8f, maxWidth - CellPadding);
                        float fontUsed;
                        string fitted = FitTextToWidth(RemoveDiacritics(texts[col]), avail, BodyFontSize, MinBodyFontSize, out fontUsed);
                        texts[col] = fitted;
                        usedFonts[col] = fontUsed;
                        float rowHeightForCol = Math.Max(defaultRowHeight, fontUsed * RowLineHeightFactor + 6f);
                        if (rowHeightForCol > maxColumnHeight) maxColumnHeight = rowHeightForCol;
                    }

                    float cellBottom = cellTop - maxColumnHeight;

                    // optional row background stripe
                    if (alternate)
                    {
                        sb.AppendLine("0.94 0.97 1 rg");
                        sb.AppendLine($"{FormatFloat(tableLeft - 2f)} {FormatFloat(cellBottom)} {FormatFloat(tableRight - tableLeft + 4f)} {FormatFloat(maxColumnHeight)} re");
                        sb.AppendLine("f");
                        sb.AppendLine("0 0 0 rg");
                    }

                    // row text baseline (centered vertically in row box)
                    float rowTextY = cellBottom + maxColumnHeight / 2f + 2f;

                    // Render each column inside its cell boundaries
                    for (int col = 0; col < ColumnHeaders.Length; col++)
                    {
                        float colLeft = positions[col];
                        float colRight = positions[col + 1];
                        float font = usedFonts[col];
                        string text = texts[col];

                        if (col >= 3)
                        {
                            // right align: place near right edge with padding
                            AddText(sb, colRight - 6f, rowTextY, font, text, TextAlign.Right);
                        }
                        else
                        {
                            // left align with padding
                            AddText(sb, colLeft + 6f, rowTextY, font, text, TextAlign.Left);
                        }

                        // draw vertical cell boundary for clarity
                        DrawLine(sb, colRight, cellTop + 2f, colRight, cellBottom - 2f, 0.5f);
                    }

                    // horizontal separator under the row
                    DrawLine(sb, tableLeft - 2f, cellBottom - 2f, tableRight + 2f, cellBottom - 2f, 0.3f);

                    lastRowBottom = cellBottom - 2f;
                    // move to next row
                    rowTop = cellBottom - 2f;
                    alternate = !alternate;
                }
            }

            // Draw left border and right border for table full height
            DrawLine(sb, tableLeft - 2f, headerTop + 2f, tableLeft - 2f, lastRowBottom - 2f, 0.8f);
            DrawLine(sb, tableRight + 2f, headerTop + 2f, tableRight + 2f, lastRowBottom - 2f, 0.8f);

            return lastRowBottom - 8f;
        }

        private static float DrawTotals(StringBuilder sb, InvoiceDocumentViewModel model, float startY)
        {
            float boxWidth = 220f;
            float boxLeft = PageWidth - MarginRight - boxWidth;
            float boxTop = startY;
            float boxHeight = 70f;

            // light blue background for totals
            sb.AppendLine("0.88 0.94 1 rg");
            sb.AppendLine($"{FormatFloat(boxLeft)} {FormatFloat(boxTop - boxHeight)} {FormatFloat(boxWidth)} {FormatFloat(boxHeight)} re");
            sb.AppendLine("f");
            sb.AppendLine("0 0 0 rg");

            float y = boxTop - 16f;
            AddText(sb, boxLeft + 10f, y, 12f, "Totales");
            y -= 16f;
            AddText(sb, boxLeft + 10f, y, 10f, $"Subtotal: {FormatCurrency(model.Data.Subtotal)}");
            y -= 14f;
            AddText(sb, boxLeft + 10f, y, 10f, $"IVA: {FormatCurrency(model.Data.Tax)}");
            y -= 14f;
            AddText(sb, boxLeft + 10f, y, 11f, $"TOTAL: {FormatCurrency(model.Data.Total)}");

            // border around totals
            DrawLine(sb, boxLeft, boxTop, boxLeft + boxWidth, boxTop, 0.8f);
            DrawLine(sb, boxLeft, boxTop - boxHeight, boxLeft + boxWidth, boxTop - boxHeight, 0.8f);
            DrawLine(sb, boxLeft, boxTop, boxLeft, boxTop - boxHeight, 0.8f);
            DrawLine(sb, boxLeft + boxWidth, boxTop, boxLeft + boxWidth, boxTop - boxHeight, 0.8f);

            return boxTop - boxHeight - 10f;
        }

        private static float DrawTotalsInWords(StringBuilder sb, InvoiceDocumentViewModel model, float startY)
        {
            float y = startY;
            AddText(sb, MarginLeft, y, 12f, "Valor en letras");
            y -= 16f;

            var lines = WrapText(model.TotalsInWords, 80);
            foreach (var line in lines)
            {
                AddText(sb, MarginLeft, y, 10f, line);
                y -= 14f;
            }

            return y;
        }

        private static float[] ComputeColumnWidths(InvoiceDocumentViewModel model)
        {
            const int headerFontSize = 9;
            const int bodyFontSize = 10;

            int cols = ColumnHeaders.Length;
            var desired = new float[cols];

            float availableWidth = PageWidth - MarginLeft - MarginRight;

            // Start with header widths
            for (int i = 0; i < cols; i++)
            {
                desired[i] = MeasureTextWidth(ColumnHeaders[i], HeaderFontSize) + CellPadding;
            }

            // Expand based on content
            foreach (var summary in model.Summaries)
            {
                string s0 = summary.Item.LineNumber.ToString(CultureInfo.InvariantCulture);
                string s1 = summary.Item.Code ?? string.Empty;
                string s2 = summary.Item.Description ?? string.Empty;
                string s3 = summary.Item.Quantity.ToString(CultureInfo.InvariantCulture);
                string s4 = FormatCurrency(summary.Item.UnitPrice);
                decimal lineTotal = summary.Item.LineTotal + summary.TaxAmount;
                string s5 = FormatCurrency(lineTotal);

                desired[0] = Math.Max(desired[0], MeasureTextWidth(s0, BodyFontSize) + CellPadding);
                desired[1] = Math.Max(desired[1], MeasureTextWidth(s1, BodyFontSize) + CellPadding);
                desired[2] = Math.Max(desired[2], MeasureTextWidth(s2, BodyFontSize) + CellPadding);
                desired[3] = Math.Max(desired[3], MeasureTextWidth(s3, BodyFontSize) + CellPadding);
                desired[4] = Math.Max(desired[4], MeasureTextWidth(s4, BodyFontSize) + CellPadding);
                desired[5] = Math.Max(desired[5], MeasureTextWidth(s5, BodyFontSize) + CellPadding);
            }

            // Minimum widths to keep layout usable (aumentados para columnas numericas)
            var min = new float[] { 20f, 50f, 80f, 40f, 90f, 110f };
            for (int i = 0; i < cols; i++)
                desired[i] = Math.Max(desired[i], min[i]);

            float totalDesired = 0f;
            for (int i = 0; i < cols; i++) totalDesired += desired[i];

            if (totalDesired <= availableWidth)
                return desired;

            // proportional scaling with minimum clamps
            float scale = availableWidth / totalDesired;
            for (int i = 0; i < cols; i++)
                desired[i] = Math.Max(min[i], desired[i] * scale);

            // If still too large, iteratively trim from columns above minimum
            float sum = 0f;
            for (int i = 0; i < cols; i++) sum += desired[i];

            if (sum <= availableWidth)
                return desired;

            float excess = sum - availableWidth;
            // compute total shrinkable amount
            float totalShrinkable = 0f;
            for (int i = 0; i < cols; i++)
            {
                float shrinkable = desired[i] - min[i];
                if (shrinkable > 0)
                    totalShrinkable += shrinkable;
            }

            if (totalShrinkable <= 0)
                return desired; // cannot shrink further

            for (int i = 0; i < cols; i++)
            {
                float shrinkable = desired[i] - min[i];
                if (shrinkable <= 0) continue;
                float take = Math.Min(shrinkable, shrinkable / totalShrinkable * excess);
                desired[i] -= take;
            }

            return desired;
        }

        private static float[] BuildColumnPositions(float[] columnWidths)
        {
            var positions = new float[columnWidths.Length + 1];
            positions[0] = MarginLeft;
            for (int i = 0; i < columnWidths.Length; i++)
            {
                positions[i + 1] = positions[i] + columnWidths[i];
            }
            return positions;
        }

        private static float MeasureTextWidth(string text, float fontSize)
        {
            if (string.IsNullOrEmpty(text))
                return 0f;
            return text.Length * fontSize * AvgCharWidthFactor;
        }

        private static string TrimForPdf(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            return value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";
        }

        private static string FitTextToWidth(string? value, float maxWidth, float initialFontSize, float minFontSize, out float fontUsed)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                fontUsed = initialFontSize;
                return string.Empty;
            }

            string txt = value;
            // try with initial font
            if (MeasureTextWidth(txt, initialFontSize) <= maxWidth)
            {
                fontUsed = initialFontSize;
                return txt;
            }

            // progressively reduce font
            float fs = initialFontSize;
            while (fs > minFontSize)
            {
                fs -= 0.5f;
                if (MeasureTextWidth(txt, fs) <= maxWidth)
                {
                    fontUsed = fs;
                    return txt;
                }
            }

            // At minimum font size, if still does not fit, trim characters
            fontUsed = minFontSize;
            int maxChars = Math.Max(1, (int)Math.Floor(maxWidth / (fontUsed * AvgCharWidthFactor)));
            if (maxChars <= 3) return txt.Substring(0, Math.Min(1, txt.Length)) + "…";
            if (txt.Length <= maxChars) return txt;
            return txt.Substring(0, maxChars - 1) + "…";
        }

        private static void AddText(StringBuilder sb, float x, float y, float fontSize, string text, TextAlign align = TextAlign.Left)
        {
            // Quitar tildes/diacriticos antes de volcar el texto al PDF
            text = RemoveDiacritics(text);

            sb.AppendLine("BT");
            sb.AppendLine($"/F1 {FormatFloat(fontSize)} Tf");
            // Alignment handling: left (x as-is), right (x - textWidth), center (x - textWidth/2)
            float textWidth = MeasureTextWidth(text, fontSize);
            float tx = x;
            if (align == TextAlign.Right)
            {
                tx = x - textWidth;
            }
            else if (align == TextAlign.Center)
            {
                tx = x - textWidth / 2f;
            }

            sb.AppendLine($"1 0 0 1 {FormatFloat(tx)} {FormatFloat(y)} Tm");
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

        private static string RemoveDiacritics(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static class SimplePdfBuilder
        {
            public static byte[] Build(string content)
            {
                var encoding = Encoding.GetEncoding(1252); // use Windows-1252 to support accented characters
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