using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MemoryFoyer.Infrastructure.ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace MemoryFoyer.Editor
{
    public static class DeckExporter
    {
        private const string MenuPath = "Tools/Memory Foyer/Export Decks → server-decks.json";
        private const string ResourcesFolder = "Decks";
        private const string OutputRelativePath = "server/decks.json";

        [MenuItem(MenuPath)]
        public static void Export()
        {
            DeckAsset[] assets = Resources.LoadAll<DeckAsset>(ResourcesFolder);
            if (assets.Length == 0)
            {
                Debug.LogError($"[DeckExporter] No DeckAssets found under Resources/{ResourcesFolder}/. Aborting.");
                return;
            }

            try
            {
                Validate(assets);
            }
            catch (InvalidDeckAssetException ex)
            {
                Debug.LogError($"[DeckExporter] Validation failed: {ex.Message}");
                return;
            }

            DeckAsset[] sorted = assets.OrderBy(a => a.DeckId, StringComparer.Ordinal).ToArray();
            string json = Serialize(sorted);

            string outputPath = ResolveOutputPath();
            string? dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(outputPath, json + "\n", new UTF8Encoding(false));

            int totalCards = sorted.Sum(a => a.Cards.Count);
            Debug.Log($"[DeckExporter] Wrote {sorted.Length} decks ({totalCards} cards) to {outputPath}");
        }

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

        private static string Serialize(IReadOnlyList<DeckAsset> sortedAssets)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[\n");
            for (int d = 0; d < sortedAssets.Count; d++)
            {
                DeckAsset deck = sortedAssets[d];
                sb.Append("  {\n");
                sb.Append("    \"deckId\": ").Append(JsonString(deck.DeckId)).Append(",\n");
                sb.Append("    \"displayName\": ").Append(JsonString(deck.DisplayName)).Append(",\n");
                sb.Append("    \"description\": ").Append(JsonString(deck.Description)).Append(",\n");
                sb.Append("    \"newCardsPerDay\": ").Append(deck.NewCardsPerDay).Append(",\n");
                sb.Append("    \"cardIds\": [");
                for (int c = 0; c < deck.Cards.Count; c++)
                {
                    if (c > 0)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(JsonString(deck.Cards[c].CardId));
                }
                sb.Append("]\n");
                sb.Append("  }");
                if (d < sortedAssets.Count - 1)
                {
                    sb.Append(",");
                }
                sb.Append("\n");
            }
            sb.Append("]");
            return sb.ToString();
        }

        private static string JsonString(string value)
        {
            StringBuilder sb = new StringBuilder(value.Length + 2);
            sb.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        private static string ResolveOutputPath()
        {
            string assetsPath = Application.dataPath;
            string projectRoot = Path.GetDirectoryName(assetsPath) ?? throw new InvalidOperationException(
                $"Could not resolve project root from Application.dataPath='{assetsPath}'.");
            return Path.Combine(projectRoot, OutputRelativePath);
        }

        private sealed class InvalidDeckAssetException : Exception
        {
            public InvalidDeckAssetException(string message) : base(message)
            {
            }
        }
    }
}
