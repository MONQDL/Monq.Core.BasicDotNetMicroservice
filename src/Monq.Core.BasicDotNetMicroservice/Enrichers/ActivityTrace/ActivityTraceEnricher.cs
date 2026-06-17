using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;

namespace Monq.Core.BasicDotNetMicroservice.Enrichers.ActivityTrace;

/// <summary>
/// Serilog enricher that adds <c>TraceId</c>, <c>SpanId</c>, and optionally <c>ParentSpanId</c>
/// from the current <see cref="System.Diagnostics.Activity"/> to every log event.
/// </summary>
public class ActivityTraceEnricher : ILogEventEnricher
{
    /// <summary>
    /// Enriches the log event with trace identifiers from the current activity.
    /// </summary>
    /// <param name="logEvent">The log event to enrich.</param>
    /// <param name="propertyFactory">Factory to create log event properties.</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity is null)
            return;

        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("TraceId", activity.TraceId.ToString()));
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("SpanId", activity.SpanId.ToString()));

        if (activity.ParentId is not null)
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("ParentSpanId", activity.ParentId));
    }
}
