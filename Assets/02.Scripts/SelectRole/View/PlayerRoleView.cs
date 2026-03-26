using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class PlayerRoleView : MonoBehaviourPunCallbacks
{
    [Header("인디케이터")]
    [SerializeField] private Renderer _indicatorRenderer;

    [Header("닉네임 (선택)")]
    [SerializeField] private TextMeshPro _nicknameText;
    [SerializeField] private TextMeshProUGUI _nicknameTextUI;

    private RoleType _currentRole = RoleType.None;
    private Material _originalMaterial;
    private Color _originalNicknameColor;
    private bool _isInitialized;

    public RoleType CurrentRole => _currentRole;

    private void Start()
    {
        Initialize();
        SubscribeToManager();

        // 이미 역할이 배정되었다면 적용
        TryApplyExistingRole();
    }

    public override void OnDisable()
    {
        base.OnDisable();
        UnsubscribeFromManager();
    }

    private void Initialize()
    {
        if (_isInitialized) return;

        // 원본 저장
        if (_indicatorRenderer != null)
        {
            _originalMaterial = _indicatorRenderer.sharedMaterial;
        }

        if (_nicknameText != null)
        {
            _originalNicknameColor = _nicknameText.color;
        }
        else if (_nicknameTextUI != null)
        {
            _originalNicknameColor = _nicknameTextUI.color;
        }

        _isInitialized = true;
    }

    private void SubscribeToManager()
    {
        var manager = SelectRoleManager.Instance;
        if (manager != null)
        {
            manager.OnPlayerRoleChanged += HandlePlayerRoleChanged;
            manager.OnRolesCleared += HandleRolesCleared;
        }
    }

    private void UnsubscribeFromManager()
    {
        var manager = SelectRoleManager.Instance;
        if (manager != null)
        {
            manager.OnPlayerRoleChanged -= HandlePlayerRoleChanged;
            manager.OnRolesCleared -= HandleRolesCleared;
        }
    }

    private void TryApplyExistingRole()
    {
        // Custom Properties에서 직접 조회 (PlayerRoleView가 늦게 생성되어도 동작)
        var role = RoleProperties.GetPlayerRole(photonView.Owner);
        if (role != RoleType.None)
        {
            ApplyRole(role);
        }
    }

    private void HandlePlayerRoleChanged(int actorNumber, RoleType role)
    {
        if (photonView.Owner.ActorNumber != actorNumber) return;
        ApplyRole(role);
    }

    private void HandleRolesCleared()
    {
        ResetToDefault();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (photonView.Owner != targetPlayer) return;

        if (RoleProperties.TryGetFromChangedProps(changedProps, out var role))
        {
            ApplyRole(role);
        }
    }

    public void ApplyRole(RoleType role)
    {
        if (!_isInitialized) Initialize();

        _currentRole = role;

        var manager = SelectRoleManager.Instance;
        var profile = manager?.VisualProfile;

        if (profile == null)
        {
            Debug.LogWarning("[PlayerRoleView] RoleVisualProfile이 없음");
            return;
        }

        if (role == RoleType.None)
        {
            ResetToDefault();
            return;
        }

        // 인디케이터 머티리얼 적용
        ApplyIndicatorMaterial(profile.GetMaterial(role));

        // 닉네임 색상 적용
        ApplyNicknameColor(profile.GetNicknameColor(role));

        Debug.Log($"[PlayerRoleView] 역할 적용 - {photonView.Owner.NickName}: {role}");
    }

    public void ResetToDefault()
    {
        _currentRole = RoleType.None;

        var manager = SelectRoleManager.Instance;
        var profile = manager?.VisualProfile;

        if (profile != null)
        {
            ApplyIndicatorMaterial(profile.DefaultMaterial);
            ApplyNicknameColor(profile.DefaultNicknameColor);
        }
        else
        {
            // Profile이 없으면 원본으로 복구
            ApplyIndicatorMaterial(_originalMaterial);
            ApplyNicknameColor(_originalNicknameColor);
        }

        Debug.Log($"[PlayerRoleView] 기본 상태 복구 - {photonView.Owner?.NickName}");
    }

    private void ApplyIndicatorMaterial(Material material)
    {
        if (_indicatorRenderer == null || material == null) return;

        // 모든 머티리얼 슬롯을 동일한 머티리얼로 변경
        var materials = _indicatorRenderer.materials;
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i] = material;
        }
        _indicatorRenderer.materials = materials;
    }

    private void ApplyNicknameColor(Color color)
    {
        if (_nicknameText != null)
        {
            _nicknameText.color = color;
        }

        if (_nicknameTextUI != null)
        {
            _nicknameTextUI.color = color;
        }
    }

    public void SetNicknameText(TextMeshPro text)
    {
        _nicknameText = text;
        if (_currentRole != RoleType.None)
        {
            var profile = SelectRoleManager.Instance?.VisualProfile;
            if (profile != null)
            {
                ApplyNicknameColor(profile.GetNicknameColor(_currentRole));
            }
        }
    }

    public void SetNicknameTextUI(TextMeshProUGUI text)
    {
        _nicknameTextUI = text;
        if (_currentRole != RoleType.None)
        {
            var profile = SelectRoleManager.Instance?.VisualProfile;
            if (profile != null)
            {
                ApplyNicknameColor(profile.GetNicknameColor(_currentRole));
            }
        }
    }
}
