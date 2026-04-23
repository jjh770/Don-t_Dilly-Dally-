using UnityEngine;

public class DataBootstrapper : MonoBehaviour
{
    [Header("Attendance")]
    [SerializeField] private AttendanceManager _attendanceManager;
    [SerializeField] private AttendanceRewardSO _rewardSO;
    [SerializeField] private AttendanceCheckUnit _attendanceCheckUnit = AttendanceCheckUnit.Daily;

    [Header("Testing")]
    [SerializeField] private string _testUserId = "local_user";

    private IRewardRepository _rewardRepository;
    private FirebaseInitializer _firebaseInitializer;
    private PhotonServerManager _photonServerManager;
    private PlayerDataManager _playerDataManager;
    private RoomDataManager _roomDataManager;
    private CustomizingManager _customizingManager;
    private bool _isPlayerDataReadySubscribed;

    private void Awake()
    {
        FirebaseInitializer.OnFirebaseInitialized += OnFirebaseSetComplete;
        LoadingUIService.Show(ELoadingStep.FirebaseInit);

        _rewardRepository = _rewardSO;
        CacheManagers();
    }

    private void Start()
    {
        CacheManagers();

        if (_firebaseInitializer == null || !_firebaseInitializer.IsFirebaseInitialized)
        {
            return;
        }

        if (_playerDataManager != null && !_playerDataManager.IsReady)
        {
            OnFirebaseSetComplete();
        }
        else
        {
            InitializedAttendance();
        }
    }

    private void OnFirebaseSetComplete()
    {
        CacheManagers();

        if (_firebaseInitializer == null) return;
        if (_photonServerManager == null || !_photonServerManager.IsEnabled) return;
        if (_playerDataManager == null) return;
        if (_roomDataManager == null) return;
        if (_customizingManager == null) return;

        LoadingUIService.Show(ELoadingStep.PlayerDataLoad);

        IRoomCurrencyRepository roomDataRepository = new FirebaseRoomCurrencyRepository(_firebaseInitializer.Database);
        IPlayerInformationRepository playerRepository = new FirebasePlayerInformationRepository(_firebaseInitializer.Database, _playerDataManager.PlayerID);
        ICustomizingRepository customizingRepository = new FirebaseCustomizingRepository(_firebaseInitializer.Database, _playerDataManager.PlayerID);
        // ICustomizingRepository customizingRepository = new LocalCustomizingRepository(_testUserId);

        _roomDataManager.Initialize(roomDataRepository);
        _customizingManager.Initialize(customizingRepository);

        if (!_isPlayerDataReadySubscribed)
        {
            _playerDataManager.OnDataManagerReady += InitializedAttendance;
            _isPlayerDataReadySubscribed = true;
        }

        _playerDataManager.Initialize(playerRepository);

        Debug.Log("[DataBootstrapper] Data load started.");
    }

    private void InitializedAttendance()
    {
        CacheManagers();

        if (_firebaseInitializer == null) return;
        if (_playerDataManager == null) return;
        if (_attendanceManager == null) return;

        LoadingUIService.Hide();

        IAttendanceRepository attendanceRepository = new FirebaseAttendanceRepository(_firebaseInitializer.Database, _playerDataManager.PlayerID);
        _attendanceManager.Initialize(attendanceRepository, _rewardRepository, AttendanceCheckPolicyFactory.Create(_attendanceCheckUnit));
    }

    private void CacheManagers()
    {
        _firebaseInitializer = FirebaseInitializer.Instance;
        _photonServerManager = PhotonServerManager.Instance;
        _playerDataManager = PlayerDataManager.Instance;
        _roomDataManager = RoomDataManager.Instance;
        _customizingManager = CustomizingManager.Instance;
    }

    private void OnDestroy()
    {
        FirebaseInitializer.OnFirebaseInitialized -= OnFirebaseSetComplete;

        if (_playerDataManager != null && _isPlayerDataReadySubscribed)
        {
            _playerDataManager.OnDataManagerReady -= InitializedAttendance;
            _isPlayerDataReadySubscribed = false;
        }
    }
}
