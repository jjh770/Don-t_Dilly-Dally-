using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PlayerBootStrapper : MonoBehaviour
{
    PlayerView _playerView;
    PlayerPresenter _playerPresenter;

    private void Start()
    {
        Player owner = GetComponent<PhotonView>().Owner;
        _playerView = GetComponentInChildren<PlayerView>();

        _playerPresenter = new PlayerPresenter(_playerView, owner);
        _playerView.Initialize(_playerPresenter);
        _playerPresenter.Initialize();
    }

    private void OnDestroy()
    {
        _playerPresenter?.OnDestroy();
    }
}
