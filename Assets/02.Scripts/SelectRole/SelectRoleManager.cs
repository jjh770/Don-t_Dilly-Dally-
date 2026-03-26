using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class SelectRoleManager : MonoBehaviour
{
    private int SelectSurgeon()
    {
        Player[] players = PhotonNetwork.PlayerList;
        int randomIndex = UnityEngine.Random.Range(0, players.Length);
        int selectedActorNumber = players[randomIndex].ActorNumber;

        Debug.Log($"[StageFlow] 집도의 선정: Actor {selectedActorNumber}");
        return selectedActorNumber;
    }
}
