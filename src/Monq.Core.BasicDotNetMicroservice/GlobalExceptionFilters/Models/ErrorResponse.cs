using System;
using System.Text;
using System.Text.Json;

namespace Monq.Core.BasicDotNetMicroservice.GlobalExceptionFilters.Models;

/// <summary>
/// Error response.
/// </summary>
public class ErrorResponse
{
    // Кэшированный JSON‑строку. null – ещё не формировался.
    string? _jsonCache;

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

    /// <summary>
    /// Возвращает объект в виде JSON‑строки без вызова JsonSerializer.
    /// Сформированную строку кэшируем, чтобы последующие вызовы были O(1).
    /// </summary>
    public override string ToString()
    {
        // Если уже есть кэш – просто возвращаем его.
        if (_jsonCache is not null)
            return _jsonCache;

        // Формируем JSON вручную, используя JsonEncodedText
        // для корректного экранирования строк.
        var sb = new StringBuilder();
        sb.Append('{');
        // Message – обязательное поле
        sb.Append("\"Message\":");
        sb.Append($"\"{JsonEncodedText.Encode(Message).ToString()}\"");
        sb.Append(',');
        // StackTrace – может быть null
        if (StackTrace is not null)
        {
            sb.Append("\"StackTrace\":");
            sb.Append($"\"{JsonEncodedText.Encode(StackTrace).ToString()}\"");
        }
        else
        {
            sb.Append("\"StackTrace\":null");
        }
        sb.Append('}');
        _jsonCache = sb.ToString();
        return _jsonCache;
    }
}
