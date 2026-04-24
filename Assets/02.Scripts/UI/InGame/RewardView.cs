using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardView : UIPopupBase
{
    [Header("Reward")]
    [SerializeField] private UI_NumberCounterTween _coin;
    [SerializeField] private UI_NumberCounterTween _star;
    [SerializeField] private float _rewardUpdateInterval = 0.18f;
    [SerializeField] private TextMeshProUGUI _summaryText;
    [SerializeField] private TextMeshProUGUI _moneyText;
    [SerializeField] private Color _plusColor = Color.green;
    [SerializeField] private Color _minusColor = Color.red;

    [Header("Stars")]
    [SerializeField] private GameObject[] _stars;

    [Header("StarAnimation")]
    [SerializeField] private float _starRevealInterval = 0.18f;
    [SerializeField] private float _starPopDuration = 0.22f;
    [SerializeField] private float _starStartScale = 0.65f;
    [SerializeField] private float _starOvershootScale = 1.2f;
    [SerializeField] private float _startRotation = -12f;
    [SerializeField] private float _starPulseScale = 1.15f;
    [SerializeField] private float _starPulseDuration = 0.5f;

    [Header("Progress")]
    [SerializeField] private Slider _progressSlider;
    [SerializeField] private float _sliderDuration = 0.6f;


    [Header("Typing")]
    [SerializeField] private float _typingCharInterval = 0.04f;


    [Header("FadeIn")]
    [SerializeField] private float _fadeInDuration = 0.35f;
    [SerializeField] private CanvasGroup _moneyGroup;
    private RectTransform _moneyGroupRect;

    [Header("etcInterval")]
    [SerializeField] private float _defaultInterval = 0.5f;
    [SerializeField] private float _returnWaitingRoomInterval = 1;

    private Sequence _rewardSequence;

    private RewardPresenter _presenter;

    // ══════════════════════════════════════════════════════════════════
    //  Unity Lifecycle
    // ══════════════════════════════════════════════════════════════════

    protected override void Awake()
    {
        base.Awake();
        HideAllStars();

        if (_progressSlider != null) _progressSlider.value = 0f;
        if (_moneyGroup != null) _moneyGroup.alpha = 0f;
        _summaryText.text = "";
        _moneyGroupRect = _moneyGroup.GetComponent<RectTransform>();
    }

    private void OnEnable() 
    {
        HideAllStars();
    } 

    private void OnDisable()
    {
        _rewardSequence?.Kill();
        ResetStars();
    }

    // ══════════════════════════════════════════════════════════════════
    //  Public API
    // ══════════════════════════════════════════════════════════════════

    public void SetPresenter(RewardPresenter presenter)
    {
        _presenter = presenter;
    }

    public void InitializeWallet(int coin, int star)
    {
        _coin.SetValueImmediate(coin);
        _star.SetValueImmediate(star);
    }

    /// <summary>
    /// 텍스트 내용만 세팅합니다. 애니메이션은 Builder로 제어합니다.
    /// </summary>
    private void ApplyRewardText(string summary, int defaultReward, int deltaReward)
    {
        _summaryText.text = "";
        _summaryText.maxVisibleCharacters = 0;

        _moneyText.text = BuildMoneyString(defaultReward, deltaReward);

        LayoutRebuilder.ForceRebuildLayoutImmediate(_moneyGroupRect);

        _pendingSummary = summary;
    }

    public void PlayRewardSequence(int starCount, float sliderRatio, int coin, int star, string summary, int defaultReward, int deltaReward)
    {
        ApplyRewardText(summary, defaultReward, deltaReward);

        CreateSequence()
        .SliderFill(sliderRatio)
        .Interval(sliderRatio > 0? _defaultInterval : 0)
        .StarReveal(starCount)
        .Interval(starCount > 0? _defaultInterval : 0)
        .SummaryTyping()
        .Interval(_defaultInterval)
        .MoneyFadeIn()
        .Interval(_defaultInterval)
        .CoinCount(coin)
        .StarCount(star)
        .Interval(_returnWaitingRoomInterval)
        .OnComplete(() => _presenter?.ReturnWaitingRoom())
        .Play();
    }

    /// <summary>
    /// 애니메이션 시퀀스 빌더를 반환합니다.
    /// </summary>
    public RewardSequenceBuilder CreateSequence()
    {
        _rewardSequence?.Kill();
        HideAllStars();
        _rewardSequence = DOTween.Sequence().SetUpdate(true);
        return new RewardSequenceBuilder(this, _rewardSequence);
    }

    public void HideAllStars()
    {
        _rewardSequence?.Kill();
        ResetStars();
    }

    // ══════════════════════════════════════════════════════════════════
    //  Builder Steps (internal — Builder에서만 호출)
    // ══════════════════════════════════════════════════════════════════

    internal void Step_SliderFill(Sequence seq, float ratio)
    {
        if (_progressSlider == null) return;

        _progressSlider.value = 0f;
        seq.Append(
            DOTween.To(
                () => _progressSlider.value,
                x => _progressSlider.value = x,
                ratio,
                _sliderDuration
            ).SetEase(Ease.OutCubic)
        );
    }

    internal void Step_StarReveal(Sequence seq, int count)
    {
        int clamped = Mathf.Clamp(count, 0, _stars.Length);

        seq.AppendCallback(() =>
        {
            if (clamped == 0)
            {
                SoundManager.Instance.Play(SFXKey.ResultZeroStar, SoundType.Local);
            }
            else if (clamped == 1)
            {
                SoundManager.Instance.Play(SFXKey.Result1Star, SoundType.Local);
            }
            else if (clamped == 2)
            {
                SoundManager.Instance.Play(SFXKey.Result2Star, SoundType.Local);
            }
            else if (clamped == 3)
            {
                SoundManager.Instance.Play(SFXKey.Result3Star, SoundType.Local);
            }
        });

        for (int i = 0; i < clamped; i++)
        {
            if (_stars[i] == null) continue;

            int index = i;
            seq.AppendCallback(() =>
            {
                PlayStarReveal(_stars[index]);
                SoundManager.Instance.Play(SFXKey.ResultStarStamp, SoundType.Local);
            });

            if (i < clamped - 1)
                seq.AppendInterval(_starRevealInterval);
        }

        seq.AppendInterval(_starPopDuration);

        seq.AppendCallback(() => PlayStarPulse(clamped));
    }


    internal void Step_CoinCount(Sequence seq, int coin)
    {
        seq.AppendCallback(() => _coin.SetValue(coin));
        seq.AppendInterval(_rewardUpdateInterval);
    }

    internal void Step_StarCount(Sequence seq, int star)
    {
        seq.AppendCallback(() => _star.SetValue(star));
        seq.AppendInterval(_rewardUpdateInterval);
    }

    internal void Step_SummaryTyping(Sequence seq)
    {
        if (_summaryText == null) return;

        string fullText = _pendingSummary;

        seq.AppendCallback(() =>
        {
            _summaryText.text = fullText;
            _summaryText.maxVisibleCharacters = 0;
            _summaryText.ForceMeshUpdate();

            int total = _summaryText.textInfo.characterCount;

            DOTween.To(
                    () => _summaryText.maxVisibleCharacters,
                    x => _summaryText.maxVisibleCharacters = x,
                    total,
                    total * _typingCharInterval
                )
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .OnComplete(() => _summaryText.maxVisibleCharacters = int.MaxValue);
        });

        // 타이핑 완료까지 시퀀스 대기
        float typingDuration = _pendingSummary.Length * _typingCharInterval;
        seq.AppendInterval(typingDuration);
    }

    internal void Step_MoneyFadeIn(Sequence seq)
    {
        if (_moneyGroup == null) return;

        seq.AppendCallback(() =>
        {
            _moneyGroup.alpha = 0f;
            _moneyGroup.DOFade(1f, _fadeInDuration)
                       .SetEase(Ease.InOutSine)
                       .SetUpdate(true);
        });

        seq.AppendInterval(_fadeInDuration);
    }

    // ══════════════════════════════════════════════════════════════════
    //  Star Helpers
    // ══════════════════════════════════════════════════════════════════

    private void PlayStarReveal(GameObject star)
    {
        if (star == null) return;

        Transform t = star.transform;
        star.SetActive(true);
        t.localScale = Vector3.one * _starStartScale;
        t.localRotation = Quaternion.Euler(0f, 0f, _startRotation);

        DOTween.Kill(t);

        Sequence pop = DOTween.Sequence().SetTarget(t).SetUpdate(true);

        // 등장 애니메이션
        pop.Append(t.DOScale(_starOvershootScale, _starPopDuration * 0.55f).SetEase(Ease.OutBack));
        pop.Join(t.DOLocalRotate(Vector3.zero, _starPopDuration).SetEase(Ease.OutCubic));
        pop.Append(t.DOScale(1f, _starPopDuration * 0.45f).SetEase(Ease.OutCubic));
    }

    private void PlayStarPulse(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (_stars[i] == null) continue;

            Transform t = _stars[i].transform;
            DOTween.Kill(t);
            t.localScale = Vector3.one;
            t.DOScale(_starPulseScale, _starPulseDuration)
             .SetEase(Ease.InOutSine)
             .SetLoops(-1, LoopType.Yoyo)
             .SetUpdate(true);
        }
    }

    private void ResetStars()
    {
        foreach (GameObject star in _stars)
        {
            if (star == null) continue;
            Transform t = star.transform;
            DOTween.Kill(t);
            t.localScale = Vector3.one;
            t.localRotation = Quaternion.identity;
            star.SetActive(false);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  Helpers
    // ══════════════════════════════════════════════════════════════════

    private string _pendingSummary = "";

    private string ToHex(Color color) => $"#{ColorUtility.ToHtmlStringRGB(color)}";

    private string BuildMoneyString(int defaultReward, int deltaReward)
    {
        string delta = deltaReward switch
        {
            > 0 => $" (<color={ToHex(_plusColor)}>+{deltaReward}</color>)",
            < 0 => $" (<color={ToHex(_minusColor)}>{deltaReward}</color>)",
            _ => ""
        };
        return $"{defaultReward}{delta}";
    }

    protected override void OnShow() { }

    [ContextMenu("Test Sequence")]
    private void TestSequence()
    {
        Show(() =>
        PlayRewardSequence(3, 0.75f, 1234, 5, "Mission Complete! You earned a reward for your performance.", 1000, 234));
    }
}