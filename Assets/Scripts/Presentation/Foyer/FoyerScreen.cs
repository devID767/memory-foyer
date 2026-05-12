using System;
using System.Collections.Generic;
using MemoryFoyer.Domain.Models;
using UnityEngine;

namespace MemoryFoyer.Presentation.Foyer
{
    public sealed class FoyerScreen : MonoBehaviour
    {
        [SerializeField] private GameObject _canvasRoot = null!; // set in Inspector
        [SerializeField] private DeckSelectionView _deckSelection = null!; // set in Inspector

        public event Action<DeckId>? DeckClicked;

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
            _canvasRoot.SetActive(true);
        }

        public void Hide()
        {
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
