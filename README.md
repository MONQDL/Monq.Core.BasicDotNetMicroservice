# Библиотека поддержки микросервисов .net core

Библиотека содержит набор методов, который применяется большинством микросервисов .net core.

### Установка

```powershell
Install-Package Monq.Core.BasicDotNetMicroservice
```

### Логирование (ElasticSearch)

Данное расширение предлагает переключение из стандартного механизма логирования ASP.NET на Serilog.

Подключение производится в файле `Program.cs`.
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

По умолчанию используется логирование в консоль, но возможно использование конфигурации из appsettings.json.

Данный пакет содержит sinc для подключения к серверу ElasticSearch. В данном случае конфигурация будет такого вида:

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

### Использования центрального хранилища конфигураций Consul

Данное расширение предлагает альтернативный механизм загрузки файла конфигурации appsettings.json, используя центральное хранилище конфигураций `Consul`.

Подключение производится в файле `Program.cs`.
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

Реализация данного расширения предполагает наличие файла `aspnet_consul_config.json` в корне проекта с таким содержанием:

```json
{
    "Address" : "http://127.0.0.1:8500",
    "Token" : "",
    "RootFolder": "custom-root/keys"
}
```
где 

`"Address"` - http(s) адрес сервера Consul, с которого можно получить конфигурацию;

`"Token"` - токен доступа к серверу.

`"RootFolder"` - базовый путь к конфигурации Consul. Если опция не задана, то будет использоваться значение переменной _ASPNETCORE_ENVIRONMENT_.

Путь к файлу `aspnet_consul_config.json` можно задать через переменную среды `ASPNETCORE_CONSUL_CONFIG_FILE`.

В реализации 1.1.0 предполагается, что конфигурация содержится в по пути:

`$"{rootFolder}/{applicationName?.ToLower()}/appsettings.json"`,

где

`applicationName` - название программы из переменной среды `ASPNETCORE_APPLICATION_NAME` *или*, если переменная среды не задана Environment.ApplicationName.

### Точка API с версией проекта.

Данное расширение вносит дополнительную точку API `/api/version` содержающую информацию по версии пакета (сборки), в которой располагается точка запуска программы `class Program`.

Подключение производится в файле `Program.cs`.
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

### Логирование цепочек запросов TraceEventId

Данное расширение вводит механизм добавления идентификатора запроса из HTTP заголовка, если он присутствовал, как уникальный идентификатор выполнения запроса MVC в текущем сервисе.
Таким образом по заголовку можно отследить какой запрос в вызывающем сервисе привел к выполнению запроса в вызываемом сервисе и так по всей цепочке от первого вызывающего до последнего
вызываемого сервиса.

Подключение производится в файле `Startup.cs`.
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

`app.UseTraceEventId();` должен стоять строго после _UseRouting_ в цепочке вызовов (pipline).

### Логирование базовой информации по пользователю

Данное расширение вводит механизм логирования Id, и имени пользователя, если он аутентифицирован.
Таким образом можно отслеживать действия пользователя по API.

Подключение производится в файле `Startup.cs`.
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

`app.UseLogUser();` должен стоять строго после _UseAuthentication_ в цепочке вызовов (pipline).

### ConfigureBasicMicroservice

Данное расширение выполняет подключение стандартного набора расширений для использования микросервисом типа AspNetCore MVC  в повседневной жизни.

В версии 4.0.0 включает в себя:

- hostBuilder.ConfigureCustomCertificates();
- hostBuilder.ConfigureConsul();
- hostBuilder.ConfigureSerilogLogging();
- hostBuilder.UseVersionApiPoint();
- hostBuilder.ConfigureAuthorizationPolicies();
- hostBuilder.ConfigBasicHttpService();

hostBuilder.ConfigureServices((context, services) =>
{
    services.AddHttpContextAccessor();
    services.AddOptions();
    services.AddDistributedMemoryCache();
    services.Configure<AppConfiguration>(context.Configuration);
});

Подключение производится в файле `Program.cs`.
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

### Конфигуратор консольной программы

Набор расширений предоставляет возможность выполнять конфигурацию консольных программ в максимально упрощенном виде.

Конфигуратор расширяет интерфейс `IHostBuilder`, поэтому некоторые методы не реализованы.

Пример конфигурации `Program.cs`.

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

### Глобальная обработка исключений

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

Метод ```.AddDefaultExceptionHandlers``` содержит в себе обработчики
- UnauthorizedAccessException (HttpStatusCode.Unauthorized)
- NotImplementedException (HttpStatusCode.NotImplemented)
- Exception (Exception.InternalServerError)

Так как метод ```.AddDefaultExceptionHandlers``` содержит обработку ```Exception```, то его следует подключать в конце.

### Аутентификация и авторизация

#### Политики авторизации

Данное расширение выполняет подключение стандартного набора политик безопасности. Политики проверяют наличие scoup в access_token.

В версии 3.1.1 включает в себя:

- [Authorize("Authenticated")]
- [Authorize("read")]
- [Authorize("write")]

Подключение производится в файле `Program.cs`.
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

#### Аутентификация API сервисов

Расширение предоставляет стандартизированный метод подключения аутентификации с помощью resource-owner flow для ASP.NET Core MVC.

Подключение производится в файле `Startup.cs`.
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

Конфигурация должна при этом содержать такой JSON:

```json
{
  "Authentication": {
    "AuthenticationEndpoint": "https://identity.example.com",
    "ApiResource": {
      "Login": "service-api",
      "Password": "RIHY1vsevEO7WEVC"
    },
    "RequireHttpsMetadata": false, // не обязательно. По умолчанию false.
	"EnableCaching": true          // не обязательно. По умолчанию true.
  }
```

Длительность кэширования данных: 5 мин.