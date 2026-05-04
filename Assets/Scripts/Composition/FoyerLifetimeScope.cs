using VContainer;
using VContainer.Unity;

namespace MemoryFoyer.Composition
{
    // Per-scene scope for the Foyer scene. Populated in Phase 5 (FoyerPresenter,
    // DeckSelectionView) and Phase 6 (ReviewPresenter, ReviewView). Parent-linked
    // to ProjectLifetimeScope via the Inspector's parent reference.
    public sealed class FoyerLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
        }
    }
}
