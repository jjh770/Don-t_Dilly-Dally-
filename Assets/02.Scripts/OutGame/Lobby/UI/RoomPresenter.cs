
public class RoomPresenter
{
    private RoomView _view;

    public RoomPresenter(RoomView view)
    {
        _view = view;
        PhotonServerManager.Instance.OnFailedToJoinRoom += OnFailedToJoinRoom;
        PlayerDataManager.Instance.OnDataManagerReady += OnDataManagerSet;
        SetDropdown();
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

    public void SelectMyHospital(string code)
    {
        _view.SetCodeInputField(code);
    }

    public void OnFailedToJoinRoom(string message)
    {
        _view?.ShowErrorMessage(message);
    }

    public void OnDataManagerSet()
    {
        SetDropdown();
    }

    public void SetDropdown()
    {
        if (!PlayerDataManager.Instance.IsReady) return;
        string[] hospitals = PlayerDataManager.Instance.GetHospitalCode();
        _view.SetDropdown(hospitals);
    }

    public void Dispose()
    {
        PhotonServerManager.Instance.OnFailedToJoinRoom -= OnFailedToJoinRoom;
    }
}
