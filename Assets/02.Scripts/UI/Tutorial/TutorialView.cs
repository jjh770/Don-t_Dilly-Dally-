using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialView : UIPopupBase
{
    [Header("Slides")]
    [SerializeField] private RectTransform _slideContainer;
    [SerializeField] private UI_SlideView _slideViewPrefab;
    [SerializeField] private List<UI_SlideData> _slides;

    [Header("Nav")]
    [SerializeField] private Button _prevButton;
    [SerializeField] private Button _nextButton;

    [Header("Progress")]
    [SerializeField] private Transform _dotsParent;
    [SerializeField] private Image _dotPrefab;

    [Header("Animation")]
    [SerializeField] private float _duration = 0.4f;
    [SerializeField] private AnimationCurve _ease;

    [Header("Close")]
    [SerializeField] private Button _closedButton;

    private List<UI_SlideView> _slideViews = new();
    private List<Image> _dots = new();
    private int _currentIndex = 0;
    private bool _isAnimating = false;

    private void OnEnable()
    {
        _prevButton.onClick.AddListener(OnPrevClicked);
        _nextButton.onClick.AddListener(OnNextClicked);
        _closedButton.onClick.AddListener(Hide);
    }
    void Start()
    {
        BuildSlides();
        BuildDots();

        Refresh();
    }

    private void OnDisable()
    {
        _prevButton.onClick.RemoveListener(OnPrevClicked);
        _nextButton.onClick.RemoveListener(OnNextClicked);
        _closedButton.onClick.RemoveListener(Hide);
    }

    void BuildSlides()
    {
        foreach (var data in _slides)
        {
            var sv = Instantiate(_slideViewPrefab, _slideContainer);
            sv.Setup(data);
            _slideViews.Add(sv);
        }
    }

    void BuildDots()
    {
        foreach (var _ in _slides)
        {
            var dot = Instantiate(_dotPrefab, _dotsParent);
            _dots.Add(dot);
        }
    }

    void OnNextClicked()
    {
        GoTo(_currentIndex + 1, direction: 1);
    }

    void OnPrevClicked()
    {
        if (_currentIndex > 0)
            GoTo(_currentIndex - 1, direction: -1);
    }

    void GoTo(int next, int direction)
    {
        if (_isAnimating) return;
        _currentIndex = next;
        Refresh();
        StartCoroutine(SlideCoroutine(direction));
    }

    void Refresh()
    {
        _prevButton.interactable = _currentIndex > 0;
        _nextButton.interactable = _currentIndex < _slides.Count - 1;

        for (int i = 0; i < _dots.Count; i++)
            _dots[i].color = i == _currentIndex
                ? Color.white
                : new Color(1, 1, 1, 0.3f);
    }

    IEnumerator SlideCoroutine(int direction)
    {
        _isAnimating = true;

        float slideWidth = _slideContainer.rect.width / _slides.Count;
        Vector2 start = _slideContainer.anchoredPosition;
        Vector2 end = new Vector2(-_currentIndex * slideWidth, 0);

        float elapsed = 0f;
        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            float t = _ease.Evaluate(elapsed / _duration);
            _slideContainer.anchoredPosition = Vector2.LerpUnclamped(start, end, t);
            yield return null;
        }

        _slideContainer.anchoredPosition = end;
        _isAnimating = false;
    }

    protected override void OnShow()
    {
       
    }
}
