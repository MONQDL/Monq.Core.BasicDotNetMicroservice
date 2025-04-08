using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Monq.Core.BasicDotNetMicroservice.Sinks.Loki.Models;

internal class LokiContent
{
    [JsonPropertyName("streams")]
    public List<LokiContentStream> Streams { get; set; } = new List<LokiContentStream>();

    public string Serialize() => JsonSerializer.Serialize(this, LokiContentContext.Default.LokiContent);
}

[JsonSerializable(typeof(LokiContent))]
[JsonSerializable(typeof(List<LokiContentStream>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(IList<IList<string>>))]
internal partial class LokiContentContext : JsonSerializerContext
{
}
