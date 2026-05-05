using System;

namespace Monq.Core.BasicDotNetMicroservice.Configuration;

/// <summary>
/// Bind-compatible options for InfluxDB reporting.
/// Only contains properties that can be configured from configuration sources.
/// </summary>
public class MetricsReportingInfluxDbBindOptions
{
    /// <summary>
    /// The flush metrics interval.
    /// </summary>
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// InfluxDB connectivity options.
    /// </summary>
    public InfluxDbBindOptions InfluxDb { get; set; } = new();

    /// <summary>
    /// Maps to App.Metrics.Reporting.InfluxDB.MetricsReportingInfluxDbOptions.
    /// </summary>
    public App.Metrics.Reporting.InfluxDB.MetricsReportingInfluxDbOptions ToMetricsReportingInfluxDbOptions()
    {
        var influxDbOptions = InfluxDb.ToInfluxDbOptions();
        return new App.Metrics.Reporting.InfluxDB.MetricsReportingInfluxDbOptions
        {
            FlushInterval = FlushInterval,
            InfluxDb = influxDbOptions
        };
    }
}
