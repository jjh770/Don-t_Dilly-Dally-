using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CustomizingCharacterView))]
public class CustomizingCharacterController : MonoBehaviourPunCallbacks
{
    private CustomizingCharacterView _view;
    private CustomizingCharacterViewModel _viewModel;
    private ICustomizingAssetLoader _assetLoader;
    private bool _hasAppliedLocalAppearance;     // 내 캐릭터의 외형을 적용했는지
    private bool _hasAppliedRemoteAppearance;    // 다른 플레이어의 외형 적용을 끝냈는지


    public bool IsLocalPlayer => photonView != null && photonView.IsMine;

    private void Awake()
    {
        _view = GetComponent<CustomizingCharacterView>();
        _assetLoader = new AddressableAssetLoader(); // 아이템을 Adderessables로 불러오기
        _view.Initialize(_assetLoader);
    }

    public override void OnDisable()
    {
        base.OnDisable();

        if (_viewModel != null)
        {
            UnsubscribeFromViewModel();
            _viewModel = null;
        }

        if (_assetLoader != null)
        {
            (_assetLoader as IDisposable)?.Dispose();
            _assetLoader = null;
        }
    }

    public void Initialize(CustomizingCharacterViewModel viewModel)
    {
        if (_viewModel != null) return;

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

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
    // 1. ViewModel을 구독하고
    // 2. 아이템을 장착하고
    // 3. 네트워크에 동기화하고
    // 4. 커스터마이징 카메라에 세팅
    private void InitializeLocal()
    {
        if (_viewModel == null) return;

        SubscribeToViewModel();
        ApplyFromViewModel();
        SyncToNetwork();
        SetCustomizingCameraTarget(transform);
    }

    // 원격 플레이어는 처음에
    // 1. ViewModel의 준비 완료 이벤트를 구독하고
    // 2. 네트워크에서 받은 커스터마이징을 적용
    // ㄴ ViewModel이 아직 준비되지 않았다면 준비 완료 후 다시 적용
    private void InitializeRemote()
    {
        if (_viewModel != null)
        {
            _viewModel.OnLoaded += HandleRemoteViewModelReady;
        }

        TryApplyRemoteCustomizing();
    }

    // ===== 로컬 플레이어 =====
    private void SubscribeToViewModel()
    {
        if (_viewModel == null) return;

        _viewModel.OnLoaded += HandleLoaded;
        _viewModel.OnItemChanged += HandleItemChanged;
        _viewModel.OnSaved += HandleSaved;
    }

    private void UnsubscribeFromViewModel()
    {
        if (_viewModel == null) return;

        _viewModel.OnLoaded -= HandleLoaded;
        _viewModel.OnItemChanged -= HandleItemChanged;
        _viewModel.OnSaved -= HandleSaved;
        _viewModel.OnLoaded -= HandleRemoteViewModelReady;
    }

    private void ApplyFromViewModel()
    {
        if (_viewModel == null || !_viewModel.IsInitialized) return;

        // 기본 장착
        ApplyAllBaseEquipment();

        // 커스터마이징
        _view.ApplyAll(type => _viewModel.GetEquipped(type));
    }

    private void ApplyAllBaseEquipment()
    {
        if (_viewModel == null || !_viewModel.IsInitialized) return;

        foreach (var (type, item) in _viewModel.GetAllBaseEquipmentItems())
        {
            _view.ApplyBaseEquipment(type, item);
        }
    }

    public void SyncToNetwork()
    {
        if (IsLocalPlayer == false) return;
        if (_viewModel == null || _viewModel.IsInitialized == false) return;

        var itemIds = _viewModel.GetEquippedItemIds();
        // Photon Custom Properties에 저장
        // ㄴ 내 캐릭터 외형을 다른 사람에게 공유
        CustomizingProperties.SetLocalPlayerCustomizing(itemIds);
    }

    private void SetCustomizingCameraTarget(Transform transform)
    {
        CharacterPreviewCamera.SetLocalPlayerTarget(transform);
        _hasAppliedLocalAppearance = true;
    }

    // 나 말고 다른 캐릭터의 커스텀 프로퍼티가 바뀌면
    // 커스터마이징 업데이트
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (photonView.Owner != targetPlayer) return;
        if (IsLocalPlayer) return;

        if (_hasAppliedRemoteAppearance == false)
        {
            TryApplyRemoteCustomizing();
        }

        // 변경된 속성만 업데이트
        if (_hasAppliedRemoteAppearance == true && CustomizingProperties.TryGetFromChangedProps(changedProps, out var items) == true)
        {
            ApplyFromItemIds(items);
        }
    }

    private void HandleLoaded()
    {
        if (IsLocalPlayer == false) return;
        ApplyFromViewModel();
    }

    private void HandleItemChanged(CustomizingType type, CustomizingItemSO item)
    {
        if (IsLocalPlayer == false) return;
        _view.ApplyItem(type, item);
    }

    private void HandleSaved()
    {
        if (IsLocalPlayer == false) return;
        SyncToNetwork();
    }

    // ===== 원격 플레이어 =====
    private void HandleRemoteViewModelReady()
    {
        if (_viewModel != null)
        {
            _viewModel.OnLoaded -= HandleRemoteViewModelReady;
        }

        TryApplyRemoteCustomizing();
    }

    private void TryApplyRemoteCustomizing()
    {
        if (_hasAppliedRemoteAppearance) return;
        if (_viewModel == null || !_viewModel.IsInitialized) return;

        var items = CustomizingProperties.GetPlayerCustomizing(photonView.Owner);
        if (items == null || items.Count == 0) return;

        ApplyFromItemIds(items);
        _hasAppliedRemoteAppearance = true;
    }

    private void ApplyFromItemIds(Dictionary<CustomizingType, string> itemIds)
    {
        if (_viewModel == null || !_viewModel.IsInitialized)
        {
            Debug.LogWarning("[CustomizingCharacterController] ViewModel이 준비되지 않음");
            return;
        }

        // 기본 장착 먼저
        ApplyAllBaseEquipment();

        // 커스터마이징 적용
        foreach (var kvp in itemIds)
        {
            var item = _viewModel.GetItemById(kvp.Value);
            _view.ApplyItem(kvp.Key, item);
        }
    }
}
