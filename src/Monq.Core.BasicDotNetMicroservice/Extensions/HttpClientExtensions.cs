using System.Linq;
using System.Net.Http;

namespace Monq.Core.BasicDotNetMicroservice.Extensions;

/// <summary>
/// <see cref="HttpClient"/> extensions.
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Add trainling slash to <see cref="HttpClient.BaseAddress"/> if needed.
    /// </summary>
    /// <param name="httpClient"></param>
    public static void AddTrailingSlash(this HttpClient httpClient)
    {
        if (httpClient.BaseAddress == null || httpClient.BaseAddress.AbsoluteUri.Last() == '/')
            return;
        httpClient.BaseAddress = new($"{httpClient.BaseAddress}/");
    }
}
