using Microsoft.AspNetCore.Builder;
using Monq.Core.BasicDotNetMicroservice.Middleware;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension method used to add the middleware to the HTTP request pipeline.
    /// </summary>
    public static class TraceEventIdExtensions
    {
        /// <summary>
        /// Добавляет обработку поля X-Trace-Event-Id в IApplicationBuilder request pipeline.
        /// </summary>
        public static IApplicationBuilder UseTraceEventId(this IApplicationBuilder builder) =>
            builder.UseMiddleware<TraceEventIdMiddleware>();
    }
}
