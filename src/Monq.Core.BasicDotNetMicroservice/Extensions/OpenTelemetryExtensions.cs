using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Monq.Core.BasicDotNetMicroservice.Configuration;
using Monq.Core.BasicDotNetMicroservice.Helpers;
using Monq.Core.BasicDotNetMicroservice.Middleware;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry;
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
    /// <param name="context">The host builder context containing configuration and hosting environment.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMonqOpenTelemetry(
        this IServiceCollection services,
        HostBuilderContext context)
    {
        var configuration = context.Configuration;
        var env = context.HostingEnvironment;
        var options = configuration.GetSection(MicroserviceConstants.HostConfiguration.OpenTelemetrySectionName).Get<OpenTelemetryOptions>() ?? new OpenTelemetryOptions();

        if (string.IsNullOrEmpty(options.ServiceName))
            options.ServiceName = Assembly.GetEntryAssembly()?.GetName().Name ?? "unknown-service";

        var microserviceName = Environment.GetEnvironmentVariable("ASPNETCORE_" + MicroserviceConstants.HostConfiguration.ApplicationNameEnv);
        var hostName = Environment.GetEnvironmentVariable("HOSTNAME");

        var resourceAttributes = new Dictionary<string, object>
        {
            { "deployment.environment.name", env.EnvironmentName },
            { "service.microservice", microserviceName ?? string.Empty },
            { "host.name", hostName ?? string.Empty }
        };

        var otelBuilder = services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(
                serviceName: options.ServiceName,
                serviceVersion: options.ServiceVersion ?? MicroserviceVersionInfo.GetEntryPointAssemblyVersion())
                .AddAttributes(resourceAttributes));

        if (options.EnableTracing)
        {
            otelBuilder.WithTracing(tracing =>
            {
                tracing
                    .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(options.SamplingRatio)))
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        // Filter out infrastructure requests
                        options.Filter = ctx => 
                            ctx.Request.Path != "/health"
                                && ctx.Request.Path != "/api/version"
                                && ctx.Request.Path != "/ready"
                                && ctx.Request.Path != "/metrics";
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation()
                    .AddGrpcClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddRedisInstrumentation()
                    .AddSource("RabbitMQ.Client.Publisher", "RabbitMQ.Client.Subscriber");

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
                    .AddMeter("Monq.*");

                if (options.EnablePrometheusEndpoint)
                {
                    metrics.AddPrometheusExporter();
                    services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter, OpenTelemetryStartupFilter>());
                }

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
