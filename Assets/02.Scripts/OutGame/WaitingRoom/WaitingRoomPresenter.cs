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
        if (isReady)
        {
            SetReady();
        }
        else
        {
            SetUnready();
        }
    }

    public void SetReady()
    {
        PlayerProperty.SetReadyState(true);
    }

    public void SetUnready()
    {
        PlayerProperty.SetReadyState(false);
    }
}
