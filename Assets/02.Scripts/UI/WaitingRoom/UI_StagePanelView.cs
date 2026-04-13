using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_StagePanelView : UIPopupBase
{
    [SerializeField] private UI_StageItemView _itemTemplate;
    [SerializeField] private Transform _contentRoot;
    [SerializeField] private ToggleGroup _toggleGroup;
    [SerializeField] private TextMeshProUGUI _selectedStageNameText;
    [SerializeField] private TextMeshProUGUI _selectedStageDescriptionText;

    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _selectButton;

    private readonly List<UI_StageItemView> _items = new();
    private UI_StagePanelPresenter _presenter;
    private IReadOnlyList<UI_StagePanelItemData> _stages;
    private int _viewedStageIndex = -1;
    private int _confirmedStageIndex = -1;
    private bool _isMasterClient;
    private bool _templatePrepared;

    protected override void Awake()
    {
        base.Awake();

        if (_contentRoot == null && _itemTemplate != null)
        {
            _contentRoot = _itemTemplate.transform.parent;
        }

        if (_toggleGroup == null)
        {
            _toggleGroup = GetComponentInChildren<ToggleGroup>(true);
        }

        if (_itemTemplate != null)
        {
            _itemTemplate.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(Hide);
        }
        if (_selectButton != null)
        {
            _selectButton.onClick.RemoveListener(HandleSelectButtonClicked);
        }
    }

    private void OnDestroy()
    {
        ClearItems();
    }

    private void OnEnable()
    {
        _presenter?.Initialize();
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(Hide);
        }
        if (_selectButton != null)
        {
            _selectButton.onClick.AddListener(HandleSelectButtonClicked);
        }
    }

    public void Initialize(UI_StagePanelPresenter presenter)
    {
        _presenter = presenter;
    }

    public void Render(IReadOnlyList<UI_StagePanelItemData> stages, int selectedStageIndex, bool isMasterClient)
    {
        PrepareTemplate();
        _stages = stages;
        _confirmedStageIndex = selectedStageIndex;
        _isMasterClient = isMasterClient;
        _viewedStageIndex = selectedStageIndex;

        if (_itemTemplate == null || _contentRoot == null || stages == null)
        {
            RefreshSelectButtonState();
            return;
        }

        if (_items.Count != stages.Count)
        {
            ClearItems();
            for (int i = 0; i < stages.Count; i++)
            {
                UI_StageItemView item = Instantiate(_itemTemplate, _contentRoot);
                item.gameObject.name = $"StageCard_{i}";
                item.gameObject.SetActive(true);
                item.OnSelected += HandleItemSelected;
                _items.Add(item);
            }
        }

        // 매번 데이터만 갱신
        for (int i = 0; i < stages.Count; i++)
        {
            _items[i].Initialize(
                stages[i].StageIndex,
                stages[i].StageNumber,
                stages[i].StageName,
                stages[i].StageThumbnail,
                stages[i].IsAvailable,
                _toggleGroup);
        }

        if (selectedStageIndex < 0 || selectedStageIndex >= stages.Count)
        {
            RefreshConfirmedStageVisual();
            RefreshSelectButtonState();
            return;
        }

        SetViewedStage(stages[selectedStageIndex]);
        RefreshConfirmedStageVisual();
    }

    public void SetViewedStage(UI_StagePanelItemData selectedStage)
    {
        if (selectedStage.StageIndex < 0 || selectedStage.StageIndex >= _items.Count)
        {
            return;
        }

        _items[selectedStage.StageIndex].SetSelectedWithoutNotify(true);
        _viewedStageIndex = selectedStage.StageIndex;
        _selectedStageNameText.text = selectedStage.StageName;
        _selectedStageDescriptionText.text = selectedStage.Description;
        RefreshSelectButtonState();
    }

    public void SetConfirmedStageIndex(int selectedStageIndex)
    {
        _confirmedStageIndex = selectedStageIndex;
        RefreshConfirmedStageVisual();
        RefreshSelectButtonState();
    }

    public void SetMasterClient(bool isMasterClient)
    {
        _isMasterClient = isMasterClient;
        RefreshSelectButtonState();
    }

    private void HandleItemSelected(int stageIndex)
    {
        if (_stages == null || stageIndex < 0 || stageIndex >= _stages.Count)
        {
            return;
        }

        SetViewedStage(_stages[stageIndex]);
    }

    private void HandleSelectButtonClicked()
    {
        if (_viewedStageIndex < 0)
        {
            return;
        }

        _presenter?.SelectStage(_viewedStageIndex);
    }

    private void ClearItems()
    {
        foreach (UI_StageItemView item in _items)
        {
            if (item == null)
            {
                continue;
            }

            item.OnSelected -= HandleItemSelected;
            Destroy(item.gameObject);
        }

        _items.Clear();
    }

    private void PrepareTemplate()
    {
        if (_templatePrepared || _contentRoot == null || _itemTemplate == null)
        {
            return;
        }

        for (int i = _contentRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = _contentRoot.GetChild(i);
            if (child == _itemTemplate.transform)
            {
                continue;
            }

            Destroy(child.gameObject);
        }

        _templatePrepared = true;
    }

    private void RefreshConfirmedStageVisual()
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i] == null)
            {
                continue;
            }

            _items[i].SetConfirmedSelected(i == _confirmedStageIndex);
        }
    }

    private void RefreshSelectButtonState()
    {
        if (_selectButton == null)
        {
            return;
        }

        _selectButton.gameObject.SetActive(_isMasterClient);

        if (!_isMasterClient)
        {
            return;
        }

        bool canSelect =
            _stages != null &&
            _viewedStageIndex >= 0 &&
            _viewedStageIndex < _stages.Count &&
            _stages[_viewedStageIndex].IsAvailable &&
            _viewedStageIndex != _confirmedStageIndex;

        _selectButton.interactable = canSelect;
    }

    public override void Show()
    {
        base.Show();
        if (_stages == null || _confirmedStageIndex < 0 || _confirmedStageIndex >= _stages.Count)
        {
            Debug.LogWarning("[UI_StagePanelView] No confirmed stage data available when showing stage panel.");
            return;
        }

        SetViewedStage(_stages[_confirmedStageIndex]);
    }
    protected override void OnShow()
    {
        
    }
}
