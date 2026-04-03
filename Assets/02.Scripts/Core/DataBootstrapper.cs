using Cysharp.Threading.Tasks;
using UnityEngine;

public class DataBootstrapper : MonoBehaviour
{
    [Header("출석체크")]
    [SerializeField] private AttendanceManager _attendanceManager;
    [SerializeField] private AttendanceRewardSO _rewardSO;

    [Header("세팅")]
    [SerializeField] private string _testUserId = "local_user";

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
        //ICustomizingRepository customizingRepository = new FirebaseCustomizingRepository(FirebaseInitializer.Instance.Database, PlayerDataManager.Instance.PlayerID);
        ICustomizingRepository customizingRepository = new LocalCustomizingRepository(_testUserId);


        RoomDataManager.Instance.Initialize(roomDataRepository);
        CustomizingManager.Instance.Initialize(customizingRepository);

        // PlayerData 준비 완료 후 AttendanceManager 초기화
        PlayerDataManager.Instance.OnDataManagerReady += InitializedAttendance;
        PlayerDataManager.Instance.Initialize(playerRepository);

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
