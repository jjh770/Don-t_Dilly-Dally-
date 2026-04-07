using UnityEngine;

public class PlayerSpotLightController : MonoBehaviour
{
    [Header("추적 설정")]
    [SerializeField] private float followSpeed = 3f;
    [SerializeField] private float heightOffset = 4f;

    private Transform targetPlayer;
    private Vector3 currentVelocity;
    private bool isActive;

    public void SetTarget(Transform player)
    {
        targetPlayer = player;
    }

    public void SetActive(bool active)
    {
        isActive = active;
        gameObject.SetActive(active);

        if (active && targetPlayer != null)
        {
            // 활성화 시 즉시 플레이어 위치로 이동
            transform.position = targetPlayer.position + Vector3.up * heightOffset;
        }
    }

    private void Update()
    {
        if (!isActive || targetPlayer == null) return;

        Vector3 targetPosition = targetPlayer.position + Vector3.up * heightOffset;

        // Slerp를 사용한 부드러운 추적
        Vector3 direction = targetPosition - transform.position;
        if (direction.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            Vector3 slerpedDirection = Vector3.Slerp(
                transform.position - targetPlayer.position,
                targetPosition - targetPlayer.position,
                Time.deltaTime * followSpeed
            );
            transform.position = targetPlayer.position + slerpedDirection.normalized * direction.magnitude;

            // 더 간단한 방식: SmoothDamp 사용
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref currentVelocity,
                1f / followSpeed
            );
        }
    }
}
