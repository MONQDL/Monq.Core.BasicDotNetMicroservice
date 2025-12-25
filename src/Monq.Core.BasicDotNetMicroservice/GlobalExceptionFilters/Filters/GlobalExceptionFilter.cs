using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Monq.Core.BasicDotNetMicroservice.GlobalExceptionFilters.DependencyInjection;

namespace Monq.Core.BasicDotNetMicroservice.GlobalExceptionFilters.Filters;

/// <summary>
/// The implementation of <see cref="IExceptionFilter"/> that handles downstream request exceptions.
/// </summary>
public class GlobalExceptionFilter : IExceptionFilter
{
    readonly ILogger _logger;
    readonly GlobalExceptionBuilderStorage _globalExceptionResult;

    /// <summary>
    /// Create new object of <see cref="GlobalExceptionFilter"/>.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="globalExceptionResult">Global exceptions cache object.</param>
    public GlobalExceptionFilter(ILoggerFactory logger,
        GlobalExceptionBuilderStorage globalExceptionResult)
    {
        _globalExceptionResult = globalExceptionResult;
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
