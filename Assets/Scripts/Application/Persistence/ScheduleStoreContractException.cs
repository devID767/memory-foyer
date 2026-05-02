using System;

namespace MemoryFoyer.Application.Persistence
{
    public sealed class ScheduleStoreContractException : Exception
    {
        public int? StatusCode { get; }

        public ScheduleStoreContractException(string message) : base(message)
        {
        }

        public ScheduleStoreContractException(string message, int? statusCode) : base(message)
        {
            StatusCode = statusCode;
        }

        public ScheduleStoreContractException(string message, int? statusCode, Exception innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }
}
