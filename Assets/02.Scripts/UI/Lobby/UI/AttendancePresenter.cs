using System;
using UnityEngine;

public class AttendancePresenter
{
    private AttendanceManager _attendanceManager;
    private AttendanceView _view;
    private IRewardRepository _rewardRepository;

    public AttendancePresenter(AttendanceManager attendanceManager, AttendanceView view)
    {
        _attendanceManager = attendanceManager;
        
        _view = view;

        _attendanceManager.OnAttendanceRecordLoaded += OnDataLoaded;
        AttendanceManager.OnAttendanceManagerReady += OnAttendanceManagerReady;
        if (_attendanceManager.IsReady) OnAttendanceManagerReady();
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

    public void Dispose()
    {
        _attendanceManager.OnAttendanceRecordLoaded -= OnDataLoaded;
        AttendanceManager.OnAttendanceManagerReady -= OnAttendanceManagerReady;
    }
}
