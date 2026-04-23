using DontDillyDally.StageFlow;
using UnityEngine;

public class RewardPresenter
{
    private readonly RewardView _view;
    private readonly StageFlowManager _stageFlowManager;

    public RewardPresenter(RewardView view)
    {
        _view = view;
        _stageFlowManager = StageFlowManager.Instance;

        if (_stageFlowManager != null)
        {
            _stageFlowManager.OnStageRewardGranted += HandleStageRewardGranted;
        }
    }

    public void ReturnWaitingRoom()
    {
        PhotonServerManager.Instance.ReturnWaitingRoom();
    }

    private void HandleStageRewardGranted(StageRewardSettlement settlement)
    {
        _view.InitializeWallet(settlement.BeforeCoin, settlement.BeforeStar);

        _view.Show(() =>
        {
            StageReward reward = settlement.Reward;
            StageResult result = settlement.Result;

            _view.PlayRewardSequence(
                reward.Stars,
                result.SurvivalRatio,
                settlement.AfterCoin,
                settlement.AfterStar,
                reward.SummaryText,
                reward.Money - reward.MoneyDelta,
                reward.MoneyDelta);
        });
    }

    public void Dispose()
    {
        if (_stageFlowManager != null)
        {
            _stageFlowManager.OnStageRewardGranted -= HandleStageRewardGranted;
        }
    }
}
