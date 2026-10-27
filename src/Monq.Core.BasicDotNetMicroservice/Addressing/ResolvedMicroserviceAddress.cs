namespace Monq.Core.BasicDotNetMicroservice.Addressing;

/// <summary>
/// Contains the resolved address and endpoint resolution mode.
/// </summary>
/// <param name="Address">Resolved absolute address.</param>
/// <param name="ResolutionMode">How the endpoint must be used by the client.</param>
public sealed record ResolvedMicroserviceAddress(
    Uri Address,
    EndpointResolutionMode ResolutionMode = EndpointResolutionMode.Direct);
