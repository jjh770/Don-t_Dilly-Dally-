using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Photon.Realtime;
using UnityEngine;

// 컷씬 캐릭터 슬롯에 할당된 플레이어의 커스터마이징을 적용하는 컴포넌트
// CustomizingCharacterController의 컷씬 전용 경량 버전
[ExecuteAlways]
[RequireComponent(typeof(CustomizingCharacterView))]
public class CutsceneCharacterSlot : MonoBehaviour
{
    [Header("에디터 미리보기")]
    [Tooltip("에디터에서 미리 볼 바디 프리팹 (예: Body_01). 플레이 시 자동으로 숨겨집니다.")]
    [SerializeField] private GameObject _previewPrefab;

#if UNITY_EDITOR
    private GameObject _previewInstance;
#endif

    private CustomizingCharacterView _view;
    private ICustomizingAssetLoader _assetLoader;
    private Player _assignedPlayer;

    public bool IsAssigned => _assignedPlayer != null;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        UnityEditor.EditorApplication.delayCall += RefreshPreview;
    }

    private void RefreshPreview()
    {
        if (this == null) return;
        if (Application.isPlaying) return;

        // 기존 미리보기 제거
        if (_previewInstance != null)
        {
            DestroyImmediate(_previewInstance);
            _previewInstance = null;
        }

        if (_previewPrefab == null) return;

        // 프리팹 에셋 자체(persistent)일 때는 Instantiate 불가 → 프리팹 편집 모드에서만 실행
        if (UnityEditor.EditorUtility.IsPersistent(gameObject)) return;

        // 미리보기 인스턴스 생성
        _previewInstance = Instantiate(_previewPrefab, transform);
        _previewInstance.name = "[Preview] " + _previewPrefab.name;
        _previewInstance.hideFlags = HideFlags.DontSaveInEditor | HideFlags.NotEditable;
        _previewInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        _previewInstance.transform.localScale = Vector3.one;

        // 스켈레톤 루트 찾기 (Base_Model 하위의 Skeleton)
        Transform skeletonRoot = FindSkeletonRootInHierarchy();
        if (skeletonRoot == null)
        {
            Debug.LogWarning("[CutsceneCharacterSlot] 스켈레톤 루트를 찾지 못했습니다.");
            return;
        }

        // SkinnedMeshBoneRemapper로 본 리매핑 (Timeline 에디터에서 애니메이션 미리보기 가능)
        var remapper = _previewInstance.GetComponentInChildren<SkinnedMeshBoneRemapper>();
        if (remapper != null)
        {
            remapper.RemapBonesTo(skeletonRoot);
        }
    }

    private Transform FindSkeletonRootInHierarchy()
    {
        // Base_Model 하위에서 스켈레톤 루트 탐색 (프리뷰 인스턴스 제외)
        string[] candidates = { "Skeleton", "Armature", "Root" };
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            // 프리뷰 인스턴스 하위는 제외
            if (_previewInstance != null && child.IsChildOf(_previewInstance.transform)) continue;

            foreach (string name in candidates)
            {
                if (child.name == name) return child;
            }
        }
        return null;
    }

#endif

    private void Awake()
    {
        if (!Application.isPlaying) return;

#if UNITY_EDITOR
        // 플레이 진입 시 미리보기 제거
        if (_previewInstance != null)
        {
            DestroyImmediate(_previewInstance);
            _previewInstance = null;
        }
#endif

        _view = GetComponent<CustomizingCharacterView>();
        _assetLoader = new AddressableAssetLoader();
        _view.Initialize(_assetLoader);
    }

    public void AssignPlayer(Player player)
    {
        _assignedPlayer = player;
    }

    // 할당된 플레이어의 Photon CustomProperties에서 커스터마이징 데이터를 읽어 적용
    public async UniTask ApplyCustomizingAsync(CancellationToken ct = default)
    {
        if (!IsAssigned) return;

        var manager = CustomizingManager.Instance;
        if (manager == null || !manager.IsInitialized)
        {
            Debug.LogWarning("[CutsceneCharacterSlot] CustomizingManager가 준비되지 않음");
            return;
        }

        // Photon CustomProperties에서 아이템 ID 조회
        var itemIds = CustomizingProperties.GetPlayerCustomizing(_assignedPlayer);

        // 기본 장착 적용 (Outfit, Gloves, Pants)
        ApplyBaseEquipment(manager);

        // 커스터마이징 데이터가 없으면 기본 장착만으로 종료
        if (itemIds == null || itemIds.Count == 0) return;

        // 커스터마이징 아이템 비동기 적용
        var tasks = new List<UniTask>();
        foreach (var kvp in itemIds)
        {
            var item = manager.GetItemById(kvp.Value);
            if (item != null)
            {
                tasks.Add(_view.ApplyItemAsync(kvp.Key, item));
            }
        }

        if (tasks.Count > 0)
        {
            await UniTask.WhenAll(tasks).AttachExternalCancellation(ct);
        }
    }

    // 플레이어 미할당 슬롯용: 기본 장착(Outfit, Gloves, Pants) + 기본 커스터마이징만 적용
    public void ApplyDefaultAppearance()
    {
        var manager = CustomizingManager.Instance;
        if (manager == null || !manager.IsInitialized)
        {
            Debug.LogWarning("[CutsceneCharacterSlot] CustomizingManager가 준비되지 않음 - 기본 외형 적용 실패");
            return;
        }

        // 기본 장착 적용
        ApplyBaseEquipment(manager);

        // 각 카테고리의 기본 아이템 적용 (IsDefault 플래그 기준)
        _view.ApplyAll(type => FindDefaultItem(manager, type));
    }

    private CustomizingItemSO FindDefaultItem(CustomizingManager manager, CustomizingType type)
    {
        var items = manager.GetUnlockedItemsByType(type);
        if (items == null || items.Count == 0) return null;

        foreach (var item in items)
        {
            if (item.IsDefault) return item;
        }

        return null;
    }

    private void ApplyBaseEquipment(CustomizingManager manager)
    {
        foreach (var (type, item) in manager.GetAllBaseEquipmentItems())
        {
            _view.ApplyBaseEquipment(type, item);
        }
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    private void OnDestroy()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (_previewInstance != null)
                DestroyImmediate(_previewInstance);
            return;
        }
#endif
        (_assetLoader as IDisposable)?.Dispose();
    }
}
