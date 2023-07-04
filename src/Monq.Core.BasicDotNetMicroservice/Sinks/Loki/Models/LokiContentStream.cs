using Monq.Core.BasicDotNetMicroservice.Sinks.Loki.Utils;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Monq.Core.BasicDotNetMicroservice.Sinks.Loki.Models
{
    internal class LokiContentStream
    {
        [JsonPropertyName("stream")]
        public Dictionary<string, string> Labels { get; } = new();

        [JsonPropertyName("values")]
        public IList<IList<string>> Entries { get; set; } = new List<IList<string>>();

        public void AddEntry(DateTimeOffset timestamp, string entry)
        {
            Entries.Add(new[] { timestamp.ToUnixNanosecondsString(), entry });
        }

        public void AddLabel(string key, string value)
        {
            Labels[key] = value;
        }
    }
}