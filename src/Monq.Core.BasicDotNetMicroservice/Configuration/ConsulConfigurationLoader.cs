using Microsoft.Extensions.Configuration;
using System.Net;

namespace Monq.Core.BasicDotNetMicroservice.Configuration;

/// <summary>
/// Loads configuration from Consul KV store using native HTTP requests.
/// </summary>
internal static class ConsulConfigurationLoader
{
    /// <summary>
    /// Loads a JSON configuration value from Consul KV store and adds it to the configuration builder.
    /// </summary>
    /// <param name="configBuilder">The configuration builder to add the configuration to.</param>
    /// <param name="key">The Consul KV key path (e.g., "production/my-service/appsettings.json").</param>
    /// <param name="consulOptions">Consul connection options.</param>
    public static void AddConsulKeyValue(
        this IConfigurationBuilder configBuilder,
        string key,
        ConsulClientBindOptions consulOptions)
    {
        if (consulOptions.Address == null)
            throw new ConsulConfigurationException("Consul address is not configured.");

        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        var uri = BuildConsulUri(consulOptions.Address, key, consulOptions.Datacenter);
        var request = new HttpRequestMessage(HttpMethod.Get, uri);

        if (!string.IsNullOrEmpty(consulOptions.Token))
            request.Headers.Add("X-Consul-Token", consulOptions.Token);

        HttpResponseMessage response;
        try
        {
            response = httpClient.Send(request);
        }
        catch (HttpRequestException ex)
        {
            throw new ConsulConfigurationException(
                $"Failed to connect to Consul. Key: '{key}', Address: '{consulOptions.Address}'.", ex);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new ConsulConfigurationException(
                $"Configuration key not found in Consul: '{key}'.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            throw new ConsulConfigurationException(
                $"Failed to load configuration from Consul. Key: '{key}', Status: {(int)response.StatusCode} {response.ReasonPhrase}, Body: {body}");
        }

        using var stream = response.Content.ReadAsStream();
        configBuilder.AddJsonStream(stream);
    }

    static Uri BuildConsulUri(Uri consulAddress, string key, string? datacenter)
    {
        var query = "raw";
        if (!string.IsNullOrEmpty(datacenter))
            query += $"&dc={Uri.EscapeDataString(datacenter)}";

        var path = $"/v1/kv/{key.TrimStart('/')}";

        var uriBuilder = new UriBuilder(consulAddress)
        {
            Path = path,
            Query = query
        };

        return uriBuilder.Uri;
    }
}
