namespace WineryOrderSystemAPI.DTOs
{
    public class UserOrderDto
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = null!;
        public DateTime? OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = null!;
        public List<OrderItemDetailDto> Items { get; set; } = new();
    }
}