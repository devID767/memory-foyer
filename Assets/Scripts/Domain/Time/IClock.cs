using System;

namespace MemoryFoyer.Domain.Time
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }
}
