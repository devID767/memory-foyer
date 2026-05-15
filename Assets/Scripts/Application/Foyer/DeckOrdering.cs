using System;
using System.Collections.Generic;
using System.Linq;
using MemoryFoyer.Domain.Models;

namespace MemoryFoyer.Application.Foyer
{
    public sealed record DeckListEntry(
        DeckId Id,
        string DisplayName,
        int DueCount,
        int TotalCount);

    public static class DeckOrdering
    {
        public static IReadOnlyList<DeckListEntry> Order(IReadOnlyList<DeckListEntry> entries)
        {
            return entries
                .OrderBy(entry => entry.DueCount > 0 ? 0 : 1)
                .ThenBy(entry => entry.Id.Value, StringComparer.Ordinal)
                .ToList();
        }
    }
}
