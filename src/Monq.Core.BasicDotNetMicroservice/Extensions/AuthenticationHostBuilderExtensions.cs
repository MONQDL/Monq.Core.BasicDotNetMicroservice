using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monq.Core.MicroserviceAddressing.Addressing;
using Monq.Core.MicroserviceAddressing.DependencyInjection;
using Monq.Core.MicroserviceAddressing.Hosting;

namespace Monq.Core.BasicDotNetMicroservice.Extensions;

/// <summary>
/// Extension methods for configuring authentication through a resolved microservice address.
/// </summary>
public static class AuthenticationHostBuilderExtensions
{
    /// <summary>
    /// Configures OAuth2 introspection using the default authentication service application name
    /// (<see cref="HostBuilderExtensions.DefaultAuthenticationApplicationName" />) and the address
    /// returned by the configured HTTP resolver.
    /// </summary>
    public static IHostBuilder ConfigureMonqAuthentication(this IHostBuilder hostBuilder)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);

        return hostBuilder.ConfigureMonqAuthentication(
            HostBuilderExtensions.DefaultAuthenticationApplicationName);
    }

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
            services.AddMicroserviceAddressing();
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
    /// Configures OAuth2 introspection using an explicit absolute authentication endpoint.
    /// </summary>
    public static IHostBuilder ConfigureMonqAuthentication(
        this IHostBuilder hostBuilder,
        Uri authenticationEndpoint)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        ArgumentNullException.ThrowIfNull(authenticationEndpoint);
        if (!authenticationEndpoint.IsAbsoluteUri)
            throw new ArgumentException(
                "The authentication endpoint must be an absolute URI.",
                nameof(authenticationEndpoint));

        return hostBuilder.ConfigureServices((context, services) =>
            services.ConfigureMonqAuthentication(context.Configuration, authenticationEndpoint));
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
    /// Configures OAuth2 introspection using an explicit absolute authentication endpoint.
    /// </summary>
    public static WebApplicationBuilder ConfigureMonqAuthentication(
        this WebApplicationBuilder builder,
        Uri authenticationEndpoint)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Host.ConfigureMonqAuthentication(authenticationEndpoint);
        return builder;
    }

    /// <summary>
    /// Configures OAuth2 introspection using the default authentication service application name
    /// (<see cref="HostBuilderExtensions.DefaultAuthenticationApplicationName" />) and the address
    /// returned by the configured HTTP resolver.
    /// </summary>
    public static WebApplicationBuilder ConfigureMonqAuthentication(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Host.ConfigureMonqAuthentication();
        return builder;
    }

    /// <summary>
    /// Configures static authentication for the default authentication service
    /// using the address returned by the configured HTTP resolver.
    /// </summary>
    public static WebApplicationBuilder ConfigureStaticAuthentication(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Host.ConfigureStaticAuthentication();
        return builder;
    }

    /// <summary>
    /// Configures static authentication for the default authentication service with custom
    /// settings for the isolated authentication HTTP client using the address returned
    /// by the configured HTTP resolver.
    /// </summary>
    public static WebApplicationBuilder ConfigureStaticAuthentication(
        this WebApplicationBuilder builder,
        Action<IHttpClientBuilder> configureAuthenticationClient)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Host.ConfigureStaticAuthentication(configureAuthenticationClient);
        return builder;
    }

    /// <summary>
    /// Configures static authentication using an explicit absolute authentication endpoint.
    /// </summary>
    public static WebApplicationBuilder ConfigureStaticAuthentication(
        this WebApplicationBuilder builder,
        Uri authenticationEndpoint,
        Action<IHttpClientBuilder>? configureAuthenticationClient = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Host.ConfigureStaticAuthentication(authenticationEndpoint, configureAuthenticationClient);
        return builder;
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
