using System.Collections.Generic;
using UnityEngine;

public class CommentaryManager : MonoBehaviour
{
    public static CommentaryManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private EventManager _eventManager;
    [SerializeField] private NarrationGenerator _narrationGenerator;
    [SerializeField] private TTSManager _ttsManager;
    [SerializeField] private CommentaryAudioManager _audioManager;
    [SerializeField] private UI_Commentary _narrationUI;

    [Header("Settings")]
    [SerializeField] private float _commentaryCooldown = 2f;
    [SerializeField] private int _recentEventCount = 5;

    private float _lastCommentaryTime;
    private GameEvent _pendingEvent;
    private bool _isProcessing;

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
        {
            _eventManager.OnEventPublished += HandleEvent;
        }
    }

    private void OnDisable()
    {
        if (_eventManager != null)
        {
            _eventManager.OnEventPublished -= HandleEvent;
        }
    }

    private void HandleEvent(GameEvent gameEvent)
    {
        // 쿨다운 체크
        if (Time.time - _lastCommentaryTime < _commentaryCooldown)
        {
            // 우선순위가 더 높은 이벤트면 대기 중인 이벤트 교체
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

    private void PlayPreGeneratedVoice(EventType eventType)
    {
        string clipName = GetPreGeneratedClipName(eventType);
        AudioClip clip = _audioManager.GetPreGeneratedClip(clipName);

        if (clip != null)
        {
            _audioManager.PlayVoice(clip);
            _narrationUI?.ShowNarration(GetPreGeneratedText(eventType));
        }
        else
        {
            Debug.LogWarning($"[CommentaryManager] Pre-generated clip not found: {clipName}");
        }
    }

    private async Awaitable GenerateAndPlayCommentary(GameEvent gameEvent)
    {
        List<GameEvent> recentEvents = _eventManager.GetRecentEvents(_recentEventCount);

        // AI 중계 텍스트 생성
        string narrationText = await _narrationGenerator.GenerateNarration(gameEvent, recentEvents);

        if (string.IsNullOrEmpty(narrationText))
        {
            Debug.LogWarning("[CommentaryManager] Failed to generate narration");
            return;
        }

        // TTS 음성 생성
        AudioClip clip = await _ttsManager.GenerateSpeech(narrationText);

        if (clip != null)
        {
            _audioManager.PlayVoice(clip);
            _narrationUI?.ShowNarration(narrationText);
        }
    }

    private string GetPreGeneratedClipName(EventType eventType)
    {
        return eventType switch
        {
            EventType.GameStart => "game_start",
            EventType.GameOver => "game_over",
            EventType.SurgerySuccess => "surgery_success",
            EventType.SurgeryFail => "surgery_fail",
            EventType.PatientDeath => "patient_death",
            _ => null
        };
    }

    private string GetPreGeneratedText(EventType eventType)
    {
        return eventType switch
        {
            EventType.GameStart => "수술을 시작합니다!",
            EventType.GameOver => "게임이 종료되었습니다.",
            EventType.SurgerySuccess => "수술이 성공적으로 완료되었습니다!",
            EventType.SurgeryFail => "수술에 실패했습니다...",
            EventType.PatientDeath => "환자가 사망했습니다...",
            _ => ""
        };
    }
}
