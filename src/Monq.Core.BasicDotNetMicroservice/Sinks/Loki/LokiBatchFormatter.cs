using Monq.Core.BasicDotNetMicroservice.Sinks.Loki.Labels;
using Monq.Core.BasicDotNetMicroservice.Sinks.Loki.Models;
using Serilog.Events;
using Serilog.Sinks.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Monq.Core.BasicDotNetMicroservice.Sinks.Loki;

internal class LokiBatchFormatter : IBatchFormatter
{
    static readonly Regex _valueWithoutSpaces = new Regex("^\"(\\S+)\"$", RegexOptions.Compiled);

    public ILogLabelProvider LogLabelProvider { get; }

    public LokiBatchFormatter()
    {
        LogLabelProvider = new DefaultLogLabelProvider();
    }

    public LokiBatchFormatter(ILogLabelProvider logLabelProvider)
    {
        LogLabelProvider = logLabelProvider;
    }

    public void Format(IEnumerable<string> logEvents, TextWriter output)
    {
        if (logEvents == null)
            throw new ArgumentNullException(nameof(logEvents));
        if (output == null)
            throw new ArgumentNullException(nameof(output));

        var streamsDictionary = new Dictionary<string, LokiContentStream>();

        // Here we have serialized list of LokiTempEntry objects.
        foreach (var logEvent in logEvents)
        {
            var lokiTempEvent = JsonSerializer.Deserialize(logEvent, LokiTempEntryContext.Default.LokiTempEntry);

            if (lokiTempEvent is null)
                continue;

            // Order the labels so they always get the same chunk in loki
            var key = string.Join(",", lokiTempEvent.Labels.Select(l => $"{l.Key}={l.Value}"));

            if (!streamsDictionary.TryGetValue(key, out var stream))
            {
                stream = new LokiContentStream();
                streamsDictionary.Add(key, stream);

                foreach (var label in lokiTempEvent.Labels)
                {
                    stream.AddLabel(label.Key, label.Value);
                }
            }

            // Loki doesn't like \r\n for new line, and we can't guarantee the message doesn't have any
            // in it, so we replace \r\n with \n on the final message
            stream.AddEntry(lokiTempEvent);
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
}
