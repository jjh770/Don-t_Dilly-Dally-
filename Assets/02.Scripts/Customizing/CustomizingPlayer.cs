using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 커스터마이징 결과를 적용받는 캐릭터 객체
/// 선택된 외형 결과를 실제로 렌더링
///
/// 적용 방식:
/// - 모든 종류가 프리팹 기반으로 처리됨
/// - 해당 슬롯에 프리팹 인스턴스 생성 및 본 재매핑
/// </summary>
public class CustomizingPlayer : MonoBehaviour
{
    [Header("Skeleton")]
    [Tooltip("공통 Skeleton의 루트 Transform")]
    [SerializeField] private Transform skeletonRoot;

    [Header("Slot Parents")]
    [Tooltip("각 종류별 파츠가 배치될 부모 Transform")]
    [SerializeField] private Transform skinColorSlot;     // 피부색 (Body + Ears)
    [SerializeField] private Transform hatSlot;           // 모자
    [SerializeField] private Transform hairStyleSlot;     // 머리스타일
    [SerializeField] private Transform facesSlot;         // 표정
    [SerializeField] private Transform faceAccessorySlot; // 얼굴장식
    [SerializeField] private Transform glassesSlot;       // 안경
    [SerializeField] private Transform shoesSlot;         // 신발
    [SerializeField] private Transform costumesSlot;      // 코스튬

    [Header("Settings")]
    [Tooltip("파츠 적용 시 자동으로 본 재매핑")]
    [SerializeField] private bool autoRemapBones = true;

    // 현재 장착된 파츠 인스턴스
    private Dictionary<CustomizingType, GameObject> equippedInstances = new Dictionary<CustomizingType, GameObject>();

    // 캐시된 본 딕셔너리
    private Dictionary<string, Transform> boneCache;

    private void Awake()
    {
        AutoFindSkeletonRoot();
        BuildBoneCache();
    }

    /// <summary>
    /// 아이템 적용 (모든 종류가 프리팹 기반)
    /// </summary>
    public void ApplyItem(CustomizingType type, CustomizingItemSO item)
    {
        if (item == null)
        {
            ClearSlot(type);
            return;
        }

        ApplyPrefabPart(type, item);
    }

    /// <summary>
    /// 특정 슬롯 비우기
    /// </summary>
    public void ClearSlot(CustomizingType type)
    {
        if (equippedInstances.TryGetValue(type, out var instance))
        {
            if (instance != null)
            {
                if (Application.isPlaying)
                    Destroy(instance);
                else
                    DestroyImmediate(instance);
            }
            equippedInstances.Remove(type);
        }
    }

    /// <summary>
    /// 모든 슬롯 비우기
    /// </summary>
    public void ClearAll()
    {
        foreach (CustomizingType type in Enum.GetValues(typeof(CustomizingType)))
        {
            ClearSlot(type);
        }
    }

    // ========== Private Methods ==========

    private void AutoFindSkeletonRoot()
    {
        if (skeletonRoot != null) return;

        string[] possibleNames = { "Skeleton", "Armature", "Root" };
        foreach (string name in possibleNames)
        {
            Transform found = transform.Find(name);
            if (found != null)
            {
                skeletonRoot = found;
                Debug.Log($"[CustomizingPlayer] 스켈레톤 루트 자동 탐색: {name}");
                return;
            }
        }
    }

    private void BuildBoneCache()
    {
        if (skeletonRoot == null) return;

        boneCache = new Dictionary<string, Transform>();
        CacheBoneRecursive(skeletonRoot);
    }

    private void CacheBoneRecursive(Transform bone)
    {
        if (!boneCache.ContainsKey(bone.name))
        {
            boneCache[bone.name] = bone;
        }

        foreach (Transform child in bone)
        {
            CacheBoneRecursive(child);
        }
    }

    /// <summary>
    /// 프리팹 기반 파츠 적용
    /// </summary>
    private void ApplyPrefabPart(CustomizingType type, CustomizingItemSO item)
    {
        if (item.PartPrefab == null)
        {
            Debug.LogWarning($"[CustomizingPlayer] 아이템에 프리팹 없음: {item.ItemId}");
            ClearSlot(type);
            return;
        }

        // 기존 파츠 제거
        ClearSlot(type);

        // 슬롯 부모 결정
        Transform slotParent = GetSlotParent(type);

        // 새 파츠 인스턴스 생성
        GameObject instance = Instantiate(item.PartPrefab, slotParent);
        instance.name = item.PartPrefab.name;
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        // 레이어 설정 (7번)
        SetLayerRecursive(instance, 7);

        // 본 재매핑
        if (autoRemapBones)
        {
            RemapBones(instance);
        }

        // 인스턴스 등록
        equippedInstances[type] = instance;
    }

    /// <summary>
    /// 슬롯 부모 Transform 가져오기
    /// </summary>
    private Transform GetSlotParent(CustomizingType type)
    {
        switch (type)
        {
            case CustomizingType.SkinColor: return skinColorSlot ?? transform;
            case CustomizingType.Hat: return hatSlot ?? transform;
            case CustomizingType.HairStyle: return hairStyleSlot ?? transform;
            case CustomizingType.Faces: return facesSlot ?? transform;
            case CustomizingType.FaceAccessory: return faceAccessorySlot ?? transform;
            case CustomizingType.Glasses: return glassesSlot ?? transform;
            case CustomizingType.Shoes: return shoesSlot ?? transform;
            case CustomizingType.Costumes: return costumesSlot ?? transform;
            default: return transform;
        }
    }

    /// <summary>
    /// 파츠의 본을 공통 Skeleton으로 재매핑
    /// </summary>
    private void RemapBones(GameObject partInstance)
    {
        if (boneCache == null || boneCache.Count == 0)
        {
            BuildBoneCache();
        }

        // SkinnedMeshBoneRemapper 컴포넌트가 있으면 사용
        var remapper = partInstance.GetComponent<SkinnedMeshBoneRemapper>();
        if (remapper != null)
        {
            remapper.RemapBonesTo(skeletonRoot);
            return;
        }

        // 없으면 직접 재매핑
        SkinnedMeshRenderer[] renderers = partInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        foreach (var renderer in renderers)
        {
            var boneInfo = renderer.GetComponent<SkinnedMeshBoneInfo>();
            if (boneInfo == null || !boneInfo.IsValid())
            {
                continue;
            }

            // rootBone 재매핑
            if (boneCache.TryGetValue(boneInfo.RootBoneName, out Transform rootBone))
            {
                renderer.rootBone = rootBone;
            }

            // bones[] 재매핑
            string[] boneNames = boneInfo.BoneNames;
            Transform[] newBones = new Transform[boneNames.Length];

            for (int i = 0; i < boneNames.Length; i++)
            {
                if (!string.IsNullOrEmpty(boneNames[i]) && boneCache.TryGetValue(boneNames[i], out Transform bone))
                {
                    newBones[i] = bone;
                }
            }

            renderer.bones = newBones;
        }
    }

    /// <summary>
    /// 레이어 재귀 설정
    /// </summary>
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
        // 슬롯이 없으면 자동 생성
        if (skinColorSlot == null) skinColorSlot = CreateSlot("SkinColorSlot");
        if (hatSlot == null) hatSlot = CreateSlot("HatSlot");
        if (hairStyleSlot == null) hairStyleSlot = CreateSlot("HairStyleSlot");
        if (facesSlot == null) facesSlot = CreateSlot("FacesSlot");
        if (faceAccessorySlot == null) faceAccessorySlot = CreateSlot("FaceAccessorySlot");
        if (glassesSlot == null) glassesSlot = CreateSlot("GlassesSlot");
        if (shoesSlot == null) shoesSlot = CreateSlot("ShoesSlot");
        if (costumesSlot == null) costumesSlot = CreateSlot("CostumesSlot");

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
