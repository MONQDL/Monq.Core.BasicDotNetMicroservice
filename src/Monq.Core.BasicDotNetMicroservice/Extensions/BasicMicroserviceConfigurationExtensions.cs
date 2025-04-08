using App.Metrics;
using App.Metrics.AspNetCore;
using App.Metrics.Formatters.Prometheus;
using Consul;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monq.Core.BasicDotNetMicroservice.Configuration;
using Monq.Core.BasicDotNetMicroservice.Filters;
using Monq.Core.BasicDotNetMicroservice.Helpers;
using Monq.Core.BasicDotNetMicroservice.Models;
using Monq.Core.HttpClientExtensions;
using System;
using System.IO;
using System.Linq;
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
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <param name="consulConfigurationOptions">The configuration options.</param>
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
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <param name="consulConfigurationOptions">The configuration options.</param>
    /// <returns>The same <see cref="IHostBuilder"/> for chaining.</returns>
    public static IHostBuilder ConfigureBasicConsoleMicroservice(
        this IHostBuilder hostBuilder,
        ConsulConfigurationOptions? consulConfigurationOptions = null)
    {
        hostBuilder.ConfigureHostConfiguration(config =>
            config.AddEnvironmentVariables(prefix: "ASPNETCORE_"));
        hostBuilder.ConfigureBasicMicroserviceCore(consulConfigurationOptions);
        hostBuilder.ConfigureServices((hostContext, services) =>
            services.AddConsoleMetrics(hostContext));
        hostBuilder.UseConsoleLifetime();
        return hostBuilder;
    }

    /// <summary>
    /// Configure microservice using a configuration from Consul.
    /// File <c>appsettings.Development.json</c> is allowed.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <param name="configOptions">The configuration options.</param>
    /// <returns>The same <see cref="IHostBuilder"/> for chaining.</returns>
    public static IHostBuilder ConfigureConsul(this IHostBuilder hostBuilder, ConsulConfigurationOptions? configOptions = null)
    {
        hostBuilder.ConfigureAppConfiguration((hostContext, config) =>
            ConfigureConsul(hostContext.Configuration, config, configOptions, hostContext.HostingEnvironment));
        return hostBuilder;
    }

    /// <summary>
    /// Load configuration from Consul.
    /// </summary>
    /// <param name="configBuilder">The configuration builder.</param>
    /// <param name="environment">The environment.</param>
    /// <param name="configOptions">The configuration options.</param>
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
    /// Configure Serilog logging.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <returns>The same <see cref="IHostBuilder"/> for chaining.</returns>
    public static IHostBuilder ConfigureSerilogLogging(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureLogging(LoggerEnvironment.Configure);
        return hostBuilder;
    }

    /// <summary>
    /// Configure the access point to the microservice version info /api/version.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <returns>The same <see cref="IHostBuilder"/> for chaining.</returns>
    public static IHostBuilder UseVersionApiPoint(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices(services => services.AddSingleton<IStartupFilter, VersionPointStartupFilter>());
        return hostBuilder;
    }

    /// <summary>
    /// Configure authorization policies.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <returns>The same <see cref="IHostBuilder"/> for chaining.</returns>
    public static IHostBuilder ConfigureAuthorizationPolicies(this IHostBuilder hostBuilder)
    {
#if NET7_0_OR_GREATER
        hostBuilder
            .ConfigureServices(services
                => services.AddAuthorizationBuilder()
                    .AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser())
                    .AddPolicy(AuthConstants.AuthorizationScopes.Read, policyAdmin => policyAdmin.RequireScope("read", "write"))
                    .AddPolicy(AuthConstants.AuthorizationScopes.Write, policyAdmin => policyAdmin.RequireScope("write"))
                    .AddPolicy(AuthConstants.AuthorizationScopes.SmonAdmin, policyAdmin => policyAdmin.RequireScope("smon-admin"))
                    .AddPolicy(AuthConstants.AuthorizationScopes.CloudAdmin, policyAdmin => policyAdmin.RequireScope("cloud-admin")));
        return hostBuilder;
#else
            hostBuilder
                .ConfigureServices(services =>
                {
                    services.AddAuthorization(options =>
                    {
                        options.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
                        options.AddPolicy(AuthConstants.AuthorizationScopes.Read, policyAdmin => policyAdmin.RequireScope("read", "write"));
                        options.AddPolicy(AuthConstants.AuthorizationScopes.Write, policyAdmin => policyAdmin.RequireScope("write"));
                        options.AddPolicy(AuthConstants.AuthorizationScopes.SmonAdmin, policyAdmin => policyAdmin.RequireScope("smon-admin"));
                        options.AddPolicy(AuthConstants.AuthorizationScopes.CloudAdmin, policyAdmin => policyAdmin.RequireScope("cloud-admin"));
                    });
                });
            return hostBuilder;
#endif
    }

    /// <summary>
    /// Configure the metrics and health checks.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <returns>The same <see cref="IWebHostBuilder"/> for chaining.</returns>
    public static IWebHostBuilder ConfigureMetricsAndHealth(this IWebHostBuilder hostBuilder)
    {
        hostBuilder
            .ConfigureHealthWithDefaults(builder => { })
            .ConfigureMetricsWithDefaults((builderContext, metricsBuilder) =>
            {
                metricsBuilder.OutputMetrics.AsPrometheusPlainText();

                var metricsConfig = builderContext.Configuration.GetSection(MicroserviceConstants.MetricsConfiguration.Metrics);
                var metricsOptions = new MetricsConfigurationOptions();
                metricsConfig.Bind(metricsOptions);

                if (metricsOptions.ReportingInfluxDb.InfluxDb.BaseUri != null)
                    metricsBuilder.Report.ToInfluxDb(metricsOptions.ReportingInfluxDb);
            })
            .UseMetricsWebTracking()
            .UseMetrics(options
                => options.EndpointOptions = endpointsOptions =>
                {
                    endpointsOptions.MetricsTextEndpointOutputFormatter = Metrics.Instance.OutputMetricsFormatters.OfType<MetricsPrometheusTextOutputFormatter>().First();
                    endpointsOptions.MetricsEndpointOutputFormatter = Metrics.Instance.OutputMetricsFormatters.OfType<MetricsPrometheusTextOutputFormatter>().First();
                })
            .UseSystemMetrics();

        return hostBuilder;
    }

    /// <summary>
    /// Load custom certificates.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <param name="certsDir">Certificates directory that will be used to load custom certificates. 
    /// This option has highest priority on the ENV variable.</param>
    /// <returns></returns>
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
                    var certificate = new X509Certificate2(cerFileName);
                    store.Add(certificate); //where cert is an X509Certificate object
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
        hostBuilder.UseVersionApiPoint();
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
        });

        return hostBuilder;
    }

    static void ConfigureConsul(
        this IConfiguration configuration,
        IConfigurationBuilder configBuilder,
        ConsulConfigurationOptions? configOptions,
        IHostEnvironment env)
    {
        // Применяем переменную APPLICATION_NAME из переменных среды,
        // если она не задана, то используем встроенное значение env.ApplicationName.
        var applicationName = env.ApplicationName;
        if (!string.IsNullOrEmpty(configuration[ApplicationNameEnv]))
            applicationName = configuration[ApplicationNameEnv];

        configOptions ??= new ConsulConfigurationOptions();

        // Если находимся в DEV, то используем appsettings.development.json
        if (env.IsDevelopment())
            return;

        // Загружаем конфигурацию подключения в Consul из примонтированного файла.
        var consulConfigFile = ConsulConfigFileDefault;
        if (!File.Exists(ConsulConfigFileDefault) && !string.IsNullOrEmpty(configuration[ConsulConfigFileEnv]))
        {
            Console.WriteLine("Consul connection file detected by environment variable.");
            consulConfigFile = configuration[ConsulConfigFileEnv];
        }
        configBuilder.AddJsonFile(consulConfigFile, optional: false, reloadOnChange: false);

        var consulClientConfiguration = new ConsulClientConfiguration();

        var consulBuilder = new ConfigurationBuilder();
        consulBuilder.SetBasePath(env.ContentRootPath);
        consulBuilder.AddJsonFile(consulConfigFile, optional: false, reloadOnChange: false);

        var consulConfig = consulBuilder.Build();

        if (string.IsNullOrEmpty(consulConfig[ConsulConfigFileSectionName + ":Address"]))
            throw new ConsulConfigurationException($"Failed to load Consul server address from {consulConfigFile}. The wrong data format may have been used.");

        var consulRoot = env.EnvironmentName?.ToLower();
        if (!string.IsNullOrEmpty(consulConfig[ConsulConfigFileSectionName + ":RootFolder"]))
            consulRoot = consulConfig[ConsulConfigFileSectionName + ":RootFolder"].ToLower();

        consulConfig
            .GetSection(ConsulConfigFileSectionName)
            .Bind(consulClientConfiguration);

        var appsettingsFileName = string.IsNullOrEmpty(configOptions.AppsettingsFileName) ? AppsettingsFile : configOptions.AppsettingsFileName;

        if (configOptions.UseCommonAppsettings)
        {
            var commonAppsettingsFileName = string.IsNullOrEmpty(configOptions.CommonAppsettingsFileName) ? CommonAppsettingsFile : configOptions.CommonAppsettingsFileName;
            configBuilder
                .AddConsul(
                    $"{consulRoot}/{commonAppsettingsFileName}",
                    options => ConfigureConsulOptions(options, consulClientConfiguration));
        }
        // Включение конфигурации для микросервиса.
        configBuilder
            .AddConsul(
                $"{consulRoot}/{applicationName?.ToLower()}/{appsettingsFileName}",
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

    /// <summary>
    /// Add GC metrics, CPU metrics and memory usage metrics.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <returns></returns>
    static IWebHostBuilder UseSystemMetrics(this IWebHostBuilder hostBuilder)
    {
        hostBuilder
            .ConfigureServices((builderContext, services) =>
            {
                var metricsConfig = builderContext.Configuration.GetSection(MicroserviceConstants.MetricsConfiguration.Metrics);
                var metricsOptions = new MetricsConfigurationOptions();
                metricsConfig.Bind(metricsOptions);

                if (metricsOptions.AddSystemMetrics)
                    services.AddAppMetricsCollectors();
            });

        return hostBuilder;
    }
}
