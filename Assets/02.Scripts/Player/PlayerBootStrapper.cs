using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class PlayerBootStrapper : MonoBehaviour
{
    private void Start()
    {
        Player owner = GetComponent<PhotonView>().Owner;
        PlayerModel model = new PlayerModel(
            PlayerProperty.GetNickname(owner),
            PlayerProperty.GetReadyState(owner)
        );
        PlayerView _playerView = GetComponentInChildren<PlayerView>();

        PlayerPresenter presenter = new PlayerPresenter(model, _playerView, owner);
        _playerView.Initialize(presenter);
        presenter.Initialize();
    }
}
