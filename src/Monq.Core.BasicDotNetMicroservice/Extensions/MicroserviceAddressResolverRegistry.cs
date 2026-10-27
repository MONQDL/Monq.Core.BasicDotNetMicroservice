using Microsoft.Extensions.Hosting;
using Monq.Core.BasicDotNetMicroservice.Addressing;

namespace Monq.Core.BasicDotNetMicroservice.Extensions;

/// <summary>
/// Stores address resolver selections associated with a host builder.
/// </summary>
internal static class MicroserviceAddressResolverRegistry
{
    static readonly object _registryKey = new();
    static readonly IMicroserviceAddressResolver _builtInResolver = new KubernetesClusterIpAddressResolver();

    internal static IHostBuilder SetDefault(
        IHostBuilder hostBuilder,
        IMicroserviceAddressResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        return Set(hostBuilder, state => state.DefaultFactory = _ => resolver);
    }

    internal static IHostBuilder SetDefault(
        IHostBuilder hostBuilder,
        Func<HostBuilderContext, IMicroserviceAddressResolver> resolverFactory)
    {
        ArgumentNullException.ThrowIfNull(resolverFactory);
        return Set(hostBuilder, state => state.DefaultFactory = resolverFactory);
    }

    internal static IHostBuilder SetGrpc(
        IHostBuilder hostBuilder,
        IMicroserviceAddressResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        return Set(hostBuilder, state => state.GrpcFactory = _ => resolver);
    }

    internal static IHostBuilder SetHttp(
        IHostBuilder hostBuilder,
        IMicroserviceAddressResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        return Set(hostBuilder, state => state.HttpFactory = _ => resolver);
    }

    internal static IMicroserviceAddressResolver Get(
        IHostBuilder hostBuilder,
        HostBuilderContext context,
        MicroserviceProtocol protocol,
        IMicroserviceAddressResolver? clientResolver)
    {
        if (clientResolver is not null)
            return clientResolver;

        var state = TryGetState(hostBuilder);
        var protocolFactory = protocol switch
        {
            MicroserviceProtocol.Grpc => state?.GrpcFactory,
            MicroserviceProtocol.Http => state?.HttpFactory,
            _ => throw new ArgumentOutOfRangeException(nameof(protocol), protocol, "Unsupported protocol."),
        };

        return Invoke(protocolFactory, context)
            ?? Invoke(state?.DefaultFactory, context)
            ?? _builtInResolver;
    }

    static IHostBuilder Set(IHostBuilder hostBuilder, Action<ResolverRegistryState> configure)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        ArgumentNullException.ThrowIfNull(configure);

        configure(GetOrCreateState(hostBuilder));
        return hostBuilder;
    }

    static ResolverRegistryState GetOrCreateState(IHostBuilder hostBuilder)
    {
        if (TryGetState(hostBuilder) is { } state)
            return state;

        state = new();
        hostBuilder.Properties[_registryKey] = state;
        return state;
    }

    static ResolverRegistryState? TryGetState(IHostBuilder hostBuilder) =>
        hostBuilder.Properties.TryGetValue(_registryKey, out var value)
            ? (ResolverRegistryState)value
            : null;

    static IMicroserviceAddressResolver? Invoke(
        Func<HostBuilderContext, IMicroserviceAddressResolver>? resolverFactory,
        HostBuilderContext context) =>
        resolverFactory is null
            ? null
            : resolverFactory(context)
                ?? throw new InvalidOperationException("The microservice address resolver factory returned null.");

    sealed class ResolverRegistryState
    {
        internal Func<HostBuilderContext, IMicroserviceAddressResolver>? DefaultFactory { get; set; }

        internal Func<HostBuilderContext, IMicroserviceAddressResolver>? GrpcFactory { get; set; }

        internal Func<HostBuilderContext, IMicroserviceAddressResolver>? HttpFactory { get; set; }
    }
}
