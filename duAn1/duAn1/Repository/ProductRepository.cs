        
using duAn1.Models;
using Microsoft.EntityFrameworkCore;

namespace duAn1.Repository
{
    public class ProductRepository
    {
        private readonly AppDbContext _context;

        public ProductRepository(AppDbContext context)
        {
            _context = context;
        }
        public List<Product> getAllProduct()
        {
            List<Product> productList = new List<Product>();
            productList = _context.Products
                .Include(p => p.Category)
                .Where(p => (p.Actived ?? true))
                .ToList();
            return productList;

        }
        public List<Product> getProductByCategory(int idCategory)
        {
            List<Product> listProductByCategory = new List<Product>();
            listProductByCategory = _context.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == idCategory && (p.Actived ?? true))
                .ToList();
            return listProductByCategory;
        }
        public Product getProductById(int id)
        {
            return _context.Products.Include(p => p.Category).FirstOrDefault(p => p.Id == id);
        }

        public List<Product> getNewestProducts(int count = 10)
        {
            List<Product> newestProducts = new List<Product>();
            newestProducts = _context.Products
                .Include(p => p.Category)
                .Where(p => (p.Actived ?? true))
                .OrderByDescending(p => p.CreatedDate)
                .Take(count)
                .ToList();
            return newestProducts;
        }

        public List<Product> getBestSellingProducts(int count = 30)
        {
            List<Product> bestSellingProducts = new List<Product>();
            bestSellingProducts = _context.OrderDetails
                .Include(od => od.Order)
                .Where(od => od.Order.PaymentStatus == 2) // Only completed orders
                .GroupBy(od => od.ProductId)
                .Select(g => new { ProductId = g.Key, TotalQuantity = g.Sum(od => od.Quantity) })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(count)
                .Join(
                    _context.Products.Include(p => p.Category).Where(p => p.Actived ?? true),
                    x => x.ProductId,
                    p => p.Id,
                    (x, p) => p
                )
                .ToList();
            return bestSellingProducts;
        }
    }
}
