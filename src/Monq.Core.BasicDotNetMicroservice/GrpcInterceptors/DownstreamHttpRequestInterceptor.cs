using Grpc.Core;
using Grpc.Core.Interceptors;
using System.Net;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Monq.Core.BasicDotNetMicroservice.GrpcInterceptors
{
    /// <summary>
    /// The interceptor converts <see cref="Monq.Core.HttpClientExtensions.Exceptions.ResponseException"/> 
    /// status codes to the RpcExeption with preconfigured status codes.
    /// </summary>
    public class DownstreamHttpRequestInterceptor : Interceptor
    {
        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                return await continuation(request, context);
            }
            catch (Monq.Core.HttpClientExtensions.Exceptions.ResponseException e)
            {
                StatusCode statusCode = e.StatusCode switch
                {
                    HttpStatusCode.BadRequest => StatusCode.FailedPrecondition,
                    HttpStatusCode.NotFound => StatusCode.NotFound,
                    HttpStatusCode.Forbidden => StatusCode.PermissionDenied,
                    HttpStatusCode.FailedDependency => StatusCode.FailedPrecondition,
                    HttpStatusCode.BadGateway => StatusCode.Unavailable,
                    HttpStatusCode.ServiceUnavailable => StatusCode.Unavailable,
                    HttpStatusCode.InternalServerError => StatusCode.Unknown,
                    HttpStatusCode.GatewayTimeout => StatusCode.DeadlineExceeded,
                    HttpStatusCode.RequestTimeout => StatusCode.DeadlineExceeded,
                    _ => StatusCode.Unknown
                };

                JsonNode? internalMessage = null;
                try
                {
                    internalMessage = JsonNode.Parse(e.ResponseData ?? "{}",
                        new JsonNodeOptions { PropertyNameCaseInsensitive = true });
                }
                catch
                {
                    // Just doing nothing with json convert error.
                    // Such a situation may arise when the response doesn't contain serialized json message.
                }

                throw new RpcException(new Status(statusCode, internalMessage?.ToString() ?? e.Message),
                    "Downstream request failed.");
            }
        }
    }
}
