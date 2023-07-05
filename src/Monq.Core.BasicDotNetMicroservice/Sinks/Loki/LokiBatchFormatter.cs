using Monq.Core.BasicDotNetMicroservice.Sinks.Loki.Labels;
using Monq.Core.BasicDotNetMicroservice.Sinks.Loki.Models;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Monq.Core.BasicDotNetMicroservice.Sinks.Loki
{
    internal class LokiBatchFormatter : IBatchFormatter
    {
        static readonly Regex ValueWithoutSpaces = new Regex("^\"(\\S+)\"$", RegexOptions.Compiled);

        public ILogLabelProvider LogLabelProvider { get; }

        public LokiBatchFormatter()
        {
            LogLabelProvider = new DefaultLogLabelProvider();
        }

        public LokiBatchFormatter(ILogLabelProvider logLabelProvider)
        {
            LogLabelProvider = logLabelProvider;
        }

        public void Format(IEnumerable<LogEvent> logEvents, ITextFormatter formatter, TextWriter output)
        {
            if (logEvents == null)
                throw new ArgumentNullException(nameof(logEvents));
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            List<LogEvent> logs = logEvents.OrderBy(le => le.Timestamp).ToList();
            if (!logs.Any())
                return;

            var streamsDictionary = new Dictionary<string, LokiContentStream>();
            foreach (LogEvent logEvent in logs)
            {
                var labels = new List<LokiLabel>();

                foreach (LokiLabel globalLabel in LogLabelProvider.GetLabels())
                    labels.Add(new LokiLabel(globalLabel.Key, globalLabel.Value));

                var sb = new StringBuilder();
                using (var tw = new StringWriter(sb))
                {
                    formatter.Format(logEvent, tw);
                }

                HandleProperty("level", GetLevel(logEvent.Level), labels, sb);
                foreach (KeyValuePair<string, LogEventPropertyValue> property in logEvent.Properties)
                {
                    HandleProperty(property.Key, property.Value.ToString(), labels, sb);
                }

                // Order the labels so they always get the same chunk in loki
                labels = labels.OrderBy(l => l.Key).ToList();
                var key = string.Join(",", labels.Select(l => $"{l.Key}={l.Value}"));
                if (!streamsDictionary.TryGetValue(key, out var stream))
                {
                    streamsDictionary.Add(key, stream = new LokiContentStream());

                    foreach (var label in labels)
                    {
                        stream.AddLabel(label.Key, label.Value);
                    }
                }

                // Loki doesn't like \r\n for new line, and we can't guarantee the message doesn't have any
                // in it, so we replace \r\n with \n on the final message
                stream.AddEntry(logEvent.Timestamp, sb.ToString().Replace("\r\n", "\n"));
            }

            if (streamsDictionary.Count > 0)
            {
                var content = new LokiContent
                {
                    Streams = streamsDictionary.Values.ToList()
                };
                output.Write(content.Serialize());
            }
        }

        public void Format(IEnumerable<string> logEvents, TextWriter output)
        {
            throw new NotImplementedException();
        }

        void HandleProperty(string name, string value, ICollection<LokiLabel> labels, StringBuilder sb)
        {
            // Some enrichers pass strings with quotes surrounding the values inside the string,
            // which results in redundant quotes after serialization and a "bad request" response.
            // To avoid this, remove all quotes from the value.
            // We also remove any \r\n newlines and replace with \n new lines to prevent "bad request" responses
            // We also remove backslashes and replace with forward slashes, Loki doesn't like those either
            value = value.Replace("\r\n", "\n");

            switch (DetermineHandleActionForProperty(name))
            {
                case HandleAction.Discard: return;
                case HandleAction.SendAsLabel:
                    value = value.Replace("\"", "").Replace("\\", "/");
                    labels.Add(new LokiLabel(name, value));
                    break;
                case HandleAction.AppendToMessage:
                    value = SimplifyValue(value);
                    sb.Append($" {name}={value}");
                    break;
            }
        }

        static string SimplifyValue(string value)
        {
            try
            {
                var match = ValueWithoutSpaces.Match(value);
                return match.Success
                    ? Regex.Unescape(match.Groups[1].Value)
                    : value;
            }
            catch
            {
                return value;
            }
        }

        static string GetLevel(LogEventLevel level)
        {
            return level switch
            {
                LogEventLevel.Verbose => "trace",
                LogEventLevel.Debug => "debug",
                LogEventLevel.Information => "info",
                LogEventLevel.Warning => "warning",
                LogEventLevel.Error => "error",
                LogEventLevel.Fatal => "critical",
                _ => "unknown"
            };
        }

        HandleAction DetermineHandleActionForProperty(string propertyName)
        {
            var provider = LogLabelProvider;
            switch (provider.FormatterStrategy)
            {
                case LokiFormatterStrategy.AllPropertiesAsLabels:
                    return HandleAction.SendAsLabel;

                case LokiFormatterStrategy.SpecificPropertiesAsLabelsAndRestDiscarded:
                    return provider.PropertiesAsLabels.Contains(propertyName)
                        ? HandleAction.SendAsLabel
                        : HandleAction.Discard;

                case LokiFormatterStrategy.SpecificPropertiesAsLabelsAndRestAppended:
                    return provider.PropertiesAsLabels.Contains(propertyName)
                        ? HandleAction.SendAsLabel
                        : HandleAction.AppendToMessage;

                //case LokiFormatterStrategy.SpecificPropertiesAsLabelsOrAppended:
                default:
                    return provider.PropertiesAsLabels.Contains(propertyName)
                        ? HandleAction.SendAsLabel
                        : provider.PropertiesToAppend.Contains(propertyName)
                            ? HandleAction.AppendToMessage
                            : HandleAction.Discard;
            }
        }

        enum HandleAction
        {
            Discard,
            SendAsLabel,
            AppendToMessage
        }
    }
}
