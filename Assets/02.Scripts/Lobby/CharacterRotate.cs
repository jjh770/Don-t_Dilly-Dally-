using UnityEngine;
using UnityEngine.UI;

public class CharacterRotate : MonoBehaviour
{
    [SerializeField]
    private LayerMask _mask;
    [SerializeField] private RawImage _renderTextureImage;
    
    [SerializeField]
    private Camera _renderCamera;

    [SerializeField]
    private float _rotationSpeed = 200f;
    private float _yaw = 0f;

    private bool _isRotating = false;

    private Transform _target;


    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!TryGetRenderTextureRay(out Ray ray)) return;

            if (Physics.Raycast(ray, out RaycastHit hit, 100.0f, _mask))
            {
                _isRotating = true;
                _target = hit.collider.gameObject.transform;
            }
        }
        else if (Input.GetMouseButton(0))
        {
            if (!_isRotating || _target == null)
            {
                return;
            }
            float mouseX = Input.GetAxis("Mouse X");

            _yaw -= _rotationSpeed * Time.deltaTime * mouseX;

            _target.transform.localRotation = Quaternion.Euler(0f, _yaw, 0f);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            _isRotating = false;
            _target = null;
        }
    }

    private bool TryGetRenderTextureRay(out Ray ray)
    {
        ray = default;

        RectTransform rect = _renderTextureImage.rectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rect, Input.mousePosition, null, out Vector2 localPoint))
            return false;

        Vector2 viewport = new Vector2(
            (localPoint.x / rect.sizeDelta.x) + 0.5f,
            (localPoint.y / rect.sizeDelta.y) + 0.5f
        );

        if (viewport.x < 0 || viewport.x > 1 || viewport.y < 0 || viewport.y > 1)
            return false;

        ray = _renderCamera.ViewportPointToRay(viewport);
        return true;
    }
}