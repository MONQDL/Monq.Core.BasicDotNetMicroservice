using System;

namespace Monq.Core.BasicDotNetMicroservice.Configuration;

/// <summary>
/// Bind-compatible options for HTTP client settings.
/// </summary>
public class HttpSettingsBindOptions
{
    /// <summary>
    /// The request URI where to POST metrics.
    /// </summary>
    public Uri? RequestUri { get; set; }

    /// <summary>
    /// The basic auth username.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// The basic auth password.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// The authorization token for the request.
    /// </summary>
    public string? AuthorizationToken { get; set; }

    /// <summary>
    /// Allow insecure SSL calls (usually used with self-signed certs).
    /// </summary>
    public bool AllowInsecureSsl { get; set; }

    /// <summary>
    /// Maps to App.Metrics.Reporting.Http.Client.HttpSettings.
    /// </summary>
    public App.Metrics.Reporting.Http.Client.HttpSettings ToHttpSettings()
    {
        return new App.Metrics.Reporting.Http.Client.HttpSettings
        {
            RequestUri = RequestUri,
            UserName = UserName,
            Password = Password,
            AuthorizationToken = AuthorizationToken,
            AllowInsecureSsl = AllowInsecureSsl
        };
    }
}
