using UnityEngine;
using UnityEngine.UI;

public class Lobby : MonoBehaviour
{
    [Header("프리뷰 캐릭터")]
    [SerializeField] private GameObject _previewCharacterPrefab;

    [Header("UI")]
    [SerializeField] private UI_Customizing _customizingUI;
    [SerializeField] private Button _customizingButton;

    [Header("전환 효과")]
    [SerializeField] private LobbyCustomizingTransition _transition;

    private GameObject _previewCharacter;
    private CustomizingUIViewModel _viewModel;
    private LobbyPreviewAnimator _previewAnimator;

    private void Start()
    {
        SpawnPreviewCharacter();
        InitializeCustomizingUI();
        SetupButtons();
    }

    private void OnDestroy()
    {
        CleanupButtons();

        if (_customizingUI != null)
        {
            _customizingUI.OnClosed -= OnCustomizingClosed;
            _customizingUI.OnSaved -= OnCustomizingSaved;
        }

        if (_previewCharacter != null)
        {
            Destroy(_previewCharacter);
        }

        _viewModel?.Dispose();
    }

    private void SpawnPreviewCharacter()
    {
        if (_previewCharacterPrefab == null) return;

        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;

        _previewCharacter = Instantiate(_previewCharacterPrefab, position, rotation);

        var photonController = _previewCharacter.GetComponent<CustomizingCharacterController>();
        if (photonController != null)
        {
            Destroy(photonController);
        }
        _previewAnimator = _previewCharacter.GetComponentInChildren<LobbyPreviewAnimator>();
    }

    private void InitializeCustomizingUI()
    {
        if (_customizingUI == null) return;

        var manager = CustomizingManager.Instance;
        if (manager == null)
        {
            Debug.LogError("[Lobby] CustomizingManager가 없습니다.");
            return;
        }

        // ViewModel 생성 및 주입
        _viewModel = new CustomizingUIViewModel(manager);
        _customizingUI.Initialize(_viewModel);

        _customizingUI.OnClosed += OnCustomizingClosed;
        _customizingUI.OnSaved += OnCustomizingSaved;
    }

    private void SetupButtons()
    {
        if (_customizingButton != null)
        {
            _customizingButton.onClick.AddListener(OnCustomizingButtonClicked);
        }
    }

    private void CleanupButtons()
    {
        if (_customizingButton != null)
        {
            _customizingButton.onClick.RemoveListener(OnCustomizingButtonClicked);
        }
    }

    private void OnCustomizingButtonClicked()
    {
        if (_transition != null)
        {
            _transition.TransitionToCustomizing();
        }
        else if (_customizingUI != null)
        {
            _customizingUI.gameObject.SetActive(true);
        }
    }

    public void OnCustomizingClosed()
    {
        if (_transition != null)
        {
            _transition.TransitionToLobby();
        }
        else if (_customizingUI != null)
        {
            _customizingUI.gameObject.SetActive(false);
        }
    }

    private void OnCustomizingSaved()
    {
        _previewAnimator?.PlayCustomizingSave();
    }
}
