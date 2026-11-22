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
    public class CartController : ControllerBase
    {
        private readonly WineryDbContext _context;
        private readonly IMapper _mapper;

        public CartController(WineryDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/v1/Cart/{userId}
        [HttpGet("{userId:int}")]
        public async Task<ActionResult<CartReadDto>> GetCart(int userId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Wine)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return Ok(_mapper.Map<CartReadDto>(cart));
        }

        // POST: api/v1/Cart/{userId}/items
        [HttpPost("{userId:int}/items")]
        public async Task<ActionResult<CartReadDto>> AddItem(int userId, [FromBody] AddCartItemDto dto)
        {
            var wine = await _context.Wines.FindAsync(dto.ProductId);
            if (wine == null)
                return BadRequest($"Wine with ProductId {dto.ProductId} does not exist.");

            if (wine.StockQuantity < dto.Quantity)
                return BadRequest($"Insufficient stock. Available: {wine.StockQuantity}");

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == dto.ProductId);

            if (existingItem != null)
            {
                int newQty = existingItem.Quantity + dto.Quantity;
                if (wine.StockQuantity < newQty)
                    return BadRequest($"Insufficient stock. Available: {wine.StockQuantity}");

                existingItem.Quantity = newQty;
                existingItem.UnitPrice = wine.Price;
            }
            else
            {
                cart.CartItems.Add(new CartItem
                {
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity,
                    UnitPrice = wine.Price,
                    AddedAt = DateTime.UtcNow
                });
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Reload with Product for mapping
            await _context.Entry(cart)
                .Collection(c => c.CartItems)
                .Query()
                .Include(ci => ci.Wine)
                .LoadAsync();

            return Ok(_mapper.Map<CartReadDto>(cart));
        }

        // PUT: api/v1/Cart/items/{cartItemId}
        [HttpPut("items/{cartItemId:int}")]
        public async Task<ActionResult<CartReadDto>> UpdateItem(int cartItemId, [FromBody] UpdateCartItemDto dto)
        {
            var item = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);

            if (item == null)
                return NotFound("Cart item not found.");

            var wine = await _context.Wines.FindAsync(item.ProductId);
            if (wine == null)
                return BadRequest("Wine not found.");

            if (wine.StockQuantity < dto.Quantity)
                return BadRequest($"Insufficient stock. Available: {wine.StockQuantity}");

            item.Quantity = dto.Quantity;
            item.Cart.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Wine)
                .FirstAsync(c => c.CartId == item.CartId);

            return Ok(_mapper.Map<CartReadDto>(cart));
        }

        // DELETE: api/v1/Cart/items/{cartItemId}
        [HttpDelete("items/{cartItemId:int}")]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            var item = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);

            if (item == null)
                return NotFound("Cart item not found.");

            _context.CartItems.Remove(item);
            item.Cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/v1/Cart/{userId}/clear
        [HttpDelete("{userId:int}/clear")]
        public async Task<IActionResult> ClearCart(int userId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                return NotFound("Cart not found.");

            _context.CartItems.RemoveRange(cart.CartItems);
            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}