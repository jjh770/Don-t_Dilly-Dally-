using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class UI_ButtonHoverBubble : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("스케일 설정")]
    [SerializeField] private float _hoverScale = 1.1f;
    [SerializeField] private float _scaleInDuration = 0.25f;
    [SerializeField] private float _scaleOutDuration = 0.2f;
    [SerializeField] private Ease _scaleInEase = Ease.OutBack;
    [SerializeField] private Ease _scaleOutEase = Ease.OutCubic;

    [Header("부들부들 떨림 (Hover 진입 시 1회)")]
    [SerializeField] private bool _useTremble = false;
    [SerializeField] private float _trembleAngle = 5f;
    [SerializeField] private float _trembleDuration = 0.3f;
    [SerializeField] private int _trembleVibrato = 12;

    private RectTransform _rectTransform;
    private Vector3 _originalScale;
    private Quaternion _originalRotation;
    private Tween _scaleTween;
    private Tween _trembleTween;
    private Tween _rotationResetTween;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _originalScale = _rectTransform.localScale;
        _originalRotation = _rectTransform.localRotation;
    }

    private void OnDisable()
    {
        KillTweens();
        _rectTransform.localScale = _originalScale;
        _rectTransform.localRotation = _originalRotation;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        KillTweens();

        _scaleTween = _rectTransform
            .DOScale(_originalScale * _hoverScale, _scaleInDuration)
            .SetEase(_scaleInEase);

        if (_useTremble)
        {
            _trembleTween = _rectTransform
                .DOShakeRotation(_trembleDuration, new Vector3(0f, 0f, _trembleAngle), _trembleVibrato);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        KillTweens();

        _scaleTween = _rectTransform
            .DOScale(_originalScale, _scaleOutDuration)
            .SetEase(_scaleOutEase);

        // Shake 중단 시 남아있을 수 있는 잔여 회전값을 부드럽게 원복.
        if (_useTremble)
        {
            _rotationResetTween = _rectTransform
                .DOLocalRotateQuaternion(_originalRotation, _scaleOutDuration)
                .SetEase(_scaleOutEase);
        }
    }

    private void KillTweens()
    {
        _scaleTween?.Kill();
        _trembleTween?.Kill();
        _rotationResetTween?.Kill();
    }
}
