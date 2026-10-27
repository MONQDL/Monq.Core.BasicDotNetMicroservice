namespace Monq.Core.BasicDotNetMicroservice.Addressing;

/// <summary>
/// Defines how a resolved endpoint must be used by a client.
/// </summary>
public enum EndpointResolutionMode
{
    /// <summary>Connect directly to the resolved address.</summary>
    Direct,

    /// <summary>Resolve and balance individual service endpoints on the client.</summary>
    ServiceDiscovery,
}
