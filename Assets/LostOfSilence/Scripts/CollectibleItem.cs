using UnityEngine;

namespace LostOfSilence
{
    public enum CollectibleKind
    {
        BedroomKey,
        Fuse,
        GateHandle,
        BlueKey,
        RedKey,
        GreenKey
    }

    public sealed class CollectibleItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private CollectibleKind kind;
        [SerializeField] private string customPrompt;

        public string Prompt
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(customPrompt))
                {
                    return customPrompt;
                }

                if (GameManager.Instance != null)
                {
                    return GameManager.Instance.Localize(kind switch
                    {
                        CollectibleKind.BedroomKey => "prompt_key",
                        CollectibleKind.GateHandle => "prompt_gate_handle",
                        CollectibleKind.BlueKey => "prompt_blue_key",
                        CollectibleKind.RedKey => "prompt_red_key",
                        CollectibleKind.GreenKey => "prompt_green_key",
                        _ => "prompt_fuse"
                    });
                }

                return kind switch
                {
                    CollectibleKind.BedroomKey => "E - Pegar chave",
                    CollectibleKind.GateHandle => "E - Pegar manivela",
                    CollectibleKind.BlueKey => "E - Pegar chave azul",
                    CollectibleKind.RedKey => "E - Pegar chave vermelha",
                    CollectibleKind.GreenKey => "E - Pegar chave verde",
                    _ => "E - Pegar fusivel"
                };
            }
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, 35f * Time.deltaTime, Space.World);
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (kind == CollectibleKind.BedroomKey)
            {
                GameManager.Instance?.CollectKey();
            }
            else if (kind == CollectibleKind.GateHandle)
            {
                GameManager.Instance?.CollectGateHandle();
            }
            else if (kind == CollectibleKind.BlueKey)
            {
                GameManager.Instance?.CollectColoredKey(ColoredKey.Blue);
            }
            else if (kind == CollectibleKind.RedKey)
            {
                GameManager.Instance?.CollectColoredKey(ColoredKey.Red);
            }
            else if (kind == CollectibleKind.GreenKey)
            {
                GameManager.Instance?.CollectColoredKey(ColoredKey.Green);
            }
            else
            {
                GameManager.Instance?.CollectFuse();
            }

            Destroy(gameObject);
        }
    }
}
