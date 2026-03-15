using duAn1.Models;
using System.Text.Json;

namespace duAn1.Services
{
    public class AIRecommendationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AIRecommendationService> _logger;

        public AIRecommendationService(AppDbContext context, ILogger<AIRecommendationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<dynamic>> GetRecommendedProducts(List<dynamic> filteredProducts)
        {
            try
            {
                // For now, return top 5 products by sales and favorites
                // This is where you would integrate with OpenAI, Claude, etc.
                // Example: Use ChatGPT API to analyze user request and select best products
                
                var recommended = filteredProducts
                    .OrderByDescending(p => (int)p.total_sales)
                    .ThenByDescending(p => (int)p.total_favorites)
                    .Take(5)
                    .ToList();

                return recommended;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommended products");
                throw;
            }
        }

        public async Task<List<dynamic>> FilterProductsByPrice(int minPrice, int maxPrice)
        {
            try
            {
                var products = _context.Products
                    .Where(p => (p.Actived ?? false) && p.Price >= minPrice && p.Price <= maxPrice)
                    .AsEnumerable()
                    .Select(p => new
                    {
                        id = p.Id,
                        name = p.Name,
                        price = p.Price,
                        description = p.Description,
                        image = p.Image,
                        category_name = _context.Categories.FirstOrDefault(c => c.Id == p.CategoryId)?.Name ?? "N/A",
                        total_sales = _context.OrderDetails.Count(od => od.ProductId == p.Id),
                        total_favorites = _context.Favorites.Count(f => f.ProductId == p.Id),
                    })
                    .ToList<dynamic>();

                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering products by price");
                throw;
            }
        }

        public async Task<List<dynamic>> FilterProductsByCategory(string categoryName)
        {
            try
            {
                var category = _context.Categories.FirstOrDefault(c => c.Name.Contains(categoryName));
                if (category == null)
                {
                    return new List<dynamic>();
                }

                var products = _context.Products
                    .Where(p => (p.Actived ?? false) && p.CategoryId == category.Id)
                    .AsEnumerable()
                    .Select(p => new
                    {
                        id = p.Id,
                        name = p.Name,
                        price = p.Price,
                        description = p.Description,
                        image = p.Image,
                        category_name = category.Name,
                        total_sales = _context.OrderDetails.Count(od => od.ProductId == p.Id),
                        total_favorites = _context.Favorites.Count(f => f.ProductId == p.Id)
                    })
                    .ToList<dynamic>();

                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering products by category");
                throw;
            }
        }

        public List<dynamic> ExtractProductFilters(string userMessage)
        {
            try
            {
                var allProducts = _context.Products
                    .Where(p => (p.Actived ?? false))
                    .AsEnumerable()
                    .Select(p => new
                    {
                        id = p.Id,
                        name = p.Name,
                        price = p.Price,
                        description = p.Description,
                        image = p.Image,
                        category_name = _context.Categories.FirstOrDefault(c => c.Id == p.CategoryId)?.Name ?? "N/A",
                        total_sales = _context.OrderDetails.Count(od => od.ProductId == p.Id),
                        total_favorites = _context.Favorites.Count(f => f.ProductId == p.Id)
                    })
                    .ToList<dynamic>();

                // Extract price range from message
                // Example patterns: "từ 200k đến 500k", "200k - 500k", "giá từ 200000 đến 500000"
                var priceMatch = System.Text.RegularExpressions.Regex.Match(
                    userMessage,
                    @"(?:từ|giá)\s*(\d+)(?:k|000)?\s*(?:đến|-)?\s*(\d+)(?:k|000)?",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );

                if (priceMatch.Success)
                {
                    int minPrice = int.Parse(priceMatch.Groups[1].Value) * (priceMatch.Groups[1].Value.Length <= 3 ? 1000 : 1);
                    int maxPrice = int.Parse(priceMatch.Groups[2].Value) * (priceMatch.Groups[2].Value.Length <= 3 ? 1000 : 1);

                    return allProducts
                        .Where(p => p.price >= minPrice && p.price <= maxPrice)
                        .ToList();
                }

                // If no price filter, return all products
                return allProducts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting product filters");
                return new List<dynamic>();
            }
        }
    }
}
