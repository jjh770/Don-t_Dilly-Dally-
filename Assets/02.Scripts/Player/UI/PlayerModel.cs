using UnityEngine;

public class PlayerModel
{
    public string Nickname { get; private set; }
    public bool IsReady { get; private set; }

    public PlayerModel(string nickname, bool isReady)
    {
        Nickname = nickname;
        IsReady = isReady;
    }

    public void SetNickname(string nickname)
    {
        Nickname = nickname;
    }

    public void SetReadyState(bool isReady)
    {
        IsReady = isReady;
    }
}
