using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Balancer;
using Grpc.Net.ClientFactory;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monq.Core.BasicDotNetMicroservice.Configuration;
using Monq.Core.BasicDotNetMicroservice.Extensions;
using Monq.Core.GrpcClientExtensions;
using Monq.Core.HttpClientExtensions;
using Monq.Core.MicroserviceAddressing.Addressing;
using Monq.Core.MicroserviceAddressing.Hosting;
using Xunit;

namespace Monq.Core.BasicDotNetMicroservice.Tests;

public class MicroserviceClientRegistrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GrpcHostRegistration_DoesNotRequireBaseUri(bool consoleClient)
    {
        var configureClientInvoked = false;
        var hostBuilder = CreateHostBuilder()
            .UseMicroserviceAddressResolver(
                new FixedMicroserviceAddressResolver(new Uri("http://localhost:5006")));

        if (consoleClient)
            hostBuilder.AddGrpcPreConfiguredConsoleClient<TestGrpcClient>(
                "test-service",
                options => options.ClientOptionsAction = _ => configureClientInvoked = true);
        else
            hostBuilder.AddGrpcPreConfiguredClient<TestGrpcClient>(
                "test-service",
                options => options.ClientOptionsAction = _ => configureClientInvoked = true);

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
            .UseGrpcMicroserviceAddressResolver(
                new KubernetesServiceDiscoveryAddressResolver())
            .AddGrpcPreConfiguredClient<TestGrpcClient>("first-service")
            .AddGrpcPreConfiguredClient<SecondTestGrpcClient>("second-service");

        using var host = hostBuilder.Build();
        var resolverFactories = host.Services.GetServices<ResolverFactory>().ToArray();

        Assert.Contains(staticResolver, resolverFactories);
        Assert.Single(resolverFactories, factory => factory.Name == "dns");
    }

    [Fact]
    public void GrpcServiceDiscovery_EnabledForServiceDiscoveryModeWithoutDnsScheme()
    {
        var hostBuilder = CreateHostBuilder()
            .UseGrpcMicroserviceAddressResolver(
                new FixedMicroserviceAddressResolver(
                    new Uri("http://test-service:5006"),
                    EndpointResolutionMode.ServiceDiscovery))
            .AddGrpcPreConfiguredClient<TestGrpcClient>("test-service");

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
            .ConfigureGrpcServiceDiscovery(options =>
                options.DnsRefreshInterval = refreshInterval)
            .UseGrpcMicroserviceAddressResolver(
                new KubernetesServiceDiscoveryAddressResolver())
            .AddGrpcPreConfiguredClient<TestGrpcClient>("test-service");

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
    [InlineData(EndpointResolutionMode.Direct, false)]
    [InlineData(EndpointResolutionMode.ServiceDiscovery, true)]
    public void HttpHostRegistration_UsesResolvedAddressAndMode(
        EndpointResolutionMode resolutionMode,
        bool serviceDiscoveryExpected)
    {
        var address = new Uri("http://test-service:5005");
        ServiceDescriptor[] registrations = [];
        var hostBuilder = CreateHostBuilder()
            .UseHttpMicroserviceAddressResolver(
                new FixedMicroserviceAddressResolver(address, resolutionMode))
            .AddRestHttpPreConfiguredClient<ITestHttpClient, TestHttpClient>(
                "test-service",
                client => client.DefaultRequestHeaders.Add("x-test", "value"))
            .ConfigureServices((_, services) => registrations = [.. services]);

        using var host = hostBuilder.Build();
        var client = Assert.IsType<TestHttpClient>(
            host.Services.GetRequiredService<ITestHttpClient>());

        Assert.Equal(new Uri("http://test-service:5005/"), client.ConfiguredHttpClient.BaseAddress);
        Assert.Equal(
            "value",
            client.ConfiguredHttpClient.DefaultRequestHeaders.GetValues("x-test").Single());
        Assert.Equal(
            serviceDiscoveryExpected,
            registrations.Any(descriptor =>
                descriptor.ServiceType.FullName
                    == "Microsoft.Extensions.ServiceDiscovery.ServiceEndpointWatcherFactory"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GrpcHostRegistration_UsesExplicitAddress(bool consoleClient)
    {
        GrpcClientFactoryOptions? clientOptions = null;
        var hostBuilder = CreateHostBuilder();

        if (consoleClient)
            hostBuilder.AddGrpcPreConfiguredConsoleClient<TestGrpcClient>(
                new Uri("http://localhost:5006"),
                options => options.ClientOptionsAction = actionOptions => clientOptions = actionOptions);
        else
            hostBuilder.AddGrpcPreConfiguredClient<TestGrpcClient>(
                new Uri("http://localhost:5006"),
                options => options.ClientOptionsAction = actionOptions => clientOptions = actionOptions);

        using var host = hostBuilder.Build();
        Assert.NotNull(host.Services.GetRequiredService<TestGrpcClient>());

        Assert.Equal(new Uri("http://localhost:5006"), clientOptions!.Address);
    }

    [Fact]
    public void GrpcChannelCredentials_SetInsecureForNonHttpSchemeWithoutServiceDiscovery()
    {
        // grpc-dotnet rejects any address scheme other than http/https without explicit
        // credentials, so e.g. an ipv4:/// address must get Insecure credentials even in
        // Direct mode, while round-robin balancing stays a Service Discovery-only feature.
        var hostBuilder = CreateHostBuilder()
            .AddGrpcPreConfiguredClient<TestGrpcClient>(
                new Uri("ipv4:///10.0.0.1:5006"),
                options => options.Name = "test-ipv4-client");

        using var host = hostBuilder.Build();
        var channelOptions = GetConfiguredChannelOptions(host.Services, "test-ipv4-client");

        Assert.Same(ChannelCredentials.Insecure, channelOptions.Credentials);
        Assert.Null(channelOptions.ServiceConfig);
    }

    [Fact]
    public void GrpcChannelCredentials_NotOverriddenForHttpScheme()
    {
        var hostBuilder = CreateHostBuilder()
            .AddGrpcPreConfiguredClient<TestGrpcClient>(
                new Uri("http://localhost:5006"),
                options => options.Name = "test-http-client");

        using var host = hostBuilder.Build();
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
    public void RestHostRegistration_UsesExplicitAddress()
    {
        var hostBuilder = CreateHostBuilder()
            .AddRestHttpPreConfiguredClient<ITestHttpClient, TestHttpClient>(
                new Uri("http://test-service:5005"),
                client => client.DefaultRequestHeaders.Add("x-test", "value"));

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
    public void ClientHostRegistrations_RejectRelativeAddress()
    {
        var hostBuilder = CreateHostBuilder();
        var address = new Uri("test-service/path", UriKind.Relative);

        Assert.Throws<ArgumentException>(
            () => hostBuilder.AddGrpcPreConfiguredClient<TestGrpcClient>(address));
        Assert.Throws<ArgumentException>(
            () => hostBuilder.AddGrpcPreConfiguredConsoleClient<TestGrpcClient>(address));
        Assert.Throws<ArgumentException>(
            () => hostBuilder.AddRestHttpPreConfiguredClient<ITestHttpClient, TestHttpClient>(address));
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

    interface ITestHttpClient
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
}
