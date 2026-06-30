using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Monq.Core.BasicDotNetMicroservice.Configuration;

namespace Monq.Core.BasicDotNetMicroservice.Middleware;

sealed class OpenTelemetryStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            var options = app.ApplicationServices
                .GetService(typeof(IConfiguration)) is IConfiguration config
                    ? config.GetSection(MicroserviceConstants.HostConfiguration.OpenTelemetrySectionName).Get<OpenTelemetryOptions>() ?? new OpenTelemetryOptions()
                    : new OpenTelemetryOptions();

            if (options.EnableMetrics && options.EnablePrometheusEndpoint)
            {
                app.UseOpenTelemetryPrometheusScrapingEndpoint();
            }

            next(app);
        };
    }
}
