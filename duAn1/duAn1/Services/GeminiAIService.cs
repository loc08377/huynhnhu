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

        private const string SYSTEM_PROMPT = @"You are an AI jewelry shopping assistant for a silver jewelry e-commerce website.

Your responsibilities:
* Chat naturally with customers in Vietnamese
* Understand their product needs
* Recommend suitable silver bracelet products

Rules:
* Only recommend products from the provided product data
* Recommend maximum 5 products
* Prioritize products with higher sales and favorites
* Always answer in Vietnamese
* If the user question is unrelated to jewelry, respond: 'Tôi chỉ có thể hỗ trợ tư vấn các sản phẩm trang sức bạc.'

IMPORTANT: You MUST respond in valid JSON format exactly like this:
{
  ""message"": ""Your response text here"",
  ""recommended_products"": [
    { ""id"": 1, ""reason"": ""Why this product"" },
    { ""id"": 2, ""reason"": ""Why this product"" }
  ]
}

Only respond with JSON, no other text.";

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
        /// </summary>
        private (string? message, List<int> recommendedProductIds) GetHeuristicRecommendations(
            List<dynamic> allProducts,
            string userMessage)
        {
            try
            {
                // Sort by sales and favorites
                var recommended = allProducts
                    .OrderByDescending(p => (int)p.total_sales)
                    .ThenByDescending(p => (int)p.total_favorites)
                    .Take(5)
                    .ToList();

                var productIds = recommended
                    .Select(p => (int)p.id)
                    .ToList();

                var message = recommended.Count > 0
                    ? $"Tôi đã tìm thấy {recommended.Count} sản phẩm phù hợp nhất dựa trên đánh giá của khách hàng:"
                    : "Xin lỗi, tidak có sản phẩm phù hợp với yêu cầu của bạn.";

                return (message, productIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating heuristic recommendations");
                return ("Không thể xử lý yêu cầu. Vui lòng thử lại.", new List<int>());
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
