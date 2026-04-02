public class UI_StagePanelPresenter
{
    private readonly UI_StagePanelView _view;
    private readonly RoomDataManager _roomDataManager;

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
    }

    private void HandleHospitalUpgraded(HospitalLevelDefinitionSO _)
    {
        Render();
    }

    private void HandleSelectedStageChanged(int selectedStageIndex)
    {
        _view?.SetSelectedStage(selectedStageIndex);
    }

    private void Render()
    {
        if (_view == null || _roomDataManager == null)
        {
            return;
        }

        _view.Render(_roomDataManager.StageDefinitions, _roomDataManager, _roomDataManager.SelectedStageIndex);
    }
}
