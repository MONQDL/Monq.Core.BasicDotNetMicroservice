using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Monq.Core.MicroserviceAddressing.Addressing;
using Monq.Core.MicroserviceAddressing.Hosting;

namespace Monq.Core.BasicDotNetMicroservice.Extensions;

/// <summary>
/// Extension methods for configuring microservice address resolution on web applications.
/// </summary>
public static class MicroserviceAddressResolverExtensions
{
    /// <summary>Sets the default resolver.</summary>
    public static WebApplicationBuilder UseMicroserviceAddressResolver(
        this WebApplicationBuilder builder,
        IMicroserviceAddressResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Host.UseMicroserviceAddressResolver(resolver);
        return builder;
    }

    /// <summary>Sets the default resolver using the final host context.</summary>
    public static WebApplicationBuilder UseMicroserviceAddressResolver(
        this WebApplicationBuilder builder,
        Func<HostBuilderContext, IMicroserviceAddressResolver> resolverFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Host.UseMicroserviceAddressResolver(resolverFactory);
        return builder;
    }

    /// <summary>Sets the HTTP resolver.</summary>
    public static WebApplicationBuilder UseHttpMicroserviceAddressResolver(
        this WebApplicationBuilder builder,
        IMicroserviceAddressResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Host.UseHttpMicroserviceAddressResolver(resolver);
        return builder;
    }

    /// <summary>Sets the HTTP resolver using the final host context.</summary>
    public static WebApplicationBuilder UseHttpMicroserviceAddressResolver(
        this WebApplicationBuilder builder,
        Func<HostBuilderContext, IMicroserviceAddressResolver> resolverFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Host.UseHttpMicroserviceAddressResolver(resolverFactory);
        return builder;
    }

    /// <summary>Sets the gRPC resolver.</summary>
    public static WebApplicationBuilder UseGrpcMicroserviceAddressResolver(
        this WebApplicationBuilder builder,
        IMicroserviceAddressResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Host.UseGrpcMicroserviceAddressResolver(resolver);
        return builder;
    }

    /// <summary>Sets the gRPC resolver using the final host context.</summary>
    public static WebApplicationBuilder UseGrpcMicroserviceAddressResolver(
        this WebApplicationBuilder builder,
        Func<HostBuilderContext, IMicroserviceAddressResolver> resolverFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Host.UseGrpcMicroserviceAddressResolver(resolverFactory);
        return builder;
    }
}
