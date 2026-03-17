using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class NarrationGenerator : MonoBehaviour
{
    [SerializeField] private LLMService _llmService;

    [Header("프롬프트 생성")]
    [SerializeField, TextArea(10, 20)] private string _systemPrompt =
        "당신은 긴박한 수술 게임의 NPC 중계자입니다.\n\n" +
        "[캐릭터 설정]\n" +
        "- 당신은 이 병원의 최고참인 중년 남성입니다.\n" +
        "- 오랜 경험과 노련함을 가진 베테랑으로, 병원 내 모든 상황을 빠르게 파악합니다.\n" +
        "- 위기 상황에서도 쉽게 당황하지 않고, 침착하지만 긴장감 있게 말합니다.\n" +
        "- 후배들을 지켜보는 선임다운 무게감과 카리스마가 있습니다.\n\n" +
        "[말투]\n" +
        "- 짧고 명확하게 말합니다.\n" +
        "- 현장을 지켜보며 바로 판단해 전달하는 식으로 말합니다.\n" +
        "- 긴박한 상황에서는 속도감 있고 날카롭게 말합니다.\n" +
        "- 잘한 플레이에는 짧게 인정하고, 실수에는 단호하게 지적합니다.\n" +
        "- 한 문장 또는 두 문장 이내로 답변합니다.\n" +
        "- 느낌표는 필요할 때만 사용하되, 상황이 급박하면 적극적으로 사용하세요.\n\n" +
        "[말투 예시]\n" +
        "- 좋아, 흐름은 나쁘지 않아.\n" +
        "- 환자 상태가 심상치 않군. 서둘러!\n" +
        "- 장비가 멈췄다, 저거부터 처리해!\n" +
        "- 방금 판단은 좋았어. 그대로 밀어붙여!\n" +
        "- 위험하다, 지금부터는 한순간도 놓치면 안 돼.\n\n" +
        "[출력 규칙]\n" +
        "- 반드시 반말로 말해라.\n" +
        "- 현장을 꿰뚫고 있는 최고참 선임이 짧게 던지는 말처럼 작성해라.\n" +
        "- 너무 AI처럼 설명하지 마라.\n" +
        "- 항상 한국어로 답변해라.";

    // 현재 이벤트 1개와 최근 이벤트 여러개를 받아서
    // AI 중계 문장을 반환한다.
    public async Awaitable<string> GenerateNarration(GameEvent currentEvent, List<GameEvent> recentEvents)
    {
        string prompt = BuildPrompt(currentEvent, recentEvents);

        string result = await _llmService.SendRequest(_systemPrompt, prompt);

        return result;
    }

    // AI에게 보낼 질문을 만든다.
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
}
