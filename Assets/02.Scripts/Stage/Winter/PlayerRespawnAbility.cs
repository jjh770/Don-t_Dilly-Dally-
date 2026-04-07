using UnityEngine;
using System.Collections;
using Photon.Pun;
using DontDillyDally.StageFlow;

public class PlayerRespawnAbility : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private float _respawnDelay = 3f;
    [SerializeField] private string _boundaryTag = "Boundary";
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private float _overlapCheckRadius = 1f;
    [SerializeField] private float _randomOffsetRange = 1.5f;

    [Header("물에 빠지는 연출")]
    [SerializeField] private float _sinkForce = 5f;

    private PhotonView _photonView;
    private Rigidbody _rigidbody;
    private PlayerMovementAbility _movementAbility;
    private RigidbodyConstraints _originalConstraints;
    private bool _isRespawning;

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        _rigidbody = GetComponent<Rigidbody>();
        _movementAbility = GetComponent<PlayerMovementAbility>();

        if (_rigidbody != null)
        {
            _originalConstraints = _rigidbody.constraints;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_photonView != null && !_photonView.IsMine) return;
        if (_isRespawning) return;

        if (other.CompareTag(_boundaryTag))
        {
            StartSinking();
            StartCoroutine(RespawnAfterDelay());
        }
    }

    private void StartSinking()
    {
        Debug.Log("StartSinking");

        // 이동 잠금
        if (_movementAbility != null)
        {
            _movementAbility.SetMovementLocked(true);
        }

        // Y축 고정 해제 (Rotation만 유지)
        if (_rigidbody != null)
        {
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            _rigidbody.AddForce(Vector3.down * _sinkForce, ForceMode.Impulse);
        }
    }

    private IEnumerator RespawnAfterDelay()
    {
        _isRespawning = true;

        yield return new WaitForSeconds(_respawnDelay);

        // 리스폰 위치로 이동
        Transform respawnPoint = GetRespawnPointByRole();
        if (respawnPoint != null)
        {
            // 속도 초기화
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
            }

            transform.position = GetPositionWithOffset(respawnPoint.position);
            transform.rotation = respawnPoint.rotation;
        }

        // constraints 복원
        if (_rigidbody != null)
        {
            _rigidbody.constraints = _originalConstraints;
        }

        // 이동 잠금 해제
        if (_movementAbility != null)
        {
            _movementAbility.SetMovementLocked(false);
        }

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

        Vector2 randomOffset = Random.insideUnitCircle * _randomOffsetRange;
        return basePosition + new Vector3(randomOffset.x, 0f, randomOffset.y);
    }
}
