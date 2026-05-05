using System;
using Constants = Monq.Core.BasicDotNetMicroservice.MicroserviceConstants.MetricsConfiguration;

namespace Monq.Core.BasicDotNetMicroservice.Configuration;

/// <summary>
/// Bind-compatible options for HTTP policy settings.
/// </summary>
public class HttpPolicyBindOptions
{
    /// <summary>
    /// The backoff period after failures.
    /// </summary>
    public TimeSpan BackoffPeriod { get; set; } = Constants.DefaultBackoffPeriod;

    /// <summary>
    /// Number of failures before entering backoff mode.
    /// </summary>
    public int FailuresBeforeBackoff { get; set; } = Constants.DefaultFailuresBeforeBackoff;

    /// <summary>
    /// The request timeout.
    /// </summary>
    public TimeSpan Timeout { get; set; } = Constants.DefaultTimeout;

    /// <summary>
    /// Maps to App.Metrics.Reporting.Http.Client.HttpPolicy.
    /// </summary>
    public App.Metrics.Reporting.Http.Client.HttpPolicy ToHttpPolicy()
    {
        return new App.Metrics.Reporting.Http.Client.HttpPolicy
        {
            BackoffPeriod = BackoffPeriod,
            FailuresBeforeBackoff = FailuresBeforeBackoff,
            Timeout = Timeout
        };
    }
}
