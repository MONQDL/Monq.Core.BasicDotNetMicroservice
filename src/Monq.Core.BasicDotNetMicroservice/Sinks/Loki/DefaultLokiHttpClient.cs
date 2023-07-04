using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Monq.Core.BasicDotNetMicroservice.Sinks.Loki
{
    public class DefaultLokiHttpClient : LokiHttpClientBase
    {
        public DefaultLokiHttpClient(HttpClient? httpClient = null)
            : base(httpClient)
        { }

        public override async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
        {
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            return await HttpClient.PostAsync(requestUri, content).ConfigureAwait(false);
        }
    }
}
