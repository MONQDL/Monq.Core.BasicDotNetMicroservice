using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Monq.Core.BasicDotNetMicroservice.Addressing;
using Monq.Core.BasicDotNetMicroservice.Extensions;
using Moq;
using Xunit;

namespace Monq.Core.BasicDotNetMicroservice.Tests;

public class MicroserviceAddressResolverTests
{
    [Theory]
    [InlineData(MicroserviceProtocol.Grpc)]
    [InlineData(MicroserviceProtocol.Http)]
    public void FixedResolver_UsesSharedAddress(MicroserviceProtocol protocol)
    {
        var context = CreateContext(Environments.Development, protocol);
        var resolver = new FixedMicroserviceAddressResolver(new Uri("https://api.example.com"));

        var result = resolver.Resolve(context);

        Assert.Equal(new Uri("https://api.example.com"), result.Address);
        Assert.Equal(EndpointResolutionMode.Direct, result.ResolutionMode);
    }

    [Theory]
    [InlineData(
        MicroserviceProtocol.Grpc,
        "http://test-service.integration.svc.cluster.local:5006")]
    [InlineData(
        MicroserviceProtocol.Http,
        "http://test-service.integration.svc.cluster.local:80")]
    public void ClusterIpResolver_UsesProtocolDefaultPort(
        MicroserviceProtocol protocol,
        string expectedAddress)
    {
        var resolver = new KubernetesClusterIpAddressResolver();

        var result = resolver.Resolve(CreateContext("Integration", protocol));

        Assert.Equal(new Uri(expectedAddress), result.Address);
        Assert.Equal(EndpointResolutionMode.Direct, result.ResolutionMode);
    }

    [Theory]
    [InlineData(
        MicroserviceProtocol.Grpc,
        "dns:///test-service-grpc.integration.svc.cluster.local:5006")]
    [InlineData(
        MicroserviceProtocol.Http,
        "http://test-service-http.integration.svc.cluster.local:5005")]
    public void ServiceDiscoveryResolver_UsesProtocolDefaultPort(
        MicroserviceProtocol protocol,
        string expectedAddress)
    {
        var resolver = new KubernetesServiceDiscoveryAddressResolver();

        var result = resolver.Resolve(CreateContext("Integration", protocol));

        Assert.Equal(new Uri(expectedAddress), result.Address);
        Assert.Equal(EndpointResolutionMode.ServiceDiscovery, result.ResolutionMode);
    }

    [Fact]
    public void Resolver_UsesPortOverride()
    {
        var resolver = new KubernetesServiceDiscoveryAddressResolver();
        var context = CreateContext("Integration", MicroserviceProtocol.Http, 8080);

        var result = resolver.Resolve(context);

        Assert.Equal(
            new Uri("http://test-service-http.integration.svc.cluster.local:8080"),
            result.Address);
    }

    [Theory]
    [InlineData(MicroserviceProtocol.Grpc)]
    [InlineData(MicroserviceProtocol.Http)]
    public void ClientResolver_OverridesProtocolAndDefaultResolvers(MicroserviceProtocol protocol)
    {
        var hostBuilder = new HostBuilder()
            .UseMicroserviceAddressResolver(
                new FixedMicroserviceAddressResolver(new Uri("https://default.example")))
            .UseGrpcMicroserviceAddressResolver(
                new FixedMicroserviceAddressResolver(new Uri("https://grpc.example")));
        var context = CreateHostBuilderContext("Integration");

        var result = hostBuilder.ResolveMicroserviceAddress(
            context,
            "test-service",
            protocol,
            addressResolver: new FixedMicroserviceAddressResolver(
                new Uri("https://client.example")));

        Assert.Equal(new Uri("https://client.example"), result.Address);
    }

    [Theory]
    [InlineData(MicroserviceProtocol.Grpc, "https://grpc.example")]
    [InlineData(MicroserviceProtocol.Http, "https://http.example")]
    public void ProtocolResolver_OverridesCommonResolver(
        MicroserviceProtocol protocol,
        string expectedAddress)
    {
        var hostBuilder = new HostBuilder()
            .UseMicroserviceAddressResolver(
                new FixedMicroserviceAddressResolver(new Uri("https://default.example")))
            .UseGrpcMicroserviceAddressResolver(
                new FixedMicroserviceAddressResolver(new Uri("https://grpc.example")))
            .UseHttpMicroserviceAddressResolver(
                new FixedMicroserviceAddressResolver(new Uri("https://http.example")));

        var result = hostBuilder.ResolveMicroserviceAddress(
            CreateHostBuilderContext("Integration"),
            "test-service",
            protocol);

        Assert.Equal(new Uri(expectedAddress), result.Address);
    }

    static MicroserviceAddressContext CreateContext(
        string environmentName,
        MicroserviceProtocol protocol,
        int? portOverride = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [nameof(AppConfiguration.BaseUri)] = "https://api.example.com",
            })
            .Build();
        var environment = Mock.Of<IHostEnvironment>(value => value.EnvironmentName == environmentName);

        return new(configuration, environment, "test-service", protocol, portOverride);
    }

    static HostBuilderContext CreateHostBuilderContext(string environmentName)
    {
        var addressContext = CreateContext(environmentName, MicroserviceProtocol.Grpc);
        return new(new Dictionary<object, object>())
        {
            Configuration = addressContext.Configuration,
            HostingEnvironment = addressContext.Environment,
        };
    }
}
