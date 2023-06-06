using Grpc.Core;
using Monq.Core.BasicDotNetMicroservice.GlobalExceptionFilters.Models;
using System;
using System.Collections.Generic;

namespace Monq.Core.BasicDotNetMicroservice.GlobalExceptionFilters.DependencyInjection
{
    /// <summary>
    /// Grpc exception builder storage.
    /// </summary>
    public class GlobalGrpcExceptionBuilderStorage
    {
        static GlobalGrpcExceptionBuilderStorage? _instance;

        static readonly object _syncRoot = new();

        readonly Dictionary<Type, Delegate> _delegateMap = new();

        /// <summary>
        /// Get builder instance.
        /// </summary>
        /// <returns></returns>
        public static GlobalGrpcExceptionBuilderStorage GetInstance()
        {
            if (_instance is null)
            {
                lock (_syncRoot)
                {
                    _instance ??= new GlobalGrpcExceptionBuilderStorage();
                }
            }
            return _instance;
        }

        /// <summary>
        /// Execute.
        /// </summary>
        /// <param name="exception"></param>
        public RpcException Execute(Exception exception)
        {
            foreach (var (exceptionType, action) in _delegateMap)
                if (action is not null && exceptionType == exception.GetType())
                    return (RpcException)action.DynamicInvoke(exception)!;

            var message = new ErrorResponse(exception);
            return new RpcException(new(StatusCode.Unknown, message.ToString()));
        }

        internal void AddAction<T>(Func<T, RpcException> action) where T : Exception
        {
            if (!_delegateMap.ContainsKey(typeof(T)))
                _delegateMap.Add(typeof(T), action);
        }
    }
}
