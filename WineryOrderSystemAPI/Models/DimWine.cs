using System;
using System.Collections.Generic;

namespace WineryOrderSystemAPI.Models;

public partial class DimWine
{
    public int WineKey { get; set; }

    public int? ProductId { get; set; }

    public string? WineName { get; set; }

    public string? Category { get; set; }

    public virtual ICollection<FactSale> FactSales { get; set; } = new List<FactSale>();
}
