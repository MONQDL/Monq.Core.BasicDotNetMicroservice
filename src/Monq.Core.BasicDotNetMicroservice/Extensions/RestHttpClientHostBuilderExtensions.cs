using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monq.Core.BasicDotNetMicroservice.Addressing;
using Monq.Core.HttpClientExtensions;
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
}
