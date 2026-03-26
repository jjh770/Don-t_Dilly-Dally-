using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CharacterRotate : MonoBehaviour
{
    [SerializeField]
    private LayerMask _mask;
    [SerializeField] private RawImage _renderTextureImage;
    
    [SerializeField]
    private Camera _renderCamera;

    public float RotationSpeed = 200f;
    private float yaw = 0f;

    private bool _isRotating = false;

    private GameObject _target;


    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!TryGetRenderTextureRay(out Ray ray)) return;

            if (Physics.Raycast(ray, out RaycastHit hit, 100.0f, _mask))
            {
                _isRotating = true;
                _target = hit.collider.gameObject;
            }
        }
        if (Input.GetMouseButton(0))
        {
            if (!_isRotating)
            {
                return;
            }
            float mouseX = Input.GetAxis("Mouse X");

            yaw -= mouseX * RotationSpeed * Time.deltaTime;

            _target.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }
        if (Input.GetMouseButtonUp(0))
        {
            _isRotating = false;
            _target = null;
        }
    }

    private bool TryGetRenderTextureRay(out Ray ray)
    {
        ray = default;

        // RawImage 기준 마우스 위치를 UV로 변환
        RectTransform rect = _renderTextureImage.rectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rect, Input.mousePosition, null, out Vector2 localPoint))
            return false;

        // -0.5 ~ 0.5 → 0 ~ 1 뷰포트 좌표로 변환
        Vector2 viewport = new Vector2(
            (localPoint.x / rect.sizeDelta.x) + 0.5f,
            (localPoint.y / rect.sizeDelta.y) + 0.5f
        );

        // 뷰포트 범위 밖이면 무시
        if (viewport.x < 0 || viewport.x > 1 || viewport.y < 0 || viewport.y > 1)
            return false;

        // 렌더카메라 기준 Ray 생성
        ray = _renderCamera.ViewportPointToRay(viewport);
        return true;
    }
}