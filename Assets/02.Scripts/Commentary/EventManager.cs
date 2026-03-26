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

    public void Publish(GameEvent gameEvent)
    {
        if (gameEvent == null) return;

        _eventLog.Add(gameEvent);

        if (_eventLog.Count > _maxEventLogCount)
        {
            _eventLog.RemoveAt(0);
        }

        Debug.Log($"[EventManager] 이벤트 발행: {gameEvent.Type} - {gameEvent.Description}");
        OnEventPublished?.Invoke(gameEvent);
    }

    public void Publish(EventType type, string description)
    {
        // 이벤트 타입과 설명만 넘겨도
        // 내부에서 GameEvent를 새로 만들어서 Publish
        Publish(new GameEvent(type, description));
    }

    // 최근 count개 이벤트 가져오기
    public List<GameEvent> GetRecentEvents(int count)
    {
        int startIndex = Mathf.Max(0, _eventLog.Count - count);
        int actualCount = Mathf.Min(count, _eventLog.Count);

        return _eventLog.GetRange(startIndex, actualCount);
    }

    // 저장된 이벤트 로그 전체 삭제하기
    public void ClearEventLog()
    {
        _eventLog.Clear();
    }
}
