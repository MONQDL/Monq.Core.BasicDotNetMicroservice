using System.Diagnostics;

namespace Monq.Core.BasicDotNetMicroservice.Telemetry;

/// <summary>
/// Default ActivitySource for Monq microservices.
/// </summary>
public sealed class MonqTelemetry
{
    /// <summary>
    /// Default activity source name.
    /// </summary>
    public const string SourceName = "Monq";

    /// <summary>
    /// Default ActivitySource. Use it in your microservices for custom activities.
    /// </summary>
    public static readonly ActivitySource Source = new(SourceName);
}
