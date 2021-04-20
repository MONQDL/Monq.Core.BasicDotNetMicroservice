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
        const sbyte _defaultUserspaceId = 0;

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
            string traceEventId;
            if (ContextHasHeader(context, MicroserviceConstants.EventIdHeader))
                traceEventId = context.Request.Headers[MicroserviceConstants.EventIdHeader];
            else
                traceEventId = Guid.NewGuid().ToString();

            context.Response.Headers[MicroserviceConstants.EventIdHeader] = traceEventId;

            if (!context.Request.Headers.ContainsKey(MicroserviceConstants.EventIdHeader))
                context.Request.Headers[MicroserviceConstants.EventIdHeader] = traceEventId;

            context.TraceIdentifier = traceEventId;

            // Если заголовок есть, но невозможно его спарсить, то принимаем значение по умолчанию.
            int userspaceId = _defaultUserspaceId;
            if (ContextHasHeader(context, MicroserviceConstants.UserspaceIdHeader))
                int.TryParse(context.Request.Headers[MicroserviceConstants.UserspaceIdHeader], out userspaceId);

            using (LogContext.PushProperty(MicroserviceConstants.EventIdHeader, traceEventId))
            using (LogContext.PushProperty(MicroserviceConstants.UserspaceIdHeader, userspaceId))
            {
                await _next(context);
            }
        }

        bool ContextHasHeader(HttpContext context, string header) => context.Request.Headers.ContainsKey(header)
                && !string.IsNullOrWhiteSpace(context.Request.Headers[header]);
    }
}
