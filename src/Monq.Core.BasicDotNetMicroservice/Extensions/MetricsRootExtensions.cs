using Monq.Core.BasicDotNetMicroservice.Metrics;

namespace Monq.Core.BasicDotNetMicroservice.Extensions;

/// <summary>
/// Extension methods for <see cref="MonqMetrics"/> to record RabbitMQ and task processing metrics.
/// </summary>
public static class MetricsRootExtensions
{
    /// <summary>
    /// Increments the RabbitMQ messages received counter.
    /// </summary>
    /// <param name="metrics">The metrics instance.</param>
    /// <param name="item">Handler or queue identifier. Defaults to "DefaultItem".</param>
    public static void IncrementRabbitMQReceived(this MonqMetrics metrics, string item = "DefaultItem") =>
        metrics.IncrementRabbitMQReceived(item);

    /// <summary>
    /// Increments the RabbitMQ messages rejected counter.
    /// </summary>
    /// <param name="metrics">The metrics instance.</param>
    /// <param name="item">Handler or queue identifier. Defaults to "DefaultItem".</param>
    public static void IncrementRabbitMQRejected(this MonqMetrics metrics, string item = "DefaultItem") =>
        metrics.IncrementRabbitMQRejected(item);

    /// <summary>
    /// Increments the RabbitMQ messages processed counter.
    /// </summary>
    /// <param name="metrics">The metrics instance.</param>
    /// <param name="item">Handler or queue identifier. Defaults to "DefaultItem".</param>
    public static void IncrementRabbitMQProcessed(this MonqMetrics metrics, string item = "DefaultItem") =>
        metrics.IncrementRabbitMQProcessed(item);

    /// <summary>
    /// Increments the RabbitMQ messages failed counter.
    /// </summary>
    /// <param name="metrics">The metrics instance.</param>
    /// <param name="item">Handler or queue identifier. Defaults to "DefaultItem".</param>
    public static void IncrementRabbitMQFailed(this MonqMetrics metrics, string item = "DefaultItem") =>
        metrics.IncrementRabbitMQFailed(item);

    /// <summary>
    /// Measures the duration of a synchronous RabbitMQ message processing action.
    /// </summary>
    /// <param name="metrics">The metrics instance.</param>
    /// <param name="action">The action to measure.</param>
    public static void MeasureRabbitMQPreprocessingTime(this MonqMetrics metrics, Action action) =>
        metrics.MeasureRabbitMQPreprocessingTime(action);

    /// <summary>
    /// Measures the duration of an asynchronous RabbitMQ message processing action.
    /// </summary>
    /// <param name="metrics">The metrics instance.</param>
    /// <param name="action">The async action to measure.</param>
    /// <returns>A task representing the measured operation.</returns>
    public static System.Threading.Tasks.Task MeasureRabbitMQPreprocessingTimeAsync(this MonqMetrics metrics, Func<System.Threading.Tasks.Task> action) =>
        metrics.MeasureRabbitMQPreprocessingTimeAsync(action);

    /// <summary>
    /// Increments the tasks received counter.
    /// </summary>
    /// <param name="metrics">The metrics instance.</param>
    /// <param name="item">Task identifier. Defaults to "DefaultItem".</param>
    public static void IncrementTasksReceived(this MonqMetrics metrics, string item = "DefaultItem") =>
        metrics.IncrementTasksReceived(item);

    /// <summary>
    /// Increments the tasks rejected counter.
    /// </summary>
    /// <param name="metrics">The metrics instance.</param>
    /// <param name="item">Task identifier. Defaults to "DefaultItem".</param>
    public static void IncrementTasksRejected(this MonqMetrics metrics, string item = "DefaultItem") =>
        metrics.IncrementTasksRejected(item);

    /// <summary>
    /// Increments the tasks processed counter.
    /// </summary>
    /// <param name="metrics">The metrics instance.</param>
    /// <param name="item">Task identifier. Defaults to "DefaultItem".</param>
    public static void IncrementTasksProcessed(this MonqMetrics metrics, string item = "DefaultItem") =>
        metrics.IncrementTasksProcessed(item);

    /// <summary>
    /// Increments the tasks failed counter.
    /// </summary>
    /// <param name="metrics">The metrics instance.</param>
    /// <param name="item">Task identifier. Defaults to "DefaultItem".</param>
    public static void IncrementTasksFailed(this MonqMetrics metrics, string item = "DefaultItem") =>
        metrics.IncrementTasksFailed(item);

    /// <summary>
    /// Measures the duration of a synchronous task processing action.
    /// </summary>
    /// <param name="metrics">The metrics instance.</param>
    /// <param name="action">The action to measure.</param>
    public static void MeasureTasksPreprocessingTime(this MonqMetrics metrics, Action action) =>
        metrics.MeasureTasksPreprocessingTime(action);

    /// <summary>
    /// Measures the duration of an asynchronous task processing action.
    /// </summary>
    /// <param name="metrics">The metrics instance.</param>
    /// <param name="action">The async action to measure.</param>
    /// <returns>A task representing the measured operation.</returns>
    public static System.Threading.Tasks.Task MeasureTasksPreprocessingTimeAsync(this MonqMetrics metrics, Func<System.Threading.Tasks.Task> action) =>
        metrics.MeasureTasksPreprocessingTimeAsync(action);
}
