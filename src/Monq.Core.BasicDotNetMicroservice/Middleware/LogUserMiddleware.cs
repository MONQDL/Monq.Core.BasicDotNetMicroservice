using Microsoft.AspNetCore.Http;
using Monq.Core.BasicDotNetMicroservice.Helpers;
using Serilog.Context;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Monq.Core.BasicDotNetMicroservice.Middleware;

/// <summary>
/// Middleware предоставляет функционал добавления идентификатора и имени пользователя в LogContext.
/// </summary>
public class LogUserMiddleware
{
    const sbyte SystemUserId = -1;
    const sbyte DefaultUserId = 0;

    const string SubjectClaim = "sub";
    const string ClientIdClaim = "client_id";
    const string ClientIdValue = "smon-res-owner";

    readonly RequestDelegate _next;

    /// <summary>
    /// Инициализирует новый объект класса <see cref="LogUserMiddleware"/>.
    /// </summary>
    public LogUserMiddleware(RequestDelegate next) => _next = next;

    /// <summary>
    /// Вызов Middleware.
    /// </summary>
    public async Task Invoke(HttpContext context)
    {
        using (LogContext.PushProperty(LoggerEnvironment.LoggerFieldNames.UserId, GetSubject(context.User)))
        using (LogContext.PushProperty(LoggerEnvironment.LoggerFieldNames.UserName, context.User?.Identity?.Name))
        {
            await _next(context);
        }
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
