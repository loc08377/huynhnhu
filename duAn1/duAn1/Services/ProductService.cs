using duAn1.Models;

namespace duAn1.Services
{
    public class ProductService
    {
        private readonly Repository.ProductRepository _productRepository;
        public ProductService(Repository.ProductRepository productRepository)
        {
            _productRepository = productRepository;
        }
        public List<Product> GetProducts()
        {
            try
            {
                List<Product> listProduct = new List<Product>();
                listProduct = _productRepository.getAllProduct();
                return listProduct;
            }
            catch (Exception ex)
            {
                return new List<Product>();
            }
            
        }
    }
}
