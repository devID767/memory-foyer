using TMPro;
using UnityEngine;

namespace MemoryFoyer.Presentation.Review
{
    public sealed class BackFaceView : MonoBehaviour
    {
        [SerializeField] private GameObject _root = null!;
        [SerializeField] private TMP_Text _questionLabel = null!;
        [SerializeField] private TMP_Text _answerLabel = null!;

        public void Bind(in BackFaceData data)
        {
            _questionLabel.text = data.Question;
            _answerLabel.text = data.Answer;
        }

        public void SetVisible(bool visible)
        {
            _root.SetActive(visible);
        }
    }
}
