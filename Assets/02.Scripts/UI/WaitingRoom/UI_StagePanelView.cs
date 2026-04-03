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

    private readonly List<UI_StageItemView> _items = new();
    private UI_StagePanelPresenter _presenter;
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
        ClearItems();
        _closeButton.onClick.RemoveListener(Hide);    
    }

    private void OnEnable()
    {
        _presenter?.Initialize();
        _closeButton.onClick.AddListener(Hide);
    }

    public void Initialize(UI_StagePanelPresenter presenter)
    {
        _presenter = presenter;
    }

    public void Render(IReadOnlyList<UI_StagePanelItemData> stages, int selectedStageIndex)
    {
        PrepareTemplate();
        ClearItems();

        if (_itemTemplate == null || _contentRoot == null || stages == null)
        {
            return;
        }

        for (int i = 0; i < stages.Count; i++)
        {
            UI_StagePanelItemData stage = stages[i];

            UI_StageItemView item = Instantiate(_itemTemplate, _contentRoot);
            item.gameObject.name = $"StageCard_{i}";
            item.gameObject.SetActive(true);
            item.OnSelected += HandleItemSelected;
            item.Initialize(
                stage.StageIndex,
                stage.StageNumber,
                stage.StageName,
                stage.StageThumbnail,
                stage.IsAvailable,
                _toggleGroup);
            _items.Add(item);
        }

        if (selectedStageIndex < 0 || selectedStageIndex >= stages.Count)
        {
            return;
        }

        SetSelectedStage(stages[selectedStageIndex]);
    }

    public void SetSelectedStage(UI_StagePanelItemData selectedStage)
    {
        if (selectedStage.StageIndex < 0 || selectedStage.StageIndex >= _items.Count)
        {
            return;
        }

        _items[selectedStage.StageIndex].SetSelectedWithoutNotify(true);
        _selectedStageNameText.text = selectedStage.StageName;
        _selectedStageDescriptionText.text = selectedStage.Description;
    }

    private void HandleItemSelected(int stageIndex)
    {
        _presenter?.SelectStage(stageIndex);
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

    protected override void OnShow()
    {
       
    }
}
