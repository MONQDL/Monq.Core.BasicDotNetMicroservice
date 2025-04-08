using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monq.Core.BasicDotNetMicroservice.Enrichers.FromHttpContextHeader;
using Serilog;
using Serilog.Events;
using System;
using System.Reflection;

namespace Monq.Core.BasicDotNetMicroservice.Helpers;

/// <summary>
/// Helper class to work with logging.
/// </summary>
public static class LoggerEnvironment
{
    static string? MicroserviceName { get; set; }

    const string PodName = "HOSTNAME";

    /// <summary>
    /// Configure logging.
    /// </summary>
    /// <param name="hostContext">The host builder context.</param>
    /// <param name="logging">The logging builder.</param>
    public static void Configure(HostBuilderContext hostContext, ILoggingBuilder logging)
    {
        if (hostContext == null)
            throw new ArgumentNullException(nameof(hostContext), $"{nameof(hostContext)} is null.");

        if (logging == null)
            throw new ArgumentNullException(nameof(logging), $"{nameof(logging)} is null.");

        logging.ClearProviders();

        var configuration = hostContext.Configuration;
        var env = hostContext.HostingEnvironment;

        ReadVariables();

        var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.FromHttpContextHeader(MicroserviceConstants.EventIdHeader, MicroserviceConstants.EventIdPropertyName)
            .Enrich.FromHttpContextHeader(MicroserviceConstants.UserspaceIdHeader, MicroserviceConstants.UserspaceIdPropertyName)
            .ReadFrom.Configuration(configuration)
            .Enrich.WithProperty(LoggerFieldNames.Application, GetAssemblyName())
            .Enrich.WithProperty(LoggerFieldNames.Microservice, MicroserviceName)
            .Enrich.WithProperty(LoggerFieldNames.AppVersion, MicroserviceInfo.GetEntryPointAssembleVersion())
            .Enrich.WithProperty(LoggerFieldNames.AppEnvironment, env.EnvironmentName)
            .Enrich.WithProperty(LoggerFieldNames.HostName, Environment.GetEnvironmentVariable(PodName))
            .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss zzz} {Level:u3}] {Scope} {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        logging.AddSerilog(logger);
    }

    static string? GetAssemblyName() => Assembly.GetEntryAssembly()?.GetName().Name;

    static void ReadVariables()
    {
        MicroserviceName = Environment.GetEnvironmentVariable("ASPNETCORE_" + MicroserviceConstants.HostConfiguration.ApplicationNameEnv);
    }

    /// <summary>
    /// Logger field names.
    /// </summary>
    public static class LoggerFieldNames
    {
        /// <summary>
        /// Microservice environment.
        /// </summary>
        public const string AppEnvironment = "AppEnvironment";

        /// <summary>
        /// Название программы в составе микросервиса.
        /// </summary>
        public const string Application = "Application";

        /// <summary>
        /// Microservice version.
        /// </summary>
        public const string AppVersion = "AppVersion";

        /// <summary>
        /// Microservice name from the APPLICATION_NAME environment variable.
        /// </summary>
        public const string Microservice = "Microservice";

        /// <summary>
        /// The Id of the user who sent the request.
        /// </summary>
        public const string UserId = "UserId";

        /// <summary>
        /// The name of the user who sent the request.
        /// </summary>
        public const string UserName = "UserName";

        /// <summary>
        /// Kubernetes pod name.
        /// </summary>
        public const string HostName = "HostName";
    }
}
