using Monq.Core.BasicDotNetMicroservice.Sinks.Loki.Labels;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Monq.Core.BasicDotNetMicroservice.Sinks.Loki.Models;

internal class LokiTempEntry
{
    public LokiTempEntry(IEnumerable<LokiLabel> labels, string ts, string line)
    {
        Ts = ts;
        Line = line;
        Labels = labels;
    }

    public string Ts { get; set; }

    public string Line { get; set; }

    public IEnumerable<LokiLabel> Labels { get; set; }
}

[JsonSerializable(typeof(LokiTempEntry))]
[JsonSerializable(typeof(IEnumerable<LokiLabel>))]
internal partial class LokiTempEntryContext : JsonSerializerContext
{
}
