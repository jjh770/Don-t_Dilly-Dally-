using Photon.Pun;
using UnityEngine;

public class WaitingRoomBootStrapper : MonoBehaviourPunCallbacks
{
    [SerializeField] private WaitingRoomClickManager _clickManager;

    [SerializeField] private WaitingRoomView _defaultView;

    [SerializeField] private ContextMenuView _popupView;

    private WaitingRoomModel _model;
    private WaitingRoomPresenter _presenter;

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
        _model = new WaitingRoomModel(PhotonServerManager.Instance.IsMasterClient, PhotonServerManager.Instance.GetLocalPlayerReadyState());

        _presenter = new WaitingRoomPresenter(_defaultView, _popupView, _model);
        _presenter.Initialize();

        _defaultView.Initialized(_presenter);
        _popupView.SetPresenter(_presenter);

        _clickManager.Initialized(_presenter);
    }
}
