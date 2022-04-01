using System;
using static App.Metrics.AppMetricsConstants;

namespace Monq.Core.BasicDotNetMicroservice.Configuration
{
    /// <summary>
    /// Настройки для репортера.
    /// </summary>
    public class MetricsReporterOptions
    {
        /// <summary>
        /// Временно интервал отправки метрик.
        /// </summary>
        public TimeSpan FlushInterval { get; set; }

        /// <summary>
        /// Коструктор.
        /// </summary>
        /// <param name="options"></param>
        public MetricsReporterOptions(MetricsConfigurationOptions options)
        {
            FlushInterval = options.ReportingInfluxDb.FlushInterval.CompareTo(options.ReportingOverHttp.FlushInterval) < 0
                ? options.ReportingInfluxDb.FlushInterval
                : options.ReportingOverHttp.FlushInterval;

            if (FlushInterval == TimeSpan.Zero) FlushInterval = Reporting.DefaultFlushInterval;
        }
    }
}
