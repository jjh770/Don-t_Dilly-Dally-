using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class GeneratedCommentaryData
{
    public string Text;
    public float EstimatedDuration;
}

// 코멘터리 문장 생성 담당 (호스트 전용)
// - 고정형: 사전 정의 텍스트
// - 템플릿형: 랜덤 선택
// - 동적형: LLM으로 생성
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
        { EventType.MaterialDeliveredLate, new[] { "재료 전달이 늦어지고 있어.", "재료가 늦어. 서둘러.", "전달이 지연되고 있다." } },
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

}
