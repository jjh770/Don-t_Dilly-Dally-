using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class RewardLLMEvaluator
{
    private const int MaxCoinBonus = RewardMoneyPolicy.MaxCoinBonus;
    private const int MaxCoinPenalty = RewardMoneyPolicy.MaxCoinPenalty;
    private const int MaxSummaryLength = 100;

    private static readonly string SystemPrompt = $@"
당신은 병원 시뮬레이션 게임의 결과를 요약하는 병원 평가 AI입니다.
플레이어 활동 로그와 환자 정보를 바탕으로 결과를 요약하고, 적절한 보상 또는 벌금 값을 제안하세요.

1. 플레이어 닉네임과 환자 이름은 입력된 데이터를 그대로 사용한다.
2. 플레이어 닉네임 뒤에는 반드시 '선생님' 호칭을 붙이고 
3. summaryText는 반드시 줄바꿈(\n)으로 구분하고 3문장을 초과하지 않는다.
4. 말투는 병원 공식 보고서처럼 정중하게 작성한다. (예: ~했습니다, ~확인되었습니다)
5. 실수는 예능감 있게 표현하되 공격적이지 않게 쓴다.
6. PatientDied 이벤트에 기록된 환자를 제외한 나머지 치료하지 못한 환자는 사망보다 수술 일정 지연으로 간주한다.
7. '보너스'나 '패널티'라는 단어를 직접 쓰지 말고 그럴듯한 유머리스한 스토리(회식비, 위로금, 벌금 등)로 자연스럽게 녹인다. 
    (EX. 성공적인 수술을 마친 00환자가 감사의 의미로 회식비 50코인을 지불했습니다. / 박발냄 환자가 본인의 냄새에 의한 위로금으로 10코인을 더 / 00환자가 소송을 재기해 벌금 50코인이 발생하였습니다. )
8. 요약 내용은 전달된 moneyDelta 결과와 어긋나지 않아야 한다.
9. 보너스가 발생했다면 (moneyDelta가 0 이상이면) 병원 자산 부족, 자금 사정, 감경 조치 같은 표현을 절대 언급하지 않는다.
    즉, 병원 자산이나 자금 부족 언급은 패널티가 발생할 경우에만 사용한다.
10. 병원 폐업, 파산, 운영 종료 같은 개념은 절대 사용하지 않는다.
11. 플레이어 전체 성공 횟수와 실패 횟수를 기반으로 moneyDelta를 결정한다.


[moneyDelta 계산 규칙]
- 보너스 최대: +{MaxCoinBonus}
- 패널티 최대: {MaxCoinPenalty}
- 현재 보유 코인이 0이면 moneyDelta는 반드시 0
- 현재 보유 코인이 {-MaxCoinPenalty}보다 적으면 보유 코인 범위 안에서만 패널티가 적용된다.

[출력 형식]
{{
  ""summaryText"": ""문장1\n문장2"",
  ""requestedMoneyDelta"": 12
}}";

    private readonly LLMService _llm;

    public RewardLLMEvaluator(LLMService llm)
    {
        _llm = llm;
    }

    public async UniTask<RewardNarrativeResult> EvaluateAsync(
        IReadOnlyList<StagePerformanceEvent> events,
        int savedCount,
        int totalCount,
        bool isGameOver,
        int currentMoney)
    {
        string userPrompt = BuildPrompt(events, savedCount, totalCount, isGameOver, currentMoney);
        string json = await _llm.SendRequest(SystemPrompt, userPrompt);

        if (string.IsNullOrWhiteSpace(json))
        {
            return RewardNarrativeResult.Fallback;
        }

        try
        {
            RewardNarrativeResult result = JsonUtility.FromJson<RewardNarrativeResult>(json);
            if (string.IsNullOrWhiteSpace(result?.summaryText))
            {
                return RewardNarrativeResult.Fallback;
            }

            result.requestedMoneyDelta = Mathf.Clamp(result.requestedMoneyDelta, MaxCoinPenalty, MaxCoinBonus);
            if (currentMoney <= 0 && result.requestedMoneyDelta < 0)
            {
                result.requestedMoneyDelta = 0;
            }

            return result;
        }
        catch (System.ArgumentException)
        {
            return RewardNarrativeResult.Fallback;
        }
    }

    private static string BuildPrompt(
        IReadOnlyList<StagePerformanceEvent> events,
        int savedCount,
        int totalCount,
        bool isGameOver,
        int currentMoney)
    {
        var countsByPlayer = new Dictionary<string, Dictionary<EPerformanceEventType, int>>();

        foreach (StagePerformanceEvent e in events)
        {
            if (string.IsNullOrWhiteSpace(e.PlayerNickname))
            {
                continue;
            }

            if (!countsByPlayer.TryGetValue(e.PlayerNickname, out Dictionary<EPerformanceEventType, int> playerCounts))
            {
                playerCounts = new Dictionary<EPerformanceEventType, int>();
                countsByPlayer[e.PlayerNickname] = playerCounts;
            }

            if (!playerCounts.ContainsKey(e.EventType))
            {
                playerCounts[e.EventType] = 0;
            }

            playerCounts[e.EventType]++;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"치료 결과: {savedCount}/{totalCount}명 치료 완료 | 게임 오버: {isGameOver}");
        sb.AppendLine($"현재 병원 보유 코인: {currentMoney}");
        sb.AppendLine();
        sb.AppendLine("플레이어별 이벤트 집계:");

        foreach ((string nickname, Dictionary<EPerformanceEventType, int> playerCounts) in countsByPlayer)
        {
            sb.AppendLine($"- {nickname}");
            foreach ((EPerformanceEventType eventType, int count) in playerCounts.OrderBy(pair => pair.Key.ToString()))
            {
                sb.AppendLine($"  {eventType}: {count}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("최근 이벤트:");
        foreach (StagePerformanceEvent e in events.TakeLast(12))
        {
            sb.AppendLine($"- [{e.PlayerNickname}] {e.PatientName}({e.DiseaseName}) / {e.EventType}");
        }

        return sb.ToString();
    }
}
