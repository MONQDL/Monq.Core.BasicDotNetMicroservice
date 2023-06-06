using System;
using System.Text.Json;

namespace Monq.Core.BasicDotNetMicroservice.GlobalExceptionFilters.Models
{
    /// <summary>
    /// Error response.
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Exception message.
        /// </summary>
        public string Message { get; set; } = default!;

        /// <summary>
        /// Exception stack trace.
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ErrorResponse()
        { }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ErrorResponse(string message, string? stackTrace = null)
            => (Message, StackTrace) = (message, stackTrace);

        /// <summary>
        /// Constructor.
        /// </summary>
        public ErrorResponse(Exception ex)
            => (Message, StackTrace) = (ex.Message, ex.StackTrace);

        /// <inheritdoc/>
        public override string ToString()
            => JsonSerializer.Serialize(this);
    }
}
