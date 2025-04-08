using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Monq.Core.BasicDotNetMicroservice.Helpers;
using Monq.Core.BasicDotNetMicroservice.Models;
using System;
using System.Text.Json;

namespace Monq.Core.BasicDotNetMicroservice.Filters;

public class VersionPointStartupFilter : IStartupFilter
{
    static string _version;
    static readonly JsonSerializerOptions _serializeOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

    /// <inheritdoc/>
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        _version = MicroserviceInfo.GetEntryPointAssembleVersion();

        return app =>
        {
            app.Map("/api/version", HandleVersionPoint);

            next(app);
        };
    }

    static void HandleVersionPoint(IApplicationBuilder app)
        => app.Run(async context =>
        {
            var version = new VersionViewModel() { Version = _version };
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(JsonSerializer.Serialize(version, _serializeOptions));
        });
}
