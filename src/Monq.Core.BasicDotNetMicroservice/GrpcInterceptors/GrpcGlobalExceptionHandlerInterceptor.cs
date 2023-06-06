using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using Monq.Core.BasicDotNetMicroservice.GlobalExceptionFilters.DependencyInjection;
using Monq.Core.BasicDotNetMicroservice.GlobalExceptionFilters.Models;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Monq.Core.BasicDotNetMicroservice.GrpcInterceptors
{
    /// <summary>
    /// Global gRPC exception handler interceptor.
    /// </summary>
    public class GrpcGlobalExceptionHandlerInterceptor : Interceptor
    {
        readonly ILogger<GrpcGlobalExceptionHandlerInterceptor> _logger;
        readonly GlobalGrpcExceptionBuilderStorage _storage;

        /// <summary>
        /// Constructor global gRPC exception handler interceptor.
        /// Creates new instance of class <see cref="GrpcGlobalExceptionHandlerInterceptor"/>.
        /// </summary>
        public GrpcGlobalExceptionHandlerInterceptor(ILogger<GrpcGlobalExceptionHandlerInterceptor> logger)
        {
            _logger = logger;
            _storage = GlobalGrpcExceptionBuilderStorage.GetInstance();
        }

        /// <inheritdoc/>
        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                return await base.UnaryServerHandler(request, context, continuation);
            }
            catch (RpcException re)
            {
                _logger.LogError(re, re.Message);
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);

                _storage.Execute(e);

                var message = new ErrorResponse
                {
                    Message = e.Message,
                    StackTrace = e.StackTrace
                };
                throw new RpcException(new Status(StatusCode.Unknown, JsonSerializer.Serialize(message)));
            }
        }
    }
}
