using System;
using System.Collections.Generic;
using MemoryFoyer.Domain.Models;
using UnityEngine;

namespace MemoryFoyer.Presentation.Foyer
{
    public sealed class DeckSelectionView : MonoBehaviour
    {
        [SerializeField] private DeckButtonView[] _buttons = null!; // set in Inspector

        public event Action<DeckId>? DeckClicked;

        private void Awake()
        {
            foreach (DeckButtonView button in _buttons)
            {
                button.Clicked += OnChildClicked;
            }
        }

        private void OnDestroy()
        {
            foreach (DeckButtonView button in _buttons)
            {
                button.Clicked -= OnChildClicked;
            }
        }

        public void Bind(IReadOnlyList<DeckButtonModel> models)
        {
            if (models.Count != _buttons.Length)
            {
                throw new InvalidOperationException(
                    $"DeckSelectionView expected {_buttons.Length} models but received {models.Count}.");
            }

            for (int i = 0; i < _buttons.Length; i++)
            {
                _buttons[i].Bind(models[i]);
            }
        }

        private void OnChildClicked(DeckId deckId)
        {
            DeckClicked?.Invoke(deckId);
        }
    }
}
