using Monq.Core.BasicDotNetMicroservice.Sinks.Loki.Labels;
using Monq.Core.BasicDotNetMicroservice.Sinks.Loki.Models;
using Monq.Core.BasicDotNetMicroservice.Sinks.Loki.Utils;
using Serilog.Events;
using Serilog.Formatting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Monq.Core.BasicDotNetMicroservice.Sinks.Loki;

internal class LokiTextFormatter : ITextFormatter
{
    static readonly Regex _valueWithoutSpaces = new Regex("^\"(\\S+)\"$", RegexOptions.Compiled);

    readonly ITextFormatter _textFormatter;

    public ILogLabelProvider LogLabelProvider { get; }

    /// <summary>
    /// Construct a <see cref="LokiTextFormatter"/>.
    /// </summary>
    public LokiTextFormatter(ILogLabelProvider logLabelProvider, ITextFormatter textFormatter)
    {
        LogLabelProvider = logLabelProvider;
        _textFormatter = textFormatter;
    }

    public void Format(LogEvent logEvent, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(output);

        var labels = new List<LokiLabel>();

        foreach (LokiLabel globalLabel in LogLabelProvider.GetLabels())
            labels.Add(new LokiLabel(globalLabel.Key, globalLabel.Value));

        var sb = new StringBuilder();
        using (var tw = new StringWriter(sb))
        {
            _textFormatter.Format(logEvent, tw);
        }

        HandleProperty("level", GetLevel(logEvent.Level), labels, sb);
        foreach (var property in logEvent.Properties)
            HandleProperty(property.Key, property.Value.ToString(), labels, sb);

        // Order the labels so they always get the same chunk in loki
        labels = labels.OrderBy(l => l.Key).ToList();

        // Loki doesn't like \r\n for new line, and we can't guarantee the message doesn't have any
        // in it, so we replace \r\n with \n on the final message
        var lokiTempEntry = new LokiTempEntry(labels, logEvent.Timestamp.ToUnixNanosecondsString(), sb.ToString().Replace("\r\n", "\n"));

        output.Write(JsonSerializer.Serialize(lokiTempEntry, LokiTempEntryContext.Default.LokiTempEntry));
    }

    void HandleProperty(string name, string value, List<LokiLabel> labels, StringBuilder sb)
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
            var match = _valueWithoutSpaces.Match(value);
            return match.Success
                ? Regex.Unescape(match.Groups[1].Value)
                : value;
        }
        catch
        {
            return value;
        }
    }

    static string GetLevel(LogEventLevel level) => level switch
        {
            LogEventLevel.Verbose => "trace",
            LogEventLevel.Debug => "debug",
            LogEventLevel.Information => "info",
            LogEventLevel.Warning => "warning",
            LogEventLevel.Error => "error",
            LogEventLevel.Fatal => "critical",
            _ => "unknown"
        };

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
