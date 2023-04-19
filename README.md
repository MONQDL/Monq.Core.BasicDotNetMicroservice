# The NetCore Microservice extensions library

The library contains extension methods that used by most .net core microservices.

### Installing

```powershell
Install-Package Monq.Core.BasicDotNetMicroservice
```

### Configures logging (ElasticSearch)

The extension configures logging by the Serilog.

Set up the logging extension in the `Program.cs`.
```csharp
using Monq.Core.BasicDotNetMicroservice.Extensions;
```

```csharp
public static void Main(string[] args)
{
    Console.OutputEncoding = Encoding.UTF8;
    CreateHostBuilder(args).Build().Run();
}
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureSerilogLogging()
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.ConfigureMetricsAndHealth();
            webBuilder.UseStartup<Startup>();
            webBuilder.UseUrls("http://0.0.0.0:5005");
        });
```

By default, logging to the console is used. For adding other logging outputs confugure the appsettings.json configuration file.

Logging to the ElasticSearch can be added. To achive that, add the properties from the rendered JSON format example to the appsettings.json file. 

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

### Configures Consul

The extension adds the configuration management with Consul. 

Set up the Consul configuring in the `Program.cs`.
```csharp
using Monq.Core.BasicDotNetMicroservice.Extensions;
```

```csharp
public static void Main(string[] args)
{
    Console.OutputEncoding = Encoding.UTF8;
    CreateHostBuilder(args).Build().Run();
}
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureConsul()
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.ConfigureMetricsAndHealth();
            webBuilder.UseStartup<Startup>();
            webBuilder.UseUrls("http://0.0.0.0:5005");
        });
```

For configuring Consul management the `aspnet_consul_config.json` file need to be added to the root of a project.

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

### API point with project version.

The extension adds the new API point `/api/version`. This API contains the information about the package version of the start point of a program `class Program` is located at.

Set up the Api version point in the `Program.cs`.
```csharp
using Monq.Core.BasicDotNetMicroservice.Extensions;
```

```csharp
public static void Main(string[] args)
{
    Console.OutputEncoding = Encoding.UTF8;
    CreateHostBuilder(args).Build().Run();
}
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .UseVersionApiPoint()
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.ConfigureMetricsAndHealth();
            webBuilder.UseStartup<Startup>();
            webBuilder.UseUrls("http://0.0.0.0:5005");
        });
```

### The TraceEventId logging request chain

The EventId is the unique identificator of the query execution in the current service.
The extension adds the EventId to the HTTP header, so that the chain of calls from the first service to the last service can be tracked.

Set up the logging request chain in the `Startup.cs`.
```csharp
using Microsoft.Extensions.DependencyInjection;
```

```csharp
public void Configure(IApplicationBuilder app)
{
    app.UseRouting();
    app.UseTraceEventId();
    app.UseEndpoints(e => e.MapControllers());
}
```

The `app.UseTraceEventId()` call have to be added strictly after the `UseRouting()` call in the pipeline.

### The logging user basic information

The extension adds the logging user by Id. A user name also adds to the logging if the user is authenticated.
So the user action can be tracked by the API.

Set up the user logging in the `Startup.cs`.
```csharp
using Microsoft.Extensions.DependencyInjection;
```

```csharp
public void Configure(IApplicationBuilder app)
{
    app.UseRouting();
    app.UseAuthentication();
    app.UseLogUser();
    app.UseEndpoints(e => e.MapControllers());
}
```

The `app.UseLogUser()` call have to be added strictly after the `UseAuthentication()` call in the pipeline.

### ConfigureBasicMicroservice

The extension configures the standard extension set for using by the AspNetCore MVC microservice.

Version 6.0.0 contains:

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
public static void Main(string[] args)
{
    Console.OutputEncoding = Encoding.UTF8;
    CreateHostBuilder(args).Build().Run();
}
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureBasicMicroservice()
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.ConfigureMetricsAndHealth();
            webBuilder.UseStartup<Startup>();
            webBuilder.UseUrls("http://0.0.0.0:5005");
        });
```

### The console app configuration

The extension brings simplicity to the console program configuration.

This configuration extends the `IHostBuilder`, that is why some methods are not implemented.

The `Program.cs` configuration example.

```csharp
public class Program
{
    static readonly AutoResetEvent Closing = new AutoResetEvent(false);
    static IRabbitMQCoreClientBuilder _rabbitMqCoreClientBuilder;
    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.CancelKeyPress += Exit;
        RabbitMQCoreClient.IQueueService queueService = null;
        var consoleApplication = ConsoleHost
            .CreateDefaultBuilder(args, new ConsoleHostConfigurationOptions
            {
                ConsulConfigurationOptions = new ConsulConfigurationOptions
                {
                    AppsettingsFileName = SettingsFile
                }
            })
            .ConfigureServices((hostContext, services) =>
            {
                var connectionString = hostContext.Configuration[AppConstants.Configuration.PostgresConnectionString];
                services
                    .AddDbContext<SyntheticTriggersContext>(opt => opt.UseNpgsql(connectionString));
                services.AddLogging();
                services.AddAutoMapper(typeof(Program), typeof(TrackedEntityProfile));
            })
            .ConfigureStaticAuthentication()
            .ConfigureServices(StartMessageHandlers)
            .Build() as IConsoleApplication;
        var log = consoleApplication?.Services.GetRequiredService<ILogger<Program>>();
        var consumer = consoleApplication?.Services.GetRequiredService<IQueueConsumer>();
        var exitCode = 0;
        try
        {
            consumer.Start();
            Closing.WaitOne();
        }
        catch (Exception e)
        {
            log.LogCritical(new EventId(), e, e.Message, e);
            exitCode = 1;
        }
        finally
        {
            try
            {
                consumer?.Dispose();
                Log.CloseAndFlush();
            }
            catch (Exception e)
            {
                log.LogCritical(new EventId(), e, e.Message, e);
                exitCode = 1;
            }
        }

        Environment.Exit(exitCode);
    }
    static void StartMessageHandlers(HostBuilderContext hostContext, IServiceCollection services)
    {
        _rabbitMqCoreClientBuilder = services
            services
            .AddRabbitMQCoreClient(opt => opt.Host = "localhost")
            .AddExchange("default")
            .AddConsumer()
            .AddHandler<Handler>("test_routing_key")
            .AddQueue("my-test-queue");
    }
    protected static void Exit(object sender, ConsoleCancelEventArgs args)
    {
        Console.WriteLine("Exit");
        Closing.Set();
    }
}
```

### The global exception handle

```csharp
using Monq.Core.BasicDotNetMicroservice.GlobalExceptionFilters.Filters;
```

```csharp
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddGlobalExceptionFilter()
            .AddExceptionHandler<ResponseException>((ex) =>
            {
                return new ObjectResult(JsonConvert.DeserializeObject(ex.ResponseData))
                {
                    StatusCode = (int)ex.StatusCode
                };
            })
            .AddDefaultExceptionHandlers();

        // Add framework services.
        services.AddControllers(opt => opt.Filters.Add(typeof(GlobalExceptionFilter)));
    }
```

The ```AddDefaultExceptionHandlers()``` method contains such exception handlers:
- UnauthorizedAccessException (HttpStatusCode.Unauthorized)
- NotImplementedException (HttpStatusCode.NotImplemented)
- Exception (Exception.InternalServerError)

The ```AddDefaultExceptionHandlers()``` method have to be added in the pipeline end.

### The global gRPC exception handle

```csharp
    builder.Services.AddGrpc(options =>
    {
        options.EnableGrpcGlobalExceptionHandling()
            .AddGrpcExceptionHandler<YourCustomException>((ex) =>
             {
                 throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
             });
    });
```

The ```EnableGrpcGlobalExceptionHandling()``` method add gRPC global exception handler interceptor.
The ```AddGrpcExceptionHandler<T>``` method add gRPC exception handler for the custom exception.
If custom exception catched, corresponding RpcException will be thrown.
Unmapped exception will be convert to RpcException with StatusCode Unknown.

### Authentication and authorization

#### Authorization policy

The extension adds a standard set of the security policies. The policies check for the presence of scoup in the access_token.

Version 6.0.0 includes:

- [Authorize("Authenticated")]
- [Authorize("read")]
- [Authorize("write")]

Set up the authorization policy in the `Program.cs`.
```csharp
using Monq.Core.BasicDotNetMicroservice.Extensions;
```

```csharp
public static void Main(string[] args)
{
    Console.OutputEncoding = Encoding.UTF8;
    __CreateHostBuilder(args).Build().Run();
}
public static IHostBuilder __CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureAuthorizationPolicies()
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.ConfigureMetricsAndHealth();
            webBuilder.UseStartup<Startup>();
            webBuilder.UseUrls("http://0.0.0.0:5005");
        });
```

#### API services authentication

The extension provides a standardized resource-owner flow authentication connection method for ASP.NET Core MVC.

Set up the API services authentication in the `Startup.cs`.
```csharp
using Monq.Core.BasicDotNetMicroservice.Extensions;
```

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.ConfigureSMAuthentication(Configuration);
}

public void Configure(IApplicationBuilder app)
{
    app.UseRouting();
    app.UseAuthentication();
    app.UseEndpoints(e => e.MapControllers());
}
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
var consoleApplication = ConsoleHost
            .CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                ...
                services.AddConsoleMetrics(hostContext);
                ...
            })
            .ConfigureServices(StartMessageHandlers)
            .Build() as IConsoleApplication;
```

The configuration must contain the following JSON:

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