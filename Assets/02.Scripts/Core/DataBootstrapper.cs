using Cysharp.Threading.Tasks;
using UnityEngine;

public class DataBootstrapper : MonoBehaviour
{
    [SerializeField] private RoomDataManager _roomDataManager;
    [SerializeField] private PlayerDataManager _playerDataManager;

    private void Awake()
    {
        FirebaseInitializer.OnFirebaseInitialized += OnFirebaseSetComplete;
    }

    private void OnFirebaseSetComplete()
    {
        // Repository 생성
        IRoomCurrencyRepository roomDataRepository = new RoomCurrencyFirebaseRepository(FirebaseInitializer.Instance.Database);
        IPlayerInformationRepository playerRepository = new PlayerInformationFirebaseRepository(FirebaseInitializer.Instance.Database);

        _roomDataManager.Initialized(roomDataRepository);
        _playerDataManager.Initialized(playerRepository);

        Debug.Log("[DataBootstrapper] Data 조회 가능");
    }

    private void OnDestroy()
    {
        FirebaseInitializer.OnFirebaseInitialized -= OnFirebaseSetComplete;
    }
}
