using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace MyMarket_ERP
{
    public sealed class ElectronicInvoiceProcessor
    {
        private const string DefaultNamespace = "urn:mymarket:electronicinvoice:v1";
        private static readonly HttpClient SharedHttpClient = CreateHttpClient();

        private readonly DigitalSignatureService _signatureService;
        private readonly string? _endpoint;
        private readonly string _senderCode;
        private readonly string? _certificateWarning;

        private ElectronicInvoiceProcessor(DigitalSignatureService signatureService, string? endpoint, string senderCode, string? certificateWarning)
        {
            _signatureService = signatureService;
            _endpoint = string.IsNullOrWhiteSpace(endpoint) ? null : endpoint;
            _senderCode = string.IsNullOrWhiteSpace(senderCode) ? "MyMarket-ERP" : senderCode;
            _certificateWarning = certificateWarning;
        }

        public static ElectronicInvoiceProcessor CreateDefault()
        {
            var certificate = TryLoadCertificate(out string? warning);
            var signatureService = new DigitalSignatureService(certificate);
            var endpoint = Environment.GetEnvironmentVariable("MYMARKET_EINVOICE_ENDPOINT");
            var sender = Environment.GetEnvironmentVariable("MYMARKET_EINVOICE_SENDER") ?? "MyMarket-ERP";
            return new ElectronicInvoiceProcessor(signatureService, endpoint, sender, warning);
        }

        public ElectronicInvoiceResult Process(ElectronicInvoiceContext context)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));
            if (context.Items is null || context.Items.Count == 0)
                throw new InvalidOperationException("La factura electrónica requiere al menos un ítem.");

            bool simulationMode = string.IsNullOrWhiteSpace(_endpoint);
            var document = BuildInvoiceXml(context, _senderCode, simulationMode);
            string canonical = document.ToString(SaveOptions.DisableFormatting);
            string signatureValue = _signatureService.Sign(canonical);

            document.Root!.Add(new XElement(XName.Get("Signature", DefaultNamespace),
                new XElement(XName.Get("SerialNumber", DefaultNamespace), _signatureService.SerialNumber),
                new XElement(XName.Get("Algorithm", DefaultNamespace), "RSA-SHA256"),
                new XElement(XName.Get("Value", DefaultNamespace), signatureValue),
                new XElement(XName.Get("SignedAtUtc", DefaultNamespace), DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture))
            ));

            string signedXml = document.ToString(SaveOptions.None);
            var result = SendToRegulator(context, signedXml);

            if (!string.IsNullOrWhiteSpace(_certificateWarning))
            {
                result.AppendMessage(_certificateWarning);
            }

            if (_signatureService.IsSimulated)
            {
                result.AppendMessage("Se utilizó una firma digital simulada. Configure MYMARKET_EINVOICE_CERT_PATH para emplear un certificado real.");
            }

            return result;
        }

        private ElectronicInvoiceResult SendToRegulator(ElectronicInvoiceContext context, string signedXml)
        {
            if (string.IsNullOrWhiteSpace(_endpoint))
            {
                string trackingId = $"SIM-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
                string message = $"Factura electrónica generada en modo simulación para {context.Number}. Configure MYMARKET_EINVOICE_ENDPOINT para habilitar el envío automático.";
                return ElectronicInvoiceResult.SuccessResult(signedXml, trackingId, message);
            }

            var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
            request.Headers.TryAddWithoutValidation("X-Integrator", _senderCode);
            request.Headers.TryAddWithoutValidation("X-Invoice-Number", context.Number);
            request.Headers.TryAddWithoutValidation("X-Certificate-Serial", _signatureService.SerialNumber);
            request.Content = new StringContent(signedXml, Encoding.UTF8, "application/xml");

            try
            {
                var response = SharedHttpClient.SendAsync(request).GetAwaiter().GetResult();
                string? payload = response.Content == null ? null : response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (response.IsSuccessStatusCode)
                {
                    string trackingId = ExtractTrackingId(payload) ?? $"R-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
                    string message = string.IsNullOrWhiteSpace(payload)
                        ? "Factura aceptada por la DIAN."
                        : $"Factura aceptada por la DIAN. Respuesta: {payload}";
                    return ElectronicInvoiceResult.SuccessResult(signedXml, trackingId, message);
                }

                string failureMessage = $"La DIAN rechazó la factura (HTTP {(int)response.StatusCode}). {payload}";
                return ElectronicInvoiceResult.FailureResult(failureMessage, signedXml, null);
            }
            catch (Exception ex)
            {
                string failureMessage = $"Error enviando la factura a la DIAN: {ex.Message}";
                return ElectronicInvoiceResult.FailureResult(failureMessage, signedXml, null);
            }
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            return client;
        }

        private static X509Certificate2? TryLoadCertificate(out string? warning)
        {
            warning = null;
            string? path = Environment.GetEnvironmentVariable("MYMARKET_EINVOICE_CERT_PATH");
            if (string.IsNullOrWhiteSpace(path))
            {
                warning = "No se configuró un certificado digital. Se utilizará una firma simulada.";
                return null;
            }

            if (!File.Exists(path))
            {
                warning = $"No se encontró el certificado digital en la ruta especificada ({path}). Se utilizará una firma simulada.";
                return null;
            }

            string? password = Environment.GetEnvironmentVariable("MYMARKET_EINVOICE_CERT_PASSWORD");
            try
            {
                return string.IsNullOrEmpty(password)
                    ? new X509Certificate2(path)
                    : new X509Certificate2(path, password);
            }
            catch (Exception ex)
            {
                warning = $"No se pudo cargar el certificado digital: {ex.Message}. Se utilizará una firma simulada.";
                return null;
            }
        }

        private static string? ExtractTrackingId(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return null;

            try
            {
                using var json = JsonDocument.Parse(payload);
                var root = json.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("trackingId", out var tracking))
                        return tracking.GetString();
                    if (root.TryGetProperty("cufe", out var cufe))
                        return cufe.GetString();
                    if (root.TryGetProperty("documento", out var documento) && documento.ValueKind == JsonValueKind.Object)
                    {
                        if (documento.TryGetProperty("cufe", out var nestedCufe))
                            return nestedCufe.GetString();
                    }
                }
            }
            catch (JsonException)
            {
                // Ignorar, la respuesta no es JSON.
            }

            const string trackingKey = "tracking";
            int index = payload.IndexOf(trackingKey, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                int colon = payload.IndexOf(':', index);
                if (colon > index)
                {
                    colon++;
                    while (colon < payload.Length && char.IsWhiteSpace(payload[colon]))
                        colon++;
                    if (colon < payload.Length && (payload[colon] == '\"' || payload[colon] == '\''))
                    {
                        char quote = payload[colon];
                        colon++;
                        int end = payload.IndexOf(quote, colon);
                        if (end > colon)
                            return payload[colon..end];
                    }
                }
            }

            return null;
        }

        private static XDocument BuildInvoiceXml(ElectronicInvoiceContext context, string senderCode, bool simulationMode)
        {
            var ns = (XNamespace)DefaultNamespace;
            var root = new XElement(ns + "Invoice",
                new XAttribute("version", "1.0"),
                new XElement(ns + "Header",
                    new XElement(ns + "Number", context.Number),
                    new XElement(ns + "IssueDate", context.IssuedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                    new XElement(ns + "IssueTime", context.IssuedAt.ToString("HH:mm:ss", CultureInfo.InvariantCulture)),
                    new XElement(ns + "Currency", context.Currency ?? "COP"),
                    new XElement(ns + "Environment", simulationMode ? "PRUEBAS" : "PRODUCCION"),
                    new XElement(ns + "Sender", string.IsNullOrWhiteSpace(context.Sender) ? senderCode : context.Sender),
                    new XElement(ns + "CashierEmail", context.CashierEmail ?? string.Empty)
                ),
                new XElement(ns + "Customer",
                    new XElement(ns + "Name", context.CustomerName ?? string.Empty),
                    new XElement(ns + "Email", context.CustomerEmail ?? string.Empty),
                    new XElement(ns + "Document", context.CustomerDocument ?? string.Empty)
                ),
                new XElement(ns + "Payment",
                    new XElement(ns + "Method", context.PaymentMethod ?? string.Empty),
                    new XElement(ns + "Status", context.PaymentStatus ?? "Pagada")
                ),
                new XElement(ns + "Totals",
                    new XElement(ns + "Subtotal", context.Subtotal.ToString("0.00", CultureInfo.InvariantCulture)),
                    new XElement(ns + "Tax", context.Tax.ToString("0.00", CultureInfo.InvariantCulture)),
                    new XElement(ns + "Total", context.Total.ToString("0.00", CultureInfo.InvariantCulture))
                ),
                new XElement(ns + "Items",
                    context.Items.Select(item => BuildItemElement(ns, item))
                )
            );

            return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root);
        }

        private static XElement BuildItemElement(XNamespace ns, ElectronicInvoiceItem item)
        {
            decimal taxAmount = Math.Round(item.LineTotal * item.TaxRate, 2, MidpointRounding.AwayFromZero);
            decimal totalWithTax = item.LineTotal + taxAmount;

            return new XElement(ns + "Item",
                new XAttribute("line", item.LineNumber),
                new XElement(ns + "Code", item.Code ?? string.Empty),
                new XElement(ns + "Description", item.Description ?? string.Empty),
                new XElement(ns + "Quantity", item.Quantity.ToString(CultureInfo.InvariantCulture)),
                new XElement(ns + "UnitPrice", item.UnitPrice.ToString("0.00", CultureInfo.InvariantCulture)),
                new XElement(ns + "LineTotal", item.LineTotal.ToString("0.00", CultureInfo.InvariantCulture)),
                new XElement(ns + "TaxRate", item.TaxRate.ToString("0.0000", CultureInfo.InvariantCulture)),
                new XElement(ns + "TaxAmount", taxAmount.ToString("0.00", CultureInfo.InvariantCulture)),
                new XElement(ns + "TotalWithTax", totalWithTax.ToString("0.00", CultureInfo.InvariantCulture))
            );
        }
    }

    public sealed class ElectronicInvoiceContext
    {
        public int InvoiceId { get; init; }
        public string Number { get; init; } = string.Empty;
        public DateTime IssuedAt { get; init; }
        public string? CashierEmail { get; init; }
        public string? CustomerName { get; init; }
        public string? CustomerEmail { get; init; }
        public string? CustomerDocument { get; init; }
        public string? PaymentMethod { get; init; }
        public string? PaymentStatus { get; init; }
        public decimal Subtotal { get; init; }
        public decimal Tax { get; init; }
        public decimal Total { get; init; }
        public string Currency { get; init; } = "COP";
        public string? Sender { get; init; }
        public IReadOnlyCollection<ElectronicInvoiceItem> Items { get; init; } = Array.Empty<ElectronicInvoiceItem>();
    }

    public sealed class ElectronicInvoiceItem
    {
        public int LineNumber { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public decimal UnitPrice { get; init; }
        public decimal LineTotal { get; init; }
        public decimal TaxRate { get; init; }
    }

    public sealed class ElectronicInvoiceResult
    {
        private ElectronicInvoiceResult(bool success, string? message, string? signedXml, string? trackingId)
        {
            Success = success;
            Message = message;
            SignedXml = signedXml;
            TrackingId = trackingId;
        }

        public bool Success { get; }
        public string? Message { get; private set; }
        public string? SignedXml { get; }
        public string? TrackingId { get; }

        public static ElectronicInvoiceResult SuccessResult(string signedXml, string trackingId, string? message)
            => new(true, message, signedXml, trackingId);

        public static ElectronicInvoiceResult FailureResult(string message, string? signedXml, string? trackingId)
            => new(false, message, signedXml, trackingId);

        public ElectronicInvoiceResult AppendMessage(string? extra)
        {
            if (string.IsNullOrWhiteSpace(extra))
                return this;

            Message = string.IsNullOrWhiteSpace(Message)
                ? extra
                : $"{Message} {extra}";
            return this;
        }
    }

    public sealed class DigitalSignatureService
    {
        private readonly RSAParameters _rsaParameters;

        public DigitalSignatureService(X509Certificate2? certificate)
        {
            if (certificate != null && certificate.HasPrivateKey)
            {
                using var rsa = certificate.GetRSAPrivateKey();
                if (rsa == null)
                    throw new InvalidOperationException("El certificado digital no contiene una llave privada RSA.");
                _rsaParameters = rsa.ExportParameters(true);
                SerialNumber = certificate.SerialNumber;
                IsSimulated = false;
            }
            else
            {
                using var rsa = RSA.Create(2048);
                _rsaParameters = rsa.ExportParameters(true);
                SerialNumber = "SIM-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(6));
                IsSimulated = true;
            }
        }

        public string SerialNumber { get; }
        public bool IsSimulated { get; }

        public string Sign(string xmlContent)
        {
            using var rsa = RSA.Create();
            rsa.ImportParameters(_rsaParameters);
            byte[] data = Encoding.UTF8.GetBytes(xmlContent);
            byte[] signature = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(signature);
        }
    }
}
