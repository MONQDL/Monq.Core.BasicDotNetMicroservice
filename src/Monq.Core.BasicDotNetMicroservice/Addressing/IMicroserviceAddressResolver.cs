namespace Monq.Core.BasicDotNetMicroservice.Addressing;

/// <summary>
/// Resolves microservice addresses for the current host environment.
/// </summary>
public interface IMicroserviceAddressResolver
{
    /// <summary>Resolves an address for the supplied context.</summary>
    ResolvedMicroserviceAddress Resolve(MicroserviceAddressContext context);
}
