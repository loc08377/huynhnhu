using duAn1.Models;
using Microsoft.EntityFrameworkCore;

namespace duAn1.Services
{
    public class FavoriteService
    {
        private readonly AppDbContext _context;

        public FavoriteService(AppDbContext context)
        {
            _context = context;
        }

        // Get all favorites for a user
        public List<Product> GetFavoritesByUserId(int userId)
        {
            try
            {
                return _context.Favorites
                    .Where(f => f.UserId == userId)
                    .Include(f => f.Product)
                    .ThenInclude(p => p.Category)
                    .Select(f => f.Product)
                    .Where(p => (p.Actived ?? true))
                    .ToList();
            }
            catch (Exception)
            {
                return new List<Product>();
            }
        }

        // Check if product is favorited by user
        public bool IsFavorited(int userId, int productId)
        {
            try
            {
                return _context.Favorites
                    .Any(f => f.UserId == userId && f.ProductId == productId);
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Get count of favorites for a user
        public int GetFavoriteCount(int userId)
        {
            try
            {
                return _context.Favorites
                    .Count(f => f.UserId == userId);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        // Add to favorites
        public bool AddFavorite(int userId, int productId)
        {
            try
            {
                // Check if already favorited
                if (IsFavorited(userId, productId))
                {
                    return false;
                }

                // Check if product exists and is active
                var product = _context.Products.FirstOrDefault(p => p.Id == productId && (p.Actived ?? true));
                if (product == null)
                {
                    return false;
                }

                var favorite = new Favorite
                {
                    UserId = userId,
                    ProductId = productId,
                    LikeDate = DateTime.Now
                };

                _context.Favorites.Add(favorite);
                _context.SaveChanges();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Remove from favorites
        public bool RemoveFavorite(int userId, int productId)
        {
            try
            {
                var favorite = _context.Favorites
                    .FirstOrDefault(f => f.UserId == userId && f.ProductId == productId);

                if (favorite == null)
                {
                    return false;
                }

                _context.Favorites.Remove(favorite);
                _context.SaveChanges();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Toggle favorite
        public (bool success, bool isFavorited) ToggleFavorite(int userId, int productId)
        {
            try
            {
                if (productId <= 0)
                {
                    return (false, false);
                }

                var favorite = _context.Favorites
                    .FirstOrDefault(f => f.UserId == userId && f.ProductId == productId);

                if (favorite == null)
                {
                    return AddFavorite(userId, productId) ? (true, true) : (false, false);
                }
                else
                {
                    return RemoveFavorite(userId, productId) ? (true, false) : (false, true);
                }
            }
            catch (Exception)
            {
                return (false, false);
            }
        }
    }
}
