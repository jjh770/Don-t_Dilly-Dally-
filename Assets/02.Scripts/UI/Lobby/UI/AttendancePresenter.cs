using System;
using UnityEngine;

public class AttendancePresenter
{
    private AttendanceManager _attendanceManager;
    private PlayerDataManager _playerDataManager;
    private AttendanceView _view;
    private IRewardRepository _rewardRepository;

    public AttendancePresenter(AttendanceManager attendanceManager,PlayerDataManager dataManager, AttendanceView view)
    {
        _attendanceManager = attendanceManager;
        _playerDataManager = dataManager;

        _view = view;

        _attendanceManager.OnAttendanceRecordLoaded += OnDataLoaded;
        _attendanceManager.OnAttendanceChecked += OnAttendanceChecked;
        AttendanceManager.OnAttendanceManagerReady += OnAttendanceManagerReady;
 
        if (_attendanceManager.IsReady)
        {
            OnAttendanceManagerReady();
        }

        SetName(_playerDataManager.PlayerID);
    }

    public void OnPopupShow()
    {
        _attendanceManager.CheckAttendance();
    }

    public void OnPopupClose()
    {
        _attendanceManager.CancelAll();
    }

    private void OnAttendanceManagerReady()
    {
        _attendanceManager.LoadAttendance();
        _rewardRepository = _attendanceManager.RewardRepo;
    }

    private void OnDataLoaded(AttendanceRecord record)
    {
        _view.SetDayList(record.TotalDays, _rewardRepository);
    }

    private void OnAttendanceChecked(int totalDays)
    {
        _view.SetComplete(totalDays);
    }

    private void SetName(string name)
    {
        _view.SetName(name);
    }


    public void Dispose()
    {
        _attendanceManager.OnAttendanceRecordLoaded -= OnDataLoaded;
        _attendanceManager.OnAttendanceChecked -= OnAttendanceChecked;
        AttendanceManager.OnAttendanceManagerReady -= OnAttendanceManagerReady;
        _attendanceManager.CancelAll();
    }
}
