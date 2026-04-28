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

Set up the API version point in the `Program.cs`.

```csharp
using Monq.Core.BasicDotNetMicroservice.Extensions;
```

```csharp
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapApiVersion(typeof(Program));// You can add it firs.
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
        "BaseUri": "http://influxdb:8888",
        "Database": "metrics",
        "UserName": "",
        "Password": "",
        "Consistenency": "",
        "Endpoint": "",
        "RetensionPolicy": ""
      }
    },
    "ReportingOverHttp": {
      "FlushInterval": "00:00:10",
      "HttpSettings": {
        "RequestUri": "http://localhost:9091/metrics",
        "UserName": "",
        "Password": "",
        "AuthorizationToken": "",
        "AllowInsecureSsl": false
      },
      "HttpPolicy": {
        "Timeout": "00:00:10",
        "BackoffPeriod": "00:00:01",
        "FailuresBeforeBackoff": 3
      }
    },
    "AddSystemMetrics": true
  }
}
```

#### ReportingInfluxDb

Options for InfluxDB reporting. Optional.

| Property | Type | Description |
|----------|------|-------------|
| `FlushInterval` | `TimeSpan` | Interval between flushing metrics. |
| `InfluxDb.BaseUri` | `Uri` | InfluxDB server URL. |
| `InfluxDb.Database` | `string` | Database name. |
| `InfluxDb.UserName` | `string` | Authentication username. |
| `InfluxDb.Password` | `string` | Authentication password. |
| `InfluxDb.Consistenency` | `string` | Write consistency level. |
| `InfluxDb.Endpoint` | `string` | Custom endpoint. |
| `InfluxDb.RetensionPolicy` | `string` | Retention policy name. |

#### ReportingOverHttp

Options of HTTP reporting. Optional.
For sending metrics to the Prometheus Pushgateway it is nessesary for the RequestUri ended up with "/metrics".

| Property | Type | Description |
|----------|------|-------------|
| `FlushInterval` | `TimeSpan` | Interval between flushing metrics. |
| `HttpSettings.RequestUri` | `Uri` | URL where to POST metrics. |
| `HttpSettings.UserName` | `string` | Basic auth username. |
| `HttpSettings.Password` | `string` | Basic auth password. |
| `HttpSettings.AuthorizationToken` | `string` | Authorization token for the request. |
| `HttpSettings.AllowInsecureSsl` | `bool` | Allow insecure SSL calls (self-signed certs). |
| `HttpPolicy.Timeout` | `TimeSpan` | Request timeout. |
| `HttpPolicy.BackoffPeriod` | `TimeSpan` | Backoff period after failures. |
| `HttpPolicy.FailuresBeforeBackoff` | `int` | Number of failures before entering backoff mode. |

#### AddSystemMetrics

Options for collect system usage and gc event metrics. Optional.

### Database Schema Management

The extension provides methods for automatic database schema creation and validation during application startup.

#### Automatic Schema Creation and Validation

The `CreateDbSchemaOnFirstRun<T>()` method automatically handles database schema initialization and validation:

!NOTE: It applies only to PostgreSQL.

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

#### NativeAOT-Compatible Schema Initialization

The `CreateDbSchemaOnFirstRunNative<T>()` method provides the same functionality but is fully compatible with **NativeAOT** and **trimming**.

**Why use this method?**

The standard `CreateDbSchemaOnFirstRun<T>()` uses `IMigrator.Migrate()` internally, which relies on runtime reflection to discover and execute migrations. This is incompatible with:

- NativeAOT compilation (requires all code to be known at compile time)
- Trimming (removes unused code, breaking reflection-based discovery)
- `[RequiresDynamicCode]` and `[RequiresUnreferencedCode]` warnings

The NativeAOT-compatible version uses a **pre-generated SQL script** that is embedded into the assembly at build time, eliminating all runtime reflection.

**How it works:**

1. At build time, an MSBuild target automatically generates an idempotent SQL script from your EF Core migrations
2. The script is embedded as a resource in your assembly (trimming-safe)
3. At runtime, the script is executed directly via `ExecuteSqlRaw` — no reflection needed

**Configuration steps:**

**Step 1: Ensure you have EF Core migrations**

Your project must have a `Migrations/` folder with migration files. If not, create an initial migration:

```bash
dotnet ef migrations add InitialCreate
```

**Step 2: Add required packages**

Ensure your project references the design-time tools:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

**Step 3: Use the NativeAOT method in Program.cs**

No manual `IDesignTimeDbContextFactory` is required! The MSBuild target automatically generates a temporary factory during build.

By convention, the target expects your DbContext to be named `$(AssemblyName)Context`. For example, if your assembly is `MyApp.Api`, the DbContext should be `MyApp.ApiContext`.

If your DbContext has a different name, specify it in your `.csproj`:

```xml
<PropertyGroup>
  <MonqEfDbContextName>MyCustomDbContext</MonqEfDbContextName>
</PropertyGroup>
```

```csharp
using Microsoft.Extensions.DependencyInjection;

var app = builder.Build();

// Option 1: Load SQL from embedded resource (recommended)
app.CreateDbSchemaOnFirstRunNative<MyDbContext>(
    typeof(Program).Assembly,
    "PgSchema.sql");

// Option 2: Provide SQL via delegate
app.CreateDbSchemaOnFirstRunNative<MyDbContext>(
    () => {
        using var stream = typeof(Program).Assembly
            .GetManifestResourceStream("PgSchema.sql");
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    });

app.Run();
```

**MSBuild Target (automatic SQL generation):**

When you reference `Monq.Core.BasicDotNetMicroservice`, an MSBuild target is automatically imported. It activates when a `Migrations/` folder exists in your project.

**What the target does:**

1. Runs `dotnet ef migrations script --idempotent` after each build
2. Extracts migration names from `Migrations/*.Designer.cs` files
3. Prepends `-- MONQ_MIGRATIONS: ["Migration1", "Migration2"]` metadata to the SQL
4. Embeds `PgSchema.sql` as a resource in your assembly

**Build workflow:**

- **First build:** Generates `PgSchema.sql` in your project root
- **Second build:** Embeds `PgSchema.sql` as a resource (available at runtime)
- **Subsequent builds:** Regenerates SQL if migrations changed, keeps resource embedded

**Customization properties:**

You can override the default behavior in your `.csproj`:

```xml
<PropertyGroup>
  <!-- Disable automatic generation -->
  <MonqEfMigrationsEnabled>false</MonqEfMigrationsEnabled>
  
  <!-- Change output filename -->
  <MonqEfMigrationsOutputFileName>MySchema.sql</MonqEfMigrationsOutputFileName>
  
  <!-- Change migrations folder location -->
  <MonqEfMigrationsFolder>Database/Migrations</MonqEfMigrationsFolder>
  
  <!-- Override DbContext name (default: $(AssemblyName)Context) -->
  <MonqEfDbContextName>MyCustomDbContext</MonqEfDbContextName>
</PropertyGroup>
```

**Connection string for design-time factory:**

The auto-generated factory uses a default connection string. To override it for migration generation, set the environment variable:

```bash
# Linux/macOS
export MONQ_DESIGN_TIME_CONNECTION_STRING="host=db;database=mydb;username=postgres;password=secret"

# Windows (PowerShell)
$env:MONQ_DESIGN_TIME_CONNECTION_STRING="host=db;database=mydb;username=postgres;password=secret"

dotnet build
```

**Example of complete setup:**

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <PublishAot>true</PublishAot>
    <!-- Optional: Override DbContext name if not following convention -->
    <!-- <MonqEfDbContextName>MyCustomDbContext</MonqEfDbContextName> -->
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Monq.Core.BasicDotNetMicroservice" Version="9.2.0" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

```csharp
// Program.cs
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

// NativeAOT-safe schema initialization
app.CreateDbSchemaOnFirstRunNative<MyDbContext>(
    typeof(Program).Assembly,
    "PgSchema.sql");

app.Run();
```

**How the auto-generated factory works:**

During build, the MSBuild target:

1. Checks if `Migrations/DesignTimeDbContextFactory.cs` exists
2. If not, generates a temporary factory for your DbContext type
3. Uses the factory to run `dotnet ef migrations script`
4. Deletes the temporary factory after SQL generation

This eliminates the need to manually create and maintain `IDesignTimeDbContextFactory` in each microservice.

### Migration guide to v9

1. Replace `services.ConfigureSMAuthentication()` with `services.ConfigureMonqAuthentication()`.
2. Replace `RestHttpClientFromOptions<T>` with `RestHttpClient` and remove unnecessary injections in implementation class constructor.
3. Replace DI registration for all classes that are derived from `RestHttpClient` with

```csharp
services.AddRestHttpPreConfiguredClient<IService, Service>();
```
