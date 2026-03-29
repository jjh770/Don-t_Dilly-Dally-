using System;
using System.Threading;
using UnityEngine;

public class AttendancePresenter
{
    private AttendanceManager _attendanceManager;
    private PlayerDataManager _playerDataManager;
    private AttendanceView _view;
    private IRewardRepository _rewardRepository;

    private CancellationTokenSource _cts;

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
        _cts = new CancellationTokenSource();
        _attendanceManager.CheckAttendance(_cts.Token);
    }

    public void OnPopupClose()
    {
        ResetCTS();
    }

    private void OnAttendanceManagerReady()
    {
        _cts = new CancellationTokenSource();

        _attendanceManager.LoadAttendance(_cts.Token);
        _rewardRepository = _attendanceManager.RewardRepo;
    }

    private void OnDataLoaded(AttendanceRecord record)
    {
        ResetCTS();

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

    private void ResetCTS()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }


    public void Dispose()
    {
        _attendanceManager.OnAttendanceRecordLoaded -= OnDataLoaded;
        _attendanceManager.OnAttendanceChecked -= OnAttendanceChecked;
        AttendanceManager.OnAttendanceManagerReady -= OnAttendanceManagerReady;
        ResetCTS();
    }
}
