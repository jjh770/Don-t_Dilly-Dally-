using UnityEngine;

public class WaitingRoomPresenter
{
    private readonly WaitingRoomView _view;

    public WaitingRoomPresenter(WaitingRoomView view)
    {
        _view = view;
    }

    public void ReadyStateChange(bool isReady)
    {
        PlayerProperty.SetReadyState(isReady);
    }
}
