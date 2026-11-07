using Duende.AspNetCore.Authentication.OAuth2Introspection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Monq.Core.BasicDotNetMicroservice.Extensions;
using Monq.Core.MicroserviceAddressing.Addressing;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace Monq.Core.BasicDotNetMicroservice.Tests;

public class MonqAuthenticationOptionsTests
{
    static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.ConfigureMonqAuthentication(
            configuration,
            new Uri("https://identity.example.com"));

        return services.BuildServiceProvider();
    }

    OAuth2IntrospectionOptions GetOptions(Dictionary<string, string?> settings)
    {
        using var provider = BuildProvider(settings);
        return provider
            .GetRequiredService<IOptionsMonitor<OAuth2IntrospectionOptions>>()
            .Get("Bearer");
    }

    [Fact(DisplayName = "Use default introspection cache duration.")]
    public void ShouldUseDefaultCacheDuration()
    {
        var options = GetOptions(new Dictionary<string, string?>
        {
            ["Authentication:ApiResource:Login"] = "login",
            ["Authentication:ApiResource:Password"] = "password"
        });

        Assert.Equal(TimeSpan.FromSeconds(300), options.CacheDuration);
    }

    [Fact(DisplayName = "Read introspection cache duration from configuration.")]
    public void ShouldReadCacheDurationFromConfiguration()
    {
        var options = GetOptions(new Dictionary<string, string?>
        {
            ["Authentication:ApiResource:Login"] = "login",
            ["Authentication:ApiResource:Password"] = "password",
            ["Authentication:CacheDurationSeconds"] = "60"
        });

        Assert.Equal(TimeSpan.FromSeconds(60), options.CacheDuration);
    }

    [Theory(DisplayName = "Fall back to the default cache duration for invalid values.")]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("abc")]
    public void ShouldFallBackToDefaultCacheDuration(string value)
    {
        var options = GetOptions(new Dictionary<string, string?>
        {
            ["Authentication:ApiResource:Login"] = "login",
            ["Authentication:ApiResource:Password"] = "password",
            ["Authentication:CacheDurationSeconds"] = value
        });

        Assert.Equal(TimeSpan.FromSeconds(300), options.CacheDuration);
    }

    [Fact(DisplayName = "Configure authority from an explicit endpoint on the service collection.")]
    public void ShouldConfigureAuthorityFromServiceCollectionEndpoint()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.ConfigureMonqAuthentication(
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Authentication:ApiResource:Login"] = "login",
                        ["Authentication:ApiResource:Password"] = "password"
                    })
                .Build(),
            new Uri("https://identity.example.com"));

        using var provider = services.BuildServiceProvider();
        var options = provider
            .GetRequiredService<IOptionsMonitor<OAuth2IntrospectionOptions>>()
            .Get("Bearer");

        Assert.Equal("https://identity.example.com/", options.Authority);
    }

    [Fact(DisplayName = "Resolve the authority for the default authentication application.")]
    public void ShouldResolveAuthorityForDefaultAuthenticationApplication()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:ApiResource:Login"] = "login",
                    ["Authentication:ApiResource:Password"] = "password"
                })
            .Build();

        var resolver = new RecordingResolver(new Uri("https://identity.example.com"));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(Mock.Of<IHostEnvironment>());
        services.UseMicroserviceAddressResolver(resolver);
        services.ConfigureMonqAuthentication(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider
            .GetRequiredService<IOptionsMonitor<OAuth2IntrospectionOptions>>()
            .Get("Bearer");

        Assert.Equal("https://identity.example.com/", options.Authority);
        Assert.Equal(
            HostBuilderExtensions.DefaultAuthenticationApplicationName,
            resolver.ApplicationName);
    }

    [Fact(DisplayName = "Reject a relative authentication endpoint on the service collection.")]
    public void ShouldRejectRelativeAuthenticationEndpoint()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(
            () => services.ConfigureMonqAuthentication(
                new ConfigurationBuilder().Build(),
                new Uri("identity.example.com", UriKind.Relative)));
    }

    sealed class RecordingResolver(Uri address) : IMicroserviceAddressResolver
    {
        public string? ApplicationName { get; private set; }

        public ResolvedMicroserviceAddress Resolve(MicroserviceAddressContext context)
        {
            ApplicationName = context.TargetApplicationName;
            return new ResolvedMicroserviceAddress(address);
        }
    }
}
