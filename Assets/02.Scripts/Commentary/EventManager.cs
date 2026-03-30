using DontDillyDally.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    public event Action<GameEvent> OnEventPublished;

    [SerializeField] private int _maxEventLogCount = 20;

    private readonly List<GameEvent> _eventLog = new();

    public IReadOnlyList<GameEvent> EventLog => _eventLog;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // ========== 이벤트 발행 메서드 ==========

    public void OnNewPatientAppeared(string patientName, string diseaseName)
    {
        Publish(EventType.NewPatientAppeared, $"새로운 환자 '{patientName}'이(가) 등장했습니다. 병명: {diseaseName}");
    }

    public void OnSurgerySuccess()
    {
        Publish(EventType.SurgerySuccess, "수술이 성공적으로 완료되었습니다.");
    }

    public void OnSurgeryFail(SurgeryFailureReason reason)
    {
        string description = reason switch
        {
            SurgeryFailureReason.RecipeMismatch => "잘못된 재료를 사용했습니다.",
            SurgeryFailureReason.MiniGameFailure => "수술 미니게임에 실패했습니다.",
            _ => "수술에 실패했습니다."
        };
        Publish(EventType.SurgeryFail, description);
    }

    public void OnPatientDeath()
    {
        Publish(EventType.PatientDeath, "환자가 사망했습니다.");
    }

    public void OnTimeOut()
    {
        Publish(EventType.TimeOut, "시간이 다 되어서 게임이 끝났습니다.");
    }

    public void OnEquipmentAccident(string equipmentName)
    {
        Publish(EventType.EquipmentAccident, $"{equipmentName} 장비에 사고가 발생했습니다.");
    }

    public void OnPatientCritical()
    {
        Publish(EventType.PatientCritical, "환자의 상태가 위험합니다.");
    }

    public void OnPatientCritical(string detail)
    {
        Publish(EventType.PatientCritical, $"환자의 상태가 위험합니다. {detail}");
    }

    public void OnEmergencyEvent()
    {
        // 임시.
        // 나중에 긴급 상황 타입이 생기면 삭제할 것.
        Publish(EventType.EmergencyEvent, "긴급 상황 발생");
    }

    public void OnEmergencyEvent(string emergencyDetail)
    {
        Publish(EventType.EmergencyEvent, $"긴급 상황 발생: {emergencyDetail}");
    }

    public void OnMachineBroken(string machineName)
    {
        Publish(EventType.MachineBroken, $"{machineName} 기계가 고장났습니다.");
    }

    public void OnMaterialDeliveredLate(string materialName)
    {
        Publish(EventType.MaterialDeliveredLate, $"{materialName} 재료가 늦게 전달되었습니다.");
    }

    public void OnEmergencyPrevented(string emergencyDetail)
    {
        Publish(EventType.EmergencyPrevented, $"긴급 이벤트를 막아냈습니다. {emergencyDetail}");
    }

    public void OnRepairTimeout(string machineName)
    {
        Publish(EventType.RepairTimeout, $"제한 시간 내에 {machineName} 장비를 고치지 못했습니다.");
    }

    public void OnRepairCompletedFast(string machineName)
    {
        Publish(EventType.RepairCompletedFast, $"{machineName} 장비 수리를 빠르게 완료했습니다.");
    }

    public void OnRepairCompletedLate(string machineName)
    {
        Publish(EventType.RepairCompletedLate, $"{machineName} 장비 수리가 늦어졌습니다.");
    }

    public void OnChainAccident(string accidentContext)
    {
        Publish(EventType.ChainAccident, accidentContext);
    }

    public void OnChainCooperation(string cooperationContext)
    {
        Publish(EventType.ChainCooperation, cooperationContext);
    }

    // ========== 내부 메서드 ==========
    private void Publish(GameEvent gameEvent)
    {
        if (gameEvent == null) return;

        _eventLog.Add(gameEvent);

        if (_eventLog.Count > _maxEventLogCount)
        {
            _eventLog.RemoveAt(0);
        }

        OnEventPublished?.Invoke(gameEvent);
    }

    private void Publish(EventType type, string description)
    {
        Publish(new GameEvent(type, description));
    }

    // ========== 조회 메서드 ==========
    public List<GameEvent> GetRecentEvents(int count)
    {
        int startIndex = Mathf.Max(0, _eventLog.Count - count);
        int actualCount = Mathf.Min(count, _eventLog.Count);

        return _eventLog.GetRange(startIndex, actualCount);
    }
}
