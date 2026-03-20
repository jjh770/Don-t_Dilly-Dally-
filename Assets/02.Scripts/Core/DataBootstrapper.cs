using Cysharp.Threading.Tasks;
using UnityEngine;

public class DataBootstrapper : MonoBehaviour
{
    [SerializeField] private RoomDataManager _roomDataManager;
    [SerializeField] private PlayerDataManager _playerDataManager;

    private async void Start()
    {
        await WaitForFirebaseAsync();

        // Repository 생성
        IRoomCurrencyRepository roomDataRepository = new RoomCurrencyFirebaseRepository();
        IPlayerInformationRepository playerRepository = new PlayerInformationFirebaseRepository();

        _roomDataManager.Initialized(roomDataRepository);
        _playerDataManager.Initialized(playerRepository);

        Debug.Log("[DataBootstrapper] Data 조회 가능");
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
