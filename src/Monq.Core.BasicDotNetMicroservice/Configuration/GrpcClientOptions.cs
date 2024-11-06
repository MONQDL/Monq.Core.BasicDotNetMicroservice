using Grpc.AspNetCore.ClientFactory;
using Grpc.Net.Client;
using Grpc.Net.ClientFactory;
using System;

namespace Monq.Core.BasicDotNetMicroservice.Configuration;

/// <summary>
/// Options used to configure a gRPC client.
/// </summary>
public class GrpcClientOptions
{
    /// <summary>
    /// The logical name of the HTTP client to configure.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// A delegate that is used to configure a <see cref="GrpcClientFactoryOptions"/>.
    /// </summary>
    public Action<GrpcClientFactoryOptions>? ClientOptionsAction { get; set; }

    /// <summary>
    /// A delegate that is used to configure a <see cref="GrpcChannelOptions"/>.
    /// </summary>
    /// <value>If null, then default channel options are not applied.</value>
    public Action<GrpcChannelOptions>? ChannelOptionsAction { get; set; }

    /// <summary>
    /// A delegate that is used to configure a <see cref="GrpcContextPropagationOptions"/>.
    /// </summary>
    /// <value>If null, then context propagation is disabled.</value>
    public Action<GrpcContextPropagationOptions>? ContextPropagationOptionsAction { get; set; }
}
