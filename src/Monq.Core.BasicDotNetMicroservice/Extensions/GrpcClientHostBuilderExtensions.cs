using Grpc.Core;
using Grpc.Net.Client.Balancer;
using Grpc.Net.Client.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Monq.Core.BasicDotNetMicroservice.Configuration;
using Monq.Core.MicroserviceAddressing.Addressing;
using Monq.Core.MicroserviceAddressing.DependencyInjection;
using Monq.Core.MicroserviceAddressing.Hosting;
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
    /// An address resolved in Service Discovery mode or using the <c>dns</c> scheme
    /// automatically enables DNS resolution and round-robin balancing.
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
            services.AddMicroserviceAddressing();
            var resolvedAddress = hostBuilder.ResolveMicroserviceAddress(
                context,
                applicationName,
                MicroserviceProtocol.Grpc,
                port,
                addressResolver);
            var clientBuilder =
                services.AddGrpcPreConfiguredClient<TClient>(resolvedAddress.Address, configureOptions);

            ConfigureChannelCredentials(clientBuilder, resolvedAddress.Address);

            if (UsesServiceDiscovery(resolvedAddress))
                ConfigureServiceDiscovery(services, clientBuilder);
        });

    /// <summary>
    /// Adds a pre-configured gRPC client using an explicit absolute address.
    /// The address is used as is, without consulting the configured resolvers.
    /// An address using the <c>dns</c> scheme automatically enables DNS resolution
    /// and round-robin balancing.
    /// </summary>
    public static IHostBuilder AddGrpcPreConfiguredClient<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TClient>(
        this IHostBuilder hostBuilder,
        Uri address,
        Action<GrpcClientOptions>? configureOptions = null)
        where TClient : class
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        ArgumentNullException.ThrowIfNull(address);
        if (!address.IsAbsoluteUri)
            throw new ArgumentException("The gRPC client address must be absolute.", nameof(address));

        return hostBuilder.ConfigureServices((_, services) =>
        {
            var clientBuilder =
                services.AddGrpcPreConfiguredClient<TClient>(address, configureOptions);

            ConfigureChannelCredentials(clientBuilder, address);

            if (UsesServiceDiscovery(new(address, EndpointResolutionMode.Direct)))
                ConfigureServiceDiscovery(services, clientBuilder);
        });
    }

    /// <summary>
    /// Adds a pre-configured console/background gRPC client using the address returned
    /// by the configured resolver.
    /// A resolver supplied for this client takes precedence over host-level resolvers.
    /// An address resolved in Service Discovery mode or using the <c>dns</c> scheme
    /// automatically enables DNS resolution and round-robin balancing.
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
            services.AddMicroserviceAddressing();
            var resolvedAddress = hostBuilder.ResolveMicroserviceAddress(
                context,
                applicationName,
                MicroserviceProtocol.Grpc,
                port,
                addressResolver);
            var clientBuilder = services.AddGrpcPreConfiguredConsoleClient<TClient>(
                resolvedAddress.Address, configureOptions);

            ConfigureChannelCredentials(clientBuilder, resolvedAddress.Address);

            if (UsesServiceDiscovery(resolvedAddress))
                ConfigureServiceDiscovery(services, clientBuilder);
        });

    /// <summary>
    /// Adds a pre-configured console/background gRPC client using an explicit absolute address.
    /// The address is used as is, without consulting the configured resolvers.
    /// An address using the <c>dns</c> scheme automatically enables DNS resolution
    /// and round-robin balancing.
    /// </summary>
    public static IHostBuilder AddGrpcPreConfiguredConsoleClient<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TClient>(
        this IHostBuilder hostBuilder,
        Uri address,
        Action<GrpcClientOptions>? configureOptions = null)
        where TClient : class
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        ArgumentNullException.ThrowIfNull(address);
        if (!address.IsAbsoluteUri)
            throw new ArgumentException("The gRPC client address must be absolute.", nameof(address));

        return hostBuilder.ConfigureServices((_, services) =>
        {
            var clientBuilder = services.AddGrpcPreConfiguredConsoleClient<TClient>(
                address, configureOptions);

            ConfigureChannelCredentials(clientBuilder, address);

            if (UsesServiceDiscovery(new(address, EndpointResolutionMode.Direct)))
                ConfigureServiceDiscovery(services, clientBuilder);
        });
    }

    static bool UsesServiceDiscovery(ResolvedMicroserviceAddress resolvedAddress) =>
        resolvedAddress.ResolutionMode == EndpointResolutionMode.ServiceDiscovery
        || resolvedAddress.Address.Scheme.Equals("dns", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// grpc-dotnet requires explicit channel credentials for every address scheme other than
    /// http/https: dns:/// (Service Discovery), but also ipv4:///, unix:/ and any custom resolver
    /// scheme. Checking only "dns" would leave such addresses to fail channel creation with
    /// "Credentials must be specified". In-cluster endpoints work without TLS, so Insecure is used.
    /// Applied to every registered client regardless of the endpoint resolution mode; http/https
    /// addresses keep TLS inferred from the scheme.
    /// </summary>
    static void ConfigureChannelCredentials(IHttpClientBuilder builder, Uri address)
    {
        if (!address.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
            && !address.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
        {
            builder.ConfigureChannel(options => options.Credentials = ChannelCredentials.Insecure);
        }
    }

    static void ConfigureServiceDiscovery(
        IServiceCollection services,
        IHttpClientBuilder builder)
    {
        // Channel credentials are set scheme-agnostically by ConfigureChannelCredentials;
        // this method only enables DNS resolution and round-robin balancing for load-balanced schemes.
        builder.ConfigureChannel(options =>
        {
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
