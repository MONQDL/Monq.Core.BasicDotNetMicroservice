using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Monq.Core.BasicDotNetMicroservice.Configuration;
using Monq.Core.BasicDotNetMicroservice.Enrichers.ActivityTrace;
using Monq.Core.BasicDotNetMicroservice.Enrichers.User;
using Monq.Core.BasicDotNetMicroservice.Helpers;
using Monq.Core.BasicDotNetMicroservice.Middleware;
using Monq.Core.HttpClientExtensions;
using Serilog;
using System.Security.Cryptography.X509Certificates;
using Winton.Extensions.Configuration.Consul;
using static Monq.Core.BasicDotNetMicroservice.MicroserviceConstants.HostConfiguration;

namespace Monq.Core.BasicDotNetMicroservice.Extensions;

/// <summary>
/// Extension methods to configure basic microservices.
/// </summary>
public static class BasicMicroserviceConfigurationExtensions
{
    /// <summary>
    /// Configure basic microservice.
    /// Includes Consul, Serilog logging, authorization policies, HTTP client configuration,
    /// metrics (OpenTelemetry) and tracing.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <param name="consulConfigurationOptions">Optional Consul configuration options.</param>
    /// <returns>The same <see cref="IHostBuilder"/> for chaining.</returns>
    public static IHostBuilder ConfigureBasicMicroservice(
        this IHostBuilder hostBuilder,
        ConsulConfigurationOptions? consulConfigurationOptions = null)
    {
        hostBuilder.ConfigureBasicMicroserviceCore(consulConfigurationOptions);
        hostBuilder.ConfigureAuthorizationPolicies();
        return hostBuilder;
    }

    /// <summary>
    /// Configure basic console microservice.
    /// Includes Consul, Serilog logging, HTTP client configuration,
    /// metrics (OpenTelemetry) and tracing. Uses console lifetime for non-web hosting.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <param name="consulConfigurationOptions">Optional Consul configuration options.</param>
    /// <returns>The same <see cref="IHostBuilder"/> for chaining.</returns>
    public static IHostBuilder ConfigureBasicConsoleMicroservice(
        this IHostBuilder hostBuilder,
        ConsulConfigurationOptions? consulConfigurationOptions = null)
    {
        hostBuilder.ConfigureHostConfiguration(config =>
            config.AddEnvironmentVariables(prefix: "ASPNETCORE_"));
        hostBuilder.ConfigureBasicMicroserviceCore(consulConfigurationOptions);
        hostBuilder.ConfigureServices((hostContext, services) =>
        {
            services.AddMonqMetrics();
            services.AddMonqOpenTelemetry(hostContext.Configuration);
        });
        hostBuilder.UseConsoleLifetime();
        return hostBuilder;
    }

    /// <summary>
    /// Configure microservice using a configuration from Consul.
    /// File <c>appsettings.Development.json</c> is allowed.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <param name="configOptions">Optional Consul configuration options.</param>
    /// <returns>The same <see cref="IHostBuilder"/> for chaining.</returns>
    public static IHostBuilder ConfigureConsul(this IHostBuilder hostBuilder, Configuration.ConsulConfigurationOptions? configOptions = null)
    {
        hostBuilder.ConfigureAppConfiguration((hostContext, config) =>
            ConfigureConsul(hostContext.Configuration, config, configOptions, hostContext.HostingEnvironment));
        return hostBuilder;
    }

    /// <summary>
    /// Load configuration from Consul.
    /// </summary>
    /// <param name="configBuilder">The configuration builder.</param>
    /// <param name="environment">The hosting environment.</param>
    /// <param name="configOptions">Optional Consul configuration options.</param>
    /// <returns>The same <see cref="IConfigurationBuilder"/> for chaining.</returns>
    public static IConfigurationBuilder ConfigureConsul(
        this IConfigurationBuilder configBuilder,
        IHostEnvironment environment,
        ConsulConfigurationOptions? configOptions = null)
    {
        ConfigureConsul(configBuilder.Build(), configBuilder, configOptions, environment);
        return configBuilder;
    }

    /// <summary>
    /// Configure Serilog logging with automatic enrichment of TraceId, SpanId, UserId, UserName,
    /// and UserspaceId from the current Activity and HTTP context.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <returns>The same <see cref="IHostBuilder"/> for chaining.</returns>
    public static IHostBuilder ConfigureSerilogLogging(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices(services =>
        {
            services.TryAddSingleton<ActivityTraceEnricher>();
            services.TryAddSingleton<UserEnricher>();
        });

        hostBuilder.UseSerilog((context, services, logger) =>
            LoggerEnvironment.Configure(context, services, logger));

        return hostBuilder;
    }

    /// <summary>
    /// Configures the <c>/api/version</c> endpoint that returns the microservice package version as JSON.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <param name="versionType">
    /// A type from the entry assembly used to determine the package version.
    /// Example: <c>typeof(Program)</c>.
    /// </param>
    /// <returns>The same <see cref="IApplicationBuilder"/> for chaining.</returns>
    public static IApplicationBuilder MapApiVersion(this IApplicationBuilder builder, Type versionType) =>
        builder.UseMiddleware<ApiVersionMiddleware>(versionType);

    /// <summary>
    /// Configure authorization policies.
    /// Adds "Authenticated", "read" and "write" policies based on OAuth2 scopes.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <returns>The same <see cref="IHostBuilder"/> for chaining.</returns>
    public static IHostBuilder ConfigureAuthorizationPolicies(this IHostBuilder hostBuilder)
    {
        hostBuilder
            .ConfigureServices(services
                => services.AddAuthorizationBuilder()
                    .AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser())
                    .AddPolicy(AuthConstants.AuthorizationScopes.Read, policyAdmin => policyAdmin.RequireScope(AuthConstants.AuthorizationScopes.Read, AuthConstants.AuthorizationScopes.Write))
                    .AddPolicy(AuthConstants.AuthorizationScopes.Write, policyAdmin => policyAdmin.RequireScope(AuthConstants.AuthorizationScopes.Write)));

        return hostBuilder;
    }

    /// <summary>
    /// Load custom certificates from the configured certificates directory.
    /// Uses the CERTS_DIR environment variable or defaults to /certs.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <param name="certsDir">
    /// Optional certificates directory. Overrides the CERTS_DIR environment variable and the default /certs path.
    /// </param>
    /// <returns>The same <see cref="IHostBuilder"/> for chaining.</returns>
    public static IHostBuilder ConfigureCustomCertificates(this IHostBuilder hostBuilder, string? certsDir = null)
    {
        if (string.IsNullOrEmpty(certsDir))
            certsDir = Environment.GetEnvironmentVariable(MicroserviceConstants.CertsDirEnv);
        if (string.IsNullOrEmpty(certsDir))
            certsDir = MicroserviceConstants.CertsDirDefault;

        if (!Directory.Exists(certsDir))
            return hostBuilder;

        var certsCount = Directory.EnumerateFiles(certsDir).Count();
        Console.WriteLine($"Installing {certsCount} certificates from {certsDir} ...");

        using (var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser))
        {
            foreach (var cerFileName in Directory.EnumerateFiles(certsDir))
            {
                store.Open(OpenFlags.ReadWrite);
                try
                {
#if NET9_0_OR_GREATER
                    var certificate = X509CertificateLoader.LoadCertificateFromFile(cerFileName);
#else
                    var certificate = new X509Certificate2(cerFileName);
#endif
                    store.Add(certificate);
                    Console.WriteLine($"Successfully installed {cerFileName}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error while installing {cerFileName}. Details: {e.Message}");
                }
            }
        }

        return hostBuilder;
    }

    static IHostBuilder ConfigureBasicMicroserviceCore(
        this IHostBuilder hostBuilder,
        ConsulConfigurationOptions? consulConfigurationOptions = null)
    {
        hostBuilder.ConfigureCustomCertificates();
        hostBuilder.ConfigureConsul(consulConfigurationOptions);
        hostBuilder.ConfigureSerilogLogging();
        hostBuilder.ConfigBasicHttpService(opts =>
        {
            var headerOptions = new RestHttpClientHeaderOptions();
            headerOptions.AddForwardedHeader(MicroserviceConstants.EventIdHeader);
            headerOptions.AddForwardedHeader(MicroserviceConstants.UserspaceIdHeader);
            headerOptions.AddForwardedHeader(MicroserviceConstants.CultureHeader);

            opts.ConfigHeaders(headerOptions);
        });

        hostBuilder.ConfigureServices((context, services) =>
        {
            services.AddHttpContextAccessor();
            services.AddOptions();
            services.AddDistributedMemoryCache();
            services.Configure<AppConfiguration>(context.Configuration);
            services.AddMonqMetrics();
            services.AddMonqOpenTelemetry(context.Configuration);
        });

        return hostBuilder;
    }

    static void ConfigureConsul(
        this IConfiguration configuration,
        IConfigurationBuilder configBuilder,
        ConsulConfigurationOptions? configOptions,
        IHostEnvironment env)
    {
        var applicationName = env.ApplicationName;
        if (!string.IsNullOrEmpty(configuration[ApplicationNameEnv]))
            applicationName = configuration[ApplicationNameEnv];

        configOptions ??= new ConsulConfigurationOptions();

        if (env.IsDevelopment())
            return;

        var consulConfigFile = ConsulConfigFileDefault;
        if (!File.Exists(ConsulConfigFileDefault) && !string.IsNullOrEmpty(configuration[ConsulConfigFileEnv]))
        {
            Console.WriteLine("Consul connection file detected by environment variable.");
            consulConfigFile = configuration[ConsulConfigFileEnv] ?? ConsulConfigFileDefault;
        }

        configBuilder.AddJsonFile(consulConfigFile, optional: false, reloadOnChange: false);

        var consulBuilder = new ConfigurationBuilder();
        consulBuilder.SetBasePath(env.ContentRootPath);
        consulBuilder.AddJsonFile(consulConfigFile, optional: false, reloadOnChange: false);

        var consulConfig = consulBuilder.Build();

        if (string.IsNullOrEmpty(consulConfig[ConsulConfigFileSectionName + ":Address"]))
            throw new ConsulConfigurationException($"Failed to load Consul server address from {consulConfigFile}. The wrong data format may have been used.");

        var consulEnv = env.EnvironmentName?.ToLower();
        var consulRoot = consulConfig[ConsulConfigFileSectionName + ":RootFolder"];
        if (!string.IsNullOrEmpty(consulRoot))
            consulEnv = consulRoot.ToLowerInvariant();

        var consulBindOptions = consulConfig
            .GetSection(ConsulConfigFileSectionName)
            .Get<ConsulClientBindOptions>() ?? new ConsulClientBindOptions();
        var consulClientConfiguration = consulBindOptions.ToConsulClientConfiguration();

        var appsettingsFileName = string.IsNullOrEmpty(configOptions.AppsettingsFileName) ? AppsettingsFile : configOptions.AppsettingsFileName;

        if (configOptions.UseCommonAppsettings)
        {
            var commonAppsettingsFileName = string.IsNullOrEmpty(configOptions.CommonAppsettingsFileName) ? CommonAppsettingsFile : configOptions.CommonAppsettingsFileName;
            configBuilder
                .AddConsul(
                    $"{consulEnv}/{commonAppsettingsFileName}",
                    options => ConfigureConsulOptions(options, consulClientConfiguration));
        }
        configBuilder
            .AddConsul(
                $"{consulEnv}/{applicationName?.ToLower()}/{appsettingsFileName}",
                options => ConfigureConsulOptions(options, consulClientConfiguration));
    }

    static void ConfigureConsulOptions(IConsulConfigurationSource options, ConsulClientConfiguration consulClientConfiguration)
    {
        options.Optional = false;
        options.ConsulConfigurationOptions = x =>
        {
            x.Address = consulClientConfiguration.Address;
            x.Datacenter = consulClientConfiguration.Datacenter;
            x.Token = consulClientConfiguration.Token;
            x.WaitTime = consulClientConfiguration.WaitTime;
        };
        options.ReloadOnChange = false;
    }
}
