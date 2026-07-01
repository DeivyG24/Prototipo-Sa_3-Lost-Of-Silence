using UnityEngine;
using UnityEngine.UI;

namespace LostOfSilence
{
    [RequireComponent(typeof(Slider))]
    public sealed class SensitivitySliderLink : MonoBehaviour
    {
        private Slider slider;

        private void Awake()
        {
            slider = GetComponent<Slider>();
            slider.onValueChanged.AddListener(OnValueChanged);
        }

        private void Start()
        {
            OnValueChanged(slider.value);
        }

        private void OnDestroy()
        {
            if (slider != null)
            {
                slider.onValueChanged.RemoveListener(OnValueChanged);
            }
        }

        private static void OnValueChanged(float value)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetSensitivity(value);
            }
        }
    }
}
