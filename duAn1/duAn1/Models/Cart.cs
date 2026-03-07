using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace duAn1.Models
{
    public class Cart
    {
        public int Id { get; set; }

        public bool Checked { get; set; }

        public int Quantity { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        [Column("create_date")]
        public DateTime? CreateDate { get; set; }

        // Navigation Properties (nếu dùng Entity Framework)
        public virtual Product Product { get; set; }

        public virtual User User { get; set; }
    }
}
