using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;

public class RoomProperties : MonoBehaviour
{
    public const string SelectedStageKey = "SelectedStage";

    public static void EnsureProperties()
    {
        if (PhotonNetwork.CurrentRoom == null) return;
        if (!PhotonNetwork.IsMasterClient) return;

        Hashtable props = new Hashtable();

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(SelectedStageKey))
            props[SelectedStageKey] = -1;

        if (props.Count > 0)
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public static void SetSelectedStage(int stageIndex)
    {
        if (PhotonNetwork.CurrentRoom == null) return;
        if (!PhotonNetwork.IsMasterClient) return;

        Hashtable props = new Hashtable
        {
            { SelectedStageKey, stageIndex },
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
}
