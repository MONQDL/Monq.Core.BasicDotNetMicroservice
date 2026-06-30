using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RabbitMQCoreClient;

namespace Monq.Core.BasicDotNetMicroservice.WebApp.Controllers;

[Route("api")]
[AllowAnonymous]
public class OtelTestController : Controller
{
    readonly ILogger<OtelTestController> _log;
    readonly IQueueService _queueService;

    public OtelTestController(ILogger<OtelTestController> log, IQueueService queueService)
    {
        _log = log;
        _queueService = queueService;
    }

    [HttpGet("otel-test-controller")]
    public async Task<IActionResult> OtelTestControllerAction([FromQuery] string? message = null)
    {
        var msg = message ?? "controller-default";
        _log.LogInformation("REST Controller /api/otel-test-controller called with message: {Message}", msg);

        await _queueService.SendAsync($"Controller processed: {msg}", "aligin-test-otel");

        return Ok(new
        {
            result = $"Controller processed: {msg}",
            traceId = Activity.Current?.TraceId.ToString() ?? string.Empty
        });
    }
}
