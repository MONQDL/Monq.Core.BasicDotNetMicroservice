using System.Diagnostics.Metrics;

namespace Monq.Core.BasicDotNetMicroservice.Metrics;

/// <summary>
/// Provides pre-configured metrics for RabbitMQ message processing and background task execution.
/// Uses <see cref="System.Diagnostics.Metrics"/> for NativeAOT-compatible, OpenTelemetry-native metric collection.
/// All metrics are registered under the "Monq.Core.Metrics" meter.
/// </summary>
public sealed class MonqMetrics
{
    static readonly Meter Meter = new("Monq.Core.Metrics", "1.0");

    static readonly Counter<long> RabbitMqReceived = Meter.CreateCounter<long>("rabbitmq.messages.received", "messages", "Count of received RabbitMQ messages");
    static readonly Counter<long> RabbitMqRejected = Meter.CreateCounter<long>("rabbitmq.messages.rejected", "messages", "Count of rejected RabbitMQ messages");
    static readonly Counter<long> RabbitMqProcessed = Meter.CreateCounter<long>("rabbitmq.messages.processed", "messages", "Count of processed RabbitMQ messages");
    static readonly Counter<long> RabbitMqFailed = Meter.CreateCounter<long>("rabbitmq.messages.failed", "messages", "Count of failed RabbitMQ messages");
    static readonly Histogram<double> RabbitMqProcessDuration = Meter.CreateHistogram<double>("rabbitmq.process.duration", "ms", "Duration of RabbitMQ message processing");

    static readonly Counter<long> TasksReceived = Meter.CreateCounter<long>("tasks.received", "tasks", "Count of received tasks");
    static readonly Counter<long> TasksRejected = Meter.CreateCounter<long>("tasks.rejected", "tasks", "Count of rejected tasks");
    static readonly Counter<long> TasksProcessed = Meter.CreateCounter<long>("tasks.processed", "tasks", "Count of processed tasks");
    static readonly Counter<long> TasksFailed = Meter.CreateCounter<long>("tasks.failed", "tasks", "Count of failed tasks");
    static readonly Histogram<double> TasksProcessDuration = Meter.CreateHistogram<double>("tasks.process.duration", "ms", "Duration of task processing");

    /// <summary>
    /// Increments the RabbitMQ messages received counter.
    /// </summary>
    /// <param name="handler">Handler or queue identifier. Defaults to "DefaultItem".</param>
    public void IncrementRabbitMQReceived(string handler = "DefaultItem") =>
        RabbitMqReceived.Add(1, new KeyValuePair<string, object?>("handler", handler), new KeyValuePair<string, object?>("action", "received"));

    /// <summary>
    /// Increments the RabbitMQ messages rejected counter.
    /// </summary>
    /// <param name="handler">Handler or queue identifier. Defaults to "DefaultItem".</param>
    public void IncrementRabbitMQRejected(string handler = "DefaultItem") =>
        RabbitMqRejected.Add(1, new KeyValuePair<string, object?>("handler", handler), new KeyValuePair<string, object?>("action", "rejected"));

    /// <summary>
    /// Increments the RabbitMQ messages processed counter.
    /// </summary>
    /// <param name="handler">Handler or queue identifier. Defaults to "DefaultItem".</param>
    public void IncrementRabbitMQProcessed(string handler = "DefaultItem") =>
        RabbitMqProcessed.Add(1, new KeyValuePair<string, object?>("handler", handler), new KeyValuePair<string, object?>("action", "processed"));

    /// <summary>
    /// Increments the RabbitMQ messages failed counter.
    /// </summary>
    /// <param name="handler">Handler or queue identifier. Defaults to "DefaultItem".</param>
    public void IncrementRabbitMQFailed(string handler = "DefaultItem") =>
        RabbitMqFailed.Add(1, new KeyValuePair<string, object?>("handler", handler), new KeyValuePair<string, object?>("action", "failed"));

    /// <summary>
    /// Measures the duration of a synchronous RabbitMQ message processing action.
    /// Records the elapsed time in milliseconds to the <c>rabbitmq.process.duration</c> histogram.
    /// </summary>
    /// <param name="action">The action to measure.</param>
    public void MeasureRabbitMQPreprocessingTime(Action action)
    {
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        try
        {
            action();
        }
        finally
        {
            var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(start);
            RabbitMqProcessDuration.Record(elapsed.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Measures the duration of an asynchronous RabbitMQ message processing action.
    /// Records the elapsed time in milliseconds to the <c>rabbitmq.process.duration</c> histogram.
    /// </summary>
    /// <param name="action">The async action to measure.</param>
    public async Task MeasureRabbitMQPreprocessingTimeAsync(Func<Task> action)
    {
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        try
        {
            await action();
        }
        finally
        {
            var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(start);
            RabbitMqProcessDuration.Record(elapsed.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Increments the tasks received counter.
    /// </summary>
    /// <param name="handler">Task identifier. Defaults to "DefaultItem".</param>
    public void IncrementTasksReceived(string handler = "DefaultItem") =>
        TasksReceived.Add(1, new KeyValuePair<string, object?>("handler", handler), new KeyValuePair<string, object?>("action", "received"));

    /// <summary>
    /// Increments the tasks rejected counter.
    /// </summary>
    /// <param name="handler">Task identifier. Defaults to "DefaultItem".</param>
    public void IncrementTasksRejected(string handler = "DefaultItem") =>
        TasksRejected.Add(1, new KeyValuePair<string, object?>("handler", handler), new KeyValuePair<string, object?>("action", "rejected"));

    /// <summary>
    /// Increments the tasks processed counter.
    /// </summary>
    /// <param name="handler">Task identifier. Defaults to "DefaultItem".</param>
    public void IncrementTasksProcessed(string handler = "DefaultItem") =>
        TasksProcessed.Add(1, new KeyValuePair<string, object?>("handler", handler), new KeyValuePair<string, object?>("action", "processed"));

    /// <summary>
    /// Increments the tasks failed counter.
    /// </summary>
    /// <param name="handler">Task identifier. Defaults to "DefaultItem".</param>
    public void IncrementTasksFailed(string handler = "DefaultItem") =>
        TasksFailed.Add(1, new KeyValuePair<string, object?>("handler", handler), new KeyValuePair<string, object?>("action", "failed"));

    /// <summary>
    /// Measures the duration of a synchronous task processing action.
    /// Records the elapsed time in milliseconds to the <c>tasks.process.duration</c> histogram.
    /// </summary>
    /// <param name="action">The action to measure.</param>
    public void MeasureTasksPreprocessingTime(Action action)
    {
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        try
        {
            action();
        }
        finally
        {
            var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(start);
            TasksProcessDuration.Record(elapsed.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Measures the duration of an asynchronous task processing action.
    /// Records the elapsed time in milliseconds to the <c>tasks.process.duration</c> histogram.
    /// </summary>
    /// <param name="action">The async action to measure.</param>
    public async Task MeasureTasksPreprocessingTimeAsync(Func<Task> action)
    {
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        try
        {
            await action();
        }
        finally
        {
            var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(start);
            TasksProcessDuration.Record(elapsed.TotalMilliseconds);
        }
    }
}
