using Photon.Realtime;

public class WaitingRoomModel
{
    public bool IsMaster { get; private set; }
    public bool IsReady { get; private set; }

    public Player SelectedPlayer { get; private set; }

    public WaitingRoomModel(bool isMaster, bool isReady)
    {
        IsMaster = isMaster;
        IsReady = isReady;
        SelectedPlayer = null;
    }

    public void ToggleReady()
    {
        IsReady = !IsReady;
    }

    public void SetIsMaster(bool isMaster)
    {
        IsMaster = isMaster;
    }

    public void SetSelectedPlayer(Player player)
    {
        SelectedPlayer = player;
    }
}