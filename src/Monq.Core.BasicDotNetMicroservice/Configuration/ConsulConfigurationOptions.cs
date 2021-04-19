using System.Diagnostics.CodeAnalysis;
using static Monq.Core.BasicDotNetMicroservice.MicroserviceConstants.HostConfiguration;

namespace Monq.Core.BasicDotNetMicroservice.Models
{
    /// <summary>
    /// Конфигурация для работы с Consul.
    /// </summary>
    public class ConsulConfigurationOptions
    {
        /// <summary>
        /// Название общего файла конфигурации, который будет прочитан после <see cref="CommonAppsettingsFileName"/>.
        /// </summary>
        public string AppsettingsFileName { get; set; } = AppsettingsFile;

        /// <summary>
        /// Использовать общий файл конфигурации в микросервисе. По умолчанию - true.
        /// </summary>
        public bool UseCommonAppsettings { get; set; } = true;

        /// <summary>
        /// Название общего файла конфигурации, который будет прочитан первым в очереди.
        /// </summary>
        public string CommonAppsettingsFileName { get; set; } = CommonAppsettingsFile;
    }
}
