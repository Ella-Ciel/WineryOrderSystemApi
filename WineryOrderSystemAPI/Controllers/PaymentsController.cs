using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WineryOrderSystemAPI.Data;
using WineryOrderSystemAPI.DTOs;
using WineryOrderSystemAPI.Models;
using WineryOrderSystemAPI.Services;

namespace WineryOrderSystemAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly WineryDbContext _context;
        private readonly XenditService _xenditService;
        private readonly IMapper _mapper;

        public PaymentsController(WineryDbContext context, XenditService xenditService, IMapper mapper)
        {
            _context = context;
            _xenditService = xenditService;
            _mapper = mapper;
        }

        // POST: api/v1/Payments/create-invoice/{orderId}
        [HttpPost("create-invoice/{orderId:int}")]
        public async Task<ActionResult> CreateInvoice(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return BadRequest("Order does not exist.");

            if (order.Status != "Pending" && order.Status != "PendingPayment")
                return BadRequest("Order is not in a payable status.");

            var payment = new Payment
            {
                OrderId = order.OrderId,
                PaymentProvider = "Xendit",
                Amount = order.TotalAmount,
                Currency = "PHP",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            order.Status = "PendingPayment";
            await _context.SaveChangesAsync();

            try
            {
                var invoice = await _xenditService.CreateInvoiceAsync(
                    externalId: order.OrderId.ToString(),
                    amount: order.TotalAmount,
                    description: $"Winery Order #{order.OrderId}",
                    customerEmail: order.User?.Email
                );

                if (invoice == null)
                    return StatusCode(500, "Failed to create Xendit invoice.");

                payment.ProviderPaymentId = invoice.Id;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    paymentId = payment.PaymentId,
                    orderId = order.OrderId,
                    invoiceUrl = invoice.InvoiceUrl,
                    status = invoice.Status,
                    amount = invoice.Amount
                });
            }
            catch (Exception ex)
            {
                payment.Status = "Failed";
                await _context.SaveChangesAsync();
                return StatusCode(500, $"Payment creation failed: {ex.Message}");
            }
        }

        // POST: api/v1/Payments/webhook
        [HttpPost("webhook")]
        public async Task<IActionResult> XenditWebhook()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            var status = root.GetProperty("status").GetString();
            var externalId = root.GetProperty("external_id").GetString();
            var invoiceId = root.GetProperty("id").GetString();

            if (string.IsNullOrEmpty(externalId) || !int.TryParse(externalId, out int orderId))
                return BadRequest();

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null)
                return NotFound();

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderId == orderId && p.ProviderPaymentId == invoiceId);

            if (payment == null)
                return NotFound();

            payment.RawResponse = body;

            if (status == "PAID" || status == "SETTLED")
            {
                payment.Status = "Paid";
                payment.PaidAt = DateTime.UtcNow;
                order.Status = "Paid";
            }
            else if (status == "EXPIRED")
            {
                payment.Status = "Expired";
                order.Status = "PaymentExpired";
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        // GET: api/v1/Payments/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<PaymentReadDto>> GetPayment(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
                return NotFound();

            return Ok(_mapper.Map<PaymentReadDto>(payment));
        }
    }
}