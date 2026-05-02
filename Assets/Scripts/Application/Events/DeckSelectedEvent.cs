using MemoryFoyer.Domain.Models;

namespace MemoryFoyer.Application.Events
{
    public sealed record DeckSelectedEvent(DeckId DeckId);
}
