using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Monq.Core.BasicDotNetMicroservice.GrpcInterceptors;

/// <summary>
/// Log and rethrow RpcException to next level call.
/// </summary>
public sealed class RpcDownstreamExceptionInterceptor : Interceptor
{
    readonly ILogger<RpcDownstreamExceptionInterceptor> _logger;
    readonly IHttpContextAccessor? _httpContextAccessor;

    /// <summary>
    /// Create new object of <see cref="RpcDownstreamExceptionInterceptor"/>.
    /// </summary>
    /// <param name="logger">The logger object.</param>
    /// <param name="httpContextAccessor">The HttpContextAccessor object.</param>
    public RpcDownstreamExceptionInterceptor(
        ILogger<RpcDownstreamExceptionInterceptor> logger,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var call = continuation(request, context);

        return new AsyncUnaryCall<TResponse>(
            HandleResponseAsync(context.Method.FullName, call.ResponseAsync),
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            call.Dispose);
    }

    /// <inheritdoc />
    public override TResponse BlockingUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        try
        {
            return continuation(request, context);
        }
        catch (RpcException ex)
        {
            LogAndEnrichException(context.Method.FullName, ex);
            throw;
        }
    }

    private async Task<TResponse> HandleResponseAsync<TResponse>(
        string methodName,
        Task<TResponse> responseTask)
    {
        try
        {
            return await responseTask.ConfigureAwait(false);
        }
        catch (RpcException ex)
        {
            LogAndEnrichException(methodName, ex);
            throw;
        }
    }

    private void LogAndEnrichException(string methodName, RpcException exception)
    {
        // Логируем с дополнительным контекстом
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["GrpcMethod"] = methodName,
            ["GrpcStatusCode"] = exception.StatusCode.ToString(),
            ["GrpcStatusDetail"] = exception.Status.Detail,
            ["TraceId"] = _httpContextAccessor?.HttpContext?.TraceIdentifier
        }))
        {
            _logger.LogError(exception,
                "Downstream gRPC call failed. Method: {Method}, Status: {StatusCode}, Detail: {Detail}",
                methodName, exception.StatusCode, exception.Status.Detail);
        }
    }
}
