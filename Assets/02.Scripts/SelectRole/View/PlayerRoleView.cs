using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PlayerRoleView : MonoBehaviourPunCallbacks
{
    [Header("인디케이터")]
    [SerializeField] private Renderer _downIndicatorRenderer;
    [SerializeField] private GameObject _upIndicator;

    private Renderer[] _upIndicatorRenderers;

    private Material _originalMaterial;
    private bool _isInitialized;

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

        if (_downIndicatorRenderer != null)
        {
            _originalMaterial = _downIndicatorRenderer.sharedMaterial;
        }

        if (_upIndicator != null)
        {
            _upIndicatorRenderers = _upIndicator.GetComponentsInChildren<Renderer>();
            _upIndicator.SetActive(false);
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

        // 자기 자신만 _upIndicator 활성화
        if (_upIndicator != null && photonView.IsMine)
        {
            _upIndicator.SetActive(true);
        }

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

        if (_upIndicator != null)
        {
            _upIndicator.SetActive(false);
        }
    }

    private void ApplyIndicatorMaterial(Material material)
    {
        if (material == null) return;

        ApplyMaterialToRenderer(_downIndicatorRenderer, material);

        if (_upIndicatorRenderers != null)
        {
            foreach (var renderer in _upIndicatorRenderers)
            {
                ApplyMaterialToRenderer(renderer, material);
            }
        }
    }

    private void ApplyMaterialToRenderer(Renderer renderer, Material material)
    {
        if (renderer == null) return;

        int count = renderer.sharedMaterials.Length;
        var materials = new Material[count];
        for (int i = 0; i < count; i++)
        {
            materials[i] = material;
        }
        renderer.materials = materials;
    }
}
