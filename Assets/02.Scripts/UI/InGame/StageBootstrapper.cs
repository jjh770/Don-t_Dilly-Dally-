using DontDillyDally.StageFlow;
using UnityEngine;

public class StageBootstrapper : MonoBehaviour
{
    [SerializeField] private RewardView _rewardView;

    private RewardPresenter _rewardPresenter;

    private void Start()
    {
        if (TryInitializePresenter())
        {
            return;
        }

        StageFlowBootstrapper.StageFlowReady += HandleStageFlowReady;
    }

    private bool TryInitializePresenter()
    {
        if (_rewardPresenter != null ||
            _rewardView == null ||
            StageFlowBootstrapper.Instance == null ||
            !StageFlowBootstrapper.Instance.IsStageFlowReady ||
            StageFlowManager.Instance == null ||
            !StageFlowManager.Instance.IsInitialized)
        {
            return false;
        }

        _rewardPresenter = new RewardPresenter(_rewardView);
        _rewardView.SetPresenter(_rewardPresenter);
        StageFlowBootstrapper.StageFlowReady -= HandleStageFlowReady;
        return true;
    }

    private void HandleStageFlowReady()
    {
        TryInitializePresenter();
    }

    private void OnDestroy()
    {
        StageFlowBootstrapper.StageFlowReady -= HandleStageFlowReady;
        _rewardPresenter?.Dispose();
    }
}
