using UnityEngine;
using UnityEngine.UI;

public class RobbyBootstrapper : MonoBehaviour
{
    [SerializeField] private RoomView _roomView;
    [SerializeField] private AttendanceView _attendanceView;
    [SerializeField] private SettingView _settingView;

    [SerializeField] private TutorialView _tutorialView;
    [SerializeField] private Button _tutorialOpenButton;
    [SerializeField] private Button _settingOpenButton;

    [SerializeField] private AttendanceManager _attendanceManager;
    [SerializeField] private UIPopupBase _attendancePopup;

    private RoomPresenter _roomPresenter;
    private AttendancePresenter _attendancePresenter;
    private SettingPresenter _settingPresenter;


    private void Start()
    {

        // 1. Model 생성

        // 2. Presenter 생성
        _roomPresenter = new RoomPresenter(_roomView, _attendancePopup);
        _attendancePresenter = new AttendancePresenter(_attendanceManager, _attendanceView, CustomizingManager.Instance, _attendancePopup);
        if (_settingView != null)
        {
            _settingPresenter = new SettingPresenter(_settingView, SoundManager.Instance);
        }

        // 3. View 초기화
        _roomView.Init(_roomPresenter);
        _attendanceView.Init(_attendancePresenter);

        _roomView.AttendancePopupButton.onClick.AddListener(_attendancePresenter.AttendancePopupOpen);
        _tutorialOpenButton.onClick.AddListener(OnTutorialOpenClicked);
        _settingOpenButton.onClick.AddListener(OnSettingOpenClicked);
    }

    private void OnTutorialOpenClicked()
    {
        _tutorialView.Show();
    }

    private void OnSettingOpenClicked()
    {
        _settingView.Show();
    }

    private void OnDestroy()
    {
        _roomPresenter.Dispose();
        _attendancePresenter.Dispose();
        _settingPresenter?.Dispose();
        _roomView.AttendancePopupButton.onClick.RemoveListener(_attendancePresenter.AttendancePopupOpen);
        _tutorialOpenButton.onClick.RemoveListener(OnTutorialOpenClicked);
        _settingOpenButton.onClick.RemoveListener(OnSettingOpenClicked);
    }
}
