using UnityEngine;
using UnityEngine.AI;

public class PenguinMove : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float wanderRadius = 10f;
    [SerializeField] private float minWaitTime = 1f;
    [SerializeField] private float maxWaitTime = 3f;

    [Header("충돌 회피")]
    [SerializeField] private float stuckCheckInterval = 0.5f;
    [SerializeField] private float stuckThreshold = 0.1f;
    [SerializeField] private int minAvoidancePriority = 30;
    [SerializeField] private int maxAvoidancePriority = 70;

    private NavMeshAgent agent;
    private Animator animator;
    private float waitTimer;
    private float stuckTimer;
    private bool isWaiting;
    private Vector3 lastPosition;

    private static readonly int IsWait = Animator.StringToHash("IsWait");

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.angularSpeed = rotationSpeed * 100f;
            agent.avoidancePriority = Random.Range(minAvoidancePriority, maxAvoidancePriority);
            lastPosition = transform.position;
            SetNewDestination();
        }
    }

    private void Update()
    {
        if (agent == null) return;

        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                animator?.SetBool(IsWait, false);
                SetNewDestination();
            }
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            StartWaiting();
            return;
        }

        stuckTimer += Time.deltaTime;
        if (stuckTimer >= stuckCheckInterval)
        {
            float distanceMoved = Vector3.Distance(transform.position, lastPosition);
            if (distanceMoved < stuckThreshold && agent.hasPath)
            {
                SetNewDestination();
            }
            lastPosition = transform.position;
            stuckTimer = 0f;
        }
    }

    private void SetNewDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private void StartWaiting()
    {
        isWaiting = true;
        waitTimer = Random.Range(minWaitTime, maxWaitTime);
        animator?.SetBool(IsWait, true);
    }
}
