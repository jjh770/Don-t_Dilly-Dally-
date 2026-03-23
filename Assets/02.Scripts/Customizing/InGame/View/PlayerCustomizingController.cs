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

    private void InitializeLocal()
    {
        SubscribeToManager();
        ApplyFromManager();
        SyncToNetwork();

        CharacterPreviewCamera.SetLocalPlayerTarget(transform);

        Debug.Log("[PlayerCustomizingController] 로컬 플레이어 초기화 완료");
    }

    private void InitializeRemote()
    {
        ApplyFromNetwork(photonView.Owner);

        Debug.Log($"[PlayerCustomizingController] 원격 플레이어 초기화 완료: {photonView.Owner.NickName}");
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (photonView.Owner != targetPlayer) return;
        if (IsLocalPlayer) return;

        if (CustomizingProperties.TryGetFromChangedProps(changedProps, out var items))
        {
            ApplyFromItemIds(items);
            Debug.Log($"[PlayerCustomizingController] 원격 업데이트: {targetPlayer.NickName}");
        }
    }

    public void SyncToNetwork()
    {
        if (!IsLocalPlayer) return;

        var manager = CustomizingManager.Instance;
        if (manager == null || !manager.IsInitialized) return;

        var itemIds = manager.GetEquippedItemIds();
        CustomizingProperties.SetLocalPlayerCustomizing(itemIds);
    }

    private void ApplyFromNetwork(Player player)
    {
        var items = CustomizingProperties.GetPlayerCustomizing(player);
        ApplyFromItemIds(items);
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
}
