using System;

namespace MemoryFoyer.Application.Persistence
{
    public sealed class ScheduleStoreUnavailableException : Exception
    {
        public ScheduleStoreUnavailableException(string message) : base(message)
        {
        }

        public ScheduleStoreUnavailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
