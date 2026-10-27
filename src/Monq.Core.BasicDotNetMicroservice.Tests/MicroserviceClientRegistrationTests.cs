using Grpc.Core;
using Grpc.Net.Client.Balancer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monq.Core.BasicDotNetMicroservice.Addressing;
using Monq.Core.BasicDotNetMicroservice.Configuration;
using Monq.Core.BasicDotNetMicroservice.Extensions;
using Monq.Core.HttpClientExtensions;
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
