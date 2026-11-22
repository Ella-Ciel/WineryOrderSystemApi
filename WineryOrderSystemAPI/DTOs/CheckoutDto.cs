namespace WineryOrderSystemAPI.DTOs
{
    public class CheckoutDto
    {
        public int UserId { get; set; }
        public List<CreateOrderItemDto> Items { get; set; } = new();
    }
}