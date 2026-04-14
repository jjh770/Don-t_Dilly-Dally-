using DontDillyDally.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    public event Action<GameEvent> OnEventPublished;

    [SerializeField] private int _maxEventLogCount = 20;
    [SerializeField] private int _chainThreshold = 3;

    private readonly List<GameEvent> _eventLog = new();
    private bool _chainAccidentTriggered;
    private bool _chainCooperationTriggered;

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

    public void OnPatientCritical()
    {
        Publish(EventType.PatientCritical, "환자의 상태가 위험합니다.");
    }

    public void OnPatientCritical(string detail)
    {
        Publish(EventType.PatientCritical, $"환자의 상태가 위험합니다. {detail}");
    }

    public void OnNoSurgery()
    {
        Publish(EventType.NoSurgery, "수술이 10초 동안 진행되지 않았습니다.");
    }

    public void OnSuccessEmergencyEvent()
    {
        Publish(EventType.SuccessEmergencyEvent, "긴급 이벤트를 성공적으로 처리했습니다.");
    }

    public void OnFailEmergencyEvent()
    {
        Publish(EventType.FailEmergencyEvent, "긴급 이벤트 처리에 실패했습니다.");
    }

    public void OnWrongMaterialUsed()
    {
        Publish(EventType.WrongMaterialUsed, "잘못된 재료를 사용했습니다.");
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

        // Chain 판정 (Chain 이벤트 자체는 판정 제외)
        if (gameEvent.Type != EventType.ChainAccident && gameEvent.Type != EventType.ChainCooperation)
        {
            CheckChainEvents();
        }
    }

    private void Publish(EventType type, string description)
    {
        Publish(new GameEvent(type, description));
    }

    private void CheckChainEvents()
    {
        var recentCategorizedEvents = GetRecentCategorizedEvents(_chainThreshold);

        if (recentCategorizedEvents.Count < _chainThreshold)
        {
            return;
        }

        // 모두 사고인지 확인
        bool allAccident = true;
        bool allCooperation = true;

        foreach (var evt in recentCategorizedEvents)
        {
            if (evt.Category != EventCategory.Accident) allAccident = false;
            if (evt.Category != EventCategory.Cooperation) allCooperation = false;
        }

        // ChainAccident 발동
        if (allAccident && !_chainAccidentTriggered)
        {
            _chainAccidentTriggered = true;
            _chainCooperationTriggered = false;
            Publish(EventType.ChainAccident, "사고가 연속으로 발생하고 있습니다.");
        }
        // ChainCooperation 발동
        else if (allCooperation && !_chainCooperationTriggered)
        {
            _chainCooperationTriggered = true;
            _chainAccidentTriggered = false;
            Publish(EventType.ChainCooperation, "협동이 연속으로 성공하고 있습니다.");
        }
        // 패턴이 깨지면 플래그 리셋
        else if (!allAccident && !allCooperation)
        {
            _chainAccidentTriggered = false;
            _chainCooperationTriggered = false;
        }
    }

    private List<GameEvent> GetRecentCategorizedEvents(int count)
    {
        var result = new List<GameEvent>();

        // 뒤에서부터 Neutral이 아닌 이벤트만 수집
        for (int i = _eventLog.Count - 1; i >= 0 && result.Count < count; i--)
        {
            if (_eventLog[i].Category != EventCategory.Neutral)
            {
                result.Add(_eventLog[i]);
            }
        }

        return result;
    }

    // ========== 조회 메서드 ==========
    public List<GameEvent> GetRecentEvents(int count)
    {
        int startIndex = Mathf.Max(0, _eventLog.Count - count);
        int actualCount = Mathf.Min(count, _eventLog.Count);

        return _eventLog.GetRange(startIndex, actualCount);
    }
}
