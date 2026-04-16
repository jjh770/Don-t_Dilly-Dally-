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
                        "좋아, 수술 성공이야. 환자 상태가 좋아졌어.",
                        "좋았어. 방금 수술이 잘 끝났어.",
                        "수술 성공이야. 환자 상태가 다시 괜찮아졌어",
            }
        },
        {
            EventType.SurgeryFail, new[]
            {
                        "처치가 잘못돼서 상태가 나빠졌어.",
                        "수술이 실패했어. 빨리 다시 해.",
                        "수술 실패다. 환자 상태가 안 좋아졌어.",
            }
        },
        {
            EventType.NoSurgery, new[]
            {
                        "환자 두고 너무 지체하고 있어.",
                        "수술이 안 되고 있어. 움직여.",
                        "수술이 지연되고 있어. 서둘러.",
            }
        },
        {
            EventType.PatientCritical, new[]
            {
                        "환자 체력이 많이 떨어졌어. 서둘러.",
                        "지금 환자 체력이 위험한 수준이야.",
                        "환자 체력이 계속 줄고 있어. 서둘러.",
            }
        },
        {
            EventType.SuccessEmergencyEvent, new[]
            {
                        "좋아, 긴급 상황 막아냈어.",
                        "비상 상황을 해결했어. 계속 가자.",
                        "긴급 처치 성공이야. 잘 막았어.",
            }
        },
        {
            EventType.FailEmergencyEvent, new[]
            {
                        "응급 대응 실패야. 상태가 악화됐어.",
                        "긴급 대응이 늦었어. 상황이 꼬였어.",
                        "긴급 상황을 못 막았어. 집중해."
            }
        },
        {
            EventType.WrongMaterialUsed, new[]
            {
                        "재료가 틀렸어. 확인하고 다시 해.",
                        "잘못된 재료야. 맞는 걸 가져와.",
                        "재료 잘못됐어. 제대로 된 거 가져와.",
            }
        },
    };

    // 사전 정의된 모든 텍스트 반환 (고정형 + 템플릿형)
    public static List<string> GetAllPredefinedTexts()
    {
        var texts = new List<string>();

        // 고정형
        foreach (var text in FixedTexts.Values)
        {
            texts.Add(text);
        }

        // 템플릿형
        foreach (var templates in TemplateTexts.Values)
        {
            texts.AddRange(templates);
        }

        return texts;
    }

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

    // 동적형 fallback 텍스트
    private static readonly Dictionary<EventType, string[]> DynamicFallbackTexts = new()
    {
        {
            EventType.NewPatientAppeared, new[]
            {
                        "새 환자가 들어왔어. 준비해.",
                        "환자 들어왔어. 시작하자.",
                        "새 환자야. 준비됐지?",
            }
        },
        {
            EventType.ChainAccident, new[]
            {
                        "사고가 계속 이어지고 있어. 정신 차려.",
                        "연달아 사고 나고 있어. 정리부터 해.",
                        "사고가 멈추질 않아. 진정하고 다시 해.",
            }
        },
        {
            EventType.ChainCooperation, new[]
            {
                        "팀 호흡 좋다. 그대로 가.",
                        "좋아, 호흡이 계속 잘 맞고 있어.",
                        "팀워크 좋아. 계속 이어가.",
            }
        }
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
        Debug.Log($"[CommentaryGenerator] 환자 소개 생성 시작 - 환자: {patientName}, 병명: {diseaseName}");

        var result = new GeneratedCommentaryData();

        if (_llmService == null)
        {
            Debug.LogWarning("[CommentaryGenerator] LLMService가 없습니다. Fallback 사용.");
            result.Text = GetPatientIntroFallback(patientName, diseaseName);
            result.EstimatedDuration = EstimateDuration(result.Text);
            Debug.Log($"[CommentaryGenerator] Fallback 텍스트: {result.Text}");
            return result;
        }

        string systemPrompt = _systemPromptFile != null ? _systemPromptFile.text : "";
        string userPrompt = $"환자 이름: {patientName}\n병명: {diseaseName}\n\n이 환자를 짧게 소개해.";
        Debug.Log($"[CommentaryGenerator] LLM 요청 - 프롬프트: {userPrompt}");

        string generatedText = await _llmService.SendRequest(systemPrompt, userPrompt);

        if (string.IsNullOrEmpty(generatedText))
        {
            Debug.LogWarning($"[CommentaryGenerator] LLM 생성 실패. Fallback 사용.");
            result.Text = GetPatientIntroFallback(patientName, diseaseName);
            Debug.Log($"[CommentaryGenerator] Fallback 텍스트: {result.Text}");
        }
        else
        {
            Debug.Log($"[CommentaryGenerator] LLM 생성 성공: {generatedText}");
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
