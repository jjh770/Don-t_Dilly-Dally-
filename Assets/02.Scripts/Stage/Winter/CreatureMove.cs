using UnityEngine;
using UnityEngine.AI;

public class CreatureMove : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float _moveSpeed = 3.5f;
    [SerializeField] private float _rotationSpeed = 5f;
    [SerializeField] private float _wanderRadius = 10f;
    [SerializeField] private float _minWaitTime = 1f;
    [SerializeField] private float _maxWaitTime = 3f;

    [Header("충돌 회피")]
    [SerializeField] private float _stuckCheckInterval = 0.5f;
    [SerializeField] private float _stuckThreshold = 0.1f;
    [SerializeField] private int _minAvoidancePriority = 30;
    [SerializeField] private int _maxAvoidancePriority = 70;

    [Header("회전")]
    [SerializeField] private bool _smoothRotation = false;
    [SerializeField] private float _smoothRotationSpeed = 5f;

    private NavMeshAgent _agent;
    private Animator _animator;
    private float _waitTimer;
    private float _stuckTimer;
    private bool _isWaiting;
    private Vector3 _lastPosition;

    private static readonly int IsWait = Animator.StringToHash("IsWait");

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        if (_agent != null)
        {
            _agent.speed = _moveSpeed;
            _agent.angularSpeed = _rotationSpeed * 100f;
            _agent.avoidancePriority = Random.Range(_minAvoidancePriority, _maxAvoidancePriority);
            _agent.updateRotation = !_smoothRotation;
            _lastPosition = transform.position;
            SetNewDestination();
        }
    }

    private void Update()
    {
        if (_agent == null) return;

        if (_isWaiting)
        {
            UpdateWaiting();
            return;
        }

        if (HasReachedDestination())
        {
            StartWaiting();
            return;
        }

        CheckStuck();
        UpdateSmoothRotation();
    }

    private void UpdateWaiting()
    {
        _waitTimer -= Time.deltaTime;
        if (_waitTimer <= 0f)
        {
            _isWaiting = false;
            _animator?.SetBool(IsWait, false);
            SetNewDestination();
        }
    }

    private bool HasReachedDestination()
    {
        return !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance;
    }

    private void CheckStuck()
    {
        _stuckTimer += Time.deltaTime;
        if (_stuckTimer >= _stuckCheckInterval)
        {
            float distanceMoved = Vector3.Distance(transform.position, _lastPosition);
            if (distanceMoved < _stuckThreshold && _agent.hasPath)
            {
                SetNewDestination();
            }
            _lastPosition = transform.position;
            _stuckTimer = 0f;
        }
    }

    private void UpdateSmoothRotation()
    {
        if (_smoothRotation && _agent.velocity.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_agent.velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _smoothRotationSpeed * Time.deltaTime);
        }
    }

    private void SetNewDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * _wanderRadius;
        randomDirection += transform.position;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, _wanderRadius, NavMesh.AllAreas))
        {
            _agent.SetDestination(hit.position);
        }
    }

    private void StartWaiting()
    {
        _isWaiting = true;
        _waitTimer = Random.Range(_minWaitTime, _maxWaitTime);
        _animator?.SetBool(IsWait, true);
    }
}
