namespace Monq.Core.BasicDotNetMicroservice.Configuration;

/// <summary>
/// Options used to configure gRPC client-side service discovery.
/// </summary>
public sealed class GrpcServiceDiscoveryOptions
{
    /// <summary>
    /// Gets or sets the interval for periodically refreshing DNS addresses.
    /// </summary>
    public TimeSpan DnsRefreshInterval { get; set; } = TimeSpan.FromSeconds(30);
}
