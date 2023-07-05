using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Monq.Core.BasicDotNetMicroservice.GlobalExceptionFilters.DependencyInjection;
using Monq.Core.BasicDotNetMicroservice.GlobalExceptionFilters.Filters;
using Xunit;

namespace Monq.Core.BasicDotNetMicroservice.Tests;

public class GlobalExceptionFilterTests
{
    GlobalExceptionFilter CreateFilter(GlobalExceptionBuilderStorage storage) => new GlobalExceptionFilter(new LoggerFactory(), storage);

    [Fact(DisplayName = "Проверка правильности выполнения обработчиков исключений.")]
    public void ShouldProperlyExecuteExceptionHandlers()
    {
        var storage = new GlobalExceptionBuilderStorage();
        Func<ArgumentNullException, IActionResult> func = x => new ObjectResult(x.Message);
        storage.ExceptionHandlers.Add(typeof(ArgumentNullException), func);

        var filter = CreateFilter(storage);

        var actionContext = new ActionContext
        {
            HttpContext = new DefaultHttpContext(),
            RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
            ActionDescriptor = new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
        };
        var exceptionContext = new ExceptionContext(actionContext, new List<IFilterMetadata>() { filter })
        {
            Exception = new ArgumentNullException("val", "argument is null")
        };

        Assert.Null(exceptionContext.Result);

        filter.OnException(exceptionContext);

        var result = exceptionContext.Result as ObjectResult;
        Assert.NotNull(result);
        Assert.Equal("argument is null (Parameter 'val')", (string)result.Value);
    }
}
