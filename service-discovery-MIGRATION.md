# Migration guide to Kubernetes service addressing

This guide describes how to migrate gRPC and REST HTTP clients from a shared
`BaseUri` to addresses resolved from an application name.

The addressing types and host-builder extensions are provided by separate
namespaces:

```csharp
using Monq.Core.BasicDotNetMicroservice.Extensions;
using Monq.Core.MicroserviceAddressing.Addressing;
using Monq.Core.MicroserviceAddressing.Hosting;
```

Existing `IServiceCollection` overloads that accept `IConfiguration` continue
to read `BaseUri`, but they are marked obsolete and produce a compiler warning.
The new resolver API was moved to `Monq.Core.MicroserviceAddressing` before it
was adopted by consumers; therefore no compatibility wrappers are retained for
the temporary Basic.NET resolver namespaces.

Breaking change in 11.0.0: the temporary resolver types that shipped in the
published `Monq.Core.BasicDotNetMicroservice` 10.1.1 package under
`Monq.Core.BasicDotNetMicroservice.Addressing` were removed without
compatibility wrappers. Update usings to
`Monq.Core.MicroserviceAddressing.Addressing` and
`Monq.Core.MicroserviceAddressing.Hosting`.

## When migration is needed

Migrate a client when it should connect directly to a Kubernetes Service or use
client-side endpoint discovery instead of receiving its destination address
from configuration.

When a client must continue using its configured `BaseUri`, select a
`FixedMicroserviceAddressResolver` and migrate its registration to the
host-builder overload. Do not remove `BaseUri` until all other consumers,
including authentication and application-specific integrations, have been
checked.

## Address strategies

| Resolver | gRPC address | HTTP address | Resolution behavior |
| --- | --- | --- | --- |
| `FixedMicroserviceAddressResolver` | Supplied address | Supplied address | Direct by default; HTTP can use the explicitly supplied mode, while gRPC uses discovery only for `dns:///` |
| `KubernetesClusterIpAddressResolver` | `http://{application}.{namespace}.svc.cluster.local:5006` | `http://{application}.{namespace}.svc.cluster.local:80` | No |
| `KubernetesServiceDiscoveryAddressResolver` | `dns:///{application}-grpc.{namespace}.svc.cluster.local:5006` | `http://{application}-http.{namespace}.svc.cluster.local:5005` | Yes |

`ConfigureBasicMicroservice` and `ConfigureBasicConsoleMicroservice` install the
standard policy as a fallback: HTTP uses `KubernetesClusterIpAddressResolver`,
while gRPC uses `KubernetesServiceDiscoveryAddressResolver`. A resolver
registered before the call is preserved, and a resolver registered after it
overrides the policy. Development mode does not
implicitly read `BaseUri`; the application must select
`FixedMicroserviceAddressResolver` explicitly when that behavior is required.
Applications that do not use a Basic.NET configuration extension retain the
addressing library's built-in ClusterIP fallback.

The Kubernetes namespace is derived from `IHostEnvironment.EnvironmentName`
and converted to lower case. Do not pass the namespace separately.

The standard ports are supplied by the resolvers. Pass `port` to a client
registration only when its service uses a non-standard port.

For a regular ClusterIP Service, use its Service port. For a headless Service,
use the application or pod port because the resolved addresses point directly
to pods.

## Before migration

The previous registration reads the same `BaseUri` for every client:

```csharp
builder.Services.AddGrpcPreConfiguredClient<MyGrpcClient>(builder.Configuration);

builder.Services.AddRestHttpPreConfiguredClient<IMyHttpClient, MyHttpClient>(
    builder.Configuration);
```

Before replacing it:

1. Determine the Kubernetes application or Service name.
2. Add that name to the application's constants library.
3. Verify whether the destination is a regular ClusterIP or a headless Service.
4. Verify the Service port and `targetPort`.
5. Keep existing client option delegates and HTTP configuration callbacks.

The examples below use neutral placeholder constants such as
`ApplicationNames.Orders`.

## Web application migration

The following example keeps a shared ingress address for local development and
uses the standard Basic.NET policies in Kubernetes:

```csharp
builder.Host.ConfigureBasicMicroservice();

if (builder.Environment.IsDevelopment())
{
    builder.UseMicroserviceAddressResolver(
        new FixedMicroserviceAddressResolver(
            builder.Configuration.GetValue<Uri>(nameof(AppConfiguration.BaseUri))
                ?? throw new InvalidOperationException(
                    "BaseUri not found at configuration.")));
}

builder.Host.AddGrpcPreConfiguredClient<MyGrpcClient>(
    ApplicationNames.Orders);

builder.Host.AddRestHttpPreConfiguredClient<IMyHttpClient, MyHttpClient>(
    ApplicationNames.Orders);
```

An example development setting can use a reserved documentation domain:

```json
{
  "BaseUri": "https://api.example.com"
}
```

In application code, registrations can be grouped into local startup
extensions while the library calls remain on `builder.Host`:

```csharp
// gRPC Clients.
builder.AddGrpcClients();

// Http Clients
builder.AddHttpClients();
```

## Different strategies for gRPC and HTTP

A host can use different resolvers for each protocol. For example, gRPC can use
a headless Service while HTTP uses a regular ClusterIP Service:

```csharp
builder
    .UseGrpcMicroserviceAddressResolver(
        new KubernetesServiceDiscoveryAddressResolver())
    .UseHttpMicroserviceAddressResolver(
        new KubernetesClusterIpAddressResolver());

builder.Host.AddGrpcPreConfiguredClient<MyGrpcClient>(
    ApplicationNames.Orders);

builder.Host.AddRestHttpPreConfiguredClient<IMyHttpClient, MyHttpClient>(
    ApplicationNames.Orders);
```

For gRPC, an address resolved in `EndpointResolutionMode.ServiceDiscovery` mode
or using the `dns:///` scheme enables the gRPC DNS resolver and `round_robin`.
Direct `http://` and `https://` addresses in `Direct` mode do not enable
client-side balancing.

`DnsResolverFactory` is registered once and shared by all gRPC clients using
`dns:///`. Each gRPC channel receives its own DNS resolver and endpoint state.
The periodic refresh interval defaults to 30 seconds and can be configured once
for the application:

```csharp
builder.ConfigureGrpcServiceDiscovery(options =>
    options.DnsRefreshInterval = TimeSpan.FromSeconds(10));
```

The address strategy can still be selected per client. The DNS refresh interval
is application-wide because gRPC selects one resolver factory for the `dns`
URI scheme.

For HTTP, the URI scheme cannot distinguish ClusterIP from a headless Service.
`ResolvedMicroserviceAddress.ResolutionMode` explicitly controls whether HTTP
endpoint discovery is enabled.

## Per-client strategy

A resolver passed to a client registration overrides host-level resolvers. This
allows clients in the same application to use different strategies:

```csharp
builder.Host.AddGrpcPreConfiguredClient<OrdersClient>(
    ApplicationNames.Orders,
    addressResolver: new KubernetesServiceDiscoveryAddressResolver());

builder.Host.AddGrpcPreConfiguredClient<NotificationsClient>(
    ApplicationNames.Notifications,
    addressResolver: new KubernetesClusterIpAddressResolver());
```

Resolver precedence is:

1. Resolver supplied to the client registration.
2. Protocol-specific host resolver.
3. Common host resolver.
4. Built-in `KubernetesClusterIpAddressResolver`.

## Console and background application migration

Console applications register clients directly on `IHostBuilder`. A resolver
factory can use the final host configuration and environment:

```csharp
using var host = Host.CreateDefaultBuilder(args)
    .ConfigureBasicConsoleMicroservice()
    .UseMicroserviceAddressResolver(context =>
        context.HostingEnvironment.IsDevelopment()
            ? new FixedMicroserviceAddressResolver(
                context.Configuration.GetValue<Uri>(nameof(AppConfiguration.BaseUri))
                    ?? throw new InvalidOperationException(
                        "BaseUri not found at configuration."))
            : new ProtocolMicroserviceAddressResolver(
                httpResolver: new KubernetesClusterIpAddressResolver(),
                grpcResolver: new KubernetesServiceDiscoveryAddressResolver()))
    .AddGrpcPreConfiguredConsoleClient<MyGrpcClient>(
        ApplicationNames.Orders)
    .AddRestHttpPreConfiguredClient<IMyHttpClient, MyHttpClient>(
        ApplicationNames.Orders)
    .Build();
```

If a console application needs environment-dependent and protocol-dependent
rules at the same time, implement `IMicroserviceAddressResolver` in the
application and select the address from `MicroserviceAddressContext`.

## Custom resolver

Implement `IMicroserviceAddressResolver` when namespace, service naming, or
routing rules differ from the standard convention:

```csharp
public sealed class ApplicationAddressResolver : IMicroserviceAddressResolver
{
    public ResolvedMicroserviceAddress Resolve(MicroserviceAddressContext context)
    {
        if (context.Environment.IsDevelopment())
        {
            var address = context.Configuration.GetValue<Uri>(
                    nameof(AppConfiguration.BaseUri))
                ?? throw new InvalidOperationException(
                    "BaseUri not found at configuration.");

            return new(address);
        }

        return new KubernetesClusterIpAddressResolver().Resolve(context);
    }
}
```

Keep application-specific routing rules outside
`Monq.Core.BasicDotNetMicroservice`.

## Kubernetes requirements

`KubernetesClusterIpAddressResolver` expects a regular Service named
`{application}`.

`KubernetesServiceDiscoveryAddressResolver` expects headless Services named:

- `{application}-grpc` for gRPC;
- `{application}-http` for HTTP.

A headless Service must publish ready pod endpoints. Client-side discovery does
not replace readiness probes, graceful shutdown, or retry policies. Requests
that are already using a pod while it terminates can still fail. Discovery has
no ClusterIP fallback: while a required headless Service is missing or publishes
no ready endpoints, gRPC calls fail with `UNAVAILABLE` and HTTP endpoint
discovery fails the request.

## Migration checklist

- Update `Monq.Core.BasicDotNetMicroservice` to a version containing the address
  resolver API.
- Replace configured client addresses with verified application-name constants.
- Override the standard Basic.NET policy only when the application requires a
  different protocol or fixed-address strategy. The override works whether it
  is registered before or after the Basic.NET configuration call.
- Configure the shared gRPC DNS refresh interval when the 30-second default is
  not appropriate.
- Preserve existing client configuration callbacks.
- Confirm Kubernetes Service names, ports, `targetPort`, and headless status.
- Verify local development through the configured `BaseUri`.
- Build the application and run its existing tests.
- Verify connections and request distribution in the target environment.
- Remove obsolete per-client address settings only after all consumers have
  migrated.
