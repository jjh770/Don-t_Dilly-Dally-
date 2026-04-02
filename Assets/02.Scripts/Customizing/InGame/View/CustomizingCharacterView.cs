using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CustomizingCharacterView : MonoBehaviour
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

    private ICustomizingAssetLoader _assetLoader;
    private Dictionary<CustomizingType, string> _loadedAssetKeys = new();               // 현재 각 슬롯에 어떤 Key가 로드되어 있는지 기록
    private Dictionary<CustomizingType, CancellationTokenSource> _loadingCts = new();   // 각 타입별 현재 진행 중인 로딩 취소 토큰 저장
    private CancellationTokenSource _applyAllCts;                                       // ApplyAll 전체 작업 취소 토큰

    private void Awake()
    {
        AutoFindSkeletonRoot();
        BuildBoneCache();
    }

    public void Initialize(ICustomizingAssetLoader assetLoader)
    {
        _assetLoader = assetLoader ?? throw new ArgumentNullException(nameof(assetLoader));
    }

    private void OnDestroy()
    {
        CancelAllLoading();
        ReleaseAllAssets();
        (_assetLoader as IDisposable)?.Dispose();
    }

    private void CancelAllLoading()
    {
        CancelApplyAll();

        foreach (var cts in _loadingCts.Values)
        {
            cts?.Cancel();
            cts?.Dispose();
        }
        _loadingCts.Clear();
    }

    private void CancelApplyAll()
    {
        _applyAllCts?.Cancel();
        _applyAllCts?.Dispose();
        _applyAllCts = null;
    }

    public async UniTask PreloadItemsAsync(IEnumerable<CustomizingItemSO> items)
    {
        var keys = items
            .Where(item => item != null && item.HasAssetRef)
            .Select(item => item.AssetKey)
            .Where(key => !string.IsNullOrEmpty(key));

        await _assetLoader.PreloadAsync(keys);
    }

    public void ApplyItem(CustomizingType type, CustomizingItemSO item)
    {
        if (item == null || !item.HasAssetRef)
        {
            ClearSlot(type);
            return;
        }

        ApplyItemAsync(type, item).Forget();
    }

    // 비동기 로딩
    public async UniTask ApplyItemAsync(CustomizingType type, CustomizingItemSO item)
    {
        // 같은 타입 슬롯에 이전 로딩이 남아있으면 먼저 취소
        CancelLoading(type);

        if (item == null || !item.HasAssetRef)
        {
            ClearSlotInternal(type);
            return;
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        _loadingCts[type] = cts;

        try
        {
            var prefab = await _assetLoader.LoadAsync(item.AssetKey);
            
            if (cts.IsCancellationRequested || prefab == null) return;

            ClearSlotInternal(type);

            Transform slotParent = GetSlotParent(type);
            GameObject instance = InstantiatePart(prefab, slotParent, prefab.name);

            _equippedInstances[type] = instance;
            _loadedAssetKeys[type] = item.AssetKey;
        }
        catch (OperationCanceledException)
        {
            // 취소됨 - 기존 파츠 유지
        }
        catch (Exception e)
        {
            // 로드 실패 - 기존 파츠 유지
            Debug.LogError($"[CustomizingCharacterView] 에셋 로드 실패: {item.ItemId}, {e.Message}");
        }
        finally
        {
            if (_loadingCts.TryGetValue(type, out var existingCts) && existingCts == cts)
            {
                _loadingCts.Remove(type);
            }
            cts.Dispose();
        }
    }

    private void CancelLoading(CustomizingType type)
    {
        if (_loadingCts.TryGetValue(type, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
            _loadingCts.Remove(type);
        }
    }

    public void ApplyBaseEquipment(BaseEquipmentType type, BaseEquipmentItemSO item)
    {
        if (item == null || item.PartPrefab == null)
        {
            Debug.LogWarning($"[CustomizingCharacterView] 기본 장착 아이템이 유효하지 않음: {type}");
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
        CancelApplyAll();

        _applyAllCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        ApplyAllAsync(itemGetter, _applyAllCts.Token).Forget();
    }

    private async UniTask ApplyAllAsync(Func<CustomizingType, CustomizingItemSO> itemGetter, CancellationToken cancellationToken)
    {
        try
        {
            // 1. 모든 아이템 수집
            var items = new List<CustomizingItemSO>();
            foreach (CustomizingType type in Enum.GetValues(typeof(CustomizingType)))
            {
                var item = itemGetter(type);
                if (item != null && item.HasAssetRef)
                {
                    items.Add(item);
                }
            }

            // 2. 프리로드
            await PreloadItemsAsync(items);

            cancellationToken.ThrowIfCancellationRequested();

            // 3. 적용 (이미 캐시되어 있으므로 빠름)
            foreach (CustomizingType type in Enum.GetValues(typeof(CustomizingType)))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var item = itemGetter(type);
                ApplyItem(type, item);
            }
        }
        catch (OperationCanceledException)
        {
            // 취소됨 - 무시
        }
    }

    public void ClearSlot(CustomizingType type)
    {
        CancelLoading(type);
        ClearSlotInternal(type);
    }

    private void ClearSlotInternal(CustomizingType type)
    {
        if (_equippedInstances.TryGetValue(type, out var instance))
        {
            DestroyInstance(instance);
            _equippedInstances.Remove(type);
        }

        ReleaseAsset(type);
    }

    private void ReleaseAsset(CustomizingType type)
    {
        if (_loadedAssetKeys.TryGetValue(type, out var key))
        {
            _assetLoader?.Release(key);
            _loadedAssetKeys.Remove(type);
        }
    }

    private void ReleaseAllAssets()
    {
        _assetLoader?.ReleaseAll();
        _loadedAssetKeys.Clear();
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

        foreach (var smr in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            smr.updateWhenOffscreen = true;
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


    // ======== Bone 매핑 ========
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
