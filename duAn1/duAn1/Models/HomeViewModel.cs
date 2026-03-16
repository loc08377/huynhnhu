namespace duAn1.Models
{
    public class HomeViewModel
    {
        public List<Product> NewestProducts { get; set; } = new List<Product>();
        public List<Product> BestSellingProducts { get; set; } = new List<Product>();
    }
}
