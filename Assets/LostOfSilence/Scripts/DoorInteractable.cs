using System.Collections;
using UnityEngine;

namespace LostOfSilence
{
    public sealed class DoorInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform hinge;
        [SerializeField] private bool requiresBedroomKey;
        [SerializeField] private ColoredKey requiredColoredKey;
        [SerializeField] private bool requiresPower;
        [SerializeField] private bool requiresAllColoredKeys;
        [SerializeField] private bool startsLocked;
        [SerializeField] private bool isExitDoor;
        [SerializeField] private float openAngle = 95f;
        [SerializeField] private float openSpeed = 4f;

        private bool open;
        private bool locked;
        private Coroutine moveRoutine;

        public string Prompt
        {
            get
            {
                if (locked)
                {
                    if (GameManager.Instance != null)
                    {
                        return GameManager.Instance.Localize(requiresBedroomKey ? "prompt_use_key" : "prompt_locked");
                    }

                    return requiresBedroomKey ? "E - Usar chave" : "Trancada";
                }

                if (GameManager.Instance != null)
                {
                    return GameManager.Instance.Localize(open ? "prompt_close" : "prompt_open");
                }

                return open ? "E - Fechar porta" : "E - Abrir porta";
            }
        }

        private void Awake()
        {
            if (hinge == null)
            {
                hinge = transform;
            }

            locked = startsLocked;
        }

        public void Unlock()
        {
            locked = false;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (locked)
            {
                if (requiresBedroomKey && GameManager.Instance != null && GameManager.Instance.HasBedroomKey)
                {
                    locked = false;
                    GameManager.Instance.ShowMessage(GameManager.Instance.Localize("door_unlocked"));
                }
                else if (requiresPower && GameManager.Instance != null && GameManager.Instance.IsPowerRestored)
                {
                    locked = false;
                    GameManager.Instance.ShowMessage(GameManager.Instance.Localize("door_unlocked"));
                }
                else if (requiresAllColoredKeys && GameManager.Instance != null && GameManager.Instance.HasAllColoredKeys && GameManager.Instance.HasSolvedAllPuzzles)
                {
                    locked = false;
                    GameManager.Instance.ShowMessage(GameManager.Instance.Localize("door_unlocked"));
                }
                else if (requiredColoredKey != ColoredKey.None && GameManager.Instance != null && GameManager.Instance.HasColoredKey(requiredColoredKey))
                {
                    locked = false;
                    GameManager.Instance.ShowMessage(GameManager.Instance.Localize("door_unlocked"));
                }
                else
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.ShowMessage(GetLockedMessage());
                    }

                    return;
                }
            }

            open = !open;
            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
            }

            moveRoutine = StartCoroutine(RotateDoor(open));
        }

        private IEnumerator RotateDoor(bool opening)
        {
            Quaternion start = hinge.localRotation;
            Quaternion target = Quaternion.Euler(0f, opening ? openAngle : 0f, 0f);
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime * openSpeed;
                hinge.localRotation = Quaternion.Slerp(start, target, t);
                yield return null;
            }

            hinge.localRotation = target;
            moveRoutine = null;
        }

        private string GetLockedMessage()
        {
            if (GameManager.Instance == null)
            {
                return "Trancada";
            }

            if (requiresPower)
            {
                return GameManager.Instance.Localize("door_need_power");
            }

            if (requiresAllColoredKeys)
            {
                return GameManager.Instance.Localize("door_need_main_keys");
            }

            return requiredColoredKey switch
            {
                ColoredKey.Blue => GameManager.Instance.Localize("door_need_blue"),
                ColoredKey.Red => GameManager.Instance.Localize("door_need_red"),
                ColoredKey.Green => GameManager.Instance.Localize("door_need_green"),
                _ => GameManager.Instance.Localize("door_locked")
            };
        }
    }
}
