using Monq.Core.BasicDotNetMicroservice.Sinks.Loki.Labels;
using Serilog;
using Serilog.Configuration;
using Serilog.Formatting.Display;
using Serilog.Sinks.Http;
using System;

namespace Monq.Core.BasicDotNetMicroservice.Sinks.Loki
{
    public static class LokiSinkExtensions
    {
        internal const string DefaultTemplate = "{Message:lj}{NewLine}{Exception}";

        public static LoggerConfiguration LokiHttp(this LoggerSinkConfiguration serilogConfig,
            string lokiUrl,
            string? lokiUsername = default,
            string? lokiPassword = default,
            IHttpClient? httpClient = default,
            IFormatProvider? formatProvider = default,
            ILogLabelProvider? logLabelProvider = default,
            string outputTemplate = DefaultTemplate,
            TimeSpan? period = default)
        {
            period ??= TimeSpan.FromSeconds(1);

            LokiCredentialsBase credentials = !string.IsNullOrWhiteSpace(lokiUsername) && !string.IsNullOrWhiteSpace(lokiPassword)
                ? new BasicAuthCredentials(lokiUrl, lokiUsername, lokiPassword)
                : new NoAuthCredentials(lokiUrl);

            var formatter = new LokiBatchFormatter(logLabelProvider ?? new DefaultLogLabelProvider());

            httpClient ??= new DefaultLokiHttpClient();

            if (httpClient is LokiHttpClientBase c)
                c.SetAuthCredentials(credentials);

            return serilogConfig.Http(LokiRouteBuilder.BuildPostUri(credentials.Url),
                batchFormatter: formatter,
                textFormatter: new MessageTemplateTextFormatter(outputTemplate, formatProvider),
                httpClient: httpClient,
                period: period);
        }
    }
}
