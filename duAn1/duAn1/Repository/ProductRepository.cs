        
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
                .ToList();
            return productList;

        }
        public List<Product> getProductByCategory(int idCategory)
        {
            List<Product> listProductByCategory = new List<Product>();
            listProductByCategory = _context.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == idCategory)
                .ToList();
            return listProductByCategory;
        }
        public Product getProductById(int id)
        {
            return _context.Products.Include(p => p.Category).FirstOrDefault(p => p.Id == id);
        }
    }
}
