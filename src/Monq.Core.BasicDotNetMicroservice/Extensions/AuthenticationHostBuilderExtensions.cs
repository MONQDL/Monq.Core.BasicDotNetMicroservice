using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monq.Core.BasicDotNetMicroservice.Addressing;

namespace Monq.Core.BasicDotNetMicroservice.Extensions;

/// <summary>
/// Extension methods for configuring authentication through a resolved microservice address.
/// </summary>
public static class AuthenticationHostBuilderExtensions
{
    /// <summary>
    /// Configures OAuth2 introspection using the address returned by the configured HTTP resolver.
    /// </summary>
    public static IHostBuilder ConfigureMonqAuthentication(
        this IHostBuilder hostBuilder,
        string applicationName,
        int? port = null,
        IMicroserviceAddressResolver? addressResolver = null)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);

        return hostBuilder.ConfigureServices((context, services) =>
        {
            var authenticationEndpoint = hostBuilder.ResolveMicroserviceAddress(
                context,
                applicationName,
                MicroserviceProtocol.Http,
                port,
                addressResolver).Address;

            services.ConfigureMonqAuthentication(
                context.Configuration,
                authenticationEndpoint);
        });
    }

    /// <summary>
    /// Configures OAuth2 introspection using the address returned by the configured HTTP resolver.
    /// </summary>
    public static WebApplicationBuilder ConfigureMonqAuthentication(
        this WebApplicationBuilder builder,
        string applicationName,
        int? port = null,
        IMicroserviceAddressResolver? addressResolver = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Host.ConfigureMonqAuthentication(applicationName, port, addressResolver);
        return builder;
    }

    /// <summary>
    /// Configures static authentication using the address returned by the configured HTTP resolver.
    /// </summary>
    public static IHostBuilder ConfigureStaticAuthentication(
        this IHostBuilder hostBuilder,
        string applicationName,
        Action<IHttpClientBuilder>? configureAuthenticationClient = null,
        int? port = null,
        IMicroserviceAddressResolver? addressResolver = null)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);

        return Microsoft.Extensions.DependencyInjection.HostBuilderExtensions
            .ConfigureStaticAuthentication(
                hostBuilder,
                context => hostBuilder.ResolveMicroserviceAddress(
                    context,
                    applicationName,
                    MicroserviceProtocol.Http,
                    port,
                    addressResolver).Address,
                configureAuthenticationClient);
    }

    /// <summary>
    /// Configures static authentication using the address returned by the configured HTTP resolver.
    /// </summary>
    public static WebApplicationBuilder ConfigureStaticAuthentication(
        this WebApplicationBuilder builder,
        string applicationName,
        Action<IHttpClientBuilder>? configureAuthenticationClient = null,
        int? port = null,
        IMicroserviceAddressResolver? addressResolver = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Host.ConfigureStaticAuthentication(
            applicationName,
            configureAuthenticationClient,
            port,
            addressResolver);
        return builder;
    }
}
