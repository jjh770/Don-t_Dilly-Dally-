using Photon.Realtime;

public class WaitingRoomModel
{
    public Player SelectedPlayer { get; private set; }

    public WaitingRoomModel()
    {
        SelectedPlayer = null;
    }

    public void SetSelectedPlayer(Player player)
    {
        SelectedPlayer = player;
    }
}
