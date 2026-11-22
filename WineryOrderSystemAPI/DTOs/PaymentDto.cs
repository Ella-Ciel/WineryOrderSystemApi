namespace WineryOrderSystemAPI.DTOs
{
    public class PaymentReadDto
    {
        public int PaymentId { get; set; }
        public int OrderId { get; set; }
        public string PaymentProvider { get; set; } = string.Empty;
        public string? ProviderPaymentId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateInvoiceResponseDto
    {
        public int PaymentId { get; set; }
        public int OrderId { get; set; }
        public string InvoiceUrl { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}