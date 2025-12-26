namespace Monq.Core.BasicDotNetMicroservice.Sinks.Loki.Labels;

/// <summary>
/// Loki label.
/// </summary>
public class LokiLabel
{
    /// <summary>
    /// Create new object of <see cref="LokiLabel"/>.
    /// </summary>
    /// <param name="key">The loki key.</param>
    /// <param name="value">The loki value.</param>
    public LokiLabel(string key, string value)
    {
        Key = key;
        Value = value;
    }

    /// <summary>
    /// The loki key.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// The loki value.
    /// </summary>
    public string Value { get; }
}
