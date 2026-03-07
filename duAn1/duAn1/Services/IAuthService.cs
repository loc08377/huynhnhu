using duAn1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace duAn1.Services
{
    public interface IAuthService
    {
        string HashPassword(User user, string password);
        bool VerifyPassword(User user, string inputPassword);
        int? GetUserId(HttpContext context);

    }
}
