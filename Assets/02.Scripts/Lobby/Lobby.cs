using UnityEngine;
using UnityEngine.UI;

public class Lobby : MonoBehaviour
{
    [Header("프리뷰 캐릭터")]
    [SerializeField] private GameObject _previewCharacterPrefab;
    [SerializeField] private Transform _spawnPoint;

    [Header("UI")]
    [SerializeField] private UI_Customizing _customizingUI;
    [SerializeField] private Button _customizingButton;

    [Header("전환 효과")]
    [SerializeField] private LobbyCustomizingTransition _transition;

    private GameObject _previewCharacter;
    private CustomizingViewModel _viewModel;

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

        Vector3 position = _spawnPoint != null ? _spawnPoint.position : Vector3.zero;
        Quaternion rotation = _spawnPoint != null ? _spawnPoint.rotation : Quaternion.identity;

        _previewCharacter = Instantiate(_previewCharacterPrefab, position, rotation);

        // 멀티플레이어용 컴포넌트 제거
        var photonController = _previewCharacter.GetComponent<PlayerCustomizingController>();
        if (photonController != null)
        {
            Destroy(photonController);
        }

        // 로컬 프리뷰용 컨트롤러 추가
        _previewCharacter.AddComponent<LobbyPreviewController>();
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
        _viewModel = new CustomizingViewModel(manager);
        _customizingUI.Initialize(_viewModel);

        // 닫기 이벤트 구독
        _customizingUI.OnClosed += OnCustomizingClosed;
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
            // 전환 효과 없으면 단순 활성화
            _customizingUI.gameObject.SetActive(true);
        }
    }

    // UI_Customizing에서 닫기 시 호출
    public void OnCustomizingClosed()
    {
        if (_transition != null)
        {
            _transition.TransitionToLobby();
        }
    }
}
