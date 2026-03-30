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
            if (!PlayerDataManager.Instance.IsReady)
            {
                OnFirebaseSetComplete();
            }
            else
            {
                InitializedAttendance();
            }                
        }
    }

    private void OnFirebaseSetComplete()
    {
        // Repository 생성
        IRoomCurrencyRepository roomDataRepository = new RoomCurrencyFirebaseRepository(FirebaseInitializer.Instance.Database);
        IPlayerInformationRepository playerRepository = new PlayerInformationTestRepository(FirebaseInitializer.Instance.Database);

        RoomDataManager.Instance.Initialized(roomDataRepository);

        // PlayerData 준비 완료 후 AttendanceManager 초기화
        PlayerDataManager.Instance.OnDataManagerReady += InitializedAttendance;
        PlayerDataManager.Instance.Initialized(playerRepository);

        Debug.Log("[DataBootstrapper] Data 조회 가능");
    }

    private void InitializedAttendance()
    {
        IAttendanceRepository attendanceRepository = new FirebaseAttendanceRepository(FirebaseInitializer.Instance.Database);
        _attendanceManager.Initialize(attendanceRepository, _rewardRepository, PlayerDataManager.Instance.PlayerID);
    }

    private void OnDestroy()
    {
        FirebaseInitializer.OnFirebaseInitialized -= OnFirebaseSetComplete;
        PlayerDataManager.Instance.OnDataManagerReady -= InitializedAttendance;
    }
}
