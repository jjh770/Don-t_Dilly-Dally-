using UnityEngine;
using UnityEngine.UI;

public class CharacterRotate : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private LayerMask _mask;
    [SerializeField] private float _raycastDistance = 100f;

    [Header("Target Settings")]
    [SerializeField] private RawImage _renderTextureImage;
    [SerializeField] private Camera _renderCamera;

    [Header("Rotation Settings")]
    [SerializeField] private float _rotationSpeed = 200f;

    private float _yaw = 0f;
    private bool _isRotating = false;
    private Transform _target;
    private RectTransform _renderTextureRect;

    private void Awake()
    {
        if (_renderTextureImage != null)
        {
            _renderTextureRect = _renderTextureImage.rectTransform;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseDown();
        }
        else if (Input.GetMouseButton(0))
        {
            HandleMouseDrag();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            HandleMouseUp();
        }
    }

    private void HandleMouseDown()
    {
        if (!TryGetRenderTextureRay(out Ray ray)) return;

        if (Physics.Raycast(ray, out RaycastHit hit, _raycastDistance, _mask))
        {
            _isRotating = true;
            _target = hit.transform;
        }
    }

    private void HandleMouseDrag()
    {
        if (!_isRotating || _target == null)
        {
            return;
        }

        float mouseX = Input.GetAxis("Mouse X");
        _yaw -= _rotationSpeed * Time.deltaTime * mouseX;
        _target.localRotation = Quaternion.Euler(0f, _yaw, 0f);
    }

    private void HandleMouseUp()
    {
        _isRotating = false;
        _target = null;
    }

    private bool TryGetRenderTextureRay(out Ray ray)
    {
        ray = default;

        if (_renderTextureRect == null) return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _renderTextureRect, Input.mousePosition, null, out Vector2 localPoint))
        {
            return false;
        }

        Vector2 viewport = new Vector2(
            (localPoint.x / _renderTextureRect.sizeDelta.x) + 0.5f,
            (localPoint.y / _renderTextureRect.sizeDelta.y) + 0.5f
        );

        if (viewport.x < 0 || viewport.x > 1 || viewport.y < 0 || viewport.y > 1)
        {
            return false;
        }

        ray = _renderCamera.ViewportPointToRay(viewport);
        return true;
    }
}