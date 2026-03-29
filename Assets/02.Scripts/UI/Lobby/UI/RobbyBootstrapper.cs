using UnityEngine;

public class RobbyBootstrapper : MonoBehaviour
{
    [SerializeField] private RoomView _roomView;
    [SerializeField] private AttendanceView _attendanceView;
    [SerializeField] private AttendanceManager _attendanceManager;

    private RoomPresenter _roomPresenter;
    private AttendancePresenter _attendancePresenter;


    private void Start()
    {

        // 1. Model 생성

        // 2. Presenter 생성
        _roomPresenter = new RoomPresenter(_roomView);
        _attendancePresenter = new AttendancePresenter(_attendanceManager,  PlayerDataManager.Instance, _attendanceView, CustomizingManager.Instance);

        // 3. View 초기화
        _roomView.Init(_roomPresenter);
        _attendanceView.Init(_attendancePresenter);
    }

    private void OnDestroy()
    {
        _roomPresenter.Dispose();
        _attendancePresenter.Dispose();
    }
}
