using Microsoft.AspNetCore.Builder;
using Monq.Core.BasicDotNetMicroservice.Middleware;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to log user data.
/// </summary>
public static class LogUserExtensions
{
    /// <summary>
    /// Добавляет логирование идентификатора и имени пользователя для всех запросов API.
    /// Размещать стоит после app.UseAuthentication().
    /// </summary>
    /// <param name="builder">The builder.</param>
    public static IApplicationBuilder UseLogUser(this IApplicationBuilder builder) =>
        builder.UseMiddleware<LogUserMiddleware>();
}
