using System.Text.Json.Serialization;

namespace Monq.Core.BasicDotNetMicroservice.Sinks.Loki.Models;

internal class LokiEntry
{
    public LokiEntry(string ts, string line)
    {
        Ts = ts;
        Line = line;
    }

    [JsonPropertyName("ts")]
    public string Ts { get; set; }

    [JsonPropertyName("line")]
    public string Line { get; set; }
}