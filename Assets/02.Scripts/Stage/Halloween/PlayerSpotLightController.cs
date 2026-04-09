using UnityEngine;

public class PlayerSpotLightController : MonoBehaviour
{
    [Header("추적 설정")]
    [SerializeField] private float _followSpeed = 5f;
    [SerializeField] private float _fixedHeight = 4f;

    private Transform targetPlayer;
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
            transform.position = new Vector3(targetPlayer.position.x, _fixedHeight, targetPlayer.position.z);
        }
    }

    private void Update()
    {
        if (!isActive || targetPlayer == null) return;

        Vector3 targetPosition = new Vector3(targetPlayer.position.x, _fixedHeight, targetPlayer.position.z);

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * _followSpeed);
        transform.position = new Vector3(transform.position.x, _fixedHeight, transform.position.z);
    }
}
