using Photon.Pun;
using UnityEngine;

public class WaitingRoomBootStrapper : MonoBehaviour
{
    [SerializeField] private WaitingRoomView _waitingRoomView;

    private WaitingRoomPresenter  _waitingRoomPresenter;

    private void Start()
    {
        _waitingRoomPresenter = new WaitingRoomPresenter(_waitingRoomView);
        _waitingRoomView.Initialized(_waitingRoomPresenter);
    }

}
