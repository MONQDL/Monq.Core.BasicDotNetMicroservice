using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Monq.Core.BasicDotNetMicroservice.Authentication;

/// <summary>
/// Keeps recently revoked reference-token keys on the current service instance.
/// </summary>
public interface ITokenRevocationStore
{
    /// <summary>
    /// Adds a persisted grant key to the local revocation set.
    /// </summary>
    void Revoke(string grantKey);

    /// <summary>
    /// Determines whether the raw token maps to a recently revoked persisted grant.
    /// </summary>
    bool IsRevoked(string token);
}

sealed class TokenRevocationStore(TimeProvider timeProvider, TimeSpan retention) : ITokenRevocationStore
{
    const string ReferenceTokenGrantType = "reference_token";
    readonly ConcurrentDictionary<string, DateTimeOffset> revokedKeys = new(StringComparer.Ordinal);

    public void Revoke(string grantKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(grantKey);
        revokedKeys[grantKey] = timeProvider.GetUtcNow().Add(retention);
    }

    public bool IsRevoked(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var grantKey = CreateGrantKey(token);
        if (!revokedKeys.TryGetValue(grantKey, out var expiresAt))
            return false;

        if (expiresAt > timeProvider.GetUtcNow())
            return true;

        revokedKeys.TryRemove(grantKey, out _);
        return false;
    }

    static string CreateGrantKey(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{token}:{ReferenceTokenGrantType}"));
        return Convert.ToBase64String(bytes);
    }
}
