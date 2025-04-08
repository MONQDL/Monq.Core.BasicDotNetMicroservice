using App.Metrics.Reporting.Http;
using App.Metrics.Reporting.InfluxDB;

namespace Monq.Core.BasicDotNetMicroservice.Configuration;

/// <summary>
/// Metrics configuration options.
/// </summary>
public class MetricsConfigurationOptions
{
    /// <summary>
    ///  Provides programmatic configuration for InfluxDB Reporting in the App Metrics framework.
    /// </summary>
    public MetricsReportingInfluxDbOptions ReportingInfluxDb { get; set; } = new();

    /// <summary>
    /// Provides programmatic configuration of HTTP Reporting in the App Metrics framework.
    /// </summary>
    public MetricsReportingHttpOptions ReportingOverHttp { get; set; } = new();

    /// <summary>
    /// Configuration option for reporting system and GC events metrics.
    /// </summary>
    public bool AddSystemMetrics { get; set; }
}
