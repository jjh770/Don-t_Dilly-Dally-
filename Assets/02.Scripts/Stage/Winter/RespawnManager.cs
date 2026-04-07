using UnityEngine;
using System.Collections;
using Photon.Pun;
using DontDillyDally.StageFlow;

public class RespawnManager : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private float _respawnDelay = 3f;
    [SerializeField] private string _boundaryTag = "Boundary";
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private float _overlapCheckRadius = 1f;
    [SerializeField] private float _randomOffsetRange = 1.5f;

    private PhotonView _photonView;
    private bool _isRespawning;

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_photonView != null && !_photonView.IsMine) return;
        if (_isRespawning) return;

        if (other.CompareTag(_boundaryTag))
        {
            StartCoroutine(RespawnAfterDelay());
        }
    }

    private IEnumerator RespawnAfterDelay()
    {
        _isRespawning = true;

        yield return new WaitForSeconds(_respawnDelay);

        Transform respawnPoint = GetRespawnPointByRole();
        if (respawnPoint != null)
        {
            transform.position = GetPositionWithOffset(respawnPoint.position);
            transform.rotation = respawnPoint.rotation;
        }

        _isRespawning = false;
    }

    private Transform GetRespawnPointByRole()
    {
        if (StageSceneConfig.Instance == null)
        {
            Debug.LogWarning("[RespawnManager] StageSceneConfig 인스턴스가 없습니다.");
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
