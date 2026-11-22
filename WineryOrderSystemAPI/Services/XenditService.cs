using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WineryOrderSystemAPI.Services
{
    public class XenditService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public XenditService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;

            var secretKey = _config["Xendit:SecretKey"];
            if (string.IsNullOrEmpty(secretKey))
                throw new Exception("Xendit SecretKey is missing in appsettings.json");

            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes(secretKey + ":"));

            _httpClient.BaseAddress = new Uri("https://api.xendit.co/");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", authToken);
        }

        public async Task<XenditInvoiceResponse?> CreateInvoiceAsync(
            string externalId,
            decimal amount,
            string description,
            string? customerEmail = null)
        {
            var payload = new
            {
                external_id = externalId,
                amount = amount,
                description = description,
                currency = "PHP",
                invoice_duration = 86400, // 24 hours
                success_redirect_url = _config["Xendit:SuccessRedirectUrl"],
                failure_redirect_url = _config["Xendit:FailureRedirectUrl"],
                payer_email = customerEmail
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("v2/invoices", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Xendit error: {response.StatusCode} - {responseBody}");
            }

            return JsonSerializer.Deserialize<XenditInvoiceResponse>(responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }

    public class XenditInvoiceResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("external_id")]
        public string ExternalId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("invoice_url")]
        public string InvoiceUrl { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }
    }
}