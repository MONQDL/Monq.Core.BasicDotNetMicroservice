using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monq.Core.BasicDotNetMicroservice.Configuration;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection;

namespace Monq.Core.BasicDotNetMicroservice.Extensions;

/// <summary>
/// Extension methods to configure OpenTelemetry tracing and metrics for MONQ microservices.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Registers and configures OpenTelemetry with tracing (AspNetCore, HttpClient, GrpcClient, RabbitMQCoreClient)
    /// and metrics (AspNetCore, HttpClient, Runtime, Monq.Core.*) based on <see cref="OpenTelemetryOptions"/>.
    /// Supports OTLP exporter and optional Prometheus endpoint.
    /// </summary>
    /// <param name="services">The service collection to add OpenTelemetry to.</param>
    /// <param name="configuration">Configuration to read the "OpenTelemetry" section from.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMonqOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection("OpenTelemetry").Get<OpenTelemetryOptions>() ?? new OpenTelemetryOptions();

        if (string.IsNullOrEmpty(options.ServiceName))
            options.ServiceName = Assembly.GetEntryAssembly()?.GetName().Name ?? "unknown-service";

        var otelBuilder = services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(
                serviceName: options.ServiceName,
                serviceVersion: options.ServiceVersion ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString()));

        if (options.EnableTracing)
        {
            otelBuilder.WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddGrpcClientInstrumentation()
                    .AddSource("RabbitMQCoreClient");

                ConfigureOtlpExporter(tracing, options);
            });
        }

        if (options.EnableMetrics)
        {
            otelBuilder.WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("Monq.Core.*");

                if (options.EnablePrometheusEndpoint)
                    metrics.AddPrometheusExporter();

                ConfigureOtlpExporter(metrics, options);
            });
        }

        return services;
    }

    static void ConfigureOtlpExporter(TracerProviderBuilder builder, OpenTelemetryOptions options)
    {
        if (string.IsNullOrEmpty(options.Otlp.Endpoint))
            return;

        builder.AddOtlpExporter(otlp =>
        {
            otlp.Endpoint = new System.Uri(options.Otlp.Endpoint);

            if (!string.IsNullOrEmpty(options.Otlp.Headers))
                otlp.Headers = options.Otlp.Headers;

            otlp.Protocol = options.Otlp.Protocol?.ToLowerInvariant() switch
            {
                "http/protobuf" => OtlpExportProtocol.HttpProtobuf,
                _ => OtlpExportProtocol.Grpc
            };
        });
    }

    static void ConfigureOtlpExporter(MeterProviderBuilder builder, OpenTelemetryOptions options)
    {
        if (string.IsNullOrEmpty(options.Otlp.Endpoint))
            return;

        builder.AddOtlpExporter(otlp =>
        {
            otlp.Endpoint = new System.Uri(options.Otlp.Endpoint);

            if (!string.IsNullOrEmpty(options.Otlp.Headers))
                otlp.Headers = options.Otlp.Headers;

            otlp.Protocol = options.Otlp.Protocol?.ToLowerInvariant() switch
            {
                "http/protobuf" => OtlpExportProtocol.HttpProtobuf,
                _ => OtlpExportProtocol.Grpc
            };
        });
    }
}
