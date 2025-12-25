using Serilog;
using Serilog.Configuration;
using System;

namespace Monq.Core.BasicDotNetMicroservice.Enrichers.FromHttpContextHeader;

/// <summary>
/// Log enrichment extensions.
/// </summary>
public static class LoggerConfigurationExtensions
{
    /// <summary>
    /// Enrich log with values from http request headers.
    /// </summary>
    /// <param name="enrichmentConfiguration">Configuration.</param>
    /// <param name="headerKey">Header name</param>
    /// <param name="propertyName">The name of the property at log.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">enrichmentConfiguration is null</exception>
    public static LoggerConfiguration FromHttpContextHeader(
        this LoggerEnrichmentConfiguration enrichmentConfiguration,
        string headerKey,
        string propertyName)
    {
        ArgumentNullException.ThrowIfNull(enrichmentConfiguration);

        return enrichmentConfiguration.With(new HttpContextHeaderEnricher(headerKey, propertyName));
    }
}
