using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MonsterAI : NetworkBehaviour
{
    public enum State { Patrol, Chase }

    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float waypointTolerance = 0.2f;

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 6f;
    [SerializeField] private float loseInterestRadius = 9f;
    [SerializeField] private LayerMask sightBlockers;

    // Resolved at spawn from the scene's PatrolRoute; a prefab cannot hold
    // references to scene objects.
    private Transform[] waypoints;

    // Replicated so clients can react to state (audio, animation) later.
    private readonly NetworkVariable<State> currentState =
        new NetworkVariable<State>(State.Patrol);

    private Rigidbody2D rb;
    private IPathfinder pathfinder;
    private int waypointIndex;
    private Transform chaseTarget;

    public State CurrentState => currentState.Value;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        pathfinder = new DirectPathfinder();
    }

    public override void OnNetworkSpawn()
    {
        // Host simulates the monster; clients only receive the result.
        rb.bodyType = IsServer ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;

        if (!IsServer) return;

        if (PatrolRoute.Instance != null)
        {
            waypoints = PatrolRoute.Instance.GetWaypoints();
        }

        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning("MonsterAI found no PatrolRoute; it will stand still.");
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        switch (currentState.Value)
        {
            case State.Patrol:
                TickPatrol();
                break;
            case State.Chase:
                TickChase();
                break;
        }
    }

    private void TickPatrol()
    {
        Transform player = FindVisiblePlayer(detectionRadius);
        if (player != null)
        {
            chaseTarget = player;
            currentState.Value = State.Chase;
            return;
        }

        if (waypoints == null || waypoints.Length == 0)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Transform waypoint = waypoints[waypointIndex];
        if (Vector2.Distance(rb.position, waypoint.position) <= waypointTolerance)
        {
            waypointIndex = (waypointIndex + 1) % waypoints.Length;
            waypoint = waypoints[waypointIndex];
        }

        MoveToward(waypoint.position, patrolSpeed);
    }

    private void TickChase()
    {
        if (chaseTarget == null)
        {
            currentState.Value = State.Patrol;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float distance = Vector2.Distance(rb.position, chaseTarget.position);
        if (distance > loseInterestRadius || !HasLineOfSight(chaseTarget))
        {
            chaseTarget = null;
            currentState.Value = State.Patrol;
            return;
        }

        MoveToward(chaseTarget.position, chaseSpeed);
    }

    private void MoveToward(Vector2 destination, float speed)
    {
        Vector2 step = pathfinder.GetNextStep(rb.position, destination);
        Vector2 direction = (step - rb.position).normalized;
        rb.linearVelocity = direction * speed;
    }

    private Transform FindVisiblePlayer(float radius)
    {
        Transform closest = null;
        float closestDistance = float.MaxValue;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject;
            if (playerObject == null) continue;

            float distance = Vector2.Distance(rb.position, playerObject.transform.position);
            if (distance > radius || distance >= closestDistance) continue;
            if (!HasLineOfSight(playerObject.transform)) continue;

            closest = playerObject.transform;
            closestDistance = distance;
        }

        return closest;
    }

    private bool HasLineOfSight(Transform target)
    {
        Vector2 origin = rb.position;
        Vector2 toTarget = (Vector2)target.position - origin;
        RaycastHit2D hit = Physics2D.Raycast(origin, toTarget.normalized, toTarget.magnitude, sightBlockers);
        return hit.collider == null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, loseInterestRadius);

        if (waypoints == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawWireCube(waypoints[i].position, Vector3.one * 0.3f);
            Transform next = waypoints[(i + 1) % waypoints.Length];
            if (next != null) Gizmos.DrawLine(waypoints[i].position, next.position);
        }
    }
}
