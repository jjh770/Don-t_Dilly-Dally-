using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class RewardLLMEvaluator
{
    private const int MaxCoinBonus = 100;
    private const int MaxCoinPenalty = -50;
    private const int MaxSummaryLength = 100; // 최대 요약 길이 (문자 수)

    private static readonly string SystemPrompt = $@"
너는 병원 시뮬레이션 게임의 결과 리포터다.
플레이어 닉네임과 환자 정보를 바탕으로 게임 결과를 짧게 요약한다 ({MaxSummaryLength}자 이내).
규칙:
- 플레이어 닉네임을 그대로 사용한다 (닉네임 다음에는 '선생님' 호칭을 붙인다.)
- 환자 이름을 자연스럽게 포함한다
- 수술하지 못한 환자는 사망이 아닌 수술 일정이 딜레이 된 것이다.
- 실수는 예능감 있게 표현하되 공격적이지 않게 한다
- 패널티나 보너스에 대한 설명을 포함한다 (스토리를 부여해야 한다.)
예시 : 
(환자명)환자께서 감사함의 선물로 20코인을 추가 지불하였습니다.
(환자명)환자께서 (플레이어 닉네임)의 잘못된 처치로 의료소송을 재기하였고, 10코인의 벌금이 부과되었습니다. 

- 보너스는 최대 {MaxCoinBonus} 코인, 패널티는 최대 {MaxCoinPenalty} 코인으로 한다
- 반드시 JSON만 반환한다 (다른 텍스트 금지)
출력 형식:
{{
  ""summaryText"": ""2-3문장 결과 요약"",
  ""coinDelta"": 보너스_또는_패널티_정수
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
        bool isGameOver)
    {
        string userPrompt = BuildPrompt(events, savedCount, totalCount, isGameOver);

        string json = await _llm.SendRequest(SystemPrompt, userPrompt);

        if (string.IsNullOrWhiteSpace(json))
            return RewardNarrativeResult.Fallback;

        try
        {
            var result = JsonUtility.FromJson<RewardNarrativeResult>(json);
            result.coinDelta = Mathf.Clamp(result.coinDelta, MaxCoinPenalty, MaxCoinBonus);
            return result;
        }
        catch
        {
            return RewardNarrativeResult.Fallback;
        }
    }

    private static string BuildPrompt(
        IReadOnlyList<StagePerformanceEvent> events,
        int savedCount, int totalCount, bool isGameOver)
    {
        // 플레이어별 성공/실패 집계
        var stats = new Dictionary<string, (int success, int fail)>();
        foreach (var e in events)
        {
            if (string.IsNullOrEmpty(e.PlayerNickname)) continue;

            if (!stats.ContainsKey(e.PlayerNickname))
                stats[e.PlayerNickname] = (0, 0);

            bool isSuccess = e.EventType is
                EPerformanceEventType.TraySuccess or
                EPerformanceEventType.MiniGameSuccess or
                EPerformanceEventType.EmergencySuccess;

            bool isFail = e.EventType is
                EPerformanceEventType.TrayFail or
                EPerformanceEventType.MiniGameFail or
                EPerformanceEventType.EmergencyFail;

            var s = stats[e.PlayerNickname];
            stats[e.PlayerNickname] = isSuccess ? (s.success + 1, s.fail)
                                    : isFail ? (s.success, s.fail + 1)
                                    : s;
        }

        // 주요 이벤트 (드라마틱한 순간)
        var highlights = events
            .Where(e => e.EventType is
                EPerformanceEventType.EmergencySuccess or
                EPerformanceEventType.EmergencyFail or
                EPerformanceEventType.PatientSaved or
                EPerformanceEventType.PatientDied)
            .TakeLast(5)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"치료 결과: {savedCount}/{totalCount}명 구함 | 게임오버: {isGameOver}");
        sb.AppendLine();

        sb.AppendLine("플레이어 통계:");
        foreach (var (nick, (success, fail)) in stats)
            sb.AppendLine($"  {nick}: 성공 {success}회, 실패 {fail}회");

        sb.AppendLine();
        sb.AppendLine("주요 이벤트:");
        foreach (var e in highlights)
            sb.AppendLine($"  [{e.PlayerNickname}] {e.PatientName}({e.DiseaseName}) - {e.EventType}");

        return sb.ToString();
    }
}