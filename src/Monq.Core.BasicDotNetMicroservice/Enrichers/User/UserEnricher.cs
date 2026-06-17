using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;
using System.Security.Claims;

namespace Monq.Core.BasicDotNetMicroservice.Enrichers.User;

/// <summary>
/// Serilog enricher that extracts <c>UserId</c> and <c>UserName</c> from the current
/// <see cref="System.Security.Claims.ClaimsPrincipal"/>. Recognizes system users by the
/// <c>client_id</c> claim value <c>smon-res-owner</c> (assigned UserId = -1).
/// </summary>
public class UserEnricher : ILogEventEnricher
{
    const sbyte SystemUserId = -1;
    const sbyte DefaultUserId = 0;

    const string SubjectClaim = "sub";
    const string ClientIdClaim = "client_id";
    const string ClientIdValue = "smon-res-owner";

    readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Creates a new instance of <see cref="UserEnricher"/>.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor to read the current user from.</param>
    public UserEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Enriches the log event with <c>UserId</c> and <c>UserName</c> from the current user principal.
    /// </summary>
    /// <param name="logEvent">The log event to enrich.</param>
    /// <param name="propertyFactory">Factory to create log event properties.</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
            return;

        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("UserId", GetSubject(user)));
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("UserName", user.Identity?.Name));
    }

    static long GetSubject(ClaimsPrincipal? user)
    {
        if (user == null)
            return DefaultUserId;

        var userSub = user.Claims.FirstOrDefault(x => x.Type == SubjectClaim)?.Value;

        if (string.IsNullOrWhiteSpace(userSub))
        {
            var isSystemUser = IsSystemUser(user);
            return isSystemUser ? SystemUserId : DefaultUserId;
        }

        return !long.TryParse(userSub, out var userId) ? DefaultUserId : userId;
    }

    static bool IsSystemUser(ClaimsPrincipal? user)
    {
        if (user == null)
            return false;

        var userClientId = user.Claims.FirstOrDefault(x => x.Type == ClientIdClaim)?.Value;

        if (string.IsNullOrWhiteSpace(userClientId))
            return false;

        return string.Equals(userClientId, ClientIdValue, StringComparison.OrdinalIgnoreCase);
    }
}
