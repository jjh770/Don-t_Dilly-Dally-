
using System;
using Photon.Pun;
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
        }
        else
        {
            if (PhotonNetwork.LocalPlayer != _owner) return;
            PlayerProperty.SetReadyState(false);
        }
    }

    public void ReadyStateChange(Player targetPlayer, bool isReady)
    {
        if (targetPlayer.ActorNumber != _owner.ActorNumber || _owner.IsMasterClient) return;

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

        if (_owner.IsMasterClient)
        {
            _view.SetMasterNickname();
        }
        else
        {
            ReadyStateChange(_owner, _model.IsReady);
        }
    }

    public void OnDestroy()
    {
        PhotonServerManager.Instance.OnNicknameChanged -= SetNickname;
        PhotonServerManager.Instance.OnReadyStateChanged -= ReadyStateChange;
    }
}
