using App.Metrics;
using System;
using System.Threading.Tasks;

namespace Monq.Core.BasicDotNetMicroservice.Extensions
{
    /// <summary>
    /// Расширение для IMetricsRoot.
    /// </summary>
    public static class MetricsRootExtensions
    {
        const string DefaultItem = "DefaultItem";

        static readonly MetricTags _recievedTags = new("Action", "recieved");
        static readonly MetricTags _rejectedTags = new("Action", "rejected");
        static readonly MetricTags _processedTags = new("Action", "processed");
        static readonly MetricTags _failedTags = new("Action", "failed");

        /// <summary>
        /// Увеличить на 1 количество принятых событий.
        /// </summary>
        /// <param name="metrics"></param>
        /// <param name="item"></param>
        public static void IncrementRabbitMQReceived(this IMetricsRoot metrics, string item = DefaultItem)
            => metrics.IncreaseRabbitMQMetricsValues(_recievedTags, item);

        /// <summary>
        /// Увеличить на 1 количество отброшенных событий.
        /// </summary>
        /// <param name="metrics"></param>
        /// <param name="item"></param>
        public static void IncrementRabbitMQRejected(this IMetricsRoot metrics, string item = DefaultItem)
            => metrics.IncreaseRabbitMQMetricsValues(_rejectedTags, item);

        /// <summary>
        /// Увеличить на 1 количество обработанных событий.
        /// </summary>
        /// <param name="metrics"></param>
        /// <param name="item"></param>
        public static void IncrementRabbitMQProcessed(this IMetricsRoot metrics, string item = DefaultItem)
            => metrics.IncreaseRabbitMQMetricsValues(_processedTags, item);

        /// <summary>
        /// Увеличить на 1 количество событий, обработанных с ошибкой.
        /// </summary>
        /// <param name="metrics"></param>
        /// <param name="item"></param>
        public static void IncrementRabbitMQFailed(this IMetricsRoot metrics, string item = DefaultItem)
            => metrics.IncreaseRabbitMQMetricsValues(_failedTags, item);

        /// <summary>
        /// Выполнить делегат-парсер и измерить время обработки сборки.
        /// </summary>
        /// <param name="metrics"></param>
        /// <param name="action"></param>
        public static void MeasureRabbitMQPreprocessingTime(this IMetricsRoot metrics, Action action)
        {
            using (metrics.Measure.Timer.Time(MicroserviceConstants.RabbitMQMetrics.Timers.EventProcessTimer))
            {
                action();
            }
        }

        /// <summary>
        /// Выполнить делегат-парсер и измерить время обработки сборки.
        /// </summary>
        /// <param name="metrics"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static async Task MeasureRabbitMQPreprocessingTimeAsync(this IMetricsRoot metrics, Func<Task> action)
        {
            using (metrics.Measure.Timer.Time(MicroserviceConstants.RabbitMQMetrics.Timers.EventProcessTimer))
            {
                await action();
            }
        }

        static void IncreaseRabbitMQMetricsValues(this IMetricsRoot metrics, MetricTags tags, string item)
        {
            metrics.Measure.Counter.Increment(MicroserviceConstants.RabbitMQMetrics.Counters.EventsCounter, tags, item);
            metrics.Measure.Meter.Mark(MicroserviceConstants.RabbitMQMetrics.Meters.EventsRate, tags, item);
        }

        /// <summary>
        /// Увеличить на 1 количество принятых заданий.
        /// </summary>
        /// <param name="metrics"></param>
        /// <param name="item"></param>
        public static void IncrementTasksReceived(this IMetricsRoot metrics, string item = DefaultItem)
            => metrics.IncreaseTasksMetricsValues(_recievedTags, item);

        /// <summary>
        /// Увеличить на 1 количество отброшенных заданий.
        /// </summary>
        /// <param name="metrics"></param>
        /// <param name="item"></param>
        public static void IncrementTasksRejected(this IMetricsRoot metrics, string item = DefaultItem)
            => metrics.IncreaseTasksMetricsValues(_rejectedTags, item);

        /// <summary>
        /// Увеличить на 1 количество обработанных заданий.
        /// </summary>
        /// <param name="metrics"></param>
        /// <param name="item"></param>
        public static void IncrementTasksProcessed(this IMetricsRoot metrics, string item = DefaultItem)
            => metrics.IncreaseTasksMetricsValues(_processedTags, item);

        /// <summary>
        /// Увеличить на 1 количество заданий, обработанных с ошибкой.
        /// </summary>
        /// <param name="metrics"></param>
        /// <param name="item"></param>
        public static void IncrementTasksFailed(this IMetricsRoot metrics, string item = DefaultItem)
            => metrics.IncreaseTasksMetricsValues(_failedTags, item);

        /// <summary>
        /// Выполнить делегат-парсер и измерить время обработки задания.
        /// </summary>
        /// <param name="metrics"></param>
        /// <param name="action"></param>
        public static void MeasureTasksPreprocessingTime(this IMetricsRoot metrics, Action action)
        {
            using (metrics.Measure.Timer.Time(MicroserviceConstants.TasksMetrics.Timers.TaskProcessTimer))
            {
                action();
            }
        }

        /// <summary>
        /// Выполнить делегат-парсер и измерить время обработки задания.
        /// </summary>
        /// <param name="metrics"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static async Task MeasureTasksPreprocessingTimeAsync(this IMetricsRoot metrics, Func<Task> action)
        {
            using (metrics.Measure.Timer.Time(MicroserviceConstants.TasksMetrics.Timers.TaskProcessTimer))
            {
                await action();
            }
        }

        static void IncreaseTasksMetricsValues(this IMetricsRoot metrics, MetricTags tags, string item)
        {
            metrics.Measure.Counter.Increment(MicroserviceConstants.TasksMetrics.Counters.TasksCounter, tags, item);
            metrics.Measure.Meter.Mark(MicroserviceConstants.TasksMetrics.Meters.TasksRate, tags, item);
        }
    }
}
