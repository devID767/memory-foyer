using System;
using System.Collections.Generic;
using MemoryFoyer.Infrastructure.ScriptableObjects;

namespace MemoryFoyer.Editor
{
    public static class DeckAssetValidation
    {
        private static void Validate(IReadOnlyList<DeckAsset> assets)
        {
            HashSet<string> deckIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> globalCardIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (DeckAsset asset in assets)
            {
                if (string.IsNullOrWhiteSpace(asset.DeckId))
                {
                    throw new InvalidDeckAssetException($"DeckAsset '{asset.name}' has empty _deckId.");
                }
                if (string.IsNullOrWhiteSpace(asset.DisplayName))
                {
                    throw new InvalidDeckAssetException($"DeckAsset '{asset.name}' has empty _displayName.");
                }
                if (asset.NewCardsPerDay < 1)
                {
                    throw new InvalidDeckAssetException(
                        $"DeckAsset '{asset.name}' has invalid _newCardsPerDay = {asset.NewCardsPerDay} (must be >= 1).");
                }
                if (!deckIds.Add(asset.DeckId))
                {
                    throw new InvalidDeckAssetException($"Duplicate deckId '{asset.DeckId}' across DeckAssets.");
                }
                if (asset.Cards.Count == 0)
                {
                    throw new InvalidDeckAssetException($"DeckAsset '{asset.name}' has zero cards.");
                }

                HashSet<string> deckCardIds = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < asset.Cards.Count; i++)
                {
                    CardData card = asset.Cards[i];
                    if (string.IsNullOrWhiteSpace(card.CardId))
                    {
                        throw new InvalidDeckAssetException(
                            $"DeckAsset '{asset.name}' card index {i} has empty _cardId.");
                    }
                    if (!deckCardIds.Add(card.CardId))
                    {
                        throw new InvalidDeckAssetException(
                            $"DeckAsset '{asset.name}' has duplicate cardId '{card.CardId}'.");
                    }
                    if (!globalCardIds.Add(card.CardId))
                    {
                        throw new InvalidDeckAssetException(
                            $"cardId '{card.CardId}' appears in multiple decks (must be globally unique).");
                    }
                }
            }
        }

        public static bool TryValidate(IReadOnlyList<DeckAsset> assets, out string error)
        {
            try
            {
                Validate(assets);
                error = string.Empty;
                return true;
            }
            catch (InvalidDeckAssetException ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private sealed class InvalidDeckAssetException : Exception
        {
            public InvalidDeckAssetException(string message) : base(message)
            {
            }
        }
    }
}
