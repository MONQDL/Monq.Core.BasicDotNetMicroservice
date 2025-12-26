using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;
using System.Linq;

namespace Monq.Core.BasicDotNetMicroservice.Enrichers.FromHttpContextHeader;

/// <summary>
/// Enrich log with values from http request headers.
/// </summary>
public class HttpContextHeaderEnricher : ILogEventEnricher
{
    readonly string _propertyName;
    readonly string _headerKey;
    readonly IHttpContextAccessor _contextAccessor;

    /// <summary>
    /// Create new object of <see cref="HttpContextHeaderEnricher"/>.
    /// </summary>
    /// <param name="headerKey">Header name</param>
    /// <param name="propertyName">The name of the property at log.</param>
    public HttpContextHeaderEnricher(string headerKey, string propertyName) :
        this(headerKey, propertyName, new HttpContextAccessor())
    {
    }

    internal HttpContextHeaderEnricher(string headerKey, string propertyName, IHttpContextAccessor contextAccessor)
    {
        _headerKey = headerKey;
        _propertyName = propertyName;
        _contextAccessor = contextAccessor;
    }
    
    /// <inheritdoc />
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (_contextAccessor.HttpContext == null)
            return;

        var headerValue = GetValueFromHeaders(_contextAccessor, _headerKey);
        if (!string.IsNullOrEmpty(headerValue))
        {
            var headerProperty = new LogEventProperty(_propertyName, new ScalarValue(headerValue));

            logEvent.AddOrUpdateProperty(headerProperty);
        }
    }

    static string? GetValueFromHeaders(IHttpContextAccessor contextAccessor, string headerKey)
    {
        string? headerValue = null;

        if (contextAccessor.HttpContext?.Request.Headers.TryGetValue(headerKey, out var values) == true)
            headerValue = values.FirstOrDefault();
        else if (contextAccessor.HttpContext?.Response.Headers.TryGetValue(headerKey, out values) == true)
            headerValue = values.FirstOrDefault();

        return headerValue;
    }
}
