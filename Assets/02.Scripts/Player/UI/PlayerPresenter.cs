using Photon.Realtime;

public class PlayerPresenter
{
    private readonly PlayerView _view;

    private readonly Player _owner;

    public PlayerPresenter(PlayerView view, Player owner)
    {
        _view = view;
        _owner = owner;
        PhotonServerManager.Instance.OnNicknameChanged += SetNickname;
        PhotonServerManager.Instance.OnReadyStateChanged += ReadyStateChange;
        PhotonServerManager.Instance.OnMasterClientChanged += MasterClientChanged;
    }

    private void MasterClientChanged()
    {
        if (_owner.IsMasterClient)
        {
            _view.SetMasterNickname();
            return;
        }

        if (PlayerProperty.GetReadyState(_owner))
        {
            if (_owner.IsLocal)
            {
                PlayerProperty.SetReadyState(false);
            }
        }
        else
        {
            ReadyStateChange(_owner, false);
        }
    }

    public void ReadyStateChange(Player targetPlayer, bool isReady)
    {
        if (targetPlayer.ActorNumber != _owner.ActorNumber || _owner.IsMasterClient) return;

        _view.SetReadyState(isReady);
    }

    public void SetNickname(Player targetPlayer, string name)
    {
        if (targetPlayer.ActorNumber != _owner.ActorNumber) return;

        _view.SetNickname(name);
    }

    public void Initialize()
    {
        _view.SetNickname(PlayerProperty.GetNickname(_owner));

        if (_owner.IsMasterClient)
        {
            _view.SetMasterNickname();
        }
        else
        {
            ReadyStateChange(_owner, PlayerProperty.GetReadyState(_owner));
        }
    }

    public void OnDestroy()
    {
        PhotonServerManager.Instance.OnNicknameChanged -= SetNickname;
        PhotonServerManager.Instance.OnReadyStateChanged -= ReadyStateChange;
        PhotonServerManager.Instance.OnMasterClientChanged -= MasterClientChanged;
    }
}
