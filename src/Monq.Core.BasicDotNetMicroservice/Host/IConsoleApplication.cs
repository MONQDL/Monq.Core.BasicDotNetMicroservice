using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Monq.Core.BasicDotNetMicroservice.Host
{
    /// <summary>
    /// Интерфейс, который представляет собой консольное приложение.
    /// </summary>
    public interface IConsoleApplication : IHost
    {
        /// <summary>
        /// Конфигурация приложения.
        /// </summary>
        IConfiguration Configuration { get; }

        /// <summary>
        /// Информация об исполняемой среде приложения.
        /// </summary>
        IHostEnvironment HostEnvironment { get; }
    }
}
