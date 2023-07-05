using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System;
using System.Threading.Tasks;

namespace Monq.Core.BasicDotNetMicroservice.Middleware
{
    /// <summary>
    /// Middleware предоставляет функционал встраивания заголовков X-Trace-Event-Id в запрос.
    /// </summary>
    public class TraceEventIdMiddleware
    {
        const sbyte DefaultUserspaceId = 0;

        readonly RequestDelegate _next;

        /// <summary>
        /// Инициализирует новый объект класса <see cref="TraceEventIdMiddleware"/>.
        /// </summary>
        public TraceEventIdMiddleware(RequestDelegate next) => _next = next;

        /// <summary>
        /// Вызов Middleware.
        /// </summary>
        public async Task Invoke(HttpContext context)
        {
            // Adding to logger TrackerId.
            var contentHasEventId = ContextHasHeader(context, MicroserviceConstants.EventIdHeader, out var traceEventId);

            if (!contentHasEventId || string.IsNullOrEmpty(traceEventId))
                traceEventId = Guid.NewGuid().ToString();

            context.Response.OnStarting(() =>
            {
                if (context.Response.Headers.ContainsKey(MicroserviceConstants.EventIdHeader))
                {
                    return Task.CompletedTask;
                }

                context.Response.Headers.Add(MicroserviceConstants.EventIdHeader, traceEventId);

                return Task.CompletedTask;
            });

            context.TraceIdentifier = traceEventId;

            // Adding to logger UserspaceId.
            var contentHasUserspaceId = ContextHasHeader(context, MicroserviceConstants.UserspaceIdHeader, out var userspaceIdStr);

            int userspaceId = DefaultUserspaceId;
            if (contentHasUserspaceId && !string.IsNullOrEmpty(userspaceIdStr))
                int.TryParse(userspaceIdStr, out userspaceId);

            using (LogContext.PushProperty(MicroserviceConstants.EventIdPropertyName, traceEventId))
            using (LogContext.PushProperty(MicroserviceConstants.UserspaceIdPropertyName, userspaceId))
            {
                await _next(context);
            }
        }

        bool ContextHasHeader(HttpContext context, string header, out string? value)
        {
            var result = context.Request.Headers.TryGetValue(header, out var val);
            if (result)
            {
                value = val;
                return true;
            }
            value = null;
            return false;
        }
    }
}
