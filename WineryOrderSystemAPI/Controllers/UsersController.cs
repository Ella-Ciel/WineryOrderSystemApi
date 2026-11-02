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
    public class UsersController : ControllerBase
    {
        private readonly WineryDbContext _context;
        private readonly IMapper _mapper;

        public UsersController(WineryDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/users/public (Check all registered person without ID/confidential info)
        [HttpGet("public")]
        public async Task<ActionResult<IEnumerable<PublicCostumerDto>>> GetPublicUserList()
        {
            var publicList = await _context.Users
                .ProjectTo<PublicCostumerDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return Ok(publicList);
        }

        // GET: api/users/orders/customer/1 (User view: essential order details by Customer ID)
        [HttpGet("orders/customer/{costumerId}")]
        public async Task<ActionResult<IEnumerable<UserOrderDto>>> GetUserOrders(int costumerId)
        {
            var orders = await _context.Orders
                .Where(o => o.UserId == costumerId)
                .ProjectTo<UserOrderDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return Ok(orders);
        }

        // PUT: api/users/5/details (Edit personal details separately)
        [HttpPut("{costumerId}/details")]
        public async Task<IActionResult> UpdatePersonalDetails(int costumerId, [FromBody] UpdateCostumerDetailsDto dto)
        {
            var costumer = await _context.Users.FindAsync(costumerId);
            if (costumer == null) return NotFound("Customer record not found.");

            costumer.FullName = dto.FullName;
            costumer.Phone = dto.Phone;
            costumer.Address = dto.Address;

            await _context.SaveChangesAsync();
            return Ok("Personal details updated successfully.");
        }

        // PUT: api/users/orders/5 (Edit order)
        [HttpPut("orders/{orderId}")]
        public async Task<IActionResult> EditOrder(int orderId, [FromBody] UpdateOrderDto dto)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return NotFound("Order not found.");

            foreach (var item in order.OrderItems)
            {
                var wine = await _context.Wines.FindAsync(item.ProductId);
                if (wine != null) wine.StockQuantity += item.Quantity;
            }

            _context.OrderItems.RemoveRange(order.OrderItems);

            decimal newTotal = 0;
            var updatedItems = new List<OrderItem>();

            foreach (var item in dto.Items)
            {
                var wine = await _context.Wines.FindAsync(item.ProductId);
                if (wine == null || wine.StockQuantity < item.Quantity)
                    return BadRequest($"Insufficient stock or invalid Wine Product ID {item.ProductId}.");

                wine.StockQuantity -= item.Quantity;
                newTotal += wine.Price * item.Quantity;

                updatedItems.Add(new OrderItem
                {
                    OrderId = orderId,
                    ProductId = wine.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = wine.Price
                });
            }

            order.Status = dto.Status;
            order.TotalAmount = newTotal;
            order.OrderItems = updatedItems;

            await _context.SaveChangesAsync();
            return Ok("Order updated successfully.");
        }

        // POST: api/users/checkout (Checkout product using ID)
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto)
        {
            var costumer = await _context.Users.FindAsync(dto.CostumerId);
            if (costumer == null) return NotFound("Customer ID not found.");

            if (dto.Items == null || !dto.Items.Any())
                return BadRequest("Checkout payload contains no items.");

            decimal totalAmount = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in dto.Items)
            {
                var wine = await _context.Wines.FindAsync(item.ProductId);
                if (wine == null)
                    return NotFound($"Wine Product ID {item.ProductId} not found.");

                if (wine.StockQuantity < item.Quantity)
                    return BadRequest($"Insufficient stock for '{wine.WineName}'. Available stock: {wine.StockQuantity}");

                wine.StockQuantity -= item.Quantity;
                totalAmount += wine.Price * item.Quantity;

                orderItems.Add(new OrderItem
                {
                    ProductId = wine.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = wine.Price
                });
            }

            var order = new Order
            {
                UserId = dto.CostumerId,
                OrderDate = DateTime.Now,
                TotalAmount = totalAmount,
                Status = "Pending",
                OrderItems = orderItems
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Order created successfully.", OrderId = order.OrderId, TotalAmount = totalAmount });
        }

        // DELETE: api/users/orders/5 (Delete or cancel order)
        [HttpDelete("orders/{orderId}")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return NotFound("Order not found.");

            foreach (var item in order.OrderItems)
            {
                var wine = await _context.Wines.FindAsync(item.ProductId);
                if (wine != null) wine.StockQuantity += item.Quantity;
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return Ok("Order cancelled and stock restored to inventory.");
        }
    }
}