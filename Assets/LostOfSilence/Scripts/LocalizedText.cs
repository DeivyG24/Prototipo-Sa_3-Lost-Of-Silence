using UnityEngine;
using UnityEngine.UI;

namespace LostOfSilence
{
    [RequireComponent(typeof(Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key;

        private Text label;

        private void Awake()
        {
            label = GetComponent<Text>();
        }

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (label == null)
            {
                label = GetComponent<Text>();
            }

            if (GameManager.Instance != null)
            {
                label.text = GameManager.Instance.Localize(key);
            }
        }
    }
}
