using UnityEngine;
using UnityEngine.InputSystem;

namespace LostOfSilence
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Light flashlight;
        [SerializeField] private float interactionDistance = 2.3f;
        [SerializeField] private LayerMask interactionMask = ~0;

        private IInteractable currentInteractable;
        private IInteractable hiddenInteractable;

        public FirstPersonController Controller { get; private set; }

        private void Awake()
        {
            Controller = GetComponent<FirstPersonController>();
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.GameplayInputActive)
            {
                return;
            }

            FindInteractable();

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.eKey.wasPressedThisFrame)
            {
                Interact();
            }

            if (keyboard.fKey.wasPressedThisFrame)
            {
                ToggleFlashlight();
            }
        }

        public void Interact()
        {
            if (GameManager.Instance != null && !GameManager.Instance.GameplayInputActive)
            {
                return;
            }

            if (Controller != null && Controller.IsHidden && hiddenInteractable != null)
            {
                hiddenInteractable.Interact(this);
                return;
            }

            currentInteractable?.Interact(this);
        }

        public void ToggleFlashlight()
        {
            if (GameManager.Instance != null && !GameManager.Instance.GameplayInputActive)
            {
                return;
            }

            if (flashlight != null)
            {
                flashlight.enabled = !flashlight.enabled;
            }
        }

        private void FindInteractable()
        {
            if (Controller != null && Controller.IsHidden && hiddenInteractable != null)
            {
                GameManager.Instance?.SetPrompt(hiddenInteractable.Prompt);
                return;
            }

            currentInteractable = null;

            if (playerCamera == null)
            {
                GameManager.Instance?.SetPrompt(string.Empty);
                return;
            }

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionMask, QueryTriggerInteraction.Collide))
            {
                currentInteractable = hit.collider.GetComponentInParent<IInteractable>();
            }

            GameManager.Instance?.SetPrompt(currentInteractable == null ? string.Empty : currentInteractable.Prompt);
        }

        public void SetHiddenInteractable(IInteractable interactable)
        {
            hiddenInteractable = interactable;
        }
    }
}
