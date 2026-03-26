using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class GeneratedCommentaryData
{
    public string Text;
    public string TtsAudioKey;
    public float EstimatedDuration;
}

/// <summary>
/// 코멘터리 문장 생성 담당 (호스트 전용)
/// - 고정형: 사전 정의 텍스트
/// - 템플릿형: 랜덤 선택
/// - 동적형: LLM으로 생성
/// </summary>
public class CommentaryGenerator : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private LLMService _llmService;
    [SerializeField] private TTSManager _ttsManager;
    [SerializeField] private CommentaryPlaybackManager _playbackManager;
    [SerializeField] private EventManager _eventManager;

    [Header("프롬프트")]
    [SerializeField] private TextAsset _systemPromptFile;

    [Header("설정")]
    [SerializeField] private int _recentEventCount = 5;
    [SerializeField] private bool _generateTtsForDynamic = true;

    // 고정형 텍스트
    private static readonly Dictionary<EventType, string> FixedTexts = new()
    {
        { EventType.GameStart, "좋아, 수술 시작이다. 집중해!" },
        { EventType.GameOver, "여기까지다. 수고했어." },
        { EventType.SurgerySuccess, "해냈군. 완벽한 수술이었어." },
        { EventType.SurgeryFail, "끝났어... 이번엔 실패다." },
        { EventType.PatientDeath, "환자를 잃었다... 다음엔 놓치지 마." }
    };

    // 템플릿형 텍스트
    private static readonly Dictionary<EventType, string[]> TemplateTexts = new()
    {
        {
            EventType.EquipmentAccident, new[]
            {
                "이런, 장비에 문제가 생겼어!",
                "장비 사고다! 빨리 대처해!",
                "장비가 말썽이야. 침착하게 처리해."
            }
        },
        {
            EventType.PatientCritical, new[]
            {
                "환자 상태가 위험해! 서둘러!",
                "위급 상황이다! 집중해!",
                "환자가 위험해, 빨리 조치를 취해!"
            }
        },
        {
            EventType.EmergencyEvent, new[]
            {
                "긴급 상황 발생! 모두 주목!",
                "비상이다! 대응 준비!",
                "긴급 이벤트! 빠른 판단이 필요해!"
            }
        },
        {
            EventType.MachineBroken, new[]
            {
                "기계가 고장났어! 수리가 필요해!",
                "장비 고장! 대체 장비를 준비해!",
                "기계 문제 발생! 빨리 해결해야 해!"
            }
        }
    };

    public async Awaitable<GeneratedCommentaryData> GenerateCommentary(GameEvent gameEvent)
    {
        var result = new GeneratedCommentaryData();

        // 1. 고정형
        if (gameEvent.UsePreGeneratedVoice && FixedTexts.TryGetValue(gameEvent.Type, out string fixedText))
        {
            result.Text = fixedText;
            result.EstimatedDuration = EstimateDuration(fixedText);
            return result;
        }

        // 2. 템플릿형
        if (TemplateTexts.TryGetValue(gameEvent.Type, out string[] templates))
        {
            result.Text = templates[UnityEngine.Random.Range(0, templates.Length)];
            result.EstimatedDuration = EstimateDuration(result.Text);

            if (_generateTtsForDynamic)
            {
                result.TtsAudioKey = await GenerateAndCacheTts(result.Text);
            }
            return result;
        }

        // 3. 동적형: LLM 생성
        return await GenerateDynamicCommentary(gameEvent);
    }

    private async Awaitable<GeneratedCommentaryData> GenerateDynamicCommentary(GameEvent gameEvent)
    {
        var result = new GeneratedCommentaryData();

        if (_llmService == null)
        {
            result.Text = GetFallbackText(gameEvent);
            result.EstimatedDuration = EstimateDuration(result.Text);
            return result;
        }

        var recentEvents = _eventManager?.GetRecentEvents(_recentEventCount) ?? new List<GameEvent>();
        string prompt = BuildPrompt(gameEvent, recentEvents);
        string systemPrompt = _systemPromptFile != null ? _systemPromptFile.text : "";

        string generatedText = await _llmService.SendRequest(systemPrompt, prompt);
        result.Text = string.IsNullOrEmpty(generatedText) ? GetFallbackText(gameEvent) : generatedText;
        result.EstimatedDuration = EstimateDuration(result.Text);

        if (_generateTtsForDynamic)
        {
            result.TtsAudioKey = await GenerateAndCacheTts(result.Text);
        }

        return result;
    }

    private string BuildPrompt(GameEvent currentEvent, List<GameEvent> recentEvents)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("현재 상황:");
        sb.AppendLine($"- {currentEvent.Description}");
        sb.AppendLine();

        if (recentEvents.Count > 0)
        {
            sb.AppendLine("최근 발생한 이벤트:");
            foreach (GameEvent evt in recentEvents)
            {
                sb.AppendLine($"- {evt.Description}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("위 상황을 반말로, 한 문장으로 중계해라.");
        return sb.ToString();
    }

    private async Awaitable<string> GenerateAndCacheTts(string text)
    {
        if (_ttsManager == null || _playbackManager == null) return null;

        try
        {
            AudioClip clip = await _ttsManager.GenerateSpeech(text);
            if (clip != null)
            {
                string cacheKey = Guid.NewGuid().ToString();
                _playbackManager.CacheClip(cacheKey, clip);
                return cacheKey;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CommentaryGenerator] TTS 생성 실패: {e.Message}");
        }
        return null;
    }

    private string GetFallbackText(GameEvent gameEvent)
    {
        return gameEvent.Type switch
        {
            EventType.NewPatientAppeared => "새로운 환자가 도착했다!",
            EventType.MaterialDeliveredLate => "재료 전달이 늦어지고 있어!",
            EventType.EmergencyPrevented => "위기를 잘 넘겼어!",
            EventType.WrongMaterialUsed => "잘못된 재료를 사용했어!",
            EventType.RepairTimeout => "수리 시간을 초과했어!",
            EventType.RepairCompletedFast => "빠른 수리였어!",
            EventType.RepairCompletedLate => "수리가 늦어졌지만 완료했어.",
            EventType.ChainAccident => "사고가 연속으로 발생하고 있어!",
            EventType.ChainCooperation => "팀워크가 훌륭해!",
            _ => gameEvent.Description ?? "상황이 발생했습니다."
        };
    }

    private float EstimateDuration(string text)
    {
        if (string.IsNullOrEmpty(text)) return 2f;
        return Mathf.Clamp(text.Length / 4.5f, 1.5f, 10f);
    }

    public async Awaitable PreGenerateFixedVoices()
    {
        if (_ttsManager == null || _playbackManager == null) return;

        foreach (var kvp in FixedTexts)
        {
            string clipId = kvp.Key.ToString();
            if (_playbackManager.HasCachedClip(clipId)) continue;

            AudioClip clip = await _ttsManager.GenerateSpeech(kvp.Value);
            if (clip != null)
            {
                _playbackManager.CacheClip(clipId, clip);
            }
        }
    }
}
