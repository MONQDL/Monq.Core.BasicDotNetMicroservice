# The NetCore Microservice extensions library

The library contains extension methods that used by most .NET Core microservices.

Version 10.0.0 introduces **OpenTelemetry** for distributed tracing and metrics, replacing App.Metrics.

### Installing

```powershell
Install-Package Monq.Core.BasicDotNetMicroservice
```

### Configure logging (ElasticSearch)

The extension configures Serilog logging with automatic `TraceId`/`SpanId` enrichment from OpenTelemetry.

Set up the logging extension in the `Program.cs`.

```csharp
using Monq.Core.BasicDotNetMicroservice.Extensions;
```

```csharp
Console.OutputEncoding = Encoding.UTF8;
var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureSerilogLogging();
```

By default, logging to the console is used. For adding other logging outputs configure the `appsettings.json` configuration file.

Logging to the ElasticSearch can be added. To achieve that, add the properties from the rendered JSON format example to the `appsettings.json` file.

More documentation at [Elastic](https://github.com/elastic/ecs-dotnet/tree/main/src/Elastic.Serilog.Sinks)

```json
{
  "Serilog": {
    "Using": [ "Elastic.Serilog.Sinks" ],
    "WriteTo": [
      {
        "Name": "Elasticsearch",
        "Args": {
          "bootstrapMethod": "Silent",
          "nodes": [ "http://elastichost:9200" ],
          "useSniffing": true,
          "apiKey": "<apiKey>",
          "username": "<username>",
          "password": "<password>",

          "ilmPolicy" : "my-policy",
          "dataStream" : "logs-dotnet-default",
          "includeHost" : true,
          "includeUser" : true,
          "includeProcess" : true,
          "includeActivity" : true,
          "filterProperties" : [ "prop1", "prop2" ],
          "proxy": "http://localhost:8200",
          "proxyUsername": "x",
          "proxyPassword": "y",
          "debugMode": false,

          //EXPERT settings, do not set unless you need to
          "maxRetries": 3,
          "maxConcurrency": 20,
          "maxInflight": 100000,
          "maxExportSize": 1000,
          "maxLifeTime": "00:00:05",
          "fullMode": "Wait"
        }
      }
    ],
    "MinimumLevel": { "Default": "Information" },
  }
}
```

### Configure Consul

The extension adds the configuration management with Consul.

Set up the Consul configuring in the `Program.cs`.

```csharp
using Monq.Core.BasicDotNetMicroservice.Extensions;
```

```csharp
Console.OutputEncoding = Encoding.UTF8;
var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureConsul();
```

To configure Consul management, you need to add the `aspnet_consul_config.json` file to the root of your project.

```json
{
    "Address" : "http://127.0.0.1:8500",
    "Datacenter" : "dc1",
    "Token" : "",
    "WaitTime" : "00:00:05",
    "RootFolder": "custom-root/keys"
}
```

`"Address"` - the http(s) address of a Consul server, where the configuration is stored.

`"Datacenter"` - the datacenter to use. Optional.

`"Token"` - the token to connect to the Consul server.

`"WaitTime"` - the maximum time to wait for a blocking query. Optional.

`"RootFolder"` - the path to the Consul configuration. By default equals the `ASPNETCORE_ENVIRONMENT` variable.

The `aspnet_consul_config.json` file path can be set by the environment variable `ASPNETCORE_CONSUL_CONFIG_FILE`.

### API point with project version

The extension adds the new API point `/api/version`. This API contains information about the package version of the program entry point where `class Program` is located at.

The endpoint returns a JSON response in the following format:

```json
{
  "version": "1.2.3.4"
}
```

Set up the API version point in the `Program.cs`.

```csharp
using Monq.Core.BasicDotNetMicroservice.Extensions;
```

```csharp
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapApiVersion(typeof(Program));
```

### Distributed tracing

Distributed tracing works automatically via OpenTelemetry. The `AddAspNetCoreInstrumentation()` creates an `Activity` for each incoming HTTP request, and the `ActivityTraceEnricher` automatically adds `TraceId` and `SpanId` to all log entries.

No middleware is required — just configure OpenTelemetry in `appsettings.json` and call `AddMonqOpenTelemetry()` during service registration (done automatically by `ConfigureBasicMicroservice()`).

### User logging

User `UserId` (from `sub` claim) and `UserName` are automatically added to all log entries via the `UserEnricher`. No middleware is required — the enricher reads the user from `IHttpContextAccessor` after authentication.

### ConfigureBasicMicroservice

The extension configures the standard extension set for use by the ASP.NET Core MVC microservice.

Version 10.0.0 contains:

- hostBuilder.ConfigureCustomCertificates();
- hostBuilder.ConfigureConsul();
- hostBuilder.ConfigureSerilogLogging();
- hostBuilder.UseVersionApiPoint();
- hostBuilder.ConfigureAuthorizationPolicies();
- hostBuilder.ConfigBasicHttpService();
- services.AddMonqMetrics();
- services.AddMonqOpenTelemetry();

```csharp
hostBuilder.ConfigureServices((context, services) =>
{
    services.AddHttpContextAccessor();
    services.AddOptions();
    services.AddDistributedMemoryCache();
    services.Configure<AppConfiguration>(context.Configuration);
});
```

Set up the basic microservice extension in the `Program.cs`.

```csharp
using Monq.Core.BasicDotNetMicroservice.Extensions;
```

```csharp
Console.OutputEncoding = Encoding.UTF8;
var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureBasicMicroservice();
```

### ConfigureBasicConsoleMicroservice

The extension configures the standard extension set for use by the console hosting microservice.

Version 10.0.0 contains:

- hostBuilder.ConfigureCustomCertificates();
- hostBuilder.ConfigureConsul();
- hostBuilder.ConfigureSerilogLogging();
- hostBuilder.UseVersionApiPoint();
- hostBuilder.ConfigBasicHttpService();
- hostBuilder.UseConsoleLifetime();
- services.AddMonqMetrics();
- services.AddMonqOpenTelemetry();

Set up the basic console microservice extension in the `Program.cs`.

```csharp
Console.OutputEncoding = Encoding.UTF8;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureBasicConsoleMicroservice()
    .Build();
```

Big Example of configuring Console host

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monq.Core.BasicDotNetMicroservice;
using Monq.Core.BasicDotNetMicroservice.Extensions;
using Monq.Core.BasicDotNetMicroservice.Models;
using Monq.Core.ClickHouseBuffer;
using Monq.Core.ClickHouseBuffer.DependencyInjection;
using Monq.Core.Service.Statistics.Buffer;
using Serilog;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureBasicConsoleMicroservice(new ConsulConfigurationOptions { AppsettingsFileName = "appsettings-buffer.json" })
    .ConfigureServices((builder, services) =>
    {
        // Infra
        var clickHouseConnectionString = builder.Configuration[MicroserviceConstants.ConfigConstants.ClickHouseConnectionString];
        services.ConfigureCHBuffer(builder.Configuration.GetSection(Monq.Core.Service.Statistics.Buffer.Configuration.AppConstants.Configuration.BufferEngineOptions), clickHouseConnectionString);

        // Services
        services.AddTransient<IPersistRepository, ClickHousePersistRepository>();
        AddRabbitClient(services, builder.Configuration);
    })
    .UseConsoleLifetime()
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

var exitCode = 0;
try
{
    host.Services
        .GetRequiredService<IQueueConsumer>()
        .Start();
    await host.RunAsync();
}
catch (Exception e)
{
    logger.LogCritical(e, e.Message);
    exitCode = 1;
}
finally
{
    try
    {
        await Log.CloseAndFlushAsync();
    }
    catch (Exception e)
    {
        logger.LogCritical(e, e.Message);
    }
    finally
    {
        Environment.Exit(exitCode);
    }
}

static void AddRabbitClient(IServiceCollection services, IConfiguration configuration)
{
    services
        .AddRabbitMQCoreClientConsumer(
            configuration.GetSection(MicroserviceConstants.ConfigConstants.RabbitMq))
        .AddHandler<Monq.Core.Service.Statistics.Buffer.QueueHandlers.StatisticsUploadMessageHandler>(Monq.Core.Models.Statistics.RoutingKeys.Inbound.StatisticsItemUpload);
}
```

### Global exception handling

```csharp
using Monq.Core.BasicDotNetMicroservice.GlobalExceptionFilters.Filters;
```

```csharp
builder.Services.AddGlobalExceptionFilter()
    .AddExceptionHandler<ResponseException>((ex) =>
    {
        return new ObjectResult(JsonConvert.DeserializeObject(ex.ResponseData))
        {
            StatusCode = (int)ex.StatusCode
        };
    })
    .AddDefaultExceptionHandlers();

// Add framework services.
builder.Services.AddControllers(opt => opt.Filters.Add(typeof(GlobalExceptionFilter)));
```

The ```AddDefaultExceptionHandlers()``` method contains such exception handlers as:

- UnauthorizedAccessException (HttpStatusCode.Unauthorized)
- NotImplementedException (HttpStatusCode.NotImplemented)
- Exception (Exception.InternalServerError)

The ```AddDefaultExceptionHandlers()``` method have to be added in the pipeline end.

### gRPC client configuration

```csharp
builder.Services.AddGrpcPreConfiguredClient(builder.Configuration, opt =>
{
    opt.Name = "clientName";
    opt.ClientOptionsAction = o => o.Address = new Uri("http://localhost");
});

builder.Services.AddGrpcPreConfiguredConsoleClient(builder.Configuration, opt =>
{
    opt.Name = "clientName";
    opt.ClientOptionsAction = o => o.Address = new Uri("http://localhost");
});
```

The library provides extensions for configuring `Grpc.AspNetCore` clients both for web API applications and for console applications with static authentication.

Configuration options are defined in `GrpcClientOptions` class. Default options are:

```csharp
static readonly Action<GrpcClientOptions, IConfiguration> _configureGrpcClient = (o, configuration) =>
{
    o.ClientOptionsAction = (clientOptions) =>
    {
        clientOptions.Address = new Uri(configuration.GetValue<string>(nameof(AppConfiguration.BaseUri)) ?? "http://localhost");
    };
    o.ChannelOptionsAction = (channelOptions) =>
    {
        channelOptions.UnsafeUseInsecureChannelCallCredentials = true;
        channelOptions.MaxReceiveMessageSize = 51 * 1024 * 1024; // 51 Mb.
    };
    o.ContextPropagationOptionsAction = (propagationOptions) =>
    {
        propagationOptions.SuppressContextNotFoundErrors = true;
    };
};
```

### Global gRPC exception handling

```csharp
builder.Services.AddGrpc(options =>
{
    options.EnableGrpcGlobalExceptionHandling()
        .AddGrpcExceptionHandler<YourCustomException>((ex) =>
        {
            return new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        });
});
```

The ```EnableGrpcGlobalExceptionHandling()``` method adds gRPC global exception handler interceptor.
The ```AddGrpcExceptionHandler<T>``` method adds gRPC exception handler for custom exceptions.
If custom exception catched, corresponding `RpcException` will be thrown.
Unmapped exception will be converted to `RpcException` with `StatusCode.Unknown` and detail message containing serialized json (see `Monq.Core.BasicDotNetMicroservice.GlobalExceptionFilters.Models.ErrorResponse`):

```json
{
  "Message": "exceptionMessage",
  "StackTrace": "exceptionStackTrace" // can be null
}
```

### gRPC request validation

The extension adds gRPC request messages validation.

```csharp
builder.Services.AddGrpcRequestValidation();
```

It works alongside with inline validator registration. More info: <https://github.com/AnthonyGiretti/grpc-aspnetcore-validator#add-inline-custom-validator>.

```csharp
builder.Services.AddInlineValidator<YourRequest>(rules =>
{
    rules.RuleFor(x => x.Id)
        .InclusiveBetween(1, long.MaxValue)
        .WithMessage("Wrong id.");
});
```

### REST HTTP client configuration

```csharp
builder.Services.AddRestHttpPreConfiguredClient(builder.Configuration, client =>
{
    client.Timeout = TimeSpan.FromMinutes(1);
});
```

The library provides extensions for configuring REST HTTP clients. More info: <https://github.com/MONQDL/Monq.Core.HttpClientExtensions/blob/master/README.md>.

### Authentication and authorization

#### Authorization policy

The extension adds a standard set of the security policies. The policies check for the presence of scope in the access_token.

Version 10.0.0 includes:

- [Authorize("Authenticated")]
- [Authorize("read")]
- [Authorize("write")]

Set up the authorization policy in the `Program.cs`.

```csharp
using Monq.Core.BasicDotNetMicroservice.Extensions;
```

```csharp
Console.OutputEncoding = Encoding.UTF8;
var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureAuthorizationPolicies();
```

#### API services authentication

The extension provides a standardized resource-owner flow authentication connection method for ASP.NET Core MVC.

Set up the API services authentication in the `Program.cs`.

```csharp
using Monq.Core.BasicDotNetMicroservice.Extensions;
```

```csharp
builder.Services.ConfigureMonqAuthentication(Configuration);

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.MapControllers();
```

The example of the configuration in JSON format:

```json
{
  "Authentication": {
    "AuthenticationEndpoint": "https://identity.example.com",
    "ApiResource": {
      "Login": "service-api",
      "Password": "RIHY1vsevEO7WEVC"
    },
    "RequireHttpsMetadata": false,
    "EnableCaching": true
  }
}
```

#### RequireHttpsMetadata

Optional. By default is false.

#### EnableCaching

Optional. By default is true.

Data cache duration: 5 minutes.

### Metrics sending

Metrics are handled by OpenTelemetry. See the [OpenTelemetry](#opentelemetry) section for configuration.

The `MonqMetrics` class is available via DI for recording custom RabbitMQ and task metrics.

### Database Schema Management

The extension provides methods for automatic database schema creation and validation during application startup.

#### Automatic Schema Creation and Validation

The `CreateDbSchemaOnFirstRun<T>()` method automatically handles database schema initialization and validation:

> NOTE: It applies only to PostgreSQL.

- If the database is empty, it creates the schema by applying all migrations
- If the database already has tables, it validates that all migrations have been applied
- If there are unapplied migrations in an existing database, it throws a `DbSchemaValidationException`

**Usage example:**

```csharp
using Monq.Core.BasicDotNetMicroservice.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext to services
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();
// Initialize database schema on application startup
app.CreateDbSchemaOnFirstRun<MyDbContext>();

app.Run();
```

**Method signature:**

```csharp
public static void CreateDbSchemaOnFirstRun<T>(this IApplicationBuilder app,
    bool terminateOnException = true,
    bool sleepBeforeTerminate = true,
    int terminationSleepMilliseconds = 10000)
    where T : DbContext
```

**Parameters:**

- `T` - The concrete database context type
- `terminateOnException` - If true, the application will terminate when an exception occurs (default: true)
- `sleepBeforeTerminate` - If true and `terminateOnException` is true, the main thread will sleep before termination (default: true)
- `terminationSleepMilliseconds` - Sleep interval when `terminateOnException` is true and `sleepBeforeTerminate` is true (default: 10000ms)

## OpenTelemetry

The library includes built-in OpenTelemetry support for distributed tracing and metrics.

### Distributed tracing

Automatically instruments:
- ASP.NET Core (Controllers and Minimal API)
- HTTP clients (including `RestHttpClient` from `Monq.Core.HttpClientExtensions`)
- gRPC clients
- RabbitMQ (via `RabbitMQCoreClient` library)

Trace context is propagated via W3C `traceparent`/`tracestate` headers. Every HTTP request, gRPC call, and RabbitMQ message carries the trace context, enabling full distributed tracing across your microservice ecosystem.

### RabbitMQ distributed tracing

When using `RabbitMQCoreClient` (v7.x+), trace context is automatically propagated:

- **On publish**: A `Producer` span is created and `traceparent`/`tracestate` are injected into message headers
- **On consume**: `traceparent`/`tracestate` are extracted from message headers and a `Consumer` span is created with the correct parent context

This enables end-to-end tracing across long message processing chains:

```
HTTP Request → ASP.NET Span → Publish to RabbitMQ → Consumer Span → Handler Span → HTTP/gRPC calls → ...
```

### Metrics

Automatically collects:
- ASP.NET Core request metrics
- HTTP client metrics
- .NET Runtime metrics (GC, CPU, memory)
- Custom metrics via `MonqMetrics` class

### Prometheus endpoint

When `EnablePrometheusEndpoint` is `true`, a `/metrics` endpoint is automatically exposed for Prometheus scraping. No additional setup is required — the endpoint is registered via `IStartupFilter` when `ConfigureBasicMicroservice()` is called.

Prometheus scraping requests to `/metrics` are automatically excluded from distributed tracing.

### Configuration

Add OpenTelemetry configuration to `appsettings.json`:

```json
{
  "OpenTelemetry": {
    "ServiceName": "my-microservice",
    "Otlp": {
      "Endpoint": "http://otel-collector:4317",
      "Protocol": "grpc",
      "Headers": ""
    },
    "EnableTracing": true,
    "EnableMetrics": true,
    "EnablePrometheusEndpoint": true,
    "SamplingRatio": 1.0
  }
}
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ServiceName` | `string` | Entry assembly name | Service name in traces |
| `ServiceVersion` | `string?` | Entry assembly version | Service version |
| `Otlp.Endpoint` | `string` | `http://localhost:4317` | OTLP collector endpoint |
| `Otlp.Protocol` | `string` | `grpc` | `grpc` or `http/protobuf` |
| `Otlp.Headers` | `string?` | `""` | Additional headers (e.g., `api-key=secret`) |
| `EnableTracing` | `bool` | `true` | Enable distributed tracing |
| `EnableMetrics` | `bool` | `true` | Enable metrics collection |
| `EnablePrometheusEndpoint` | `bool` | `true` | Expose `/metrics` endpoint |
| `SamplingRatio` | `double` | `1.0` | Trace sampling ratio (0.0–1.0). Uses `ParentBasedSampler` with `TraceIdRatioBasedSampler`. |

### Resource attributes

The following resource attributes are automatically added to all telemetry data, following OpenTelemetry semantic conventions:

| Attribute | Source | Description |
|-----------|--------|-------------|
| `deployment.environment.name` | `IHostEnvironment.EnvironmentName` | Hosting environment (e.g., `Development`, `Production`) |
| `service.microservice` | `ASPNETCORE_APPLICATION_NAME` env var | Microservice name |
| `host.name` | `HOSTNAME` env var | Kubernetes pod name or host name |

Example telemetry output:

```json
{
  "deployment.environment.name": "Production",
  "service.microservice": "my-microservice",
  "host.name": "pod-abc123",
  "service.version": "1.2.3.4",
  "service.instance.id": "54cbf61b-e3c4-4d89-a58e-a483c0337721",
  "telemetry.sdk.language": "dotnet",
  "telemetry.sdk.name": "opentelemetry",
  "telemetry.sdk.version": "1.16.0"
}
```

### Custom metrics

Use the `MonqMetrics` class to record custom business metrics:

```csharp
public class MyService
{
    readonly MonqMetrics _metrics;

    public MyService(MonqMetrics metrics) => _metrics = metrics;

    public void HandleMessage(string handlerName)
    {
        _metrics.IncrementRabbitMQReceived(handlerName);

        _metrics.MeasureRabbitMQPreprocessingTime(() =>
        {
            // processing logic
        });
    }
}
```

Or use `System.Diagnostics.Metrics` directly:

```csharp
using System.Diagnostics.Metrics;

public class MyService
{
    static readonly Meter Meter = new("MyService", "1.0");
    static readonly Counter<long> ItemsProcessed = Meter.CreateCounter<long>("items.processed");

    public void Process() => ItemsProcessed.Add(1, new KeyValuePair<string, object?>("type", "document"));
}
```

Custom meters are automatically picked up by OpenTelemetry when the meter name matches the `Monq.Core.*` pattern. To use custom meter names, configure them in `AddMonqOpenTelemetry()`.

### Complete ASP.NET Core example

```csharp
using Monq.Core.BasicDotNetMicroservice.Extensions;
using Monq.Core.BasicDotNetMicroservice.Metrics;

Console.OutputEncoding = Encoding.UTF8;
var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureBasicMicroservice();

builder.Services.AddControllers();

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
```

### Complete Console Host example

```csharp
using Monq.Core.BasicDotNetMicroservice.Extensions;
using Monq.Core.BasicDotNetMicroservice.Metrics;

Console.OutputEncoding = Encoding.UTF8;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureBasicConsoleMicroservice()
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<MyBackgroundWorker>();
    })
    .UseConsoleLifetime()
    .Build();

await host.RunAsync();

public class MyBackgroundWorker : BackgroundService
{
    readonly MonqMetrics _metrics;
    readonly ILogger<MyBackgroundWorker> _logger;

    public MyBackgroundWorker(MonqMetrics metrics, ILogger<MyBackgroundWorker> logger)
    {
        _metrics = metrics;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _metrics.IncrementTasksReceived("my-task");

            await _metrics.MeasureTasksPreprocessingTimeAsync(async () =>
            {
                _logger.LogInformation("Processing task...");
                await Task.Delay(1000, stoppingToken);
            });

            _metrics.IncrementTasksProcessed("my-task");
        }
    }
}
```

## Migration guides

- [Migration to v10](v10-MIGRATION.md)
- [Migration to v9](v9-MIGRATION.md)
