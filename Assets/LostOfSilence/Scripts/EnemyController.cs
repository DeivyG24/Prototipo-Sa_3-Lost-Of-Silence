using UnityEngine;
using UnityEngine.AI;

namespace LostOfSilence
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class EnemyController : MonoBehaviour
    {
        [SerializeField] private FirstPersonController player;
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private Transform enemySpawn;
        [SerializeField] private float patrolSpeed = 1.35f;
        [SerializeField] private float chaseSpeed = 2.9f;
        [SerializeField] private float sightDistance = 8.5f;
        [SerializeField] private float sightAngle = 75f;
        [SerializeField] private float catchDistance = 1.25f;
        [SerializeField] private float searchDuration = 3f;
        [SerializeField] private LayerMask sightMask = ~0;

        private NavMeshAgent agent;
        private int patrolIndex;
        private float searchTimer;
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
            agent.updateRotation = false;
            agent.acceleration = 12f;
            agent.angularSpeed = 540f;
            agent.stoppingDistance = 0.35f;
            agent.autoRepath = true;
        }

        private void Start()
        {
            if (enemySpawn == null)
            {
                enemySpawn = transform;
            }

            if (player == null)
            {
                player = FindFirstObjectByType<FirstPersonController>();
            }

            WarpToNavMesh(transform.position);
            GoToCurrentPatrolPoint();
        }

        private void Update()
        {
            if (player == null)
            {
                return;
            }

            if (GameManager.Instance != null && !GameManager.Instance.GameplayInputActive)
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
        }

        private void Patrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                return;
            }

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
            {
                return;
            }

            SetDestination(patrolPoints[patrolIndex].position);
        }

        private bool SetDestination(Vector3 position)
        {
            if (!agent.isOnNavMesh)
            {
                WarpToNavMesh(transform.position);
            }

            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 2.5f, WalkableAreaMask()))
            {
                agent.SetDestination(hit.position);
                return true;
            }

            return false;
        }

        private bool HasReachedDestination()
        {
            if (agent.pathPending)
            {
                return false;
            }

            return agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, 0.45f);
        }

        private void RotateToMovement()
        {
            Vector3 velocity = agent.desiredVelocity;
            velocity.y = 0f;

            if (velocity.sqrMagnitude < 0.01f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 8f * Time.deltaTime);
        }

        private bool CanSeePlayer()
        {
            if (player.IsHidden)
            {
                return false;
            }

            Vector3 origin = transform.position + Vector3.up * 1.45f;
            Vector3 target = player.transform.position + Vector3.up * 1.1f;
            Vector3 toPlayer = target - origin;

            if (toPlayer.magnitude > sightDistance)
            {
                return false;
            }

            float angle = Vector3.Angle(transform.forward, toPlayer);
            if (angle > sightAngle * 0.5f)
            {
                return false;
            }

            if (Physics.Raycast(origin, toPlayer.normalized, out RaycastHit hit, sightDistance, sightMask, QueryTriggerInteraction.Ignore))
            {
                return hit.collider.GetComponentInParent<FirstPersonController>() == player;
            }

            return false;
        }

        private void TryCatchPlayer()
        {
            if (Vector3.Distance(transform.position, player.transform.position) > catchDistance)
            {
                return;
            }

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
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 5f, WalkableAreaMask()))
            {
                agent.Warp(hit.position);
            }
        }

        private static int WalkableAreaMask()
        {
            int walkableArea = NavMesh.GetAreaFromName("Walkable");
            return walkableArea >= 0 ? 1 << walkableArea : NavMesh.AllAreas;
        }
    }
}
