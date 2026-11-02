namespace WineryOrderSystemAPI.DTOs
{
    public class CheckoutDto
    {
        public int CostumerId { get; set; }
        public List<CreateOrderItemDto> Items { get; set; } = new();
    }
}