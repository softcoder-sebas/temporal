using System;
using System.Globalization;
using System.Net;
using System.Text;

namespace MyMarket_ERP
{
    public sealed class InvoiceDocumentGenerator
    {
        private const string CompanyName = "MyMarket";
        private static readonly CultureInfo NumberCulture = CultureInfo.InvariantCulture;

        public string GenerateHtml(InvoiceDocumentData data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            var model = InvoiceDocumentModelBuilder.Build(data);
            var normalized = model.Data;
            var summaries = model.Summaries;
            string totalsInWords = model.TotalsInWords;
            string? qrBase64 = model.QrBase64 ?? SignatureQrGenerator.TryGenerate(normalized.QrPayload, normalized.SignatureHash);

            string customerName = Encode(normalized.CustomerName);
            string customerEmail = Encode(normalized.CustomerEmail);
            string customerDocument = Encode(normalized.CustomerDocument);
            string paymentMethod = Encode(string.IsNullOrWhiteSpace(normalized.PaymentMethod) ? "Sin método" : normalized.PaymentMethod);
            string paymentStatus = Encode(string.IsNullOrWhiteSpace(normalized.PaymentStatus) ? "Sin estado" : normalized.PaymentStatus);
            string tracking = Encode(string.IsNullOrWhiteSpace(normalized.TrackingId) ? "No registrado" : normalized.TrackingId);
            string signatureHash = Encode(string.IsNullOrWhiteSpace(normalized.SignatureHash) ? "No disponible" : normalized.SignatureHash);
            string issuedDate = normalized.IssuedAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture);
            string subtotal = InvoiceDocumentModelBuilder.FormatNumber(normalized.Subtotal);
            string tax = InvoiceDocumentModelBuilder.FormatNumber(normalized.Tax);
            string total = InvoiceDocumentModelBuilder.FormatNumber(normalized.Total);
            decimal taxRatePercent = model.TaxRatePercent;
            string taxRateText = taxRatePercent.ToString("0.##", NumberCulture);

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"es\">");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset=\"utf-8\">");
            sb.AppendLine("    <title>Factura " + Encode(normalized.Number) + "</title>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        body { font-family: 'Segoe UI', Arial, sans-serif; background: #f5f7fa; color: #1f2933; margin: 0; padding: 32px; }");
            sb.AppendLine("        .invoice { max-width: 920px; margin: 0 auto; background: #ffffff; border-radius: 12px; box-shadow: 0 18px 36px rgba(15, 23, 42, 0.12); overflow: hidden; }");
            sb.AppendLine("        .top { display: flex; justify-content: space-between; align-items: flex-start; gap: 24px; padding: 32px 36px; background: linear-gradient(120deg, #009688, #00bfa5); color: #ffffff; }");
            sb.AppendLine("        .brand h1 { margin: 0; font-size: 36px; letter-spacing: 1px; text-transform: uppercase; }");
            sb.AppendLine("        .brand p { margin: 8px 0 0; font-size: 18px; }");
            sb.AppendLine("        .meta { background: rgba(255, 255, 255, 0.12); padding: 16px 20px; border-radius: 12px; font-size: 14px; }");
            sb.AppendLine("        .meta table { border-collapse: collapse; }");
            sb.AppendLine("        .meta th { text-align: left; padding: 4px 12px 4px 0; opacity: 0.86; font-weight: 500; }");
            sb.AppendLine("        .meta td { padding: 4px 0; font-weight: 600; }");
            sb.AppendLine("        .section { padding: 28px 36px; border-bottom: 1px solid #e4ebf5; }");
            sb.AppendLine("        .section h2 { margin: 0 0 16px; font-size: 20px; color: #016a63; }");
            sb.AppendLine("        .info-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 16px; }");
            sb.AppendLine("        .info-card { background: #f8fbfd; border: 1px solid #e3edf8; border-radius: 10px; padding: 16px; }");
            sb.AppendLine("        .info-card span { display: block; font-size: 12px; text-transform: uppercase; letter-spacing: 0.08em; color: #5f6c7b; margin-bottom: 6px; }");
            sb.AppendLine("        .info-card strong { font-size: 15px; color: #102a43; }");
            sb.AppendLine("        table.items { width: 100%; border-collapse: collapse; margin-top: 12px; }");
            sb.AppendLine("        table.items th { background: #e0f2f1; color: #014d44; font-weight: 600; font-size: 13px; padding: 12px 10px; text-transform: uppercase; letter-spacing: 0.05em; }");
            sb.AppendLine("        table.items td { padding: 12px 10px; border-bottom: 1px solid #ecf2f8; font-size: 13px; }");
            sb.AppendLine("        table.items tr:last-child td { border-bottom: none; }");
            sb.AppendLine("        table.items td.align-right { text-align: right; font-variant-numeric: tabular-nums; }");
            sb.AppendLine("        .totals { display: flex; justify-content: flex-end; margin-top: 20px; }");
            sb.AppendLine("        .totals table { border-collapse: collapse; min-width: 260px; }");
            sb.AppendLine("        .totals th, .totals td { padding: 10px 14px; font-size: 14px; }");
            sb.AppendLine("        .totals th { text-align: left; color: #5f6c7b; }");
            sb.AppendLine("        .totals td { text-align: right; font-weight: 600; color: #102a43; }");
            sb.AppendLine("        .totals tr.total { background: #009688; color: #ffffff; }");
            sb.AppendLine("        .totals tr.total td, .totals tr.total th { color: #ffffff; font-size: 15px; }");
            sb.AppendLine("        .notes { padding: 20px 36px 28px; font-size: 13px; line-height: 1.6; color: #3c4858; background: #f8fbfd; }");
            sb.AppendLine("        .notes strong { display: block; margin-bottom: 6px; text-transform: uppercase; letter-spacing: 0.08em; color: #016a63; }");
            sb.AppendLine("        .qr { display: flex; align-items: center; gap: 24px; padding: 24px 36px 36px; }");
            sb.AppendLine("        .qr img { width: 150px; height: 150px; border: 8px solid #f8fbfd; border-radius: 12px; background: #ffffff; box-shadow: 0 6px 14px rgba(0,0,0,0.12); }");
            sb.AppendLine("        .qr .details { font-size: 13px; line-height: 1.7; color: #344050; }
            ");
            sb.AppendLine("        .qr .details span { display: block; font-weight: 600; color: #014d44; margin-bottom: 6px; text-transform: uppercase; letter-spacing: 0.08em; }");
            sb.AppendLine("        .qr-placeholder { width: 150px; height: 150px; border-radius: 12px; background: repeating-linear-gradient(45deg, #e0e7ef, #e0e7ef 8px, #f0f4fa 8px, #f0f4fa 16px); display: flex; align-items: center; justify-content: center; font-size: 12px; color: #5f6c7b; font-weight: 600; text-align: center; padding: 12px; }");
            sb.AppendLine("        .footer { padding: 0 36px 36px; font-size: 11px; color: #7b8794; text-align: right; }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("    <div class=\"invoice\">");
            sb.AppendLine("        <div class=\"top\">");
            sb.AppendLine("            <div class=\"brand\">");
            sb.AppendLine("                <h1>" + CompanyName + "</h1>");
            sb.AppendLine("                <p>Factura electrónica de venta No. <strong>" + Encode(normalized.Number) + "</strong></p>");
            sb.AppendLine("            </div>");
            sb.AppendLine("            <div class=\"meta\">");
            sb.AppendLine("                <table>");
            sb.AppendLine("                    <tr><th>Fecha generación:</th><td>" + issuedDate + "</td></tr>");
            sb.AppendLine("                    <tr><th>Estado:</th><td>" + paymentStatus + "</td></tr>");
            sb.AppendLine("                    <tr><th>Método de pago:</th><td>" + paymentMethod + "</td></tr>");
            sb.AppendLine("                    <tr><th>Tracking:</th><td>" + tracking + "</td></tr>");
            sb.AppendLine("                </table>");
            sb.AppendLine("            </div>");
            sb.AppendLine("        </div>");

            sb.AppendLine("        <section class=\"section\">");
            sb.AppendLine("            <h2>Información del cliente</h2>");
            sb.AppendLine("            <div class=\"info-grid\">");
            sb.AppendLine("                <div class=\"info-card\"><span>Cliente</span><strong>" + (string.IsNullOrWhiteSpace(customerName) ? "Consumidor final" : customerName) + "</strong></div>");
            sb.AppendLine("                <div class=\"info-card\"><span>Correo electrónico</span><strong>" + (string.IsNullOrWhiteSpace(customerEmail) ? "No registrado" : customerEmail) + "</strong></div>");
            sb.AppendLine("                <div class=\"info-card\"><span>Identificación</span><strong>" + (string.IsNullOrWhiteSpace(customerDocument) ? "No disponible" : customerDocument) + "</strong></div>");
            sb.AppendLine("                <div class=\"info-card\"><span>Emisor</span><strong>" + Encode(normalized.Sender ?? "Caja principal") + "</strong></div>");
            sb.AppendLine("            </div>");
            sb.AppendLine("        </section>");

            sb.AppendLine("        <section class=\"section\">");
            sb.AppendLine("            <h2>Detalle de la operación</h2>");
            sb.AppendLine("            <table class=\"items\">");
            sb.AppendLine("                <thead>");
            sb.AppendLine("                    <tr><th>#</th><th>Código</th><th>Descripción</th><th class=\"align-right\">Cantidad</th><th class=\"align-right\">Valor unitario</th><th class=\"align-right\">Base</th><th class=\"align-right\">IVA %</th><th class=\"align-right\">Valor IVA</th><th class=\"align-right\">Total</th></tr>");
            sb.AppendLine("                </thead>");
            sb.AppendLine("                <tbody>");

            if (summaries.Count == 0)
            {
                sb.AppendLine("                    <tr><td colspan=\"9\" style=\"text-align:center; padding: 28px 12px; color: #7b8794;\">Sin ítems registrados</td></tr>");
            }
            else
            {
                foreach (var summary in summaries)
                {
                    string lineNumber = summary.Item.LineNumber.ToString(NumberCulture);
                    string code = Encode(summary.Item.Code);
                    string description = Encode(summary.Item.Description);
                    string qty = summary.Item.Quantity.ToString(NumberCulture);
                    string unitPrice = InvoiceDocumentModelBuilder.FormatNumber(summary.Item.UnitPrice);
                    string baseValue = InvoiceDocumentModelBuilder.FormatNumber(summary.Item.LineTotal);
                    string taxAmount = InvoiceDocumentModelBuilder.FormatNumber(summary.TaxAmount);
                    string lineTotal = InvoiceDocumentModelBuilder.FormatNumber(summary.Item.LineTotal + summary.TaxAmount);

                    sb.Append("                    <tr>");
                    sb.Append("<td class=\"align-right\">" + lineNumber + "</td>");
                    sb.Append("<td>" + code + "</td>");
                    sb.Append("<td>" + description + "</td>");
                    sb.Append("<td class=\"align-right\">" + qty + "</td>");
                    sb.Append("<td class=\"align-right\">$" + unitPrice + "</td>");
                    sb.Append("<td class=\"align-right\">$" + baseValue + "</td>");
                    sb.Append("<td class=\"align-right\">" + taxRateText + "%</td>");
                    sb.Append("<td class=\"align-right\">$" + taxAmount + "</td>");
                    sb.Append("<td class=\"align-right\">$" + lineTotal + "</td>");
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("                </tbody>");
            sb.AppendLine("            </table>");
            sb.AppendLine("            <div class=\"totals\">");
            sb.AppendLine("                <table>");
            sb.AppendLine("                    <tr><th>Subtotal</th><td>$" + subtotal + "</td></tr>");
            sb.AppendLine("                    <tr><th>IVA</th><td>$" + tax + "</td></tr>");
            sb.AppendLine("                    <tr class=\"total\"><th>Total</th><td>$" + total + "</td></tr>");
            sb.AppendLine("                </table>");
            sb.AppendLine("            </div>");
            sb.AppendLine("        </section>");

            sb.AppendLine("        <div class=\"notes\">");
            sb.AppendLine("            <strong>Observaciones</strong>");
            sb.AppendLine("            Somos grandes contribuyentes según la resolución 011122 del 2024. Esta factura genera obligación de pago de los impuestos sobre la renta.");
            sb.AppendLine("            <br><br><strong>Valor en letras:</strong> " + Encode(totalsInWords));
            sb.AppendLine("        </div>");

            sb.AppendLine("        <div class=\"qr\">");
            if (!string.IsNullOrWhiteSpace(qrBase64))
            {
                sb.AppendLine("            <img src=\"data:image/png;base64," + qrBase64 + "\" alt=\"Código QR de la firma digital\">");
            }
            else
            {
                sb.AppendLine("            <div class=\"qr-placeholder\">QR no disponible</div>");
            }
            sb.AppendLine("            <div class=\"details\">");
            sb.AppendLine("                <span>Firma digital</span>");
            sb.AppendLine("                Hash: " + signatureHash + "<br>");
            sb.AppendLine("                Tracking: " + tracking + "<br>");
            sb.AppendLine("                Documento generado por " + CompanyName + " ERP");
            sb.AppendLine("            </div>");
            sb.AppendLine("        </div>");

            sb.AppendLine("        <div class=\"footer\">Documento electrónico generado automáticamente. No requiere firma manuscrita.</div>");
            sb.AppendLine("    </div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
    }

    public sealed record class InvoiceDocumentData
    {
        public string Number { get; init; } = string.Empty;
        public DateTime IssuedAt { get; init; }
        public string? PaymentMethod { get; init; }
        public string? PaymentStatus { get; init; }
        public string? CustomerName { get; init; }
        public string? CustomerEmail { get; init; }
        public string? CustomerDocument { get; init; }
        public string? Sender { get; init; }
        public decimal Subtotal { get; init; }
        public decimal Tax { get; init; }
        public decimal Total { get; init; }
        public string? TrackingId { get; init; }
        public string? SignatureHash { get; init; }
        public string? QrPayload { get; init; }
        public string? ElectronicXml { get; init; }
        public IReadOnlyCollection<InvoiceDocumentItem> Items { get; init; } = Array.Empty<InvoiceDocumentItem>();
    }

    public sealed record class InvoiceDocumentItem
    {
        public int LineNumber { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public decimal UnitPrice { get; init; }
        public decimal LineTotal { get; init; }
    }
}
