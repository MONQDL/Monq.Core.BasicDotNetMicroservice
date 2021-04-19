using System;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace Monq.Core.BasicDotNetMicroservice.Helpers
{
    /// <summary>
    /// Хелпер для настройки логгирования в Serilog.
    /// </summary>
    public static class LoggerEnvironment
    {
        static string? MicroserviceName { get; set; }

        /// <summary>
        /// Выполнить конфигурацию системы логирования для микросервиса.
        /// </summary>
        /// <param name="env">Конфигурация окружения, в котором выполняется сборка.</param>
        /// <param name="configuration">Коллекция ключ-значение типа <see cref="IConfiguration" />.</param>
        public static void Configure(IHostEnvironment env, IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration), $"{nameof(configuration)} is null.");

            if (env == null)
                throw new ArgumentNullException(nameof(env), $"{nameof(env)} is null.");

            ReadVariables();

            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .ReadFrom.Configuration(configuration)
                .Enrich.WithProperty(LoggerFieldNames.Application, GetAssemblyName())
                .Enrich.WithProperty(LoggerFieldNames.Microservice, MicroserviceName)
                .Enrich.WithProperty(LoggerFieldNames.AppVersion, MicroserviceInfo.GetEntryPointAssembleVersion())
                .Enrich.WithProperty(LoggerFieldNames.AppEnvironment, env.EnvironmentName)
                .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss zzz} {Level:u3}] {Scope} {Message:lj}{NewLine}{Exception}");
            Log.Logger = loggerConfig.CreateLogger();
        }

        static string? GetAssemblyName() => Assembly.GetEntryAssembly()?.GetName().Name;

        /// <summary>
        /// Прочитать значения переменных окружения.
        /// </summary>
        static void ReadVariables()
        {
            MicroserviceName = Environment.GetEnvironmentVariable("ASPNETCORE_" + MicroserviceConstants.HostConfiguration.ApplicationNameEnv);
        }

        /// <summary>
        /// Названия полей в системе логирования.
        /// </summary>
        public static class LoggerFieldNames
        {
            /// <summary>
            /// Среда исполнения микросервиса.
            /// </summary>
            public const string AppEnvironment = "AppEnvironment";

            /// <summary>
            /// Название программы в составе микросервиса.
            /// </summary>
            public const string Application = "Application";

            /// <summary>
            /// Версия микросервиса.
            /// </summary>
            public const string AppVersion = "AppVersion";

            /// <summary>
            /// Название микросервиса из переменной окружения APPLICATION_NAME.
            /// </summary>
            public const string Microservice = "Microservice";

            /// <summary>
            /// Id пользователя в системе, который отправил запрос.
            /// </summary>
            public const string UserId = "UserId";

            /// <summary>
            /// Имя пользователя в системе, который отправил запрос.
            /// </summary>
            public const string UserName = "UserName";
        }
    }
}
