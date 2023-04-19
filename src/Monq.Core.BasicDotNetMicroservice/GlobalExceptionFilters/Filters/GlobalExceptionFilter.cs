using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Monq.Core.BasicDotNetMicroservice.GlobalExceptionFilters.DependencyInjection;
using System;

namespace Monq.Core.BasicDotNetMicroservice.GlobalExceptionFilters.Filters
{
    public class GlobalExceptionFilter : IExceptionFilter
    {
        readonly ILogger _logger;
        readonly GlobalExceptionBuilderStorage _globalExceptionResult;

        public GlobalExceptionFilter(ILoggerFactory? logger, GlobalExceptionBuilderStorage globalExceptionResult)
        {
            _globalExceptionResult = globalExceptionResult;
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = logger.CreateLogger<GlobalExceptionFilter>();
        }

        /// <inheritdoc/>
        public void OnException(ExceptionContext context)
        {
            var result = _globalExceptionResult.Execute(context.Exception);

            context.Result = result;

            _logger.LogError(new EventId(), context.Exception, "GlobalExceptionFilter");
        }
    }
}
