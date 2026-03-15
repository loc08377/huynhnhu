using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace duAn1.Models
{
    [Table("chatMessages")]
    public class ChatMessage
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        [Column("role")]
        public string Role { get; set; } // "user" or "assistant"

        [Column("content")]
        public string Content { get; set; }

        [Column("recommended_products")]
        public string? RecommendedProducts { get; set; } // JSON array of product IDs

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("session_id")]
        public string? SessionId { get; set; } // For tracking conversation sessions
    }
}
