using System.Collections.Concurrent;

namespace RaceDay.API.Services;

/// <summary>
/// Remembers the IDs (jti) of tokens that have been logged out, so the API rejects them even though they
/// have not expired yet. Entries are dropped once the token would have expired anyway.
/// Held in memory: restarting the API forgets it, which is fine for this assignment.
/// </summary>
public class TokenRevocationList
{
    private readonly ConcurrentDictionary<string, DateTime> _revoked = new();

    public void Revoke(string? tokenId, DateTime expiresUtc)
    {
        if (string.IsNullOrEmpty(tokenId)) return;

        _revoked[tokenId] = expiresUtc;

        foreach (var expired in _revoked.Where(kv => kv.Value < DateTime.UtcNow).Select(kv => kv.Key).ToList())
            _revoked.TryRemove(expired, out _);
    }

    public bool IsRevoked(string? tokenId) => tokenId is not null && _revoked.ContainsKey(tokenId);
}
