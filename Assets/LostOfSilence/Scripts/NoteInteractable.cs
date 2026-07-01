using UnityEngine;

namespace LostOfSilence
{
    public sealed class NoteInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private int puzzleId;
        [SerializeField] private string messageKey = "note_code_message";

        private bool read;

        public string Prompt => GameManager.Instance != null
            ? GameManager.Instance.Localize(read ? "prompt_read_again" : "prompt_read_note")
            : "E - Ler nota";

        public void Interact(PlayerInteractor interactor)
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            read = true;
            GameManager.Instance.CompletePuzzle(puzzleId);
            GameManager.Instance.ShowMessage(GameManager.Instance.Localize(messageKey), 7f);
        }
    }
}
