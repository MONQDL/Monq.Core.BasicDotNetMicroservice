using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQCoreClient;
using RabbitMQCoreClient.Models;

namespace Monq.Core.BasicDotNetMicroservice.ConsoleApp.Handlers;

public class OtelTestMessageHandler : IMessageHandler
{
    readonly ILogger<OtelTestMessageHandler> _logger;

    public OtelTestMessageHandler(ILogger<OtelTestMessageHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleMessage(ReadOnlyMemory<byte> message, RabbitMessageEventArgs args, MessageHandlerContext context)
    {
        var body = Encoding.UTF8.GetString(message.Span);
        _logger.LogInformation("Received RabbitMQ message: {Message}, RoutingKey: {RoutingKey}", body, args.RoutingKey);
        return Task.CompletedTask;
    }
}
