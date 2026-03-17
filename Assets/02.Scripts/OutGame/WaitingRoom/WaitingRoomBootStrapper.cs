using Photon.Pun;
using UnityEngine;

public class WaitingRoomBootStrapper : MonoBehaviour
{
    [SerializeField] private WaitingRoomView _view;
    private WaitingRoomModel _model;
    private WaitingRoomPresenter  _presenter;

    private void Start()
    {
        _model = new WaitingRoomModel(PhotonNetwork.IsMasterClient, PlayerProperty.GetReadyState(PhotonNetwork.LocalPlayer));
        _presenter = new WaitingRoomPresenter(_view, _model);
        _presenter.Initialize();
        _view.Initialized(_presenter);
    }
}
