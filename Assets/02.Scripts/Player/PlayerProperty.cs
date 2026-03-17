
using System.Globalization;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

public static class PlayerProperty
{
    public const string NicknameKey = "Nickname";
    public const string IsReadyKey = "IsReady";

    public static void EnsureProperties()
    {
        if (PhotonNetwork.LocalPlayer == null)
        {
            return;
        }

        Hashtable props = new Hashtable();

        if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(NicknameKey))
        {
            props[NicknameKey] = PhotonNetwork.NickName;
        }

        if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(IsReadyKey))
        {
            props[IsReadyKey] = false;
        }

        if (props.Count > 0)
        {
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
    }

    public static void SetNickname(string nickname)
    {
        Hashtable props = new Hashtable
        {
            { PlayerProperty.NicknameKey, nickname },
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public static void SetReadyState(bool isReady)
    {
        Hashtable props = new Hashtable
        {
            { PlayerProperty.IsReadyKey, isReady },
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public static string GetNickname(Player player)
    {
        if (player.CustomProperties.TryGetValue(NicknameKey, out object value) && value is string name)
        {
            return name;
        }
        else
        {
            return "Unknown";
        }
    }

    public static bool GetReadyState(Player player)
    {
        if (player.CustomProperties.TryGetValue(IsReadyKey, out object value) && value is bool isReady)
        {
            return isReady;
        }
        else
        {
            return false;
        }
    }
}