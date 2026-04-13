using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PlayerRoleView : MonoBehaviourPunCallbacks
{
    [Header("인디케이터")]
    [SerializeField] private Renderer _downIndicatorRenderer;
    [SerializeField] private GameObject _upIndicator;

    private Material _originalMaterial;
    private bool _isInitialized;

    private void Start()
    {
        Initialize();
        SubscribeToManager();

        // 자기 자신만 _upIndicator 활성화
        if (_upIndicator != null)
        {
            _upIndicator.SetActive(photonView.IsMine);
        }

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

        if (_downIndicatorRenderer != null)
        {
            _originalMaterial = _downIndicatorRenderer.sharedMaterial;
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
        // Custom Properties에서 직접 조회
        // PlayerRoleView가 늦게 생성되어도 동작
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

        Debug.Log($"[PlayerRoleView] 역할 적용 - {photonView.Owner.NickName}: {role}");
    }

    public void ResetToDefault()
    {
        var manager = SelectRoleManager.Instance;
        var profile = manager?.VisualProfile;

        if (profile != null)
        {
            ApplyIndicatorMaterial(profile.DefaultMaterial);
        }
        else
        {
            ApplyIndicatorMaterial(_originalMaterial);
        }
    }

    private void ApplyIndicatorMaterial(Material material)
    {
        if (_downIndicatorRenderer == null || material == null) return;

        var materials = _downIndicatorRenderer.materials;
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i] = material;
        }
        _downIndicatorRenderer.materials = materials;
    }
}
