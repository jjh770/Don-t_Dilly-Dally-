using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class LobbyCustomizingTransition : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform _lobbyMainPanel;
    [SerializeField] private RectTransform _customizingPanel;
    [SerializeField] private RectTransform _previewRawImage;
    [SerializeField] private CanvasGroup _lobbyMainCanvasGroup;

    [Header("Transition Settings")]
    [SerializeField] private float _transitionDuration = 0.4f;
    [SerializeField] private Ease _easeType = Ease.OutQuad;

    [Header("Preview Transform - Customizing Mode")]
    [SerializeField] private Vector2 _previewCustomizingPosition = new Vector2(-200f, 0f);
    [SerializeField] private Vector2 _previewCustomizingScale = new Vector2(1.2f, 1.2f);

    [Header("Panel Slide")]
    [SerializeField] private float _customizingPanelSlideOffset = 600f;

    private Vector2 _previewOriginalPosition;
    private Vector2 _previewOriginalScale;
    private Vector2 _lobbyMainOriginalPosition;

    private bool _isInCustomizingMode;
    private Sequence _currentSequence;

    public bool IsInCustomizingMode => _isInCustomizingMode;
    public event Action OnTransitionToCustomizingComplete;
    public event Action OnTransitionToLobbyComplete;

    private void Awake()
    {
        CacheOriginalValues();
        SetupInitialState();
    }

    private void CacheOriginalValues()
    {
        if (_previewRawImage != null)
        {
            _previewOriginalPosition = _previewRawImage.anchoredPosition;
            _previewOriginalScale = _previewRawImage.localScale;
        }

        if (_lobbyMainPanel != null)
        {
            _lobbyMainOriginalPosition = _lobbyMainPanel.anchoredPosition;
        }
    }

    private void SetupInitialState()
    {
        // Customizing 패널은 처음에 화면 밖에 위치
        if (_customizingPanel != null)
        {
            var pos = _customizingPanel.anchoredPosition;
            pos.x += _customizingPanelSlideOffset;
            _customizingPanel.anchoredPosition = pos;
            _customizingPanel.gameObject.SetActive(false);
        }
    }

    public void TransitionToCustomizing()
    {
        if (_isInCustomizingMode) return;

        _currentSequence?.Kill();
        _currentSequence = DOTween.Sequence();

        // Customizing 패널 활성화
        if (_customizingPanel != null)
        {
            _customizingPanel.gameObject.SetActive(true);
        }

        // Lobby 메인 패널 페이드 아웃 + 슬라이드
        if (_lobbyMainCanvasGroup != null)
        {
            _lobbyMainCanvasGroup.blocksRaycasts = false;
            _currentSequence.Join(
                _lobbyMainCanvasGroup.DOFade(0f, _transitionDuration).SetEase(_easeType)
            );
        }

        if (_lobbyMainPanel != null)
        {
            _currentSequence.Join(
                _lobbyMainPanel.DOAnchorPosX(_lobbyMainOriginalPosition.x - 200f, _transitionDuration).SetEase(_easeType)
            );
        }

        // 프리뷰 이미지 확대 + 이동
        if (_previewRawImage != null)
        {
            _currentSequence.Join(
                _previewRawImage.DOAnchorPos(_previewCustomizingPosition, _transitionDuration).SetEase(_easeType)
            );
            _currentSequence.Join(
                _previewRawImage.DOScale(_previewCustomizingScale, _transitionDuration).SetEase(_easeType)
            );
        }

        // Customizing 패널 슬라이드 인
        if (_customizingPanel != null)
        {
            var targetPos = _customizingPanel.anchoredPosition;
            targetPos.x -= _customizingPanelSlideOffset;
            _currentSequence.Join(
                _customizingPanel.DOAnchorPosX(targetPos.x, _transitionDuration).SetEase(_easeType)
            );
        }

        _currentSequence.OnComplete(() =>
        {
            _isInCustomizingMode = true;
  
            OnTransitionToCustomizingComplete?.Invoke();
        });
    }

    public void TransitionToLobby()
    {
        if (!_isInCustomizingMode) return;

        _currentSequence?.Kill();
        _currentSequence = DOTween.Sequence();

        // Lobby 메인 패널 페이드 인 + 슬라이드
        if (_lobbyMainCanvasGroup != null)
        {
            _lobbyMainCanvasGroup.blocksRaycasts = true;
            _currentSequence.Join(
                _lobbyMainCanvasGroup.DOFade(1f, _transitionDuration).SetEase(_easeType)
            );
        }

        if (_lobbyMainPanel != null)
        {
            _currentSequence.Join(
                _lobbyMainPanel.DOAnchorPos(_lobbyMainOriginalPosition, _transitionDuration).SetEase(_easeType)
            );
        }

        // 프리뷰 이미지 원래 크기/위치로 복귀
        if (_previewRawImage != null)
        {
            _currentSequence.Join(
                _previewRawImage.DOAnchorPos(_previewOriginalPosition, _transitionDuration).SetEase(_easeType)
            );
            _currentSequence.Join(
                _previewRawImage.DOScale(_previewOriginalScale, _transitionDuration).SetEase(_easeType)
            );
        }

        // Customizing 패널 슬라이드 아웃
        if (_customizingPanel != null)
        {
            var targetPos = _customizingPanel.anchoredPosition;
            targetPos.x += _customizingPanelSlideOffset;
            _currentSequence.Join(
                _customizingPanel.DOAnchorPosX(targetPos.x, _transitionDuration).SetEase(_easeType)
            );
        }

        _currentSequence.OnComplete(() =>
        {
            _isInCustomizingMode = false;
            if (_customizingPanel != null)
            {
                _customizingPanel.gameObject.SetActive(false);
            }
            OnTransitionToLobbyComplete?.Invoke();
        });
    }

    public void Toggle()
    {
        if (_isInCustomizingMode)
            TransitionToLobby();
        else
            TransitionToCustomizing();
    }

    private void OnDestroy()
    {
        _currentSequence?.Kill();
    }
}
