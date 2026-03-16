using duAn1.Models;
using duAn1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace duAn1.Controllers
{
    public class StylistController : Controller
    {
        private readonly GeminiAIService _geminiService;
        private readonly ILogger<StylistController> _logger;
        private readonly AppDbContext _context;

        public StylistController(
            GeminiAIService geminiService,
            ILogger<StylistController> logger,
            AppDbContext context)
        {
            _geminiService = geminiService;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Display the AI Stylist chat interface
        /// </summary>
        [Route("stylist")]
        public IActionResult Index()
        {
            return View("~/Views/Stylist/Index.cshtml");
        }

        /// <summary>
        /// API endpoint to handle chat messages with Gemini AI
        /// </summary>
        [Route("api/stylist/chat")]
        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.UserMessage))
                {
                    return Json(new { success = false, message = "Vui lòng nhập tin nhắn" });
                }

                // Validate message length (100-150 characters)
                var messageLength = request.UserMessage.Trim().Length;
                if (messageLength < 1)
                {
                    return Json(new { 
                        success = false, 
                        message = $"Đoạn chat quá ngắn" 
                    });
                }

                if (messageLength > 150)
                {
                    return Json(new { 
                        success = false, 
                        message = $"Tin nhắn quá dài! Tối đa 150 kí tự (hiện tại: {messageLength}/150)" 
                    });
                }

                // Get or create session ID
                var sessionId = request.SessionId;
                if (string.IsNullOrEmpty(sessionId))
                {
                    sessionId = Guid.NewGuid().ToString();
                }

                // Step 1: Get all active products from database
                var allProducts = await _geminiService.GetAllActiveProducts();

                if (allProducts.Count == 0)
                {
                    return Json(new
                    {
                        success = true,
                        sessionId = sessionId,
                        message = "Xin lỗi, hiện tại không có sản phẩm nào trong kho.",
                        products = new List<object>(),
                        recommendedProductIds = new List<int>()
                    });
                }

                // Step 2: Try to filter products by price range if user mentions it
                var priceRange = _geminiService.ExtractPriceRange(request.UserMessage);
                List<dynamic> filteredProducts = allProducts;

                if (priceRange.HasValue)
                {
                    filteredProducts = _geminiService.FilterByPrice(
                        filteredProducts,
                        priceRange.Value.min,
                        priceRange.Value.max
                    );
                }

                // Step 3: Sort by popularity
                filteredProducts = _geminiService.SortByPopularity(filteredProducts);

                // Step 4: Get conversation history
                var conversationHistory = await _geminiService.GetConversationHistory(sessionId);

                // Step 5: Call Gemini AI to get recommendations
                var (aiMessage, recommendedProductIds) = await _geminiService.AskAI(
                    request.UserMessage,
                    sessionId
                );

                // Step 6: Save messages to conversation history
                await _geminiService.SaveChatMessage(sessionId, "user", request.UserMessage ?? "");
                if (!string.IsNullOrEmpty(aiMessage))
                {
                    await _geminiService.SaveChatMessage(sessionId, "assistant", aiMessage);
                }

                // Step 7: Get product details for recommended products
                var products = new List<dynamic>();
                if (recommendedProductIds.Count > 0)
                {
                    products = filteredProducts
                        .Where(p => recommendedProductIds.Contains((int)p.id))
                        .Cast<dynamic>()
                        .ToList();
                }

                return Json(new
                {
                    success = true,
                    sessionId = sessionId,
                    message = aiMessage,
                    products = products,
                    recommendedProductIds = recommendedProductIds
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in chat endpoint");
                return Json(new
                {
                    success = false,
                    message = $"Lỗi: {ex.Message}"
                });
            }
        }

        /*
        /// <summary>
        /// Get conversation history for current session
        /// NOTE: Disabled - ChatMessage table not in database schema
        /// </summary>
        [Route("api/stylist/history")]
        [HttpGet]
        public async Task<IActionResult> GetHistory(string sessionId, int limit = 10)
        {
            try
            {
                if (string.IsNullOrEmpty(sessionId))
                {
                    return Json(new { success = false, message = "Session ID required" });
                }

                // ChatMessage table not in current database
                // This endpoint returns empty history
                return Json(new { success = true, messages = new List<object>() });
                
                // Original code commented out:
                // var history = await _context.ChatMessages
                //     .AsNoTracking()
                //     .Where(m => m.SessionId == sessionId)
                //     .OrderBy(m => m.CreatedAt)
                //     .Take(limit)
                //     .Select(m => new
                //     {
                //         role = m.Role,
                //         content = m.Content,
                //         createdAt = m.CreatedAt
                //     })
                //     .ToListAsync();

                // return Json(new { success = true, messages = history });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat history");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Clear conversation history
        /// NOTE: Disabled - ChatMessage table not in database schema
        /// </summary>
        [Route("api/stylist/clear")]
        [HttpPost]
        public async Task<IActionResult> ClearHistory(string sessionId)
        {
            try
            {
                if (string.IsNullOrEmpty(sessionId))
                {
                    return Json(new { success = false, message = "Session ID required" });
                }

                // ChatMessage table not in current database
                // This is a no-op endpoint
                return Json(new { success = true, message = "Lịch sử được xóa" });
                
                // Original code commented out:
                // var messages = await _context.ChatMessages
                //     .Where(m => m.SessionId == sessionId)
                //     .ToListAsync();

                // _context.ChatMessages.RemoveRange(messages);
                // await _context.SaveChangesAsync();

                // return Json(new { success = true, message = "Lịch sử được xóa" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing history");
                return Json(new { success = false, message = ex.Message });
            }
        }
        */
    }

    public class ChatRequest
    {
        public string? UserMessage { get; set; }
        public string? SessionId { get; set; }
    }
}
