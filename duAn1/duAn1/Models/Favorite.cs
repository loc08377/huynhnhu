using System.ComponentModel.DataAnnotations.Schema;

namespace duAn1.Models
{
    public class Favorite
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        [Column("product_id")]
        public int? ProductId { get; set; }

        [Column("like_date")]
        public DateTime LikeDate { get; set; } = DateTime.Now;

        // Navigation properties
        public User? User { get; set; }
        public Product? Product { get; set; }
    }
}
