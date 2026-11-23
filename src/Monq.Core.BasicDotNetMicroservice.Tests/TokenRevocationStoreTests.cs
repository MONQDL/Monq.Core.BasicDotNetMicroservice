using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Monq.Core.BasicDotNetMicroservice.Authentication;
using Monq.Core.BasicDotNetMicroservice.Extensions;
using Xunit;

namespace Monq.Core.BasicDotNetMicroservice.Tests;

public class TokenRevocationStoreTests
{
    [Fact(DisplayName = "Reject a non-positive revocation retention period.")]
    public void ShouldRejectInvalidRetention()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => services.AddMonqTokenRevocation(TimeSpan.Zero));
    }

    [Fact(DisplayName = "Register the token revocation store as a singleton.")]
    public void ShouldRegisterSingletonStore()
    {
        var services = new ServiceCollection();
        services.AddMonqTokenRevocation(TimeSpan.FromMinutes(5));

        using var provider = services.BuildServiceProvider();

        Assert.Same(
            provider.GetRequiredService<ITokenRevocationStore>(),
            provider.GetRequiredService<ITokenRevocationStore>());
    }

    [Fact(DisplayName = "Reject a token when its persisted grant key is revoked.")]
    public void ShouldRejectRevokedToken()
    {
        var services = new ServiceCollection();
        services.AddMonqTokenRevocation(TimeSpan.FromMinutes(5));

        using var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<ITokenRevocationStore>();
        const string token = "reference-token";

        store.Revoke(CreateReferenceTokenGrantKey(token));

        Assert.True(store.IsRevoked(token));
    }

    [Fact(DisplayName = "Keep an unrelated token valid after another token is revoked.")]
    public void ShouldKeepUnrelatedTokenValid()
    {
        var services = new ServiceCollection();
        services.AddMonqTokenRevocation(TimeSpan.FromMinutes(5));

        using var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<ITokenRevocationStore>();

        store.Revoke(CreateReferenceTokenGrantKey("revoked-token"));

        Assert.False(store.IsRevoked("active-token"));
    }

    [Fact(DisplayName = "Allow a token after its revocation retention period expires.")]
    public void ShouldAllowExpiredRevocation()
    {
        var timeProvider = new TestTimeProvider(DateTimeOffset.UtcNow);
        var store = new TokenRevocationStore(timeProvider, TimeSpan.FromMinutes(5));
        const string token = "reference-token";

        store.Revoke(CreateReferenceTokenGrantKey(token));
        timeProvider.Advance(TimeSpan.FromMinutes(5));

        Assert.False(store.IsRevoked(token));
    }

    static string CreateReferenceTokenGrantKey(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{token}:reference_token"));
        return Convert.ToBase64String(bytes);
    }

    sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        DateTimeOffset currentUtcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => currentUtcNow;

        public void Advance(TimeSpan duration) => currentUtcNow += duration;
    }
}
