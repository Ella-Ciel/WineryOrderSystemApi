using System.ComponentModel.DataAnnotations;

namespace WineryOrderSystemAPI.DTOs
{
    public class CartReadDto
    {
        public int CartId { get; set; }
        public int UserId { get; set; }
        public DateTime UpdatedAt { get; set; }
        public decimal SubTotal { get; set; }
        public List<CartItemReadDto> Items { get; set; } = new();
    }

    public class CartItemReadDto
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public string WineName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal => Quantity * UnitPrice;
        public int AvailableStock { get; set; }
    }

    public class AddCartItemDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required, Range(1, 100)]
        public int Quantity { get; set; }
    }

    public class UpdateCartItemDto
    {
        [Required, Range(1, 100)]
        public int Quantity { get; set; }
    }
}