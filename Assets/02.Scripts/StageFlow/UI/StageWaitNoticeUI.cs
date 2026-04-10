using DG.Tweening;
using DontDillyDally.StageFlow;
using UniRx;
using UnityEngine;

public class StageWaitNoticeUI : MonoBehaviour
{
    [Header("Countdown Prefabs")]
    [SerializeField] private GameObject _prefab3;
    [SerializeField] private GameObject _prefab2;
    [SerializeField] private GameObject _prefab1;
    [SerializeField] private GameObject _prefabGo;

    [Header("Animation Settings")]
    [SerializeField] private float _popScale = 1.3f;
    [SerializeField] private float _animDuration = 0.3f;

    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    private StageFlowManager _stageFlowManager;
    private int _lastDisplayedCount = -1;
    private GameObject _currentCountdownObject;
    private bool _isGoAnimationPlaying;

    private void Start()
    {
        TryBind();
        RefreshUi();
    }

    private void Update()
    {
        if (_stageFlowManager != null)
        {
            RefreshUi();
        }
    }

    private void OnDestroy()
    {
        StageFlowBootstrapper.StageFlowReady -= HandleStageFlowReady;
        _disposables.Dispose();
        ClearCountdownObject();
    }

    private bool TryBind()
    {
        if (_stageFlowManager != null ||
            StageFlowBootstrapper.Instance == null ||
            !StageFlowBootstrapper.Instance.IsStageFlowReady ||
            StageFlowManager.Instance == null ||
            !StageFlowManager.Instance.IsInitialized)
        {
            return false;
        }

        _stageFlowManager = StageFlowManager.Instance;

        _stageFlowManager.CurrentPhase
            .Subscribe(_ =>
            {
                RefreshUi();
            })
            .AddTo(_disposables);

        _stageFlowManager.CountdownStartTime
            .Subscribe(_ =>
            {
                RefreshUi();
            })
            .AddTo(_disposables);

        _stageFlowManager.CountdownDuration
            .Subscribe(_ =>
            {
                RefreshUi();
            })
            .AddTo(_disposables);

        StageFlowBootstrapper.StageFlowReady -= HandleStageFlowReady;
        return true;
    }

    private void HandleStageFlowReady()
    {
        TryBind();
    }

    private void RefreshUi()
    {
        bool isLoading = _stageFlowManager == null ||
                         _stageFlowManager.CurrentPhase.Value == EStagePhase.Loading;
        bool isCountdown = _stageFlowManager != null &&
                           _stageFlowManager.CurrentPhase.Value == EStagePhase.Countdown;
        bool shouldShow = isLoading || isCountdown || _isGoAnimationPlaying;

        gameObject.SetActive(shouldShow);
        if (!shouldShow)
        {
            ClearCountdownObject();
            _lastDisplayedCount = -1;
            return;
        }

        if (_isGoAnimationPlaying)
        {
            return;
        }

        if (isLoading)
        {
            ClearCountdownObject();
            _lastDisplayedCount = -1;
            return;
        }

        float remainingTime = _stageFlowManager.GetRemainingCountdownTime();
        int displayCount = Mathf.CeilToInt(remainingTime);

        if (displayCount == _lastDisplayedCount)
        {
            return;
        }

        _lastDisplayedCount = displayCount;
        ShowCountdownPrefab(displayCount);
    }

    private void ShowCountdownPrefab(int count)
    {
        ClearCountdownObject();

        GameObject prefab = count switch
        {
            3 => _prefab3,
            2 => _prefab2,
            1 => _prefab1,
            0 => _prefabGo,
            _ => null
        };

        if (prefab == null)
        {
            return;
        }

        _currentCountdownObject = Instantiate(prefab, transform);
        PlayCountdownAnimation(_currentCountdownObject, count == 0);
    }

    private void PlayCountdownAnimation(GameObject obj, bool isGo)
    {
        if (obj == null)
        {
            return;
        }

        RectTransform rectTransform = obj.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.localScale = Vector3.zero;

        if (isGo)
        {
            _isGoAnimationPlaying = true;
        }

        Sequence sequence = DOTween.Sequence();

        // Pop in
        sequence.Append(rectTransform.DOScale(_popScale, _animDuration * 0.5f).SetEase(Ease.OutBack));
        sequence.Append(rectTransform.DOScale(1f, _animDuration * 0.3f).SetEase(Ease.InOutSine));

        // Hold 
        float holdTime = 0.3f;
        sequence.AppendInterval(holdTime);

        // Fade out
        CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = obj.AddComponent<CanvasGroup>();
        }

        sequence.Append(canvasGroup.DOFade(0f, _animDuration * 0.4f).SetEase(Ease.InQuad));
        sequence.Join(rectTransform.DOScale(0.8f, _animDuration * 0.4f).SetEase(Ease.InQuad));

        sequence.OnComplete(() =>
        {
            if (obj != null)
            {
                Destroy(obj);
            }

            if (_currentCountdownObject == obj)
            {
                _currentCountdownObject = null;
            }

            if (isGo)
            {
                _isGoAnimationPlaying = false;
                gameObject.SetActive(false);
            }
        });

        sequence.SetTarget(obj);
    }

    private void ClearCountdownObject()
    {
        if (_currentCountdownObject != null)
        {
            DOTween.Kill(_currentCountdownObject);
            Destroy(_currentCountdownObject);
            _currentCountdownObject = null;
        }
    }
}
