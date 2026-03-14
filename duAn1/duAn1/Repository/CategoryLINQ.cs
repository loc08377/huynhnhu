using duAn1.Models;

namespace duAn1.Repository
{
    public class CategoryLINQ
    {
        private readonly AppDbContext _context;
        public CategoryLINQ(AppDbContext context)
        {
            _context = context;
        }
        public List<Category> GetCategory()
        {
            List<Category> categorytList = new List<Category>();
            categorytList = _context.Categories.Where(c => c.Actived == true)
                .ToList();
            return categorytList;

        }

    }
}
