using System;

namespace duAn1.Models
{
    public class Cart
    {
        public int Id { get; set; }

        public bool Checked { get; set; }

        public int Quantity { get; set; }

        public int ProductId { get; set; }

        public int UserId { get; set; }

        // Navigation Properties (nếu dùng Entity Framework)
        public virtual Product Product { get; set; }

        public virtual User User { get; set; }
    }
}
