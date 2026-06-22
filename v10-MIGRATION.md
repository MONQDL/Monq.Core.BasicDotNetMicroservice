# Migration guide from v9 to v10

Version 10.0.0 is a major release with breaking changes. The primary changes are:

1. **App.Metrics removed** — replaced with `System.Diagnostics.Metrics` and OpenTelemetry.
2. **OpenTelemetry added** — distributed tracing and metrics via OTLP.
3. **X-Trace-Event-Id removed** — replaced by W3C `traceparent` from OpenTelemetry.

## Metrics migration

Replace `IMetricsRoot` with `MonqMetrics`:

```csharp
// BEFORE (v9):
public class MyService
{
    readonly IMetricsRoot _metrics;
    public MyService(IMetricsRoot metrics) => _metrics = metrics;

    public void Process() => _metrics.IncrementRabbitMQReceived("my-handler");
}

// AFTER (v10):
public class MyService
{
    readonly MonqMetrics _metrics;
    public MyService(MonqMetrics metrics) => _metrics = metrics;

    public void Process() => _metrics.IncrementRabbitMQReceived("my-handler");
}
```

Remove `services.AddConsoleMetrics(context)` — metrics are now configured automatically via `AddMonqOpenTelemetry()`.

## Tracing migration

Remove `app.UseTraceEventId()` — tracing is now handled automatically by OpenTelemetry.
Remove `app.UseLogUser()` — user is now handled automatically by OpenTelemetry.

```csharp
// BEFORE (v9):
app.UseRouting();
app.UseTraceEventId();
app.UseAuthentication();
app.UseLogUser();
app.MapControllers();

// AFTER (v10):
app.UseRouting();
app.UseAuthentication();
app.MapControllers();
```

`TraceId` and `SpanId` are automatically added to all log entries via `ActivityTraceEnricher`.
`UserId` and `UserName` are automatically added via `UserEnricher`.

## Configuration

Add OpenTelemetry configuration to `appsettings.json`:

```json
{
  "OpenTelemetry": {
    "ServiceName": "my-microservice",
    "Otlp": {
      "Endpoint": "http://otel-collector:4317",
      "Protocol": "grpc"
    },
    "EnableTracing": true,
    "EnableMetrics": true,
    "EnablePrometheusEndpoint": true
  }
}
```

## Removed configuration sections

The following `appsettings.json` sections are no longer used and can be removed:

- `Metrics:ReportingInfluxDb` — InfluxDB reporting removed
- `Metrics:ReportingOverHttp` — HTTP reporting removed (use Prometheus scrape or OTLP instead)

## RabbitMQCoreClient upgrade

Upgrade `RabbitMQCoreClient` to v7.1+ to get automatic distributed tracing. No code changes required in message handlers — trace context is automatically propagated.
