using Photon.Pun;
using TMPro;
using UnityEngine;

public class PlayerView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _nicknameText;
    [SerializeField] private Color _readyColor = Color.green;
    [SerializeField] private Color _notReadyColor = Color.red;
    [SerializeField] private Color _masterColor = Color.black;

    private PlayerPresenter _presenter;

    private Camera _camera;

    public void Start()
    {
        _camera = Camera.main;
    }
    public void Initialize(PlayerPresenter presenter)
    {
        _presenter = presenter;
    }

    private void LateUpdate()
    {
        _nicknameText.transform.rotation = Quaternion.LookRotation(
            _camera.transform.forward,
            _camera.transform.up);
    }

    public void SetNickname(string name)
    {
        _nicknameText.text = name;
    }

    public void SetReadyState(bool isReady)
    {
        _nicknameText.color = isReady ? _readyColor : _notReadyColor;
    }

    public void SetMasterNickname()
    {
        _nicknameText.color = _masterColor;
    }
}
