using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WineryOrderSystemAPI.Data;
using WineryOrderSystemAPI.DTOs;
using WineryOrderSystemAPI.Models;

namespace WineryOrderSystemAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly WineryDbContext _context;
        private readonly IMapper _mapper;

        public OrdersController(WineryDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/v1/Orders/{id}
        // Get a single order by OrderId
        [HttpGet("{id:int}")]
        public async Task<ActionResult<OrderReadDto>> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound("Order not found.");

            return Ok(_mapper.Map<OrderReadDto>(order));
        }

        // GET: api/v1/Orders/user/{userId}
        // Get all orders for a specific user
        [HttpGet("user/{userId:int}")]
        public async Task<ActionResult<IEnumerable<OrderReadDto>>> GetOrdersByUser(int userId)
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Ok(_mapper.Map<List<OrderReadDto>>(orders));
        }

        // POST: api/v1/Orders/checkout
        // Create an order from the user's cart (checkout)
        [HttpPost("checkout")]
        public async Task<ActionResult<OrderReadDto>> Checkout([FromBody] CheckoutDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == dto.UserId);

                if (cart == null || !cart.CartItems.Any())
                    return BadRequest("Cart is empty.");

                var productIds = cart.CartItems.Select(ci => ci.ProductId).ToList();
                var wines = await _context.Wines
                    .Where(w => productIds.Contains(w.ProductId))
                    .ToDictionaryAsync(w => w.ProductId);

                foreach (var item in cart.CartItems)
                {
                    if (!wines.TryGetValue(item.ProductId, out var wine) || wine.StockQuantity < item.Quantity)
                        return BadRequest($"Insufficient stock for ProductId {item.ProductId}");
                }

                decimal total = cart.CartItems.Sum(ci => ci.Quantity * ci.UnitPrice);

                var order = new Order
                {
                    UserId = dto.UserId,
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = total,
                    Status = "Pending"
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var cartItem in cart.CartItems)
                {
                    _context.OrderItems.Add(new OrderItem
                    {
                        OrderId = order.OrderId,
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        UnitPrice = cartItem.UnitPrice
                    });

                    if (wines.TryGetValue(cartItem.ProductId, out var wine))
                    {
                        wine.StockQuantity -= cartItem.Quantity;
                    }
                }

                _context.CartItems.RemoveRange(cart.CartItems);
                cart.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Reload with navigation properties for mapping
                await _context.Entry(order).Reference(o => o.User).LoadAsync();
                await _context.Entry(order)
                    .Collection(o => o.OrderItems)
                    .Query()
                    .Include(oi => oi.Product)
                    .LoadAsync();

                return CreatedAtAction(nameof(GetOrder), new { id = order.OrderId }, _mapper.Map<OrderReadDto>(order));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Checkout failed: {ex.Message}");
            }
        }

        // PUT: api/v1/Orders/{id}/status
        // Update the status of an order
        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound("Order not found.");

            order.Status = status;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}