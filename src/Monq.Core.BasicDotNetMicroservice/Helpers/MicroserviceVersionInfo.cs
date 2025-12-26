using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Monq.Core.BasicDotNetMicroservice.Helpers;

/// <summary>
/// Microservice version information.
/// </summary>
public static class MicroserviceVersionInfo
{
    /// <summary>
    /// Кэш версий сборок. Ключ – сама сборка, значение – строка версии.
    /// </summary>
    static readonly ConcurrentDictionary<Assembly, string> _assemblyVersionCache
        = new ConcurrentDictionary<Assembly, string>();

    /// <summary>
    /// Получить версию сборки, которая содержит в себе тип <paramref name="assemblyType"/>.
    /// </summary>
    /// <param name="assemblyType">Любой тип, который содержится в сборке, для которой требуется определить версию.</param>
    public static string GetVersion(Type assemblyType)
    {
        ArgumentNullException.ThrowIfNull(assemblyType);

        var asm = assemblyType.Assembly;
        // InformationalVersion хранится в метаданных и доступен без атрибутов
        return _assemblyVersionCache.GetOrAdd(asm, a =>
        {
            var name = a.GetName();
            return name.Version?.ToString() ?? "<unknown>";
        });
    }

    /// <summary>
    /// Ленивый кэш версии entry‑point (сборки, из которой стартует приложение).
    /// </summary>
    static readonly Lazy<string> _entryAssemblyVersion = new(() =>
    {
        var entryAsm = Assembly.GetEntryAssembly();
        return entryAsm?.GetName()?.Version?.ToString() ?? "<unknown>";
    });

    /// <summary>
    /// Получить версию EntryPoint сборки.
    /// </summary>
    public static string GetEntryPointAssemblyVersion() => _entryAssemblyVersion.Value;
}
