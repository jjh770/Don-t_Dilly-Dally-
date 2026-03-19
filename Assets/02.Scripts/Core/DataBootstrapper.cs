using Cysharp.Threading.Tasks;
using UnityEngine;

public class DataBootstrapper : MonoBehaviour
{
    [SerializeField] private RoomDataManager roomDataManager;
    
    private async void Start()
    {
        await WaitForFirebaseAsync();

        // Repository 생성
        Debug.Log("Data 조회 가능");
        IRoomCurrencyRepository roomDataRepository = new RoomCurrencyFirebaseRepository();
        roomDataManager.Initialized(roomDataRepository);
    }

    private async UniTask WaitForFirebaseAsync()
    {
        // FirebaseManager가 준비될 때까지 대기
        while (FirebaseInitializer.Instance == null ||
               !FirebaseInitializer.Instance.IsInitialized)
        {
            await UniTask.Yield();
        }
    }
}
