namespace Monq.Core.BasicDotNetMicroservice.Configuration;

/// <summary>
/// Bind-compatible options for Consul client configuration.
/// Only contains properties that can be configured from configuration sources.
/// </summary>
public class ConsulClientBindOptions
{
    /// <summary>
    /// The address of the Consul server.
    /// </summary>
    public Uri? Address { get; set; }

    /// <summary>
    /// The datacenter to use.
    /// </summary>
    public string? Datacenter { get; set; }

    /// <summary>
    /// The ACL token to use.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// The wait time for blocking queries (not used for one-time load).
    /// </summary>
    public TimeSpan WaitTime { get; set; }

    /// <summary>
    /// The root folder path in Consul KV store.
    /// If set, overrides the environment name as the base path.
    /// </summary>
    public string? RootFolder { get; set; }
}
