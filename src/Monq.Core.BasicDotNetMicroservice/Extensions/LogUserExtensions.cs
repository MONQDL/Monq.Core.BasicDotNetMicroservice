using Microsoft.AspNetCore.Builder;
using Monq.Core.BasicDotNetMicroservice.Middleware;

namespace Microsoft.Extensions.DependencyInjection;

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
