using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Monq.Core.BasicDotNetMicroservice.Addressing;

namespace Monq.Core.BasicDotNetMicroservice.Extensions;

/// <summary>
/// Extension methods for selecting and invoking microservice address resolvers.
/// </summary>
public static class MicroserviceAddressResolverExtensions
{
    /// <summary>
    /// Replaces the default microservice address resolver used by registrations on this web application builder.
    /// </summary>
    public static WebApplicationBuilder UseMicroserviceAddressResolver(
        this WebApplicationBuilder builder,
        IMicroserviceAddressResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Host.UseMicroserviceAddressResolver(resolver);
        return builder;
    }

    /// <summary>
    /// Replaces the address resolver used by gRPC client registrations on this web application builder.
    /// </summary>
    public static WebApplicationBuilder UseGrpcMicroserviceAddressResolver(
        this WebApplicationBuilder builder,
        IMicroserviceAddressResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Host.UseGrpcMicroserviceAddressResolver(resolver);
        return builder;
    }

    /// <summary>
    /// Replaces the address resolver used by HTTP client registrations on this web application builder.
    /// </summary>
    public static WebApplicationBuilder UseHttpMicroserviceAddressResolver(
        this WebApplicationBuilder builder,
        IMicroserviceAddressResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Host.UseHttpMicroserviceAddressResolver(resolver);
        return builder;
    }

    /// <summary>
    /// Replaces the default microservice address resolver used by registrations on this host builder.
    /// </summary>
    public static IHostBuilder UseMicroserviceAddressResolver(
        this IHostBuilder hostBuilder,
        IMicroserviceAddressResolver resolver) =>
        MicroserviceAddressResolverRegistry.SetDefault(hostBuilder, resolver);

    /// <summary>
    /// Replaces the default microservice address resolver using the final host context.
    /// </summary>
    public static IHostBuilder UseMicroserviceAddressResolver(
        this IHostBuilder hostBuilder,
        Func<HostBuilderContext, IMicroserviceAddressResolver> resolverFactory) =>
        MicroserviceAddressResolverRegistry.SetDefault(hostBuilder, resolverFactory);

    /// <summary>
    /// Replaces the address resolver used by gRPC client registrations.
    /// </summary>
    public static IHostBuilder UseGrpcMicroserviceAddressResolver(
        this IHostBuilder hostBuilder,
        IMicroserviceAddressResolver resolver) =>
        MicroserviceAddressResolverRegistry.SetGrpc(hostBuilder, resolver);

    /// <summary>
    /// Replaces the address resolver used by HTTP client registrations.
    /// </summary>
    public static IHostBuilder UseHttpMicroserviceAddressResolver(
        this IHostBuilder hostBuilder,
        IMicroserviceAddressResolver resolver) =>
        MicroserviceAddressResolverRegistry.SetHttp(hostBuilder, resolver);

    /// <summary>
    /// Resolves a microservice address using the supplied client resolver, protocol-specific resolver,
    /// default host resolver, or built-in ClusterIP resolver, in that order.
    /// </summary>
    public static ResolvedMicroserviceAddress ResolveMicroserviceAddress(
        this IHostBuilder hostBuilder,
        HostBuilderContext context,
        string applicationName,
        MicroserviceProtocol protocol,
        int? port = null,
        IMicroserviceAddressResolver? addressResolver = null)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);

        var resolver = MicroserviceAddressResolverRegistry.Get(
            hostBuilder,
            context,
            protocol,
            addressResolver);

        return resolver.Resolve(new(
            context.Configuration,
            context.HostingEnvironment,
            applicationName,
            protocol,
            port));
    }
}
