using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

public static class PlayerProperty
{
    public const string NicknameKey = "Nickname";
    public const string IsReadyKey = "IsReady";
    public const string IsVoiceSpeakingKey = "IsVoiceSpeaking";
    public const string IsVoiceMutedKey = "IsVoiceMuted";

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

        if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(IsVoiceSpeakingKey))
        {
            props[IsVoiceSpeakingKey] = false;
        }

        if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(IsVoiceMutedKey))
        {
            props[IsVoiceMutedKey] = false;
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
            { NicknameKey, nickname },
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public static void SetReadyState(bool isReady)
    {
        Hashtable props = new Hashtable
        {
            { IsReadyKey, isReady },
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public static void SetVoiceSpeaking(bool isSpeaking)
    {
        if (PhotonNetwork.LocalPlayer == null)
        {
            return;
        }

        Hashtable props = new Hashtable
        {
            { IsVoiceSpeakingKey, isSpeaking },
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public static void SetVoiceMuted(bool isMuted)
    {
        if (PhotonNetwork.LocalPlayer == null)
        {
            return;
        }

        Hashtable props = new Hashtable
        {
            { IsVoiceMutedKey, isMuted },
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public static string GetNickname(Player player)
    {
        if (player == null)
        {
            return "Unknown";
        }

        if (player.CustomProperties.TryGetValue(NicknameKey, out object value) && value is string name)
        {
            return name;
        }

        if (!string.IsNullOrWhiteSpace(player.NickName))
        {
            return player.NickName;
        }

        return "Unknown";
    }

    public static bool GetReadyState(Player player)
    {
        if (player != null && player.CustomProperties.TryGetValue(IsReadyKey, out object value) && value is bool isReady)
        {
            return isReady;
        }

        return false;
    }

    public static bool GetVoiceSpeaking(Player player)
    {
        if (player != null && player.CustomProperties.TryGetValue(IsVoiceSpeakingKey, out object value) && value is bool isSpeaking)
        {
            return isSpeaking;
        }

        return false;
    }

    public static bool GetVoiceMuted(Player player)
    {
        if (player != null && player.CustomProperties.TryGetValue(IsVoiceMutedKey, out object value) && value is bool isMuted)
        {
            return isMuted;
        }

        return false;
    }
}
