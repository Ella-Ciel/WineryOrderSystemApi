using System;
using System.Collections.Generic;

namespace WineryOrderSystemAPI.Models;

public partial class Wine
{
    public int ProductId { get; set; }

    public string WineName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public string? Category { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
