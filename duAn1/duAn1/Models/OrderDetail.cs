using System.ComponentModel.DataAnnotations.Schema;

namespace duAn1.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }

        public int Price { get; set; }

        public int Quantity { get; set; }

        [Column("order_id")]
        public int? OrderId { get; set; }

        [Column("product_id")]
        public int? ProductId { get; set; }

        // Navigation properties
        public virtual Order? Order { get; set; }
        public virtual Product? Product { get; set; }
    }
}
