using System.Collections.Generic;
using DontDillyDally.StageFlow;

public class UI_StagePanelPresenter
{
    private readonly UI_StagePanelView _view;
    private readonly RoomDataManager _roomDataManager;
    private readonly List<UI_StagePanelItemData> _stageItems = new();

    public UI_StagePanelPresenter(UI_StagePanelView view)
    {
        _view = view;
        _roomDataManager = RoomDataManager.Instance;

        if (_roomDataManager == null)
        {
            return;
        }

        _roomDataManager.OnRoomDataLoaded += Render;
        _roomDataManager.OnHospitalUpgraded += HandleHospitalUpgraded;
        _roomDataManager.OnSelectedStageChanged += HandleSelectedStageChanged;
        if (PhotonServerManager.Instance != null)
        {
            PhotonServerManager.Instance.OnMasterClientChanged += HandleMasterClientChanged;
        }
    }

    public void Initialize()
    {
        Render();
    }

    public void SelectStage(int stageIndex)
    {
        _roomDataManager?.TrySelectStage(stageIndex);
    }

    public void Dispose()
    {
        if (_roomDataManager == null)
        {
            return;
        }

        _roomDataManager.OnRoomDataLoaded -= Render;
        _roomDataManager.OnHospitalUpgraded -= HandleHospitalUpgraded;
        _roomDataManager.OnSelectedStageChanged -= HandleSelectedStageChanged;
        if (PhotonServerManager.Instance != null)
        {
            PhotonServerManager.Instance.OnMasterClientChanged -= HandleMasterClientChanged;
        }
    }

    private void HandleHospitalUpgraded(HospitalLevelDefinitionSO _)
    {
        Render();
    }

    private void HandleSelectedStageChanged(int selectedStageIndex, StageDefinitionSO selectedStage)
    {
        if (_view == null)
        {
            return;
        }

        _view.SetConfirmedStageIndex(selectedStageIndex);
    }

    private void HandleMasterClientChanged()
    {
        _view?.SetMasterClient(PhotonServerManager.Instance != null && PhotonServerManager.Instance.IsMasterClient);
    }

    private void Render()
    {
        if (_view == null || _roomDataManager == null)
        {
            return;
        }

        BuildStageItems();
        _view.Render(
            _stageItems,
            _roomDataManager.SelectedStageIndex,
            PhotonServerManager.Instance != null && PhotonServerManager.Instance.IsMasterClient);
    }

    private void BuildStageItems()
    {
        _stageItems.Clear();

        IReadOnlyList<StageDefinitionSO> stageDefinitions = _roomDataManager.StageDefinitions;
        for (int i = 0; i < stageDefinitions.Count; i++)
        {
            StageDefinitionSO stage = stageDefinitions[i];
            _stageItems.Add(new UI_StagePanelItemData(
                i,
                i + 1,
                stage != null ? stage.StageName : string.Empty,
                stage != null ? stage.Description : string.Empty,
                stage != null ? stage.StageThumbnail : null,
                stage != null && _roomDataManager.IsStageAvailable(stage)));
        }
    }
}
