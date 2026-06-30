namespace Monq.Core.BasicDotNetMicroservice.Configuration;

/// <summary>
/// Configuration options for OpenTelemetry integration, read from the "OpenTelemetry" config section.
/// </summary>
public class OpenTelemetryOptions
{
    /// <summary>Service name used in telemetry data. Defaults to entry assembly name.</summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>Optional service version. Defaults to entry assembly version.</summary>
    public string? ServiceVersion { get; set; }

    /// <summary>OTLP exporter configuration (endpoint, headers, protocol).</summary>
    public OtlpExporterOptions Otlp { get; set; } = new();

    /// <summary>Enables distributed tracing instrumentation. Default is true.</summary>
    public bool EnableTracing { get; set; } = true;

    /// <summary>Enables metrics collection. Default is true.</summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>Enables Prometheus metrics scraping endpoint. Default is true.</summary>
    public bool EnablePrometheusEndpoint { get; set; } = true;

    /// <summary>Optional configuration for pushing metrics to a Prometheus Pushgateway.</summary>
    public PrometheusPushOptions? PrometheusPush { get; set; }

    /// <summary>
    /// Sampling ratio for traces (0.0 to 1.0). Default is 1.0 (sample all traces).
    /// Uses ParentBasedSampler wrapping TraceIdRatioBasedSampler.
    /// </summary>
    public double SamplingRatio { get; set; } = 1.0;
}

/// <summary>
/// Configuration for the OTLP (OpenTelemetry Protocol) exporter.
/// </summary>
public class OtlpExporterOptions
{
    /// <summary>OTLP collector endpoint URL. Default is "http://localhost:4317".</summary>
    public string Endpoint { get; set; } = "http://localhost:4317";

    /// <summary>Optional headers for authentication (e.g. "Authorization=Bearer token").</summary>
    public string? Headers { get; set; }

    /// <summary>Transport protocol: "grpc" (default) or "http/protobuf".</summary>
    public string Protocol { get; set; } = "grpc";
}

/// <summary>
/// Configuration for pushing metrics to a Prometheus Pushgateway.
/// </summary>
public class PrometheusPushOptions
{
    /// <summary>Pushgateway endpoint URL.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Interval in milliseconds between metric flushes. Default is 10000.</summary>
    public int FlushIntervalMs { get; set; } = 10000;
}
