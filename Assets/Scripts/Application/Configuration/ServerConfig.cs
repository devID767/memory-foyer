using System;

namespace MemoryFoyer.Application.Configuration
{
    public sealed record ServerConfig(
        string BaseUrl,
        TimeSpan RequestTimeout,
        int Retries,
        TimeSpan RetryBackoff);
}
