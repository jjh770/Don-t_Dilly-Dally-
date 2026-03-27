using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;



public class UI_HoverSlidePanel : MonoBehaviour, IPointerExitHandler, IPointerEnterHandler
{
    [Header("이동 설정")]
    [SerializeField] private RectTransform _targetUI;
    [SerializeField] private Vector2 _moveOffset = new Vector2(200f, 0f); // X축으로 200만큼 이동
    [SerializeField] private float _duration = 0.3f;  // 이동하는데 걸리는 시간 (0.3초)

    private Vector2 _originalPosition; // 원래 위치 기억용
    private Vector2 _targetPosition;   // 도착할 위치

    private bool _isOpened = false;

    void Awake()
    {
        if (_targetUI == null) _targetUI = GetComponent<RectTransform>();

        _originalPosition = _targetUI.anchoredPosition;

        _targetPosition = _originalPosition + _moveOffset;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_isOpened) return;
        _isOpened = true;
        _targetUI.DOKill(); 

        _targetUI.DOAnchorPos(_targetPosition, _duration).SetEase(Ease.OutCubic);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        bool isMouseInMenu = RectTransformUtility.RectangleContainsScreenPoint(
            _targetUI,
            Input.mousePosition,
            null
        );


        if (isMouseInMenu) return;
        _isOpened = false;

        _targetUI.DOKill();

        _targetUI.DOAnchorPos(_originalPosition, _duration).SetEase(Ease.OutCubic);

    }
}