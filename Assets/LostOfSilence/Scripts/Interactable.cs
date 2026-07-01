using UnityEngine;

namespace LostOfSilence
{
    public interface IInteractable
    {
        string Prompt { get; }
        void Interact(PlayerInteractor interactor);
    }
}
