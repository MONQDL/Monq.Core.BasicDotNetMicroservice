# The NetCore Microservice extensions library

The library contains extension methods that used by most .NET Core microservices.

### Installing

```powershell
Install-Package Monq.Core.BasicDotNetMicroservice
```

### Configure logging (ElasticSearch)

The extension configures Serilog logging.

Set up the logging extension in the `Program.cs`.

```csharp
using Monq.Core.BasicDotNetMicroservice.Extensions;
```

```csharp
Console.OutputEncoding = Encoding.UTF8;
var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureSerilogLogging();
```

By default, logging to the console is used. For adding other logging outputs confugure the `appsettings.json` configuration file.

Logging to the ElasticSearch can be added. To achive that, add the properties from the rendered JSON format example to the `appsettings.json` file.

```json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "Elasticsearch",
        "Args": {
          "nodeUris": "http://els-1.example.com",
          "indexFormat": "aspnetcore-{0:yyyy.MM.dd}",
          "typeName": "aspnet_events",
          "autoRegisterTemplate": true
        }
      }
    ],
    "MinimumLevel": "Information"
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
    "Token" : "",
    "RootFolder": "custom-root/keys"
}
```

`"Address"` - the http(s) address of a Consul server, where the configuration is stored.

`"Token"` - the token to connect to the Consul server.

`"RootFolder"` - the path to the Consul configuration. By default equals the `ASPNETCORE_ENVIRONMENT` variable.

The `aspnet_consul_config.json` file path can be set by the environment variable `ASPNETCORE_CONSUL_CONFIG_FILE`.

### API point with project version

The extension adds the new API point `/api/version`. This API contains information about the package version of the program entry point where `class Program` is located at.

Set up the API version point in the `Program.cs`.

```csharp
using Monq.Core.BasicDotNetMicroservice.Extensions;
```

```csharp
Console.OutputEncoding = Encoding.UTF8;
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseVersionApiPoint();
```

### TraceEventId logging request chain

The EventId is the unique identificator of the query execution in the current service.
The extension adds the EventId to the HTTP header, so that the chain of calls from the first service to the last service can be tracked.

Set up the logging request chain in the `Program.cs`.

```csharp
using Microsoft.Extensions.DependencyInjection;
```

```csharp
var app = builder.Build();

app.UseRouting();
app.UseTraceEventId();
app.MapControllers();
```

The `app.UseTraceEventId()` call have to be added strictly after the `app.UseRouting()` call in the pipeline.

### Logging user basic information

The extension adds the logging user by Id. A user name also adds to the logging if the user is authenticated.
So the user action can be tracked by the API.

Set up the user logging in the `Program.cs`.

```csharp
using Microsoft.Extensions.DependencyInjection;
```

```csharp
var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseLogUser();
app.MapControllers();
```

The `app.UseLogUser()` call have to be added strictly after the `app.UseAuthentication()` call in the pipeline.

### ConfigureBasicMicroservice

The extension configures the standard extension set for use by the ASP.NET Core MVC microservice.

Version 7.0.0 contains:

- hostBuilder.ConfigureCustomCertificates();
- hostBuilder.ConfigureConsul();
- hostBuilder.ConfigureSerilogLogging();
- hostBuilder.UseVersionApiPoint();
- hostBuilder.ConfigureAuthorizationPolicies();
- hostBuilder.ConfigBasicHttpService();

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

Version 7.0.0 contains:

- hostBuilder.ConfigureCustomCertificates();
- hostBuilder.ConfigureConsul();
- hostBuilder.ConfigureSerilogLogging();
- hostBuilder.UseVersionApiPoint();
- hostBuilder.ConfigBasicHttpService();
- hostBuilder.UseConsoleLifetime();

```csharp
hostBuilder.ConfigureServices((context, services) =>
{
    services.AddHttpContextAccessor();
    services.AddOptions();
    services.AddDistributedMemoryCache();
    services.Configure<AppConfiguration>(context.Configuration);
    services.AddConsoleMetrics(context);
});
```

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
        services.AddConsoleMetrics(builder);
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

### Authentication and authorization

#### Authorization policy

The extension adds a standard set of the security policies. The policies check for the presence of scoup in the access_token.

Version 7.0.0 includes:

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
builder.Services.ConfigureSMAuthentication(Configuration);

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

The extension adds the ability to send message queue metrics, tasks metrics, system load metrics and garbage collector metrics.

Set up the metrics sending in the `Program.cs`.

```csharp
using Monq.Core.BasicDotNetMicroservice.Extensions;
```

```csharp
Console.OutputEncoding = Encoding.UTF8;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddConsoleMetrics(context);
    })
    .UseConsoleLifetime()
    .Build();
```

The configuration should contain the following JSON:

```json
{
  "Metrics": {
    "ReportingInfluxDb": {
      "FlushInterval": "00:00:10",
      "InfluxDb": {
        "Consistenency": "",
        "Endpoint": "",
        "BaseUri": "http://influxdb:8888",
        "Database": "",
        "Password": "",
        "RetensionPolicy": "",
        "UserName": ""
      }
    },
    "ReportingOverHttp": {
      "FlushInterval": "00:00:10",
      "HttpSettings": {
        "RequestUri": "http://localhost:9091/metrics"
      }
    },
    "AddSystemMetrics": true
  }
}
```

#### ReportingInfluxDb

Options for InfluxDB reporting. Optional.

#### ReportingOverHttp

Options of HTTP reporting. Optional.
For sending metrics to the Prometheus Pushgateway it is nessesary for the RequestUri ended up with "/metrics".

#### AddSystemMetrics

Options for collect system usage and gc event metrics. Optional.
