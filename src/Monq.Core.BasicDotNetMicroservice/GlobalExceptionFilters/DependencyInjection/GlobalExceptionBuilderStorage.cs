using Microsoft.AspNetCore.Mvc;
using Monq.Core.BasicDotNetMicroservice.GlobalExceptionFilters.Models;
using System;
using System.Collections.Generic;
using System.Net;

namespace Monq.Core.BasicDotNetMicroservice.GlobalExceptionFilters.DependencyInjection
{
    public class GlobalExceptionBuilderStorage
    {
        readonly Dictionary<Type, Delegate> _delegateMap = new Dictionary<Type, Delegate>();

        /// <summary>
        /// Список обработчиков исключений.
        /// </summary>
        public IDictionary<Type, Delegate> ExceptionHandlers => _delegateMap;

        public IActionResult? Execute(Exception exception)
        {
            foreach (var (exceptionType, action) in _delegateMap)
            {
                if (action is not null && exception.GetType() == exceptionType)
                {
                    return (IActionResult?)action.DynamicInvoke(exception);
                }
            }

            return ReturnDefaultExceptionResponse(exception);
        }

        static IActionResult ReturnDefaultExceptionResponse(Exception exception)
        {
            var response = new ErrorResponse(exception);

            return new ObjectResult(response)
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                DeclaredType = typeof(ErrorResponse)
            };
        }

        internal void AddAction<T>(Func<T, IActionResult> action) where T : Exception
        {
            if (!_delegateMap.ContainsKey(typeof(T)))
                _delegateMap.Add(typeof(T), action);
        }
    }
}
