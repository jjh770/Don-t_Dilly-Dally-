
using Photon.Realtime;

public class PlayerPresenter
{
    private readonly PlayerModel _model;
    private readonly PlayerView _view;

    private readonly Player _owner;

    public PlayerPresenter(PlayerModel model, PlayerView view, Player owner)
    {
        _view = view;
        _model = model;
        PhotonServerManager.Instance.OnNicknameChanged += SetNickname;
        PhotonServerManager.Instance.OnReadyStateChanged += ReadyStateChange;
        _owner = owner;
    }

    public void ReadyStateChange(Player targetPlayer, bool isReady)
    {
        if (targetPlayer.ActorNumber != _owner.ActorNumber) return;

        _model.SetReadyState(isReady);
        _view.SetReadyState(_model.IsReady);
    }

    
    public void SetNickname(Player targetPlayer, string name)
    {
        if (targetPlayer.ActorNumber != _owner.ActorNumber) return;

        _model.SetNickname(name);
        _view.SetNickname(_model.Nickname);
    }

    public void Initialize()
    {
        _view.SetNickname(_model.Nickname);
        _view.SetReadyState(_model.IsReady);
    }

    public void OnDestroy()
    {
        PhotonServerManager.Instance.OnNicknameChanged -= SetNickname;
        PhotonServerManager.Instance.OnReadyStateChanged -= ReadyStateChange;
    }
}
