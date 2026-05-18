using System.Collections.Generic;
using MemoryFoyer.Domain.Models;
using MemoryFoyer.Infrastructure.ScriptableObjects;
using MemoryFoyer.Presentation.Banners;
using MemoryFoyer.Presentation.Common;
using MemoryFoyer.Presentation.Foyer;
using MemoryFoyer.Presentation.Review;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MemoryFoyer.Composition
{
    public sealed class FoyerLifetimeScope : LifetimeScope
    {
        private const string LayoutConfigResourcePath = "Config/FoyerLayoutConfig";
        private const string PaletteConfigResourcePath = "Config/ArtPaletteConfig";
        private const string UIAnimConfigResourcePath = "Config/UIAnimationConfig";
        private const string DecksResourceFolder = "Decks";

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(LoadLayoutConfig());
            builder.RegisterInstance(LoadPaletteConfig());
            builder.RegisterInstance(LoadUIAnimationConfig());
            builder.RegisterInstance<IReadOnlyDictionary<DeckId, Sprite>>(BuildIconDictionary());

            builder.RegisterComponentInHierarchy<FoyerScreen>();
            builder.RegisterComponentInHierarchy<OfflineBannerView>();
            builder.RegisterComponentInHierarchy<ErrorBannerView>();
            builder.RegisterComponentInHierarchy<LoadingView>();
            builder.RegisterComponentInHierarchy<ReviewScreen>();
            builder.RegisterComponentInHierarchy<InputSystemReviewInputSource>()
                .AsImplementedInterfaces();
            builder.RegisterEntryPoint<FoyerPresenter>();
            builder.RegisterEntryPoint<ReviewPresenter>();
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

        private static ArtPaletteConfig LoadPaletteConfig()
        {
            ArtPaletteConfig? config = Resources.Load<ArtPaletteConfig>(PaletteConfigResourcePath);
            if (config == null)
            {
                throw new MissingReferenceException(
                    $"ArtPaletteConfig not found at Resources/{PaletteConfigResourcePath}.asset");
            }
            return config;
        }

        private static UIAnimationConfig LoadUIAnimationConfig()
        {
            UIAnimationConfig? config = Resources.Load<UIAnimationConfig>(UIAnimConfigResourcePath);
            if (config == null)
            {
                throw new MissingReferenceException(
                    $"UIAnimationConfig not found at Resources/{UIAnimConfigResourcePath}.asset");
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
