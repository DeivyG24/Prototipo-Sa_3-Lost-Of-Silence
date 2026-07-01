using UnityEngine;
using UnityEngine.InputSystem;

namespace LostOfSilence
{
    public sealed class KeypadPuzzleInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private int puzzleId = 1;
        [SerializeField] private int requiredPuzzleId = 0;
        [SerializeField] private string code = "413";
        [SerializeField] private DoorInteractable doorToUnlock;
        [SerializeField] private GameObject objectToEnable;

        private string current = string.Empty;
        private bool active;
        private bool solved;

        public string Prompt => GameManager.Instance != null
            ? GameManager.Instance.Localize(solved ? "prompt_puzzle_done" : "prompt_keypad")
            : "E - Teclado";

        private void Update()
        {
            if (!active || solved || GameManager.Instance == null || !GameManager.Instance.GameplayInputActive)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            string digit = ReadDigit(keyboard);
            if (string.IsNullOrEmpty(digit))
            {
                return;
            }

            current += digit;
            GameManager.Instance.ShowMessage(GameManager.Instance.Localize("keypad_typing") + " " + current, 1.2f);

            if (current.Length < code.Length)
            {
                return;
            }

            if (current == code)
            {
                solved = true;
                active = false;
                doorToUnlock?.Unlock();
                if (objectToEnable != null)
                {
                    objectToEnable.SetActive(true);
                }

                GameManager.Instance.CompletePuzzle(puzzleId);
                GameManager.Instance.ShowMessage(GameManager.Instance.Localize("keypad_solved"), 4f);
            }
            else
            {
                current = string.Empty;
                GameManager.Instance.ShowMessage(GameManager.Instance.Localize("keypad_wrong"), 3f);
            }
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (solved || GameManager.Instance == null)
            {
                return;
            }

            if (!GameManager.Instance.IsPuzzleComplete(requiredPuzzleId))
            {
                GameManager.Instance.ShowMessage(GameManager.Instance.Localize("keypad_needs_note"), 4f);
                return;
            }

            current = string.Empty;
            active = true;
            GameManager.Instance.ShowMessage(GameManager.Instance.Localize("keypad_start"), 5f);
        }

        private static string ReadDigit(Keyboard keyboard)
        {
            if (keyboard.digit0Key.wasPressedThisFrame || keyboard.numpad0Key.wasPressedThisFrame) return "0";
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) return "1";
            if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) return "2";
            if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) return "3";
            if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) return "4";
            if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame) return "5";
            if (keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame) return "6";
            if (keyboard.digit7Key.wasPressedThisFrame || keyboard.numpad7Key.wasPressedThisFrame) return "7";
            if (keyboard.digit8Key.wasPressedThisFrame || keyboard.numpad8Key.wasPressedThisFrame) return "8";
            if (keyboard.digit9Key.wasPressedThisFrame || keyboard.numpad9Key.wasPressedThisFrame) return "9";
            return string.Empty;
        }
    }
}
