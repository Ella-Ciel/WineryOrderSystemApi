using System;
using System.Collections.Generic;

namespace WineryOrderSystemAPI.Models;

public partial class DimTime
{
    public int TimeKey { get; set; }

    public DateTime? Date { get; set; }

    public int? Month { get; set; }

    public int? Quarter { get; set; }

    public int? Year { get; set; }

    public virtual ICollection<FactSale> FactSales { get; set; } = new List<FactSale>();
}
