using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class CommentaryController : MonoBehaviour
{
    public static CommentaryController Instance { get; private set; }

    public event Action<string> OnNarrationGenerated;

    [Header("참조")]
    [SerializeField] private CommentarySyncManager _syncManager;
    [SerializeField] private CommentaryPlaybackManager _playbackManager;
    [SerializeField] private CommentaryGenerator _generator;

    [Header("설정")]
    [SerializeField] private float _duplicateEventCooldown = 1f;
    [SerializeField] private int _maxQueueSize = 5;

    private readonly Queue<GameEvent> _eventQueue = new();
    private readonly Dictionary<EventType, float> _lastEventTimes = new();
    private readonly Dictionary<int, PreGeneratedIntro> _preGeneratedIntros = new();
    private int _currentPatientIndex = -1;

    private class PreGeneratedIntro
    {
        public string Text;
        public float EstimatedDuration;
    }

    private int _sequenceCounter = 0;
    private bool _isProcessing = false;
    private bool _isGameEnded = false;
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
        if (EventManager.Instance != null)
            EventManager.Instance.OnEventPublished += OnEventPublished;

        if (_syncManager != null)
            _syncManager.OnCommentaryReceived += OnCommentaryReceived;

        if (_playbackManager != null)
            _playbackManager.OnPlaybackCompleted += OnPlaybackCompleted;
    }

    private void OnDisable()
    {
        if (EventManager.Instance != null)
            EventManager.Instance.OnEventPublished -= OnEventPublished;

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

    private void OnEventPublished(GameEvent gameEvent)
    {
            // 호스트에게 이벤트 전달
        _syncManager.SendEventToHost(gameEvent);
    }

    public void HandleEventAsHost(GameEvent gameEvent)
    {
        if (!IsHost) return;
        if (_isGameEnded) return;
        if (IsDuplicateEvent(gameEvent)) return;

        bool isEndingEvent = gameEvent.Type == EventType.TimeOut || gameEvent.Type == EventType.PatientDeath;
        if (isEndingEvent)
        {
            _isGameEnded = true;
            _eventQueue.Clear();
            if (_isProcessing)
            {
                _playbackManager.StopPlayback();
                _isProcessing = false;
            }
            ProcessEventImmediately(gameEvent);
            return;
        }

            // 우선순위 기반 처리
        if (_isProcessing && _currentCommentary != null)
        {
            if (gameEvent.Priority > _currentCommentary.Priority)
            {
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

        GeneratedCommentaryData generatedData;

            // 사전 생성된 환자 소개가 있으면 사용
        if (gameEvent.Type == EventType.NewPatientAppeared && TryGetCurrentPatientIntro(out var introText, out var introDuration))
        {
            generatedData = new GeneratedCommentaryData
            {
                Text = introText,
                EstimatedDuration = introDuration
            };
        }
        else
        {
            // 코멘터리 생성 (호스트만)
            generatedData = await _generator.GenerateCommentary(gameEvent);
        }

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
            generatedData.EstimatedDuration
        );

        _currentCommentary = syncData;

            // 모든 클라이언트에게 브로드캐스트
        _syncManager.BroadcastCommentary(syncData);
    }

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

    // ========== 환자 소개 사전 생성 ==========

    public async UniTask PreGeneratePatientIntros(List<(string patientName, string diseaseName)> patients, CancellationToken ct)
    {
        if (patients == null || patients.Count == 0) return;

        Debug.Log($"[CommentaryController] 환자 소개 사전 생성 시작: {patients.Count}명");

        var tasks = new List<UniTask>();
        for (int i = 0; i < patients.Count; i++)
        {
            int index = i;
            tasks.Add(PreGeneratePatientIntroAsync(index, patients[index].patientName, patients[index].diseaseName, ct));
        }

        await UniTask.WhenAll(tasks);

        Debug.Log($"[CommentaryController] 환자 소개 사전 생성 완료: {_preGeneratedIntros.Count}개");
    }

    private async UniTask PreGeneratePatientIntroAsync(int patientIndex, string patientName, string diseaseName, CancellationToken ct)
    {
        try
        {
                // 1. 텍스트 생성
            var generatedData = await _generator.GeneratePatientIntro(patientName, diseaseName);

            if (ct.IsCancellationRequested) return;

            if (generatedData == null || string.IsNullOrEmpty(generatedData.Text))
            {
                Debug.LogWarning($"[CommentaryController] 환자 {patientIndex + 1} 텍스트 생성 실패");
                return;
            }

                  // 2. TTS 음성 사전 생성 및 캐싱
            await _playbackManager.PreGenerateAndCache(generatedData.Text);

            if (ct.IsCancellationRequested) return;

                  // 3. 저장
            _preGeneratedIntros[patientIndex] = new PreGeneratedIntro
            {
                Text = generatedData.Text,
                EstimatedDuration = generatedData.EstimatedDuration
            };

            Debug.Log($"[CommentaryController] 환자 {patientIndex + 1} 사전 생성 완료: {generatedData.Text}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CommentaryController] 환자 {patientIndex + 1} 사전 생성 실패: {e.Message}");
        }
    }

    public bool TryGetPreGeneratedIntro(int patientIndex, out string text, out float duration)
    {
        if (_preGeneratedIntros.TryGetValue(patientIndex, out var intro))
        {
            text = intro.Text;
            duration = intro.EstimatedDuration;
            return true;
        }

        text = null;
        duration = 0f;
        return false;
    }

    private bool TryGetCurrentPatientIntro(out string text, out float duration)
    {
        return TryGetPreGeneratedIntro(_currentPatientIndex, out text, out duration);
    }

    public void SetCurrentPatientIndex(int patientIndex)
    {
        _currentPatientIndex = patientIndex;
    }

    public void ClearPreGeneratedIntros()
    {
        _preGeneratedIntros.Clear();
        _currentPatientIndex = -1;
    }

    public void ResetGameState()
    {
        _isGameEnded = false;
        _eventQueue.Clear();
        _lastEventTimes.Clear();
        _isProcessing = false;
        _currentCommentary = null;
    }
}
