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

        readonly Dictionary<Type, Action<Exception>> _actionMap = new();

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
        public void Execute(Exception exception)
        {
            foreach (KeyValuePair<Type, Action<Exception>> item in _actionMap)
            {
                item.Deconstruct(out var type, out var action);
                if (type == exception.GetType() && action is not null)
                {
                    action.Invoke(exception);
                }
            }
        }

        internal void AddAction<T>(Action<Exception> action) where T : Exception
        {
            if (!_actionMap.ContainsKey(typeof(T)))
                _actionMap.Add(typeof(T), action);
        }
    }
}
