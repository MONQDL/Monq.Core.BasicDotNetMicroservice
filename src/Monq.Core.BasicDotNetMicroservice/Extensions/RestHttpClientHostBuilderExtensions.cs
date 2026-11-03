using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monq.Core.HttpClientExtensions;
using Monq.Core.MicroserviceAddressing.Addressing;
using Monq.Core.MicroserviceAddressing.Hosting;
using System.Diagnostics.CodeAnalysis;

namespace Monq.Core.BasicDotNetMicroservice.Extensions;

/// <summary>
/// Extension methods for registering pre-configured REST HTTP clients on a host builder.
/// </summary>
public static class RestHttpClientHostBuilderExtensions
{
    /// <summary>
    /// Adds a pre-configured REST HTTP client using the address and resolution mode returned by the configured resolver.
    /// A resolver supplied for this client takes precedence over host-level resolvers.
    /// </summary>
    public static IHostBuilder AddRestHttpPreConfiguredClient<
        TClient,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(
        this IHostBuilder hostBuilder,
        string applicationName,
        Action<HttpClient>? configureHttpClient = null,
        int? port = null,
        IMicroserviceAddressResolver? addressResolver = null)
        where TClient : class
        where TImplementation : RestHttpClient, TClient =>
        hostBuilder.ConfigureServices((context, services) =>
        {
            var resolvedAddress = hostBuilder.ResolveMicroserviceAddress(
                context,
                applicationName,
                MicroserviceProtocol.Http,
                port,
                addressResolver);
            var clientBuilder = services.AddRestHttpPreConfiguredClient<TClient, TImplementation>(
                resolvedAddress.Address,
                configureHttpClient);

            if (resolvedAddress.ResolutionMode != EndpointResolutionMode.ServiceDiscovery)
                return;

            services.AddServiceDiscoveryCore();
            services.AddDnsServiceEndpointProvider();
            clientBuilder.AddServiceDiscovery();
        });

    /// <summary>
    /// Adds a pre-configured REST HTTP client using an explicit absolute address.
    /// The address is used as is, without consulting the configured resolvers.
    /// </summary>
    public static IHostBuilder AddRestHttpPreConfiguredClient<
        TClient,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(
        this IHostBuilder hostBuilder,
        Uri address,
        Action<HttpClient>? configureHttpClient = null)
        where TClient : class
        where TImplementation : RestHttpClient, TClient
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        ArgumentNullException.ThrowIfNull(address);
        if (!address.IsAbsoluteUri)
            throw new ArgumentException("The HTTP client base address must be absolute.", nameof(address));

        return hostBuilder.ConfigureServices((_, services) =>
            services.AddRestHttpPreConfiguredClient<TClient, TImplementation>(address, configureHttpClient));
    }
}
