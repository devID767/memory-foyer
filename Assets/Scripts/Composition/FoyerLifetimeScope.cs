using System.Collections.Generic;
using MemoryFoyer.Domain.Models;
using MemoryFoyer.Infrastructure.ScriptableObjects;
using MemoryFoyer.Presentation.Foyer;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MemoryFoyer.Composition
{
    public sealed class FoyerLifetimeScope : LifetimeScope
    {
        private const string LayoutConfigResourcePath = "Config/FoyerLayoutConfig";
        private const string DecksResourceFolder = "Decks";

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(LoadLayoutConfig());
            builder.RegisterInstance<IReadOnlyDictionary<DeckId, Sprite>>(BuildIconDictionary());

            builder.RegisterComponentInHierarchy<DeckSelectionView>();
            builder.RegisterComponentInHierarchy<OfflineBannerView>();
            builder.RegisterEntryPoint<FoyerPresenter>();
        }

        private static FoyerLayoutConfig LoadLayoutConfig()
        {
            FoyerLayoutConfig? config = Resources.Load<FoyerLayoutConfig>(LayoutConfigResourcePath);
            if (config == null)
            {
                throw new MissingReferenceException(
                    $"FoyerLayoutConfig not found at Resources/{LayoutConfigResourcePath}.asset");
            }
            return config;
        }

        private static Dictionary<DeckId, Sprite> BuildIconDictionary()
        {
            DeckAsset[] assets = Resources.LoadAll<DeckAsset>(DecksResourceFolder);
            Dictionary<DeckId, Sprite> map = new Dictionary<DeckId, Sprite>(assets.Length);
            foreach (DeckAsset asset in assets)
            {
                if (asset.Icon == null || string.IsNullOrEmpty(asset.DeckId))
                {
                    continue;
                }
                map[new DeckId(asset.DeckId)] = asset.Icon;
            }
            return map;
        }
    }
}
