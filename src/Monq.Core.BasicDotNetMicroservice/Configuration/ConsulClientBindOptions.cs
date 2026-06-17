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
    /// The wait time for blocking queries.
    /// </summary>
    public TimeSpan WaitTime { get; set; }

    /// <summary>
    /// Maps to Consul.ConsulClientConfiguration.
    /// </summary>
    public Consul.ConsulClientConfiguration ToConsulClientConfiguration()
    {
        return new Consul.ConsulClientConfiguration
        {
            Address = Address,
            Datacenter = Datacenter,
            Token = Token,
            WaitTime = WaitTime
        };
    }
}
