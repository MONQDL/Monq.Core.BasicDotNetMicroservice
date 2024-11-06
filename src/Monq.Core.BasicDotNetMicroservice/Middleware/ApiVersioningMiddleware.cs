using Microsoft.AspNetCore.Http;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Monq.Core.BasicDotNetMicroservice.Middleware;

public class ApiVersioningMiddleware
{
    const string ApiSourceHeaderName = "X-Api-Source";
    const string ApiVersionHeaderName = "X-Api-Version";

    readonly RequestDelegate _next;

    readonly string? _version;
    readonly string? _assemblyName;

    /// <summary>
    /// Инициализирует новый объект класса <see cref="ApiVersioningMiddleware" />.
    /// </summary>
    /// <param name="next">The next.</param>
    /// <param name="modelsLibAssemblyType">Тип, содержащийся в сборке с моделями.</param>
    public ApiVersioningMiddleware(RequestDelegate next, Type? modelsLibAssemblyType)
    {
        _version = GetApiVersion(modelsLibAssemblyType);
        _assemblyName = GetAssemblyName(modelsLibAssemblyType);
        _next = next;
    }

    /// <summary>
    /// Вызов Middleware.
    /// </summary>
    public async Task Invoke(HttpContext context)
    {
        context.Response.Headers[ApiSourceHeaderName] = _assemblyName;
        context.Response.Headers[ApiVersionHeaderName] = _version;

        await _next(context);
    }

    static string? GetAssemblyName(Type? assemblyType)
    {
        if (assemblyType == null)
        {
            throw new ArgumentNullException(nameof(assemblyType), $"{nameof(assemblyType)} is null.");
        }

        return Assembly.GetAssembly(assemblyType)?.GetName().Name;
    }

    static string? GetApiVersion(Type? assemblyType)
    {
        if (assemblyType is null)
            throw new ArgumentNullException(nameof(assemblyType), $"{nameof(assemblyType)} is null.");

        var version = assemblyType
            .GetTypeInfo()
            .Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        return version;
    }
}
