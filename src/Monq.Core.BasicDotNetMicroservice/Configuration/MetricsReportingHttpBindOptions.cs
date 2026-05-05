using App.Metrics.Formatters.Prometheus;
using System;

namespace Monq.Core.BasicDotNetMicroservice.Configuration;

/// <summary>
/// Bind-compatible options for HTTP reporting.
/// Only contains properties that can be configured from configuration sources.
/// </summary>
public class MetricsReportingHttpBindOptions
{
    /// <summary>
    /// The interval between flushing metrics.
    /// </summary>
    public TimeSpan FlushInterval { get; set; }

    /// <summary>
    /// HTTP client settings.
    /// </summary>
    public HttpSettingsBindOptions HttpSettings { get; set; } = new();

    /// <summary>
    /// HTTP policy settings for circuit breaker configuration.
    /// </summary>
    public HttpPolicyBindOptions HttpPolicy { get; set; } = new();

    /// <summary>
    /// Maps to App.Metrics.Reporting.Http.MetricsReportingHttpOptions.
    /// </summary>
    public App.Metrics.Reporting.Http.MetricsReportingHttpOptions ToMetricsReportingHttpOptions()
    {
        return new App.Metrics.Reporting.Http.MetricsReportingHttpOptions
        {
            FlushInterval = FlushInterval,
            HttpSettings = HttpSettings.ToHttpSettings(),
            HttpPolicy = HttpPolicy.ToHttpPolicy(),
            MetricsOutputFormatter = new MetricsPrometheusTextOutputFormatter()
        };
    }
}
