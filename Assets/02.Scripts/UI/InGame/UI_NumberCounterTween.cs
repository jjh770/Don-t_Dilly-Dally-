using System.Collections.Generic;
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

        string formatted = _decimalPlaces > 0
            ? value.ToString($"F{_decimalPlaces}")
            : Mathf.RoundToInt(value).ToString();

        if (_useComma)
            formatted = AddComma(formatted);

        _label.text = $"{_prefix}{formatted}{_suffix}";
    }

    private string AddComma(string numberStr)
    {
        // 소수점 처리
        int dotIndex = numberStr.IndexOf('.');
        string intPart = dotIndex >= 0 ? numberStr[..dotIndex] : numberStr;
        string decPart = dotIndex >= 0 ? numberStr[dotIndex..] : "";

        bool isNegative = intPart.StartsWith('-');
        if (isNegative) intPart = intPart[1..];

        char[] chars = intPart.ToCharArray();
        List<char> result = new();
        for (int i = 0; i < chars.Length; i++)
        {
            if (i > 0 && (chars.Length - i) % 3 == 0)
                result.Add(',');
            result.Add(chars[i]);
        }

        string formatted = new string(result.ToArray()) + decPart;
        return isNegative ? "-" + formatted : formatted;
    }

    private void KillTween()
    {
        _tween?.Kill();
        _tween = null;
    }

    private void OnDestroy() => KillTween();
}