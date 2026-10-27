using Grpc.Core;
using Grpc.Net.Client.Balancer;
using Grpc.Net.Client.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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
    /// Configures gRPC client-side service discovery for this application.
    /// The DNS refresh interval is shared by all gRPC clients using <c>dns</c> addresses.
    /// </summary>
    public static WebApplicationBuilder ConfigureGrpcServiceDiscovery(
        this WebApplicationBuilder builder,
        Action<GrpcServiceDiscoveryOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Host.ConfigureGrpcServiceDiscovery(configureOptions);
        return builder;
    }

    /// <summary>
    /// Configures gRPC client-side service discovery for this host.
    /// The DNS refresh interval is shared by all gRPC clients using <c>dns</c> addresses.
    /// </summary>
    public static IHostBuilder ConfigureGrpcServiceDiscovery(
        this IHostBuilder hostBuilder,
        Action<GrpcServiceDiscoveryOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        return hostBuilder.ConfigureServices(services =>
            services.Configure(configureOptions));
    }

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
            var clientBuilder = services.AddGrpcPreConfiguredClient<TClient>(address, configureOptions);

            if (IsServiceDiscoveryAddress(address))
                ConfigureServiceDiscovery(services, clientBuilder);
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
            var clientBuilder = services.AddGrpcPreConfiguredConsoleClient<TClient>(address, configureOptions);

            if (IsServiceDiscoveryAddress(address))
                ConfigureServiceDiscovery(services, clientBuilder);
        });

    static bool IsServiceDiscoveryAddress(Uri address) =>
        address.Scheme.Equals("dns", StringComparison.OrdinalIgnoreCase);

    static void ConfigureServiceDiscovery(
        IServiceCollection services,
        IHttpClientBuilder builder)
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

        services
            .AddOptions<GrpcServiceDiscoveryOptions>()
            .Validate(
                options => options.DnsRefreshInterval == Timeout.InfiniteTimeSpan
                    || options.DnsRefreshInterval > TimeSpan.Zero,
                $"{nameof(GrpcServiceDiscoveryOptions.DnsRefreshInterval)} must be positive or Timeout.InfiniteTimeSpan.");
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ResolverFactory, ConfiguredDnsResolverFactory>());
    }

    sealed class ConfiguredDnsResolverFactory : ResolverFactory
    {
        readonly DnsResolverFactory _factory;

        public ConfiguredDnsResolverFactory(IOptions<GrpcServiceDiscoveryOptions> options)
        {
            ArgumentNullException.ThrowIfNull(options);

            _factory = new(options.Value.DnsRefreshInterval);
        }

        public override string Name => _factory.Name;

        public override Resolver Create(ResolverOptions options) => _factory.Create(options);
    }
}
