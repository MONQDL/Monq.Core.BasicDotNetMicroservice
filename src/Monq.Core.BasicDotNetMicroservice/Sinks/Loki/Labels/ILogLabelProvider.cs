using System.Collections.Generic;

namespace Monq.Core.BasicDotNetMicroservice.Sinks.Loki.Labels;

#pragma warning disable CS1591 // Отсутствует комментарий XML для открытого видимого типа или члена
public interface ILogLabelProvider
{
    IList<LokiLabel> GetLabels();
    IList<string> PropertiesAsLabels { get; }
    IList<string> PropertiesToAppend { get; }
    LokiFormatterStrategy FormatterStrategy { get; }
}
