using duAn1.Mappers;
using duAn1.Models;
using duAn1.Utils;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace duAn1.Repository
{
    public class UserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public User UserByEmail(string email)
        {
            return _context.Users
                .FromSqlRaw("SELECT * FROM users WHERE email = @email AND actived = 1",
                    new SqlParameter("@email", email))
                .AsEnumerable()
                .FirstOrDefault();
        }
    }
}
