
public class RoomPresenter
{
    private RoomView _view;

    public RoomPresenter(RoomView view)
    {
        _view = view;
        PhotonServerManager.Instance.OnFailedToJoinRoom += OnFailedToJoinRoom;
    }
    public void EnterRoom(string code)
    {
        PhotonServerManager.Instance.TryJoinRoom(code);
    }

    public void CreateRoom()
    {
        PhotonServerManager.Instance.CreateNewRoom();
    }

    public void SetNickName(string name)
    {
        PhotonServerManager.Instance.SetNickname(name);
    }

    public void OnFailedToJoinRoom(string message)
    {
        _view?.ShowErrorMessage(message);
    }

    public void Dispose()
    {
        PhotonServerManager.Instance.OnFailedToJoinRoom -= OnFailedToJoinRoom;
    }
}
