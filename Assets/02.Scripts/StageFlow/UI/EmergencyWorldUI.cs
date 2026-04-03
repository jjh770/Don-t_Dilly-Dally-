using DontDillyDally.Data;
using DontDillyDally.StageFlow;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EmergencyWorldUI : MonoBehaviour
{
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
    private bool _wasVisible;
    private EmergencyEventKind _displayedKind = EmergencyEventKind.None;
    private CraftedMaterialType _displayedTrayTarget = CraftedMaterialType.None;
    private DiagnosisScanType _displayedDiagnosisTarget = DiagnosisScanType.None;

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
            if (_wasVisible)
            {
                ResetDisplayState();
            }

            return;
        }

        EmergencyEventKind kind = _stageFlowManager.CurrentEmergencyKind;
        if (!_wasVisible || _displayedKind != kind)
        {
            UpdateLayout(kind);
            _displayedKind = kind;
            _displayedTrayTarget = CraftedMaterialType.None;
            _displayedDiagnosisTarget = DiagnosisScanType.None;
        }

        UpdateTimer(kind, _stageFlowManager.EmergencyRemainingTime);

        switch (kind)
        {
            case EmergencyEventKind.Tray:
                CraftedMaterialType trayTarget = _stageFlowManager.CurrentEmergencyTrayTarget;
                if (_displayedTrayTarget != trayTarget)
                {
                    UpdateTrayIcons(trayTarget);
                    _displayedTrayTarget = trayTarget;
                }
                break;

            case EmergencyEventKind.Diagnosis:
                DiagnosisScanType diagnosisTarget = _stageFlowManager.CurrentEmergencyDiagnosisTarget;
                if (_displayedDiagnosisTarget != diagnosisTarget)
                {
                    UpdateDiagnosisIcon(diagnosisTarget);
                    _displayedDiagnosisTarget = diagnosisTarget;
                }
                break;
        }

        _wasVisible = true;
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

    private void UpdateTrayIcons(CraftedMaterialType targetMaterial)
    {
        if (_uiCatalog == null)
        {
            return;
        }

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

    private void UpdateDiagnosisIcon(DiagnosisScanType diagnosisTarget)
    {
        if (_uiCatalog == null)
        {
            return;
        }

        Sprite diagnosisIcon = _uiCatalog.GetDiagnosisIcon(diagnosisTarget);
        SetImageSprite(_diagnosisIconImage, diagnosisIcon);
    }

    private void UpdateTimer(EmergencyEventKind kind, float remainingTime)
    {
        if (_timerFillImage == null)
        {
            return;
        }

        StageEmergencySettings emergencySettings = _stageFlowManager.CurrentStageData?.Settings?.EmergencySettings;
        float duration = kind == EmergencyEventKind.Tray
            ? emergencySettings?.TrayDurationSec ?? 0f
            : emergencySettings?.DiagnosisDurationSec ?? 0f;

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

    private void ResetDisplayState()
    {
        _wasVisible = false;
        _displayedKind = EmergencyEventKind.None;
        _displayedTrayTarget = CraftedMaterialType.None;
        _displayedDiagnosisTarget = DiagnosisScanType.None;
    }
}
