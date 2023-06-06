using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Monq.Core.BasicDotNetMicroservice.GlobalExceptionFilters.Models;
using System;
using System.Net;

namespace Monq.Core.BasicDotNetMicroservice.GlobalExceptionFilters.DependencyInjection
{
    public class GlobalExceptionFilterBuilder : IGlobalExceptionFilterBuilder
    {
        public GlobalExceptionBuilderStorage GlobalExceptionResult { get; }

        public IServiceCollection Services { get; }

        /// <summary>
        /// Инициализировать новый экземпляр класса <see cref="GlobalExceptionFilterBuilder"/>.
        /// </summary>
        public GlobalExceptionFilterBuilder(IServiceCollection? services, GlobalExceptionBuilderStorage storage)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));

            GlobalExceptionResult = storage;
        }

        /// <summary>
        /// Добавить обработчики исключений по умолчанию.
        /// </summary>
        /// <returns></returns>
        public IGlobalExceptionFilterBuilder AddDefaultExceptionHandlers()
        {
            AddExceptionHandler<UnauthorizedAccessException>(ex => CreateHttpResponse("Unauthorized Access.", ex.StackTrace, HttpStatusCode.Unauthorized));

            AddExceptionHandler<NotImplementedException>(ex => CreateHttpResponse("A server error occurred.", ex.StackTrace, HttpStatusCode.NotImplemented));

            AddExceptionHandler<Exception>(ex => CreateHttpResponse("Internal server error.", ex.StackTrace, HttpStatusCode.InternalServerError));

            return this;
        }

        /// <summary>
        /// Добавить обработчик конкретного исключения.
        /// </summary>
        /// <typeparam name="T">Тип исключения.</typeparam>
        /// <param name="action">Обработчик исключения.</param>
        public IGlobalExceptionFilterBuilder AddExceptionHandler<T>(Func<T, IActionResult> action) where T : Exception
        {
            GlobalExceptionResult.AddAction(action);

            return this;
        }

        static ObjectResult CreateHttpResponse(string message, string? stackTrace, HttpStatusCode statusCode)
        {
            var response = new ErrorResponse(message, stackTrace);

            return new ObjectResult(response)
            {
                StatusCode = (int)statusCode,
                DeclaredType = typeof(ErrorResponse)
            };
        }
    }
}
