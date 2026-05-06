using Microsoft.AspNetCore.Http;
using Monq.Core.BasicDotNetMicroservice.Helpers;
using System;
using System.Threading.Tasks;

namespace Monq.Core.BasicDotNetMicroservice.Middleware;

/// <summary>
/// Middleware that exposes the microservice version at the <c>/api/version</c> endpoint.
/// Returns a JSON response containing the package version of the assembly
/// that contains the specified type.
/// </summary>
public class ApiVersionMiddleware
{
    readonly RequestDelegate _next;
    readonly Type _versionType;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiVersionMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next delegate in the request pipeline.</param>
    /// <param name="versionType">
    /// A type from the entry assembly used to determine the package version.
    /// Typically <c>typeof(Program)</c>.
    /// </param>
    public ApiVersionMiddleware(RequestDelegate next, Type versionType)
    {
        _next = next;
        _versionType = versionType;
    }

    /// <summary>
    /// Executes the middleware logic.
    /// If the request path is <c>/api/version</c>, returns the version as JSON.
    /// Otherwise, passes the request to the next middleware in the pipeline.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Path.Equals("/api/version", StringComparison.OrdinalIgnoreCase))
        {
            var version = MicroserviceVersionInfo.GetVersion(_versionType);
            var json = "{\"version\": \"" + version + "\"}";

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync(json);
            return;
        }

        await _next(context);
    }
}
