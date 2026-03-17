using UnityEngine;

public class WaitingRoomBootStrapper : MonoBehaviour
{
    [SerializeField] private WaitingRoomClickManager _clickManager;

    [SerializeField] private WaitingRoomView _defaultView;

    [SerializeField] private PlayerPopupView _popupView;

    private WaitingRoomModel _model;
    private WaitingRoomPresenter  _presenter;

    private void Start()
    {
        _model = new WaitingRoomModel(PhotonServerManager.Instance.IsMasterClient, PhotonServerManager.Instance.GetLocalPlayerReadyState());

        _presenter = new WaitingRoomPresenter(_defaultView, _popupView, _model);
        _presenter.Initialize();

        _defaultView.Initialized(_presenter);
        _popupView.Initialized(_presenter);

        _clickManager.Initialized(_presenter);
    }
}
