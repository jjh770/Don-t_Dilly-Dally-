using Photon.Pun;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(PhotonView))]
public class CreatureMove : MonoBehaviourPunCallbacks, IPunObservable
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

    [Header("네트워크 보간")]
    [SerializeField] private float _positionLerpSpeed = 10f;
    [SerializeField] private float _rotationLerpSpeed = 10f;

    private NavMeshAgent _agent;
    private Animator _animator;
    private PhotonView _photonView;
    private float _waitTimer;
    private float _stuckTimer;
    private bool _isWaiting;
    private Vector3 _lastPosition;

    private Vector3 _networkPosition;
    private Quaternion _networkRotation;
    private bool _networkIsWaiting;

    private static readonly int IsWait = Animator.StringToHash("IsWait");

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();
        _photonView = GetComponent<PhotonView>();
    }

    private void Start()
    {
        _networkPosition = transform.position;
        _networkRotation = transform.rotation;

        if (_agent != null)
        {
            _agent.speed = _moveSpeed;
            _agent.angularSpeed = _rotationSpeed * 100f;
            _agent.avoidancePriority = Random.Range(_minAvoidancePriority, _maxAvoidancePriority);
            _agent.updateRotation = !_smoothRotation;
            _lastPosition = transform.position;

            if (PhotonNetwork.IsMasterClient)
            {
                SetNewDestination();
            }
            else
            {
                _agent.enabled = false;
            }
        }
    }

    private void Update()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            UpdateMaster();
        }
        else
        {
            UpdateRemote();
        }
    }

    private void UpdateMaster()
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

    private void UpdateRemote()
    {
        transform.position = Vector3.Lerp(transform.position, _networkPosition, _positionLerpSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, _networkRotation, _rotationLerpSpeed * Time.deltaTime);

        if (_animator != null && _isWaiting != _networkIsWaiting)
        {
            _isWaiting = _networkIsWaiting;
            _animator.SetBool(IsWait, _isWaiting);
        }
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient && _agent != null)
        {
            _agent.enabled = true;
            _agent.Warp(transform.position);
            SetNewDestination();
        }
        else if (_agent != null)
        {
            _agent.enabled = false;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(_isWaiting);
        }
        else
        {
            _networkPosition = (Vector3)stream.ReceiveNext();
            _networkRotation = (Quaternion)stream.ReceiveNext();
            _networkIsWaiting = (bool)stream.ReceiveNext();
        }
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
