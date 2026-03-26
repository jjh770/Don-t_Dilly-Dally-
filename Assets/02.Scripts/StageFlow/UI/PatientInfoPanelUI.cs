using DG.Tweening;
using DontDillyDally.Data;
using DontDillyDally.StageFlow;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전체 환자 정보를 보여주는 슬라이드 패널입니다.
/// Tab 키를 누르고 있으면 우측에서 슬라이드 인, 떼면 슬라이드 아웃합니다.
/// ScrollRect + Mask가 적용된 세로 리스트로 구성합니다.
/// </summary>
public class PatientInfoPanelUI : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private RectTransform _panelRoot;

    [Header("환자 항목 프리팹")]
    [SerializeField] private PatientInfoEntry _entryPrefab;
    [SerializeField] private RectTransform _contentRoot;

    [Header("슬라이드 애니메이션")]
    [SerializeField] private float _slideDistance = 500f;
    [SerializeField] private float _slideInDuration = 0.3f;
    [SerializeField] private float _slideOutDuration = 0.25f;
    [SerializeField] private Ease _slideInEase = Ease.OutCubic;
    [SerializeField] private Ease _slideOutEase = Ease.InCubic;

    private StageFlowManager _stageFlowManager;
    private readonly List<PatientInfoEntry> _entries = new List<PatientInfoEntry>();
    private Vector2 _hiddenPos;
    private Vector2 _shownPos;
    private bool _isShowing;
    private bool _isBuilt;
    private Tween _currentTween;

    private void Start()
    {
        if (_panelRoot != null)
        {
            _shownPos = _panelRoot.anchoredPosition;
            _hiddenPos = new Vector2(_shownPos.x + _slideDistance, _shownPos.y);
            _panelRoot.anchoredPosition = _hiddenPos;
        }
    }

    private void Update()
    {
        if (_stageFlowManager == null)
        {
            TryBind();
        }

        if (_stageFlowManager == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ShowPanel();
        }
        else if (Input.GetKeyUp(KeyCode.Tab))
        {
            HidePanel();
        }
    }

    private void OnDestroy()
    {
        _currentTween?.Kill();
    }

    private void TryBind()
    {
        if (StageFlowManager.Instance == null)
        {
            return;
        }

        _stageFlowManager = StageFlowManager.Instance;
    }

    // ================================================================
    //  패널 표시 / 숨기기
    // ================================================================

    private void ShowPanel()
    {
        if (_isShowing || _panelRoot == null)
        {
            return;
        }

        _isShowing = true;

        if (!_isBuilt)
        {
            BuildEntries();
        }

        UpdateCurrentPatientHighlight();

        _currentTween?.Kill();
        _currentTween = _panelRoot
            .DOAnchorPos(_shownPos, _slideInDuration)
            .SetEase(_slideInEase)
            .SetUpdate(true);
    }

    private void HidePanel()
    {
        if (!_isShowing || _panelRoot == null)
        {
            return;
        }

        _isShowing = false;

        _currentTween?.Kill();
        _currentTween = _panelRoot
            .DOAnchorPos(_hiddenPos, _slideOutDuration)
            .SetEase(_slideOutEase)
            .SetUpdate(true);
    }

    // ================================================================
    //  항목 생성
    // ================================================================

    private void BuildEntries()
    {
        ClearEntries();

        StageRuntimeData stageData = _stageFlowManager.CurrentStageData;
        if (stageData == null || stageData.Patients == null)
        {
            return;
        }

        for (int i = 0; i < stageData.Patients.Count; i++)
        {
            DiseaseData disease = stageData.Patients[i];
            PatientInfoEntry entry = Instantiate(_entryPrefab, _contentRoot);
            entry.gameObject.name = $"PatientInfo_{i}";
            entry.SetData(i + 1, disease);
            _entries.Add(entry);
        }

        _isBuilt = true;
    }

    private void UpdateCurrentPatientHighlight()
    {
        int currentIndex = _stageFlowManager.CurrentPatientIndex.Value;

        for (int i = 0; i < _entries.Count; i++)
        {
            _entries[i].SetCurrent(i == currentIndex);
        }
    }

    private void ClearEntries()
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i] != null)
            {
                Destroy(_entries[i].gameObject);
            }
        }

        _entries.Clear();
        _isBuilt = false;
    }
}
