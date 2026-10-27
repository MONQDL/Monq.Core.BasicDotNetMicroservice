namespace Monq.Core.BasicDotNetMicroservice.Addressing;

/// <summary>
/// Resolves addresses of regular Kubernetes ClusterIP Services.
/// </summary>
public sealed class KubernetesClusterIpAddressResolver : IMicroserviceAddressResolver
{
    /// <inheritdoc />
    public ResolvedMicroserviceAddress Resolve(MicroserviceAddressContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(context.ApplicationName);

        var namespaceName = context.Environment.EnvironmentName.ToLowerInvariant();
        var port = context.PortOverride ?? context.Protocol switch
        {
            MicroserviceProtocol.Grpc => 5006,
            MicroserviceProtocol.Http => 80,
            _ => throw new ArgumentOutOfRangeException(nameof(context), context.Protocol, "Unsupported protocol."),
        };

        return new(new Uri(
            $"http://{context.ApplicationName}.{namespaceName}.svc.cluster.local:{port}"));
    }
}
