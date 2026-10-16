using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace RaceDay.API.Services;

public class JwtSettings
{
    public string Key { get; set; } = null!;      // at least 32 characters
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public int ExpiryMinutes { get; set; } = 120;
}

public record JwtToken(string Token, DateTime ExpiresAtUtc);

/// <summary>Creates signed JWTs carrying the user's ID, name, email and role.</summary>
public class JwtService(IOptions<JwtSettings> options)
{
    private readonly JwtSettings _settings = options.Value;

    public JwtToken CreateToken(int userId, string name, string email, string role)
    {
        var expires = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            }),
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key)),
                SecurityAlgorithms.HmacSha256)
        };

        return new JwtToken(new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }
}