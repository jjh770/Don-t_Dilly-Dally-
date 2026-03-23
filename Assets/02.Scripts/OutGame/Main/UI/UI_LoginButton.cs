using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;


public class UI_LoginButton : MonoBehaviour
{
    [SerializeField] private GoogleAuthManager _authManager;
    [SerializeField] private GameObject _loadingEffect;
    [SerializeField] private Button _loginButton;
    [SerializeField] private Button _cancelButton;


    private void OnEnable()
    {
        _loginButton.onClick.AddListener(OnLogInButtonClick);
        _cancelButton.onClick.AddListener(OnCancelButtonButtonClick);
        ShowLoginUI();
        StopRoading();
    }

    private void OnCancelButtonButtonClick()
    {
        _authManager.CancelLogin();
    }

    private void OnLogInButtonClick()
    {
        Login().Forget();
    }

    private async UniTask Login()
    {
        StartRoading();
        ShowCancelUI();
        await _authManager.StartGoogleLogin();
        ShowLoginUI();
        StopRoading();
    }

    private void StartRoading()
    {
        _loadingEffect.SetActive(true);
    }
    private void StopRoading()
    {
        _loadingEffect.SetActive(false);
    }

    public void ShowLoginUI()
    {
        _loginButton.gameObject.SetActive(true);
        _cancelButton.gameObject.SetActive(false);
    }

    public void ShowCancelUI()
    {
        _loginButton.gameObject.SetActive(false);
        _cancelButton.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        _loginButton.onClick.RemoveListener(OnLogInButtonClick);
        _cancelButton.onClick.RemoveListener(OnCancelButtonButtonClick);
    }
}
