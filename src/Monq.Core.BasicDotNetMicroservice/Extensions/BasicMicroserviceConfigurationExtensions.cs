using App.Metrics;
using App.Metrics.AspNetCore;
using App.Metrics.Formatters.Prometheus;
using App.Metrics.Reporting.InfluxDB;
using Consul;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monq.Core.BasicDotNetMicroservice.Configuration;
using Monq.Core.BasicDotNetMicroservice.Filters;
using Monq.Core.BasicDotNetMicroservice.Helpers;
using Monq.Core.BasicDotNetMicroservice.Models;
using Monq.Core.HttpClientExtensions;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Winton.Extensions.Configuration.Consul;
using static Monq.Core.BasicDotNetMicroservice.MicroserviceConstants.HostConfiguration;

namespace Monq.Core.BasicDotNetMicroservice.Extensions
{
    public static class BasicMicroserviceConfigurationExtensions
    {
        /// <summary>
        /// Выполнить конфигурацию базового микросервиса, которая включает в себя конфигурацию Consul,
        /// а так же конфигурацию логирования.
        /// </summary>
        /// <param name="hostBuilder">The host builder.</param>
        /// <param name="consulConfigurationOptions">The configuration options.</param>
        public static IHostBuilder ConfigureBasicMicroservice(
            this IHostBuilder hostBuilder,
            ConsulConfigurationOptions? consulConfigurationOptions = null)
        {
            hostBuilder.ConfigureCustomCertificates();
            hostBuilder.ConfigureConsul(consulConfigurationOptions);
            hostBuilder.ConfigureSerilogLogging();
            hostBuilder.UseVersionApiPoint();
            hostBuilder.ConfigureAuthorizationPolicies();
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

        /// <summary>
        /// Выполнить конфигурацию сервиса, используя файл конфигурации из Consul.
        /// Позволяется использовать файл appsettings.Development.json
        /// </summary>
        /// <param name="hostBuilder">The host builder.</param>
        /// <param name="configOptions">The configuration options.</param>
        public static IHostBuilder ConfigureConsul(this IHostBuilder hostBuilder, ConsulConfigurationOptions? configOptions = null)
        {
            hostBuilder
                .ConfigureAppConfiguration((builderContext, config) =>
                    ConfigureConsul(builderContext.Configuration, config, configOptions, builderContext.HostingEnvironment));
            return hostBuilder;
        }

        /// <summary>
        /// Загрузить конфигурацию из Consul.
        /// </summary>
        /// <param name="configBuilder">The configuration builder.</param>
        /// <param name="environment">The environment.</param>
        /// <param name="configOptions">The configuration options.</param>
        /// <returns></returns>
        public static IConfigurationBuilder ConfigureConsul(
            this IConfigurationBuilder configBuilder,
            IHostEnvironment environment,
            ConsulConfigurationOptions? configOptions = null)
        {
            ConfigureConsul(configBuilder.Build(), configBuilder, configOptions, environment);

            return configBuilder;
        }

        /// <summary>
        /// Configures the serilog logging.
        /// </summary>
        /// <param name="hostBuilder">The host builder.</param>
        public static IHostBuilder ConfigureSerilogLogging(this IHostBuilder hostBuilder)
        {
            hostBuilder
                .ConfigureAppConfiguration((builderContext, config) =>
                {
                    var env = builderContext.HostingEnvironment;
                    var configuration = config.Build();
                    LoggerEnvironment.Configure(env, configuration);
                });
            hostBuilder.UseSerilog();

            return hostBuilder;
        }

        /// <summary>
        /// Выполнить конфигурацию точки доступа с информацией по версии микросервиса /api/version.
        /// </summary>
        /// <param name="hostBuilder">The host builder.</param>
        public static IHostBuilder UseVersionApiPoint(this IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices(services => services.AddSingleton<IStartupFilter, VersionPointStartupFilter>());

            return hostBuilder;
        }

        /// <summary>
        /// Выполнить конфигурацию политик авторизации СМ.
        /// </summary>
        /// <param name="hostBuilder">The host builder.</param>
        /// <returns></returns>
        public static IHostBuilder ConfigureAuthorizationPolicies(this IHostBuilder hostBuilder)
        {
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
        }

        /// <summary>
        /// Configures the metrics and health checks.
        /// </summary>
        /// <param name="hostBuilder">The host builder.</param>
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

                    if (metricsOptions.AddSystemMetrics)
                    {
                        var services = hostBuilder.ConfigureServices(services =>
                        {
                            services.AddAppMetricsCollectors();
                        });
                    }
                })
                .UseMetricsWebTracking()
                .UseMetrics(options =>
                {
                    options.EndpointOptions = endpointsOptions =>
                    {
                        endpointsOptions.MetricsTextEndpointOutputFormatter = Metrics.Instance.OutputMetricsFormatters.OfType<MetricsPrometheusTextOutputFormatter>().First();
                        endpointsOptions.MetricsEndpointOutputFormatter = Metrics.Instance.OutputMetricsFormatters.OfType<MetricsPrometheusTextOutputFormatter>().First();
                    };
                });

            return hostBuilder;
        }

        /// <summary>
        /// Load custom certificates from the 
        /// </summary>
        /// <param name="hostBuilder"></param>
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

            using (X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser))
            {
                foreach (var cerFileName in Directory.EnumerateFiles(certsDir))
                {
                    store.Open(OpenFlags.ReadWrite);
                    try
                    {
                        X509Certificate2 certificate = new X509Certificate2(cerFileName);
                        store.Add(certificate); //where cert is an X509Certificate object
                        Console.WriteLine($"Successfully installed {cerFileName}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error while installing {cerFileName}. Detailes: {e.Message}");
                    }
                }
            }

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
    }
}
