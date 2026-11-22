namespace WineryOrderSystemAPI.DTOs
{
    public class OrderReadDto
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime? OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public List<OrderItemReadDto> Items { get; set; } = new();
    }

    public class OrderItemReadDto
    {
        public int ProductId { get; set; }
        public string WineName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal => Quantity * UnitPrice;
    }
}