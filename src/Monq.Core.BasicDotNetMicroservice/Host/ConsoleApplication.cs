using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Monq.Core.BasicDotNetMicroservice.Host
{
    /// <summary>
    /// Реализация <see cref="IConsoleApplication"/>.
    /// </summary>
    public class ConsoleApplication : IConsoleApplication
    {
        public ConsoleApplication(
            ServiceProvider hostingServiceProvider,
            IConfiguration configuration,
            IHostEnvironment hostingEnvironment)
        {
            Services = hostingServiceProvider;
            Configuration = configuration;
            HostEnvironment = hostingEnvironment;
        }

        /// <inheritdoc />
        public IServiceProvider Services { get; }

        /// <inheritdoc />
        public IConfiguration Configuration { get; }

        /// <inheritdoc />
        public IHostEnvironment HostEnvironment { get; }

        /// <inheritdoc />
        public void Dispose() => throw new NotImplementedException();

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken = new CancellationToken()) =>
            throw new NotImplementedException();

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken = new CancellationToken()) =>
            throw new NotImplementedException();
    }
}
