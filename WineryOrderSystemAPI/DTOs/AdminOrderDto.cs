namespace WineryOrderSystemAPI.DTOs
{
    public class AdminOrderDto
    {
        public int OrderId { get; set; }
        public int CostumerId { get; set; }
        public string CustomerName { get; set; } = null!;
        public string CustomerEmail { get; set; } = null!;
        public string? CustomerPhone { get; set; }
        public string? DeliveryAddress { get; set; }
        public DateTime? OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = null!;
        public List<OrderItemDetailDto> Items { get; set; } = new();
    }
}