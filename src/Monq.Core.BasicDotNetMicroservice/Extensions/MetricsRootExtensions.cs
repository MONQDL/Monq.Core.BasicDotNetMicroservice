using App.Metrics;
using System;
using System.Threading.Tasks;

namespace Monq.Core.BasicDotNetMicroservice.Extensions;

/// <summary>
/// IMetricsRoot extensions.
/// </summary>
public static class MetricsRootExtensions
{
    const string DefaultItem = "DefaultItem";

    static readonly MetricTags _recievedTags = new("Action", "recieved");
    static readonly MetricTags _rejectedTags = new("Action", "rejected");
    static readonly MetricTags _processedTags = new("Action", "processed");
    static readonly MetricTags _failedTags = new("Action", "failed");

    /// <summary>
    /// Increments by 1 count of received events.
    /// </summary>
    /// <param name="metrics">IMetricsRoot to extend the behavior.</param>
    /// <param name="item">Item identifier for a message handler.</param>
    public static void IncrementRabbitMQReceived(this IMetricsRoot metrics, string item = DefaultItem)
        => metrics.IncreaseRabbitMQMetricsValues(_recievedTags, item);

    /// <summary>
    /// Increments by 1 count of rejected events.
    /// </summary>
    /// <param name="metrics">IMetricsRoot to extend the behavior.</param>
    /// <param name="item">Item identifier for a message handler.</param>
    public static void IncrementRabbitMQRejected(this IMetricsRoot metrics, string item = DefaultItem)
        => metrics.IncreaseRabbitMQMetricsValues(_rejectedTags, item);

    /// <summary>
    /// Increments by 1 count of processed events.
    /// </summary>
    /// <param name="metrics">IMetricsRoot to extend the behavior.</param>
    /// <param name="item">Item identifier for a message handler.</param>
    public static void IncrementRabbitMQProcessed(this IMetricsRoot metrics, string item = DefaultItem)
        => metrics.IncreaseRabbitMQMetricsValues(_processedTags, item);

    /// <summary>
    /// Increments by 1 count of failed events.
    /// </summary>
    /// <param name="metrics">IMetricsRoot to extend the behavior.</param>
    /// <param name="item">Item identifier for a message handler.</param>
    public static void IncrementRabbitMQFailed(this IMetricsRoot metrics, string item = DefaultItem)
        => metrics.IncreaseRabbitMQMetricsValues(_failedTags, item);

    /// <summary>
    /// Measures the time taken to process an action.
    /// </summary>
    /// <param name="metrics">IMetricsRoot to extend the behavior.</param>
    /// <param name="action">The action to measure.</param>
    public static void MeasureRabbitMQPreprocessingTime(this IMetricsRoot metrics, Action action)
    {
        using (metrics.Measure.Timer.Time(MicroserviceConstants.RabbitMQMetrics.Timers.EventProcessTimer))
        {
            action();
        }
    }

    /// <summary>
    /// Measures the time taken to process an action.
    /// </summary>
    /// <param name="metrics">IMetricsRoot to extend the behavior.</param>
    /// <param name="action">The action to measure.</param>
    public static async Task MeasureRabbitMQPreprocessingTimeAsync(this IMetricsRoot metrics, Func<Task> action)
    {
        using (metrics.Measure.Timer.Time(MicroserviceConstants.RabbitMQMetrics.Timers.EventProcessTimer))
        {
            await action();
        }
    }

    /// <summary>
    /// Increments by 1 count of received tasks.
    /// </summary>
    /// <param name="metrics">IMetricsRoot to extend the behavior.</param>
    /// <param name="item">Item identifier for a message handler.</param>
    public static void IncrementTasksReceived(this IMetricsRoot metrics, string item = DefaultItem)
        => metrics.IncreaseTasksMetricsValues(_recievedTags, item);

    /// <summary>
    /// Increments by 1 count of rejected tasks.
    /// </summary>
    /// <param name="metrics">IMetricsRoot to extend the behavior.</param>
    /// <param name="item">Item identifier for a message handler.</param>
    public static void IncrementTasksRejected(this IMetricsRoot metrics, string item = DefaultItem)
        => metrics.IncreaseTasksMetricsValues(_rejectedTags, item);

    /// <summary>
    /// Increments by 1 count of processed tasks.
    /// </summary>
    /// <param name="metrics">IMetricsRoot to extend the behavior.</param>
    /// <param name="item">Item identifier for a message handler.</param>
    public static void IncrementTasksProcessed(this IMetricsRoot metrics, string item = DefaultItem)
        => metrics.IncreaseTasksMetricsValues(_processedTags, item);

    /// <summary>
    /// Increments by 1 count of failed tasks.
    /// </summary>
    /// <param name="metrics">IMetricsRoot to extend the behavior.</param>
    /// <param name="item">Item identifier for a message handler.</param>
    public static void IncrementTasksFailed(this IMetricsRoot metrics, string item = DefaultItem)
        => metrics.IncreaseTasksMetricsValues(_failedTags, item);

    /// <summary>
    /// Measures the time taken to process an action.
    /// </summary>
    /// <param name="metrics">IMetricsRoot to extend the behavior.</param>
    /// <param name="action">The action to measure.</param>
    public static void MeasureTasksPreprocessingTime(this IMetricsRoot metrics, Action action)
    {
        using (metrics.Measure.Timer.Time(MicroserviceConstants.TasksMetrics.Timers.TaskProcessTimer))
        {
            action();
        }
    }

    /// <summary>
    /// Measures the time taken to process an action.
    /// </summary>
    /// <param name="metrics">IMetricsRoot to extend the behavior.</param>
    /// <param name="action">The action to measure.</param>
    /// <returns></returns>
    public static async Task MeasureTasksPreprocessingTimeAsync(this IMetricsRoot metrics, Func<Task> action)
    {
        using (metrics.Measure.Timer.Time(MicroserviceConstants.TasksMetrics.Timers.TaskProcessTimer))
        {
            await action();
        }
    }

    /// <summary>
    /// Increments by 1 count of tagged events.
    /// </summary>
    /// <param name="metrics">IMetricsRoot to extend the behavior.</param>
    /// <param name="tags">The runtime tags to set in addition to those defined on the options, this will
    /// create a separate metric per unique App.Metrics.MetricTags</param>
    /// <param name="item">Item identifier for a message handler.</param>
    static void IncreaseRabbitMQMetricsValues(this IMetricsRoot metrics, MetricTags tags, string item)
    {
        metrics.Measure.Counter.Increment(MicroserviceConstants.RabbitMQMetrics.Counters.EventsCounter, tags, item);
        metrics.Measure.Meter.Mark(MicroserviceConstants.RabbitMQMetrics.Meters.EventsRate, tags, item);
    }

    /// <summary>
    /// Increments by 1 count of tagged tasks.
    /// </summary>
    /// <param name="metrics">IMetricsRoot to extend the behavior.</param>
    /// <param name="tags">The runtime tags to set in addition to those defined on the options, this will
    /// create a separate metric per unique App.Metrics.MetricTags</param>
    /// <param name="item">Item identifier for a message handler.</param>
    static void IncreaseTasksMetricsValues(this IMetricsRoot metrics, MetricTags tags, string item)
    {
        metrics.Measure.Counter.Increment(MicroserviceConstants.TasksMetrics.Counters.TasksCounter, tags, item);
        metrics.Measure.Meter.Mark(MicroserviceConstants.TasksMetrics.Meters.TasksRate, tags, item);
    }
}
