using Serilog;
using Serilog.Configuration;
using System;

namespace Monq.Core.BasicDotNetMicroservice.Enrichers.FromHttpContextHeader;

public static class HttpContextHeaderLoggerConfigurationExtensions
{
    public static LoggerConfiguration FromHttpContextHeader(
        this LoggerEnrichmentConfiguration enrichmentConfiguration,
        string headerKey,
        string propertyName)
    {
        if (enrichmentConfiguration == null)
            throw new ArgumentNullException(nameof(enrichmentConfiguration));
        return enrichmentConfiguration.With(new HttpContextHeaderEnricher(headerKey, propertyName));
    }
}