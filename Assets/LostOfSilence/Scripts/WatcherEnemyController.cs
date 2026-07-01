using UnityEngine;

namespace LostOfSilence
{
    public sealed class WatcherEnemyController : MonoBehaviour
    {
        [SerializeField] private FirstPersonController player;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float speed = 1.65f;
        [SerializeField] private float catchDistance = 1.15f;
        [SerializeField] private Vector2 xBounds = new Vector2(-2.8f, 8.2f);
        [SerializeField] private Vector2 zBounds = new Vector2(9.2f, 16.2f);

        private float fixedY;

        private void Start()
        {
            if (player == null)
            {
                player = FindFirstObjectByType<FirstPersonController>();
            }

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            fixedY = transform.position.y;
        }

        private void Update()
        {
            if (player == null || playerCamera == null || GameManager.Instance == null || !GameManager.Instance.GameplayInputActive)
            {
                return;
            }

            if (!IsPlayerOnSameFloor() || IsBeingWatched())
            {
                return;
            }

            Vector3 target = player.transform.position;
            target.y = fixedY;
            target.x = Mathf.Clamp(target.x, xBounds.x, xBounds.y);
            target.z = Mathf.Clamp(target.z, zBounds.x, zBounds.y);

            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            Vector3 look = player.transform.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
            }

            if (Vector3.Distance(transform.position, player.transform.position) <= catchDistance)
            {
                GameManager.Instance.RespawnPlayer();
            }
        }

        private bool IsPlayerOnSameFloor()
        {
            return player.transform.position.y > 2.2f && player.transform.position.y < 4.2f;
        }

        private bool IsBeingWatched()
        {
            Vector3 toEnemy = transform.position + Vector3.up * 1.2f - playerCamera.transform.position;
            if (toEnemy.magnitude > 12f)
            {
                return false;
            }

            return Vector3.Angle(playerCamera.transform.forward, toEnemy.normalized) < 28f;
        }
    }
}
