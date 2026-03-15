using System.ComponentModel.DataAnnotations.Schema;

namespace duAn1.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string? Address { get; set; }

        [Column("create_date")]
        public DateTime CreateDate { get; set; }

        public bool Status { get; set; }

        [Column("payment_status")]
        public int? PaymentStatus { get; set; }
        // 0: người dùng mới đặt hàng chờ xác nhận
        // 1: admin đã xác nhận đang giao
        // 2: người dùng đã nhận được hàng
        // 3: người dùng mới đặt hàng chưa xác nhận đã hủy
        // 4: admin không xác nhận đơn hàng

        [Column("user_id")]
        public int? UserId { get; set; }

        // Navigation property
        public virtual User? User { get; set; }
        public virtual ICollection<OrderDetail>? OrderDetails { get; set; }
    }
}
