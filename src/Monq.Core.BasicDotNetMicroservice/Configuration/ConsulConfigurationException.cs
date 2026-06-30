namespace Monq.Core.BasicDotNetMicroservice.Configuration;

/// <summary>
/// Exception thrown when Consul configuration loading fails.
/// </summary>
public class ConsulConfigurationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsulConfigurationException"/> class.
    /// </summary>
    public ConsulConfigurationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsulConfigurationException"/> class with a specified error message.
    /// </summary>
    public ConsulConfigurationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsulConfigurationException"/> class with a specified error message and inner exception.
    /// </summary>
    public ConsulConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
