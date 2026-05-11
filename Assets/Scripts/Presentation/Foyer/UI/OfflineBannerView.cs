using TMPro;
using UnityEngine;

namespace MemoryFoyer.Presentation.Foyer
{
    public sealed class OfflineBannerView : MonoBehaviour
    {
        [SerializeField] private GameObject _root = null!; // set in Inspector
        [SerializeField] private TMP_Text _label = null!; // set in Inspector
        [SerializeField] private string _bannerText = "Server offline — stats may be stale";

        private void Awake()
        {
            _label.text = _bannerText;
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            _root.SetActive(visible);
        }
    }
}
