using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WineryOrderSystemAPI.Data;

namespace WineryOrderSystemAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticsController : ControllerBase
    {
        private readonly WineryReadDbContext _readContext;

        public AnalyticsController(WineryReadDbContext readContext)
        {
            _readContext = readContext;
        }

        [HttpGet("sales-report")]
        public async Task<IActionResult> GetSalesReport()
        {
            var report = await _readContext.FactSales
                .Include(f => f.WineKeyNavigation)
                .GroupBy(f => f.WineKeyNavigation.WineName)
                .Select(g => new
                {
                    WineName = g.Key,
                    TotalQuantitySold = g.Sum(x => x.QuantitySold),
                    TotalRevenue = g.Sum(x => x.TotalRevenue)
                })
                .ToListAsync();

            return Ok(report);
        }
    }
}