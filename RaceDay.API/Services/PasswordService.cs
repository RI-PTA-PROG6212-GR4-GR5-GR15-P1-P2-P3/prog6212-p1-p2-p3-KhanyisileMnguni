using Microsoft.AspNetCore.Identity;

namespace RaceDay.API.Services;

/// <summary>Hashes and verifies passwords with ASP.NET's salted PBKDF2 hasher. Plain text is never stored.</summary>
public class PasswordService
{
    private readonly PasswordHasher<string> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(string.Empty, password);

    public bool Verify(string hash, string password) =>
        _hasher.VerifyHashedPassword(string.Empty, hash, password) != PasswordVerificationResult.Failed;
}
