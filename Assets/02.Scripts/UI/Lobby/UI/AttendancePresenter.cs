using System.Threading;
using UnityEngine;

public class AttendancePresenter
{
    private AttendanceManager _attendanceManager;
    private ICustomizingManager _customizingManager;
    private AttendanceView _view;
    private IRewardRepository _rewardRepository;
    private UIPopupBase _attendancePopup;


    private CancellationTokenSource _cts;

    public AttendancePresenter(AttendanceManager attendanceManager, AttendanceView view, ICustomizingManager customizingManager, UIPopupBase attendancePopup)
    {
        _attendanceManager = attendanceManager;
        _customizingManager = customizingManager;

        _view = view;

        _attendancePopup = attendancePopup;

        _attendanceManager.OnAttendanceRecordLoaded += HandleDataLoaded;
        _attendanceManager.OnAttendanceChecked += HandleAttendanceChecked;
        AttendanceManager.OnAttendanceManagerReady += HandleAttendanceManagerReady;
        PlayerDataManager.Instance.OnNicknameChanged += SetName;


        if (_attendanceManager.IsReady)
        {
            HandleAttendanceManagerReady();
        }
    }

    public void OnPopupShow()
    {
        ResetCTS();
        _attendanceManager.CheckAttendance(_cts.Token);
    }

    public void OnPopupClose()
    {
        ResetCTS();
    }

    private void ResetCTS()
    {
        var oldCts = _cts;

        _cts = new CancellationTokenSource();

        oldCts?.Cancel();
    }

    private void HandleAttendanceManagerReady()
    {
        ResetCTS(); 

        SetName(PlayerDataManager.Instance.PlayerNickname);

        _attendanceManager.LoadAttendance(_cts.Token);
        _rewardRepository = _attendanceManager.RewardRepo;
    }

    private void HandleDataLoaded(AttendanceRecord record)
    {

        int totalRewardCount = _rewardRepository.GetRewardCount();


        Sprite[] itemSprites = new Sprite[totalRewardCount]; 

        for (int i = 0; i < totalRewardCount; i++)
        {
            itemSprites[i] = _customizingManager.GetItemById(_rewardRepository.GetReward(i+1).ItemId).PreviewIcon;
        }

        _view.SetDayList(record.TotalDays, _rewardRepository.GetRewardCount(), itemSprites);

        if (record.CanCheckToday())
        {
            AttendancePopupOpen();
        }
    }

    private void HandleAttendanceChecked(int totalDays)
    {
        _view.SetComplete(totalDays);
    }

    private void SetName(string name)
    {
        _view.SetName(name);
    }

    public void AttendancePopupOpen()
    {
        _attendancePopup.Show();
    }


    public void Dispose()
    {
        _attendanceManager.OnAttendanceRecordLoaded -= HandleDataLoaded;
        _attendanceManager.OnAttendanceChecked -= HandleAttendanceChecked;
        AttendanceManager.OnAttendanceManagerReady -= HandleAttendanceManagerReady;
        PlayerDataManager.Instance.OnNicknameChanged -= SetName;

        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }
}
