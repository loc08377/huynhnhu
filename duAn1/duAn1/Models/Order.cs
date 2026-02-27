using System;

namespace duAn1.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string Address { get; set; }

        public DateTime CreateDate { get; set; }

        public bool Status { get; set; }

        public int UserId { get; set; }
    }
}
