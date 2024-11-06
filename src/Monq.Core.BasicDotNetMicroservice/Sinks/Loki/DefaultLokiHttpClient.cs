using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Monq.Core.BasicDotNetMicroservice.Sinks.Loki;

public class DefaultLokiHttpClient : LokiHttpClientBase
{
    public DefaultLokiHttpClient(HttpClient? httpClient = null)
        : base(httpClient)
    { }

    public override async Task<HttpResponseMessage> PostAsync(string requestUri, Stream contentStream, CancellationToken cancellationToken)
    {
        var content = new StreamContent(contentStream);

        content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
        return await HttpClient.PostAsync(requestUri, content).ConfigureAwait(false);
    }
}
