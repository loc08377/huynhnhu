using System.Collections.Generic;

namespace duAn1.Models
{
    public class Category
    {
        public int Id { get; set; }

        public bool Actived { get; set; }

        public string Description { get; set; }

        public string Image { get; set; }

        public string Name { get; set; }

        // Navigation Property (1 Category có nhiều Product)
        public virtual ICollection<Product> Products { get; set; }
    }
}
