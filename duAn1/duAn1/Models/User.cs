using System;

namespace duAn1.Models
{
    public class User
    {
        public int Id { get; set; }

        public bool Actived { get; set; }

        public string? Avatar { get; set; }

        public string Email { get; set; }

        public string? FullName { get; set; }

        public string Password { get; set; }

        public int Role { get; set; }
    }
}
