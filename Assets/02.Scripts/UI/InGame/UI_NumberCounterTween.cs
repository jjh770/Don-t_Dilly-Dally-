using DG.Tweening;
using TMPro;
using UnityEngine;
 
/// <summary>
/// DOTween 기반 숫자 카운팅 애니메이션 컴포넌트
/// 텍스트가 촤르르르 올라가거나 내려가며 바뀜
/// </summary>
public class UI_NumberCounterTween : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Inspector
    // ─────────────────────────────────────────────

    [Header("연결")]
    [SerializeField] private TMP_Text _label;

    [Header("애니메이션")]
    [SerializeField] private float _duration = 1f;
    [SerializeField] private Ease _ease = Ease.OutExpo;

    [Header("포맷")]
    [SerializeField] private bool _useComma = true;   // 1,000 처럼 콤마
    [SerializeField] private string _prefix = "";     // 앞에 붙일 문자 (예: "$")
    [SerializeField] private string _suffix = "";     // 뒤에 붙일 문자 (예: "점")
    [SerializeField] private int _decimalPlaces = 0;  // 소수점 자리 수

    // ─────────────────────────────────────────────
    // 내부 상태
    // ─────────────────────────────────────────────

    private float _current;
    private Tween _tween;

    // ─────────────────────────────────────────────
    // 공개 API
    // ─────────────────────────────────────────────

    /// <summary>현재 표시 중인 값</summary>
    public float CurrentValue => _current;

    /// <summary>즉시 값 설정 (애니메이션 없음)</summary>
    public void SetValueImmediate(float value)
    {
        KillTween();
        _current = value;
        UpdateText(_current);
    }

    /// <summary>목표값까지 촤르르르 애니메이션</summary>
    public void SetValue(float target, float? overrideDuration = null, Ease? overrideEase = null)
    {
        KillTween();

        float from = _current;
        float duration = overrideDuration ?? _duration;
        Ease ease = overrideEase ?? _ease;

        _tween = DOTween
            .To(getter: () => from,
                setter: v =>
                {
                    _current = v;
                    UpdateText(v);
                },
                endValue: target,
                duration: duration)
            .SetEase(ease)
            .SetUpdate(UpdateType.Normal, isIndependentUpdate: false)
            .OnComplete(() => _current = target);
    }

    /// <summary>현재 값에서 delta만큼 더하거나 빼기</summary>
    public void AddValue(float delta, float? overrideDuration = null, Ease? overrideEase = null)
        => SetValue(_current + delta, overrideDuration, overrideEase);

    // ─────────────────────────────────────────────
    // 내부 유틸
    // ─────────────────────────────────────────────

    private void UpdateText(float value)
    {
        if (_label == null) return;

        string format = _useComma ? "N" : "F";
        _label.text = $"{_prefix}{value.ToString($"{format}{_decimalPlaces}")}{_suffix}";
    }

    private void KillTween()
    {
        _tween?.Kill();
        _tween = null;
    }

    private void OnDestroy() => KillTween();
}