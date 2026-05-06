using MemoryFoyer.Presentation.Foyer;
using VContainer;
using VContainer.Unity;

namespace MemoryFoyer.Composition
{
    public sealed class FoyerLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<DeckSelectionView>();
            builder.RegisterComponentInHierarchy<OfflineBannerView>();
            builder.RegisterEntryPoint<FoyerPresenter>();
        }
    }
}
