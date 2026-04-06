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
    너는 수술 결과에 대해 보고하는 병원의 원무팀장이다.
    플레이어 닉네임과 환자 정보를 바탕으로 게임 결과를 {MaxSummaryLength}자 이내로 요약한다.

    [규칙]
    1. 플레이어 닉네임과 환자 이름은 입력된 데이터 그대로만 사용한다
    2. 플레이어 닉네임 뒤에는 반드시 '선생님' 호칭을 붙인다 (예: 민수 → 민수 선생님)
    3. summaryText의  반드시 줄바꿈(\n)으로 구분하며 3문장을 초과하지 않는다.
    4. 말투는 병원 공식 보고서처럼 사무적으로 작성한다 (예: ~했습니다, ~되었습니다)
    5. 실수는 예능감 있게 표현하되 공격적이지 않게 한다
    6. 처치하지 못한 환자는 사망이 아닌 수술 일정 지연으로 표현한다
    7. 병원 폐업은 절대 언급하지 않는다
    8. '보너스'/'패널티' 단어를 절대 직접 쓰지 않고 스토리로 자연스럽게 녹인다
    9. 요약 내용과 moneyDelta는 반드시 일관되어야 한다

    [moneyDelta 계산 규칙]
    - 보너스 최대: +{MaxCoinBonus}
    - 패널티 최대: {MaxCoinPenalty}
    - 현재 보유 코인이 0이면 moneyDelta는 반드시 0
    - 현재 보유 코인이 {-MaxCoinPenalty}보다 적으면 보유 코인 범위 안에서만 패널티 적용

    [출력 형식 - JSON만 반환, 다른 텍스트 금지]
    {{
      ""summaryText"": ""문장1\n문장2\n문장3"",
      ""moneyDelta"": 정수
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
            int minAllowedPenalty = -Mathf.Min(currentMoney, -MaxCoinPenalty);
            result.moneyDelta = Mathf.Clamp(result.moneyDelta, minAllowedPenalty, MaxCoinBonus);
            return result;
        }
        catch
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
        var stats = new Dictionary<string, (int success, int fail)>();
        foreach (StagePerformanceEvent e in events)
        {
            if (string.IsNullOrEmpty(e.PlayerNickname))
            {
                continue;
            }

            if (!stats.ContainsKey(e.PlayerNickname))
            {
                stats[e.PlayerNickname] = (0, 0);
            }

            bool isSuccess = e.EventType is
                EPerformanceEventType.TraySuccess or
                EPerformanceEventType.MiniGameSuccess or
                EPerformanceEventType.EmergencySuccess;

            bool isFail = e.EventType is
                EPerformanceEventType.TrayFail or
                EPerformanceEventType.MiniGameFail or
                EPerformanceEventType.EmergencyFail;

            (int success, int fail) s = stats[e.PlayerNickname];
            stats[e.PlayerNickname] = isSuccess ? (s.success + 1, s.fail)
                : isFail ? (s.success, s.fail + 1)
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

        int minAllowedPenalty = -Mathf.Min(currentMoney, -MaxCoinPenalty);

        var sb = new StringBuilder();
        sb.AppendLine($"치료 결과: {savedCount}/{totalCount}명 구함 | 게임오버: {isGameOver}");
        sb.AppendLine($"현재 병원 보유 코인: {currentMoney}");
        sb.AppendLine($"이번 결과에서 허용 가능한 최소 패널티: {minAllowedPenalty}");

        if (currentMoney <= 0)
        {
            sb.AppendLine("병원 자산이 0이므로 벌금 차감은 불가능하다. moneyDelta는 반드시 0으로 작성하고 경고 조치만 받은 것으로 요약한다.");
        }
        else if (currentMoney < -MaxCoinPenalty)
        {
            sb.AppendLine("병원 자산이 부족하므로 전체 최대 패널티를 그대로 적용할 수 없다. 보유 자산 범위 안에서만 패널티를 제시하고, 자산 부족을 감안해 경고 조치가 함께 언급되도록 요약한다.");
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
}
