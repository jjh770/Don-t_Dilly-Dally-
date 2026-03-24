using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PlayerBootStrapper : MonoBehaviour
{
    PlayerModel _playerModel;
    PlayerView _playerView;
    PlayerPresenter _playerPresenter;

    private void Start()
    {
        Player owner = GetComponent<PhotonView>().Owner;
        _playerModel = new PlayerModel(
            PlayerProperty.GetNickname(owner),
            PlayerProperty.GetReadyState(owner),
            owner.IsMasterClient
        );
        _playerView = GetComponentInChildren<PlayerView>();

        _playerPresenter = new PlayerPresenter(_playerModel, _playerView, owner);
        _playerView.Initialize(_playerPresenter);
        _playerPresenter.Initialize();
    }

    private void OnDestroy()
    {
        _playerPresenter?.OnDestroy();
    }
}
