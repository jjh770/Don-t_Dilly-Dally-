using System.Collections.Generic;
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
    [Tooltip("최종 splash 생성 Y에 더해지는 오프셋 (양수 = 위로).")]
    [SerializeField] private float _splashYOffset = 0f;

    [Header("중복 방지")]
    [Tooltip("같은 플레이어에 대해 이 시간 내 재발동을 무시한다. 원격 클라이언트에서 리스폰 Lerp 경로가 수면을 관통할 때 중복 splash를 방지.")]
    [SerializeField, Min(0f)] private float _perPlayerCooldown = 10f;

    private readonly Dictionary<Collider, float> _lastSplashTime = new();

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
        if (!other.CompareTag(_playerTag)) return;
        if (IsInCooldown(other)) return;

        _lastSplashTime[other] = Time.time;

        SoundManager.Instance.Play(SFXKey.PlayerWaterSplash, SoundType.Local);

        if (_splashPrefab == null) return;

        Vector3 playerPos = other.transform.position;
        float baseY = _surfaceAnchor != null
            ? _surfaceAnchor.position.y
            : transform.position.y;

        Vector3 spawnPos = new Vector3(playerPos.x, baseY + _splashYOffset, playerPos.z);
        GameObject fx = Instantiate(_splashPrefab, spawnPos, Quaternion.identity);
        FxHelper.PlayAll(fx);
        Destroy(fx, _splashLifetime);
    }

    private bool IsInCooldown(Collider other)
    {
        if (!_lastSplashTime.TryGetValue(other, out float lastTime))
        {
            return false;
        }
        return Time.time - lastTime < _perPlayerCooldown;
    }
}
