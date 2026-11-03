using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monq.Core.BasicDotNetMicroservice.Configuration;
using Monq.Core.MicroserviceAddressing.Addressing;
using Monq.Core.MicroserviceAddressing.DependencyInjection;
using Monq.Core.MicroserviceAddressing.Hosting;
using System.Diagnostics.CodeAnalysis;
using GrpcServiceDiscoveryOptions = Monq.Core.GrpcClientExtensions.GrpcServiceDiscoveryOptions;

namespace Monq.Core.BasicDotNetMicroservice.Extensions;

/// <summary>
/// Extension methods for registering pre-configured gRPC clients on a host builder.
/// Channel credentials and load balancing rules are applied by
/// <see cref="Monq.Core.GrpcClientExtensions"/>; this class adds the host-level API, eager
/// address resolution and the Basic microservice policies (headers interceptor, credentials).
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

        return hostBuilder.ConfigureServices((_, services) =>
            services.ConfigureGrpcServiceDiscovery(configureOptions));
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

            services.AddGrpcPreConfiguredClient<TClient>(resolvedAddress, configureOptions);
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
            services.AddGrpcPreConfiguredClient<TClient>(
                new ResolvedMicroserviceAddress(address), configureOptions));
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

            services.AddGrpcPreConfiguredConsoleClient<TClient>(resolvedAddress, configureOptions);
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
            services.AddGrpcPreConfiguredConsoleClient<TClient>(
                new ResolvedMicroserviceAddress(address), configureOptions));
    }
}
