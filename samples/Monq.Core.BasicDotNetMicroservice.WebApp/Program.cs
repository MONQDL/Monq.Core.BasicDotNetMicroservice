using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Monq.Core.BasicDotNetMicroservice.Extensions;
using Monq.Core.BasicDotNetMicroservice.GlobalExceptionFilters.Filters;
using Monq.Core.HttpClientExtensions.Exceptions;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
Console.OutputEncoding = Encoding.UTF8;

builder.Host.ConfigureBasicMicroservice();
builder.Host.ConfigureStaticAuthentication();
builder.WebHost.ConfigureMetricsAndHealth();

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Listen(IPAddress.Any, 5005, listenOptions => listenOptions.Protocols = HttpProtocols.Http1);
    serverOptions.Listen(IPAddress.Any, 5006, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
});

builder.Services.ConfigureSMAuthentication(builder.Configuration);

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
    })
    .AddMetrics();

Serilog.Debugging.SelfLog.Enable(Console.Error);

var app = builder.Build();

app.MapApiVersion(typeof(Program));
app.UseTraceEventId();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseLogUser();
app.UseRequestLocalization();
app.MapControllers();

app.Run();
