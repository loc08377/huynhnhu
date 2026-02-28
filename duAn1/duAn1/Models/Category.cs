using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace duAn1.Models
{
    [Table("categories")]
    public class Category
    {
        public int Id { get; set; }

        public bool Actived { get; set; }

        public string Description { get; set; }

        public string Image { get; set; }

        public string Name { get; set; }

        // Navigation Property (1 Category có nhiều Product)
        public ICollection<Product> Products { get; set; }
    }
}
