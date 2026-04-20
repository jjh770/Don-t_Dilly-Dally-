using UnityEngine;
using System;
using System.Collections;
using Photon.Pun;
using DontDillyDally.StageFlow;

public class PlayerRespawnAbility : MonoBehaviour
{
    public static PlayerRespawnAbility LocalInstance { get; private set; }

    public event Action OnRespawnStarted;
    public event Action<float> OnRespawnCountdown;
    public event Action OnRespawnEnded;

    [Header("설정")]
    [SerializeField] private float _respawnDelay = 3f;
    [SerializeField] private string _safeZoneTag = "SafeZone";
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private float _overlapCheckRadius = 1f;
    [SerializeField] private float _randomOffsetRange = 1.5f;

    [Header("물에 빠지는 연출")]
    [SerializeField] private float _sinkDuration = 1.5f;
    [SerializeField] private float _sinkSpeed = 3f;

    [Header("리스폰 이펙트")]
    [SerializeField] private ParticleSystem _respawnFx;
    [SerializeField] private float _respawnFxDuration = 1f;

    private PhotonView _photonView;
    private Rigidbody _rigidbody;
    private PlayerMovementAbility _movementAbility;
    private RigidbodyConstraints _originalConstraints;
    private bool _isRespawning;
    private bool _isTeleporting;
    private int _safeZoneCount;
    private readonly object _movementLockSource = new();

    public bool IsTeleporting => _isTeleporting;

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        _rigidbody = GetComponent<Rigidbody>();
        _movementAbility = GetComponent<PlayerMovementAbility>();

        if (_rigidbody != null)
        {
            _originalConstraints = _rigidbody.constraints;
        }

        if (_photonView != null && _photonView.IsMine)
        {
            LocalInstance = this;
        }
    }

    private void OnDestroy()
    {
        if (LocalInstance == this)
        {
            LocalInstance = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_photonView != null && !_photonView.IsMine) return;

        if (other.CompareTag(_safeZoneTag))
        {
            _safeZoneCount++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (_photonView != null && !_photonView.IsMine) return;

        if (other.CompareTag(_safeZoneTag))
        {
            _safeZoneCount--;

            if (_safeZoneCount <= 0 && !_isRespawning)
            {
                _safeZoneCount = 0;
                StartCoroutine(SinkAndRespawn());
            }
        }
    }

    private IEnumerator SinkAndRespawn()
    {
        _isRespawning = true;

        // 1. 싱크 시작
        if (_movementAbility != null)
        {
            _movementAbility.SetMovementLocked(_movementLockSource, true);
        }

        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;
        }

        // 2. 싱크 연출 - 강제로 아래로 이동 
        float elapsed = 0f;
        while (elapsed < _sinkDuration)
        {
            transform.position += Vector3.down * _sinkSpeed * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 3. 리스폰 대기
        OnRespawnStarted?.Invoke();

        float remaining = _respawnDelay;
        while (remaining > 0f)
        {
            OnRespawnCountdown?.Invoke(remaining);
            yield return null;
            remaining -= Time.deltaTime;
        }

        // 4. 리스폰 처리
        Transform respawnPoint = GetRespawnPointByRole();
        if (respawnPoint != null)
        {
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
            }

            // 순간이동은 Unity가 OnTriggerEnter로 감지하므로 guard 플래그를 세운다.
            // Water Collider와 겹치는 리스폰 포인트에서 잘못된 splash가 나오는 것을 방지.
            _isTeleporting = true;
            transform.position = GetPositionWithOffset(respawnPoint.position);
            transform.rotation = respawnPoint.rotation;

            // Physics가 teleport로 인한 trigger 이벤트를 처리한 뒤 해제한다.
            yield return new WaitForFixedUpdate();
            _isTeleporting = false;
        }

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = false;
            _rigidbody.constraints = _originalConstraints;
        }

        if (_movementAbility != null)
        {
            _movementAbility.SetMovementLocked(_movementLockSource, false);
        }

        _photonView.RPC(nameof(RPC_PlayRespawnFx), RpcTarget.All);
        OnRespawnEnded?.Invoke();
        _isRespawning = false;
    }

    private Transform GetRespawnPointByRole()
    {
        if (StageSceneConfig.Instance == null)
        {
            Debug.LogWarning("[RespawnAbility] StageSceneConfig 인스턴스가 없습니다.");
            return null;
        }

        RoleType role = RoleProperties.GetPlayerRole(PhotonNetwork.LocalPlayer);
        return StageSceneConfig.Instance.GetAvailableRespawnPoint(role, IsPointOccupied);
    }

    private bool IsPointOccupied(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, _overlapCheckRadius);
        foreach (Collider col in colliders)
        {
            if (col.gameObject != gameObject && col.CompareTag(_playerTag))
            {
                return true;
            }
        }
        return false;
    }

    private Vector3 GetPositionWithOffset(Vector3 basePosition)
    {
        if (!IsPointOccupied(basePosition))
        {
            return basePosition;
        }

        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * _randomOffsetRange;
        return basePosition + new Vector3(randomOffset.x, 0f, randomOffset.y);
    }

    [PunRPC]
    private void RPC_PlayRespawnFx()
    {
        StartCoroutine(PlayRespawnFxCoroutine());
    }

    private IEnumerator PlayRespawnFxCoroutine()
    {
        if (_respawnFx == null) yield break;

        FxHelper.Play(_respawnFx);
        yield return new WaitForSeconds(_respawnFxDuration);
        FxHelper.Stop(_respawnFx);
    }
}
