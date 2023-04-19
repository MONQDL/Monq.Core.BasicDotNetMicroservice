using Grpc.AspNetCore.Server;
using Monq.Core.BasicDotNetMicroservice.GlobalExceptionFilters.DependencyInjection;
using Monq.Core.BasicDotNetMicroservice.GrpcInterceptors;
using System;

namespace Monq.Core.BasicDotNetMicroservice.Helpers
{
    /// <summary>
    /// Helper class to work with GrpcServiceOptions.
    /// </summary>
    public static class GrpcServiceOptionsHelper
    {
        /// <summary>
        /// Add global gRPC exception handling.
        /// </summary>
        public static GrpcServiceOptions EnableGrpcGlobalExceptionHandling(this GrpcServiceOptions options)
        {
            options.Interceptors.Add<GrpcGlobalExceptionHandlerInterceptor>(Array.Empty<object>());
            return options;
        }

        /// <summary>
        /// Add gRPC exception handler for the custom exception.
        /// </summary>
        public static GrpcServiceOptions AddGrpcExceptionHandler<T>(this GrpcServiceOptions options, Action<Exception> action) where T : Exception
        {
            var storage = GlobalGrpcExceptionBuilderStorage.GetInstance();
            storage.AddAction<T>(action);

            return options;
        }
    }
}
