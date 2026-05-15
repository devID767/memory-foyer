using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MemoryFoyer.Application.Foyer;
using MemoryFoyer.Domain.Models;

namespace MemoryFoyer.Tests.EditMode.Application.Foyer
{
    [TestFixture]
    public sealed class DeckOrderingTests
    {
        private static DeckListEntry Entry(string id, int due, int total = 10)
            => new DeckListEntry(new DeckId(id), id, due, total);

        private static string[] OrderedIds(IEnumerable<DeckListEntry> entries)
            => DeckOrdering.Order(entries.ToList()).Select(e => e.Id.Value).ToArray();

        [Test]
        public void Order_DecksWithDue_RankAboveCaughtUpDecks()
        {
            string[] result = OrderedIds(new[]
            {
                Entry("alpha", due: 0),
                Entry("zulu", due: 5),
            });

            Assert.That(result, Is.EqualTo(new[] { "zulu", "alpha" }));
        }

        [Test]
        public void Order_WithinDueGroup_TiebreaksByDeckIdOrdinal()
        {
            string[] result = OrderedIds(new[]
            {
                Entry("zebra", due: 1),
                Entry("apple", due: 9),
            });

            Assert.That(result, Is.EqualTo(new[] { "apple", "zebra" }));
        }

        [Test]
        public void Order_WithinCaughtUpGroup_TiebreaksByDeckIdOrdinal()
        {
            string[] result = OrderedIds(new[]
            {
                Entry("zeta", due: 0),
                Entry("beta", due: 0),
            });

            Assert.That(result, Is.EqualTo(new[] { "beta", "zeta" }));
        }

        [Test]
        public void Order_AllCaughtUp_OrdersPurelyByDeckId()
        {
            string[] result = OrderedIds(new[]
            {
                Entry("c", due: 0),
                Entry("a", due: 0),
                Entry("b", due: 0),
            });

            Assert.That(result, Is.EqualTo(new[] { "a", "b", "c" }));
        }

        [Test]
        public void Order_IsDeterministic_RegardlessOfInputOrder()
        {
            DeckListEntry[] set =
            {
                Entry("myths", due: 0),
                Entry("capitals", due: 10),
                Entry("idioms", due: 2),
                Entry("movies", due: 0),
            };

            string[] forward = OrderedIds(set);
            string[] reversed = OrderedIds(set.Reverse());

            Assert.That(forward, Is.EqualTo(reversed));
        }

        [Test]
        public void Order_MixedRealistic_DueGroupThenCaughtUpEachByDeckId()
        {
            string[] result = OrderedIds(new[]
            {
                Entry("capitals", due: 10),
                Entry("movies", due: 0),
                Entry("idioms", due: 2),
                Entry("myths", due: 0),
            });

            // Due group by id: capitals, idioms — then caught-up by id: movies, myths.
            Assert.That(result, Is.EqualTo(new[] { "capitals", "idioms", "movies", "myths" }));
        }

        [Test]
        public void Order_EmptyInput_ReturnsEmpty()
        {
            IReadOnlyList<DeckListEntry> result = DeckOrdering.Order(new List<DeckListEntry>());

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void Order_DoesNotSlice_ReturnsEveryEntry()
        {
            string[] result = OrderedIds(new[]
            {
                Entry("a", due: 1),
                Entry("b", due: 1),
                Entry("c", due: 1),
                Entry("d", due: 1),
            });

            Assert.That(result.Length, Is.EqualTo(4));
        }
    }
}
