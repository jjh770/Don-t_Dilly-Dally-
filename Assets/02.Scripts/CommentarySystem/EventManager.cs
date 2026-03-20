using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    public event Action<GameEvent> OnEventPublished;

    [SerializeField] private int _maxEventLogCount = 20;
    [SerializeField] private bool _warnOnNonHostPublish = true;

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

        if (_warnOnNonHostPublish && PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning($"[EventManager] Non-host에서 이벤트 발행됨: {gameEvent.Type}. 코멘터리가 동기화되지 않을 수 있습니다.");
        }

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
        Publish(new GameEvent(type, description));
    }

    public void PublishAsHost(GameEvent gameEvent)
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"[EventManager] Non-host이므로 이벤트 무시: {gameEvent?.Type}");
            return;
        }

        Publish(gameEvent);
    }

    public void PublishAsHost(EventType type, string description)
    {
        PublishAsHost(new GameEvent(type, description));
    }

    public List<GameEvent> GetRecentEvents(int count)
    {
        int startIndex = Mathf.Max(0, _eventLog.Count - count);
        int actualCount = Mathf.Min(count, _eventLog.Count);
        return _eventLog.GetRange(startIndex, actualCount);
    }

    public void ClearEventLog()
    {
        _eventLog.Clear();
    }
}
