namespace Monq.Core.BasicDotNetMicroservice
{
    /// <summary>
    /// Набор константных значений для обслуживания Http запросов.
    /// </summary>
    public class MicroserviceConstants
    {
        /// <summary>
        /// Название заголовка, в котором хранится Id запроса системы логирования.
        /// </summary>
        public const string EventIdHeader = "X-Trace-Event-Id";

        /// <summary>
        /// Название Http заголовка, в котором хранится Id пространства пользователя.
        /// </summary>
        public const string UserspaceIdHeader = "X-Smon-Userspace-Id";

        /// <summary>
        /// Наименование Http заголовок с наименованием локализации (культуры).
        /// </summary>
        public const string CultureHeader = "Accept-Language";

        /// <summary>
        /// Certificates directory environment variable name.
        /// </summary>
        public const string CertsDirEnv = "CERTS_DIR";

        /// <summary>
        /// Default certificates directory that will be used if ENV CERTS_DIR is not set.
        /// </summary>
        public const string CertsDirDefault = "/certs";


        internal static class HostConfiguration
        {
            public const string ConsulConfigFileEnv = "CONSUL_CONFIG_FILE";
            public const string ConsulConfigFileDefault = "aspnet_consul_config.json";
            public const string ConsulConfigFileSectionName = "Consul";
            public const string ApplicationNameEnv = "APPLICATION_NAME";
            public const string AppsettingsFile = "appsettings.json";
            public const string CommonAppsettingsFile = "common-appsettings.json";
        }

        internal static class MetricsConfiguration
        {
            public const string ConfigSection = "Metrics:ReportingInfluxDb";
        }
    }
}
