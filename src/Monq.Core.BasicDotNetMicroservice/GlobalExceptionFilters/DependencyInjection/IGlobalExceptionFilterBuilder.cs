using Microsoft.AspNetCore.Mvc;
using System;

namespace Monq.Core.BasicDotNetMicroservice.GlobalExceptionFilters.DependencyInjection;

/// <summary>
/// The Global exception result builder.
/// </summary>
public interface IGlobalExceptionFilterBuilder
{
    /// <summary>
    /// Добавить обработчик конкретного исключения.
    /// </summary>
    /// <typeparam name="T">Тип исключения.</typeparam>
    /// <param name="action">Обработчик исключения.</param>
    IGlobalExceptionFilterBuilder AddExceptionHandler<T>(Func<T, IActionResult> action)
        where T : Exception;

    /// <summary>
    /// Добавить обработчики исключений по умолчанию.
    /// </summary>
    IGlobalExceptionFilterBuilder AddDefaultExceptionHandlers();
}
