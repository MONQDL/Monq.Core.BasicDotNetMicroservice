using Calzolari.Grpc.AspNetCore.Validation;
using Duende.AspNetCore.Authentication.OAuth2Introspection;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Monq.Core.BasicDotNetMicroservice.Configuration;
using Monq.Core.BasicDotNetMicroservice.Metrics;
using Monq.Core.BasicDotNetMicroservice.Validation;
using Monq.Core.HttpClientExtensions;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;

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
        var authConfig = configuration.GetSection("Authentication");

        if (!bool.TryParse(authConfig[AuthConstants.AuthenticationConfiguration.RequireHttpsMetadata], out var requireHttps))
            requireHttps = false;

        services.AddAuthentication(AuthConstants.AuthenticationScheme)
            .AddOAuth2Introspection(AuthConstants.AuthenticationScheme, x =>
            {
                x.Authority = authConfig[AuthConstants.AuthenticationConfiguration.Authority];
                x.ClientId = authConfig[AuthConstants.AuthenticationConfiguration.ScopeName];
                x.ClientSecret = authConfig[AuthConstants.AuthenticationConfiguration.ScopeSecret];
                x.CacheDuration = TimeSpan.FromMinutes(5);
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

    static void ConfigureGrpcClient(
        GrpcClientOptions options,
        IConfiguration configuration)
    {
        options.ClientOptionsAction = clientOptions =>
            clientOptions.Address = new Uri(
                configuration.GetValue<string>(nameof(AppConfiguration.BaseUri))
                ?? throw new Exception("BaseUri not found at configuration."));
        options.ChannelOptionsAction = channelOptions =>
        {
            channelOptions.UnsafeUseInsecureChannelCallCredentials = true;
            channelOptions.MaxReceiveMessageSize = 51 * 1024 * 1024;
        };
        options.ContextPropagationOptionsAction = propagationOptions =>
            propagationOptions.SuppressContextNotFoundErrors = true;
    }

    /// <summary>
    /// Adds a pre-configured gRPC client with address from configuration, insecure channel, 
    /// max message size of 51MB, context propagation, authorization header forwarding, and additional headers interceptor.
    /// </summary>
    /// <typeparam name="TClient">The gRPC client type to register as transient.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration to read BaseUri from.</param>
    /// <param name="configureOptions">Optional delegate to customize <see cref="GrpcClientOptions"/>.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> for further client configuration.</returns>
    public static IHttpClientBuilder AddGrpcPreConfiguredClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TClient>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<GrpcClientOptions>? configureOptions = null)
        where TClient : class
    {
        var options = new GrpcClientOptions();
        ConfigureGrpcClient(options, configuration);
        configureOptions?.Invoke(options);

        return services.AddGrpcPreConfiguredClient<TClient>(options);
    }

    /// <summary>
    /// Adds a pre-configured gRPC client for console/background applications with static authentication fallback.
    /// If no HttpContext is available, obtains a token via <see cref="RestHttpClient.GetAccessToken"/>.
    /// </summary>
    /// <typeparam name="TClient">The gRPC client type to register as transient.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration to read BaseUri from.</param>
    /// <param name="configureOptions">Optional delegate to customize <see cref="GrpcClientOptions"/>.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> for further client configuration.</returns>
    public static IHttpClientBuilder AddGrpcPreConfiguredConsoleClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TClient>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<GrpcClientOptions>? configureOptions = null)
        where TClient : class
    {
        var options = new GrpcClientOptions();
        ConfigureGrpcClient(options, configuration);
        configureOptions?.Invoke(options);

        return services.AddGrpcPreConfiguredConsoleClient<TClient>(options);
    }

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
        var httpClientBuilder = services.AddHttpClient<TClient, TImplementation>(
            client =>
            {
                client.BaseAddress = baseUri;

                client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
                configureHttpClient?.Invoke(client);

                client.AddTrailingSlash();
            });

        return httpClientBuilder;
    }

    static IHttpClientBuilder AddGrpcPreConfiguredClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TClient>(
        this IServiceCollection services,
        GrpcClientOptions? options = null)
        where TClient : class
    {
        services.TryAddTransient<AdditionalHeadersInterceptor>();

        return services
            .AddGrpcClient<TClient>(options)
            .AddInterceptor<AdditionalHeadersInterceptor>()
            .AddCallCredentials((context, metadata, provider) =>
            {
                var httpContext = provider.GetRequiredService<IHttpContextAccessor>();
                if (httpContext.HttpContext?.Request.Headers.TryGetValue(HttpRequestHeader.Authorization.ToString(), out var token) == true
                    && !string.IsNullOrEmpty(token))
                    metadata.Add(HttpRequestHeader.Authorization.ToString(), token!);

                return Task.CompletedTask;
            });
    }

    static IHttpClientBuilder AddGrpcPreConfiguredConsoleClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TClient>(
        this IServiceCollection services,
        GrpcClientOptions? options = null)
        where TClient : class
    {
        services.AddHttpClient<RestHttpClient>();

        services.TryAddTransient<AdditionalHeadersInterceptor>();

        return services
            .AddGrpcClient<TClient>(options)
            .AddInterceptor<AdditionalHeadersInterceptor>()
            .AddCallCredentials(async (context, metadata, provider) =>
            {
                var httpContextAccessor = provider.GetService<IHttpContextAccessor>();
                if (httpContextAccessor?.HttpContext?.Request.Headers.TryGetValue(HttpRequestHeader.Authorization.ToString(), out var token) == true
                    && !string.IsNullOrEmpty(token))
                {
                    metadata.Add(HttpRequestHeader.Authorization.ToString(), token!);
                }
                else
                {
                    var client = provider.GetService<RestHttpClient>();
                    if (client is not null)
                    {
                        var tokenResponse = await client.GetAccessToken(false);
                        if (tokenResponse is null)
                            return;
                        const string scheme = "Bearer";
                        var authorizationHeaderValue = new AuthenticationHeaderValue(scheme, tokenResponse.AccessToken);
                        metadata.Add(HttpRequestHeader.Authorization.ToString(), authorizationHeaderValue.ToString());
                    }
                }
            });
    }

    /// <summary>
    /// gRPC client interceptor that propagates <c>userspace-id</c> and <c>culture</c> headers
    /// from the current HTTP request to outgoing gRPC calls.
    /// </summary>
    public class AdditionalHeadersInterceptor : Interceptor
    {
        readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Creates a new instance of <see cref="AdditionalHeadersInterceptor"/>.
        /// </summary>
        /// <param name="httpContextAccessor">The HTTP context accessor to read incoming headers from.</param>
        public AdditionalHeadersInterceptor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc />
        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            var newContext = CreateModifiedInterceptorContext(context);

            return base.AsyncServerStreamingCall(request, newContext, continuation);
        }

        /// <inheritdoc />
        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            var newContext = CreateModifiedInterceptorContext(context);

            return base.AsyncClientStreamingCall(newContext, continuation);
        }

        /// <inheritdoc />
        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            var newContext = CreateModifiedInterceptorContext(context);

            return base.AsyncDuplexStreamingCall(newContext, continuation);
        }

        /// <inheritdoc />
        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var newContext = CreateModifiedInterceptorContext(context);

            return base.AsyncUnaryCall(request, newContext, continuation);
        }

        /// <inheritdoc />
        public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var newContext = CreateModifiedInterceptorContext(context);

            return base.BlockingUnaryCall(request, newContext, continuation);
        }

        ClientInterceptorContext<TRequest, TResponse> CreateModifiedInterceptorContext<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context)
            where TRequest : class
            where TResponse : class
        {
            var headers = new Metadata();
            if (context.Options.Headers is not null)
                foreach (var header in context.Options.Headers)
                    headers.Add(header);

            if (!headers.Any(x => x.Key.Equals(MicroserviceConstants.UserspaceIdHeader, StringComparison.OrdinalIgnoreCase))
                    && _httpContextAccessor?.HttpContext?.Request?.Headers?.TryGetValue(MicroserviceConstants.UserspaceIdHeader, out var userspaceId) == true
                    && !string.IsNullOrEmpty(userspaceId))
                headers.Add(MicroserviceConstants.UserspaceIdHeader, userspaceId!);

            if (!headers.Any(x => x.Key.Equals(MicroserviceConstants.CultureHeader, StringComparison.OrdinalIgnoreCase))
                && _httpContextAccessor?.HttpContext?.Request?.Headers?.TryGetValue(MicroserviceConstants.CultureHeader, out var culture) == true
                && !string.IsNullOrEmpty(culture))
                headers.Add(MicroserviceConstants.CultureHeader, culture!);

            var newOptions = context.Options.WithHeaders(headers);

            var newContext = new ClientInterceptorContext<TRequest, TResponse>(
                context.Method,
                context.Host,
                newOptions);
            return newContext;
        }
    }

    static IHttpClientBuilder AddGrpcClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TClient>(
        this IServiceCollection services,
        GrpcClientOptions? options = null)
        where TClient : class
    {
        IHttpClientBuilder builder;
        if (string.IsNullOrWhiteSpace(options?.Name))
            builder = services.AddGrpcClient<TClient>(o => options?.ClientOptionsAction?.Invoke(o));
        else
            builder = services.AddGrpcClient<TClient>(options.Name, o => options?.ClientOptionsAction?.Invoke(o));

        if (options?.ChannelOptionsAction != null)
            builder = builder.ConfigureChannel(options.ChannelOptionsAction);

        if (options?.ContextPropagationOptionsAction != null)
            builder.EnableCallContextPropagation(options.ContextPropagationOptionsAction);

        return builder;
    }
}
