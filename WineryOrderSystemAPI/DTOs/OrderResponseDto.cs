namespace WineryOrderSystemAPI.DTOs
{
    public class OrderResponseDto
    {
        public int OrderId { get; set; }
        public int UserId { get; set; } // Foreign key to CostumerId
        public DateTime? OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = null!;
        public List<OrderItemDetailDto> Items { get; set; } = new();
    }
}