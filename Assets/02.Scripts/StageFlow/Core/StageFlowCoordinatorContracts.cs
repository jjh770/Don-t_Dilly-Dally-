using Cysharp.Threading.Tasks;
using DontDillyDally.Data;
using System.Threading;

namespace DontDillyDally.StageFlow
{
    // 코디네이터가 공통으로 참조하는 읽기 전용 스테이지 상태입니다.
    public interface IStageFlowState
    {
        StageRuntimeData StageData { get; }
        bool IsGameOver { get; }
        EStagePhase CurrentPhase { get; }
        bool IsWaitingForRecipeSubmission { get; }
        float RemainingTime { get; }
        float PatientHealth { get; }
    }

    // 코디네이터가 StageFlow 바깥으로 알릴 공통 명령입니다.
    public interface IStageFlowCommands
    {
        void MarkGameOver();
        void PauseStageTimer();
        void SyncTimerState();
        void ClearRoles();
        void PublishGameOver(EGameOverReason reason);
        void PublishReward(StageReward reward, StageResult result);
    }

    // 환자 체력과 자연 감소 제어를 묶은 공통 환자 흐름 인터페이스입니다.
    public interface IStagePatientFlow
    {
        float CurrentHealth { get; }
        void Initialize(float maxHealth, float drainPerSecond, EStagePhase currentPhase);
        float ApplyDamage(float damage);
        float ApplyHeal(float heal);
        void PauseDrain();
        void ResumeDrain(EStagePhase currentPhase);
        void Tick(float deltaTime);
    }

    // 레시피 정답 뒤 미니게임 실행을 추상화한 인터페이스입니다.
    public interface IStageMiniGameRunner
    {
        UniTask<bool> RunRecipeMiniGame(CancellationToken ct);
    }

    // 결과 처리 코디네이터가 필요로 하는 공통 호스트 묶음입니다.
    public interface IStageOutcomeHost :
        IStageFlowState,
        IStageFlowCommands,
        IStagePatientFlow
    {
    }

    // 레시피 진행 코디네이터가 필요로 하는 공통 호스트 묶음입니다.
    public interface IStageRecipeProgressHost :
        IStageFlowState,
        IStagePatientFlow,
        IStageMiniGameRunner
    {
    }

    // 환자 치료 코디네이터가 필요로 하는 공통 호스트 묶음입니다.
    public interface IStagePatientTreatmentHost :
        IStageFlowState,
        IStageFlowCommands,
        IStagePatientFlow
    {
    }

    // 미니게임 결과 후처리 코디네이터가 필요로 하는 공통 호스트 묶음입니다.
    public interface IStageMiniGameResolutionHost :
        IStageFlowState,
        IStagePatientFlow
    {
    }
}
