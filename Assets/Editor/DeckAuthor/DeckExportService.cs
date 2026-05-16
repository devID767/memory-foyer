using System;
using System.IO;
using System.Linq;
using System.Text;
using MemoryFoyer.Infrastructure.ScriptableObjects;
using UnityEngine;

namespace MemoryFoyer.Editor.DeckAuthor
{
    public readonly struct DeckExportResult
    {
        public DeckExportResult(bool success, string message, int deckCount, int cardCount, string outputPath)
        {
            Success = success;
            Message = message;
            DeckCount = deckCount;
            CardCount = cardCount;
            OutputPath = outputPath;
        }

        public bool Success { get; }
        public string Message { get; }
        public int DeckCount { get; }
        public int CardCount { get; }
        public string OutputPath { get; }
    }

    public static class DeckExportService
    {
        private const string ResourcesFolder = "Decks";
        private const string OutputRelativePath = "server/decks.json";

        public static DeckExportResult Export()
        {
            DeckAsset[] assets = Resources.LoadAll<DeckAsset>(ResourcesFolder);
            if (assets.Length == 0)
            {
                return new DeckExportResult(false,
                    $"No DeckAssets found under Resources/{ResourcesFolder}/.", 0, 0, string.Empty);
            }

            if (!DeckAssetValidation.TryValidate(assets, out string validationError))
            {
                return new DeckExportResult(false, $"Validation failed: {validationError}", 0, 0, string.Empty);
            }

            DeckAsset[] sorted = assets.OrderBy(a => a.DeckId, StringComparer.Ordinal).ToArray();
            string json = DeckJsonSerializer.Serialize(sorted);

            string outputPath = ResolveOutputPath();
            string? dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(outputPath, json + "\n", new UTF8Encoding(false));

            int totalCards = sorted.Sum(a => a.Cards.Count);
            return new DeckExportResult(true,
                $"Wrote {sorted.Length} decks ({totalCards} cards) to {outputPath}",
                sorted.Length, totalCards, outputPath);
        }

        private static string ResolveOutputPath()
        {
            string assetsPath = UnityEngine.Application.dataPath;
            string projectRoot = Path.GetDirectoryName(assetsPath) ?? throw new InvalidOperationException(
                $"Could not resolve project root from Application.dataPath='{assetsPath}'.");
            return Path.Combine(projectRoot, OutputRelativePath);
        }
    }
}
