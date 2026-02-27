using System.Security.Cryptography;
using System.Text;

public class CryUtils
{
    private readonly IConfiguration _configuration;

    public CryUtils(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string ComputeHmac(string text)
    {
        var secretKey = _configuration["Security:SecretKey"];

        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var inputBytes = Encoding.UTF8.GetBytes(text);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(inputBytes);

        return Convert.ToBase64String(hashBytes);
    }
}