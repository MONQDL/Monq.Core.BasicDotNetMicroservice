using App.Metrics;
using App.Metrics.Counter;
using App.Metrics.Meter;
using App.Metrics.ReservoirSampling.Uniform;
using App.Metrics.Timer;

namespace Monq.Core.BasicDotNetMicroservice;

/// <summary>
/// Unified microservice constants.
/// </summary>
public class MicroserviceConstants
{
    /// <summary>
    /// Http header containing logging event Id.
    /// </summary>
    public const string EventIdHeader = "X-Trace-Event-Id";

    public const string EventIdPropertyName = "XTraceEventId";

    /// <summary>
    /// Http header containing userspace Id.
    /// </summary>
    public const string UserspaceIdHeader = "X-Smon-Userspace-Id";
    public const string UserspaceIdPropertyName = "XSmonUserspaceId";

    /// <summary>
    /// Http header containing culture info.
    /// </summary>
    public const string CultureHeader = "Accept-Language";

    /// <summary>
    /// Certificates directory environment variable name.
    /// </summary>
    public const string CertsDirEnv = "CERTS_DIR";

    /// <summary>
    /// Default certificates directory that will be used if ENV CERTS_DIR is not set.
    /// </summary>
    public const string CertsDirDefault = "/certs";


    internal static class HostConfiguration
    {
        public const string ConsulConfigFileEnv = "CONSUL_CONFIG_FILE";
        public const string ConsulConfigFileDefault = "aspnet_consul_config.json";
        public const string ConsulConfigFileSectionName = "Consul";
        public const string ApplicationNameEnv = "APPLICATION_NAME";
        public const string AppsettingsFile = "appsettings.json";
        public const string CommonAppsettingsFile = "common-appsettings.json";
    }

    internal static class MetricsConfiguration
    {
        public const string ConfigSection = "Metrics:ReportingInfluxDb";
        public const string Metrics = "Metrics";
    }

    /// <summary>
    /// Metrics parameters for RabbitMQ.
    /// </summary>
    internal static class RabbitMQMetrics
    {
        static readonly string _rabbitMQContextLabel = "NetCoreRabbitMQ Metrics";

        public static class Counters
        {
            public static readonly CounterOptions EventsCounter = new()
            {
                Context = _rabbitMQContextLabel,
                Name = "EventsCountCounter",
                MeasurementUnit = Unit.Items,
                ResetOnReporting = true
            };
        }

        public static class Timers
        {
            public static readonly TimerOptions EventProcessTimer = new()
            {
                Context = _rabbitMQContextLabel,
                Name = "EventProcessTimer",
                Reservoir = () => new DefaultAlgorithmRReservoir(),
            };
        }

        public static class Meters
        {
            public static readonly MeterOptions EventsRate = new()
            {
                Context = _rabbitMQContextLabel,
                Name = "EventsRate",
                MeasurementUnit = Unit.Events
            };
        }
    }

    /// <summary>
    /// Metrics parameters for tasks.
    /// </summary>
    internal static class TasksMetrics
    {
        static readonly string _tasksContextLabel = "ConsoleTasks Metrics";

        public static class Counters
        {
            public static readonly CounterOptions TasksCounter = new()
            {
                Context = _tasksContextLabel,
                Name = "TasksCountCounter",
                MeasurementUnit = Unit.Items,
                ResetOnReporting = true
            };
        }

        public static class Timers
        {
            public static readonly TimerOptions TaskProcessTimer = new()
            {
                Context = _tasksContextLabel,
                Name = "TaskProcessTimer",
                Reservoir = () => new DefaultAlgorithmRReservoir(),
            };
        }

        public static class Meters
        {
            public static readonly MeterOptions TasksRate = new()
            {
                Context = _tasksContextLabel,
                Name = "TasksRate",
                MeasurementUnit = Unit.Events
            };
        }
    }
}
