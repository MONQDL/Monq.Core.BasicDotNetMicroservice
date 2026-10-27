namespace Monq.Core.BasicDotNetMicroservice.Addressing;

/// <summary>
/// Defines the supported microservice protocols.
/// </summary>
public enum MicroserviceProtocol
{
    /// <summary>gRPC over HTTP/2.</summary>
    Grpc,

    /// <summary>HTTP.</summary>
    Http,
}
