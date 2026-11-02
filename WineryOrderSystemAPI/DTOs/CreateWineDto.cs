namespace WineryOrderSystemAPI.DTOs
{
    public class CreateWineDto
    {
        public string WineName { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? Category { get; set; }
    }
}