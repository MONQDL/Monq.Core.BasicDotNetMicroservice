# Migration guide to Kubernetes service addressing

This guide describes how to migrate gRPC and REST HTTP clients from a shared
`BaseUri` to addresses resolved from an application name.

The addressing types and host-builder extensions are provided by separate
namespaces:

```csharp
using Monq.Core.BasicDotNetMicroservice.Addressing;
using Monq.Core.BasicDotNetMicroservice.Extensions;
```

The migration is optional and backward compatible. Existing
`IServiceCollection` overloads that accept `IConfiguration` continue to read
`BaseUri`. Applications can migrate one client at a time.

## When migration is needed

Migrate a client when it should connect directly to a Kubernetes Service or use
client-side endpoint discovery instead of receiving its destination address
from configuration.

No changes are required when a client must continue using its configured
`BaseUri`. Do not remove `BaseUri` until all other consumers, including
authentication and application-specific integrations, have been checked.

## Address strategies

| Resolver | gRPC address | HTTP address | Resolution behavior |
| --- | --- | --- | --- |
| `FixedMicroserviceAddressResolver` | Supplied address | Supplied address | Direct by default; HTTP can use the explicitly supplied mode, while gRPC uses discovery only for `dns:///` |
| `KubernetesClusterIpAddressResolver` | `http://{application}.{namespace}.svc.cluster.local:5006` | `http://{application}.{namespace}.svc.cluster.local:80` | No |
| `KubernetesServiceDiscoveryAddressResolver` | `dns:///{application}-grpc.{namespace}.svc.cluster.local:5006` | `http://{application}-http.{namespace}.svc.cluster.local:5005` | Yes |

When no resolver is selected, host-level client registrations use
`KubernetesClusterIpAddressResolver`. Development mode does not implicitly read
`BaseUri`; select `FixedMicroserviceAddressResolver` explicitly when that
behavior is required.

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

Select the resolver before registering clients. The following example keeps a
shared ingress address for local development and uses ClusterIP in Kubernetes:

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.UseMicroserviceAddressResolver(
        new FixedMicroserviceAddressResolver(
            builder.Configuration.GetValue<Uri>(nameof(AppConfiguration.BaseUri))
                ?? throw new InvalidOperationException(
                    "BaseUri not found at configuration.")));
}
else
{
    builder.UseMicroserviceAddressResolver(
        new KubernetesClusterIpAddressResolver());
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

For gRPC, only a `dns:///` address enables the gRPC DNS resolver and
`round_robin`. Direct `http://` and `https://` addresses do not enable
client-side balancing.

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
    .UseMicroserviceAddressResolver(context =>
        context.HostingEnvironment.IsDevelopment()
            ? new FixedMicroserviceAddressResolver(
                context.Configuration.GetValue<Uri>(nameof(AppConfiguration.BaseUri))
                    ?? throw new InvalidOperationException(
                        "BaseUri not found at configuration."))
            : new KubernetesClusterIpAddressResolver())
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
that are already using a pod while it terminates can still fail.

## Migration checklist

- Update `Monq.Core.BasicDotNetMicroservice` to a version containing the address
  resolver API.
- Replace configured client addresses with verified application-name constants.
- Select ClusterIP, service discovery, or a fixed address explicitly.
- Preserve existing client configuration callbacks.
- Confirm Kubernetes Service names, ports, `targetPort`, and headless status.
- Verify local development through the configured `BaseUri`.
- Build the application and run its existing tests.
- Verify connections and request distribution in the target environment.
- Remove obsolete per-client address settings only after all consumers have
  migrated.
