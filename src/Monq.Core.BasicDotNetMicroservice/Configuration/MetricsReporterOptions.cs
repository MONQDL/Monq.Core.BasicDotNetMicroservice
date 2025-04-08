using System;
using static App.Metrics.AppMetricsConstants;

namespace Monq.Core.BasicDotNetMicroservice.Configuration;

/// <summary>
/// Metrics reporter options.
/// </summary>
public class MetricsReporterOptions
{
    /// <summary>
    /// Time interval for flushing metrics.
    /// </summary>
    public TimeSpan FlushInterval { get; set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="options">Metrics configuration options.</param>
    public MetricsReporterOptions(MetricsConfigurationOptions options)
    {
        if (options.ReportingInfluxDb.FlushInterval <= TimeSpan.Zero)
            FlushInterval = options.ReportingOverHttp.FlushInterval;
        else if (options.ReportingOverHttp.FlushInterval <= TimeSpan.Zero)
            FlushInterval = options.ReportingInfluxDb.FlushInterval;
        else
            FlushInterval = options.ReportingInfluxDb.FlushInterval.CompareTo(options.ReportingOverHttp.FlushInterval) < 0
                ? options.ReportingInfluxDb.FlushInterval
                : options.ReportingOverHttp.FlushInterval;

        if (FlushInterval <= TimeSpan.Zero) FlushInterval = Reporting.DefaultFlushInterval;
    }
}
