using DontDillyDally.StageFlow;
using UnityEngine;

public class StageReleaseTest : MonoBehaviour
{
    [SerializeField] private StageDefinitionSO _targetStage;

    [ContextMenu("해금")]
    public void DebugUnlockStage()
    {
        RoomDataManager.Instance.TryUnlockStage(_targetStage);
    }

    public void Start()
    {
        DontDestroyOnLoad(gameObject);
    }
}
