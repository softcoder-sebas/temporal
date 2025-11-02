using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace MyMarket_ERP
{
    internal static class InvoiceDocumentModelBuilder
    {
        private static readonly CultureInfo NumberCulture = CultureInfo.InvariantCulture;

        public static InvoiceDocumentViewModel Build(InvoiceDocumentData data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            var normalized = Normalize(data);
            var items = normalized.Items?.ToList() ?? new List<InvoiceDocumentItem>();
            var summaries = BuildItemSummaries(items, normalized.Tax);
            decimal taxRatePercent = normalized.Subtotal == 0m
                ? 0m
                : Math.Round(normalized.Tax / normalized.Subtotal * 100m, 2, MidpointRounding.AwayFromZero);
            string totalsInWords = ToMoneyInWords(normalized.Total);

            bool[,]? qrMatrix = SignatureQrGenerator.TryGenerateMatrix(normalized.QrPayload, normalized.SignatureHash);
            string? qrBase64 = qrMatrix is null ? null : SignatureQrGenerator.RenderToBase64(qrMatrix, 8, 4);

            return new InvoiceDocumentViewModel(normalized, summaries, totalsInWords, taxRatePercent, qrMatrix, qrBase64);
        }

        internal static string FormatNumber(decimal value) => value.ToString("N2", NumberCulture);

        internal static InvoiceDocumentData Normalize(InvoiceDocumentData data)
        {
            string? signatureHash = data.SignatureHash;
            if (string.IsNullOrWhiteSpace(signatureHash) && !string.IsNullOrWhiteSpace(data.ElectronicXml))
            {
                string? signatureValue = ExtractSignatureValue(data.ElectronicXml);
                if (!string.IsNullOrWhiteSpace(signatureValue))
                {
                    signatureHash = ComputeSignatureHash(signatureValue);
                }
            }

            string trackingId = string.IsNullOrWhiteSpace(data.TrackingId) && !string.IsNullOrWhiteSpace(signatureHash)
                ? GenerateLocalTrackingId(data.Number, signatureHash)
                : data.TrackingId ?? string.Empty;

            string? qrPayload = data.QrPayload;
            if (string.IsNullOrWhiteSpace(qrPayload) && !string.IsNullOrWhiteSpace(signatureHash))
            {
                qrPayload = BuildQrPayload(data, trackingId, signatureHash);
            }

            return data with
            {
                SignatureHash = signatureHash,
                TrackingId = trackingId,
                QrPayload = qrPayload,
                Items = data.Items ?? Array.Empty<InvoiceDocumentItem>()
            };
        }

        internal static IReadOnlyList<ItemSummary> BuildItemSummaries(IReadOnlyList<InvoiceDocumentItem> items, decimal tax)
        {
            var list = new List<ItemSummary>(items.Count);
            if (items.Count == 0)
                return list;

            decimal baseTotal = items.Sum(i => i.LineTotal);
            if (baseTotal <= 0m || tax <= 0m)
            {
                list.AddRange(items.Select(item => new ItemSummary(item, 0m)));
                return list;
            }

            decimal assigned = 0m;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                decimal taxAmount;
                if (i == items.Count - 1)
                {
                    taxAmount = Math.Round(tax - assigned, 2, MidpointRounding.AwayFromZero);
                }
                else
                {
                    decimal proportion = item.LineTotal <= 0m ? 0m : item.LineTotal / baseTotal;
                    taxAmount = Math.Round(tax * proportion, 2, MidpointRounding.AwayFromZero);
                    assigned += taxAmount;
                }

                list.Add(new ItemSummary(item, taxAmount));
            }

            decimal difference = tax - list.Sum(i => i.TaxAmount);
            if (difference != 0m && list.Count > 0)
            {
                var last = list[^1];
                list[^1] = last with { TaxAmount = last.TaxAmount + difference };
            }

            return list;
        }

        private static string BuildQrPayload(InvoiceDocumentData data, string trackingId, string signatureHash)
        {
            var builder = new StringBuilder();
            builder.Append("MYMARKET");
            builder.Append('|').Append(trackingId);
            builder.Append('|').Append(data.Number);
            builder.Append('|').Append(data.IssuedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            builder.Append('|').Append(data.Total.ToString("0.00", CultureInfo.InvariantCulture));
            builder.Append('|').Append(signatureHash);
            return builder.ToString();
        }

        private static string GenerateLocalTrackingId(string number, string signatureHash)
        {
            string suffix = signatureHash.Length > 12 ? signatureHash[..12] : signatureHash;
            if (string.IsNullOrWhiteSpace(number))
                return $"MM-{suffix}";
            return $"MM-{number}-{suffix}";
        }

        private static string ToMoneyInWords(decimal amount)
        {
            bool negative = amount < 0;
            amount = Math.Abs(amount);
            long integerPart = (long)Math.Truncate(amount);
            int cents = (int)Math.Round((amount - integerPart) * 100m, MidpointRounding.AwayFromZero);
            if (cents == 100)
            {
                integerPart++;
                cents = 0;
            }

            string words = ConvertIntegerToSpanish(integerPart);
            string centsText = cents.ToString("00", CultureInfo.InvariantCulture);
            var builder = new StringBuilder();
            if (negative)
                builder.Append("MENOS ");
            builder.Append(words);
            builder.Append(" PESOS COLOMBIANOS CON ");
            builder.Append(centsText);
            builder.Append("/100");
            return builder.ToString();
        }

        private static string ConvertIntegerToSpanish(long value)
        {
            if (value == 0)
                return "CERO";
            if (value < 0)
                return "MENOS " + ConvertIntegerToSpanish(-value);

            var parts = new List<string>();

            void Append(long number, string singular, string plural)
            {
                if (number <= 0)
                    return;
                if (number == 1)
                    parts.Add(singular);
                else
                    parts.Add(ConvertIntegerToSpanish(number) + " " + plural);
            }

            long billions = value / 1_000_000_000;
            Append(billions, "MIL MILLONES", "MIL MILLONES");
            value %= 1_000_000_000;

            long millions = value / 1_000_000;
            if (millions == 1)
                parts.Add("UN MILLON");
            else if (millions > 1)
                parts.Add(ConvertIntegerToSpanish(millions) + " MILLONES");
            value %= 1_000_000;

            long thousands = value / 1000;
            if (thousands == 1)
                parts.Add("MIL");
            else if (thousands > 1)
                parts.Add(ConvertIntegerToSpanish(thousands) + " MIL");
            value %= 1000;

            if (value > 0)
                parts.Add(ConvertHundreds((int)value));

            return string.Join(' ', parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        }

        private static string ConvertHundreds(int number)
        {
            string[] units = { "", "UNO", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE" };
            string[] teens = { "DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE", "DIECISEIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE" };
            string[] tens = { "", "DIEZ", "VEINTE", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA" };
            string[] hundreds = { "", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS", "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS" };

            if (number == 0)
                return string.Empty;
            if (number == 100)
                return "CIEN";

            var sb = new StringBuilder();
            int h = number / 100;
            int remainder = number % 100;
            if (h > 0)
            {
                sb.Append(hundreds[h]);
                if (remainder > 0)
                    sb.Append(' ');
            }

            if (remainder >= 30)
            {
                int t = remainder / 10;
                int u = remainder % 10;
                sb.Append(tens[t]);
                if (u > 0)
                    sb.Append(" Y ").Append(units[u]);
            }
            else if (remainder >= 20)
            {
                if (remainder == 20)
                {
                    sb.Append("VEINTE");
                }
                else
                {
                    sb.Append("VEINTI").Append(units[remainder - 20]);
                }
            }
            else if (remainder >= 10)
            {
                sb.Append(teens[remainder - 10]);
            }
            else if (remainder > 0)
            {
                if (remainder == 1 && sb.Length > 0)
                    sb.Append("UN");
                else
                    sb.Append(units[remainder]);
            }

            return sb.ToString().Trim();
        }

        private static string? ExtractSignatureValue(string xml)
        {
            try
            {
                var document = XDocument.Parse(xml);
                var signature = document.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals("Signature", StringComparison.OrdinalIgnoreCase));
                if (signature is null)
                    return null;
                var value = signature.Elements().FirstOrDefault(x => x.Name.LocalName.Equals("Value", StringComparison.OrdinalIgnoreCase));
                return value?.Value;
            }
            catch
            {
                return null;
            }
        }

        private static string ComputeSignatureHash(string signatureValue)
        {
            using var sha = SHA256.Create();
            byte[] data = Encoding.UTF8.GetBytes(signatureValue);
            byte[] hash = sha.ComputeHash(data);
            return Convert.ToHexString(hash);
        }
    }

    internal sealed record InvoiceDocumentViewModel(
        InvoiceDocumentData Data,
        IReadOnlyList<ItemSummary> Summaries,
        string TotalsInWords,
        decimal TaxRatePercent,
        bool[,]? QrMatrix,
        string? QrBase64);

    internal sealed record ItemSummary(InvoiceDocumentItem Item, decimal TaxAmount);
}
