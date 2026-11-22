namespace WineryOrderSystemAPI.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int OrderId { get; set; }

    public string PaymentProvider { get; set; } = null!;

    public string? ProviderPaymentId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? PaymentMethod { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? RawResponse { get; set; }

    public virtual Order Order { get; set; } = null!;
}
