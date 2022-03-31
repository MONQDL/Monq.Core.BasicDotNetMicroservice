using App.Metrics.Reporting.Http;
using App.Metrics.Reporting.InfluxDB;

namespace Monq.Core.BasicDotNetMicroservice.Configuration
{
    /// <summary>
    /// Настройки конфигурации для отправки метрик.
    /// </summary>
    public class MetricsConfigurationOptions
    {
        /// <summary>
        /// Настройки для отправки метрик в InfluxDb.
        /// </summary>
        public MetricsReportingInfluxDbOptions ReportingInfluxDb { get; set; } = new();

        /// <summary>
        /// Настройки для отправки метрик OverHttp.
        /// </summary>
        public MetricsReportingHttpOptions ReportingOverHttp { get; set; } = new();

        /// <summary>
        /// Настройка для отправки системных метрик.
        /// </summary>
        public bool AddSystemMetrics { get; set; }
    }
}
