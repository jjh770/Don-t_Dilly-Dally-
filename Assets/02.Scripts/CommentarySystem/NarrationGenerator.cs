using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class NarrationGenerator : MonoBehaviour
{
    [SerializeField] private LLMService _llmService;

    [Header("Prompt Settings")]
    [SerializeField, TextArea(3, 10)] private string _systemPrompt =
        "당신은 긴박한 수술 게임의 NPC 중계자입니다. " +
        "게임 상황을 짧고 긴박하게 중계해주세요. " +
        "한 문장으로 답변하고, 느낌표를 적극적으로 사용하세요.";

    public async Awaitable<string> GenerateNarration(GameEvent currentEvent, List<GameEvent> recentEvents)
    {
        string prompt = BuildPrompt(currentEvent, recentEvents);

        string result = await _llmService.SendRequest(_systemPrompt, prompt);

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

        sb.AppendLine("위 상황을 긴박한 NPC 중계 톤으로 한 문장으로 설명해주세요.");

        return sb.ToString();
    }
}
