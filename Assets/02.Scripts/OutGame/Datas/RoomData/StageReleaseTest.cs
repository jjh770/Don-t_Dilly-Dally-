using DontDillyDally.StageFlow;
using UnityEngine;

public class StageReleaseTest : MonoBehaviour
{

    [ContextMenu("해금")]
    public void DebugUnlockStage()
    {
        RoomDataManager.Instance.TryUpgradeHospital();
    }

    public void Start()
    {
        DontDestroyOnLoad(gameObject);
    }
}
