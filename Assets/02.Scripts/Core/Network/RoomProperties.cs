using ExitGames.Client.Photon;
using Photon.Pun;

public static class RoomProperties
{
    public const string SelectedStageKey = "SelectedStage";
    public const string IsGameInProgressKey = "IsGameInProgress";
    public const string IsStageDataPrepCompleteKey = "IsStageDataPrepComplete";

    public static void EnsureProperties()
    {
        if (PhotonNetwork.CurrentRoom == null) return;
        if (!PhotonNetwork.IsMasterClient) return;

        var props = new Hashtable();

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(SelectedStageKey))
            props[SelectedStageKey] = -1;

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(IsGameInProgressKey))
            props[IsGameInProgressKey] = false;

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(IsStageDataPrepCompleteKey))
            props[IsStageDataPrepCompleteKey] = false;

        if (props.Count > 0)
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public static void SetSelectedStage(int stageIndex)
    {
        if (PhotonNetwork.CurrentRoom == null) return;
        if (!PhotonNetwork.IsMasterClient) return;

        var props = new Hashtable
        {
            { SelectedStageKey, stageIndex },
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public static void SetGameInProgress(bool isInProgress)
    {
        if (PhotonNetwork.CurrentRoom == null) return;
        if (!PhotonNetwork.IsMasterClient) return;

        var props = new Hashtable
        {
            { IsGameInProgressKey, isInProgress },
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public static void SetStageDataPrepComplete(bool isComplete)
    {
        if (PhotonNetwork.CurrentRoom == null) return;
        if (!PhotonNetwork.IsMasterClient) return;

        var props = new Hashtable
        {
            { IsStageDataPrepCompleteKey, isComplete },
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public static int GetSelectedStage(int fallback = -1)
    {
        if (PhotonNetwork.CurrentRoom == null) return fallback;

        if (PhotonNetwork.CurrentRoom.CustomProperties
            .TryGetValue(SelectedStageKey, out object value) && value is int index)
        {
            return index;
        }

        return fallback;
    }

    public static bool GetGameInProgress()
    {
        if (PhotonNetwork.CurrentRoom == null) return false;

        if (PhotonNetwork.CurrentRoom.CustomProperties
            .TryGetValue(IsGameInProgressKey, out object value) && value is bool isInProgress)
        {
            return isInProgress;
        }

        return false;
    }

    public static bool GetStageDataPrepComplete()
    {
        if (PhotonNetwork.CurrentRoom == null) return false;

        if (PhotonNetwork.CurrentRoom.CustomProperties
            .TryGetValue(IsStageDataPrepCompleteKey, out object value) && value is bool isComplete)
        {
            return isComplete;
        }

        return false;
    }
}
