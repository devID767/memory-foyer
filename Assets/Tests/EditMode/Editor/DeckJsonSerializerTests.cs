using System.Collections.Generic;
using MemoryFoyer.Editor.DeckAuthor;
using MemoryFoyer.Infrastructure.ScriptableObjects;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MemoryFoyer.Tests.EditMode.Editor
{
    [TestFixture]
    public sealed class DeckJsonSerializerTests
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
            string description,
            int newCardsPerDay,
            params string[] cardIds)
        {
            DeckAsset asset = ScriptableObject.CreateInstance<DeckAsset>();
            _created.Add(asset);

            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("_deckId").stringValue = deckId;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_description").stringValue = description;
            so.FindProperty("_newCardsPerDay").intValue = newCardsPerDay;

            SerializedProperty cardsProp = so.FindProperty("_cards");
            cardsProp.arraySize = cardIds.Length;
            for (int i = 0; i < cardIds.Length; i++)
            {
                SerializedProperty element = cardsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_cardId").stringValue = cardIds[i];
                element.FindPropertyRelative("_front").stringValue = "Front";
                element.FindPropertyRelative("_back").stringValue = "Back";
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        [Test]
        public void Serialize_SingleDeckMultipleCards_ProducesExpectedJson()
        {
            DeckAsset deck = MakeDeck("deck-1", "Deck One", "First deck", 10, "card-1", "card-2");

            string json = DeckJsonSerializer.Serialize(new[] { deck });

            string expected =
                "[\n" +
                "  {\n" +
                "    \"deckId\": \"deck-1\",\n" +
                "    \"displayName\": \"Deck One\",\n" +
                "    \"description\": \"First deck\",\n" +
                "    \"newCardsPerDay\": 10,\n" +
                "    \"cardIds\": [\"card-1\", \"card-2\"]\n" +
                "  }\n" +
                "]";
            Assert.That(json, Is.EqualTo(expected));
        }

        [Test]
        public void Serialize_MultipleDecks_SeparatesObjectsWithCommaAndNewline()
        {
            DeckAsset deck1 = MakeDeck("deck-1", "Deck One", "A", 10, "card-1");
            DeckAsset deck2 = MakeDeck("deck-2", "Deck Two", "B", 5, "card-2");

            string json = DeckJsonSerializer.Serialize(new[] { deck1, deck2 });

            string expected =
                "[\n" +
                "  {\n" +
                "    \"deckId\": \"deck-1\",\n" +
                "    \"displayName\": \"Deck One\",\n" +
                "    \"description\": \"A\",\n" +
                "    \"newCardsPerDay\": 10,\n" +
                "    \"cardIds\": [\"card-1\"]\n" +
                "  },\n" +
                "  {\n" +
                "    \"deckId\": \"deck-2\",\n" +
                "    \"displayName\": \"Deck Two\",\n" +
                "    \"description\": \"B\",\n" +
                "    \"newCardsPerDay\": 5,\n" +
                "    \"cardIds\": [\"card-2\"]\n" +
                "  }\n" +
                "]";
            Assert.That(json, Is.EqualTo(expected));
        }

        [Test]
        public void Serialize_DeckWithZeroCards_ProducesEmptyCardIdsArray()
        {
            DeckAsset deck = MakeDeck("deck-1", "Deck One", "Empty", 10);

            string json = DeckJsonSerializer.Serialize(new[] { deck });

            Assert.That(json, Does.Contain("\"cardIds\": []\n"));
        }

        [Test]
        public void Serialize_DescriptionWithSpecialCharacters_EscapesPerJsonSpec()
        {
            string description = "a\"b\\c\nd\te" + (char)0x01 + "f";
            DeckAsset deck = MakeDeck("deck-1", "Deck One", description, 10, "card-1");

            string json = DeckJsonSerializer.Serialize(new[] { deck });

            string expectedFragment =
                "\"description\": \"a\\\"b\\\\c\\nd\\te\\u0001f\",";
            Assert.That(json, Does.Contain(expectedFragment));
        }
    }
}
