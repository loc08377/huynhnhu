using duAn1.Models;
using duAn1.Services;
using Microsoft.AspNetCore.Identity;

public class AuthService : IAuthService
{
    private readonly PasswordHasher<User> _hasher;
    private readonly CryUtils _cryUtils;

    public AuthService(CryUtils cryUtils)
    {
        _hasher = new PasswordHasher<User>();
        _cryUtils = cryUtils;
    }

    public string HashPassword(User user, string password)
    {
        var hmacPassword = _cryUtils.ComputeHmac(password);
        return _hasher.HashPassword(user, hmacPassword);
    }

    public bool VerifyPassword(User user, string password)
    {
        try
        {
            var hmacPassword = _cryUtils.ComputeHmac(password);

            var result = _hasher.VerifyHashedPassword(
                user,
                user.Password,
                hmacPassword
            );
            return result == PasswordVerificationResult.Success;
        }
        catch (Exception)
        {
            return false;
        }
    }
    public bool IsLoggedIn(HttpContext httpContext)
    {
        if (httpContext == null)
            return false;

        if (httpContext.User == null)
            return false;

        if (httpContext.User.Identity == null || !httpContext.User.Identity.IsAuthenticated)
            return false;

        var userId = httpContext.User.FindFirst("UserId")?.Value;

        if (string.IsNullOrEmpty(userId))
            return false;

        return true;
    }
    public int? GetUserId(HttpContext httpContext)
    {
        if (httpContext == null)
            return null;

        if (httpContext.User == null)
            return null;

        if (httpContext.User.Identity == null || !httpContext.User.Identity.IsAuthenticated)
            return null;

        var userId = httpContext.User.FindFirst("UserId")?.Value;

        if (string.IsNullOrEmpty(userId))
            return null;

        return int.Parse(userId);
    }
}