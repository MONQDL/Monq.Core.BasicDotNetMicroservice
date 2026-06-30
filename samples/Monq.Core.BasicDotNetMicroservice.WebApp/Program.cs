using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Monq.Core.BasicDotNetMicroservice.Extensions;
using Monq.Core.BasicDotNetMicroservice.GlobalExceptionFilters.Filters;
using Monq.Core.BasicDotNetMicroservice.WebApp.Grpc;
using Monq.Core.BasicDotNetMicroservice.WebApp.Services;
using Monq.Core.HttpClientExtensions.Exceptions;
using RabbitMQCoreClient;
using RabbitMQCoreClient.DependencyInjection;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
Console.OutputEncoding = Encoding.UTF8;

builder.Host.ConfigureBasicMicroservice();
builder.Host.ConfigureStaticAuthentication();

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Listen(System.Net.IPAddress.Any, 5005, listenOptions => listenOptions.Protocols = HttpProtocols.Http1);
    serverOptions.Listen(System.Net.IPAddress.Any, 5006, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
});

builder.Services.ConfigureMonqAuthentication(builder.Configuration);

builder.Services
        .AddGlobalExceptionFilter()
        .AddExceptionHandler<ResponseException>(ex =>
            new ObjectResult(JsonSerializer.Deserialize<object>(ex.ResponseData, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }))
            {
                StatusCode = (int)ex.StatusCode
            })
        .AddDefaultExceptionHandlers();

builder.Services
    .AddControllers(opt => { opt.Filters.Add(typeof(GlobalExceptionFilter)); })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddGrpc();

builder.Services.AddRabbitMQCoreClient(builder.Configuration.GetSection("RabbitMQ"));

Serilog.Debugging.SelfLog.Enable(Console.Error);

var app = builder.Build();

app.MapApiVersion(typeof(Program));
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseRequestLocalization();
app.MapControllers();

app.MapGet("/api/otel-test-minimal", async (ILoggerFactory loggerFactory, IQueueService queueService, string? message = null) =>
{
    var log = loggerFactory.CreateLogger("OtelTestMinimal");
    var msg = message ?? "minimal-default";
    log.LogInformation("REST Minimal API /api/otel-test-minimal called with message: {Message}", msg);

    await queueService.SendAsync($"Minimal API processed: {msg}", "aligin-test-otel");

    return Results.Ok(new
    {
        result = $"Minimal API processed: {msg}",
        traceId = Activity.Current?.TraceId.ToString() ?? string.Empty
    });
});

app.MapGrpcService<OtelTestService>();

app.Run();
