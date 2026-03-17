
public class PlayerModel
{
    public string Nickname { get; private set; }
    public bool IsReady { get; private set; }

    public bool IsMaster { get; private set; }

    public PlayerModel(string nickname, bool isReady, bool isMaster)
    {
        Nickname = nickname;
        IsReady = isReady;
        IsMaster = isMaster;
    }

    public void SetNickname(string nickname)
    {
        Nickname = nickname;
    }

    public void SetReadyState(bool isReady)
    {
        IsReady = isReady;
    }
    public void SetIsMaster(bool isMaster)
    {
        IsMaster = isMaster;
    }
}
