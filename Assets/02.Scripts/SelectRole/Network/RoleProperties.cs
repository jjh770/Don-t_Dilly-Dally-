using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public static class RoleProperties
{
    private const string ROLE_KEY = "role";

    public static void SetLocalPlayerRole(RoleType role)
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.LocalPlayer == null)
        {
            Debug.LogWarning("[RoleProperties] Photon에 연결되지 않음");
            return;
        }

        var props = new Hashtable { { ROLE_KEY, (int)role } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        Debug.Log($"[RoleProperties] 로컬 플레이어 역할 설정: {role}");
    }

    public static RoleType GetPlayerRole(Player player)
    {
        if (player == null)
            return RoleType.None;

        if (player.CustomProperties.TryGetValue(ROLE_KEY, out object data))
        {
            return (RoleType)(int)data;
        }

        return RoleType.None;
    }

    public static bool TryGetFromChangedProps(Hashtable changedProps, out RoleType role)
    {
        role = RoleType.None;

        if (changedProps.TryGetValue(ROLE_KEY, out object data))
        {
            role = (RoleType)(int)data;
            return true;
        }

        return false;
    }

    public static void ClearLocalPlayerRole()
    {
        SetLocalPlayerRole(RoleType.None);
    }
}
