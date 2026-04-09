using Photon.Pun;
using UnityEngine;
public class WaitingRoomBootStrapper : MonoBehaviourPunCallbacks
{
    [SerializeField] private WaitingRoomClickManager _clickManager;

    [SerializeField] private WaitingRoomView _defaultView;

    [SerializeField] private ContextMenuView _popupView;

    [SerializeField] private UI_Customizing _customizingUI;
    [SerializeField] private UI_StagePanelView _stageUnlockPanelView;
    [SerializeField] private UI_HospitalUpgradeView _hospitalUpgradeView;

    private WaitingRoomModel _model;
    private CustomizingUIViewModel _customizingViewModel;

    private WaitingRoomPresenter _presenter;
    private UI_StagePanelPresenter _stageUnlockPanelPresenter;
    private UI_HospitalUpgradePresenter _hospitalUpgradePresenter;

    private void Start()
    {
        if (!PhotonNetwork.InRoom) return;

        Init();
        RoomDataManager.Instance.LoadRoomData();
    }

    public override void OnJoinedRoom()
    {
        Init();
    }

    private void Init()
    {
        _presenter?.Dispose();
        _stageUnlockPanelPresenter?.Dispose();
        _hospitalUpgradePresenter?.Dispose();
        _model = new WaitingRoomModel();

        _presenter = new WaitingRoomPresenter(_defaultView, _popupView, _model);
        _presenter.Initialize();

        _defaultView.Initialized(_presenter);
        _popupView.SetPresenter(_presenter);

        _clickManager.Initialized(_presenter);

        if (_stageUnlockPanelView != null)
        {
            _stageUnlockPanelPresenter = new UI_StagePanelPresenter(_stageUnlockPanelView);
            _stageUnlockPanelView.Initialize(_stageUnlockPanelPresenter);
            _stageUnlockPanelPresenter.Initialize();
        }

        if (_hospitalUpgradeView != null)
        {
            _hospitalUpgradePresenter = new UI_HospitalUpgradePresenter(_hospitalUpgradeView);
            _hospitalUpgradeView.Initialize(_hospitalUpgradePresenter);
            _hospitalUpgradePresenter.Initialize();
        }

        if (_customizingUI == null) return;

        if (_customizingViewModel == null)
        {
            var manager = CustomizingManager.Instance;
            if (manager == null)
            {
                Debug.LogError("[WaitingRoom] CustomizingManager가 없습니다.");
                return;
            }
            _customizingViewModel = new CustomizingUIViewModel(manager);
            _customizingUI.Initialize(_customizingViewModel);
        }
    }

    private void OnDestroy()
    {
        _presenter?.Dispose();
        _stageUnlockPanelPresenter?.Dispose();
        _hospitalUpgradePresenter?.Dispose();
    }
}
