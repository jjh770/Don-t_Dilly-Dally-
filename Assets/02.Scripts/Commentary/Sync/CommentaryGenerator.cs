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
            EventType.SurgeryFail, new[]
            {
                "수술 실패야. 다시 준비해.",
                "처치가 잘못돼서 상태가 나빠졌어.",
                "수술이 실패했어. 빨리 다시 해.",
                "실패다. 환자 상태 안 좋아졌어.",
                "지금 수술 실패했어. 다시 진행해.",
                "처치 실패야. 환자 상태 확인해.",
                "수술이 틀어졌어. 빨리 수습해.",
                "수술 실패했어. 환자 상태가 떨어지고 있어.",
                "수술 결과가 안 좋아. 다시 해."
            }
        },
        {
            EventType.NoSurgery, new[]
            {
                "지금 수술이 멈췄어. 빨리 진행해.",
                "환자 두고 너무 지체하고 있어.",
                "수술이 안 되고 있어. 움직여.",
                "환자 방치되고 있어. 수술 들어가.",
                "왜 수술이 안 돼? 빨리 해.",
                "지금 시간 낭비 중이야. 수술해.",
                "환자 기다리고 있어. 빨리 처치해.",
                "수술이 지연되고 있어. 서둘러.",
                "멈추지 마. 수술 계속 진행해."
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
        {
            EventType.SuccessEmergencyEvent, new[]
            {
                "좋아, 긴급 상황 막아냈어.",
                "위기를 넘겼어. 잘했어.",
                "긴급 상황 처리 완료. 좋았어.",
                "비상 상황을 해결했어. 계속 가자.",
                "긴급 처치 성공이야. 잘 막았어.",
                "위기 상황 넘겼어. 좋아.",
                "응급 상황을 막아냈어. 다음 준비해.",
                "긴급 상황 잘 처리했어.",
                "위험 상황이 해결됐어. 잘했다."
            }
        },
        {
            EventType.FailEmergencyEvent, new[]
            {
                "긴급 상황 대응 실패야.",
                "위기를 못 막았어. 상황이 나빠졌어.",
                "긴급 처치 실패했어.",
                "비상 상황 막지 못했어.",
                "응급 대응 실패야. 상태가 악화됐어.",
                "위기 상황을 놓쳤어.",
                "긴급 이벤트 실패. 빨리 수습해.",
                "대응이 늦었어. 상황이 꼬였어.",
                "긴급 상황을 못 막았어. 집중해."
            }
        },
        {
            EventType.WrongMaterialUsed, new[]
            {
                "재료 잘못 넣었어. 다시 확인해.",
                "지금 쓴 재료 틀렸어. 제대로 가져와.",
                "그 재료 아니야. 다시 맞춰.",
                "재료가 틀렸어. 확인하고 다시 해.",
                "잘못된 재료야. 맞는 걸 가져와.",
                "재료 확인해. 그거 아니야.",
                "틀린 재료 넣었어. 다시 봐.",
                "재료 잘못됐어. 제대로 된 거 가져와.",
                "그거 아니야. 재료 다시 확인해."
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

    // 동적형 fallback 텍스트
    private static readonly Dictionary<EventType, string[]> DynamicFallbackTexts = new()
    {
        {
            EventType.NewPatientAppeared, new[]
            {
                "새 환자 들어왔어. 준비해.",
                "환자 한 명 더 왔어. 바로 움직여.",
                "새로운 환자 도착했어. 자리 잡아.",
                "다음 환자야. 빨리 준비해.",
                "환자 왔어. 움직여.",
                "새 환자다. 바로 시작해."
            }
        },
        {
            EventType.ChainAccident, new[]
            {
                "사고가 계속 이어지고 있어. 정신 차려.",
                "연달아 사고 나고 있어. 정리부터 해.",
                "또 사고다. 흐름 완전히 꼬였어.",
                "계속 꼬이고 있어. 흐름부터 다시 잡아.",
                "연속 사고야. 집중해.",
                "사고가 멈추질 않아. 진정하고 다시 해.",
                "흐름이 계속 안 좋아. 정신 차려.",
                "또 실수야. 연속으로 터지고 있어.",
                "사고가 이어지고 있어. 한 번 멈추고 정리해."
            }
        },
        {
            EventType.ChainCooperation, new[]
            {
                "좋아, 연계가 잘 맞고 있어.",
                "팀 호흡 좋다. 그대로 가.",
                "협동이 계속 잘 이어지고 있어.",
                "좋아, 호흡이 계속 잘 맞고 있어.",
                "연계 좋다. 지금 흐름 유지해.",
                "계속 성공이야. 이대로 가자.",
                "팀워크 좋아. 계속 이어가.",
                "좋은 흐름이야. 멈추지 마.",
                "연속 성공. 잘하고 있어."
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
