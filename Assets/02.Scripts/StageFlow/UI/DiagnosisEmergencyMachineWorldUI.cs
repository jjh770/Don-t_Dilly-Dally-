using DontDillyDally.StageFlow;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DiagnosisEmergencyMachineWorldUI : MonoBehaviour
{
    private const float DiagnosisOperationDurationSec = 5f;

    [Header("References")]
    [SerializeField] private DiagnosisEmergencyMachine _machine;
    [SerializeField] private GameObject _root;
    [SerializeField] private Image _timerFillImage;

    private StageFlowManager _stageFlowManager;
    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        if (_machine == null)
        {
            _machine = GetComponentInParent<DiagnosisEmergencyMachine>();
        }

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

        bool shouldShow = _machine != null && _machine.ShouldShowOperationTimerUi(_stageFlowManager);
        SetVisible(shouldShow);
        if (!shouldShow)
        {
            return;
        }

        if (_timerFillImage != null)
        {
            float remainingRatio = Mathf.Clamp01(
                _stageFlowManager.EmergencyDiagnosisOperationRemainingTime / DiagnosisOperationDurationSec);
            _timerFillImage.fillAmount = 1f - remainingRatio;
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
}
