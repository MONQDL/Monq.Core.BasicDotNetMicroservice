using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Monq.Core.BasicDotNetMicroservice.Sinks.Loki.Models;

internal class LokiContentStream
{
    [JsonPropertyName("stream")]
    public Dictionary<string, string> Labels { get; } = new();

    [JsonPropertyName("values")]
    public IList<IList<string>> Entries { get; set; } = new List<IList<string>>();

    public void AddEntry(LokiTempEntry entry)
    {
        Entries.Add(new[] { entry.Ts, entry.Line });
    }

    public void AddLabel(string key, string value)
    {
        Labels[key] = value;
    }
}
