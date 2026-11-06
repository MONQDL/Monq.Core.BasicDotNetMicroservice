# Migration guide from v10 to v11

Version 11.0.0 is a major release with breaking changes compared to 10.1.0.
The primary changes are:

1. **`BaseUri`-based client overloads removed** — the `IServiceCollection`
   overloads of `AddGrpcPreConfiguredClient`, `AddGrpcPreConfiguredConsoleClient`
   and `AddRestHttpPreConfiguredClient` that read a shared `BaseUri` were
   removed. gRPC client registration now comes from
   `Monq.Core.GrpcClientExtensions` as `IServiceCollection` extensions that
   resolve an address from an application name; alternatively pass an explicit
   absolute `Uri`
   (see [Keeping a fixed address](#keeping-a-fixed-address)).
2. **`AdditionalHeadersInterceptor` removed** — `userspace-id` and `culture`
   header propagation and `Authorization` forwarding are now applied by
   `Monq.Core.GrpcClientExtensions` (`HeaderPropagationInterceptor`) to every
   pre-configured gRPC client. Remove manual registrations of the interceptor.
3. **gRPC channel defaults live in `Monq.Core.GrpcClientExtensions`** — unsafe
   call credentials, the 51 MB receive limit and call context propagation are
   applied by the library for every client.

New in 11.0.0:

- **Microservice addressing** — application-name based client registration
  through `IServiceCollection` extensions. The resolver API lives in the separate
  `Monq.Core.MicroserviceAddressing` package, in the
  `Monq.Core.MicroserviceAddressing.Addressing` and
  `Microsoft.Extensions.DependencyInjection` namespaces.
- **Authentication endpoint resolution** — `ConfigureMonqAuthentication` service-collection
  overloads and the `ConfigureStaticAuthentication` host-builder overloads (from
  `Monq.Core.HttpClientExtensions`) resolve the authentication address lazily from an
  application name or accept an explicit `Uri`;
  the introspection cache is configured with
  `Authentication:CacheDurationSeconds` (default 300 seconds).

> Libraries that call the removed overloads must be rebuilt against 11.0.0
> before an application upgrades, otherwise the removed methods cause
> `MissingMethodException` at runtime.

The addressing and service-discovery migration reference follows.

## Migrating clients to resolved addresses

Migrate a client when it should connect directly to a Kubernetes Service or use
client-side endpoint discovery instead of receiving its destination address
from configuration.

When a client must continue using its configured `BaseUri`, pass the address
as an explicit `Uri` (to the `Uri` overload of `AddGrpcPreConfiguredClient`,
`AddGrpcPreConfiguredConsoleClient`, or `AddRestHttpPreConfiguredClient`).
Select a
`FixedMicroserviceAddressResolver` only when the fixed address should be the
default for all clients of the application. Do not remove `BaseUri` until all
other consumers, including authentication and application-specific
integrations, have been checked.

### Address strategies

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

### Before migration

The previous registration reads the same `BaseUri` for every client:

```csharp
builder.Services.AddGrpcPreConfiguredClient<MyGrpcClient>(builder.Configuration);

builder.Services.AddRestHttpPreConfiguredClient<IMyHttpClient, MyHttpClient>(
    builder.Configuration);
```

These overloads were removed in 11.0.0. Before replacing them:

1. Determine the Kubernetes application or Service name.
2. Add that name to the application's constants library.
3. Verify whether the destination is a regular ClusterIP or a headless Service.
4. Verify the Service port and `targetPort`.
5. Keep existing client option delegates and HTTP configuration callbacks.

The examples below use neutral placeholder constants such as
`ApplicationNames.Orders`.

### Keeping a fixed address

When migration is not needed yet, keep the old single-address behavior by
passing an explicit absolute `Uri`. The address is
used as is, without consulting any resolver:

```csharp
builder.Services.AddGrpcPreConfiguredClient<MyGrpcClient>(
    new Uri("https://api.example.com"));

builder.Services.AddGrpcPreConfiguredConsoleClient<MyGrpcClient>(
    new Uri("https://api.example.com"));

builder.Services.AddRestHttpPreConfiguredClient<IMyHttpClient, MyHttpClient>(
    new Uri("https://api.example.com"));
```

The address can still come from the existing `BaseUri` setting:

```csharp
builder.Services.AddGrpcPreConfiguredClient<MyGrpcClient>(
    builder.Configuration.GetValue<Uri>(nameof(AppConfiguration.BaseUri))
        ?? throw new InvalidOperationException(
            "BaseUri not found at configuration."));
```

An explicit `Uri` with the `dns:///` scheme enables the gRPC DNS resolver and
`round_robin`; other schemes such as `https://` through an ingress are used
directly.

To pin one client to a fixed address while the rest of the application is
addressed by name, pass the fixed resolver to that registration:

```csharp
builder.Services.AddGrpcPreConfiguredClient<MyThresholdsServiceClient>(
    ApplicationNames.Thresholds,
    addressResolver: new FixedMicroserviceAddressResolver(new Uri("https://api.example.com")));
```

### Web application migration

The following example keeps a shared ingress address for local development and
uses the standard Basic.NET policies in Kubernetes:

```csharp
builder.Host.ConfigureBasicMicroservice();

if (builder.Environment.IsDevelopment())
{
    builder.Services.UseMicroserviceAddressResolver(
        new FixedMicroserviceAddressResolver(
            builder.Configuration.GetValue<Uri>(nameof(AppConfiguration.BaseUri))
                ?? throw new InvalidOperationException(
                    "BaseUri not found at configuration.")));
}

builder.Services.AddGrpcPreConfiguredClient<MyGrpcClient>(
    ApplicationNames.Orders);

builder.Services.AddRestHttpPreConfiguredClient<IMyHttpClient, MyHttpClient>(
    ApplicationNames.Orders);
```

An example development setting can use a reserved documentation domain:

```json
{
  "BaseUri": "https://api.example.com"
}
```

In application code, registrations can be grouped into local startup
extensions while the library calls remain unchanged:

```csharp
// gRPC Clients.
builder.Services.AddGrpcClients();

// Http Clients
builder.Services.AddHttpClients();
```

### Different strategies for gRPC and HTTP

An application can use different resolvers for each protocol. For example, gRPC can use
a headless Service while HTTP uses a regular ClusterIP Service:

```csharp
builder.Services
    .UseGrpcMicroserviceAddressResolver(
        new KubernetesServiceDiscoveryAddressResolver())
    .UseHttpMicroserviceAddressResolver(
        new KubernetesClusterIpAddressResolver());

builder.Services.AddGrpcPreConfiguredClient<MyGrpcClient>(
    ApplicationNames.Orders);

builder.Services.AddRestHttpPreConfiguredClient<IMyHttpClient, MyHttpClient>(
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
builder.Services.ConfigureGrpcServiceDiscovery(options =>
    options.DnsRefreshInterval = TimeSpan.FromSeconds(10));
```

The address strategy can still be selected per client. The DNS refresh interval
is application-wide because gRPC selects one resolver factory for the `dns`
URI scheme.

For REST clients, name-based registrations always enable HTTP endpoint discovery
through the DNS endpoint provider, so `ResolutionMode` does not need to be used
to distinguish ClusterIP from a headless Service.

### Per-client strategy

A resolver passed to a client registration overrides the resolvers registered
for the application. This
allows clients in the same application to use different strategies:

```csharp
builder.Services.AddGrpcPreConfiguredClient<OrdersClient>(
    ApplicationNames.Orders,
    addressResolver: new KubernetesServiceDiscoveryAddressResolver());

builder.Services.AddGrpcPreConfiguredClient<NotificationsClient>(
    ApplicationNames.Notifications,
    addressResolver: new KubernetesClusterIpAddressResolver());
```

Resolver precedence is:

1. Resolver supplied to the client registration.
2. Protocol-specific host resolver.
3. Common host resolver.
4. Built-in `KubernetesClusterIpAddressResolver`.

### Console and background application migration

Console applications configure the host builder and register gRPC and REST
clients on `IServiceCollection`. Select the resolver inside `ConfigureServices` to
use the final host configuration and environment:

```csharp
using var host = Host.CreateDefaultBuilder(args)
    .ConfigureBasicConsoleMicroservice()
    .ConfigureServices((context, services) =>
    {
        services.UseMicroserviceAddressResolver(
            context.HostingEnvironment.IsDevelopment()
                ? new FixedMicroserviceAddressResolver(
                    context.Configuration.GetValue<Uri>(nameof(AppConfiguration.BaseUri))
                        ?? throw new InvalidOperationException(
                            "BaseUri not found at configuration."))
                : new ProtocolMicroserviceAddressResolver(
                    httpResolver: new KubernetesClusterIpAddressResolver(),
                    grpcResolver: new KubernetesServiceDiscoveryAddressResolver()));

        services.AddRestHttpPreConfiguredClient<IMyHttpClient, MyHttpClient>(
            ApplicationNames.Orders);

        services.AddGrpcPreConfiguredConsoleClient<MyGrpcClient>(
            ApplicationNames.Orders);
    })
    .Build();
```

If a console application needs environment-dependent and protocol-dependent
rules at the same time, implement `IMicroserviceAddressResolver` in the
application and select the address from `MicroserviceAddressContext`.

### Custom resolver

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

### Kubernetes requirements

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

- Update `Monq.Core.BasicDotNetMicroservice` to 11.0.0.
- Update libraries that call client registration APIs to versions built
  against 11.0.0 before deploying.
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
- Remove per-client `BaseUri` address settings only after all consumers have
  migrated.
