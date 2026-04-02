using DontDillyDally.Data;
using DontDillyDally.StageFlow;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EmergencyWorldUI : MonoBehaviour
{
    private const float TrayDurationSec = 20f;
    private const float DiagnosisDurationSec = 5f;

    [Header("Patient")]
    [SerializeField] private int _patientIndex;

    [Header("Catalog")]
    [SerializeField] private EmergencyUiCatalogSO _uiCatalog;

    [Header("Root")]
    [SerializeField] private GameObject _root;

    [Header("Shared")]
    [SerializeField] private Image _timerFillImage;

    [Header("Tray Layout")]
    [SerializeField] private GameObject _trayLayout;
    [SerializeField] private Image _targetMaterialIconImage;
    [SerializeField] private Image _processActionIconImage;

    [Header("Diagnosis Layout")]
    [SerializeField] private GameObject _diagnosisLayout;
    [SerializeField] private Image _diagnosisIconImage;

    private StageFlowManager _stageFlowManager;
    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        if (_root == null)
        {
            _root = gameObject;
        }

        _canvasGroup = _root.GetComponent<CanvasGroup>();

        SetVisible(false);
    }

    private void Update()
    {
        if (_stageFlowManager == null)
        {
            _stageFlowManager = StageFlowManager.Instance;
            if (_stageFlowManager == null)
            {
                SetVisible(false);
                return;
            }
        }

        bool isCurrentPatient = _stageFlowManager.CurrentPatientIndex.Value == _patientIndex;
        bool shouldShow = _stageFlowManager.ShouldShowEmergencyUi && isCurrentPatient;

        SetVisible(shouldShow);
        if (!shouldShow)
        {
            return;
        }

        EmergencyEventKind kind = _stageFlowManager.CurrentEmergencyKind;
        UpdateLayout(kind);
        UpdateTimer(kind, _stageFlowManager.EmergencyRemainingTime);

        switch (kind)
        {
            case EmergencyEventKind.Tray:
                UpdateTrayIcons();
                break;

            case EmergencyEventKind.Diagnosis:
                UpdateDiagnosisIcon();
                break;
        }
    }

    private void SetVisible(bool visible)
    {
        if (_root == null || _canvasGroup == null)
        {
            return;
        }

        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }

    private void UpdateLayout(EmergencyEventKind kind)
    {
        if (_trayLayout != null)
        {
            _trayLayout.SetActive(kind == EmergencyEventKind.Tray);
        }

        if (_diagnosisLayout != null)
        {
            _diagnosisLayout.SetActive(kind == EmergencyEventKind.Diagnosis);
        }
    }

    private void UpdateTrayIcons()
    {
        if (_uiCatalog == null)
        {
            return;
        }

        CraftedMaterialType targetMaterial = _stageFlowManager.CurrentEmergencyTrayTarget;
        MaterialIconTable iconTable = _uiCatalog.MaterialIconTable;

        SetImageSprite(_targetMaterialIconImage, iconTable != null ? iconTable.GetMaterialIcon(targetMaterial) : null);

        Sprite actionIcon = null;
        bool shouldShowProcessAction = false;
        if (_uiCatalog.TryGetTargetMaterialProcess(targetMaterial, out EmergencyUiCatalogSO.TargetMaterialProcessEntry entry) &&
            iconTable != null &&
            entry.ProcessAction != ActionType.None)
        {
            actionIcon = iconTable.GetActionIcon(entry.ProcessAction);
            shouldShowProcessAction = actionIcon != null;
        }

        SetImageSprite(_processActionIconImage, actionIcon);
        if (_processActionIconImage != null)
        {
            _processActionIconImage.gameObject.SetActive(shouldShowProcessAction);
        }
    }

    private void UpdateDiagnosisIcon()
    {
        if (_uiCatalog == null)
        {
            return;
        }

        Sprite diagnosisIcon = _uiCatalog.GetDiagnosisIcon(_stageFlowManager.CurrentEmergencyDiagnosisTarget);
        SetImageSprite(_diagnosisIconImage, diagnosisIcon);
    }

    private void UpdateTimer(EmergencyEventKind kind, float remainingTime)
    {
        if (_timerFillImage == null)
        {
            return;
        }

        float duration = kind == EmergencyEventKind.Tray ? TrayDurationSec : DiagnosisDurationSec;
        if (duration <= 0f)
        {
            _timerFillImage.fillAmount = 0f;
            return;
        }

        _timerFillImage.fillAmount = Mathf.Clamp01(remainingTime / duration);
    }

    private static void SetImageSprite(Image image, Sprite sprite)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = sprite;
        image.enabled = sprite != null;
    }
}
