using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WineryOrderSystemAPI.Data;
using WineryOrderSystemAPI.DTOs;
using WineryOrderSystemAPI.Models;
using WineryOrderSystemAPI.Services;

namespace WineryOrderSystemAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _chatbotService;
        private readonly WineryDbContext _context;

        public ChatbotController(IChatbotService chatbotService, WineryDbContext context)
        {
            _chatbotService = chatbotService;
            _context = context;
        }

        // Send message to Chatbot
        [HttpPost("message")]
        public async Task<IActionResult> SendMessage([FromBody] ChatPromptDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
                return BadRequest("Prompt cannot be empty.");

            var response = await _chatbotService.ProcessUserMessageAsync(request);
            return Ok(response);
        }

        // Get full chat log history for a specific session
        [HttpGet("session/{sessionId}/history")]
        public async Task<IActionResult> GetSessionHistory(int sessionId)
        {
            var history = await _context.ChatMessages
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    m.MessageId,
                    m.MessageText,
                    m.IsBot,
                    m.SentAt
                })
                .ToListAsync();

            return Ok(history);
        }

        // Admin Endpoint: Train / Add new FAQ to Database
        [HttpPost("faqs/add")]
        public async Task<IActionResult> AddFaq([FromBody] FaqCreateDto dto)
        {
            var faq = new Faq
            {
                Question = dto.Question,
                Answer = dto.Answer
            };

            _context.Faqs.Add(faq);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "FAQ added successfully.", FaqId = faq.FaqId });
        }

        // Get all FAQs
        [HttpGet("faqs")]
        public async Task<IActionResult> GetFaqs()
        {
            var faqs = await _context.Faqs.ToListAsync();
            return Ok(faqs);
        }
    }
}