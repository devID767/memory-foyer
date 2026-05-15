using System;
using System.Collections.Generic;
using NUnit.Framework;
using MemoryFoyer.Application.Persistence;
using MemoryFoyer.Domain.Models;
using MemoryFoyer.Infrastructure.Dtos;

namespace MemoryFoyer.Tests.EditMode.Infrastructure.Dtos
{
    [TestFixture]
    public sealed class DeckSummaryMapperTests
    {
        [Test]
        public void FromDto_MapsAllFields()
        {
            DeckSummaryDto dto = new DeckSummaryDto
            {
                deckId = "capitals",
                displayName = "Capitals of Europe",
                dueCount = 12,
                newCount = 10,
                totalCount = 44,
            };

            DeckSummary summary = ScheduleMappers.FromDto(dto);

            Assert.That(summary.Id, Is.EqualTo(new DeckId("capitals")));
            Assert.That(summary.DisplayName, Is.EqualTo("Capitals of Europe"));
            Assert.That(summary.DueCount, Is.EqualTo(12));
            Assert.That(summary.NewCount, Is.EqualTo(10));
            Assert.That(summary.TotalCount, Is.EqualTo(44));
        }

        [Test]
        public void FromDtos_MapsEveryEntryInOrder()
        {
            DeckSummaryDto[] dtos =
            {
                new DeckSummaryDto { deckId = "a", displayName = "A", dueCount = 1, newCount = 0, totalCount = 5 },
                new DeckSummaryDto { deckId = "b", displayName = "B", dueCount = 2, newCount = 1, totalCount = 6 },
            };

            IReadOnlyList<DeckSummary> result = ScheduleMappers.FromDtos(dtos);

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].Id, Is.EqualTo(new DeckId("a")));
            Assert.That(result[1].Id, Is.EqualTo(new DeckId("b")));
        }

        [Test]
        public void FromDtos_EmptyArray_ReturnsEmptyList()
        {
            IReadOnlyList<DeckSummary> result = ScheduleMappers.FromDtos(Array.Empty<DeckSummaryDto>());

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ToDto_RoundTripsThroughFromDto()
        {
            DeckSummary original = new DeckSummary(
                Id: new DeckId("idioms"),
                DisplayName: "English Idioms",
                DueCount: 7,
                NewCount: 8,
                TotalCount: 30);

            DeckSummary roundTripped = ScheduleMappers.FromDto(ScheduleMappers.ToDto(original));

            Assert.That(roundTripped, Is.EqualTo(original));
        }
    }
}
