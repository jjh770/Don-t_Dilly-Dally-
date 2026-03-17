public class WaitingRoomModel
{
    public bool IsMaster { get; private set; }
    public bool IsReady { get; private set; }

    public WaitingRoomModel(bool isMaster, bool isReady)
    {
        IsMaster = isMaster;
        IsReady = isReady;
    }

    public void ToggleReady()
    {
        IsReady = !IsReady;
    }

    public void SetIsMater(bool isMaster)
    {
        IsMaster = isMaster;
    }
}