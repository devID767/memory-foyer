using System.Collections.Generic;
using MemoryFoyer.Editor;
using MemoryFoyer.Infrastructure.ScriptableObjects;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MemoryFoyer.Tests.EditMode.Editor
{
    [TestFixture]
    public sealed class DeckAssetValidationTests
    {
        private readonly List<DeckAsset> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (DeckAsset asset in _created)
            {
                if (asset != null)
                {
                    ScriptableObject.DestroyImmediate(asset);
                }
            }
            _created.Clear();
        }

        private DeckAsset MakeDeck(
            string deckId,
            string displayName,
            int newCardsPerDay,
            params (string id, string front, string back)[] cards)
        {
            DeckAsset asset = ScriptableObject.CreateInstance<DeckAsset>();
            _created.Add(asset);

            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("_deckId").stringValue = deckId;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_newCardsPerDay").intValue = newCardsPerDay;

            SerializedProperty cardsProp = so.FindProperty("_cards");
            cardsProp.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; i++)
            {
                SerializedProperty element = cardsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_cardId").stringValue = cards[i].id;
                element.FindPropertyRelative("_front").stringValue = cards[i].front;
                element.FindPropertyRelative("_back").stringValue = cards[i].back;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        [Test]
        public void TryValidate_ValidSingleDeck_ReturnsTrue()
        {
            DeckAsset deck = MakeDeck("deck-1", "Deck One", 10, ("card-1", "Front", "Back"));
            DeckAsset[] decks = { deck };

            bool result = DeckAssetValidation.TryValidate(decks, out string error);

            Assert.That(result, Is.True);
            Assert.That(error, Is.Empty);
        }

        [Test]
        public void TryValidate_EmptyDeckId_ReturnsFalseWithMessage()
        {
            DeckAsset deck = MakeDeck("", "Deck One", 10, ("card-1", "Front", "Back"));
            DeckAsset[] decks = { deck };

            bool result = DeckAssetValidation.TryValidate(decks, out string error);

            Assert.That(result, Is.False);
            Assert.That(error, Does.Contain("empty _deckId"));
        }

        [Test]
        public void TryValidate_EmptyDisplayName_ReturnsFalseWithMessage()
        {
            DeckAsset deck = MakeDeck("deck-1", "", 10, ("card-1", "Front", "Back"));
            DeckAsset[] decks = { deck };

            bool result = DeckAssetValidation.TryValidate(decks, out string error);

            Assert.That(result, Is.False);
            Assert.That(error, Does.Contain("empty _displayName"));
        }

        [Test]
        public void TryValidate_NewCardsPerDayZero_ReturnsFalseWithMessage()
        {
            DeckAsset deck = MakeDeck("deck-1", "Deck One", 0, ("card-1", "Front", "Back"));
            DeckAsset[] decks = { deck };

            bool result = DeckAssetValidation.TryValidate(decks, out string error);

            Assert.That(result, Is.False);
            Assert.That(error, Does.Contain("_newCardsPerDay"));
        }

        [Test]
        public void TryValidate_DuplicateDeckIdAcrossDecks_ReturnsFalseWithMessage()
        {
            DeckAsset deck1 = MakeDeck("deck-1", "Deck One", 10, ("card-1", "Front", "Back"));
            DeckAsset deck2 = MakeDeck("deck-1", "Deck Two", 10, ("card-2", "Front", "Back"));
            DeckAsset[] decks = { deck1, deck2 };

            bool result = DeckAssetValidation.TryValidate(decks, out string error);

            Assert.That(result, Is.False);
            Assert.That(error, Does.Contain("Duplicate deckId"));
        }

        [Test]
        public void TryValidate_DeckWithZeroCards_ReturnsFalseWithMessage()
        {
            DeckAsset deck = MakeDeck("deck-1", "Deck One", 10);
            DeckAsset[] decks = { deck };

            bool result = DeckAssetValidation.TryValidate(decks, out string error);

            Assert.That(result, Is.False);
            Assert.That(error, Does.Contain("zero cards"));
        }

        [Test]
        public void TryValidate_EmptyCardId_ReturnsFalseWithMessage()
        {
            DeckAsset deck = MakeDeck("deck-1", "Deck One", 10, ("", "Front", "Back"));
            DeckAsset[] decks = { deck };

            bool result = DeckAssetValidation.TryValidate(decks, out string error);

            Assert.That(result, Is.False);
            Assert.That(error, Does.Contain("empty _cardId"));
        }

        [Test]
        public void TryValidate_DuplicateCardIdWithinDeck_ReturnsFalseWithMessage()
        {
            DeckAsset deck = MakeDeck("deck-1", "Deck One", 10,
                ("card-1", "Front A", "Back A"),
                ("card-1", "Front B", "Back B"));
            DeckAsset[] decks = { deck };

            bool result = DeckAssetValidation.TryValidate(decks, out string error);

            Assert.That(result, Is.False);
            Assert.That(error, Does.Contain("duplicate cardId"));
        }

        [Test]
        public void TryValidate_SameCardIdAcrossDecks_ReturnsFalseWithMessage()
        {
            DeckAsset deck1 = MakeDeck("deck-1", "Deck One", 10, ("card-shared", "Front", "Back"));
            DeckAsset deck2 = MakeDeck("deck-2", "Deck Two", 10, ("card-shared", "Front", "Back"));
            DeckAsset[] decks = { deck1, deck2 };

            bool result = DeckAssetValidation.TryValidate(decks, out string error);

            Assert.That(result, Is.False);
            Assert.That(error, Does.Contain("globally unique"));
        }
    }
}
