using System;

namespace duAn1.Models
{
    public class Product
    {
        public int Id { get; set; }

        public bool Actived { get; set; }

        public DateTime CreatedDate { get; set; }

        public string Description { get; set; }

        public string Image { get; set; }

        public string Name { get; set; }

        public int Price { get; set; }

        public int CategoryId { get; set; }
    }
}
