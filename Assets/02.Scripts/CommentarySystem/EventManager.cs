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

        Debug.Log($"[EventManager] Event Published: {gameEvent.Type} - {gameEvent.Description}");
        OnEventPublished?.Invoke(gameEvent);
    }

    public void Publish(EventType type, string description)
    {
        Publish(new GameEvent(type, description));
    }

    public List<GameEvent> GetRecentEvents(int count)
    {
        int startIndex = Mathf.Max(0, _eventLog.Count - count);
        int actualCount = Mathf.Min(count, _eventLog.Count);

        return _eventLog.GetRange(startIndex, actualCount);
    }

    public List<GameEvent> GetRecentEvents(float secondsAgo)
    {
        DateTime threshold = DateTime.Now.AddSeconds(-secondsAgo);
        return _eventLog.FindAll(e => e.Timestamp >= threshold);
    }

    public void ClearEventLog()
    {
        _eventLog.Clear();
    }
}
