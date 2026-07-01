using UnityEngine;

namespace LostOfSilence
{
    public sealed class HidingSpot : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform hidePoint;
        [SerializeField] private Transform exitPoint;

        private PlayerInteractor hiddenPlayer;

        public string Prompt
        {
            get
            {
                if (GameManager.Instance != null)
                {
                    return GameManager.Instance.Localize(hiddenPlayer == null ? "prompt_hide" : "prompt_leave");
                }

                return hiddenPlayer == null ? "E - Esconder" : "E - Sair";
            }
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (interactor == null || interactor.Controller == null)
            {
                return;
            }

            if (hiddenPlayer == null)
            {
                hiddenPlayer = interactor;
                interactor.SetHiddenInteractable(this);
                interactor.Controller.SetHidden(true, hidePoint != null ? hidePoint : transform);
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ShowMessage(GameManager.Instance.Localize("hide_message"));
                }
            }
            else if (hiddenPlayer == interactor)
            {
                hiddenPlayer = null;
                interactor.SetHiddenInteractable(null);
                Transform target = exitPoint != null ? exitPoint : transform;
                interactor.Controller.SetHidden(false);
                interactor.Controller.Teleport(target.position, target.rotation);
            }
        }
    }
}
