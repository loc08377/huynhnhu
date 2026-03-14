using duAn1.Models;

namespace duAn1.Services
{
    public class CategoryService
    {
        private readonly Repository.CategoryLINQ _categoryLINQ;
        public CategoryService(Repository.CategoryLINQ categoryLINQ)
        {
            _categoryLINQ = categoryLINQ;
        }
        public List<Category> GetCategories()
        {
            try
            {
                List<Category> listCategory = new List<Category>();
                listCategory = _categoryLINQ.GetCategory();
                return listCategory;
            }
            catch (Exception)
            {
                return new List<Category>();
            }

        }
    }
}
