using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace LostOfSilence
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraRoot;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 2.6f;
        [SerializeField] private float runSpeed = 4.8f;
        [SerializeField] private float crouchSpeed = 1.35f;
        [SerializeField] private float gravity = -18f;
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float touchLookSensitivity = 0.045f;
        [SerializeField] private float fallRespawnY = -6f;

        [Header("Stamina")]
        [SerializeField] private float maxStamina = 5f;
        [SerializeField] private float staminaDrain = 1f;
        [SerializeField] private float staminaRecover = 0.65f;

        private CharacterController characterController;
        private float verticalVelocity;
        private float cameraPitch;
        private float stamina;
        private bool frozen;
        private float standingHeight;
        private Vector3 standingCenter;
        private float baseMouseSensitivity;
        private float baseTouchLookSensitivity;

        private int moveTouchId = -1;
        private int lookTouchId = -1;
        private Vector2 moveTouchStart;
        private Vector2 mobileMove;

        public bool IsHidden { get; private set; }
        public float Stamina01 => maxStamina <= 0f ? 1f : stamina / maxStamina;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            stamina = maxStamina;
            standingHeight = characterController.height;
            standingCenter = characterController.center;
            baseMouseSensitivity = mouseSensitivity;
            baseTouchLookSensitivity = touchLookSensitivity;

            if (cameraRoot == null && Camera.main != null)
            {
                cameraRoot = Camera.main.transform;
            }
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            if (transform.position.y < fallRespawnY)
            {
                GameManager.Instance?.RespawnPlayer();
                return;
            }

            if (!frozen && !IsHidden)
            {
                Move();
                Look(ReadLookDelta());
            }

            GameManager.Instance?.UpdateStamina(Stamina01);
        }

        private void Move()
        {
            Vector2 moveInput = ReadMoveInput();
            bool wantsRun = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed && moveInput.y > 0.05f;
            bool crouching = Keyboard.current != null && Keyboard.current.leftCtrlKey.isPressed;
            float speed = crouching ? crouchSpeed : walkSpeed;

            if (wantsRun && stamina > 0.1f && !crouching)
            {
                speed = runSpeed;
                stamina = Mathf.Max(0f, stamina - staminaDrain * Time.deltaTime);
            }
            else
            {
                stamina = Mathf.Min(maxStamina, stamina + staminaRecover * Time.deltaTime);
            }

            characterController.height = Mathf.Lerp(characterController.height, crouching ? 1.15f : standingHeight, 10f * Time.deltaTime);
            characterController.center = Vector3.Lerp(characterController.center, crouching ? new Vector3(0f, 0.58f, 0f) : standingCenter, 10f * Time.deltaTime);

            Vector3 movement = transform.right * moveInput.x + transform.forward * moveInput.y;
            movement = Vector3.ClampMagnitude(movement, 1f) * speed;

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -1f;
            }

            verticalVelocity += gravity * Time.deltaTime;
            movement.y = verticalVelocity;
            characterController.Move(movement * Time.deltaTime);
        }

        private Vector2 ReadMoveInput()
        {
            Vector2 input = Vector2.zero;
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed) input.y += 1f;
                if (keyboard.sKey.isPressed) input.y -= 1f;
                if (keyboard.dKey.isPressed) input.x += 1f;
                if (keyboard.aKey.isPressed) input.x -= 1f;
            }

            UpdateTouchInput();
            input += mobileMove;
            return Vector2.ClampMagnitude(input, 1f);
        }

        private Vector2 ReadLookDelta()
        {
            Vector2 delta = Vector2.zero;
            if (Mouse.current != null)
            {
                delta += Mouse.current.delta.ReadValue() * mouseSensitivity;
            }

            if (Touchscreen.current != null && lookTouchId >= 0)
            {
                foreach (TouchControl touch in Touchscreen.current.touches)
                {
                    if (touch.touchId.ReadValue() == lookTouchId)
                    {
                        delta += touch.delta.ReadValue() * touchLookSensitivity;
                        break;
                    }
                }
            }

            return delta;
        }

        private void UpdateTouchInput()
        {
            mobileMove = Vector2.zero;
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                moveTouchId = -1;
                lookTouchId = -1;
                return;
            }

            bool moveTouchAlive = false;
            bool lookTouchAlive = false;

            foreach (TouchControl touch in touchscreen.touches)
            {
                if (!touch.press.isPressed)
                {
                    continue;
                }

                int id = touch.touchId.ReadValue();
                Vector2 position = touch.position.ReadValue();

                if (id == moveTouchId)
                {
                    moveTouchAlive = true;
                    Vector2 offset = position - moveTouchStart;
                    mobileMove = Vector2.ClampMagnitude(offset / 85f, 1f);
                }
                else if (id == lookTouchId)
                {
                    lookTouchAlive = true;
                }
                else if (position.x < Screen.width * 0.45f && moveTouchId < 0)
                {
                    moveTouchId = id;
                    moveTouchStart = position;
                    moveTouchAlive = true;
                }
                else if (position.x > Screen.width * 0.55f && lookTouchId < 0)
                {
                    lookTouchId = id;
                    lookTouchAlive = true;
                }
            }

            if (!moveTouchAlive) moveTouchId = -1;
            if (!lookTouchAlive) lookTouchId = -1;
        }

        private void Look(Vector2 delta)
        {
            transform.Rotate(Vector3.up * delta.x);
            cameraPitch = Mathf.Clamp(cameraPitch - delta.y, -80f, 80f);

            if (cameraRoot != null)
            {
                cameraRoot.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);
            }
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            characterController.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            verticalVelocity = 0f;
            characterController.enabled = true;
        }

        public void SetHidden(bool hidden, Transform hidePoint = null)
        {
            IsHidden = hidden;
            frozen = hidden;

            if (hidden && hidePoint != null)
            {
                Teleport(hidePoint.position, hidePoint.rotation);
            }
        }

        public void SetFrozen(bool value)
        {
            frozen = value;
        }

        public void SetLookSensitivity(float multiplier)
        {
            float clamped = Mathf.Clamp(multiplier, 0.35f, 2.5f);
            mouseSensitivity = baseMouseSensitivity * clamped;
            touchLookSensitivity = baseTouchLookSensitivity * clamped;
        }
    }
}
