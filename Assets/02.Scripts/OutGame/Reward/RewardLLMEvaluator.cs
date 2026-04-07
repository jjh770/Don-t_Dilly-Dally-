using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class RewardLLMEvaluator
{
    private const int MaxCoinBonus = 100;
    private const int MaxCoinPenalty = -50;
    private const int MaxSummaryLength = 100;

    private static readonly string SystemPrompt = $@"
너는 병원 시뮬레이션 게임의 결과를 보고하는 병원 평가 AI다.
플레이어 닉네임과 환자 정보를 바탕으로 게임 결과를 {MaxSummaryLength}자 이내로 요약한다.

[규칙]
1. 플레이어 닉네임과 환자 이름은 입력된 데이터를 그대로 사용한다.
2. 플레이어 닉네임 뒤에는 반드시 '선생님' 호칭을 붙이고 
3. summaryText는 반드시 줄바꿈(\n)으로 구분하고 3문장을 초과하지 않는다.
4. 말투는 병원 공식 보고서처럼 정중하게 작성한다. (예: ~했습니다, ~확인되었습니다)
5. 실수는 예능감 있게 표현하되 공격적이지 않게 쓴다.
6. 치료하지 못한 환자는 사망보다 수술 일정 지연이나 병원 사고처럼 표현한다.
7. '보너스'나 '패널티'라는 단어를 직접 쓰지 말고 현실적인 스토리(회식비, 위로금, 벌금 등)로 자연스럽게 녹인다. 
    (EX. 성공적인 수술을 마친 00환자가 감사의 의미로 회식비 50코인을 지불했습니다. / 00환자가 소송을 재기해 벌금 50코인이 발생하였습니다. )
8. 요약 내용은 전달된 moneyDelta 결과와 어긋나지 않아야 한다.
9. 보너스가 발생했다면 (moneyDelta가 0 이상이면) 병원 자산 부족, 자금 사정, 감경 조치 같은 표현을 절대 언급하지 않는다.
    즉, 병원 자산이나 자금 부족 언급은 패널티가 발생할 경우에만 사용한다.
10. 병원 폐업, 파산, 운영 종료 같은 개념은 절대 사용하지 않는다.
11. 플레이어 전체 성공 횟수와 실패 횟수를 기반으로 moneyDelta를 결정한다.
12. 보너스가 발생한 경우 가장 성공 횟수가 많은 플레이어를 언급하며 그 선생님 덕분에 보너스가 발생했음을 서술한다
    (예: 민수 선생님의 활약 덕분에 ~)
13. 패널티가 발생한 경우 가장 실패 횟수가 많은 플레이어를 언급하며 그 선생님으로 인해 패널티가 발생했음을 서술한다
    (예: 지현 선생님의 잦은 실수로 인해 ~)
    단, 표현은 공격적이지 않게 사무적으로 작성한다

[moneyDelta 계산 규칙]
- 보너스 최대: +{MaxCoinBonus}
- 패널티 최대: {MaxCoinPenalty}
- 현재 보유 코인이 0이면 moneyDelta는 반드시 0
- 현재 보유 코인이 {-MaxCoinPenalty}보다 적으면 보유 코인 범위 안에서만 패널티가 적용된다.

[출력 형식 - JSON만 반환, 다른 텍스트 금지]
{{
  ""summaryText"": ""문장1\n문장2\n문장3""
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
        int currentMoney,
        RewardMoneyAdjustment adjustment)
    {
        string userPrompt = BuildPrompt(events, savedCount, totalCount, isGameOver, currentMoney, adjustment);
        string json = await _llm.SendRequest(SystemPrompt, userPrompt);

        if (string.IsNullOrWhiteSpace(json))
        {
            return RewardNarrativeResult.Fallback;
        }

        try
        {
            RewardNarrativeResult result = JsonUtility.FromJson<RewardNarrativeResult>(json);
            return string.IsNullOrWhiteSpace(result?.summaryText)
                ? RewardNarrativeResult.Fallback
                : result;
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
        int currentMoney,
        RewardMoneyAdjustment adjustment)
    {
        var stats = new Dictionary<string, (int success, int fail)>();

        foreach (StagePerformanceEvent e in events)
        {
            if (string.IsNullOrEmpty(e.PlayerNickname)) continue;

            if (!stats.ContainsKey(e.PlayerNickname))
                stats[e.PlayerNickname] = (0, 0);

            (int success, int fail) s = stats[e.PlayerNickname];

            stats[e.PlayerNickname] = IsSuccess(e.EventType) ? (s.success + 1, s.fail)
                                    : IsFailure(e.EventType) ? (s.success, s.fail + 1)
                                    : s;
        }

        List<StagePerformanceEvent> highlights = events
            .Where(e => e.EventType is
                EPerformanceEventType.EmergencySuccess or
                EPerformanceEventType.EmergencyFail or
                EPerformanceEventType.PatientSaved or
                EPerformanceEventType.PatientDied or
                EPerformanceEventType.Timeout)
            .TakeLast(5)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"치료 결과: {savedCount}/{totalCount}명 치료 완료 | 게임 오버: {isGameOver}");
        sb.AppendLine($"현재 병원 보유 코인: {currentMoney}");
        sb.AppendLine($"원래 계산된 moneyDelta: {adjustment.RequestedDelta}");
        sb.AppendLine($"최종 적용된 moneyDelta: {adjustment.AppliedDelta}");
        sb.AppendLine($"자산 부족으로 감경되었는지: {adjustment.WasLimitedByBalance}");

        if (adjustment.AppliedDelta < 0 && adjustment.WasLimitedByBalance && currentMoney <= 0)
        {
            sb.AppendLine("병원 자산이 0이므로 금전 차감은 불가능했다. 경고 조치만 받은 흐름으로 요약한다.");
        }
        else if (adjustment.AppliedDelta < 0 && adjustment.WasLimitedByBalance)
        {
            sb.AppendLine("병원 자산이 부족하므로 전체 패널티를 그대로 적용하지 못했다. 자산 부족을 감안해 경고 또는 감경 조치가 내려진 흐름으로 요약한다.");
        }

        sb.AppendLine();
        sb.AppendLine("플레이어 통계:");
        foreach ((string nick, (int success, int fail)) in stats)
        {
            sb.AppendLine($"  {nick}: 성공 {success}회 / 실패 {fail}회");
        }

        sb.AppendLine();
        sb.AppendLine("주요 이벤트:");
        foreach (StagePerformanceEvent e in highlights)
        {
            sb.AppendLine($"  [{e.PlayerNickname}] {e.PatientName}({e.DiseaseName}) - {e.EventType}");
        }

        return sb.ToString();
    }
    private static bool IsSuccess(EPerformanceEventType type) => type is
    EPerformanceEventType.TraySuccess or
    EPerformanceEventType.MiniGameSuccess or
    EPerformanceEventType.EmergencySuccess or
    EPerformanceEventType.PatientSaved;

    private static bool IsFailure(EPerformanceEventType type) => type is
        EPerformanceEventType.TrayFail or
        EPerformanceEventType.MiniGameFail or
        EPerformanceEventType.EmergencyFail or
        EPerformanceEventType.PatientDied or
        EPerformanceEventType.Timeout;

}
