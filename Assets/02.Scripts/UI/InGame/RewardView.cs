using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class RewardView : UIPopupBase
{
    [Header("Reward")]
    [SerializeField] private UI_NumberCounterTween _coin;
    [SerializeField] private UI_NumberCounterTween _star;
    [SerializeField] private float _rewardUpdateInterval = 0.18f;

    [Header("Stars")]
    [SerializeField] private GameObject[] _stars;

    [Header("RewardAnimation")]
    [SerializeField] private float _starRevealInterval = 0.18f;
    [SerializeField] private float _starPopDuration = 0.22f;
    [SerializeField] private float _starStartScale = 0.65f;
    [SerializeField] private float _starOvershootScale = 1.2f;
    [SerializeField] private float _startRotation = -12f;
    [SerializeField] private float _starToRewardInterval = 0.5f;
    [SerializeField] private float _returnWaitingRoomInterval = 1;

    [Header("Progress")]
    [SerializeField] private Slider _progressSlider;
    [SerializeField] private float _sliderDuration = 0.6f;
    [SerializeField] private float _sliderToStarInterval = 0.3f;




    private Sequence _starSequence;

    private RewardPresenter _presenter;

    protected override void Awake()
    {
        base.Awake();
        _progressSlider.value = 0;
        HideAllStars();
    }

    private void OnEnable()
    {
        HideAllStars();
    }

    private void OnDisable()
    {
        _starSequence?.Kill();
        ResetStars();
    }

    public void SetPresenter(RewardPresenter presenter)
    {
        _presenter = presenter;
    }

    public void InitializeReward(int coin, int star)
    {
        _coin.SetValueImmediate(coin);
        _star.SetValueImmediate(star);
    }

    public void PlayRewardSequence(int count, float ratio, int coin, int star)
    {
        _starSequence?.Kill();
        HideAllStars();

        int clampedCount = Mathf.Clamp(count, 0, _stars.Length);

        _starSequence = DOTween.Sequence().SetUpdate(true);

        // 슬라이더 채우기
        if (_progressSlider != null)
        {
            _progressSlider.value = 0f;
            _starSequence.Append(
                DOTween.To(() => _progressSlider.value, x => _progressSlider.value = x, ratio, _sliderDuration)
                    .SetEase(Ease.OutCubic)
            );
            _starSequence.AppendInterval(_sliderToStarInterval);
        }

        for (int i = 0; i < clampedCount; i++)
        {
            if (_stars[i] == null) continue;

            int index = i;
            _starSequence.AppendCallback(() => PlayStarReveal(_stars[index]));

            if (i < clampedCount - 1)
                _starSequence.AppendInterval(_starRevealInterval);
        }

        // 마지막 별 팝 애니메이션 끝날 때까지 대기
        _starSequence.AppendInterval(_starPopDuration);

        _starSequence.AppendInterval(_starToRewardInterval);

        // 코인 카운팅
        _starSequence.AppendCallback(() => _coin.SetValue(coin));

        // 인터벌 후 별 카운팅
        _starSequence.AppendInterval(_rewardUpdateInterval);
        _starSequence.AppendCallback(() => _star.SetValue(star));
        _starSequence.AppendInterval(_returnWaitingRoomInterval);
        _starSequence.OnComplete(() => _presenter.ReturnWaitingRoom());
    }

    public void HideAllStars()
    {
        _starSequence?.Kill();
        ResetStars();
    }

    private void PlayStarReveal(GameObject star)
    {
        if (star == null)
        {
            return;
        }

        Transform starTransform = star.transform;
        star.SetActive(true);

        starTransform.localScale = Vector3.one * _starStartScale;
        starTransform.localRotation = Quaternion.Euler(0f, 0f, _startRotation);

        DOTween.Kill(starTransform);

        Sequence popSequence = DOTween.Sequence().SetTarget(starTransform).SetUpdate(true);
        popSequence.Append(starTransform.DOScale(_starOvershootScale, _starPopDuration * 0.55f).SetEase(Ease.OutBack));
        popSequence.Join(starTransform.DOLocalRotate(Vector3.zero, _starPopDuration).SetEase(Ease.OutCubic));
        popSequence.Append(starTransform.DOScale(1f, _starPopDuration * 0.45f).SetEase(Ease.OutCubic));
    }

    private void ResetStars()
    {
        foreach (GameObject star in _stars)
        {
            if (star == null)
            {
                continue;
            }

            Transform starTransform = star.transform;
            DOTween.Kill(starTransform);
            starTransform.localScale = Vector3.one;
            starTransform.localRotation = Quaternion.identity;
            star.SetActive(false);
        }
    }

    [ContextMenu("Debug/Show")]
    private void DebugShow()
    {
        Show(() => PlayRewardSequence(10, 1, 10, 3));
        
    }


    protected override void OnShow()
    {
        
    }
}
