using IdentityServer4.AccessTokenValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Monq.Core.BasicDotNetMicroservice.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Выполнить конфигурацию аутентификации на проекте из провайдера <paramref name="configuration"/>.
        /// </summary>
        public static IServiceCollection ConfigureSMAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var authConfig = configuration.GetSection("Authentication");

            if (!bool.TryParse(authConfig[AuthConstants.AuthenticationConfiguration.RequireHttpsMetadata], out var requireHttps))
                requireHttps = false;
            if (!bool.TryParse(authConfig[AuthConstants.AuthenticationConfiguration.EnableCaching], out var enableCaching))
                enableCaching = true;

            services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddOAuth2Introspection(IdentityServerAuthenticationDefaults.AuthenticationScheme, x =>
                {
                    x.Authority = authConfig[AuthConstants.AuthenticationConfiguration.Authority];
                    x.ClientId = authConfig[AuthConstants.AuthenticationConfiguration.ScopeName];
                    x.ClientSecret = authConfig[AuthConstants.AuthenticationConfiguration.ScopeSecret];
                    x.EnableCaching = enableCaching;
                    x.CacheDuration = TimeSpan.FromMinutes(5);
                    x.NameClaimType = "fullName";
                });

            return services;
        }
    }
}
