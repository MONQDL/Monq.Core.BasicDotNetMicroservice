using App.Metrics;
using App.Metrics.Formatters.Prometheus;
using App.Metrics.Reporting.Http;
using App.Metrics.Reporting.InfluxDB;
using IdentityServer4.AccessTokenValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monq.Core.BasicDotNetMicroservice.Configuration;
using Monq.Core.BasicDotNetMicroservice.Services.Implementation;
using System;

namespace Monq.Core.BasicDotNetMicroservice.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Выполнить конфигурацию аутентификации на проекте из провайдера <paramref name="configuration"/>.
        /// </summary>
        public static IServiceCollection ConfigureSMAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var authConfig = configuration.GetSection("Authentication");

            if (!bool.TryParse(authConfig[AuthConstants.AuthenticationConfiguration.RequireHttpsMetadata], out var requireHttps))
                requireHttps = false;
            if (!bool.TryParse(authConfig[AuthConstants.AuthenticationConfiguration.EnableCaching], out var enableCaching))
                enableCaching = true;

            services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddOAuth2Introspection(IdentityServerAuthenticationDefaults.AuthenticationScheme, x =>
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
        /// Добавить отправку метрик на проекте.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="hostContext"></param>
        /// <returns></returns>
        public static IServiceCollection AddDataAsyncMetrics(this IServiceCollection services, HostBuilderContext hostContext)
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

            AddSystemMetrics(services, metricsOptions);

            services.AddHostedService(
                serviceProvider =>
                    new MetricsReporterService(
                        serviceProvider.GetService<IMetricsRoot>(),
                        serviceProvider.GetService<ILoggerFactory>(),
                        metricsOptions));

            return services;
        }

        /// <summary>
        /// Добавить отправку системных метрик.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="metricsOptions"></param>
        static void AddSystemMetrics(IServiceCollection services, MetricsConfigurationOptions metricsOptions)
        {
            if (metricsOptions.AddSystemMetrics) services.AddAppMetricsCollectors();
        }

        /// <summary>
        /// Добавить отправку метрик в InfluxDb.
        /// </summary>
        /// <param name="metricsBuilder"></param>
        /// <param name="influxDbOptions"></param>
        static void AddInfluxDb(this IMetricsBuilder metricsBuilder, MetricsReportingInfluxDbOptions influxDbOptions)
        {
            if (influxDbOptions.InfluxDb.BaseUri == null) return;

            metricsBuilder.Report.ToInfluxDb(influxDbOptions);
        }

        /// <summary>
        /// Добавить отправку метрик через Http.
        /// </summary>
        /// <param name="metricsBuilder"></param>
        /// <param name="hostEnvironment"></param>
        /// <param name="httpOptions"></param>
        static void AddOverHttp(this IMetricsBuilder metricsBuilder, IHostEnvironment hostEnvironment, MetricsReportingHttpOptions httpOptions)
        {
            if (httpOptions.HttpSettings.RequestUri == null) return;

            var jobName = hostEnvironment.ApplicationName.Replace('/', '.');
            var requestUrl = $"{httpOptions.HttpSettings.RequestUri.AbsoluteUri.TrimEnd('/')}/job/{jobName}";

            httpOptions.HttpSettings.RequestUri = new Uri(requestUrl);
            httpOptions.MetricsOutputFormatter = new MetricsPrometheusTextOutputFormatter();

            metricsBuilder.Report.OverHttp(httpOptions);
        }
    }
}