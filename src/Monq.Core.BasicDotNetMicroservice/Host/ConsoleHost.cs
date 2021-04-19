using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monq.Core.BasicDotNetMicroservice.Extensions;
using Monq.Core.HttpClientExtensions;
using System.Diagnostics.CodeAnalysis;

namespace Monq.Core.BasicDotNetMicroservice.Host
{
    public class ConsoleHost
    {
        [return: NotNull]
        public static IHostBuilder CreateDefaultBuilder(string[] args, ConsoleHostConfigurationOptions? options = null)
        {
            var consoleBuilder = new ConsoleBuilder();

            consoleBuilder.ConfigureAppConfiguration((builderContext, config) =>
            {
                var env = builderContext.HostingEnvironment;
                config.AddJsonFile("appsettings.json", optional: true)
                      .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);
            });

            consoleBuilder.ConfigureConsul(options?.ConsulConfigurationOptions);
            consoleBuilder.ConfigureSerilogLogging();
            consoleBuilder.ConfigBasicHttpService(opts =>
            {
                var headerOptions = new BasicHttpServiceHeaderOptions();
                headerOptions.AddForwardedHeader(MicroserviceConstants.EventIdHeader);
                headerOptions.AddForwardedHeader(MicroserviceConstants.UserspaceIdHeader);
                headerOptions.AddForwardedHeader(MicroserviceConstants.CultureHeader);

                opts.ConfigHeaders(headerOptions);
            });

            consoleBuilder.ConfigureServices((context, services) =>
            {
                services.AddOptions();
                services.Configure<AppConfiguration>(context.Configuration);
                services.AddLogging();
            });

            return consoleBuilder;
        }
    }
}
