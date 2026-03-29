using NUnit.Framework;
using System;
using System.Threading;
using UnityEngine;

public class AttendancePresenter
{
    private AttendanceManager _attendanceManager;
    private PlayerDataManager _playerDataManager;
    private ICustomizingManager _customizingManager;
    private AttendanceView _view;
    private IRewardRepository _rewardRepository;

    private CancellationTokenSource _cts;

    public AttendancePresenter(AttendanceManager attendanceManager,PlayerDataManager dataManager, AttendanceView view, ICustomizingManager customizingManager)
    {
        _attendanceManager = attendanceManager;
        _playerDataManager = dataManager;
        _customizingManager = customizingManager;

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

        int totalRewardCount = _rewardRepository.GetRewardCount();


        Sprite[] itemSprites = new Sprite[totalRewardCount]; 

        for (int i = 0; i < totalRewardCount; i++)
        {
            itemSprites[i] = _customizingManager.GetItemById(_rewardRepository.GetReward(i+1).ItemId).PreviewIcon;
        }

        _view.SetDayList(record.TotalDays, _rewardRepository.GetRewardCount(), itemSprites);
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
