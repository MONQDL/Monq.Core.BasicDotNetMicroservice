using Calzolari.Grpc.AspNetCore.Validation;
using Duende.AspNetCore.Authentication.OAuth2Introspection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monq.Core.BasicDotNetMicroservice.Configuration;
using Monq.Core.BasicDotNetMicroservice.Telemetry;
using Monq.Core.BasicDotNetMicroservice.Validation;
using Monq.Core.HttpClientExtensions;
using Monq.Core.MicroserviceAddressing.Addressing;
using System.Diagnostics.CodeAnalysis;
using MonqGrpcClientOptions = Monq.Core.GrpcClientExtensions.GrpcClientOptions;

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
            authConfig[AuthConstants.AuthenticationConfiguration.Authority]);
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
            authenticationEndpoint.AbsoluteUri);
    }

    static IServiceCollection ConfigureMonqAuthentication(
        IServiceCollection services,
        IConfiguration configuration,
        string? authenticationEndpoint)
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
                x.Authority = authenticationEndpoint;
                x.ClientId = authConfig[AuthConstants.AuthenticationConfiguration.ScopeName];
                x.ClientSecret = authConfig[AuthConstants.AuthenticationConfiguration.ScopeSecret];
                x.CacheDuration = TimeSpan.FromSeconds(cacheDurationSeconds);
                x.NameClaimType = "fullName";
                x.DiscoveryPolicy.RequireHttps = requireHttps;
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

    static Uri GetBaseUri(IConfiguration configuration) =>
        new(configuration.GetValue<string>(nameof(AppConfiguration.BaseUri))
            ?? throw new Exception("BaseUri not found at configuration."));

    /// <summary>
    /// Adds a pre-configured gRPC client with an address from configuration. Channel defaults,
    /// header propagation and authorization forwarding are applied by
    /// Monq.Core.GrpcClientExtensions.
    /// </summary>
    /// <typeparam name="TClient">The gRPC client type to register as transient.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration to read BaseUri from.</param>
    /// <param name="configureOptions">Optional delegate to customize <see cref="GrpcClientOptions"/>.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> for further client configuration.</returns>
    /// <remarks>
    /// This overload is retained for compatibility with clients that read a shared BaseUri from configuration.
    /// New registrations should use the IHostBuilder overload and an application name.
    /// </remarks>
    [Obsolete(
        "Use AddGrpcPreConfiguredClient<TClient>(IHostBuilder, string, ...) and configure an IMicroserviceAddressResolver on the host instead.",
        false)]
    public static IHttpClientBuilder AddGrpcPreConfiguredClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TClient>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<GrpcClientOptions>? configureOptions = null)
        where TClient : class =>
        services.AddGrpcPreConfiguredClient<TClient>(
            new ResolvedMicroserviceAddress(GetBaseUri(configuration)), configureOptions);

    internal static IHttpClientBuilder AddGrpcPreConfiguredClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TClient>(
        this IServiceCollection services,
        ResolvedMicroserviceAddress resolvedAddress,
        Action<GrpcClientOptions>? configureOptions)
        where TClient : class =>
        services.AddGrpcPreConfiguredClientCore<TClient>(resolvedAddress, configureOptions, console: false);

    /// <summary>
    /// Adds a pre-configured gRPC client for console/background applications with static authentication fallback.
    /// If no HttpContext is available, obtains a token via <see cref="RestHttpClient.GetAccessToken"/>.
    /// </summary>
    /// <typeparam name="TClient">The gRPC client type to register as transient.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration to read BaseUri from.</param>
    /// <param name="configureOptions">Optional delegate to customize <see cref="GrpcClientOptions"/>.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> for further client configuration.</returns>
    /// <remarks>
    /// This overload is retained for compatibility with clients that read a shared BaseUri from configuration.
    /// New registrations should use the IHostBuilder overload and an application name.
    /// </remarks>
    [Obsolete(
        "Use AddGrpcPreConfiguredConsoleClient<TClient>(IHostBuilder, string, ...) and configure an IMicroserviceAddressResolver on the host instead.",
        false)]
    public static IHttpClientBuilder AddGrpcPreConfiguredConsoleClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TClient>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<GrpcClientOptions>? configureOptions = null)
        where TClient : class =>
        services.AddGrpcPreConfiguredConsoleClient<TClient>(
            new ResolvedMicroserviceAddress(GetBaseUri(configuration)), configureOptions);

    internal static IHttpClientBuilder AddGrpcPreConfiguredConsoleClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TClient>(
        this IServiceCollection services,
        ResolvedMicroserviceAddress resolvedAddress,
        Action<GrpcClientOptions>? configureOptions)
        where TClient : class =>
        services.AddGrpcPreConfiguredClientCore<TClient>(resolvedAddress, configureOptions, console: true);

    /// <summary>
    /// Adds a pre-configured REST HTTP client based on <see cref="RestHttpClient"/> with base address from configuration,
    /// infinite timeout (managed via cancellation tokens), and trailing slash normalization.
    /// </summary>
    /// <typeparam name="TClient">The typed client interface.</typeparam>
    /// <typeparam name="TImplementation">The implementation type inheriting from <see cref="RestHttpClient"/>.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration to read BaseUri from.</param>
    /// <param name="configureHttpClient">Optional delegate to customize the <see cref="HttpClient"/>.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> for further client configuration.</returns>
    /// <remarks>
    /// This overload is retained for compatibility with clients that read a shared BaseUri from configuration.
    /// New registrations should use the IHostBuilder overload and an application name.
    /// </remarks>
    [Obsolete(
        "Use AddRestHttpPreConfiguredClient<TClient, TImplementation>(IHostBuilder, string, ...) and configure an IMicroserviceAddressResolver on the host instead.",
        false)]
    public static IHttpClientBuilder AddRestHttpPreConfiguredClient<TClient, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<HttpClient>? configureHttpClient = null)
        where TClient : class
        where TImplementation : RestHttpClient, TClient
    {
        var baseUri = new Uri(
            configuration.GetValue<string>(nameof(AppConfiguration.BaseUri))
            ?? throw new Exception("BaseUri not found at configuration."));

        return services.AddRestHttpPreConfiguredClient<TClient, TImplementation>(
            baseUri,
            configureHttpClient);
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

    static IHttpClientBuilder AddGrpcPreConfiguredClientCore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TClient>(
        this IServiceCollection services,
        ResolvedMicroserviceAddress resolvedAddress,
        Action<GrpcClientOptions>? configureOptions,
        bool console)
        where TClient : class
    {
        var options = new GrpcClientOptions();
        configureOptions?.Invoke(options);

        var monqOptions = new MonqGrpcClientOptions
        {
            ClientOptionsAction = options.ClientOptionsAction,
            ChannelOptionsAction = options.ChannelOptionsAction,
            ContextPropagationOptionsAction = options.ContextPropagationOptionsAction,
        };

        var builder = options.Name is not null
            ? console
                ? services.AddMicroserviceGrpcConsoleClient<TClient>(
                    options.Name, resolvedAddress.Address, resolvedAddress.ResolutionMode, monqOptions)
                : services.AddMicroserviceGrpcClient<TClient>(
                    options.Name, resolvedAddress.Address, resolvedAddress.ResolutionMode, monqOptions)
            : console
                ? services.AddMicroserviceGrpcConsoleClient<TClient>(
                    resolvedAddress.Address, resolvedAddress.ResolutionMode, monqOptions)
                : services.AddMicroserviceGrpcClient<TClient>(
                    resolvedAddress.Address, resolvedAddress.ResolutionMode, monqOptions);

        // Header propagation, channel defaults (unsafe call credentials, 51 MB receive limit),
        // call context propagation and Authorization forwarding (with static token fallback for
        // console clients) are applied by Monq.Core.GrpcClientExtensions for every client.
        return builder;
    }
}
