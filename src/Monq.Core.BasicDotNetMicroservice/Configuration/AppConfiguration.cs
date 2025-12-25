namespace Monq.Core.BasicDotNetMicroservice;

/// <summary>
/// Main app configuration.
/// </summary>
public class AppConfiguration
{
    /// <summary>
    /// Базовый Uri, относительно которого строятся запросы к Api СМ.
    /// </summary>
    public required string BaseUri { get; set; }
}
