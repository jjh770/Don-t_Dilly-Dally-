using UnityEngine;

public class PlayerBoundaryAbility : PlayerAbility
{
    [SerializeField] private Camera _camera;
    [SerializeField] private float _margin = 0.05f;

    protected override void Awake()
    {
        base.Awake();
        if (_camera == null)
        {
            _camera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        if (_camera == null) return;
        if (_owner.PhotonView != null && !_owner.PhotonView.IsMine) return;

        Vector3 viewportPos = _camera.WorldToViewportPoint(transform.position);

        viewportPos.x = Mathf.Clamp(viewportPos.x, _margin, 1f - _margin);
        viewportPos.y = Mathf.Clamp(viewportPos.y, _margin, 1f - _margin);

        Vector3 clampedWorldPos = _camera.ViewportToWorldPoint(viewportPos);

        clampedWorldPos.y = transform.position.y;

        transform.position = clampedWorldPos;
    }
}
