using System.Net;

namespace Monq.Core.BasicDotNetMicroservice.WebApp.ModelsExceptions;

/// <summary>
/// Класс представляет расширенную версия исключения для обслуживания запросов RestHttpClient
/// </summary>
public class ResponseException : Exception
{
    /// <summary>
    /// Код ответа HttpClient.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Строка с данными ответа на Http запрос.
    /// </summary>
    public string? ResponseData { get; }

    /// <summary>
    /// Id запроса к нижестоящему сервису для выявления связности событий в системе логирования.
    /// </summary>
    public string? EventId { get; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ResponseException"/>.
    /// </summary>
    /// <param name="message">Сообщение, описывающее текущее исключение.</param>
    /// <param name="traceEventId">Id запроса к нижестоящему сервису для выявления связности событий в системе логирования.</param>
    /// <param name="statusCode">Код ответа HttpClient.</param>
    public ResponseException(string message, string? traceEventId, HttpStatusCode statusCode)
        : base(message)
    {
        StatusCode = statusCode;
        EventId = traceEventId;
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ResponseException"/>.
    /// </summary>
    /// <param name="message">Сообщение, описывающее текущее исключение.</param>
    /// <param name="traceEventId">Id запроса к нижестоящему сервису для выявления связности событий в системе логирования.</param>
    /// <param name="statusCode">Код ответа HttpClient.</param>
    /// <param name="responseData">Строка с данными ответа на Http запрос.</param>
    public ResponseException(string message, string? traceEventId, HttpStatusCode statusCode, string? responseData = null)
        : this(message, traceEventId, statusCode) => ResponseData = responseData;
}
