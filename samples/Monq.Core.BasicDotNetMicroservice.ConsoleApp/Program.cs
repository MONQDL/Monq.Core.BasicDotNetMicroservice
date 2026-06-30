using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monq.Core.BasicDotNetMicroservice;
using Monq.Core.BasicDotNetMicroservice.Configuration;
using Monq.Core.BasicDotNetMicroservice.ConsoleApp.Grpc;
using Monq.Core.BasicDotNetMicroservice.ConsoleApp.Handlers;
using Monq.Core.BasicDotNetMicroservice.Extensions;
using RabbitMQCoreClient.DependencyInjection;
using System.Diagnostics;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureBasicConsoleMicroservice(new ConsulConfigurationOptions
    {
        AppsettingsFileName = "appsettings-async.json"
    })
    .ConfigureServices((builder, services) =>
    {
        Console.WriteLine("HostName" + builder.Configuration.GetSection("RabbitMQ")["HostName"]);
        Console.WriteLine("BaseUri" + builder.Configuration["BaseUri"]);

        services.AddRabbitMQCoreClientConsumer(builder.Configuration.GetSection("RabbitMQ"))
            .AddHandler<OtelTestMessageHandler>(["aligin-test-otel"]);

        services.AddGrpcPreConfiguredConsoleClient<OtelTest.OtelTestClient>(builder.Configuration, opt =>
        {
            opt.ClientOptionsAction = o => o.Address = new Uri("http://localhost:5006");
        });
    })
    .Build();

Console.WriteLine("=== OTEL Test Console App ===");
Console.WriteLine("Press 1 - Call REST Minimal API");
Console.WriteLine("Press 2 - Call REST Controller");
Console.WriteLine("Press 3 - Call gRPC Service");
Console.WriteLine("Press Q - Quit");
Console.WriteLine();

var httpClientFactory = host.Services.GetRequiredService<IHttpClientFactory>();
var grpcClient = host.Services.GetRequiredService<OtelTest.OtelTestClient>();
var configuration = host.Services.GetRequiredService<IConfiguration>();
var baseUri = configuration.GetValue<string>(nameof(AppConfiguration.BaseUri)) ?? "http://localhost:5005";

var cts = new CancellationTokenSource();
_ = Task.Run(() =>
{
    while (!cts.IsCancellationRequested)
    {
        var key = Console.ReadKey(true);
        if (key.Key == ConsoleKey.Q)
        {
            cts.Cancel();
            break;
        }

        _ = key.Key switch
        {
            ConsoleKey.D1 => CallRestMinimalApi(baseUri),
            ConsoleKey.D2 => CallRestController(baseUri),
            ConsoleKey.D3 => CallGrpcService(grpcClient),
            _ => Task.CompletedTask
        };
    }
}, cts.Token);

await host.RunAsync();

async Task CallRestMinimalApi(string baseUri)
{
    try
    {
        using var activity = new ActivitySource("ConsoleApp").StartActivity("CallRestMinimalApi");
        activity?.SetTag("http.method", "GET");

        var client = new HttpClient { BaseAddress = new Uri(baseUri) };
        var response = await client.GetAsync("/api/otel-test-minimal?message=hello-from-console-minimal");
        var content = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"[Minimal API] Status: {response.StatusCode}");
        Console.WriteLine($"[Minimal API] Response: {content}");
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Minimal API] Error: {ex.Message}");
    }
}

async Task CallRestController(string baseUri)
{
    try
    {
        using var activity = new ActivitySource("ConsoleApp").StartActivity("CallRestController");
        activity?.SetTag("http.method", "GET");

        var client = new HttpClient { BaseAddress = new Uri(baseUri) };
        var response = await client.GetAsync("/api/otel-test-controller?message=hello-from-console-controller");
        var content = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"[Controller] Status: {response.StatusCode}");
        Console.WriteLine($"[Controller] Response: {content}");
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Controller] Error: {ex.Message}");
    }
}

async Task CallGrpcService(OtelTest.OtelTestClient client)
{
    try
    {
        using var activity = new ActivitySource("ConsoleApp").StartActivity("CallGrpcService");

        var response = await client.TestAsync(new TestRequest { Message = "hello-from-console-grpc" });

        Console.WriteLine($"[gRPC] Result: {response.Result}");
        Console.WriteLine($"[gRPC] TraceId: {response.TraceId}");
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[gRPC] Error: {ex.Message}");
    }
}
