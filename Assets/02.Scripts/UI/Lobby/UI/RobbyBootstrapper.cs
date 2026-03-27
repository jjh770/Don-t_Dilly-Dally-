using UnityEngine;

public class RobbyBootstrapper : MonoBehaviour
{
    [SerializeField] private RoomView _roomView;
    [SerializeField] private AttendanceView _attendanceView;
    [SerializeField] private AttendanceManager _attendanceManager;


    private void Start()
    {
        
        // 1. Model 생성

        // 2. Presenter 생성
        RoomPresenter roomPresenter = new RoomPresenter(_roomView);
        AttendancePresenter attendancePresenter = new AttendancePresenter(_attendanceManager, _attendanceView);

        // 3. View 초기화
        _roomView.Init(roomPresenter);
        _attendanceView.Init(attendancePresenter);
    }
}
