using Microsoft.EntityFrameworkCore;
using Polly;
using System.Net.Http.Headers;
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
                replyText = await FetchLiveGrokResponseAsync(request.Prompt);
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

        private async Task<string> FetchLiveGrokResponseAsync(string userPrompt)
        {
            var apiKey = _configuration["Grok:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return "AI key is missing in appsettings.json.";
            }

            var requestUrl = _configuration["Grok:BaseUrl"] ?? "https://api.x.ai/v1/chat/completions";

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

            string fullSystemPrompt = "You are a virtual assistant for the 'Winery Order System'. " +
                                      "You MUST only answer questions related to this winery system (wines, prices, stock, orders, payments, delivery, FAQs). " +
                                      "If the question is unrelated to the system, reply only with: " +
                                      "'I'm sorry, I can only help with questions about the Winery Order System.' " +
                                      "Use the SYSTEM DATA CONTEXT below for relevant answers.\n\n" +
                                      systemContext;

            // 2. Polly retry policy for 429 rate limits or 5xx transient server errors
            var retryPolicy = Policy
                .HandleResult<HttpResponseMessage>(r => (int)r.StatusCode == 429 || (int)r.StatusCode >= 500)
                .WaitAndRetryAsync(
                    retryCount: 1,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(2)
                );

            // 3. Multi-model fallback using Grok models from appsettings.json (with defaults)
            var candidateModels = _configuration.GetSection("Grok:Models").Get<string[]>()
                                  ?? new[] { "grok-3-mini", "grok-3" };

            string lastRawError = string.Empty;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            foreach (var model in candidateModels)
            {
                var requestBody = new
                {
                    model = model,
                    messages = new[]
                    {
                        new { role = "system", content = fullSystemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                    temperature = 0.3
                };

                try
                {
                    var response = await retryPolicy.ExecuteAsync(async () =>
                    {
                        var jsonPayload = JsonSerializer.Serialize(requestBody);
                        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                        httpRequest.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                        return await _httpClient.SendAsync(httpRequest);
                    });

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(jsonResponse);

                        var reply = doc.RootElement
                            .GetProperty("choices")[0]
                            .GetProperty("message")
                            .GetProperty("content")
                            .GetString();

                        return reply ?? "I'm sorry, I couldn't generate a response.";
                    }

                    lastRawError = await response.Content.ReadAsStringAsync();
                }
                catch (TaskCanceledException)
                {
                    return "Error: Request to Grok timed out after 30 seconds. Please check your network connection.";
                }
                catch (Exception ex)
                {
                    return $"Error connecting to AI service: {ex.Message}";
                }
            }

            return $"Grok API Error: {lastRawError}";
        }
    }
}