using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;
using System.IO;
using static Monq.Core.BasicDotNetMicroservice.MicroserviceConstants.HostConfiguration;

namespace Monq.Core.BasicDotNetMicroservice.Host
{
    public class ConsoleBuilder : IHostBuilder
    {
        bool _webHostBuilt;
        readonly IConfiguration _config;
        readonly HostBuilderContext _context;
        readonly IHostEnvironment _hostEnvironment = new HostingEnvironment();
        readonly List<Action<HostBuilderContext, IServiceCollection>> _configureServicesDelegates =
            new List<Action<HostBuilderContext, IServiceCollection>>();
        readonly List<Action<HostBuilderContext, IConfigurationBuilder>> _configureAppConfigurationBuilderDelegates =
            new List<Action<HostBuilderContext, IConfigurationBuilder>>();

        public ConsoleBuilder()
        {
            _config = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .Build();

            var environmentName = _config.GetValue<string>("ENVIRONMENT");
            if (string.IsNullOrWhiteSpace(environmentName))
                throw new ArgumentNullException(ApplicationNameEnv, $"Environment not found in ASPNETCORE_ENVIRONMENT.");

            var applicationName = _config.GetValue<string>(ApplicationNameEnv);
            if (string.IsNullOrWhiteSpace(applicationName))
                throw new ArgumentNullException(ApplicationNameEnv, $"ApplicationName not found in {ApplicationNameEnv}.");

            _hostEnvironment.EnvironmentName = environmentName;
            _hostEnvironment.ContentRootPath = Directory.GetCurrentDirectory();
            _hostEnvironment.ApplicationName = applicationName;
            Console.WriteLine($"Environment: {environmentName}");

            _context = new HostBuilderContext(Properties) { Configuration = _config };
        }

        public IHost Build()
        {
            if (_webHostBuilt)
                throw new InvalidOperationException("Построение можно вызвать только 1 раз.");

            _webHostBuilt = true;

            var hostingServices = BuildCommonServices(out _);
            var hostingServiceProvider = hostingServices.BuildServiceProvider();

            var host = new ConsoleApplication(hostingServiceProvider, _context.Configuration, _context.HostingEnvironment);
            return host;
        }

        public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            if (configureDelegate == null)
                throw new ArgumentNullException(nameof(configureDelegate));

            _configureAppConfigurationBuilderDelegates.Add(configureDelegate);
            return this;
        }

        public IHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
        {
            if (configureServices == null)
                throw new ArgumentNullException(nameof(configureServices));

            return ConfigureServices((_, services) => configureServices(services));
        }

        /// <inheritdoc />
        public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            if (configureDelegate == null)
                throw new ArgumentNullException(nameof(configureDelegate));

            _configureServicesDelegates.Add(configureDelegate);
            return this;
        }

        /// <inheritdoc />
        public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate) =>
            throw new NotImplementedException();

        /// <inheritdoc />
        public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate) =>
            throw new NotImplementedException();

        /// <inheritdoc />
        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory) =>
            throw new NotImplementedException();

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory) =>
            throw new NotImplementedException();

        public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

        ServiceCollection BuildCommonServices(out AggregateException? hostingStartupErrors)
        {
            hostingStartupErrors = null;

            // Initialize the hosting environment
            _context.HostingEnvironment = _hostEnvironment;

            var services = new ServiceCollection();
            services.AddSingleton(_hostEnvironment);
            services.AddSingleton(_context);

            var builder = new ConfigurationBuilder()
                .SetBasePath(_hostEnvironment.ContentRootPath)
                .AddInMemoryCollection(_config.AsEnumerable());

            foreach (var configureAppConfiguration in _configureAppConfigurationBuilderDelegates)
            {
                configureAppConfiguration(_context, builder);
            }

            var configuration = builder.Build();
            services.AddSingleton<IConfiguration>(configuration);
            _context.Configuration = configuration;
            services.AddHttpContextAccessor();
            services.AddOptions();

            // Conjure up a RequestServices
            services.AddTransient<IServiceProviderFactory<IServiceCollection>, DefaultServiceProviderFactory>();

            // Ensure object pooling is available everywhere.
            services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();

            foreach (var configureServices in _configureServicesDelegates)
            {
                configureServices(_context, services);
            }

            return services;
        }
    }
}
