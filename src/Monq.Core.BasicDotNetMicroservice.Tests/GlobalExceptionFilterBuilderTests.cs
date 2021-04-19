using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Monq.Core.BasicDotNetMicroservice.GlobalExceptionFilters.DependencyInjection;
using System;
using Xunit;

namespace Monq.Core.BasicDotNetMicroservice.Tests
{
    public class GlobalExceptionFilterBuilderTests
    {
        readonly IServiceCollection _services;

        public GlobalExceptionFilterBuilderTests()
        {
            _services = new ServiceCollection();
            _services.AddSingleton<GlobalExceptionBuilderStorage>();
        }

        GlobalExceptionFilterBuilder CreateBuilder(GlobalExceptionBuilderStorage builder) => new GlobalExceptionFilterBuilder(_services, builder);

        [Fact(DisplayName = "Проверить правильность добавления обработчиков по умолчанию.")]
        public void ShouldProperlyAddDefaultExceptionFilters()
        {
            var storage = new GlobalExceptionBuilderStorage();
            var builder = CreateBuilder(storage);

            Assert.Equal(0, storage.ExceptionHandlers.Count);

            builder.AddDefaultExceptionHandlers();

            Assert.Equal(3, storage.ExceptionHandlers.Count);
        }

        [Fact(DisplayName = "Проверить правильность добавления своего обработчика.")]
        public void ShouldProperlyAddCustomExceptionFilters()
        {
            var storage = new GlobalExceptionBuilderStorage();
            var builder = CreateBuilder(storage);

            Assert.Equal(0, storage.ExceptionHandlers.Count);

            builder.AddExceptionHandler<ArgumentNullException>(x => new ObjectResult(x.Message));

            Assert.Equal(1, storage.ExceptionHandlers.Count);
        }
    }
}
