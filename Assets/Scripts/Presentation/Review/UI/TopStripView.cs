using TMPro;
using UnityEngine;

namespace MemoryFoyer.Presentation.Review
{
    public sealed class TopStripView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _deckNameLabel = null!;
        [SerializeField] private TMP_Text _progressLabel = null!;

        public void SetDeckName(string name)
        {
            _deckNameLabel.text = name;
        }

        public void SetProgress(int current, int total)
        {
            _progressLabel.text = $"{current} / {total}";
        }
    }
}
