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

            if (!DeckAssetValidation.TryValidate(assets, out string validationError))
            {
                Debug.LogError($"[DeckExporter] Validation failed: {validationError}");
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
    }
}
