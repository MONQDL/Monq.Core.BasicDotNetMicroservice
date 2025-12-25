using System;
using System.Linq;
using System.Reflection;

namespace Monq.Core.BasicDotNetMicroservice.Helpers;

/// <summary>
/// Microservice version information.
/// </summary>
public static class MicroserviceVersionInfo
{
    static string? _version;
    static readonly Version _defaultVersion = new Version();

    /// <summary>
    /// Получить версию сборки, которая содержит в себе тип <paramref name="assemblyType"/>.
    /// </summary>
    /// <param name="assemblyType">Любой тип, который содержится в сборке, для которой требуется определить версию.</param>
    public static string GetVersion(Type assemblyType)
    {
        if (_version != null)
            return _version;

        ArgumentNullException.ThrowIfNull(assemblyType);

        var asmName = assemblyType.Assembly.GetName();
        // InformationalVersion хранится в метаданных и доступен без атрибутов
        var version = asmName.Version?.ToString() ?? "<unknown>";
        _version = version;
        return _version;
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
            return _defaultVersion.ToString();

        return GetVersion(programAssembly);
    }
}
