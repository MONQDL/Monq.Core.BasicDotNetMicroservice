using Monq.Core.BasicDotNetMicroservice.GlobalExceptionFilters.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Класс, содержащий методы-расширения для создания интерфейса настройки глобального обработчика исключений <see cref="IGlobalExceptionFilterBuilder"/>.
    /// </summary>
    public static class GlobalExceptionFilterServiceCollectionExtensions
    {
        /// <summary>
        /// Добавить глобальный обработчик исключений в MVC pipeline.
        /// </summary>
        public static IGlobalExceptionFilterBuilder AddGlobalExceptionFilter(this IServiceCollection services)
        {
            var storage = new GlobalExceptionBuilderStorage();
            services.AddSingleton(storage);
            return new GlobalExceptionFilterBuilder(services, storage);
        }
    }
}
