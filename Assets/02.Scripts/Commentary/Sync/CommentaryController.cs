using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 코멘터리 전체 흐름 제어
/// - 이벤트 수신
/// - 큐 관리
/// - 현재 재생 상태 관리
/// - 호스트 여부에 따른 분기
/// - 브로드캐스트 명령 송신
/// - 수신된 코멘터리 재생 시작
/// </summary>
public class CommentaryController : MonoBehaviour
{
    public static CommentaryController Instance { get; private set; }

    public event Action<string> OnNarrationGenerated;

    [Header("참조")]
    [SerializeField] private CommentarySyncManager _syncManager;
    [SerializeField] private CommentaryPlaybackManager _playbackManager;
    [SerializeField] private CommentaryGenerator _generator;
    [SerializeField] private EventManager _eventManager;

    [Header("설정")]
    [SerializeField] private float _duplicateEventCooldown = 1f;
    [SerializeField] private int _maxQueueSize = 5;

    private readonly Queue<GameEvent> _eventQueue = new();
    private readonly Dictionary<EventType, float> _lastEventTimes = new();

    private int _sequenceCounter = 0;
    private bool _isProcessing = false;
    private CommentarySyncData _currentCommentary = null;

    private bool IsHost => _syncManager != null && _syncManager.IsHost;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        if (_eventManager != null)
            _eventManager.OnEventPublished += OnEventPublished;

        if (_syncManager != null)
            _syncManager.OnCommentaryReceived += OnCommentaryReceived;

        if (_playbackManager != null)
            _playbackManager.OnPlaybackCompleted += OnPlaybackCompleted;
    }

    private void OnDisable()
    {
        if (_eventManager != null)
            _eventManager.OnEventPublished -= OnEventPublished;

        if (_syncManager != null)
            _syncManager.OnCommentaryReceived -= OnCommentaryReceived;

        if (_playbackManager != null)
            _playbackManager.OnPlaybackCompleted -= OnPlaybackCompleted;
    }

    private void Update()
    {
        // 호스트만 큐 처리
        if (IsHost && !_isProcessing && _eventQueue.Count > 0)
        {
            ProcessNextEvent();
        }
    }

    /// <summary>
    /// 로컬에서 이벤트가 발생했을 때 (EventManager에서 호출)
    /// </summary>
    private void OnEventPublished(GameEvent gameEvent)
    {
        // 호스트에게 이벤트 전달
        _syncManager.SendEventToHost(gameEvent);
    }

    /// <summary>
    /// 호스트로서 이벤트를 처리 (SyncManager에서 호출)
    /// </summary>
    public void HandleEventAsHost(GameEvent gameEvent)
    {
        if (!IsHost) return;

        // 중복 이벤트 필터링
        if (IsDuplicateEvent(gameEvent))
        {
            Debug.Log($"[CommentaryController] 중복 이벤트 무시: {gameEvent.Type}");
            return;
        }

        // 우선순위 기반 처리
        if (_isProcessing && _currentCommentary != null)
        {
            if (gameEvent.Priority > _currentCommentary.Priority)
            {
                // 현재 재생 중단하고 새 이벤트 처리
                _playbackManager.StopPlayback();
                _isProcessing = false;
                ProcessEventImmediately(gameEvent);
                return;
            }
        }

        // 큐에 추가
        EnqueueEvent(gameEvent);
    }

    private bool IsDuplicateEvent(GameEvent gameEvent)
    {
        if (_lastEventTimes.TryGetValue(gameEvent.Type, out float lastTime))
        {
            if (Time.time - lastTime < _duplicateEventCooldown)
            {
                return true;
            }
        }

        _lastEventTimes[gameEvent.Type] = Time.time;
        return false;
    }

    private void EnqueueEvent(GameEvent gameEvent)
    {
        // 큐가 가득 찼으면 낮은 우선순위 이벤트 제거
        while (_eventQueue.Count >= _maxQueueSize)
        {
            _eventQueue.Dequeue();
        }

        _eventQueue.Enqueue(gameEvent);
    }

    private void ProcessNextEvent()
    {
        if (_eventQueue.Count == 0) return;

        var gameEvent = _eventQueue.Dequeue();
        ProcessEventImmediately(gameEvent);
    }

    private async void ProcessEventImmediately(GameEvent gameEvent)
    {
        _isProcessing = true;

        // 코멘터리 생성 (호스트만)
        var generatedData = await _generator.GenerateCommentary(gameEvent);

        if (generatedData == null)
        {
            Debug.LogWarning($"[CommentaryController] 코멘터리 생성 실패: {gameEvent.Type}");
            _isProcessing = false;
            return;
        }

        // SyncData 생성
        var syncData = CommentarySyncData.CreateFromEvent(
            gameEvent,
            ++_sequenceCounter,
            generatedData.Text,
            _syncManager.NetworkTime,
            generatedData.EstimatedDuration,
            generatedData.TtsAudioKey
        );

        _currentCommentary = syncData;

        // 모든 클라이언트에게 브로드캐스트
        _syncManager.BroadcastCommentary(syncData);
    }

    /// <summary>
    /// 코멘터리 수신 시 (모든 클라이언트)
    /// </summary>
    private void OnCommentaryReceived(CommentarySyncData syncData)
    {
        _currentCommentary = syncData;

        // 예약된 시간에 재생
        float delay = (float)(syncData.ScheduledNetworkTime - _syncManager.NetworkTime);
        delay = Mathf.Max(0, delay);

        StartCoroutine(PlayAfterDelay(syncData, delay));
    }

    private System.Collections.IEnumerator PlayAfterDelay(CommentarySyncData syncData, float delay)
    {
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        _playbackManager.PlayCommentary(syncData);
        OnNarrationGenerated?.Invoke(syncData.FinalText);
    }

    private void OnPlaybackCompleted()
    {
        _isProcessing = false;
        _currentCommentary = null;
    }

    /// <summary>
    /// 상태 초기화 (씬 전환 등)
    /// </summary>
    public void Reset()
    {
        _eventQueue.Clear();
        _lastEventTimes.Clear();
        _sequenceCounter = 0;
        _isProcessing = false;
        _currentCommentary = null;
    }
}
