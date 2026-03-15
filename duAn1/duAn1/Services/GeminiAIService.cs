using duAn1.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Net.Http.Json;

namespace duAn1.Services
{
    public class GeminiAIService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<GeminiAIService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        private const string SYSTEM_PROMPT = @"You are AI Stylist - a friendly and helpful jewelry fashion advisor for a silver bracelet e-commerce platform.

YOUR PERSONALITY:
* Friendly, warm, and conversational like a real fashion consultant
* Passionate about silver jewelry and styling
* Always respond in Vietnamese
* Be concise but engaging

YOUR CAPABILITIES:
1. **Greetings**: Respond warmly to hellos, goodbyes, thank yous
2. **New Products**: When asked about new items, highlight best-selling or recently added bracelets
3. **Price-based Recommendations**: When user mentions a budget, recommend products within that price range
4. **Style Advice**: Suggest products based on user preferences (casual, formal, trendy, classic, etc.)
5. **Product Info**: Answer questions about materials, sizes, prices
6. **General Fashion Chat**: Engage in light conversation about jewelry trends

OUT-OF-DOMAIN RESPONSE:
If user asks about math, programming, cooking, politics, or anything NOT related to jewelry/fashion:
Respond: 'Xin lỗi, tôi chỉ có thể tư vấn về trang sức bạc. Hãy hỏi tôi về các sản phẩm vòng tay bạc, kiểu dáng, giá cả hoặc bất kỳ điều gì liên quan đến thời trang!'

IMPORTANT RESPONSE FORMAT - You MUST respond in valid JSON:
{
  ""message"": ""Your conversational response here"",
  ""recommended_products"": [
    { ""id"": 1, ""reason"": ""Why this product fits their needs"" },
    { ""id"": 2, ""reason"": ""Why this product fits their needs"" }
  ]
}

KEY RULES:
* Recommend max 5 products
* Only recommend from provided product data
* Prioritize by sales and favorites when relevant
* Recommend products ONLY if relevant to user's request
* In case of greeting (hello, hi, xin chào): respond warmly, recommend_products can be empty
* Always be natural and conversational, not like a search tool

SAMPLE INTERACTIONS:
User: 'Hello!' → Me: 'Xin chào! Tôi là AI Stylist, rất vui được tư vấn cho bạn. Hôm nay bạn đang tìm kiếm loại vòng tay bạc nào?'
User: 'có hàng mới không?' → Me: 'Có chứ! Hôm nay chúng tôi có những mẫu vòng tay bạc...', recommend top new items
User: 'tôi muốn vòng 500k' → Me: '500k là ngân sách tốt! Tôi có những lựa chọn tuyệt vời...', recommend in that price range
User: '2+2 bằng mấy?' → Me: 'Xin lỗi, tôi chỉ có thể...' (out of domain)";

        public GeminiAIService(AppDbContext context, ILogger<GeminiAIService> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Get all active products from database with sales and favorites count
        /// </summary>
        public async Task<List<dynamic>> GetAllActiveProducts()
        {
            try
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
                            .FirstOrDefault() ?? "N/A",
                        total_sales = _context.OrderDetails.Count(od => od.ProductId == p.Id),
                        total_favorites = _context.Favorites.Count(f => f.ProductId == p.Id)
                    })
                    .ToListAsync();

                return products.Cast<dynamic>().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching active products");
                throw;
            }
        }

        /// <summary>
        /// Extract price range from user message using regex
        /// Example: "từ 200k đến 500k" → (200000, 500000)
        /// </summary>
        public (int minPrice, int maxPrice)? ExtractPriceRange(string message)
        {
            try
            {
                var priceMatch = System.Text.RegularExpressions.Regex.Match(
                    message,
                    @"(?:từ|giá)\s*(\d+)(?:k|000)?\s*(?:đến|tới|-)?\s*(\d+)(?:k|000)?",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );

                if (priceMatch.Success)
                {
                    int minPrice = int.Parse(priceMatch.Groups[1].Value);
                    int maxPrice = int.Parse(priceMatch.Groups[2].Value);

                    // Multiply by 1000 if less than 4 digits (assuming "k" means thousand)
                    if (minPrice < 1000) minPrice *= 1000;
                    if (maxPrice < 1000) maxPrice *= 1000;

                    return (minPrice, maxPrice);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting price range");
                return null;
            }
        }

        /// <summary>
        /// Filter products by price range
        /// </summary>
        public List<dynamic> FilterByPrice(List<dynamic> products, int minPrice, int maxPrice)
        {
            return products
                .Where(p => ((int)p.price >= minPrice && (int)p.price <= maxPrice))
                .ToList();
        }

        /// <summary>
        /// Filter products by category keyword
        /// </summary>
        public List<dynamic> FilterByCategory(List<dynamic> products, string categoryKeyword)
        {
            return products
                .Where(p => ((string)p.category_name).Contains(categoryKeyword, StringComparison.OrdinalIgnoreCase) ||
                           ((string)p.name).Contains(categoryKeyword, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Sort products by sales and favorites (popularity)
        /// </summary>
        public List<dynamic> SortByPopularity(List<dynamic> products)
        {
            return products
                .OrderByDescending(p => (int)p.total_sales)
                .ThenByDescending(p => (int)p.total_favorites)
                .ToList();
        }

        /// <summary>
        /// Generate AI recommendations with fallback to heuristic system
        /// </summary>
        public async Task<(string? message, List<int> recommendedProductIds)> GetAIRecommendationsWithFallback(
            List<dynamic> allProducts,
            string userMessage,
            List<(string role, string content)> conversationHistory)
        {
            try
            {
                var apiKey = _configuration["Gemini:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException("Gemini API key not configured");
                }

                // Build conversation messages for context
                var messages = new List<object>();

                // Add conversation history
                foreach (var (role, content) in conversationHistory)
                {
                    messages.Add(new { role, parts = new[] { new { text = content } } });
                }

                // Prepare products data for AI
                var productsJson = JsonSerializer.Serialize(allProducts.Take(20).ToList()); // Limit to top 20 to avoid token limits

                // Create the full prompt
                var fullPrompt = $@"Available products data:
{productsJson}

User message: {userMessage}";

                // Prepare request to Gemini API
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new[] { new { text = fullPrompt } }
                        }
                    },
                    systemInstruction = new
                    {
                        parts = new[] { new { text = SYSTEM_PROMPT } }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 1024,
                        topP = 0.9,
                        topK = 40
                    }
                };

                var url = $"https://generativelanguage.googleapis.com/v1/models/gemini-pro:generateContent?key={apiKey}";
                var response = await _httpClient.PostAsJsonAsync(url, requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Gemini API error: {response.StatusCode} - {errorContent}");
                    throw new HttpRequestException($"Gemini API failed: {response.StatusCode}");
                }

                var responseText = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseText);
                var textContent = result
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                // Parse AI response as JSON
                var aiResponse = JsonSerializer.Deserialize<JsonElement>(textContent ?? "{}");
                var aiMessage = aiResponse.TryGetProperty("message", out var msgProp) 
                    ? msgProp.GetString() 
                    : "Không thể xử lý yêu cầu";
                
                var recommendedIds = new List<int>();
                if (aiResponse.TryGetProperty("recommended_products", out var productsArray))
                {
                    foreach (var product in productsArray.EnumerateArray())
                    {
                        if (product.TryGetProperty("id", out var idElement))
                        {
                            recommendedIds.Add(idElement.GetInt32());
                        }
                    }
                }

                return (aiMessage, recommendedIds);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini AI failed, falling back to heuristic recommendations");
                // Fall back to heuristic recommendations
                return GetHeuristicRecommendations(allProducts, userMessage);
            }
        }

        /// <summary>
        /// Heuristic recommendations when Gemini API is unavailable
        /// Uses intelligent pattern matching for greetings, price queries, and product recommendations
        /// </summary>
        private (string? message, List<int> recommendedProductIds) GetHeuristicRecommendations(
            List<dynamic> allProducts,
            string userMessage)
        {
            try
            {
                var lowerMessage = userMessage.ToLower().Trim();
                
                // 1. GREETING - just chat, don't recommend yet
                if (lowerMessage == "j" || lowerMessage.Contains("hello") || lowerMessage.Contains("hi") || 
                    lowerMessage.Contains("xin chào") || lowerMessage.Contains("chào") || 
                    lowerMessage.Contains("hey") || lowerMessage == "chào")
                {
                    var greetings = new[] {
                        "Xin chào! 👋 Tôi là AI Stylist của bạn. Rất vui được gặp bạn! Bạn đang tìm kiếm vòng tay bạc nào hôm nay? Tôi có thể giúp bạn chọn một mẫu phù hợp nhất.",
                        "Hey! Chào bạn! 😊 Tôi đây, AI Stylist của bạn. Hôm nay bạn muốn tìm vòng tay bạc theo kiểu gì? Kiểu cổ điển, hiện đại, hay trendy?",
                        "Xin chào bạn! 💎 Tôi là trợ lý tư vấn thời trang của bạn. Hôm nay chúng ta hãy tìm chiếc vòng tay bạc hoàn hảo cho bạn nhé!"
                    };
                    var randomGreeting = greetings[new Random().Next(greetings.Length)];
                    return (randomGreeting, new List<int>());
                }

                // 2. ASKING WHICH MODEL / NEEDS HELP CHOOSING
                if (lowerMessage.Contains("mẫu nào") || lowerMessage.Contains("cái nào") || lowerMessage.Contains("loại nào") || 
                    lowerMessage.Contains("chọn") || lowerMessage.Contains("giúp") || lowerMessage.Contains("lựa chọn") ||
                    lowerMessage.Contains("nên mua") || lowerMessage.Contains("ưu tiên"))
                {
                    // Ask for preference without recommending yet
                    var responses = new[] {
                        "Tất nhiên! Để tôi tìm cho bạn mẫu phù hợp nhất. Bạn thích kiểu nào: kiểu đơn giản và thanh lịch, hay kiểu sáng tạo và nổi bật? Và bạn có ngân sách bao nhiêu không?",
                        "Great! 😊 Hãy cho tôi biết: bạn muốn mang dịp nào (hằng ngày, dự tiệc, hay formal)? Và bạn định chi bao nhiêu tiền?",
                        "Chắc chắn rồi! Mình hãy tìm cùng nhau. Bạn thích vòng tay có đá quý không? Và ngân sách của bạn là bao nhiêu nè?"
                    };
                    var response = responses[new Random().Next(responses.Length)];
                    return (response, new List<int>());
                }

                // 3. PRICE MENTION - extract and recommend
                var priceRange = ExtractPriceRange(userMessage);
                if (priceRange.HasValue)
                {
                    var filtered = allProducts
                        .Where(p => (int)p.price >= priceRange.Value.minPrice && (int)p.price <= priceRange.Value.maxPrice)
                        .OrderByDescending(x => (int)x.total_sales)
                        .ThenByDescending(x => (int)x.total_favorites)
                        .Take(5)
                        .ToList();

                    if (filtered.Count > 0)
                    {
                        var productIds = filtered.Select(p => (int)p.id).ToList();
                        var message = $"Perfect! 💎 Với ngân sách {priceRange.Value.minPrice:N0} - {priceRange.Value.maxPrice:N0} đồng, tôi có {productIds.Count} mẫu vòng tay bạc đẹp lắm cho bạn:";
                        return (message, productIds);
                    }
                    else
                    {
                        return ($"Xin lỗi, hiện tại chúng tôi không có mẫu nào trong khoảng giá {priceRange.Value.minPrice:N0} - {priceRange.Value.maxPrice:N0} đồng. Hãy thử giá khác nhé!", new List<int>());
                    }
                }

                // 4. NEW PRODUCTS
                if (lowerMessage.Contains("hàng mới") || lowerMessage.Contains("sản phẩm mới") || lowerMessage.Contains("new") || 
                    lowerMessage.Contains("mới nhất") || lowerMessage.Contains("hàng chưa") || lowerMessage.Contains("mới không"))
                {
                    var newProducts = allProducts
                        .OrderByDescending(p => (int)p.total_favorites)
                        .ThenByDescending(p => (int)p.total_sales)
                        .Take(5)
                        .ToList();

                    if (newProducts.Count > 0)
                    {
                        var productIds = newProducts.Select(p => (int)p.id).ToList();
                        return ($"Hôm nay chúng tôi có những mẫu vòng tay bạc được yêu thích nhất! Tôi gợi ý {productIds.Count} mẫu cho bạn:", productIds);
                    }
                    else
                    {
                        return ("Xin lỗi, hiện tại chúng tôi không có sản phẩm nào.", new List<int>());
                    }
                }

                // 5. OUT OF DOMAIN (math, programming, cooking, etc)
                if (lowerMessage.Contains("toán") || lowerMessage.Contains("lập trình") || lowerMessage.Contains("+") || 
                    lowerMessage.Contains("code") || lowerMessage.Contains("nấu") || lowerMessage.Contains("chính trị") ||
                    lowerMessage.Contains("weather") || lowerMessage.Contains("thời tiết"))
                {
                    return ("Xin lỗi nhé! 😅 Tôi chỉ là AI Stylist, chuyên tư vấn về trang sức bạc thôi. Hãy hỏi tôi về vòng tay bạc, kiểu dáng, giá cả hoặc bất kỳ điều gì liên quan đến thời trang nhé!", new List<int>());
                }

                // 6. DEFAULT - recommend best-sellers
                var recommended = allProducts
                    .OrderByDescending(p => (int)p.total_sales)
                    .ThenByDescending(p => (int)p.total_favorites)
                    .Take(5)
                    .ToList();

                if (recommended.Count > 0)
                {
                    var productIds = recommended.Select(p => (int)p.id).ToList();
                    var defaultMessage = "Không rõ ý của bạn, nhưng tôi có thể gợi ý những mẫu vòng tay bạc bán chạy nhất hiện nay. Nếu bạn có yêu cầu cụ thể (kiểu dáng, giá cả), hãy cho tôi biết nhé! 💎";
                    return (defaultMessage, productIds);
                }
                else
                {
                    return ("Hiện tại chúng tôi không có sản phẩm nào. Vui lòng quay lại sau!", new List<int>());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating heuristic recommendations");
                return ("Xin lỗi, tôi gặp một chút sự cố. Vui lòng thử lại sau!", new List<int>());
            }
        }

        /// <summary>
        /// Save chat message to database for conversation history
        /// NOTE: ChatMessage table not in current database schema - this is a no-op
        /// </summary>
        public async Task SaveChatMessage(string sessionId, string role, string content, List<int>? recommendedProductIds = null)
        {
            // ChatMessage table not in database - no-op
            // To enable conversation history, create ChatMessage table via EF migration
            // and uncomment the code below:
            
            /*
            try
            {
                var chatMessage = new ChatMessage
                {
                    SessionId = sessionId,
                    Role = role,
                    Content = content,
                    RecommendedProducts = recommendedProductIds?.Count > 0 
                        ? JsonSerializer.Serialize(recommendedProductIds) 
                        : null,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ChatMessages.Add(chatMessage);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving chat message");
                // Don't throw - logging failure shouldn't break the chat
            }
            */
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Get conversation history from database
        /// NOTE: ChatMessage table not in current database schema - returns empty list
        /// </summary>
        public async Task<List<(string role, string content)>> GetConversationHistory(string sessionId, int limit = 10)
        {
            // ChatMessage table not in database - return empty list
            // To enable conversation history, create ChatMessage table via EF migration
            return await Task.FromResult(new List<(string, string)>());
            
            /*
            try
            {
                var messages = await _context.ChatMessages
                    .AsNoTracking()
                    .Where(m => m.SessionId == sessionId)
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(limit)
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync();

                return messages
                    .Select(m => (m.Role, m.Content))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading conversation history");
                return new List<(string, string)>();
            }
            */
        }
    }
}
