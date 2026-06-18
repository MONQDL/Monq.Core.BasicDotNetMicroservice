using System.Diagnostics;
using Grpc.Core;
using Monq.Core.BasicDotNetMicroservice.WebApp.Grpc;
using RabbitMQCoreClient;

namespace Monq.Core.BasicDotNetMicroservice.WebApp.Services;

public class OtelTestService : OtelTest.OtelTestBase
{
    readonly ILogger<OtelTestService> _log;
    readonly IQueueService _queueService;

    public OtelTestService(ILogger<OtelTestService> log, IQueueService queueService)
    {
        _log = log;
        _queueService = queueService;
    }

    public override async Task<TestResponse> Test(TestRequest request, ServerCallContext context)
    {
        _log.LogInformation("gRPC OtelTest.Test called with message: {Message}", request.Message);

        await _queueService.SendAsync($"gRPC processed: {request.Message}", "aligin-test-otel");

        return new TestResponse
        {
            Result = $"gRPC processed: {request.Message}",
            TraceId = Activity.Current?.TraceId.ToString() ?? string.Empty
        };
    }
}
