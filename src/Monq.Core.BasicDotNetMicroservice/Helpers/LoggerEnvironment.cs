using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monq.Core.BasicDotNetMicroservice.Enrichers.FromHttpContextHeader;
using Serilog;
using Serilog.Events;
using System.Reflection;

namespace Monq.Core.BasicDotNetMicroservice.Helpers;

/// <summary>
/// Helper class to configure Serilog logging with microservice-specific enrichers and properties.
/// </summary>
public static class LoggerEnvironment
{
    static string? MicroserviceName { get; set; }

    const string PodName = "HOSTNAME";

    /// <summary>
    /// Configures Serilog with minimum debug level, Microsoft overrides, activity trace enrichment,
    /// user enrichment, HTTP context header enrichment, configuration binding, and console output
    /// with TraceId/SpanId in the template.
    /// </summary>
    /// <param name="hostContext">The host builder context containing configuration and hosting environment.</param>
    /// <param name="services">The service provider to resolve enrichers from.</param>
    /// <param name="logger">The Serilog logger configuration to enrich.</param>
    public static void Configure(HostBuilderContext hostContext, IServiceProvider services, LoggerConfiguration logger)
    {
        if (hostContext == null)
            throw new ArgumentNullException(nameof(hostContext), $"{nameof(hostContext)} is null.");

        if (services == null)
            throw new ArgumentNullException(nameof(services), $"{nameof(services)} is null.");

        if (logger == null)
            throw new ArgumentNullException(nameof(logger), $"{nameof(logger)} is null.");

        var configuration = hostContext.Configuration;
        var env = hostContext.HostingEnvironment;

        ReadVariables();

        logger
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.With(services.GetRequiredService<Enrichers.ActivityTrace.ActivityTraceEnricher>())
            .Enrich.With(services.GetRequiredService<Enrichers.User.UserEnricher>())
            .Enrich.FromHttpContextHeader(MicroserviceConstants.UserspaceIdHeader, MicroserviceConstants.UserspaceIdPropertyName)
            .ReadFrom.Configuration(configuration)
            .Enrich.WithProperty(LoggerFieldNames.Application, GetAssemblyName())
            .Enrich.WithProperty(LoggerFieldNames.Microservice, MicroserviceName)
            .Enrich.WithProperty(LoggerFieldNames.AppVersion, MicroserviceVersionInfo.GetEntryPointAssemblyVersion())
            .Enrich.WithProperty(LoggerFieldNames.AppEnvironment, env.EnvironmentName)
            .Enrich.WithProperty(LoggerFieldNames.HostName, Environment.GetEnvironmentVariable(PodName))
            .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss zzz} {Level:u3} {TraceId}/{SpanId}] {Scope} {Message:lj}{NewLine}{Exception}");
    }

    static string? GetAssemblyName() => Assembly.GetEntryAssembly()?.GetName().Name;

    static void ReadVariables() =>
        MicroserviceName = Environment.GetEnvironmentVariable("ASPNETCORE_" + MicroserviceConstants.HostConfiguration.ApplicationNameEnv);

    /// <summary>
    /// Standard log property names used across the microservice logging pipeline.
    /// </summary>
    public static class LoggerFieldNames
    {
        /// <summary>Hosting environment name (e.g. Development, Production).</summary>
        public const string AppEnvironment = "AppEnvironment";
        /// <summary>Entry assembly name of the application.</summary>
        public const string Application = "Application";
        /// <summary>Application version from the entry assembly.</summary>
        public const string AppVersion = "AppVersion";
        /// <summary>Microservice name from the APPLICATION_NAME environment variable.</summary>
        public const string Microservice = "Microservice";
        /// <summary>Authenticated user ID extracted from the <c>sub</c> claim.</summary>
        public const string UserId = "UserId";
        /// <summary>Authenticated user name from the principal identity.</summary>
        public const string UserName = "UserName";
        /// <summary>Kubernetes pod name from the HOSTNAME environment variable.</summary>
        public const string HostName = "HostName";
    }
}
