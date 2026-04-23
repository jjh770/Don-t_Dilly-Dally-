using Photon.Pun;
using UnityEngine;

public static class LogoutService
{
    public static void Logout()
    {
        GoogleAuthManager.Instance?.Logout();

        if (PhotonNetwork.InRoom)
        {
            PhotonServerManager.Instance?.LeaveRoom();
        }

        DestroyIfExists(PhotonVoiceManager.Instance);
        DestroyIfExists(PortraitManager.Instance);
        DestroyIfExists(RoomDataManager.Instance);
        DestroyIfExists(CustomizingManager.Instance);
        DestroyIfExists(PlayerDataManager.Instance);

        SceneLoadManager.Instance?.BeginSceneLoad(ESceneType.MainMenu);
    }

    private static void DestroyIfExists(MonoBehaviour manager)
    {
        if (manager == null)
        {
            return;
        }

        Object.Destroy(manager.gameObject);
    }
}
