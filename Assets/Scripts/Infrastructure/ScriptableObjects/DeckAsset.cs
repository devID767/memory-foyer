using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemoryFoyer.Infrastructure.ScriptableObjects
{
    [CreateAssetMenu(menuName = "MemoryFoyer/Deck", fileName = "Deck")]
    public sealed class DeckAsset : ScriptableObject
    {
        [SerializeField] private string _deckId = "";
        [SerializeField] private string _displayName = "";
        [SerializeField, TextArea] private string _description = "";
        [SerializeField, Min(1)] private int _newCardsPerDay = 10;
        [SerializeField] private CardData[] _cards = Array.Empty<CardData>();

        public string DeckId => _deckId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public int NewCardsPerDay => _newCardsPerDay;
        public IReadOnlyList<CardData> Cards => _cards;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_deckId))
            {
                Debug.LogWarning($"[DeckAsset] '{name}' has empty deckId — server lookups will fail.", this);
            }
        }
    }

    [Serializable]
    public sealed class CardData
    {
        [SerializeField] private string _cardId = "";
        [SerializeField, TextArea] private string _front = "";
        [SerializeField, TextArea] private string _back = "";

        public string CardId => _cardId;
        public string Front => _front;
        public string Back => _back;
    }
}
