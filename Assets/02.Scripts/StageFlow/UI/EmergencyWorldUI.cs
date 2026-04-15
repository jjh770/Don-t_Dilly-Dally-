using DontDillyDally.Data;
using DontDillyDally.MiniGame;
using DontDillyDally.StageFlow;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EmergencyWorldUI : MonoBehaviour
{
    [Header("Catalog")]
    [SerializeField] private EmergencyUiCatalogSO _uiCatalog;

    [Header("Root")]
    [SerializeField] private GameObject _root;

    [Header("Timer")]
    [SerializeField] private RadialTimerView _timer;

    [Header("Tray Layout")]
    [SerializeField] private GameObject _trayLayout;
    [SerializeField] private GameObject _targetMaterialIconRoot;
    [SerializeField] private Image _targetMaterialIconImage;
    [SerializeField] private GameObject _processActionIconRoot;
    [SerializeField] private Image _processActionIconImage;

    [Header("Diagnosis Layout")]
    [SerializeField] private GameObject _diagnosisLayout;
    [SerializeField] private GameObject _diagnosisIconRoot;
    [SerializeField] private Image _diagnosisIconImage;

    private StageFlowManager _stageFlowManager;
    private CanvasGroup _canvasGroup;
    private bool _isInitialized;
    private bool _wasVisible;
    private bool _isExternallyControlled;
    private EmergencyEventKind _displayedKind = EmergencyEventKind.None;
    private CraftedMaterialType _displayedTrayTarget = CraftedMaterialType.None;
    private DiagnosisScanType _displayedDiagnosisTarget = DiagnosisScanType.None;

    private void Awake()
    {
        EnsureInitialized();
        SetVisible(false);
    }

    private void Update()
    {
        if (_isExternallyControlled)
        {
            return;
        }

        if (_stageFlowManager == null)
        {
            _stageFlowManager = StageFlowManager.Instance;
        }

        if (_stageFlowManager == null || !_stageFlowManager.IsInitialized)
        {
            SetVisible(false);
            return;
        }

        bool shouldShow = _stageFlowManager.ShouldShowEmergencyUi;

        SetVisible(shouldShow);
        if (!shouldShow)
        {
            return;
        }

        Refresh(_stageFlowManager);
    }

    public void UseExternalController()
    {
        _isExternallyControlled = true;
        EnsureInitialized();
        SetVisible(false);
    }

    public void Refresh(StageFlowManager stageFlowManager)
    {
        if (stageFlowManager == null || !stageFlowManager.IsInitialized)
        {
            SetVisible(false);
            return;
        }

        _stageFlowManager = stageFlowManager;

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

    public void SetVisible(bool visible)
    {
        EnsureInitialized();

        if (_root == null)
        {
            return;
        }

        if (_canvasGroup == null)
        {
            _root.SetActive(visible);
        }
        else
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }

        if (!visible && _wasVisible)
        {
            ResetDisplayState();
        }
    }

    private void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        if (_root == null)
        {
            _root = gameObject;
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = _root.GetComponent<CanvasGroup>();
        }

        InitializeTimer();
        _isInitialized = true;
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

        Sprite targetIcon = iconTable != null ? iconTable.GetMaterialIcon(targetMaterial) : null;
        SetIcon(_targetMaterialIconRoot, _targetMaterialIconImage, targetIcon);

        Sprite actionIcon = null;
        if (_uiCatalog.TryGetTargetMaterialProcess(targetMaterial, out EmergencyUiCatalogSO.TargetMaterialProcessEntry entry) &&
            iconTable != null &&
            entry.ProcessAction != ActionType.None)
        {
            actionIcon = iconTable.GetActionIcon(entry.ProcessAction);
        }

        SetIcon(_processActionIconRoot, _processActionIconImage, actionIcon);
    }

    private void UpdateDiagnosisIcon(DiagnosisScanType diagnosisTarget)
    {
        if (_uiCatalog == null)
        {
            return;
        }

        Sprite diagnosisIcon = _uiCatalog.GetDiagnosisIcon(diagnosisTarget);
        SetIcon(_diagnosisIconRoot, _diagnosisIconImage, diagnosisIcon);
    }

    private void UpdateTimer(EmergencyEventKind kind, float remainingTime)
    {
        if (_timer == null)
        {
            return;
        }

        StageEmergencySettings emergencySettings = _stageFlowManager.CurrentStageData?.Settings?.EmergencySettings;
        float duration = kind == EmergencyEventKind.Tray
            ? emergencySettings?.TrayDurationSec ?? 0f
            : emergencySettings?.DiagnosisDurationSec ?? 0f;

        if (duration <= 0f)
        {
            SetTimerRatio(0f);
            return;
        }

        SetTimerRatio(Mathf.Clamp01(remainingTime / duration));
    }

    private void InitializeTimer()
    {
        _timer ??= new RadialTimerView();
        _timer.Initialize();
    }

    private void SetTimerRatio(float ratio)
    {
        if (_timer != null)
        {
            _timer.SetRatio(ratio);
        }
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

    private static void SetIcon(GameObject root, Image image, Sprite sprite)
    {
        SetImageSprite(image, sprite);

        GameObject iconRoot = root != null
            ? root
            : image != null ? image.gameObject : null;

        if (iconRoot != null)
        {
            iconRoot.SetActive(sprite != null);
        }
    }

    private void ResetDisplayState()
    {
        _wasVisible = false;
        _displayedKind = EmergencyEventKind.None;
        _displayedTrayTarget = CraftedMaterialType.None;
        _displayedDiagnosisTarget = DiagnosisScanType.None;
    }
}
