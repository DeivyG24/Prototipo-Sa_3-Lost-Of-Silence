using UnityEngine;

namespace LostOfSilence
{
    public sealed class FuseBoxInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private Light[] lightsToEnable;
        [SerializeField] private AudioSource powerAudio;

        private bool restored;

        public string Prompt
        {
            get
            {
                if (GameManager.Instance != null)
                {
                    return GameManager.Instance.Localize(restored ? "prompt_power_done" : "prompt_place_fuses");
                }

                return restored ? "Energia restaurada" : "E - Colocar fusivels";
            }
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (restored)
            {
                return;
            }

            if (GameManager.Instance != null && GameManager.Instance.TryRestorePower())
            {
                restored = true;
                foreach (Light targetLight in lightsToEnable)
                {
                    if (targetLight != null)
                    {
                        targetLight.enabled = true;
                        targetLight.intensity *= 1.7f;
                    }
                }

                if (powerAudio != null)
                {
                    powerAudio.Play();
                }
            }
        }
    }
}
