using Cysharp.Threading.Tasks;
using UnityEngine;

public class DataBootstrapper : MonoBehaviour
{
    [Header("출석체크")]
    [SerializeField] private AttendanceManager _attendanceManager;
    [SerializeField] private AttendanceRewardSO _rewardSO;

    private IRewardRepository _rewardRepository;
   
    private void Awake()
    {
        FirebaseInitializer.OnFirebaseInitialized += OnFirebaseSetComplete;

        _rewardRepository = _rewardSO;    
    }

    private void Start()
    {
        if (FirebaseInitializer.Instance.IsFirebaseInitialized)
        {
            IAttendanceRepository attendanceRepository = new FirebaseAttendanceRepository(FirebaseInitializer.Instance.Database);
            _attendanceManager.Initialize(attendanceRepository, _rewardRepository, PlayerDataManager.Instance.PlayerID);
        }
    }

    private void OnFirebaseSetComplete()
    {
        // Repository 생성
        IRoomCurrencyRepository roomDataRepository = new RoomCurrencyFirebaseRepository(FirebaseInitializer.Instance.Database);
        IPlayerInformationRepository playerRepository = new PlayerInformationFirebaseRepository(FirebaseInitializer.Instance.Database);
        IAttendanceRepository attendanceRepository = new FirebaseAttendanceRepository(FirebaseInitializer.Instance.Database);

        RoomDataManager.Instance.Initialized(roomDataRepository);
        PlayerDataManager.Instance.Initialized(playerRepository);
        _attendanceManager.Initialize(attendanceRepository, _rewardRepository, PlayerDataManager.Instance.PlayerID);

        Debug.Log("[DataBootstrapper] Data 조회 가능");
    }

    private void OnDestroy()
    {
        FirebaseInitializer.OnFirebaseInitialized -= OnFirebaseSetComplete;
    }
}
