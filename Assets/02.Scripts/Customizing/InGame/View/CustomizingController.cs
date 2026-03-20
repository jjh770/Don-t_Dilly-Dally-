using UnityEngine;
using System;
using System.Collections.Generic;

public class CustomizingController : MonoBehaviour
{
    [Header("Skeleton")]
    [SerializeField] private Transform _skeletonRoot;

    [Header("고정 장착 슬롯")]
    [SerializeField] private Transform _baseEquipmentSlot;

    [Header("각 종류별 파츠가 배치될 부모 Transform")]
    [SerializeField] private Transform _skinColorSlot;
    [SerializeField] private Transform _hatSlot;
    [SerializeField] private Transform _hairStyleSlot;
    [SerializeField] private Transform _facesSlot;
    [SerializeField] private Transform _faceAccessorySlot;
    [SerializeField] private Transform _glassesSlot;
    [SerializeField] private Transform _shoesSlot;
    [SerializeField] private Transform _costumesSlot;

    [Header("세팅")]
    [Tooltip("파츠 적용 시 자동으로 본 재매핑")]
    [SerializeField] private bool _autoRemapBones = true;

    // 현재 장착된 커스터마이징 파츠 인스턴스
    private Dictionary<CustomizingType, GameObject> _equippedInstances = new Dictionary<CustomizingType, GameObject>();

    // 현재 장착된 기본 장착 파츠 인스턴스
    private Dictionary<BaseEquipmentType, GameObject> _baseEquipmentInstances = new Dictionary<BaseEquipmentType, GameObject>();

    // 캐시된 본 딕셔너리
    private Dictionary<string, Transform> _boneCache;

    private void Awake()
    {
        AutoFindSkeletonRoot();
        BuildBoneCache();
    }

    private void Start()
    {
        SubscribeToManager();
    }

    private void OnDestroy()
    {
        UnsubscribeFromManager();
    }

    private void SubscribeToManager()
    {
        if (CustomizingManager.Instance == null) return;

        CustomizingManager.Instance.OnLoaded += HandleLoaded;
        CustomizingManager.Instance.OnItemChanged += HandleItemChanged;
    }

    private void UnsubscribeFromManager()
    {
        if (CustomizingManager.Instance == null) return;

        CustomizingManager.Instance.OnLoaded -= HandleLoaded;
        CustomizingManager.Instance.OnItemChanged -= HandleItemChanged;
    }

    private void HandleLoaded()
    {
        ApplyAllFromManager();
    }

    private void HandleItemChanged(CustomizingType type, CustomizingItemSO item)
    {
        ApplyItem(type, item);
    }

    private void ApplyAllFromManager()
    {
        var manager = CustomizingManager.Instance;
        if (manager == null) return;

        // 기본 장착 적용
        if (manager.BaseEquipmentCatalog != null)
        {
            ApplyAllBaseEquipment(manager.BaseEquipmentCatalog);
        }

        // 모든 커스터마이징 아이템 적용
        foreach (CustomizingType type in Enum.GetValues(typeof(CustomizingType)))
        {
            var item = manager.GetEquipped(type);
            ApplyItem(type, item);
        }
    }

    // 기본 아이템 장착
    public void ApplyBaseEquipment(BaseEquipmentType type, BaseEquipmentItemSO item)
    {
        if (item == null || item.PartPrefab == null)
        {
            Debug.LogWarning($"[CustomizingController] 기본 장착 아이템이 유효하지 않음: {type}");
            return;
        }

        ClearBaseEquipmentSlot(type);

        Transform slotParent = _baseEquipmentSlot ?? transform;

        GameObject instance = Instantiate(item.PartPrefab, slotParent);
        instance.name = $"BaseEquipment_{type}_{item.PartPrefab.name}";
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        SetLayerRecursive(instance, 7);

        if (_autoRemapBones)
        {
            RemapBones(instance);
        }

        _baseEquipmentInstances[type] = instance;
    }

    // 모든 기본 장착 아이템 적용
    public void ApplyAllBaseEquipment(BaseEquipmentCatalogSO catalog)
    {
        if (catalog == null)
        {
            Debug.LogWarning("[CustomizingController] 기본 장착 카탈로그가 null");
            return;
        }

        foreach (BaseEquipmentType type in Enum.GetValues(typeof(BaseEquipmentType)))
        {
            var item = catalog.GetItem(type);
            if (item != null)
            {
                ApplyBaseEquipment(type, item);
            }
        }
    }

    // 특정 기본 장착 슬롯 비우기
    public void ClearBaseEquipmentSlot(BaseEquipmentType type)
    {
        if (_baseEquipmentInstances.TryGetValue(type, out var instance))
        {
            if (instance != null)
            {
                if (Application.isPlaying)
                    Destroy(instance);
                else
                    DestroyImmediate(instance);
            }
            _baseEquipmentInstances.Remove(type);
        }
    }

    // ========== 커스터마이징 ==========

    // 커스터마이징 아이템 적용
    public void ApplyItem(CustomizingType type, CustomizingItemSO item)
    {
        if (item == null)
        {
            ClearSlot(type);
            return;
        }

        ApplyPrefabPart(type, item);
    }

    // 특정 슬롯 비우기
    public void ClearSlot(CustomizingType type)
    {
        if (_equippedInstances.TryGetValue(type, out var instance))
        {
            if (instance != null)
            {
                if (Application.isPlaying)
                    Destroy(instance);
                else
                    DestroyImmediate(instance);
            }
            _equippedInstances.Remove(type);
        }
    }

    // ========== Private Methods ==========

    private void AutoFindSkeletonRoot()
    {
        if (_skeletonRoot != null) return;

        string[] possibleNames = { "Skeleton", "Armature", "Root" };
        foreach (string name in possibleNames)
        {
            Transform found = transform.Find(name);
            if (found != null)
            {
                _skeletonRoot = found;
                Debug.Log($"[CustomizingController] 스켈레톤 루트 자동 탐색: {name}");
                return;
            }
        }
    }

    private void BuildBoneCache()
    {
        if (_skeletonRoot == null) return;

        _boneCache = new Dictionary<string, Transform>();
        CacheBoneRecursive(_skeletonRoot);
    }

    private void CacheBoneRecursive(Transform bone)
    {
        if (!_boneCache.ContainsKey(bone.name))
        {
            _boneCache[bone.name] = bone;
        }

        foreach (Transform child in bone)
        {
            CacheBoneRecursive(child);
        }
    }

    private void ApplyPrefabPart(CustomizingType type, CustomizingItemSO item)
    {
        if (item.PartPrefab == null)
        {
            Debug.LogWarning($"[CustomizingController] 아이템에 프리팹 없음: {item.ItemId}");
            ClearSlot(type);
            return;
        }
        ClearSlot(type);

        Transform slotParent = GetSlotParent(type);

        GameObject instance = Instantiate(item.PartPrefab, slotParent);
        instance.name = item.PartPrefab.name;
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        SetLayerRecursive(instance, 7);

        if (_autoRemapBones)
        {
            RemapBones(instance);
        }

        _equippedInstances[type] = instance;
    }

    private Transform GetSlotParent(CustomizingType type)
    {
        switch (type)
        {
            case CustomizingType.SkinColor: return _skinColorSlot ?? transform;
            case CustomizingType.Hat: return _hatSlot ?? transform;
            case CustomizingType.HairStyle: return _hairStyleSlot ?? transform;
            case CustomizingType.Faces: return _facesSlot ?? transform;
            case CustomizingType.FaceAccessory: return _faceAccessorySlot ?? transform;
            case CustomizingType.Glasses: return _glassesSlot ?? transform;
            case CustomizingType.Shoes: return _shoesSlot ?? transform;
            case CustomizingType.Costumes: return _costumesSlot ?? transform;
            default: return transform;
        }
    }

    // 파츠의 본을 공통 Skeleton으로 재매핑
    private void RemapBones(GameObject partInstance)
    {
        if (_boneCache == null || _boneCache.Count == 0)
        {
            BuildBoneCache();
        }

        var remapper = partInstance.GetComponent<SkinnedMeshBoneRemapper>();
        if (remapper != null)
        {
            remapper.RemapBonesTo(_skeletonRoot);
            return;
        }

        SkinnedMeshRenderer[] renderers = partInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        foreach (var renderer in renderers)
        {
            var boneInfo = renderer.GetComponent<SkinnedMeshBoneInfo>();
            if (boneInfo == null || !boneInfo.IsValid())
            {
                continue;
            }

            if (_boneCache.TryGetValue(boneInfo.RootBoneName, out Transform rootBone))
            {
                renderer.rootBone = rootBone;
            }

            string[] boneNames = boneInfo.BoneNames;
            Transform[] newBones = new Transform[boneNames.Length];

            for (int i = 0; i < boneNames.Length; i++)
            {
                if (!string.IsNullOrEmpty(boneNames[i]) && _boneCache.TryGetValue(boneNames[i], out Transform bone))
                {
                    newBones[i] = bone;
                }
            }

            renderer.bones = newBones;
        }
    }

    // 레이어 재귀 설정
    private void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Setup Slots")]
    private void AutoSetupSlots()
    {
        // 기본 장착 슬롯 생성
        if (_baseEquipmentSlot == null) _baseEquipmentSlot = CreateSlot("BaseEquipmentSlot");

        // 커스터마이징 슬롯 생성
        if (_skinColorSlot == null) _skinColorSlot = CreateSlot("SkinColorSlot");
        if (_hatSlot == null) _hatSlot = CreateSlot("HatSlot");
        if (_hairStyleSlot == null) _hairStyleSlot = CreateSlot("HairStyleSlot");
        if (_facesSlot == null) _facesSlot = CreateSlot("FacesSlot");
        if (_faceAccessorySlot == null) _faceAccessorySlot = CreateSlot("FaceAccessorySlot");
        if (_glassesSlot == null) _glassesSlot = CreateSlot("GlassesSlot");
        if (_shoesSlot == null) _shoesSlot = CreateSlot("ShoesSlot");
        if (_costumesSlot == null) _costumesSlot = CreateSlot("CostumesSlot");

        UnityEditor.EditorUtility.SetDirty(this);
    }

    private Transform CreateSlot(string name)
    {
        var existing = transform.Find(name);
        if (existing != null) return existing;

        var slot = new GameObject(name);
        slot.transform.SetParent(transform);
        slot.transform.localPosition = Vector3.zero;
        slot.transform.localRotation = Quaternion.identity;
        slot.transform.localScale = Vector3.one;
        return slot.transform;
    }
#endif
}
