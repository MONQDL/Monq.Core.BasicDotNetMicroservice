using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Monq.Core.BasicDotNetMicroservice.Addressing;

/// <summary>
/// Describes an address resolution request.
/// </summary>
/// <param name="Configuration">Application configuration.</param>
/// <param name="Environment">Host environment.</param>
/// <param name="ApplicationName">Target application name.</param>
/// <param name="Protocol">Target protocol.</param>
/// <param name="PortOverride">Optional service port override.</param>
public sealed record MicroserviceAddressContext(
    IConfiguration Configuration,
    IHostEnvironment Environment,
    string ApplicationName,
    MicroserviceProtocol Protocol,
    int? PortOverride = null);
