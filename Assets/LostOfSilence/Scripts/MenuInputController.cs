using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LostOfSilence
{
    public sealed class MenuInputController : MonoBehaviour
    {
        [SerializeField] private Button firstButton;

        private void OnEnable()
        {
            SelectFirstButton();
        }

        private void Start()
        {
            SelectFirstButton();
        }

        private void Update()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.enterKey.wasPressedThisFrame && IsPanelOpen("MainMenuPanel"))
            {
                GameManager.Instance.StartGame();
            }
            else if (keyboard.tKey.wasPressedThisFrame && IsPanelOpen("MainMenuPanel"))
            {
                GameManager.Instance.ShowTutorial();
            }
            else if (keyboard.sKey.wasPressedThisFrame && IsPanelOpen("MainMenuPanel"))
            {
                GameManager.Instance.OpenSettingsFromMenu();
            }
            else if (keyboard.escapeKey.wasPressedThisFrame && IsFrontMenuOpen())
            {
                GameManager.Instance.CloseOpenMenuWithEscape();
            }
        }

        public void SelectFirstButton()
        {
            if (firstButton != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
            }
        }

        private static bool IsPanelOpen(string panelName)
        {
            GameObject panel = GameObject.Find(panelName);
            return panel != null && panel.activeInHierarchy;
        }

        private static bool IsFrontMenuOpen()
        {
            return IsPanelOpen("MainMenuPanel") || IsPanelOpen("TutorialPanel") || IsPanelOpen("SettingsPanel");
        }
    }
}
