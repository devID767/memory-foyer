using System;

namespace MemoryFoyer.Domain.Time
{
    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
