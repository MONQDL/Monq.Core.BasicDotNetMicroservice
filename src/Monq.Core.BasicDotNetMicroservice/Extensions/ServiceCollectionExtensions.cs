using Calzolari.Grpc.AspNetCore.Validation;
using Duende.AspNetCore.Authentication.OAuth2Introspection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Monq.Core.BasicDotNetMicroservice.Configuration;
using Monq.Core.BasicDotNetMicroservice.Telemetry;
using Monq.Core.BasicDotNetMicroservice.Validation;
using Monq.Core.HttpClientExtensions;
using Monq.Core.MicroserviceAddressing.Addressing;
using Monq.Core.MicroserviceAddressing.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Monq.Core.BasicDotNetMicroservice.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to configure authentication, metrics, gRPC clients, and HTTP clients.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures OAuth2 introspection authentication using Duende library.
    /// </summary>
    /// <param name="services">The service collection to add authentication to.</param>
    /// <param name="configuration">The configuration containing authentication settings.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection ConfigureMonqAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var authConfig = configuration.GetSection("Authentication");
        return ConfigureMonqAuthentication(
            services,
            configuration,
            configureEndpoint: x => x.Authority = authConfig[AuthConstants.AuthenticationConfiguration.Authority]);
    }

    /// <summary>
    /// Configures OAuth2 introspection authentication using an explicitly resolved authentication endpoint.
    /// </summary>
    /// <param name="services">The service collection to add authentication to.</param>
    /// <param name="configuration">The configuration containing authentication credentials and settings.</param>
    /// <param name="authenticationEndpoint">The absolute authentication authority URI.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection ConfigureMonqAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        Uri authenticationEndpoint)
    {
        ArgumentNullException.ThrowIfNull(authenticationEndpoint);
        if (!authenticationEndpoint.IsAbsoluteUri)
            throw new ArgumentException(
                "The authentication endpoint must be an absolute URI.",
                nameof(authenticationEndpoint));

        return ConfigureMonqAuthentication(
            services,
            configuration,
            configureEndpoint: x => x.Authority = authenticationEndpoint.AbsoluteUri);
    }

    /// <summary>
    /// Configures OAuth2 introspection authentication for an authentication service addressed by
    /// application name. The authority is resolved from the registered
    /// <see cref="IMicroserviceAddressProvider"/> when the authentication options are created,
    /// using the HTTP address resolver selected for the application.
    /// </summary>
    /// <param name="services">The service collection to add authentication to.</param>
    /// <param name="configuration">The configuration containing authentication credentials and settings.</param>
    /// <param name="applicationName">The authentication service application name.</param>
    /// <param name="port">Port for address resolution. When <see langword="null"/>, the resolver selects the port.</param>
    /// <param name="addressResolver">Resolver that takes precedence over the registered resolvers.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection ConfigureMonqAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string applicationName,
        int? port = null,
        IMicroserviceAddressResolver? addressResolver = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);

        services.AddMicroserviceAddressing();

        ConfigureMonqAuthentication(
            services,
            configuration,
            configureEndpoint: null);

        services.AddOptions<OAuth2IntrospectionOptions>(AuthConstants.AuthenticationScheme)
            .Configure<IServiceProvider>((options, provider) =>
                options.Authority = provider
                    .GetRequiredService<IMicroserviceAddressProvider>()
                    .Resolve(applicationName, MicroserviceProtocol.Http, port, addressResolver)
                    .Address
                    .AbsoluteUri);

        return services;
    }

    static IServiceCollection ConfigureMonqAuthentication(
        IServiceCollection services,
        IConfiguration configuration,
        Action<OAuth2IntrospectionOptions>? configureEndpoint)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var authConfig = configuration.GetSection("Authentication");

        if (!bool.TryParse(authConfig[AuthConstants.AuthenticationConfiguration.RequireHttpsMetadata], out var requireHttps))
            requireHttps = false;

        if (!int.TryParse(authConfig[AuthConstants.AuthenticationConfiguration.CacheDurationSeconds],
                out var cacheDurationSeconds)
            || cacheDurationSeconds <= 0)
            cacheDurationSeconds = 300;

        services.AddAuthentication(AuthConstants.AuthenticationScheme)
            .AddOAuth2Introspection(AuthConstants.AuthenticationScheme, x =>
            {
                x.ClientId = authConfig[AuthConstants.AuthenticationConfiguration.ScopeName];
                x.ClientSecret = authConfig[AuthConstants.AuthenticationConfiguration.ScopeSecret];
                x.CacheDuration = TimeSpan.FromSeconds(cacheDurationSeconds);
                x.NameClaimType = "fullName";
                x.DiscoveryPolicy.RequireHttps = requireHttps;

                configureEndpoint?.Invoke(x);
            });

        return services;
    }

    /// <summary>
    /// Registers <see cref="MonqMetrics"/> as a singleton for application metrics collection.
    /// </summary>
    /// <param name="services">The service collection to add metrics to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMonqMetrics(this IServiceCollection services)
    {
        services.AddSingleton<MonqMetrics>();
        return services;
    }

    /// <summary>
    /// Enables gRPC request validation with message validation and default error message handler.
    /// </summary>
    /// <param name="services">The service collection to add gRPC validation to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGrpcRequestValidation(this IServiceCollection services)
    {
        services.AddGrpc(options => options.EnableMessageValidation());

        services.AddSingleton<IValidatorErrorMessageHandler>(new DefaultValidatorMessageHandler());
        services.AddGrpcValidation();

        return services;
    }

    /// <summary>
    /// Adds a pre-configured REST HTTP client whose base address is resolved by application name when
    /// the typed client is created, using the addressing configured on the host. Service discovery is
    /// enabled for the client: Kubernetes Service names are watched through the DNS endpoint provider,
    /// so ClusterIP addresses resolve to the single service endpoint and headless Service names to the
    /// ready pod endpoints.
    /// </summary>
    /// <typeparam name="TClient">The typed client interface.</typeparam>
    /// <typeparam name="TImplementation">The implementation type inheriting from <see cref="RestHttpClient"/>.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="applicationName">The Kubernetes application name to resolve the address from.</param>
    /// <param name="configureHttpClient">Optional delegate to customize the <see cref="HttpClient"/>.</param>
    /// <param name="port">Port for address resolution. When <see langword="null"/>, the resolver selects the port.</param>
    /// <param name="addressResolver">Resolver that takes precedence over host-level resolvers.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> for further client configuration.</returns>
    public static IHttpClientBuilder AddRestHttpPreConfiguredClient<
        TClient,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(
        this IServiceCollection services,
        string applicationName,
        Action<HttpClient>? configureHttpClient = null,
        int? port = null,
        IMicroserviceAddressResolver? addressResolver = null)
        where TClient : class
        where TImplementation : RestHttpClient, TClient
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);

        services.AddServiceDiscoveryCore();

        if (!services.Any(descriptor =>
                descriptor.ServiceType.Name == "IServiceEndpointProviderFactory"
                && descriptor.ImplementationType?.Name == "DnsServiceEndpointProviderFactory"))
        {
            services.AddDnsServiceEndpointProvider();
        }

        return services
            .AddHttpClient<TClient, TImplementation>()
            .ConfigureHttpClient((provider, client) =>
            {
                var resolvedAddress = provider
                    .GetRequiredService<IMicroserviceAddressProvider>()
                    .Resolve(applicationName, MicroserviceProtocol.Http, port, addressResolver);

                client.BaseAddress = resolvedAddress.Address;
                client.Timeout = Timeout.InfiniteTimeSpan;

                client.AddTrailingSlash();
                configureHttpClient?.Invoke(client);
            })
            .AddServiceDiscovery();
    }

    /// <summary>
    /// Adds a pre-configured REST HTTP client based on <see cref="RestHttpClient"/> with the specified base address,
    /// infinite timeout (managed via cancellation tokens), and trailing slash normalization.
    /// </summary>
    /// <typeparam name="TClient">The typed client interface.</typeparam>
    /// <typeparam name="TImplementation">The implementation type inheriting from <see cref="RestHttpClient"/>.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="baseAddress">The absolute base address.</param>
    /// <param name="configureHttpClient">Optional delegate to customize the <see cref="HttpClient"/>.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> for further client configuration.</returns>
    public static IHttpClientBuilder AddRestHttpPreConfiguredClient<TClient, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(
        this IServiceCollection services,
        Uri baseAddress,
        Action<HttpClient>? configureHttpClient = null)
        where TClient : class
        where TImplementation : RestHttpClient, TClient
    {
        ArgumentNullException.ThrowIfNull(baseAddress);

        if (!baseAddress.IsAbsoluteUri)
            throw new ArgumentException("The HTTP client base address must be absolute.", nameof(baseAddress));

        return services.AddHttpClient<TClient, TImplementation>(
            client =>
            {
                client.BaseAddress = baseAddress;

                client.Timeout = Timeout.InfiniteTimeSpan;
                configureHttpClient?.Invoke(client);

                client.AddTrailingSlash();
            });
    }
}
