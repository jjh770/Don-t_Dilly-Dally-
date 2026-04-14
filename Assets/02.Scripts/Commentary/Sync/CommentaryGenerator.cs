using Cysharp.Threading.Tasks;
using DontDillyDally.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;

public class GeneratedCommentaryData
{
    public string Text;
    public float EstimatedDuration;
}

// 코멘터리 문장 생성 담당 (호스트 전용)
public class CommentaryGenerator : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private LLMService _llmService;

    [Header("프롬프트")]
    [SerializeField] private TextAsset _systemPromptFile;

    [Header("설정")]
    [SerializeField] private int _recentEventCount = 5;

    // 고정형 텍스트
    private static readonly Dictionary<EventType, string> FixedTexts = new()
    {
        { EventType.TimeOut, "이번 수술은 여기까지다. 수고했어." },
        { EventType.PatientDeath, "환자 상태가 끝내 무너졌다..." }
    };

    // 템플릿형 텍스트
    private static readonly Dictionary<EventType, string[]> TemplateTexts = new()
    {
         {
            EventType.SurgerySuccess, new[]
            {
                "좋아, 수술 성공이야. 환자 상태가 안정됐어.",
                "좋았어. 방금 수술이 잘 끝났어.",
                "잘했어. 수술이 제대로 마무리됐어.",
                "좋아, 이번 처치는 성공이야.",
                "방금 수술 잘 들어갔어.",
                "좋아, 수술 결과 괜찮아. 계속 가자.",
                "수술 성공이야. 환자 상태가 다시 괜찮아졌어",
                "잘했다. 수술 하나 깔끔하게 끝냈어.",
                "좋아, 수술은 성공이다. 다음 준비해."
            }
        },
        {
            EventType.PatientCritical, new[]
            {
                "환자 체력이 많이 떨어졌어. 서둘러.",
                "환자 체력이 낮아. 빨리 처치해.",
                "지금 환자 체력이 위험한 수준이야.",
                "환자 체력이 계속 줄고 있어. 서둘러.",
                "환자 상태 안 좋아. 체력부터 회복시켜.",
                "환자 체력이 얼마 안 남았어. 빨리 움직여.",
                "지금 환자 체력 낮아졌어. 집중해.",
                "환자 체력이 바닥나기 직전이야. 서둘러.",
                "환자부터 봐. 체력이 너무 낮아."
            }
        },
    };

    // 타입에 따라 최종 텍스트를 생성
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
            Debug.LogWarning("[CommentaryGenerator] LLMService가 없습니다. Fallback 텍스트 사용.");
            result.Text = GetFallbackText(gameEvent);
            result.EstimatedDuration = EstimateDuration(result.Text);
            return result;
        }

        var recentEvents = EventManager.Instance?.GetRecentEvents(_recentEventCount) ?? new List<GameEvent>();
        string prompt = BuildPrompt(gameEvent, recentEvents);
        string systemPrompt = _systemPromptFile != null ? _systemPromptFile.text : "";

        string generatedText = await _llmService.SendRequest(systemPrompt, prompt);

        if (string.IsNullOrEmpty(generatedText))
        {
            Debug.LogWarning($"[CommentaryGenerator] LLM 생성 실패. Fallback 텍스트 사용: {gameEvent.Type}");
            result.Text = GetFallbackText(gameEvent);
        }
        else
        {
            result.Text = generatedText;
        }

        result.EstimatedDuration = EstimateDuration(result.Text);
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

    // 동적형 fallback 텍스트 (LLM 실패 시 랜덤 선택)
    private static readonly Dictionary<EventType, string[]> DynamicFallbackTexts = new()
    {
        { EventType.NewPatientAppeared, new[] { "새로운 환자가 도착했다.", "환자가 들어왔어. 준비해.", "새 환자야, 집중." } },
        { EventType.EmergencyPrevented, new[] { "위기를 잘 넘겼어.", "훌륭해, 위기 대응 성공.", "잘 막았어." } },
        { EventType.WrongMaterialUsed, new[] { "잘못된 재료를 사용했어.", "재료가 틀렸어. 확인해.", "그건 아니야." } },
        { EventType.RepairTimeout, new[] { "수리 시간을 초과했어.", "수리 실패. 시간 초과.", "늦었어..." } },
        { EventType.RepairCompletedFast, new[] { "빠른 수리였어", "수리 완료. 빨랐어.", "훌륭한 속도야!" } },
        { EventType.RepairCompletedLate, new[] { "수리가 늦어졌지만 완료했어.", "어쨌든 고쳤어.", "늦었지만 성공이야." } },
        { EventType.ChainAccident, new[] { "사고가 연속으로 발생하고 있어", "연속 사고다. 정신 차려!", "또 사고야" } },
        { EventType.ChainCooperation, new[] { "팀워크가 훌륭해.", "연속 협동! 잘하고 있어.", "호흡이 좋아." } }
    };

    private string GetFallbackText(GameEvent gameEvent)
    {
        if (DynamicFallbackTexts.TryGetValue(gameEvent.Type, out string[] texts))
        {
            return texts[UnityEngine.Random.Range(0, texts.Length)];
        }

        return gameEvent.Description ?? "상황이 발생했습니다.";
    }

    private float EstimateDuration(string text)
    {
        if (string.IsNullOrEmpty(text)) return 2f;
        return Mathf.Clamp(text.Length / 4.5f, 1.5f, 10f);
    }

    // ========== 환자 소개 텍스트 생성 ==========

    public async Awaitable<GeneratedCommentaryData> GeneratePatientIntro(string patientName, string diseaseName)
    {
        var result = new GeneratedCommentaryData();

        if (_llmService == null)
        {
            result.Text = GetPatientIntroFallback(patientName, diseaseName);
            result.EstimatedDuration = EstimateDuration(result.Text);
            return result;
        }

        string systemPrompt = _systemPromptFile != null ? _systemPromptFile.text : "";
        string userPrompt = $"환자 이름: {patientName}\n병명: {diseaseName}\n\n이 환자를 짧게 소개해.";
        string generatedText = await _llmService.SendRequest(systemPrompt, userPrompt);

        if (string.IsNullOrEmpty(generatedText))
        {
            result.Text = GetPatientIntroFallback(patientName, diseaseName);
        }
        else
        {
            result.Text = generatedText;
        }

        result.EstimatedDuration = EstimateDuration(result.Text);
        return result;
    }

    private string GetPatientIntroFallback(string patientName, string diseaseName)
    {
        string[] fallbacks = new[]
        {
            $"새 환자다. {patientName}, {diseaseName}. 준비해.",
            $"{patientName} 환자, {diseaseName}으로 입원했어.",
            $"다음 환자 {patientName}. {diseaseName}이야."
        };

        return fallbacks[UnityEngine.Random.Range(0, fallbacks.Length)];
    }
}
