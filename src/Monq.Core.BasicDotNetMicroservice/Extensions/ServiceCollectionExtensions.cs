﻿using App.Metrics;
using App.Metrics.Formatters.Prometheus;
using App.Metrics.Reporting.Http;
using App.Metrics.Reporting.InfluxDB;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monq.Core.BasicDotNetMicroservice.Configuration;
using Monq.Core.BasicDotNetMicroservice.Services.Implementation;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Monq.Core.BasicDotNetMicroservice.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configures authentication.
        /// </summary>
        /// <param name="services">IServiceCollection to add the services to.</param>
        /// <param name="configuration">The configuration being bound.</param>
        /// <returns>The Microsoft.Extensions.DependencyInjection.IServiceCollection so that additional
        /// calls can be chained.</returns>
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
        /// Configures sending message handlers, tasks, system and GC events metrics
        /// over http and into InfluxDb.
        /// </summary>
        /// <param name="services">IServiceCollection to add the services to.</param>
        /// <param name="hostContext">Context containing the common services on the IHost.</param>
        /// <returns>The Microsoft.Extensions.DependencyInjection.IServiceCollection so that additional
        /// calls can be chained.</returns>
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
            if (httpOptions.HttpSettings.RequestUri == null) return;

            var jobName = hostEnvironment.ApplicationName.Replace('/', '.');
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
        /// Add preconfigred Grpc service with configured Address and CallCredentials.
        /// </summary>
        /// <typeparam name="TClient">The type of the gRPC client. The type specified will be registered in the service collection as
        /// a transient service.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/>.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        public static IHttpClientBuilder AddGrpcPreConfiguredClient<TClient>(this IServiceCollection services, IConfiguration configuration)
            where TClient : class
        {
            var builder = services.AddGrpcClient<TClient>(o =>
            {
                o.Address = new Uri(configuration.GetValue<string>(nameof(AppConfiguration.BaseUri)));
            })
            .ConfigureChannel(o =>
            {
                o.UnsafeUseInsecureChannelCallCredentials = true;
            })
            .AddCallCredentials((context, metadata, provider) =>
            {
                var httpContext = provider.GetRequiredService<IHttpContextAccessor>();
                if (httpContext.HttpContext.Request.Headers.TryGetValue(HttpRequestHeader.Authorization.ToString(), out var token) && !string.IsNullOrEmpty(token))
                    metadata.Add(HttpRequestHeader.Authorization.ToString(), token);

                if (httpContext.HttpContext.Request.Headers.TryGetValue(MicroserviceConstants.UserspaceIdHeader, out var userspaceId)
                    && !string.IsNullOrEmpty(userspaceId))
                    metadata.Add(MicroserviceConstants.UserspaceIdHeader, userspaceId);

                return Task.CompletedTask;
            });
            return builder;
        }
    }
}