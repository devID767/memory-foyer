using System;

namespace MemoryFoyer.Infrastructure.Dtos
{
    [Serializable]
    public sealed class HealthDto
    {
        public string status = "";
        public string version = "";
    }
}
