using DontDillyDally.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 환자 정보 패널의 개별 항목 UI입니다.
/// 프리팹으로 만들어서 PatientInfoPanelUI에 연결합니다.
///
/// 구조:
///   ┌─────────────────────────────┐
///   │ [#] 환자명 — 질병명          │  ← Header
///   │ 질병 설명                    │  ← Description
///   │ 환자 배경 이야기              │  ← Backstory
///   └─────────────────────────────┘
/// </summary>
public class PatientInfoEntry : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Image _background;
    [SerializeField] private TextMeshProUGUI _headerText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _backstoryText;

    [Header("색상")]
    [SerializeField] private Color _currentBgColor = new Color(1f, 0.95f, 0.7f, 0.95f);
    [SerializeField] private Color _defaultBgColor = new Color(1f, 1f, 1f, 0.6f);
    [SerializeField] private Color _currentHeaderColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color _defaultHeaderColor = new Color(0.9f, 0.9f, 0.9f, 1f);

    public void SetData(int patientNumber, DiseaseData disease)
    {
        string patientName = string.IsNullOrWhiteSpace(disease.PatientName)
            ? $"환자 {patientNumber}"
            : disease.PatientName;

        string diseaseName = string.IsNullOrWhiteSpace(disease.DiseaseName)
            ? "원인 불명"
            : disease.DiseaseName;

        if (_headerText != null)
        {
            _headerText.text = $"#{patientNumber} {patientName} — {diseaseName}";
        }

        if (_descriptionText != null)
        {
            _descriptionText.text = string.IsNullOrWhiteSpace(disease.Description)
                ? ""
                : disease.Description;
        }

        if (_backstoryText != null)
        {
            _backstoryText.text = string.IsNullOrWhiteSpace(disease.Backstory)
                ? ""
                : disease.Backstory;
        }
    }

    public void SetCurrent(bool isCurrent)
    {
        if (_background != null)
        {
            _background.color = isCurrent ? _currentBgColor : _defaultBgColor;
        }

        if (_headerText != null)
        {
            _headerText.color = isCurrent ? _currentHeaderColor : _defaultHeaderColor;
            _headerText.fontStyle = isCurrent ? FontStyles.Bold : FontStyles.Normal;
        }
    }
}
