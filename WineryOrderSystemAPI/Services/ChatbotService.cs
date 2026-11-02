using Microsoft.EntityFrameworkCore;
using Polly;
using System.Text;
using System.Text.Json;
using WineryOrderSystemAPI.Data;
using WineryOrderSystemAPI.DTOs;
using WineryOrderSystemAPI.Models;

namespace WineryOrderSystemAPI.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly WineryDbContext _primaryContext;
        private readonly WineryReadDbContext _secondaryContext;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ChatbotService(
            WineryDbContext primaryContext,
            WineryReadDbContext secondaryContext,
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _primaryContext = primaryContext;
            _secondaryContext = secondaryContext;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<ChatResponseDto> ProcessUserMessageAsync(ChatPromptDto request)
        {
            // 1. Get or Create Session in Primary DB
            ChatSession session;
            if (request.SessionId.HasValue && request.SessionId > 0)
            {
                session = await _primaryContext.ChatSessions.FindAsync(request.SessionId.Value)
                          ?? await CreateNewSessionAsync(request.UserId);
            }
            else
            {
                session = await CreateNewSessionAsync(request.UserId);
            }

            // 2. Log User Prompt
            _primaryContext.ChatMessages.Add(new ChatMessage
            {
                SessionId = session.SessionId,
                MessageText = request.Prompt,
                IsBot = false,
                SentAt = DateTime.UtcNow
            });

            // 3. Rule-Based Match (Check local FAQs first for instant response)
            var faqs = await _primaryContext.Faqs.AsNoTracking().ToListAsync();
            var matchedFaq = faqs.FirstOrDefault(f =>
                request.Prompt.Contains(f.Question, StringComparison.OrdinalIgnoreCase) ||
                f.Question.Contains(request.Prompt, StringComparison.OrdinalIgnoreCase));

            string replyText;
            bool isFromFaq = false;

            if (matchedFaq != null)
            {
                replyText = matchedFaq.Answer;
                isFromFaq = true;
            }
            else
            {
                // 4. Live External AI Fallback with System Data Context
                replyText = await FetchLiveGeminiResponseAsync(request.Prompt);
            }

            // 5. Save AI Bot Response
            _primaryContext.ChatMessages.Add(new ChatMessage
            {
                SessionId = session.SessionId,
                MessageText = replyText,
                IsBot = true,
                SentAt = DateTime.UtcNow
            });

            await _primaryContext.SaveChangesAsync();

            return new ChatResponseDto
            {
                SessionId = session.SessionId,
                Response = replyText,
                IsFromFaq = isFromFaq
            };
        }

        private async Task<ChatSession> CreateNewSessionAsync(int userId)
        {
            var session = new ChatSession
            {
                UserId = userId,
                StartedAt = DateTime.UtcNow
            };
            _primaryContext.ChatSessions.Add(session);
            await _primaryContext.SaveChangesAsync();
            return session;
        }

        private async Task<string> FetchLiveGeminiResponseAsync(string userPrompt)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return "AI key is missing in appsettings.json.";
            }

            // 1. Gather System Context concurrently with non-tracking queries
            var winesTask = _primaryContext.Wines
                .AsNoTracking()
                .Select(w => $"{w.WineName} ({w.Category}): Price=₱{w.Price}, Stock={w.StockQuantity}")
                .ToListAsync();

            var topSalesTask = _secondaryContext.FactSales
                .AsNoTracking()
                .Include(f => f.WineKeyNavigation)
                .GroupBy(f => f.WineKeyNavigation.WineName)
                .Select(g => $"{g.Key} (Total Sales: ₱{g.Sum(x => x.TotalRevenue)})")
                .Take(3)
                .ToListAsync();

            await Task.WhenAll(winesTask, topSalesTask);

            var winesSummary = await winesTask;
            var topSalesSummary = await topSalesTask;

            string systemContext = $"SYSTEM DATA CONTEXT:\n" +
                                   $"Available Catalog & Stock:\n- {string.Join("\n- ", winesSummary)}\n" +
                                   $"Top Revenue Wines (Analytics):\n- {string.Join("\n- ", topSalesSummary)}\n";

            // 2. Structured JSON Payload with explicit systemInstruction
            var requestBody = new
            {
                systemInstruction = new
                {
                    parts = new[]
                    {
                        new
                        {
                            text = "You are an intelligent virtual assistant for 'Winery Order System'. " +
                                   "Use the provided SYSTEM DATA CONTEXT to give precise, polite, and concise answers regarding prices, stock, top sellers, and recommendations.\n\n" +
                                   systemContext
                        }
                    }
                },
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = userPrompt }
                        }
                    }
                }
            };

            // 3. Define Polly Resilience Strategy (Retry on 503 Service Unavailable / 5xx with Exponential Backoff + Jitter)
            var retryPolicy = Policy
                .HandleResult<HttpResponseMessage>(r => (int)r.StatusCode == 503 || (int)r.StatusCode >= 500)
                .WaitAndRetryAsync(
                    retryCount: 1,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(2)
                );

            // 4. Active Multi-Model Fallback Chain
            string[] candidateModels = { "gemini-3.8-flash", "gemini-3.5-flash-lite" };
            string lastRawError = string.Empty;

            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            foreach (var model in candidateModels)
            {
                var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

                try
                {
                    var response = await retryPolicy.ExecuteAsync(async () =>
                    {
                        var jsonPayload = JsonSerializer.Serialize(requestBody);
                        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                        return await _httpClient.PostAsync(requestUrl, content);
                    });

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(jsonResponse);

                        var reply = doc.RootElement
                            .GetProperty("candidates")[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0]
                            .GetProperty("text")
                            .GetString();

                        return reply ?? "I'm sorry, I couldn't generate a response.";
                    }

                    lastRawError = await response.Content.ReadAsStringAsync();
                }
                catch (TaskCanceledException)
                {
                    return "Error: Request to Gemini timed out after 30 seconds. Please check your network connection.";
                }
                catch (Exception ex)
                {
                    return $"Error connecting to AI service: {ex.Message}";
                }
            }

            return $"Gemini API Error: {lastRawError}";
        }
    }
}