using UnityEngine;

namespace LostOfSilence
{
    public sealed class PuzzleStationInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private int puzzleId = 2;
        [SerializeField] private int requiredPuzzleId = -1;
        [SerializeField] private string promptKey = "prompt_puzzle";
        [SerializeField] private string completeKey = "puzzle_solved";
        [SerializeField] private DoorInteractable doorToUnlock;
        [SerializeField] private GameObject objectToEnable;

        private bool solved;

        public string Prompt => GameManager.Instance != null
            ? GameManager.Instance.Localize(solved ? "prompt_puzzle_done" : promptKey)
            : "E - Resolver";

        public void Interact(PlayerInteractor interactor)
        {
            if (solved || GameManager.Instance == null)
            {
                return;
            }

            if (requiredPuzzleId >= 0 && !GameManager.Instance.IsPuzzleComplete(requiredPuzzleId))
            {
                GameManager.Instance.ShowMessage(GameManager.Instance.Localize("puzzle_missing_step"), 4f);
                return;
            }

            solved = true;
            doorToUnlock?.Unlock();
            if (objectToEnable != null)
            {
                objectToEnable.SetActive(true);
            }

            GameManager.Instance.CompletePuzzle(puzzleId);
            GameManager.Instance.ShowMessage(GameManager.Instance.Localize(completeKey), 4f);
        }
    }
}
