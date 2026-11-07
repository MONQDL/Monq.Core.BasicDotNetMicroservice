using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Balancer;
using Grpc.Net.ClientFactory;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monq.Core.BasicDotNetMicroservice.Extensions;
using Monq.Core.GrpcClientExtensions;
using Monq.Core.HttpClientExtensions;
using Monq.Core.MicroserviceAddressing.Addressing;
using Xunit;

namespace Monq.Core.BasicDotNetMicroservice.Tests;

public class MicroserviceClientRegistrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GrpcRegistration_DoesNotRequireBaseUri(bool consoleClient)
    {
        var configureClientInvoked = false;
        var hostBuilder = CreateHostBuilder()
            .ConfigureServices(services => services.UseMicroserviceAddressResolver(
                new FixedMicroserviceAddressResolver(new Uri("http://localhost:5006"))))
            .ConfigureServices(services =>
            {
                var options = new GrpcClientOptions
                {
                    ClientOptionsAction = _ => configureClientInvoked = true,
                };

                if (consoleClient)
                    services.AddGrpcPreConfiguredConsoleClient<TestGrpcClient>("test-service", options: options);
                else
                    services.AddGrpcPreConfiguredClient<TestGrpcClient>("test-service", options: options);
            });

        using var host = hostBuilder.Build();

        Assert.NotNull(host.Services.GetRequiredService<TestGrpcClient>());
        Assert.Equal(
            new Uri("http://localhost:5006"),
            host.Services
                .GetRequiredService<IMicroserviceAddressProvider>()
                .Resolve("test-service", MicroserviceProtocol.Grpc)
                .Address);
        Assert.True(configureClientInvoked);
        Assert.Empty(host.Services.GetServices<ResolverFactory>().OfType<DnsResolverFactory>());
    }

    [Fact]
    public void GrpcServiceDiscovery_RegistersOneDnsResolverAlongsideOtherResolvers()
    {
        var staticResolver = new StaticResolverFactory(_ => []);
        var hostBuilder = CreateHostBuilder()
            .ConfigureServices(services =>
                services.AddSingleton<ResolverFactory>(staticResolver))
            .ConfigureServices(services => services.UseGrpcMicroserviceAddressResolver(
                new KubernetesServiceDiscoveryAddressResolver()))
            .ConfigureServices(services =>
            {
                services.AddGrpcPreConfiguredClient<TestGrpcClient>("first-service");
                services.AddGrpcPreConfiguredClient<SecondTestGrpcClient>("second-service");
            });

        using var host = hostBuilder.Build();
        var resolverFactories = host.Services.GetServices<ResolverFactory>().ToArray();

        Assert.Contains(staticResolver, resolverFactories);
        Assert.Single(resolverFactories, factory => factory.Name == "dns");
    }

    [Fact]
    public void GrpcServiceDiscovery_EnabledForServiceDiscoveryModeWithoutDnsScheme()
    {
        var hostBuilder = CreateHostBuilder()
            .ConfigureServices(services => services.UseGrpcMicroserviceAddressResolver(
                new FixedMicroserviceAddressResolver(
                    new Uri("http://test-service:5006"),
                    EndpointResolutionMode.ServiceDiscovery)))
            .ConfigureServices(services =>
                services.AddGrpcPreConfiguredClient<TestGrpcClient>("test-service"));

        using var host = hostBuilder.Build();

        Assert.Single(
            host.Services.GetServices<ResolverFactory>(),
            factory => factory.Name == "dns");
    }

    [Fact]
    public void GrpcServiceDiscovery_UsesConfiguredDnsRefreshInterval()
    {
        var refreshInterval = TimeSpan.FromSeconds(10);
        var hostBuilder = CreateHostBuilder()
            .ConfigureServices(services =>
                services.ConfigureGrpcServiceDiscovery(options =>
                    options.DnsRefreshInterval = refreshInterval))
            .ConfigureServices(services => services.UseGrpcMicroserviceAddressResolver(
                new KubernetesServiceDiscoveryAddressResolver()))
            .ConfigureServices(services =>
                services.AddGrpcPreConfiguredClient<TestGrpcClient>("test-service"));

        using var host = hostBuilder.Build();

        Assert.Equal(
            refreshInterval,
            host.Services
                .GetRequiredService<IOptions<GrpcServiceDiscoveryOptions>>()
                .Value.DnsRefreshInterval);
        Assert.Single(
            host.Services.GetServices<ResolverFactory>(),
            factory => factory.Name == "dns");
    }

    [Theory]
    [InlineData(EndpointResolutionMode.Direct)]
    [InlineData(EndpointResolutionMode.ServiceDiscovery)]
    public void HttpRegistration_UsesResolvedAddressAndMode(EndpointResolutionMode resolutionMode)
    {
        var address = new Uri("http://test-service:5005");
        ServiceDescriptor[] registrations = [];
        var hostBuilder = CreateHostBuilder()
            .ConfigureServices(services => services.UseHttpMicroserviceAddressResolver(
                new FixedMicroserviceAddressResolver(address, resolutionMode)))
            .ConfigureServices((_, services) =>
            {
                services.AddRestHttpPreConfiguredClient<ITestHttpClient, TestHttpClient>(
                    "test-service",
                    client => client.DefaultRequestHeaders.Add("x-test", "value"));

                registrations = [.. services];
            });

        using var host = hostBuilder.Build();
        var client = Assert.IsType<TestHttpClient>(
            host.Services.GetRequiredService<ITestHttpClient>());

        Assert.Equal(new Uri("http://test-service:5005/"), client.ConfiguredHttpClient.BaseAddress);
        Assert.Equal(
            "value",
            client.ConfiguredHttpClient.DefaultRequestHeaders.GetValues("x-test").Single());

        // HTTP endpoint discovery is registered only for Service Discovery resolutions;
        // ClusterIP (Direct) clients get no endpoint watcher and connect to the Service ClusterIP.
        var hasServiceDiscovery = resolutionMode == EndpointResolutionMode.ServiceDiscovery;
        Assert.Equal(
            hasServiceDiscovery,
            registrations.Any(descriptor =>
                descriptor.ServiceType.FullName
                    == "Microsoft.Extensions.ServiceDiscovery.ServiceEndpointWatcherFactory"));
        Assert.Equal(
            hasServiceDiscovery,
            registrations.Any(descriptor =>
                descriptor.ServiceType.Name == "IServiceEndpointProviderFactory"
                && descriptor.ImplementationType?.Name == "DnsServiceEndpointProviderFactory"));
    }

    [Fact]
    public void HttpRegistration_WithoutDeterminableResolutionModeThrows()
    {
        InvalidOperationException? failure = null;
        using var host = CreateHostBuilder()
            .ConfigureServices(services => services.UseHttpMicroserviceAddressResolver(
                new ThrowingAddressResolver()))
            .ConfigureServices((_, services) =>
                failure = Assert.Throws<InvalidOperationException>(
                    () => services.AddRestHttpPreConfiguredClient<ITestHttpClient, TestHttpClient>("test-service")))
            .Build();

        Assert.NotNull(failure);
        Assert.Contains("test-service", failure!.Message);
    }

    [Fact]
    public void HttpRegistration_MultipleClientsPerApplication_MixedResolutionModes()
    {
        IHttpClientBuilder discoveryBuilder = null!;
        IHttpClientBuilder directBuilder = null!;

        using var host = CreateHostBuilder()
            .ConfigureServices(services => services.UseHttpMicroserviceAddressResolver(
                new FixedMicroserviceAddressResolver(
                    new Uri("http://test-service-http:5005"),
                    EndpointResolutionMode.ServiceDiscovery)))
            .ConfigureServices((_, services) =>
            {
                discoveryBuilder = services.AddRestHttpPreConfiguredClient<ITestHttpClient, TestHttpClient>("test-service");
                directBuilder = services.AddRestHttpPreConfiguredClient<IOtherTestHttpClient, OtherTestHttpClient>(
                    "test-service",
                    addressResolver: new FixedMicroserviceAddressResolver(new Uri("http://test-service:8080")));
            })
            .Build();

        var clientOptions = host.Services.GetRequiredService<IOptionsMonitor<HttpClientFactoryOptions>>();

        Assert.NotEmpty(clientOptions.Get(discoveryBuilder.Name).HttpMessageHandlerBuilderActions);
        Assert.Empty(clientOptions.Get(directBuilder.Name).HttpMessageHandlerBuilderActions);

        var discoveryClient = Assert.IsType<TestHttpClient>(
            host.Services.GetRequiredService<ITestHttpClient>());
        var directClient = Assert.IsType<OtherTestHttpClient>(
            host.Services.GetRequiredService<IOtherTestHttpClient>());

        Assert.Equal("http://test-service-http:5005/", discoveryClient.ConfiguredHttpClient.BaseAddress!.AbsoluteUri);
        Assert.Equal("http://test-service:8080/", directClient.ConfiguredHttpClient.BaseAddress!.AbsoluteUri);
    }

    [Fact]
    public void HttpRegistration_MultipleServiceDiscoveryClients_RegisterInfrastructureOnce()
    {
        ServiceDescriptor[] registrations = [];
        IHttpClientBuilder discoveryBuilderOne = null!;
        IHttpClientBuilder discoveryBuilderTwo = null!;

        using var host = CreateHostBuilder()
            .ConfigureServices(services => services.UseHttpMicroserviceAddressResolver(
                new FixedMicroserviceAddressResolver(
                    new Uri("http://test-service-http:5005"),
                    EndpointResolutionMode.ServiceDiscovery)))
            .ConfigureServices((_, services) =>
            {
                discoveryBuilderOne = services.AddRestHttpPreConfiguredClient<ITestHttpClient, TestHttpClient>(
                    "test-service");
                discoveryBuilderTwo = services.AddRestHttpPreConfiguredClient<IOtherTestHttpClient, OtherTestHttpClient>(
                    "test-service",
                    addressResolver: new FixedMicroserviceAddressResolver(
                        new Uri("http://test-service-http:5005"),
                        EndpointResolutionMode.ServiceDiscovery));

                registrations = [.. services];
            })
            .Build();

        Assert.Single(registrations, descriptor =>
            descriptor.ServiceType.FullName
                == "Microsoft.Extensions.ServiceDiscovery.ServiceEndpointWatcherFactory");
        Assert.Single(registrations, descriptor =>
            descriptor.ServiceType.Name == "IServiceEndpointProviderFactory"
            && descriptor.ImplementationType?.Name == "DnsServiceEndpointProviderFactory");

        var clientOptions = host.Services.GetRequiredService<IOptionsMonitor<HttpClientFactoryOptions>>();
        Assert.NotEmpty(clientOptions.Get(discoveryBuilderOne.Name).HttpMessageHandlerBuilderActions);
        Assert.NotEmpty(clientOptions.Get(discoveryBuilderTwo.Name).HttpMessageHandlerBuilderActions);
    }

    [Fact]
    public void HttpRegistration_BuiltInClusterIpResolverRegistersNoServiceDiscovery()
    {
        ServiceDescriptor[] registrations = [];
        using var host = CreateHostBuilder()
            .ConfigureServices(services => services.AddMicroserviceAddressing())
            .ConfigureServices((_, services) =>
            {
                services.AddRestHttpPreConfiguredClient<ITestHttpClient, TestHttpClient>("test-service");
                registrations = [.. services];
            })
            .Build();

        var client = Assert.IsType<TestHttpClient>(
            host.Services.GetRequiredService<ITestHttpClient>());

        Assert.Equal(
            "http://test-service.production.svc.cluster.local/",
            client.ConfiguredHttpClient.BaseAddress!.AbsoluteUri);
        Assert.DoesNotContain(registrations, descriptor =>
            descriptor.ServiceType.FullName
                == "Microsoft.Extensions.ServiceDiscovery.ServiceEndpointWatcherFactory");
    }

    [Fact]
    public void HttpRegistration_ConfigureDelegateBaseAddressGetsTrailingSlash()
    {
        // Relative RestHttpClient request paths are merged into the base address per RFC 3986:
        // a base without a trailing slash loses its last path segment on every request.
        using var host = CreateHostBuilder()
            .ConfigureServices(services => services.UseHttpMicroserviceAddressResolver(
                new FixedMicroserviceAddressResolver(new Uri("http://test-service:5005"))))
            .ConfigureServices((_, services) =>
                services.AddRestHttpPreConfiguredClient<ITestHttpClient, TestHttpClient>(
                    "test-service",
                    client => client.BaseAddress = new(client.BaseAddress!, "api/pl")))
            .Build();

        var client = Assert.IsType<TestHttpClient>(
            host.Services.GetRequiredService<ITestHttpClient>());

        Assert.Equal(new Uri("http://test-service:5005/api/pl/"), client.ConfiguredHttpClient.BaseAddress);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GrpcRegistration_UsesExplicitAddress(bool consoleClient)
    {
        GrpcClientFactoryOptions? clientOptions = null;
        var options = new GrpcClientOptions
        {
            ClientOptionsAction = actionOptions => clientOptions = actionOptions,
        };

        using var host = CreateHostBuilder()
            .ConfigureServices(services =>
            {
                if (consoleClient)
                    services.AddGrpcPreConfiguredConsoleClient<TestGrpcClient>(
                        new Uri("http://localhost:5006"), options: options);
                else
                    services.AddGrpcPreConfiguredClient<TestGrpcClient>(
                        new Uri("http://localhost:5006"), options: options);
            })
            .Build();

        Assert.NotNull(host.Services.GetRequiredService<TestGrpcClient>());

        Assert.Equal(new Uri("http://localhost:5006"), clientOptions!.Address);
    }

    [Fact]
    public void GrpcChannelCredentials_SetInsecureForNonHttpSchemeWithoutServiceDiscovery()
    {
        // grpc-dotnet rejects any address scheme other than http/https without explicit
        // credentials, so e.g. an ipv4:/// address must get Insecure credentials even in
        // Direct mode, while round-robin balancing stays a Service Discovery-only feature.
        using var host = CreateHostBuilder()
            .ConfigureServices(services =>
                services.AddGrpcPreConfiguredClient<TestGrpcClient>(
                    "test-ipv4-client", new Uri("ipv4:///10.0.0.1:5006")))
            .Build();

        var channelOptions = GetConfiguredChannelOptions(host.Services, "test-ipv4-client");

        Assert.Same(ChannelCredentials.Insecure, channelOptions.Credentials);
        Assert.Null(channelOptions.ServiceConfig);
    }

    [Fact]
    public void GrpcChannelCredentials_NotOverriddenForHttpScheme()
    {
        using var host = CreateHostBuilder()
            .ConfigureServices(services =>
                services.AddGrpcPreConfiguredClient<TestGrpcClient>(
                    "test-http-client", new Uri("http://localhost:5006")))
            .Build();

        var channelOptions = GetConfiguredChannelOptions(host.Services, "test-http-client");

        Assert.NotSame(ChannelCredentials.Insecure, channelOptions.Credentials);
    }

    static GrpcChannelOptions GetConfiguredChannelOptions(
        IServiceProvider provider,
        string clientName)
    {
        var factoryOptions = provider
            .GetRequiredService<IOptionsMonitor<GrpcClientFactoryOptions>>()
            .Get(clientName);

        var channelOptions = new GrpcChannelOptions();
        foreach (var configureChannel in factoryOptions.ChannelOptionsActions)
            configureChannel(channelOptions);

        return channelOptions;
    }

    [Fact]
    public void RestRegistration_UsesExplicitAddress()
    {
        var hostBuilder = CreateHostBuilder()
            .ConfigureServices((_, services) => services.AddRestHttpPreConfiguredClient<ITestHttpClient, TestHttpClient>(
                new Uri("http://test-service:5005"),
                client => client.DefaultRequestHeaders.Add("x-test", "value")));

        using var host = hostBuilder.Build();
        var client = Assert.IsType<TestHttpClient>(host.Services.GetRequiredService<ITestHttpClient>());

        Assert.Equal(
            new Uri("http://test-service:5005/"),
            client.ConfiguredHttpClient.BaseAddress);
        Assert.Equal(
            "value",
            client.ConfiguredHttpClient.DefaultRequestHeaders.GetValues("x-test").Single());
    }

    [Fact]
    public void ClientRegistrations_RejectRelativeAddress()
    {
        var relativeAddress = new Uri("test-service/path", UriKind.Relative);

        Assert.Throws<ArgumentException>(
            () => new ServiceCollection().AddGrpcPreConfiguredClient<TestGrpcClient>(relativeAddress));
        Assert.Throws<ArgumentException>(
            () => new ServiceCollection().AddGrpcPreConfiguredConsoleClient<TestGrpcClient>(relativeAddress));
        Assert.Throws<ArgumentException>(
            () => new ServiceCollection().AddRestHttpPreConfiguredClient<ITestHttpClient, TestHttpClient>(relativeAddress));
    }

    [Fact]
    public void AddMonqMicroserviceAddressing_SelectsClusterIpForHttpAndDiscoveryForGrpc()
    {
        using var host = CreateHostBuilder()
            .ConfigureServices(services => services.AddMonqMicroserviceAddressing())
            .Build();

        var provider = host.Services.GetRequiredService<IMicroserviceAddressProvider>();

        var http = provider.Resolve("orders-service", MicroserviceProtocol.Http);
        var grpc = provider.Resolve("orders-service", MicroserviceProtocol.Grpc);

        Assert.Equal(EndpointResolutionMode.Direct, http.ResolutionMode);
        Assert.Equal("http://orders-service.production.svc.cluster.local/", http.Address.AbsoluteUri);
        Assert.Equal(EndpointResolutionMode.ServiceDiscovery, grpc.ResolutionMode);
        Assert.Equal("dns:///orders-service-grpc.production.svc.cluster.local:5006", grpc.Address.AbsoluteUri);
    }

    static IHostBuilder CreateHostBuilder() =>
        new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddHttpContextAccessor();
                services.AddLogging();
                services.AddSingleton(new RestHttpClientOptions());
            });

    sealed class TestGrpcClient : ClientBase<TestGrpcClient>
    {
        public TestGrpcClient(CallInvoker callInvoker)
            : base(callInvoker)
        {
        }

        TestGrpcClient(ClientBaseConfiguration configuration)
            : base(configuration)
        {
        }

        protected override TestGrpcClient NewInstance(ClientBaseConfiguration configuration) =>
            new(configuration);
    }

    sealed class SecondTestGrpcClient : ClientBase<SecondTestGrpcClient>
    {
        public SecondTestGrpcClient(CallInvoker callInvoker)
            : base(callInvoker)
        {
        }

        SecondTestGrpcClient(ClientBaseConfiguration configuration)
            : base(configuration)
        {
        }

        protected override SecondTestGrpcClient NewInstance(ClientBaseConfiguration configuration) =>
            new(configuration);
    }

    sealed class ThrowingAddressResolver : IMicroserviceAddressResolver
    {
        public ResolvedMicroserviceAddress Resolve(MicroserviceAddressContext context) =>
            throw new InvalidOperationException("Resolver failed.");
    }

    interface ITestHttpClient
    {
    }

    interface IOtherTestHttpClient
    {
    }

    sealed class TestHttpClient : RestHttpClient, ITestHttpClient
    {
        public TestHttpClient(
            HttpClient httpClient,
            ILoggerFactory loggerFactory,
            RestHttpClientOptions configuration,
            IHttpContextAccessor httpContextAccessor)
            : base(httpClient, loggerFactory, configuration, httpContextAccessor)
        {
            ConfiguredHttpClient = httpClient;
        }

        internal HttpClient ConfiguredHttpClient { get; }
    }

    sealed class OtherTestHttpClient : RestHttpClient, IOtherTestHttpClient
    {
        public OtherTestHttpClient(
            HttpClient httpClient,
            ILoggerFactory loggerFactory,
            RestHttpClientOptions configuration,
            IHttpContextAccessor httpContextAccessor)
            : base(httpClient, loggerFactory, configuration, httpContextAccessor)
        {
            ConfiguredHttpClient = httpClient;
        }

        internal HttpClient ConfiguredHttpClient { get; }
    }
}
