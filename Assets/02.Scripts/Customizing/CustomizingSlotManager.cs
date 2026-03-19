using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 캐릭터 커스터마이징 슬롯 타입
/// </summary>
public enum CustomizingSlotType
{
    Body,
    Hairstyle,
    Face,
    FaceAccessory,
    Glasses,
    Ears,
    Hat,
    Outfit,
    Outwear,
    Pants,
    Shorts,
    Gloves,
    Socks,
    Shoes,
    Costume  // 전신 의상
}

/// <summary>
/// 슬롯 정보
/// </summary>
[Serializable]
public class CustomizingSlot
{
    public CustomizingSlotType slotType;
    public Transform slotParent;      // 슬롯 Transform (파츠가 이 아래에 배치됨)
    public GameObject currentPart;     // 현재 장착된 파츠
}

/// <summary>
/// CustomizingPlayer에 부착하여 파츠 교체를 관리하는 컴포넌트
///
/// 사용법:
/// 1. CustomizingPlayer 오브젝트에 이 컴포넌트 추가
/// 2. Skeleton Root에 스켈레톤의 루트(예: Armature/Skeleton) 할당
/// 3. EquipPart()로 파츠 장착, UnequipSlot()으로 제거
/// </summary>
public class CustomizingSlotManager : MonoBehaviour
{
    [Header("Skeleton Reference")]
    [Tooltip("공통 Skeleton의 루트 Transform")]
    [SerializeField]
    private Transform skeletonRoot;

    [Header("Slot Configuration")]
    [SerializeField]
    private List<CustomizingSlot> slots = new List<CustomizingSlot>();

    [Header("Settings")]
    [Tooltip("파츠 장착 시 자동으로 본 재매핑")]
    [SerializeField]
    private bool autoRemapOnEquip = true;

    // 슬롯 빠른 접근용 딕셔너리
    private Dictionary<CustomizingSlotType, CustomizingSlot> slotDict;

    private void Awake()
    {
        InitializeSlotDictionary();
        AutoFindSkeletonRoot();
    }

    private void InitializeSlotDictionary()
    {
        slotDict = new Dictionary<CustomizingSlotType, CustomizingSlot>();
        foreach (var slot in slots)
        {
            slotDict[slot.slotType] = slot;
        }
    }

    /// <summary>
    /// Skeleton 루트 자동 탐색
    /// </summary>
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
                Debug.Log($"[SlotManager] Auto-found skeleton root: {name}");
                return;
            }
        }

        Debug.LogWarning("[SlotManager] Could not auto-find skeleton root. Please assign manually.");
    }

    /// <summary>
    /// 파츠 장착
    /// </summary>
    /// <param name="slotType">장착할 슬롯</param>
    /// <param name="partPrefab">파츠 프리팹</param>
    /// <returns>생성된 파츠 인스턴스</returns>
    public GameObject EquipPart(CustomizingSlotType slotType, GameObject partPrefab)
    {
        if (partPrefab == null)
        {
            Debug.LogError("[SlotManager] partPrefab is null");
            return null;
        }

        // 기존 파츠 제거
        UnequipSlot(slotType);

        // 슬롯 부모 결정
        Transform parent = GetSlotParent(slotType);

        // 파츠 인스턴스 생성
        GameObject partInstance = Instantiate(partPrefab, parent);
        partInstance.name = partPrefab.name; // (Clone) 제거
        partInstance.transform.localPosition = Vector3.zero;
        partInstance.transform.localRotation = Quaternion.identity;
        partInstance.transform.localScale = Vector3.one;

        // 본 재매핑
        if (autoRemapOnEquip)
        {
            RemapPartBones(partInstance);
        }

        // 슬롯에 등록
        if (slotDict.TryGetValue(slotType, out CustomizingSlot slot))
        {
            slot.currentPart = partInstance;
        }

        return partInstance;
    }

    /// <summary>
    /// 슬롯 제거 (현재 파츠 삭제)
    /// </summary>
    public void UnequipSlot(CustomizingSlotType slotType)
    {
        if (slotDict.TryGetValue(slotType, out CustomizingSlot slot))
        {
            if (slot.currentPart != null)
            {
                if (Application.isPlaying)
                    Destroy(slot.currentPart);
                else
                    DestroyImmediate(slot.currentPart);

                slot.currentPart = null;
            }
        }
    }

    /// <summary>
    /// 모든 슬롯 제거
    /// </summary>
    public void UnequipAll()
    {
        foreach (var slot in slots)
        {
            UnequipSlot(slot.slotType);
        }
    }

    /// <summary>
    /// 특정 슬롯의 현재 파츠 가져오기
    /// </summary>
    public GameObject GetEquippedPart(CustomizingSlotType slotType)
    {
        if (slotDict.TryGetValue(slotType, out CustomizingSlot slot))
        {
            return slot.currentPart;
        }
        return null;
    }

    /// <summary>
    /// 파츠의 본을 공통 Skeleton으로 재매핑
    /// </summary>
    private void RemapPartBones(GameObject partInstance)
    {
        if (skeletonRoot == null)
        {
            Debug.LogError("[SlotManager] Skeleton root not set");
            return;
        }

        // BoneRemapper 컴포넌트가 있으면 사용
        var remapper = partInstance.GetComponent<SkinnedMeshBoneRemapper>();
        if (remapper != null)
        {
            remapper.RemapBonesTo(skeletonRoot);
            return;
        }

        // 없으면 직접 재매핑
        RemapBonesDirectly(partInstance);
    }

    /// <summary>
    /// BoneRemapper 없이 직접 재매핑 (fallback)
    /// </summary>
    private void RemapBonesDirectly(GameObject partInstance)
    {
        // 본 캐시 구축
        Dictionary<string, Transform> boneCache = new Dictionary<string, Transform>();
        CacheBones(skeletonRoot, boneCache);

        // 모든 SkinnedMeshRenderer 처리
        SkinnedMeshRenderer[] renderers = partInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        foreach (var renderer in renderers)
        {
            var boneInfo = renderer.GetComponent<SkinnedMeshBoneInfo>();
            if (boneInfo == null || !boneInfo.IsValid())
            {
                Debug.LogWarning($"[SlotManager] No BoneInfo on {renderer.name}");
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

    private void CacheBones(Transform root, Dictionary<string, Transform> cache)
    {
        cache[root.name] = root;
        foreach (Transform child in root)
        {
            CacheBones(child, cache);
        }
    }

    /// <summary>
    /// 슬롯의 부모 Transform 가져오기
    /// </summary>
    private Transform GetSlotParent(CustomizingSlotType slotType)
    {
        if (slotDict.TryGetValue(slotType, out CustomizingSlot slot) && slot.slotParent != null)
        {
            return slot.slotParent;
        }

        // 기본값: 이 오브젝트 자체
        return transform;
    }

    /// <summary>
    /// 슬롯 자동 생성 (에디터용)
    /// </summary>
    [ContextMenu("Auto Create Slots")]
    public void AutoCreateSlots()
    {
        slots.Clear();

        foreach (CustomizingSlotType slotType in Enum.GetValues(typeof(CustomizingSlotType)))
        {
            slots.Add(new CustomizingSlot
            {
                slotType = slotType,
                slotParent = transform,
                currentPart = null
            });
        }

        InitializeSlotDictionary();
        Debug.Log($"[SlotManager] Created {slots.Count} slots");
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 에디터에서 슬롯 변경 시 딕셔너리 재구축
        if (slots != null && slots.Count > 0)
        {
            InitializeSlotDictionary();
        }
    }
#endif
}
