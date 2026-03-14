using duAn1.Models;
using Microsoft.EntityFrameworkCore;

namespace duAn1.Repository
{
    public class CartLINQ
    {
        private readonly AppDbContext _context;

        public CartLINQ(AppDbContext context)
        {
            _context = context;
        }

        public void InsertCart(Cart cart)
        {
            _context.Carts.Add(cart);
            _context.SaveChanges();
        }
        public Cart GetCartByProductIdAndUserId(int productId, int? userId)
        {
            return _context.Carts
                .FirstOrDefault(c => c.ProductId == productId && c.UserId == userId);
        }
        public int GetCartCount(int userId)
        {
            return _context.Carts
                .Where(c => c.UserId == userId)
                .Count();
        }
        public List<Cart> GetCartsByUserId(int? userId)
        {
            return _context.Carts
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .ToList();
        }

        public bool UpdateQuantity(int cartId, int quantity)
        {
            var cart = _context.Carts.FirstOrDefault(c => c.Id == cartId);
            if (cart == null) return false;
            cart.Quantity = quantity;
            _context.SaveChanges();
            return true;
        }
        public bool RemoveCartItem(int cartId)
        {
            var cart = _context.Carts.FirstOrDefault(c => c.Id == cartId);
            if (cart == null) return false;
            _context.Carts.Remove(cart);
            _context.SaveChanges();
            return true;
        }
    }
}
