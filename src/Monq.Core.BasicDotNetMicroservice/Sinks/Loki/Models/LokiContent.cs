using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Monq.Core.BasicDotNetMicroservice.Sinks.Loki.Models
{
    internal class LokiContent
    {
        [JsonPropertyName("streams")]
        public List<LokiContentStream> Streams { get; set; } = new List<LokiContentStream>();

        public string Serialize() => JsonSerializer.Serialize(this);
    }
}