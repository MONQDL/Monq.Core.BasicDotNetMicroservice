using Grpc.Core;
using Grpc.Net.Client.Balancer;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Monq.Core.BasicDotNetMicroservice.Addressing;
using Monq.Core.BasicDotNetMicroservice.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace Monq.Core.BasicDotNetMicroservice.Extensions;

/// <summary>
/// Extension methods for registering pre-configured gRPC clients on a host builder.
/// </summary>
public static class GrpcClientHostBuilderExtensions
{
    /// <summary>
    /// Adds a pre-configured gRPC client using the address returned by the configured resolver.
    /// A resolver supplied for this client takes precedence over host-level resolvers.
    /// A <c>dns</c> address automatically enables DNS resolution and round-robin balancing.
    /// </summary>
    public static IHostBuilder AddGrpcPreConfiguredClient<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TClient>(
        this IHostBuilder hostBuilder,
        string applicationName,
        Action<GrpcClientOptions>? configureOptions = null,
        int? port = null,
        TimeSpan? dnsRefreshInterval = null,
        IMicroserviceAddressResolver? addressResolver = null)
        where TClient : class =>
        hostBuilder.ConfigureServices((context, services) =>
        {
            var address = hostBuilder.ResolveMicroserviceAddress(
                context,
                applicationName,
                MicroserviceProtocol.Grpc,
                port,
                addressResolver).Address;
            var clientBuilder = services.AddGrpcPreConfiguredClient<TClient>(context.Configuration, options =>
            {
                configureOptions?.Invoke(options);
                var configureClient = options.ClientOptionsAction;
                options.ClientOptionsAction = clientOptions =>
                {
                    configureClient?.Invoke(clientOptions);
                    clientOptions.Address = address;
                };
            });

            if (IsServiceDiscoveryAddress(address))
                ConfigureServiceDiscovery(services, clientBuilder, dnsRefreshInterval);
        });

    /// <summary>
    /// Adds a pre-configured console/background gRPC client using the address returned by the configured resolver.
    /// A resolver supplied for this client takes precedence over host-level resolvers.
    /// A <c>dns</c> address automatically enables DNS resolution and round-robin balancing.
    /// </summary>
    public static IHostBuilder AddGrpcPreConfiguredConsoleClient<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TClient>(
        this IHostBuilder hostBuilder,
        string applicationName,
        Action<GrpcClientOptions>? configureOptions = null,
        int? port = null,
        TimeSpan? dnsRefreshInterval = null,
        IMicroserviceAddressResolver? addressResolver = null)
        where TClient : class =>
        hostBuilder.ConfigureServices((context, services) =>
        {
            var address = hostBuilder.ResolveMicroserviceAddress(
                context,
                applicationName,
                MicroserviceProtocol.Grpc,
                port,
                addressResolver).Address;
            var clientBuilder = services.AddGrpcPreConfiguredConsoleClient<TClient>(context.Configuration, options =>
            {
                configureOptions?.Invoke(options);
                var configureClient = options.ClientOptionsAction;
                options.ClientOptionsAction = clientOptions =>
                {
                    configureClient?.Invoke(clientOptions);
                    clientOptions.Address = address;
                };
            });

            if (IsServiceDiscoveryAddress(address))
                ConfigureServiceDiscovery(services, clientBuilder, dnsRefreshInterval);
        });

    static bool IsServiceDiscoveryAddress(Uri address) =>
        address.Scheme.Equals("dns", StringComparison.OrdinalIgnoreCase);

    static void ConfigureServiceDiscovery(
        IServiceCollection services,
        IHttpClientBuilder builder,
        TimeSpan? dnsRefreshInterval)
    {
        builder.ConfigureChannel(options =>
        {
            options.Credentials = ChannelCredentials.Insecure;
            options.ServiceConfig = new ServiceConfig
            {
                LoadBalancingConfigs =
                {
                    new RoundRobinConfig(),
                },
            };
        });

        services.TryAddSingleton<ResolverFactory>(
            _ => new DnsResolverFactory(dnsRefreshInterval ?? TimeSpan.FromSeconds(30)));
    }
}
