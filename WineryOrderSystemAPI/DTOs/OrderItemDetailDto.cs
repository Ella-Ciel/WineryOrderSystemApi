namespace WineryOrderSystemAPI.DTOs
{
    public class OrderItemDetailDto
    {
        public int ProductId { get; set; }
        public string WineName { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}