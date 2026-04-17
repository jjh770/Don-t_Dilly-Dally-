using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WaterSurface : MonoBehaviour
{
    [Header("Splash VFX")]
    [SerializeField] private GameObject _splashPrefab;
    [SerializeField] private string _playerTag = "Player";
    [SerializeField, Min(0f)] private float _splashLifetime = 3f;

    [Header("수면 높이 보정")]
    [Tooltip("비워두면 이 Transform의 Y를 수면 높이로 사용한다.")]
    [SerializeField] private Transform _surfaceAnchor;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_splashPrefab == null) return;
        if (!other.CompareTag(_playerTag)) return;

        Vector3 playerPos = other.transform.position;
        float surfaceY = _surfaceAnchor != null
            ? _surfaceAnchor.position.y
            : transform.position.y;

        Vector3 spawnPos = new Vector3(playerPos.x, surfaceY, playerPos.z);
        GameObject fx = Instantiate(_splashPrefab, spawnPos, Quaternion.identity);
        FxHelper.PlayAll(fx);
        Destroy(fx, _splashLifetime);
    }
}
