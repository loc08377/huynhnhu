using duAn1.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace duAn1.Services
{
    public class GeminiAIService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<GeminiAIService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        private const string SYSTEM_PROMPT = @"
You are AI Stylist – a friendly fashion consultant for an online silver jewelry shop.

Your role:
Help customers discover beautiful silver bracelets and chat naturally like a real store assistant.

Personality:
- Friendly and warm
- Natural Vietnamese conversation
- Helpful and enthusiastic about jewelry
- Never sound like a robot or search engine

Important abilities:

You must understand informal Vietnamese.

Users might say things like:
- 'cho mình xem vài chiếc vòng'
- 'shop có gì mới'
- 'có mẫu nào đẹp không'
- 'tôi muốn vòng khoảng 300k'
- 'tôi cần vòng tặng bạn gái'
- 'trendy'

All of these mean the user wants product suggestions.

When recommending products:
- Recommend max 5 products
- Only use products from the provided product list
- Prefer popular products (sales / favorites)

If the question is unrelated to jewelry respond politely.

Response format MUST be JSON:

{
  ""message"": ""friendly conversational response"",
  ""recommended_products"": [
    { ""id"": 1 },
    { ""id"": 2 }
  ]
}
";

        public GeminiAIService(
            AppDbContext context,
            ILogger<GeminiAIService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        // --------------------------------------------------
        // GET PRODUCTS
        // --------------------------------------------------

        public async Task<List<dynamic>> GetAllActiveProducts()
        {
            var products = await _context.Products
                .AsNoTracking()
                .Where(p => p.Actived == true)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price,
                    description = p.Description,
                    image = p.Image,
                    category_name = _context.Categories
                        .Where(c => c.Id == p.CategoryId)
                        .Select(c => c.Name)
                        .FirstOrDefault(),

                    total_sales = _context.OrderDetails.Count(o => o.ProductId == p.Id),
                    total_favorites = _context.Favorites.Count(f => f.ProductId == p.Id)
                })
                .ToListAsync();

            return products.Cast<dynamic>().ToList();
        }

        // --------------------------------------------------
        // PRICE EXTRACTION
        // --------------------------------------------------

        public (int min, int max)? ExtractPriceRange(string message)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                message,
                @"(\d+)\s*(k|000)?\s*(?:đến|-)?\s*(\d+)\s*(k|000)?",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            if (!match.Success) return null;

            int min = int.Parse(match.Groups[1].Value);
            int max = int.Parse(match.Groups[3].Value);

            if (min < 1000) min *= 1000;
            if (max < 1000) max *= 1000;

            return (min, max);
        }

        // --------------------------------------------------
        // MAIN AI METHOD
        // --------------------------------------------------

        public async Task<(string? message, List<int> ids)> AskAI(
            string userMessage,
            string sessionId)
        {
            try
            {
                var apiKey = _configuration["Gemini:ApiKey"];

                if (string.IsNullOrEmpty(apiKey))
                    throw new Exception("Missing Gemini API key");

                var history = await GetConversationHistory(sessionId);

                var products = await GetAllActiveProducts();

                // reduce context size
                var candidateProducts = products
                    .OrderByDescending(p => (int)p.total_sales)
                    .ThenByDescending(p => (int)p.total_favorites)
                    .Take(15)
                    .ToList();

                var productJson = JsonSerializer.Serialize(candidateProducts);

                var contents = new List<object>();

                // history
                foreach (var (role, content) in history)
                {
                    contents.Add(new
                    {
                        role = role == "assistant" ? "model" : "user",
                        parts = new[] { new { text = content } }
                    });
                }

                // product context
                contents.Add(new
                {
                    role = "user",
                    parts = new[]
                    {
                        new
                        {
                            text = $"Danh sách sản phẩm:\n{productJson}"
                        }
                    }
                });

                // actual user message
                contents.Add(new
                {
                    role = "user",
                    parts = new[] { new { text = userMessage } }
                });

                var request = new
                {
                    contents = contents,

                    systemInstruction = new
                    {
                        parts = new[]
                        {
                            new { text = SYSTEM_PROMPT }
                        }
                    },

                    generationConfig = new
                    {
                        temperature = 0.15,
                        topP = 0.9,
                        maxOutputTokens = 1000
                    }
                };

                var url =
                    $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3-flash-preview:generateContent?key={apiKey}";

                var response = await _httpClient.PostAsJsonAsync(url, request);

                var json = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Gemini API Response: {json}");

                JsonElement result;
                try
                {
                    result = JsonSerializer.Deserialize<JsonElement>(json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse Gemini API response JSON");
                    return ("Xin lỗi, tôi gặp lỗi khi xử lý phản hồi. Vui lòng thử lại!", new List<int>());
                }

                // Check if API returned an error
                if (result.TryGetProperty("error", out var errorProp))
                {
                    string errorMsg = "Xin lỗi, API gặp lỗi";
                    if (errorProp.TryGetProperty("message", out var msgProp))
                    {
                        errorMsg = msgProp.GetString() ?? errorMsg;
                    }
                    
                    // Check for quota exceeded error
                    if (errorMsg.Contains("quota", StringComparison.OrdinalIgnoreCase) || 
                        errorMsg.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("API Quota exceeded: {msg}", errorMsg);
                        return ("hết token rồi.", new List<int>());
                    }
                    
                    _logger.LogError("API Error: {msg}", errorMsg);
                    return ($"Lỗi từ API: {errorMsg}", new List<int>());
                }

                // Extract text from API response with safe parsing
                string text = "";
                try
                {
                    if (!result.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                    {
                        _logger.LogWarning("API response has no candidates");
                        text = "Xin lỗi, tôi không nhận được phản hồi từ AI";
                    }
                    else
                    {
                        var candidate = candidates[0];
                        if (candidate.TryGetProperty("content", out var content) &&
                            content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                        {
                            var part = parts[0];
                            if (part.TryGetProperty("text", out var textProp))
                            {
                                text = textProp.GetString() ?? "";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract text from Gemini response");
                    text = "Xin lỗi, tôi gặp lỗi khi xử lý phản hồi";
                }

                string message = "";
                List<int> ids = new();

                // Try to parse text as JSON
                try
                {
                    var aiJson = JsonSerializer.Deserialize<JsonElement>(text ?? "{}");

                    // Try to get message from JSON response
                    if (aiJson.TryGetProperty("message", out var msgProperty))
                    {
                        message = msgProperty.GetString() ?? "";
                    }
                    else
                    {
                        // If no "message" key, use the raw text response
                        message = text ?? "";
                    }

                    // Get recommended products
                    if (aiJson.TryGetProperty("recommended_products", out var arr))
                    {
                        foreach (var p in arr.EnumerateArray())
                        {
                            if (p.TryGetProperty("id", out var idProperty))
                            {
                                ids.Add(idProperty.GetInt32());
                            }
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse JSON from AI response, using raw text: {text}", text);
                    // AI response might not be JSON, use raw text as message
                    message = text ?? "Xin lỗi, tôi gặp sự cố khi xử lý phản hồi";
                }

                // Fallback message if empty
                if (string.IsNullOrWhiteSpace(message))
                {
                    message = "Xin lỗi, tôi không thể xử lý yêu cầu của bạn. Vui lòng thử lại!";
                }

                return (message, ids);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI error");

                return ("Xin lỗi, tôi gặp chút sự cố. Bạn thử lại nhé!", new List<int>());
            }
        }

        // --------------------------------------------------
        // CHAT HISTORY
        // --------------------------------------------------

        public async Task SaveChatMessage(
            string sessionId,
            string role,
            string content)
        {
            var chat = new ChatMessage
            {
                SessionId = sessionId,
                Role = role,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(chat);

            await _context.SaveChangesAsync();
        }

        public async Task<List<(string role, string content)>> GetConversationHistory(
            string sessionId)
        {
            var messages = await _context.ChatMessages
                .Where(x => x.SessionId == sessionId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(10)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();

            return messages
                .Select(x => (x.Role, x.Content))
                .ToList();
        }

        // --------------------------------------------------
        // PRODUCT FILTERING & SORTING
        // --------------------------------------------------

        public List<dynamic> FilterByPrice(List<dynamic> products, int minPrice, int maxPrice)
        {
            return products
                .Where(p => (int)p.price >= minPrice && (int)p.price <= maxPrice)
                .ToList();
        }

        public List<dynamic> SortByPopularity(List<dynamic> products)
        {
            return products
                .OrderByDescending(p => (int)p.total_sales)
                .ThenByDescending(p => (int)p.total_favorites)
                .ToList();
        }
    }
}