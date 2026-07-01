using UnityEngine;

namespace LostOfSilence
{
    public sealed class FinalGateInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform gatePanel;

        private bool opened;

        public string Prompt => GameManager.Instance != null
            ? GameManager.Instance.Localize("prompt_final_gate")
            : "E - Abrir portao";

        private void Awake()
        {
            if (gatePanel == null)
            {
                gatePanel = transform;
            }
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (opened || GameManager.Instance == null)
            {
                return;
            }

            if (GameManager.Instance.TryOpenFinalGate())
            {
                opened = true;
                gatePanel.gameObject.SetActive(false);
            }
        }
    }
}
