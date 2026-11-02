namespace WineryOrderSystemAPI.DTOs
{
    public class UpdateOrderDto
    {
        public string Status { get; set; } = null!;
        public List<CreateOrderItemDto> Items { get; set; } = new();
    }
}