using duAn1.Models;
using duAn1.Services;
using duAn1.Utils;
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
        catch(Exception e)
        {
            return false;
        }
        

       
    }
}