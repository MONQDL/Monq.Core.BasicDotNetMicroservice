using Monq.Core.BasicDotNetMicroservice.Sinks.Loki.Labels;
using Serilog;
using Serilog.Configuration;
using Serilog.Formatting.Display;
using Serilog.Sinks.Http;
using System;

namespace Monq.Core.BasicDotNetMicroservice.Sinks.Loki;

public static class LokiSinkExtensions
{
    internal const string DefaultTemplate = "{Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Adds a non-durable sink that sends log events using HTTP POST to the Loki. A
    /// non-durable sink will lose data after a system or process restart.
    /// </summary>
    /// <param name="serilogConfig">The logger configuration.</param>
    /// <param name="lokiUrl">The Loki url.</param>
    /// <param name="lokiUsername">The Loki user, if authentication required.</param>
    /// <param name="lokiPassword">The Loki password, if authentication required.</param>
    /// <param name="httpClient">If not null, then the custom HttpClient wil be used to send requests.</param>
    /// <param name="formatProvider"></param>
    /// <param name="logLabelProvider"></param>
    /// <param name="outputTemplate"></param>
    /// <param name="period">The time to wait between checking for event batches. Default value is 2 seconds.</param>
    /// <param name="logEventsInBatchLimit">
    /// The maximum number of events to post in a single batch. Default value is 1000.
    /// </param>
    /// <param name="queueLimitBytes">
    /// The maximum size, in bytes, of events stored in memory, waiting to be sent over the
    /// network. Specify <see langword="null"/> for no limit.
    /// </param>
    /// <returns></returns>
    public static LoggerConfiguration LokiHttp(this LoggerSinkConfiguration serilogConfig,
        string lokiUrl,
        string? lokiUsername = default,
        string? lokiPassword = default,
        IHttpClient? httpClient = default,
        IFormatProvider? formatProvider = default,
        ILogLabelProvider? logLabelProvider = default,
        string outputTemplate = DefaultTemplate,
        TimeSpan? period = default,
        int logEventsInBatchLimit = 1000,
        long? queueLimitBytes = default
        )
    {
        period ??= TimeSpan.FromSeconds(2);

        LokiCredentialsBase credentials = !string.IsNullOrWhiteSpace(lokiUsername) && !string.IsNullOrWhiteSpace(lokiPassword)
            ? new BasicAuthCredentials(lokiUrl, lokiUsername, lokiPassword)
            : new NoAuthCredentials(lokiUrl);

        var labelProvider = logLabelProvider ?? new DefaultLogLabelProvider();
        var batchFormatter = new LokiBatchFormatter(labelProvider);
        var textFormatter = new LokiTextFormatter(labelProvider, 
            new MessageTemplateTextFormatter(outputTemplate, formatProvider));

        httpClient ??= new DefaultLokiHttpClient();

        if (httpClient is LokiHttpClientBase c)
            c.SetAuthCredentials(credentials);

        return serilogConfig.Http(credentials.Url,
            queueLimitBytes: queueLimitBytes,
            batchFormatter: batchFormatter,
            textFormatter: textFormatter,
            httpClient: httpClient,
            period: period,
            logEventsInBatchLimit: logEventsInBatchLimit);
    }
}
