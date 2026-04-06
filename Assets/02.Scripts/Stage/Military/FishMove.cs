using UnityEngine;
using UnityEngine.AI;

public class FishMove : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 3f;

    [Header("Direction Change")]
    [SerializeField] private float directionChangeInterval = 2f;
    [SerializeField] private float maxTurnAngle = 30f;

    private NavMeshAgent agent;
    private float directionTimer;
    private float targetRotationY;
    private float currentRotationY;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.angularSpeed = 0f;
            agent.updateRotation = false;
            currentRotationY = transform.eulerAngles.y;
            targetRotationY = currentRotationY;
        }
    }

    private void Update()
    {
        if (agent == null) return;

        directionTimer += Time.deltaTime;
        if (directionTimer >= directionChangeInterval)
        {
            SetNewDirection();
            directionTimer = 0f;
        }

        if (IsPathBlocked())
        {
            TurnAway();
        }

        currentRotationY = Mathf.LerpAngle(currentRotationY, targetRotationY, rotationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, currentRotationY, 0f);

        Vector3 destination = transform.position + Quaternion.Euler(0f, currentRotationY, 0f) * Vector3.forward * 10f;
        agent.SetDestination(destination);
    }

    private bool IsPathBlocked()
    {
        if (agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathPartial ||
            agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid)
        {
            return true;
        }

        if (agent.hasPath && agent.remainingDistance > 0.1f && agent.velocity.magnitude < 0.1f)
        {
            return true;
        }

        return false;
    }

    private void TurnAway()
    {
        targetRotationY += Random.Range(90f, 180f) * (Random.value > 0.5f ? 1f : -1f);
        directionTimer = 0f;
    }

    private void SetNewDirection()
    {
        float randomAngle = Random.Range(-maxTurnAngle, maxTurnAngle);
        targetRotationY += randomAngle;
    }
}
