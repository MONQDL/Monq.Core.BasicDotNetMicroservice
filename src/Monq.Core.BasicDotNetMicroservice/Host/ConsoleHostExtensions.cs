using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Monq.Core.BasicDotNetMicroservice.Host
{
    public static class ConsoleHostExtensions
    {
        /// <summary>
        /// Adds a delegate for configuring the provided <see cref="ILoggingBuilder"/>. This may be called multiple times.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IWebHostBuilder" /> to configure.</param>
        /// <param name="configureLogging">The delegate that configures the <see cref="ILoggingBuilder"/>.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public static IHostBuilder ConfigureLogging(this IHostBuilder hostBuilder, Action<ILoggingBuilder> configureLogging) => 
            hostBuilder.ConfigureServices(collection => collection.AddLogging(configureLogging));

        /// <summary>
        /// Adds a delegate for configuring the provided <see cref="LoggerFactory"/>. This may be called multiple times.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IWebHostBuilder" /> to configure.</param>
        /// <param name="configureLogging">The delegate that configures the <see cref="LoggerFactory"/>.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public static IHostBuilder ConfigureLogging(this IHostBuilder hostBuilder, Action<HostBuilderContext, ILoggingBuilder> configureLogging) => 
            hostBuilder.ConfigureServices((context, collection) => collection.AddLogging(builder => configureLogging(context, builder)));
    }
}
