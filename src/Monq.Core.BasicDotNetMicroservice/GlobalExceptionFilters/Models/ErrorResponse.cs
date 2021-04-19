namespace Monq.Core.BasicDotNetMicroservice.GlobalExceptionFilters.Models
{
    public class ErrorResponse
    {
        /// <summary>
        /// Текст сообщения об ошибке.
        /// </summary>
        public string Message { get; set; } = default!;

        /// <summary>
        /// StackTrace исключения.
        /// </summary>
        public string? StackTrace { get; set; }
    }
}
