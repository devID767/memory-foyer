using System;

namespace MemoryFoyer.Infrastructure.Dtos
{
    [Serializable]
    public sealed class SessionUploadResponseDto
    {
        public bool ok = false;
        public bool dedup = false;
        public DeckScheduleDto updatedSchedule = new DeckScheduleDto();
    }
}
