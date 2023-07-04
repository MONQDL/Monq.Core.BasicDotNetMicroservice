using Microsoft.Extensions.Configuration;
using Monq.Core.BasicDotNetMicroservice.Sinks.Loki;
using Serilog.Sinks.Http;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Monq.Core.BasicDotNetMicroservice.Sinks.Loki
{
    public abstract class LokiHttpClientBase : IHttpClient
    {
        protected readonly HttpClient HttpClient;

        protected LokiHttpClientBase(HttpClient? httpClient = null)
        {
            HttpClient = httpClient ?? new HttpClient();
        }

        public virtual void Configure(IConfiguration configuration) { }

        public virtual void Dispose() => HttpClient?.Dispose();

        public void SetAuthCredentials(LokiCredentialsBase credentials)
        {
            if (!(credentials is BasicAuthCredentials c))
                return;

            var headers = HttpClient.DefaultRequestHeaders;
            if (headers.Any(x => x.Key == "Authorization"))
                return;

            var token = Base64Encode($"{c.Username}:{c.Password}");
            headers.Add("Authorization", $"Basic {token}");
        }

        static string Base64Encode(string plainText) =>
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plainText));

        public abstract Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content);
    }
}