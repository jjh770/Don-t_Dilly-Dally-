using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCustomizingView : MonoBehaviour
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
    [SerializeField] private bool _autoRemapBones = true;
    [SerializeField] private int _targetLayer = 7;

    private Dictionary<CustomizingType, GameObject> _equippedInstances = new();
    private Dictionary<BaseEquipmentType, GameObject> _baseEquipmentInstances = new();
    private Dictionary<string, Transform> _boneCache;

    private void Awake()
    {
        AutoFindSkeletonRoot();
        BuildBoneCache();
    }

    // 커스터마이징 아이템 하나 입히기
    public void ApplyItem(CustomizingType type, CustomizingItemSO item)
    {
        if (item == null)
        {
            ClearSlot(type);
            return;
        }

        if (item.PartPrefab == null)
        {
            Debug.LogWarning($"[PlayerCustomizingView] 아이템에 프리팹 없음: {item.ItemId}");
            ClearSlot(type);
            return;
        }

        ClearSlot(type);

        Transform slotParent = GetSlotParent(type);
        GameObject instance = InstantiatePart(item.PartPrefab, slotParent, item.PartPrefab.name);

        _equippedInstances[type] = instance;
    }

    public void ApplyBaseEquipment(BaseEquipmentType type, BaseEquipmentItemSO item)
    {
        if (item == null || item.PartPrefab == null)
        {
            Debug.LogWarning($"[PlayerCustomizingView] 기본 장착 아이템이 유효하지 않음: {type}");
            return;
        }

        ClearBaseEquipmentSlot(type);

        Transform slotParent = _baseEquipmentSlot ?? transform;
        string instanceName = $"BaseEquipment_{type}_{item.PartPrefab.name}";
        GameObject instance = InstantiatePart(item.PartPrefab, slotParent, instanceName);

        _baseEquipmentInstances[type] = instance;
    }

    public void ApplyAll(Func<CustomizingType, CustomizingItemSO> itemGetter)
    {
        foreach (CustomizingType type in Enum.GetValues(typeof(CustomizingType)))
        {
            var item = itemGetter(type);
            ApplyItem(type, item);
        }
    }

    public void ClearSlot(CustomizingType type)
    {
        if (_equippedInstances.TryGetValue(type, out var instance))
        {
            DestroyInstance(instance);
            _equippedInstances.Remove(type);
        }
    }

    public void ClearBaseEquipmentSlot(BaseEquipmentType type)
    {
        if (_baseEquipmentInstances.TryGetValue(type, out var instance))
        {
            DestroyInstance(instance);
            _baseEquipmentInstances.Remove(type);
        }
    }

    private GameObject InstantiatePart(GameObject prefab, Transform parent, string instanceName)
    {
        GameObject instance = Instantiate(prefab, parent);
        instance.name = instanceName;
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        SetLayerRecursive(instance, _targetLayer);

        if (_autoRemapBones)
        {
            RemapBones(instance);
        }

        return instance;
    }
    private void DestroyInstance(GameObject instance)
    {
        if (instance == null) return;

        if (Application.isPlaying)
            Destroy(instance);
        else
            DestroyImmediate(instance);
    }

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

    private Transform GetSlotParent(CustomizingType type)
    {
        return type switch
        {
            CustomizingType.SkinColor => _skinColorSlot ?? transform,
            CustomizingType.Hat => _hatSlot ?? transform,
            CustomizingType.HairStyle => _hairStyleSlot ?? transform,
            CustomizingType.Faces => _facesSlot ?? transform,
            CustomizingType.FaceAccessory => _faceAccessorySlot ?? transform,
            CustomizingType.Glasses => _glassesSlot ?? transform,
            CustomizingType.Shoes => _shoesSlot ?? transform,
            CustomizingType.Costumes => _costumesSlot ?? transform,
            _ => transform
        };
    }

    private void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }
}
