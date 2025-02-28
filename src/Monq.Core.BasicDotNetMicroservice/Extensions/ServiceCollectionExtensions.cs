using App.Metrics;
using App.Metrics.Formatters.Prometheus;
using App.Metrics.Reporting.Http;
using App.Metrics.Reporting.InfluxDB;
using Calzolari.Grpc.AspNetCore.Validation;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Monq.Core.BasicDotNetMicroservice.Configuration;
using Monq.Core.BasicDotNetMicroservice.Services.Implementation;
using Monq.Core.BasicDotNetMicroservice.Validation;
using Monq.Core.HttpClientExtensions;
using System;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using static Grpc.Core.Interceptors.Interceptor;

namespace Monq.Core.BasicDotNetMicroservice.Extensions;

/// <summary>
/// <see cref="IServiceCollection"/> extensions.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures authentication.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">The configuration being bound.</param>
    /// <returns><see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection ConfigureSMAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var authConfig = configuration.GetSection("Authentication");

        if (!bool.TryParse(authConfig[AuthConstants.AuthenticationConfiguration.RequireHttpsMetadata], out var requireHttps))
            requireHttps = false;
        if (!bool.TryParse(authConfig[AuthConstants.AuthenticationConfiguration.EnableCaching], out var enableCaching))
            enableCaching = true;

        services.AddAuthentication(AuthConstants.AuthenticationScheme)
            .AddOAuth2Introspection(AuthConstants.AuthenticationScheme, x =>
            {
                x.Authority = authConfig[AuthConstants.AuthenticationConfiguration.Authority];
                x.ClientId = authConfig[AuthConstants.AuthenticationConfiguration.ScopeName];
                x.ClientSecret = authConfig[AuthConstants.AuthenticationConfiguration.ScopeSecret];
                x.EnableCaching = enableCaching;
                x.CacheDuration = TimeSpan.FromMinutes(5);
                x.NameClaimType = "fullName";
                x.DiscoveryPolicy.RequireHttps = requireHttps;
            });

        return services;
    }

    /// <summary>
    /// Configures sending message handlers, tasks, system and GC events metrics
    /// over http and into InfluxDb.
    /// </summary>
    /// <param name="services">IServiceCollection to add the services to.</param>
    /// <param name="hostContext">Context containing the common services on the IHost.</param>
    /// <returns><see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddConsoleMetrics(this IServiceCollection services, HostBuilderContext hostContext)
    {
        var metricsBuilder = AppMetrics.CreateDefaultBuilder()
            .Configuration.Configure(options =>
            {
                options.Enabled = true;
                options.ReportingEnabled = true;
            });
        metricsBuilder.OutputMetrics.AsPrometheusPlainText();

        var metricsConfig = hostContext.Configuration.GetSection(MicroserviceConstants.MetricsConfiguration.Metrics);
        var metricsOptions = new MetricsConfigurationOptions();
        metricsConfig.Bind(metricsOptions);

        metricsBuilder.AddInfluxDb(metricsOptions.ReportingInfluxDb);
        metricsBuilder.AddOverHttp(hostContext.HostingEnvironment, metricsOptions.ReportingOverHttp);

        var metrics = metricsBuilder.Build();

        services.AddSingleton(metrics.OutputEnvFormatters);
        services.AddSingleton<IMetrics>(metrics);
        services.AddSingleton(metrics);
        services.AddMetricsReporter(metricsOptions);

        if (metricsOptions.AddSystemMetrics)
            services.AddAppMetricsCollectors();

        return services;
    }

    /// <summary>
    /// Adds InfluxDB reporting.
    /// </summary>
    /// <param name="metricsBuilder">IMetricBuilder to add InfluxDB reporting to.</param>
    /// <param name="influxDbOptions">Configuration options for InfluxDB reporting.</param>
    static void AddInfluxDb(this IMetricsBuilder metricsBuilder, MetricsReportingInfluxDbOptions influxDbOptions)
    {
        if (influxDbOptions.InfluxDb.BaseUri == null) return;

        metricsBuilder.Report.ToInfluxDb(influxDbOptions);
    }

    /// <summary>
    /// Adds HTTP reporting.
    /// </summary>
    /// <param name="metricsBuilder">IMetricBuilder to add InfluxDB reporting to.</param>
    /// <param name="hostEnvironment">Provides information about the hosting environment an application is running in.</param>
    /// <param name="httpOptions">Configuration options of HTTP reporting. </param>
    static void AddOverHttp(this IMetricsBuilder metricsBuilder, IHostEnvironment hostEnvironment, MetricsReportingHttpOptions httpOptions)
    {
        if (httpOptions.HttpSettings.RequestUri == null)
            return;

        string jobName;
        var hostName = Environment.GetEnvironmentVariable("HOSTNAME");
        if (!string.IsNullOrEmpty(hostName))
            jobName = hostName.Replace('/', '.');
        else
            jobName = hostEnvironment.ApplicationName.Replace('/', '.');
        var requestUrl = $"{httpOptions.HttpSettings.RequestUri.AbsoluteUri.TrimEnd('/')}/job/{jobName}";

        httpOptions.HttpSettings.RequestUri = new Uri(requestUrl);
        httpOptions.MetricsOutputFormatter = new MetricsPrometheusTextOutputFormatter();

        metricsBuilder.Report.OverHttp(httpOptions);
    }

    /// <summary>
    /// Add metrics reporter.
    /// </summary>
    /// <param name="services">IServiceCollection to add the services to.</param>
    /// <param name="metricsOptions">Metrics configuration options.</param>
    /// <returns></returns>
    static IServiceCollection AddMetricsReporter(this IServiceCollection services, MetricsConfigurationOptions metricsOptions)
    {
        var metricsReporterOptions = new MetricsReporterOptions(metricsOptions);
        services.AddSingleton(metricsReporterOptions);
        services.AddHostedService<MetricsReporterService>();

        return services;
    }

    /// <summary>
    /// Add gRPC request validation.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns><see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddGrpcRequestValidation(this IServiceCollection services)
    {
        services.AddGrpc(options =>
        {
            options.EnableMessageValidation();
        });

        services.AddSingleton<IValidatorErrorMessageHandler>(new DefaultValidatorMessageHandler());
        services.AddGrpcValidation();

        return services;
    }

    static readonly Action<GrpcClientOptions, IConfiguration> _configureGrpcClient = (o, configuration) =>
    {
        o.ClientOptionsAction = (clientOptions) =>
        {
            clientOptions.Address = new Uri(configuration.GetValue<string>(nameof(AppConfiguration.BaseUri)) ?? "http://localhost");
        };
        o.ChannelOptionsAction = (channelOptions) =>
        {
            channelOptions.UnsafeUseInsecureChannelCallCredentials = true;
            channelOptions.MaxReceiveMessageSize = 51 * 1024 * 1024; // 51 Mb.
        };
        o.ContextPropagationOptionsAction = (propagationOptions) =>
        {
            propagationOptions.SuppressContextNotFoundErrors = true;
        };
    };

    /// <summary>
    /// Add preconfigured gRPC client with configured address, channel options, call credentials and context propagation.
    /// </summary>
    /// <typeparam name="TClient">The type of the gRPC client. The type specified will be registered in the service collection as
    /// a transient service.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/>.</param>
    /// <param name="configureOptions">A delegate that is used to configure a <see cref="GrpcClientOptions"/>.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
    public static IHttpClientBuilder AddGrpcPreConfiguredClient<TClient>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<GrpcClientOptions>? configureOptions = null)
        where TClient : class
    {
        var options = new GrpcClientOptions();
        _configureGrpcClient(options, configuration);
        configureOptions?.Invoke(options);

        return services.AddGrpcPreConfiguredClient<TClient>(options);
    }

    /// <summary>
    /// Add preconfigured gRPC client with configured address, channel options, call credentials and context propagation for console application with static authentication.
    /// </summary>
    /// <typeparam name="TClient">The type of the gRPC client. The type specified will be registered in the service collection as
    /// a transient service.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/>.</param>
    /// <param name="configureOptions">A delegate that is used to configure a <see cref="GrpcClientOptions"/>.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
    public static IHttpClientBuilder AddGrpcPreConfiguredConsoleClient<TClient>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<GrpcClientOptions>? configureOptions = null)
        where TClient : class
    {
        var options = new GrpcClientOptions();
        _configureGrpcClient(options, configuration);
        configureOptions?.Invoke(options);

        return services.AddGrpcPreConfiguredConsoleClient<TClient>(options);
    }

    /// <summary>
    /// Add preconfigured gRPC client with configured call credentials.
    /// </summary>
    /// <typeparam name="TClient">The type of the gRPC client. The type specified will be registered in the service collection as
    /// a transient service.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="options">The <see cref="GrpcClientOptions"/>.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
    static IHttpClientBuilder AddGrpcPreConfiguredClient<TClient>(
        this IServiceCollection services,
        GrpcClientOptions? options = null)
        where TClient : class
    {
        services.TryAddTransient<AuthorizationHeaderInterceptor>();

        return services
            .AddGrpcClient<TClient>(options)
            .AddInterceptor<AuthorizationHeaderInterceptor>()
            .AddCallCredentials((context, metadata, provider) =>
            {
                var httpContext = provider.GetRequiredService<IHttpContextAccessor>();
                if (httpContext.HttpContext.Request.Headers.TryGetValue(HttpRequestHeader.Authorization.ToString(), out var token)
                    && !string.IsNullOrEmpty(token))
                    metadata.Add(HttpRequestHeader.Authorization.ToString(), token);

                return Task.CompletedTask;
            });
    }

    /// <summary>
    /// Add preconfigured gRPC client with configured call credentials for console application with static authentication.
    /// </summary>
    /// <typeparam name="TClient">The type of the gRPC client. The type specified will be registered in the service collection as
    /// a transient service.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="options">The <see cref="GrpcClientOptions"/>.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
    static IHttpClientBuilder AddGrpcPreConfiguredConsoleClient<TClient>(
        this IServiceCollection services,
        GrpcClientOptions? options = null)
        where TClient : class
    {
        // REM: for getting an auth token in .AddCallCredentials().
        services.AddHttpClient<RestHttpClient>();

        services.TryAddTransient<AuthorizationHeaderInterceptor>();

        return services
            .AddGrpcClient<TClient>(options)
            .AddInterceptor<AuthorizationHeaderInterceptor>()
            .AddCallCredentials(async (context, metadata, provider) =>
            {
                var httpContextAccessor = provider.GetService<IHttpContextAccessor>();
                if (httpContextAccessor?.HttpContext?.Request?.Headers?.TryGetValue(HttpRequestHeader.Authorization.ToString(), out var token) == true
                    && !string.IsNullOrEmpty(token))
                {
                    metadata.Add(HttpRequestHeader.Authorization.ToString(), token);
                }
                else
                {
                    // HACK: temporary solution. Consider getting an auth token in a special service.
                    var client = provider.GetRequiredService<RestHttpClient>();

                    var tokenResponse = await client.GetAccessToken(false);
                    if (tokenResponse is null)
                        return;
                    const string scheme = "Bearer";
                    var authorizationHeaderValue = new AuthenticationHeaderValue(scheme, tokenResponse.AccessToken);
                    metadata.Add(HttpRequestHeader.Authorization.ToString(), authorizationHeaderValue.ToString());
                }
            });
    }

    public class AuthorizationHeaderInterceptor : Interceptor
    {
        readonly IHttpContextAccessor _httpContextAccessor;

        public AuthorizationHeaderInterceptor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {

            var headers = new Metadata();
            if (context.Options.Headers is not null)
                foreach (var header in context.Options.Headers)
                    headers.Add(header);

            if (!headers.Any(x => x.Key.ToLowerInvariant() == MicroserviceConstants.UserspaceIdHeader.ToLowerInvariant())
                    && _httpContextAccessor?.HttpContext?.Request?.Headers?.TryGetValue(MicroserviceConstants.UserspaceIdHeader, out var userspaceId) == true
                    && !string.IsNullOrEmpty(userspaceId))
                headers.Add(MicroserviceConstants.UserspaceIdHeader, userspaceId);

            if (!headers.Any(x => x.Key.ToLowerInvariant() == MicroserviceConstants.CultureHeader.ToLowerInvariant())
                && _httpContextAccessor?.HttpContext?.Request?.Headers?.TryGetValue(MicroserviceConstants.CultureHeader, out var culture) == true
                && !string.IsNullOrEmpty(culture))
                headers.Add(MicroserviceConstants.CultureHeader, culture);

            var newOptions = context.Options.WithHeaders(headers);

            var newContext = new ClientInterceptorContext<TRequest, TResponse>(
                context.Method,
                context.Host,
                newOptions);

            return base.AsyncUnaryCall(request, newContext, continuation);
        }
    }

    /// <summary>
    /// Add gRPC client.
    /// </summary>
    /// <typeparam name="TClient">The type of the gRPC client. The type specified will be registered in the service collection as
    /// a transient service.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="options">The <see cref="GrpcClientOptions"/>.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
    static IHttpClientBuilder AddGrpcClient<TClient>(
        this IServiceCollection services,
        GrpcClientOptions? options = null)
        where TClient : class
    {
        IHttpClientBuilder builder;
        if (string.IsNullOrWhiteSpace(options?.Name))
            builder = services.AddGrpcClient<TClient>(o =>
            {
                options?.ClientOptionsAction?.Invoke(o);
            });
        else
            builder = services.AddGrpcClient<TClient>(options.Name, o =>
            {
                options?.ClientOptionsAction?.Invoke(o);
            });

        if (options?.ChannelOptionsAction != null)
            builder = builder.ConfigureChannel(options.ChannelOptionsAction);

        if (options?.ContextPropagationOptionsAction != null)
            builder.EnableCallContextPropagation(options.ContextPropagationOptionsAction);

        return builder;
    }
}
