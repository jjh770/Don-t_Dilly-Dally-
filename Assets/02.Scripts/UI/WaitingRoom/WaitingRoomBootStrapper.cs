using Photon.Pun;
using UnityEngine;

public class WaitingRoomBootStrapper : MonoBehaviourPunCallbacks
{
    [SerializeField] private WaitingRoomClickManager _clickManager;

    [SerializeField] private WaitingRoomView _defaultView;

    [SerializeField] private ContextMenuView _popupView;
    [SerializeField] private UI_StagePanelView _stageUnlockPanelView;

    private WaitingRoomModel _model;
    private WaitingRoomPresenter _presenter;
    private UI_StagePanelPresenter _stageUnlockPanelPresenter;

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
    }

    private void OnDestroy()
    {
        _presenter?.Dispose();
        _stageUnlockPanelPresenter?.Dispose();
    }
}
