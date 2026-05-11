using TMPro;
using UnityEngine;

namespace MemoryFoyer.Presentation.Review
{
    public sealed class FrontFaceView : MonoBehaviour
    {
        [SerializeField] private GameObject _root = null!;
        [SerializeField] private TMP_Text _questionLabel = null!;

        public void Bind(in FrontFaceData data)
        {
            _questionLabel.text = data.Question;
        }

        public void SetVisible(bool visible)
        {
            _root.SetActive(visible);
        }
    }
}
