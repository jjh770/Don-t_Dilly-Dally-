using System.Collections.Generic;
using DontDillyDally.StageFlow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_StagePanelView : MonoBehaviour
{
    [SerializeField] private UI_StageItemView _itemTemplate;
    [SerializeField] private Transform _contentRoot;
    [SerializeField] private ToggleGroup _toggleGroup;
    [SerializeField] private TextMeshProUGUI _selectedStageNameText;
    [SerializeField] private TextMeshProUGUI _selectedStageDescriptionText;

    private readonly List<UI_StageItemView> _items = new();
    private UI_StagePanelPresenter _presenter;
    private bool _templatePrepared;

    private void Awake()
    {
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
    }

    private void OnEnable()
    {
        _presenter?.Initialize();
    }

    public void Initialize(UI_StagePanelPresenter presenter)
    {
        _presenter = presenter;
    }

    public void Render(IReadOnlyList<StageDefinitionSO> stages, RoomDataManager roomDataManager, int selectedStageIndex)
    {
        PrepareTemplate();
        ClearItems();

        if (_itemTemplate == null || _contentRoot == null || stages == null || roomDataManager == null)
        {
            return;
        }

        for (int i = 0; i < stages.Count; i++)
        {
            StageDefinitionSO stage = stages[i];
            if (stage == null)
            {
                continue;
            }

            UI_StageItemView item = Instantiate(_itemTemplate, _contentRoot);
            item.gameObject.name = $"StageCard_{i}";
            item.gameObject.SetActive(true);
            item.OnSelected += HandleItemSelected;
            item.Initialize(
                i,
                i + 1,
                stage.StageName,
                stage.StageThumbnail,
                roomDataManager.IsStageAvailable(stage),
                _toggleGroup);
            _items.Add(item);
        }

        StageDefinitionSO selectedStage = stages[selectedStageIndex];
        SetSelectedStage(selectedStageIndex, selectedStage.StageName, selectedStage.Description);
    }

    public void SetSelectedStage(int selectedStageIndex, string name, string description)
    {
        _items[selectedStageIndex].SetSelectedWithoutNotify(true);
        _selectedStageNameText.text = name;
        _selectedStageDescriptionText.text = description;
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
}
