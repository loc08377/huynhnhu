using duAn1.Models;

namespace duAn1.Services
{
    public interface IAuthService
    {
        string HashPassword(User user, string password);
        bool VerifyPassword(User user, string inputPassword);
        int? GetUserId(HttpContext context);

    }
}
