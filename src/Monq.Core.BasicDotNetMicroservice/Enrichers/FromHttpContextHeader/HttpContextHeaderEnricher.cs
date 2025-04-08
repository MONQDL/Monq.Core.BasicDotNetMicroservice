using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;
using System.Linq;

namespace Monq.Core.BasicDotNetMicroservice.Enrichers.FromHttpContextHeader;

public class HttpContextHeaderEnricher : ILogEventEnricher
{
    readonly string _propertyName;
    readonly string _headerKey;
    readonly IHttpContextAccessor _contextAccessor;

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

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (_contextAccessor.HttpContext == null)
            return;

        var headerValue = GetValueFromHeaders();
        if (!string.IsNullOrEmpty(headerValue))
        {
            var headerProperty = new LogEventProperty(_propertyName, new ScalarValue(headerValue));

            logEvent.AddOrUpdateProperty(headerProperty);
        }
    }

    private string? GetValueFromHeaders()
    {
        var headerValue = string.Empty;

        if (_contextAccessor.HttpContext.Request.Headers.TryGetValue(_headerKey, out var values))
        {
            headerValue = values.FirstOrDefault();
        }
        else if (_contextAccessor.HttpContext.Response.Headers.TryGetValue(_headerKey, out values))
        {
            headerValue = values.FirstOrDefault();
        }

        return headerValue;
    }
}