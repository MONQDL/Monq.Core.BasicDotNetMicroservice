using Monq.Core.BasicDotNetMicroservice.Middleware;
using System;

namespace Microsoft.AspNetCore.Builder
{
    public static class ApiVersioningExtensions
    {
        /// <summary>
        /// Uses the API versioning.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="modelsLibAssemblyType">Тип, содержащийся в сборке с моделями.</param>
        public static IApplicationBuilder UseApiVersioning(this IApplicationBuilder builder, Type modelsLibAssemblyType) =>
            builder.UseMiddleware<ApiVersioningMiddleware>(modelsLibAssemblyType);
    }
}
