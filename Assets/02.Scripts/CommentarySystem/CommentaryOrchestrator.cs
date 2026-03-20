using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class CommentaryOrchestrator : MonoBehaviour
{
    public static CommentaryOrchestrator Instance { get; private set; }

    public event Action<string> OnNarrationGenerated;           // 코멘터리 재생 시작 시 호출 (UI 동기화용)
    public event Action<CommentaryData> OnCommentaryScheduled;  // // 코멘터리 재생 예약 시 호출 (UI 준비용)

    [Header("참조 설정")]
    [SerializeField] private EventManager _eventManager;
    [SerializeField] private NarrationGenerator _narrationGenerator;
    [SerializeField] private TTSService _ttsService;
    [SerializeField] private CommentaryAudioManager _audioManager;
    [SerializeField] private CommentaryNetworkBridge _networkBridge;

    [Header("세팅")]
    [SerializeField] private float _commentaryCooldown = 2f;
    [SerializeField] private int _recentEventCount = 5;

    private float _lastCommentaryTime;
    private GameEvent _pendingEvent;
    private bool _isProcessing;

    private static readonly Dictionary<EventType, string> PreGeneratedTexts = new()
    {
        { EventType.GameStart, "좋아, 수술 시작이다. 집중해!" },
        { EventType.GameOver, "여기까지다. 수고했어." },
        { EventType.SurgerySuccess, "해냈군. 완벽한 수술이었어." },
        { EventType.SurgeryFail, "끝났어... 이번엔 실패다." },
        { EventType.PatientDeath, "환자를 잃었다... 다음엔 놓치지 마." }
    };

    // ========== Host 전용 ==========
    private bool IsHost => _networkBridge == null || _networkBridge.IsHost;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private async void Start()
    {
        await PreGenerateVoiceClips();
    }

    private void OnEnable()
    {
        // Host만 이벤트 판정
        if (_eventManager != null)
        {
            _eventManager.OnEventPublished += HandleEvent;
            Debug.Log("[CommentaryOrchestrator] EventManager 구독 완료");
        }
        else
        {
            Debug.LogError("[CommentaryOrchestrator] EventManager가 null입니다!");
        }

        // 모든 클라이언트가 네트워크 수신 처리
        if (_networkBridge != null)
        {
            _networkBridge.OnCommentaryReceived += HandleNetworkCommentary;
            Debug.Log("[CommentaryOrchestrator] NetworkBridge 구독 완료");
        }
        else
        {
            Debug.LogWarning("[CommentaryOrchestrator] NetworkBridge가 null - 싱글플레이 모드");
        }
    }

    private void OnDisable()
    {
        _eventManager.OnEventPublished -= HandleEvent;

        if (_networkBridge != null)
        {
            _networkBridge.OnCommentaryReceived -= HandleNetworkCommentary;
        }
    }

    // Host만 이벤트를 처리하여 코멘터리 생성 여부 결정
    private void HandleEvent(GameEvent gameEvent)
    {
        Debug.Log($"[CommentaryOrchestrator] HandleEvent: {gameEvent?.Type}, IsHost={IsHost}");

        if (!IsHost) return;

        if (_isProcessing || Time.time - _lastCommentaryTime < _commentaryCooldown)
        {
            if (_pendingEvent == null || gameEvent.Priority > _pendingEvent.Priority)
            {
                _pendingEvent = gameEvent;
            }
            return;
        }

        ProcessEventAsHost(gameEvent);
    }

    private void Update()
    {
        // Host만 대기 이벤트 처리
        if (!IsHost) return;

        if (_pendingEvent != null && !_isProcessing && Time.time - _lastCommentaryTime >= _commentaryCooldown)
        {
            ProcessEventAsHost(_pendingEvent);
            _pendingEvent = null;
        }
    }

    // [Host] 이벤트를 처리하여 코멘터리 생성 및 네트워크 전파
    private async void ProcessEventAsHost(GameEvent gameEvent)
    {
        if (_isProcessing) return;

        _isProcessing = true;
        _lastCommentaryTime = Time.time;

        if (gameEvent.UsePreGeneratedVoice)
        {
            if (PreGeneratedTexts.TryGetValue(gameEvent.Type, out string text))
            {
                BroadcastCommentary(gameEvent.Type, text, true);
            }
        }
        else
        {
            await GenerateAndBroadcastCommentary(gameEvent);
        }

        _isProcessing = false;
    }

    // [Host] AI로 텍스트 생성 후 네트워크 전파
    private async Awaitable GenerateAndBroadcastCommentary(GameEvent gameEvent)
    {
        List<GameEvent> recentEvents = _eventManager.GetRecentEvents(_recentEventCount);

        string narrationText = await _narrationGenerator.GenerateNarration(gameEvent, recentEvents);

        if (string.IsNullOrEmpty(narrationText))
        {
            Debug.LogWarning("[CommentaryOrchestrator] 중계 문장 생성 실패");
            return;
        }

        // 네트워크 전파 (TTS는 각 클라이언트에서 로컬 처리)
        BroadcastCommentary(gameEvent.Type, narrationText, false);
    }

    // [Host] 코멘터리를 네트워크로 전파
    private void BroadcastCommentary(EventType eventType, string text, bool usePreGenerated)
    {
        if (_networkBridge != null)
        {
            _networkBridge.BroadcastCommentary(eventType, text, usePreGenerated);
        }
        else
        {
            PlayCommentaryLocal(new CommentaryData(0, eventType, text, usePreGenerated, 0));
        }
    }

    // ========== 모든 클라이언트 공통 ==========
    private void HandleNetworkCommentary(CommentaryData data)
    {
        Debug.Log($"[CommentaryOrchestrator] HandleNetworkCommentary 수신: {data.GetEventType()}");
        PlayCommentaryLocal(data);
    }

    private async void PlayCommentaryLocal(CommentaryData data)
    {
        Debug.Log($"[CommentaryOrchestrator] PlayCommentaryLocal: {data.GetEventType()}, UsePreGenerated={data.UsePreGenerated}");

        OnCommentaryScheduled?.Invoke(data);

        await WaitUntilNetworkPlayTime(data.NetworkPlayTime);

        if (data.UsePreGenerated)
        {
            PlayPreGeneratedVoice(data.GetEventType(), data.NarrationText);
        }
        else
        {
            await PlayGeneratedVoice(data.NarrationText);
        }
    }

    private async Awaitable WaitUntilNetworkPlayTime(double networkPlayTime)
    {
        if (!PhotonNetwork.IsConnected || networkPlayTime <= 0)
            return;

        double currentTime = PhotonNetwork.Time;
        double waitTime = networkPlayTime - currentTime;

        if (waitTime > 0)
        {
            Debug.Log($"[CommentaryOrchestrator] 동기화 대기: {waitTime:F3}초");
            await Awaitable.WaitForSecondsAsync((float)waitTime);
        }
        else if (waitTime < -0.5)
        {
            Debug.LogWarning($"[CommentaryOrchestrator] 재생 시점 놓침: {-waitTime:F3}초 지연");
        }
    }

    private void PlayPreGeneratedVoice(EventType eventType, string text)
    {
        Debug.Log($"[CommentaryOrchestrator] PlayPreGeneratedVoice: {eventType}, {text}");

        string clipName = eventType.ToString();
        AudioClip clip = _audioManager.GetCachedClip(clipName);

        if (clip != null)
        {
            _audioManager.PlayVoice(clip);
            Debug.Log($"[CommentaryOrchestrator] OnNarrationGenerated 이벤트 발생: 구독자 {OnNarrationGenerated?.GetInvocationList()?.Length ?? 0}명");
            OnNarrationGenerated?.Invoke(text);
        }
        else
        {
            Debug.LogWarning($"[CommentaryOrchestrator] 캐싱된 클립 없음: {clipName}");
        }
    }

    private async Awaitable PlayGeneratedVoice(string narrationText)
    {
        Debug.Log($"[CommentaryOrchestrator] PlayGeneratedVoice: {narrationText}");

        AudioClip clip = await _ttsService.GenerateSpeech(narrationText);

        if (clip != null)
        {
            _audioManager.PlayVoice(clip);
            Debug.Log($"[CommentaryOrchestrator] OnNarrationGenerated 이벤트 발생: 구독자 {OnNarrationGenerated?.GetInvocationList()?.Length ?? 0}명");
            OnNarrationGenerated?.Invoke(narrationText);
        }
    }

    private async Awaitable PreGenerateVoiceClips()
    {
        Debug.Log("[CommentaryOrchestrator] 사전 음성 생성 시작...");

        foreach (var kvp in PreGeneratedTexts)
        {
            string clipName = kvp.Key.ToString();

            if (_audioManager.HasCachedClip(clipName))
                continue;

            AudioClip clip = await _ttsService.GenerateSpeech(kvp.Value);

            if (clip != null)
            {
                _audioManager.CacheClip(clipName, clip);
            }
            else
            {
                Debug.LogWarning($"[CommentaryOrchestrator] 사전 음성 생성 실패: {clipName}");
            }
        }

        Debug.Log("[CommentaryOrchestrator] 사전 음성 생성 완료");
    }
}
