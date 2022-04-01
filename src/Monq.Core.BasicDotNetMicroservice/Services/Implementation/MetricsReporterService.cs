using App.Metrics;
using App.Metrics.Scheduling;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monq.Core.BasicDotNetMicroservice.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Monq.Core.BasicDotNetMicroservice.Services.Implementation
{
    /// <summary>
    /// Сервис отправки отправки метрик.
    /// </summary>
    public class MetricsReporterService : IHostedService, IDisposable
    {
        readonly IMetricsRoot _metricsRoot;
        readonly ILogger<MetricsReporterService> _logger;
        AppMetricsTaskScheduler? _scheduler;
        readonly MetricsReporterOptions _options;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="MetricsReporterService"/> класса.
        /// </summary>
        /// <param name="metricsRoot"></param>
        /// <param name="loggerFactory"></param>
        public MetricsReporterService(IMetricsRoot metricsRoot, ILoggerFactory loggerFactory, MetricsReporterOptions options)
        {
            _logger = loggerFactory.CreateLogger<MetricsReporterService>();
            _metricsRoot = metricsRoot;
            _options = options;
        }

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Starting {nameof(MetricsReporterService)} ...");

            _scheduler = new AppMetricsTaskScheduler(_options.FlushInterval,
                async () =>
                {
                    _logger.LogDebug("Run all metrics report runner.");
                    await Task.WhenAll(_metricsRoot.ReportRunner.RunAllAsync());
                });
            _scheduler.Start();

            _logger.LogInformation($"Started {nameof(MetricsReporterService)}.");
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _scheduler = null;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _scheduler?.Dispose();
            _logger?.LogInformation($"Stopped {nameof(MetricsReporterService)}");
        }
    }
}
