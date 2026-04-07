using UnityEngine;

public class PlayerSpotLightController : MonoBehaviour
{
    [Header("추적 설정")]
    [SerializeField] private float _followSpeed = 3f;
    [SerializeField] private float _heightOffset = 4f;

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
            transform.position = targetPlayer.position + Vector3.up * _heightOffset;
        }
    }

    private void Update()
    {
        if (!isActive || targetPlayer == null) return;

        Vector3 targetPosition = targetPlayer.position + Vector3.up * _heightOffset;

        Vector3 direction = targetPosition - transform.position;
        if (direction.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            Vector3 slerpedDirection = Vector3.Slerp(
                transform.position - targetPlayer.position,
                targetPosition - targetPlayer.position,
                Time.deltaTime * _followSpeed
            );
            transform.position = targetPlayer.position + slerpedDirection.normalized * direction.magnitude;

            /*
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref currentVelocity,
                1f / _followSpeed
            );
            */
        }
    }
}
