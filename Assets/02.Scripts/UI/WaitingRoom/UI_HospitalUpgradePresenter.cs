public class UI_HospitalUpgradePresenter
{
    private readonly UI_HospitalUpgradeView _view;
    private readonly RoomDataManager _roomDataManager;

    public UI_HospitalUpgradePresenter(UI_HospitalUpgradeView view)
    {
        _view = view;
        _roomDataManager = RoomDataManager.Instance;

        if (_roomDataManager == null)
        {
            return;
        }

        _roomDataManager.OnRoomDataLoaded += Render;
        _roomDataManager.OnRoomDataChanged += HandleRoomDataChanged;
        _roomDataManager.OnHospitalUpgraded += HandleHospitalUpgraded;
    }

    public void Initialize()
    {
        Render();
    }

    public void TryUpgradeHospital()
    {
        _roomDataManager?.TryUpgradeHospital();
    }

    public void Dispose()
    {
        if (_roomDataManager == null)
        {
            return;
        }

        _roomDataManager.OnRoomDataLoaded -= Render;
        _roomDataManager.OnRoomDataChanged -= HandleRoomDataChanged;
        _roomDataManager.OnHospitalUpgraded -= HandleHospitalUpgraded;
    }

    private void HandleRoomDataChanged(int _, int __)
    {
        Render();
    }

    private void HandleHospitalUpgraded(HospitalLevelDefinitionSO _)
    {
        Render();
    }

    private void Render()
    {
        if (_view == null || _roomDataManager == null)
        {
            return;
        }

        if (!_roomDataManager.HasRoomWallet || _roomDataManager.CurrentLevelDefinition == null)
        {
            return;
        }

        HospitalLevelDefinitionSO nextLevel = _roomDataManager.NextLevelDefinition;
        bool hasNextLevel = nextLevel != null;
        int requiredCoin = hasNextLevel ? nextLevel.UpgradeCost : 0;
        int requiredStar = hasNextLevel ? nextLevel.RequiredStars : 0;
        int currentCoin = _roomDataManager.Coin.Value;
        int currentStar = _roomDataManager.Star;
        bool canUpgrade = hasNextLevel &&
                          currentCoin >= requiredCoin &&
                          currentStar >= requiredStar;

        _view.Render(currentCoin, requiredCoin, currentStar, requiredStar, canUpgrade, hasNextLevel, PhotonServerManager.Instance.IsMasterClient);
    }
}
