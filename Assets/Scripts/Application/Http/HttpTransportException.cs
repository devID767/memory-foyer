using System;

namespace MemoryFoyer.Application.Http
{
    /// <summary>
    /// Thrown when an HTTP request fails due to a transport-level error:
    /// connection failure, timeout, data processing error, or a 5xx server response.
    /// This exception is considered retryable.
    /// </summary>
    public sealed class HttpTransportException : Exception
    {
        public HttpTransportException(string message) : base(message)
        {
        }

        public HttpTransportException(string message, Exception? inner) : base(message, inner)
        {
        }
    }
}
