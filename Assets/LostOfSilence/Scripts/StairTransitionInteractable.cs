using UnityEngine;

namespace LostOfSilence
{
    public sealed class StairTransitionInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform destination;
        [SerializeField] private string promptKey = "prompt_use_stairs";

        public string Prompt => GameManager.Instance != null
            ? GameManager.Instance.Localize(promptKey)
            : "E - Usar escada";

        public void Interact(PlayerInteractor interactor)
        {
            if (destination == null || interactor == null || interactor.Controller == null)
            {
                return;
            }

            interactor.Controller.Teleport(destination.position, destination.rotation);
        }
    }
}
