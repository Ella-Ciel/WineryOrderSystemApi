using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WineryOrderSystemAPI.Data;
using WineryOrderSystemAPI.DTOs;
using WineryOrderSystemAPI.Models;

namespace WineryOrderSystemAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly WineryDbContext _context;
        private readonly IMapper _mapper;

        public AdminController(WineryDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/admin/customers (See all customers with their ID)
        [HttpGet("customers")]
        public async Task<ActionResult<IEnumerable<AdminCostumerDto>>> GetAllCustomers()
        {
            var customers = await _context.Users
                .ProjectTo<AdminCostumerDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return Ok(customers);
        }

        // GET: api/admin/orders (Admin view: view all customer orders with full info)
        [HttpGet("orders")]
        public async Task<ActionResult<IEnumerable<AdminOrderDto>>> GetAllOrders()
        {
            var orders = await _context.Orders
                .ProjectTo<AdminOrderDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return Ok(orders);
        }

        // GET: api/admin/orders/5 (Admin view: view specific order with full info)
        [HttpGet("orders/{id}")]
        public async Task<ActionResult<AdminOrderDto>> GetOrderById(int id)
        {
            var order = await _context.Orders
                .Where(o => o.OrderId == id)
                .ProjectTo<AdminOrderDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            if (order == null) return NotFound("Order not found.");

            return Ok(order);
        }

        // PUT: api/admin/wines/5 (Update or edit product)
        [HttpPut("wines/{id}")]
        public async Task<IActionResult> UpdateWine(int id, [FromBody] CreateWineDto dto)
        {
            var wine = await _context.Wines.FindAsync(id);
            if (wine == null) return NotFound("Wine product not found.");

            _mapper.Map(dto, wine);

            await _context.SaveChangesAsync();
            return Ok("Wine details updated successfully.");
        }

        // POST: api/admin/wines (Create new products)
        [HttpPost("wines")]
        public async Task<IActionResult> CreateWine([FromBody] CreateWineDto dto)
        {
            var wine = _mapper.Map<Wine>(dto);

            _context.Wines.Add(wine);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetWine", "Wines", new { id = wine.ProductId }, _mapper.Map<WineDto>(wine));
        }

        // DELETE: api/admin/wines/5 (Delete a product)
        [HttpDelete("wines/{id}")]
        public async Task<IActionResult> DeleteWine(int id)
        {
            var wine = await _context.Wines.FindAsync(id);
            if (wine == null) return NotFound("Wine product not found.");

            _context.Wines.Remove(wine);
            await _context.SaveChangesAsync();

            return Ok("Wine product deleted successfully.");
        }

        // DELETE: api/admin/customers/5 (Delete customer information)
        [HttpDelete("customers/{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var costumer = await _context.Users.FindAsync(id);
            if (costumer == null) return NotFound("Customer record not found.");

            _context.Users.Remove(costumer);
            await _context.SaveChangesAsync();

            return Ok("Customer information deleted successfully.");
        }

        // DELETE: api/admin/orders/5 (Delete order)
        [HttpDelete("orders/{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound("Order record not found.");

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return Ok("Order deleted by admin.");
        }
    }
}