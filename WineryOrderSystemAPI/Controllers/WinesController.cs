using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WineryOrderSystemAPI.Data;
using WineryOrderSystemAPI.DTOs;

namespace WineryOrderSystemAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WinesController : ControllerBase
    {
        private readonly WineryDbContext _context;
        private readonly IMapper _mapper;

        public WinesController(WineryDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/wines (View all products)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WineDto>>> GetWines()
        {
            var wines = await _context.Wines
                .ProjectTo<WineDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return Ok(wines);
        }

        // GET: api/wines/5 (View specific product by ID)
        [HttpGet("{id}")]
        public async Task<ActionResult<WineDto>> GetWine(int id)
        {
            var wine = await _context.Wines.FindAsync(id);
            if (wine == null) return NotFound("Wine product not found.");

            return Ok(_mapper.Map<WineDto>(wine));
        }
    }
}