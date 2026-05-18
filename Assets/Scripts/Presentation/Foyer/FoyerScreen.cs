using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MemoryFoyer.Domain.Models;
using MemoryFoyer.Presentation.Common;
using UnityEngine;
using VContainer;

namespace MemoryFoyer.Presentation.Foyer
{
    public sealed class FoyerScreen : MonoBehaviour
    {
        [SerializeField] private GameObject _canvasRoot = null!; // set in Inspector
        [SerializeField] private CanvasGroup _canvasGroup = null!; // set in Inspector — on _canvasRoot
        [SerializeField] private RectTransform _canvasRect = null!; // set in Inspector — on _canvasRoot
        [SerializeField] private DeckSelectionView _deckSelection = null!; // set in Inspector

        public event Action<DeckId>? DeckClicked;

        private CanvasTransition _transition = null!;

        [Inject]
        public void Construct(UIAnimationConfig uiConfig)
        {
            _transition = new CanvasTransition(_canvasGroup, _canvasRect, uiConfig);
        }

        private void Awake()
        {
            _deckSelection.DeckClicked += OnDeckClicked;
        }

        private void OnDestroy()
        {
            _deckSelection.DeckClicked -= OnDeckClicked;
        }

        public void Show()
        {
            _transition.FadeInAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        public void Hide()
        {
            _transition.Kill();
            _canvasRoot.SetActive(false);
        }

        public void Bind(IReadOnlyList<DeckButtonModel> models)
        {
            _deckSelection.Bind(models);
        }

        private void OnDeckClicked(DeckId deckId)
        {
            DeckClicked?.Invoke(deckId);
        }
    }
}
