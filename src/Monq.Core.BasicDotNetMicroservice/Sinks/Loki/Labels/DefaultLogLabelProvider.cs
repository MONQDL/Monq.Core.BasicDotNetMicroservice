using System.Collections.Generic;
using System.Linq;

namespace Monq.Core.BasicDotNetMicroservice.Sinks.Loki.Labels
{
    internal class DefaultLogLabelProvider : ILogLabelProvider
    {
        readonly IList<LokiLabel> _labels;

        public DefaultLogLabelProvider() : this(null)
        {
        }

        public DefaultLogLabelProvider(IEnumerable<LokiLabel>? labels,
            IEnumerable<string>? propertiesAsLabels = null,
            IEnumerable<string>? propertiesToAppend = null,
            LokiFormatterStrategy formatterStrategy = LokiFormatterStrategy.SpecificPropertiesAsLabelsAndRestAppended)
        {
            _labels = labels?.ToList() ?? new List<LokiLabel>();
            PropertiesAsLabels = propertiesAsLabels?.ToList() ?? new List<string> {
                "level",
                "AppEnvironment",
                "Microservice",
                "Method",
                "RequestId",
                "StatusCode",
                "RequestPath",
                "X-Smon-Userspace-Id",
                "X-Trace-Event-Id"
            };
            PropertiesToAppend = propertiesToAppend?.ToList() ?? new List<string>();
            FormatterStrategy = formatterStrategy;
        }

        public IList<LokiLabel> GetLabels() => _labels;
        
        public IList<string> PropertiesAsLabels { get; }
        public IList<string> PropertiesToAppend { get; }
        public LokiFormatterStrategy FormatterStrategy { get; }
    }
}