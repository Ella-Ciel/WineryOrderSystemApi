using System;
using System.Collections.Generic;

namespace WineryOrderSystemAPI.Models;

public partial class DimCustomer
{
    public int CustomerKey { get; set; }

    public int? UserId { get; set; }

    public string? FullName { get; set; }

    public string? Address { get; set; }

    public virtual ICollection<FactSale> FactSales { get; set; } = new List<FactSale>();
}
