using System.Collections;
using DontDillyDally.StageFlow;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EmergencyUIController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private EmergencyWorldUI _emergencyView;
    [SerializeField] private GameObject _overlayPanel;
    [SerializeField] private RectTransform _followRoot;

    [Header("Follow Target")]
    [SerializeField] private Transform _targetAnchor;
    [SerializeField] private Vector2 _screenOffset = new(0f, 80f);

    [Header("Projection")]
    [SerializeField] private Camera _worldCamera;
    [SerializeField] private RectTransform _canvasRect;
    [SerializeField] private bool _hideWhenBehindCamera = true;

    private StageFlowManager _stageFlowManager;
    private Canvas _canvas;
    private CanvasGroup _overlayCanvasGroup;
    private bool _hasAppliedOverlayVisibility;
    private bool _isOverlayVisible;

    private void Awake()
    {
        ResolveReferences();
        _emergencyView?.UseExternalController();
        SetOverlayVisible(false);
    }

    private IEnumerator Start()
    {
        while (_stageFlowManager == null || !_stageFlowManager.IsInitialized || _worldCamera == null || _canvasRect == null)
        {
            ResolveRuntimeReferences();
            yield return null;
        }
    }

    private void Update()
    {
        if (_stageFlowManager == null || !_stageFlowManager.IsInitialized)
        {
            SetOverlayVisible(false);
            return;
        }

        bool shouldShow = _stageFlowManager.ShouldShowEmergencyUi && _targetAnchor != null;

        if (!shouldShow)
        {
            SetOverlayVisible(false);
            return;
        }

        if (!UpdateOverlayPosition(_targetAnchor))
        {
            SetOverlayVisible(false);
            return;
        }

        SetOverlayVisible(true);
        _emergencyView?.Refresh(_stageFlowManager);
    }

    private void ResolveRuntimeReferences()
    {
        if (_stageFlowManager == null)
        {
            _stageFlowManager = StageFlowManager.Instance;
        }

        if (_worldCamera == null)
        {
            _worldCamera = Camera.main;
        }

        if (_canvasRect == null)
        {
            _canvas = GetComponentInParent<Canvas>();
            _canvasRect = _canvas != null ? _canvas.transform as RectTransform : null;
        }
        else if (_canvas == null)
        {
            _canvas = _canvasRect.GetComponentInParent<Canvas>();
        }
    }

    private void ResolveReferences()
    {
        if (_emergencyView == null)
        {
            _emergencyView = GetComponentInChildren<EmergencyWorldUI>(true);
        }

        if (_followRoot == null && _emergencyView != null)
        {
            _followRoot = _emergencyView.transform as RectTransform;
        }

        if (_overlayPanel == null && _followRoot != null)
        {
            _overlayPanel = _followRoot.gameObject;
        }

        if (_overlayPanel != null)
        {
            _overlayCanvasGroup = _overlayPanel.GetComponent<CanvasGroup>();
            if (_overlayCanvasGroup == null)
            {
                _overlayCanvasGroup = _overlayPanel.AddComponent<CanvasGroup>();
            }
        }

        if (_worldCamera == null)
        {
            _worldCamera = Camera.main;
        }

        if (_canvasRect == null)
        {
            _canvas = GetComponentInParent<Canvas>();
            _canvasRect = _canvas != null ? _canvas.transform as RectTransform : null;
        }
        else
        {
            _canvas = _canvasRect.GetComponentInParent<Canvas>();
        }
    }

    private bool UpdateOverlayPosition(Transform anchor)
    {
        if (_followRoot == null || _canvasRect == null || _worldCamera == null)
        {
            return false;
        }

        Vector3 screenPoint = _worldCamera.WorldToScreenPoint(anchor.position);
        if (_hideWhenBehindCamera && screenPoint.z < 0f)
        {
            return false;
        }

        Camera uiCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? _canvas.worldCamera
            : null;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            screenPoint,
            uiCamera,
            out Vector2 localPoint);

        _followRoot.anchoredPosition = localPoint + _screenOffset;
        return true;
    }

    private void SetOverlayVisible(bool visible)
    {
        if (_hasAppliedOverlayVisibility && _isOverlayVisible == visible)
        {
            return;
        }

        _hasAppliedOverlayVisibility = true;
        _isOverlayVisible = visible;

        if (_overlayCanvasGroup != null)
        {
            _overlayCanvasGroup.alpha = visible ? 1f : 0f;
            _overlayCanvasGroup.interactable = visible;
            _overlayCanvasGroup.blocksRaycasts = visible;
        }
        else if (_overlayPanel != null && _overlayPanel != gameObject)
        {
            _overlayPanel.SetActive(visible);
        }

    }
}
