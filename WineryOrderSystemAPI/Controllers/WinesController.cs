using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WineryOrderSystemAPI.Data;
using WineryOrderSystemAPI.DTOs;
using WineryOrderSystemAPI.Models;

namespace WineryOrderSystemAPI.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class WinesController : ControllerBase
    {
        private readonly WineryDbContext _context;
        private readonly IMapper _mapper;

        public WinesController(WineryDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/v1/Wines
        // Get all wines with optional filtering and sorting
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WineDto>>> GetWines(
            [FromQuery] string? search,
            [FromQuery] string? category,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] bool? inStockOnly,
            [FromQuery] string? sort)
        {
            var query = _context.Wines.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(w => w.WineName.Contains(search) ||
                                         (w.Category != null && w.Category.Contains(search)));

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(w => w.Category == category);

            if (minPrice.HasValue)
                query = query.Where(w => w.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(w => w.Price <= maxPrice.Value);

            if (inStockOnly == true)
                query = query.Where(w => w.StockQuantity > 0);

            query = sort?.ToLower() switch
            {
                "price_asc" => query.OrderBy(w => w.Price),
                "price_desc" => query.OrderByDescending(w => w.Price),
                "name_desc" => query.OrderByDescending(w => w.WineName),
                _ => query.OrderBy(w => w.WineName)
            };

            var wines = await query.ToListAsync();
            return Ok(_mapper.Map<List<WineDto>>(wines));
        }

        // GET: api/v1/Wines/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<WineDto>> GetWine(int id)
        {
            var wine = await _context.Wines.FindAsync(id);
            if (wine == null) return NotFound();
            return Ok(_mapper.Map<WineDto>(wine));
        }

        // GET: api/v1/Wines/featured
        // Returns latest wines for homepage hero/featured section
        [HttpGet("featured")]
        public async Task<ActionResult<IEnumerable<WineDto>>> GetFeatured()
        {
            var featured = await _context.Wines
                .OrderByDescending(w => w.CreatedAt)
                .Take(6)
                .ToListAsync();

            return Ok(_mapper.Map<List<WineDto>>(featured));
        }

        // GET: api/v1/Wines/top-sellers?limit=4
        // Returns top selling wines based on order quantity
        [HttpGet("top-sellers")]
        public async Task<ActionResult<IEnumerable<WineDto>>> GetTopSellers([FromQuery] int limit = 4)
        {
            var topSellerIds = await _context.OrderItems
                .GroupBy(oi => oi.ProductId)
                .Select(g => new { ProductId = g.Key, TotalSold = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.TotalSold)
                .Take(limit)
                .Select(x => x.ProductId)
                .ToListAsync();

            var wines = await _context.Wines
                .Where(w => topSellerIds.Contains(w.ProductId))
                .ToListAsync();

            // Preserve the order of top sellers
            var ordered = topSellerIds
                .Select(id => wines.FirstOrDefault(w => w.ProductId == id))
                .Where(w => w != null)
                .ToList();

            return Ok(_mapper.Map<List<WineDto>>(ordered));
        }

        // GET: api/v1/Wines/{id}/stock
        [HttpGet("{id:int}/stock")]
        public async Task<ActionResult<int>> GetStock(int id)
        {
            var wine = await _context.Wines.FindAsync(id);
            if (wine == null) return NotFound("Wine not found.");
            return Ok(wine.StockQuantity);
        }

        // POST: api/v1/Wines (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<WineDto>> CreateWine([FromBody] CreateWineDto dto)
        {
            var wine = _mapper.Map<Wine>(dto);
            wine.CreatedAt = DateTime.UtcNow;

            _context.Wines.Add(wine);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetWine), new { id = wine.ProductId }, _mapper.Map<WineDto>(wine));
        }

        // PUT: api/v1/Wines/{id} (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateWine(int id, [FromBody] CreateWineDto dto)
        {
            var wine = await _context.Wines.FindAsync(id);
            if (wine == null) return NotFound();

            _mapper.Map(dto, wine);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/v1/Wines/{id} (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteWine(int id)
        {
            var wine = await _context.Wines.FindAsync(id);
            if (wine == null) return NotFound();

            _context.Wines.Remove(wine);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}