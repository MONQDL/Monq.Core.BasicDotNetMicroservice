using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Monq.Core.BasicDotNetMicroservice.Helpers;

namespace Monq.Core.BasicDotNetMicroservice.Middleware
{
    /// <summary>
    /// Middleware предоставляет функционал добавления идентификатора и имени пользователя в LogContext.
    /// </summary>
    public class LogUserMiddleware
    {
        const sbyte _systemUserId = -1;
        const sbyte _defaultUserId = 0;

        const string _subjectClaim = "sub";
        const string _clientIdClaim = "client_id";
        const string _clientIdValue = "smon-res-owner";

        readonly RequestDelegate _next;

        /// <summary>
        /// Инициализирует новый объект класса <see cref="LogUserMiddleware"/>.
        /// </summary>
        public LogUserMiddleware(RequestDelegate next) => _next = next;

        /// <summary>
        /// Вызов Middleware.
        /// </summary>
        public async Task Invoke(HttpContext? context)
        {
            using (LogContext.PushProperty(LoggerEnvironment.LoggerFieldNames.UserId, GetSubject(context?.User)))
            using (LogContext.PushProperty(LoggerEnvironment.LoggerFieldNames.UserName, context?.User?.Identity?.Name))
            {
                await _next(context);
            }
        }

        long GetSubject(ClaimsPrincipal? user)
        {
            if (user == null)
                return _defaultUserId;

            var userSub = user.Claims.FirstOrDefault(x => x.Type == _subjectClaim)?.Value;

            if (string.IsNullOrWhiteSpace(userSub))
            {
                var isSystemUser = IsSystemUser(user);
                return isSystemUser ? _systemUserId : _defaultUserId;
            }

            return !long.TryParse(userSub, out var userId) ? _defaultUserId : userId;
        }

        bool IsSystemUser(ClaimsPrincipal? user)
        {
            if (user == null)
                return false;

            var userClientId = user.Claims.FirstOrDefault(x => x.Type == _clientIdClaim)?.Value;

            if (string.IsNullOrWhiteSpace(userClientId))
                return false;

            return string.Equals(userClientId, _clientIdValue, StringComparison.OrdinalIgnoreCase);
        }
    }
}
