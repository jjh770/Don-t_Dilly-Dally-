using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EventManager))]
[RequireComponent(typeof(NarrationGenerator))]
[RequireComponent(typeof(TTSManager))]
[RequireComponent(typeof(CommentaryAudioManager))]
public class CommentaryManager : MonoBehaviour
{
    public static CommentaryManager Instance { get; private set; }

    [Header("참조 설정")]
    [SerializeField] private EventManager _eventManager;                // 게임 이벤트를 받아오는 매니저
    [SerializeField] private NarrationGenerator _narrationGenerator;    // 텍스트 생성기
    [SerializeField] private TTSManager _ttsManager;                    // 텍스트 -> 음성
    [SerializeField] private CommentaryAudioManager _audioManager;      // 음성 재생
    [SerializeField] private UI_Commentary _narrationUI;

    [Header("세팅")]
    [SerializeField] private float _commentaryCooldown = 2f;
    [SerializeField] private int _recentEventCount = 5;

    private float _lastCommentaryTime;
    private GameEvent _pendingEvent;
    private bool _isProcessing;
    private bool _isPreGenerating;

    private static readonly Dictionary<EventType, string> PreGeneratedTexts = new()
    {
        { EventType.GameStart, "좋아, 수술 시작이다. 집중해!" },
        { EventType.GameOver, "여기까지다. 수고했어." },
        { EventType.SurgerySuccess, "해냈군. 완벽한 수술이었어." },
        { EventType.SurgeryFail, "끝났어... 이번엔 실패다." },
        { EventType.PatientDeath, "환자를 잃었다... 다음엔 놓치지 마." }
    };

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
        _eventManager.OnEventPublished += HandleEvent;
    }

    private void OnDisable()
    {
        _eventManager.OnEventPublished -= HandleEvent;
    }

    private void HandleEvent(GameEvent gameEvent)
    {
        if (Time.time - _lastCommentaryTime < _commentaryCooldown)
        {
            // 대기 이벤트가 없거나 우선순위가 더 높은 이벤트면
            // 대기 중인 이벤트 교체
            if (_pendingEvent == null || gameEvent.Priority > _pendingEvent.Priority)
            {
                _pendingEvent = gameEvent;
            }
            return;
        }

        ProcessEvent(gameEvent);
    }

    private void Update()
    {
        // 대기 중인 이벤트 처리
        if (_pendingEvent != null && !_isProcessing && Time.time - _lastCommentaryTime >= _commentaryCooldown)
        {
            ProcessEvent(_pendingEvent);
            _pendingEvent = null;
        }
    }

    private async void ProcessEvent(GameEvent gameEvent)
    {
        if (_isProcessing) return;

        _isProcessing = true;
        _lastCommentaryTime = Time.time;

        if (gameEvent.UsePreGeneratedVoice)
        {
            // 사전 생성 음성 재생
            PlayPreGeneratedVoice(gameEvent.Type);
        }
        else
        {
            // AI 실시간 음성 생성
            await GenerateAndPlayCommentary(gameEvent);
        }

        _isProcessing = false;
    }

    private async Awaitable PreGenerateVoiceClips()
    {
        _isPreGenerating = true;
        Debug.Log("[CommentaryManager] 사전 음성 생성 시작...");

        foreach (var kvp in PreGeneratedTexts)
        {
            string clipName = kvp.Key.ToString();

            if (_audioManager.HasCachedClip(clipName))
            {
                continue;
            }

            AudioClip clip = await _ttsManager.GenerateSpeech(kvp.Value);

            if (clip != null)
            {
                _audioManager.CacheClip(clipName, clip);
            }
            else
            {
                Debug.LogWarning($"[CommentaryManager] 사전 음성 생성 실패: {clipName}");
            }
        }

        _isPreGenerating = false;
        Debug.Log("[CommentaryManager] 사전 음성 생성 완료");
    }

    private void PlayPreGeneratedVoice(EventType eventType)
    {
        string clipName = eventType.ToString();
        AudioClip clip = _audioManager.GetCachedClip(clipName);

        if (clip != null)
        {
            _audioManager.PlayVoice(clip);

            if (PreGeneratedTexts.TryGetValue(eventType, out string text))
            {
                _narrationUI?.ShowNarration(text);
            }
        }
        else
        {
            Debug.LogWarning($"[CommentaryManager] 캐싱된 클립을 찾을 수 없습니다: {clipName}");
        }
    }

    private async Awaitable GenerateAndPlayCommentary(GameEvent gameEvent)
    {
        List<GameEvent> recentEvents = _eventManager.GetRecentEvents(_recentEventCount);

        // 1. 이벤트를 바탕으로 중계 문장 생성하기
        string narrationText = await _narrationGenerator.GenerateNarration(gameEvent, recentEvents);

        if (string.IsNullOrEmpty(narrationText))
        {
            Debug.LogWarning("[CommentaryManager] 중계 문장을 생성하는 데 실패했습니다.");
            return;
        }

        // 2. 그 문장을 음성으로 변환하기 (TTS)
        AudioClip clip = await _ttsManager.GenerateSpeech(narrationText);

        // 3. 음성을 실제로 재생하기
        if (clip != null)
        {
            _audioManager.PlayVoice(clip);
            _narrationUI?.ShowNarration(narrationText);
        }
    }
}
