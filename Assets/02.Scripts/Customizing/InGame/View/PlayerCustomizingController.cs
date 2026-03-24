using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerCustomizingView))]
public class PlayerCustomizingController : MonoBehaviourPunCallbacks
{
    private PlayerCustomizingView _view;
    private bool _isInitialized;
    private bool _isCustomizingApplied;

    public bool IsLocalPlayer => photonView != null && photonView.IsMine;

    private void Awake()
    {
        _view = GetComponent<PlayerCustomizingView>();
    }

    private void Start()
    {
        Initialize();
    }

    public override void OnDisable()
    {
        base.OnDisable();

        if (IsLocalPlayer)
        {
            UnsubscribeFromManager();
        }
        else
        {
            var manager = CustomizingManager.Instance;
            if (manager != null)
            {
                manager.OnLoaded -= HandleRemoteManagerReady;
            }
        }
    }

    private void Initialize()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        if (IsLocalPlayer)
        {
            InitializeLocal();
        }
        else
        {
            InitializeRemote();
        }
    }

    // 로컬 플레이어는 처음에
    // 1. 매니저를 구독하고
    // 2. 아이템을 장착하고
    // 3. 네트워크에 동기화하고
    // 4. 커스터마이징 카메라에 세팅
    private void InitializeLocal()
    {
        SubscribeToManager();
        ApplyFromManager();
        SyncToNetwork();
        SetCustomizingCameraTarget(transform);
    }

    // 원격 플레이어는 처음에
    // 1. 매니저의 준비 완료 이벤트를 구독하고
    // 2. 네트워크에서 받은 커스터마이징을 적용
    // ㄴ 매니저가 아직 준비되지 않았다면 준비 완료 후 다시 적용
    private void InitializeRemote()
    {
        var manager = CustomizingManager.Instance;
        if (manager != null)
        {
            manager.OnLoaded += HandleRemoteManagerReady;
        }

        TryApplyRemoteCustomizing();
    }

    // ===== 로컬 플레이어 =====
    private void SubscribeToManager()
    {
        var manager = CustomizingManager.Instance;
        if (manager == null) return;

        manager.OnLoaded += HandleLoaded;
        manager.OnItemChanged += HandleItemChanged;
        manager.OnSaved += HandleSaved;
    }

    private void UnsubscribeFromManager()
    {
        var manager = CustomizingManager.Instance;
        if (manager == null) return;

        manager.OnLoaded -= HandleLoaded;
        manager.OnItemChanged -= HandleItemChanged;
        manager.OnSaved -= HandleSaved;
    }

    private void ApplyFromManager()
    {
        var manager = CustomizingManager.Instance;
        if (manager == null || !manager.IsInitialized) return;

        // 기본 장착
        ApplyAllBaseEquipment();

        // 커스터마이징
        _view.ApplyAll(type => manager.GetEquipped(type));
    }

    private void ApplyAllBaseEquipment()
    {
        var manager = CustomizingManager.Instance;
        if (manager == null || !manager.IsInitialized) return;

        foreach (var (type, item) in manager.GetAllBaseEquipmentItems())
        {
            _view.ApplyBaseEquipment(type, item);
        }
    }

    public void SyncToNetwork()
    {
        if (!IsLocalPlayer) return;

        var manager = CustomizingManager.Instance;
        if (manager == null || !manager.IsInitialized) return;

        var itemIds = manager.GetEquippedItemIds();
        // Photon Custom Properties에 저장
        // ㄴ 내 캐릭터 외형을 다른 사람에게 공유
        CustomizingProperties.SetLocalPlayerCustomizing(itemIds);
    }

    private void SetCustomizingCameraTarget(Transform transform)
    {
        CharacterPreviewCamera.SetLocalPlayerTarget(transform);
        _isCustomizingApplied = true;
    }

    // 나 말고 다른 캐릭터의 커스텀 프로퍼티가 바뀌면
    // 커스터마이징 업데이트
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (photonView.Owner != targetPlayer) return;
        if (IsLocalPlayer) return;

        if (_isCustomizingApplied == false)
        {
            TryApplyRemoteCustomizing();
        }

        // 변경된 속성만 업데이트
        if (_isCustomizingApplied && CustomizingProperties.TryGetFromChangedProps(changedProps, out var items))
        {
            ApplyFromItemIds(items);
        }
    }
    private void HandleLoaded()
    {
        if (!IsLocalPlayer) return;
        ApplyFromManager();
    }

    private void HandleItemChanged(CustomizingType type, CustomizingItemSO item)
    {
        if (!IsLocalPlayer) return;
        _view.ApplyItem(type, item);
    }

    private void HandleSaved()
    {
        if (!IsLocalPlayer) return;
        SyncToNetwork();
    }

    // ===== 원격 플레이어 =====
    private void HandleRemoteManagerReady()
    {
        var manager = CustomizingManager.Instance;
        if (manager != null)
        {
            manager.OnLoaded -= HandleRemoteManagerReady;
        }

        TryApplyRemoteCustomizing();
    }

    private void TryApplyRemoteCustomizing()
    {
        if (_isCustomizingApplied) return;

        var manager = CustomizingManager.Instance;
        if (manager == null || !manager.IsInitialized) return;

        var items = CustomizingProperties.GetPlayerCustomizing(photonView.Owner);
        if (items == null || items.Count == 0) return;

        ApplyFromItemIds(items);
        _isCustomizingApplied = true;
    }

    private void ApplyFromItemIds(Dictionary<CustomizingType, string> itemIds)
    {
        var manager = CustomizingManager.Instance;
        if (manager == null || !manager.IsInitialized)
        {
            Debug.LogWarning("[PlayerCustomizingController] CustomizingManager가 준비되지 않음");
            return;
        }

        // 기본 장착 먼저
        ApplyAllBaseEquipment();

        // 커스터마이징 적용
        foreach (var kvp in itemIds)
        {
            var item = manager.GetItemById(kvp.Value);
            _view.ApplyItem(kvp.Key, item);
        }
    }
}
