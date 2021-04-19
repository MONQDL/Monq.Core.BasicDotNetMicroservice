using System;
using System.Linq;
using System.Reflection;

namespace Monq.Core.BasicDotNetMicroservice.Helpers
{
    public static class MicroserviceInfo
    {
        static readonly Version DefaultVersion = new Version();

        /// <summary>
        /// Получить версию сборки, которая содержит в себе тип <paramref name="assemblyType"/>.
        /// </summary>
        /// <param name="assemblyType">Любой тип, который содержится в сборке, для которой требуется определить версию.</param>
        public static string GetVersion(Type assemblyType)
        {
            if (assemblyType == null)
                throw new ArgumentNullException(nameof(assemblyType), $"{nameof(assemblyType)} is null.");

            var version = assemblyType
                .GetTypeInfo()
                .Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            return version ?? DefaultVersion.ToString();
        }

        /// <summary>
        /// Получить версию сборки, которая содержит в себе тип <see name="Program" />.
        /// </summary>
        public static string GetEntryPointAssembleVersion()
        {
            var programAssembly = (from t in Assembly.GetEntryAssembly()!.GetTypes() // Can't be called from unmanaged code.
                where t.IsClass && t.Name == "Program"
                select t).FirstOrDefault();

            if (programAssembly is null)
                return DefaultVersion.ToString();

            return GetVersion(programAssembly);
        }
    }
}
