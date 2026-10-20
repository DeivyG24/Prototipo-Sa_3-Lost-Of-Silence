using UnityEngine;
using UnityEngine.AI;

namespace LostOfSilence
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(AudioSource))]
    public sealed class EnemyController : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private FirstPersonController player;
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private Transform enemySpawn;

        [Header("Movimento")]
        [SerializeField] private float patrolSpeed = 1.35f;
        [SerializeField] private float chaseSpeed = 2.9f;
        [SerializeField] private float sightDistance = 8.5f;
        [SerializeField] private float sightAngle = 75f;
        [SerializeField] private float catchDistance = 1.25f;
        [SerializeField] private float searchDuration = 3f;
        [SerializeField] private LayerMask sightMask = ~0;

        [Header("Sons de Movimento")]
        [SerializeField] private AudioClip[] movementSounds;
        [SerializeField, Range(0f, 1f)] private float soundVolume = 0.7f;
        [SerializeField] private float minSoundInterval = 5f;
        [SerializeField] private float maxSoundInterval = 12f;
        [SerializeField] private float movementThreshold = 0.15f;

        [Header("Detecção do Olhar")]
        [SerializeField] private float playerLookAngle = 35f;
        [SerializeField] private float soundDistance = 12f;

        private NavMeshAgent agent;
        private AudioSource audioSource;
        private Camera playerCamera;

        private int patrolIndex;
        private float searchTimer;
        private float nextSoundTime;

        private Vector3 lastKnownPlayerPosition;
        private EnemyState state;

        private enum EnemyState
        {
            Patrol,
            Chase,
            Search
        }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            audioSource = GetComponent<AudioSource>();

            agent.updateRotation = false;
            agent.acceleration = 12f;
            agent.angularSpeed = 540f;
            agent.stoppingDistance = 0.35f;
            agent.autoRepath = true;

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 1f;
            audioSource.volume = soundVolume;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = soundDistance;
        }

        private void Start()
        {
            if (enemySpawn == null)
                enemySpawn = transform;

            if (player == null)
                player = FindFirstObjectByType<FirstPersonController>();

            playerCamera = Camera.main;

            nextSoundTime = Time.time + Random.Range(
                minSoundInterval,
                maxSoundInterval
            );

            WarpToNavMesh(transform.position);
            GoToCurrentPatrolPoint();
        }

        private void Update()
        {
            if (player == null)
                return;

            if (GameManager.Instance != null &&
                !GameManager.Instance.GameplayInputActive)
            {
                agent.isStopped = true;
                return;
            }

            agent.isStopped = false;

            bool canSeePlayer = CanSeePlayer();

            if (canSeePlayer)
            {
                state = EnemyState.Chase;
                lastKnownPlayerPosition = player.transform.position;
            }
            else if (state == EnemyState.Chase)
            {
                state = EnemyState.Search;
                searchTimer = searchDuration;
                SetDestination(lastKnownPlayerPosition);
            }

            if (state == EnemyState.Chase)
            {
                agent.speed = chaseSpeed;
                SetDestination(player.transform.position);
                TryCatchPlayer();
            }
            else if (state == EnemyState.Search)
            {
                agent.speed = patrolSpeed;
                searchTimer -= Time.deltaTime;

                if (searchTimer <= 0f || HasReachedDestination())
                {
                    state = EnemyState.Patrol;
                    GoToCurrentPatrolPoint();
                }
            }
            else
            {
                Patrol();
            }

            RotateToMovement();
            UpdateMovementAudio();
        }

        private void Patrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
                return;

            agent.speed = patrolSpeed;

            if (!agent.hasPath || HasReachedDestination())
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                GoToCurrentPatrolPoint();
            }
        }

        private void GoToCurrentPatrolPoint()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
                return;

            if (patrolPoints[patrolIndex] != null)
                SetDestination(patrolPoints[patrolIndex].position);
        }

        private bool SetDestination(Vector3 position)
        {
            if (!agent.isOnNavMesh)
                WarpToNavMesh(transform.position);

            if (NavMesh.SamplePosition(
                position,
                out NavMeshHit hit,
                2.5f,
                WalkableAreaMask()))
            {
                agent.SetDestination(hit.position);
                return true;
            }

            return false;
        }

        private bool HasReachedDestination()
        {
            if (agent.pathPending)
                return false;

            return agent.remainingDistance <=
                   Mathf.Max(agent.stoppingDistance, 0.45f);
        }

        private void RotateToMovement()
        {
            Vector3 velocity = agent.desiredVelocity;
            velocity.y = 0f;

            if (velocity.sqrMagnitude < 0.01f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(
                velocity.normalized,
                Vector3.up
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                8f * Time.deltaTime
            );
        }

        private bool CanSeePlayer()
        {
            if (player.IsHidden)
                return false;

            Vector3 origin = transform.position + Vector3.up * 1.45f;
            Vector3 target = player.transform.position + Vector3.up * 1.1f;
            Vector3 toPlayer = target - origin;

            if (toPlayer.magnitude > sightDistance)
                return false;

            float angle = Vector3.Angle(transform.forward, toPlayer);

            if (angle > sightAngle * 0.5f)
                return false;

            if (Physics.Raycast(
                origin,
                toPlayer.normalized,
                out RaycastHit hit,
                sightDistance,
                sightMask,
                QueryTriggerInteraction.Ignore))
            {
                return hit.collider.GetComponentInParent<FirstPersonController>()
                       == player;
            }

            return false;
        }

        private void UpdateMovementAudio()
        {
            if (movementSounds == null || movementSounds.Length == 0)
                return;

            if (playerCamera == null)
                return;

            // Toca sons apenas enquanto o manequim está se movendo.
            if (agent.velocity.magnitude < movementThreshold)
                return;

            // Não toca o som quando o jogador está olhando para o manequim.
            if (IsPlayerLookingAtMannequin())
                return;

            if (Time.time < nextSoundTime)
                return;

            if (audioSource.isPlaying)
                return;

            AudioClip clip = movementSounds[
                Random.Range(0, movementSounds.Length)
            ];

            if (clip != null)
            {
                audioSource.volume = soundVolume;
                audioSource.PlayOneShot(clip);
            }

            nextSoundTime = Time.time + Random.Range(
                minSoundInterval,
                maxSoundInterval
            );
        }

        private bool IsPlayerLookingAtMannequin()
        {
            Vector3 directionToEnemy =
                transform.position - playerCamera.transform.position;

            float distance = directionToEnemy.magnitude;

            if (distance > soundDistance)
                return false;

            float angle = Vector3.Angle(
                playerCamera.transform.forward,
                directionToEnemy
            );

            if (angle > playerLookAngle)
                return false;

            if (Physics.Raycast(
                playerCamera.transform.position,
                directionToEnemy.normalized,
                out RaycastHit hit,
                distance,
                ~0,
                QueryTriggerInteraction.Ignore))
            {
                Transform hitTransform = hit.collider.transform;

                return hitTransform == transform ||
                       hitTransform.IsChildOf(transform);
            }

            return false;
        }

        private void TryCatchPlayer()
        {
            if (Vector3.Distance(
                transform.position,
                player.transform.position) > catchDistance)
                return;

            GameManager.Instance?.RespawnPlayer();
            ResetEnemy();
        }

        private void ResetEnemy()
        {
            if (enemySpawn != null)
            {
                WarpToNavMesh(enemySpawn.position);
                transform.rotation = enemySpawn.rotation;
            }

            state = EnemyState.Patrol;
            patrolIndex = 0;
            GoToCurrentPatrolPoint();
        }

        private void WarpToNavMesh(Vector3 position)
        {
            if (NavMesh.SamplePosition(
                position,
                out NavMeshHit hit,
                5f,
                WalkableAreaMask()))
            {
                agent.Warp(hit.position);
            }
        }

        private static int WalkableAreaMask()
        {
            int walkableArea = NavMesh.GetAreaFromName("Walkable");

            return walkableArea >= 0
                ? 1 << walkableArea
                : NavMesh.AllAreas;
        }
    }
}