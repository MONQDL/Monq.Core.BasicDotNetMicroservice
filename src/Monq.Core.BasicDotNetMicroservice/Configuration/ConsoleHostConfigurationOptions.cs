using Monq.Core.BasicDotNetMicroservice.Models;

namespace Monq.Core.BasicDotNetMicroservice.Host
{
    /// <summary>
    /// Конфигурация хоста консольного приложения.
    /// </summary>
    public class ConsoleHostConfigurationOptions
    {
        /// <summary>
        /// Конфигурация для работы с Consul.
        /// </summary>
        public ConsulConfigurationOptions? ConsulConfigurationOptions { get; set; } = null;
    }
}
