using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class UI_LoginButton : MonoBehaviour
{
    [SerializeField] private GoogleAuthManager _authManager;
    [SerializeField] private GameObject _loadingEffect;
    [SerializeField] private Button _loginButton;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private TextMeshProUGUI _loginButtonText;

    [SerializeField] private string _loginLabel = "Sign <size=70%>in with</size> Google";
    [SerializeField] private string _cancelLoginLabel = "<size=50%>웹 브라우저에서 로그인을 완료해주세요 ••• </size>";

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
        _loginButton.interactable = true;
        _cancelButton.gameObject.SetActive(false);
        _loginButtonText.text = _loginLabel;
    }

    public void ShowCancelUI()
    {
         _loginButton.interactable = false;
        _cancelButton.gameObject.SetActive(true);
        _loginButtonText.text = _cancelLoginLabel;
    }

    private void OnDisable()
    {
        _loginButton.onClick?.RemoveListener(OnLogInButtonClick);
        _cancelButton.onClick?.RemoveListener(OnCancelButtonButtonClick);
    }
}
