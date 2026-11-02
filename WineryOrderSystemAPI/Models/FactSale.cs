using System;
using System.Collections.Generic;

namespace WineryOrderSystemAPI.Models;

public partial class FactSale
{
    public int SalesId { get; set; }

    public int WineKey { get; set; }

    public int CustomerKey { get; set; }

    public int TimeKey { get; set; }

    public int QuantitySold { get; set; }

    public decimal TotalRevenue { get; set; }

    public virtual DimCustomer CustomerKeyNavigation { get; set; } = null!;

    public virtual DimTime TimeKeyNavigation { get; set; } = null!;

    public virtual DimWine WineKeyNavigation { get; set; } = null!;
}
