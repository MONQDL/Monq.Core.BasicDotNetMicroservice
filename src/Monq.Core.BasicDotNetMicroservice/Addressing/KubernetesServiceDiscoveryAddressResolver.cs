namespace Monq.Core.BasicDotNetMicroservice.Addressing;

/// <summary>
/// Resolves addresses of Kubernetes headless Services for client-side endpoint discovery.
/// </summary>
public sealed class KubernetesServiceDiscoveryAddressResolver : IMicroserviceAddressResolver
{
    /// <inheritdoc />
    public ResolvedMicroserviceAddress Resolve(MicroserviceAddressContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(context.ApplicationName);

        var namespaceName = context.Environment.EnvironmentName.ToLowerInvariant();
        var address = context.Protocol switch
        {
            MicroserviceProtocol.Grpc => new Uri(
                $"dns:///{context.ApplicationName}-grpc.{namespaceName}.svc.cluster.local:{context.PortOverride ?? 5006}"),
            MicroserviceProtocol.Http => new Uri(
                $"http://{context.ApplicationName}-http.{namespaceName}.svc.cluster.local:{context.PortOverride ?? 5005}"),
            _ => throw new ArgumentOutOfRangeException(nameof(context), context.Protocol, "Unsupported protocol."),
        };

        return new(address, EndpointResolutionMode.ServiceDiscovery);
    }
}
