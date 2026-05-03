using System;

namespace MemoryFoyer.Application.Http
{
    /// <summary>
    /// Thrown when the server returns a 4xx response or when the success response
    /// body cannot be deserialized. This exception is not retryable.
    /// </summary>
    public sealed class HttpContractException : Exception
    {
        public int StatusCode { get; }
        public string Body { get; }

        public HttpContractException(int statusCode, string body, string message, Exception? inner = null)
            : base(message, inner)
        {
            StatusCode = statusCode;
            Body = body;
        }
    }
}
