using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 파츠 프리팹의 SkinnedMeshRenderer 본을 부모(CustomizingPlayer)의 Skeleton에 재매핑하는 컴포넌트
///
/// 사용법:
/// 1. 추출된 파츠 프리팹을 CustomizingPlayer 하위에 배치
/// 2. 런타임에 자동으로 본이 재매핑됨
/// 3. 또는 에디터에서 [Remap Bones] 버튼 클릭
///
/// 주의:
/// - 본 이름이 정확히 일치해야 함
/// - 부모 계층에 Skeleton(Armature) 오브젝트가 있어야 함
/// </summary>
public class SkinnedMeshBoneRemapper : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("부모에서 Skeleton/Armature를 찾을 때 사용할 이름들")]
    [SerializeField]
    private string[] skeletonRootNames = { "Skeleton", "Armature", "Root", "Hips" };

    [Tooltip("자동으로 재매핑 실행 (Awake 시) - CustomizingPlayer 사용 시 false 권장")]
    [SerializeField]
    private bool autoRemapOnAwake = false;

    [Tooltip("재매핑 성공 후 이 컴포넌트 제거")]
    [SerializeField]
    private bool destroyAfterRemap = false;

    [Header("Bone Name Mapping (Optional)")]
    [Tooltip("본 이름이 다른 경우 사용할 매핑 테이블")]
    [SerializeField]
    private BoneNameMapping boneNameMapping;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLogs = false;

    // 캐시된 본 딕셔너리
    private Dictionary<string, Transform> boneCache;

    private void Awake()
    {
        if (autoRemapOnAwake)
        {
            RemapBones();
        }
    }

    /// <summary>
    /// 모든 하위 SkinnedMeshRenderer의 본을 재매핑
    /// </summary>
    [ContextMenu("Remap Bones")]
    public void RemapBones()
    {
        // 부모에서 Skeleton 찾기
        Transform skeletonRoot = FindSkeletonRoot();
        if (skeletonRoot == null)
        {
            Debug.LogError($"[BoneRemapper] Cannot find skeleton root in parent hierarchy of {gameObject.name}");
            return;
        }

        if (showDebugLogs)
            Debug.Log($"[BoneRemapper] Found skeleton root: {skeletonRoot.name}");

        // 본 캐시 구축
        BuildBoneCache(skeletonRoot);

        // 모든 SkinnedMeshRenderer 처리
        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        int successCount = 0;

        foreach (var renderer in renderers)
        {
            if (RemapRendererBones(renderer))
            {
                successCount++;
            }
        }

        if (showDebugLogs)
            Debug.Log($"[BoneRemapper] Remapped {successCount}/{renderers.Length} renderers on {gameObject.name}");

        // 완료 후 컴포넌트 제거
        if (destroyAfterRemap && successCount == renderers.Length)
        {
            if (Application.isPlaying)
                Destroy(this);
            else
                DestroyImmediate(this);
        }
    }

    /// <summary>
    /// 부모 계층에서 Skeleton 루트 찾기
    /// </summary>
    private Transform FindSkeletonRoot()
    {
        Transform current = transform.parent;

        while (current != null)
        {
            // 현재 오브젝트의 자식에서 Skeleton 찾기
            foreach (string name in skeletonRootNames)
            {
                Transform skeleton = current.Find(name);
                if (skeleton != null)
                    return skeleton;
            }

            // 재귀적으로 모든 자식 검색
            foreach (string name in skeletonRootNames)
            {
                Transform skeleton = FindChildRecursive(current, name);
                if (skeleton != null)
                    return skeleton;
            }

            current = current.parent;
        }

        return null;
    }

    /// <summary>
    /// 재귀적으로 자식에서 이름으로 찾기
    /// </summary>
    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform found = FindChildRecursive(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    /// <summary>
    /// Skeleton의 모든 본을 딕셔너리에 캐싱
    /// </summary>
    private void BuildBoneCache(Transform root)
    {
        boneCache = new Dictionary<string, Transform>();

        // 루트 자체도 캐시에 추가
        boneCache[root.name] = root;

        // 모든 자식 본 캐싱
        CacheBoneRecursive(root);

        if (showDebugLogs)
            Debug.Log($"[BoneRemapper] Cached {boneCache.Count} bones");
    }

    private void CacheBoneRecursive(Transform bone)
    {
        foreach (Transform child in bone)
        {
            // 중복 이름 처리: 먼저 발견된 것 우선
            if (!boneCache.ContainsKey(child.name))
            {
                boneCache[child.name] = child;
            }
            else if (showDebugLogs)
            {
                Debug.LogWarning($"[BoneRemapper] Duplicate bone name: {child.name}");
            }

            CacheBoneRecursive(child);
        }
    }

    /// <summary>
    /// 단일 SkinnedMeshRenderer의 본 재매핑
    /// </summary>
    private bool RemapRendererBones(SkinnedMeshRenderer renderer)
    {
        // BoneInfo 컴포넌트에서 원본 본 이름 가져오기
        SkinnedMeshBoneInfo boneInfo = renderer.GetComponent<SkinnedMeshBoneInfo>();

        if (boneInfo == null || !boneInfo.IsValid())
        {
            Debug.LogWarning($"[BoneRemapper] No valid BoneInfo on {renderer.name}, skipping");
            return false;
        }

        string[] boneNames = boneInfo.BoneNames;
        string rootBoneName = boneInfo.RootBoneName;

        // 본 이름 매핑 적용 (옵션)
        if (boneNameMapping != null)
        {
            rootBoneName = boneNameMapping.MapBoneName(rootBoneName);
            boneNames = boneNameMapping.MapBoneNames(boneNames);
        }

        // rootBone 재매핑
        if (boneCache.TryGetValue(rootBoneName, out Transform newRootBone))
        {
            renderer.rootBone = newRootBone;
        }
        else
        {
            Debug.LogWarning($"[BoneRemapper] Root bone not found: {rootBoneName}");
            return false;
        }

        // bones[] 재매핑
        Transform[] newBones = new Transform[boneNames.Length];
        int missingCount = 0;

        for (int i = 0; i < boneNames.Length; i++)
        {
            string boneName = boneNames[i];

            if (string.IsNullOrEmpty(boneName))
            {
                newBones[i] = null;
                continue;
            }

            if (boneCache.TryGetValue(boneName, out Transform newBone))
            {
                newBones[i] = newBone;
            }
            else
            {
                newBones[i] = null;
                missingCount++;

                if (showDebugLogs)
                    Debug.LogWarning($"[BoneRemapper] Bone not found: {boneName} (index {i})");
            }
        }

        renderer.bones = newBones;

        if (missingCount > 0)
        {
            Debug.LogWarning($"[BoneRemapper] {renderer.name}: {missingCount}/{boneNames.Length} bones not found");
        }

        if (showDebugLogs)
            Debug.Log($"[BoneRemapper] Successfully remapped {renderer.name}");

        return missingCount == 0;
    }

    /// <summary>
    /// 수동으로 특정 Skeleton Transform을 지정하여 재매핑
    /// </summary>
    public void RemapBonesTo(Transform skeletonRoot)
    {
        if (skeletonRoot == null)
        {
            Debug.LogError("[BoneRemapper] skeletonRoot is null");
            return;
        }

        BuildBoneCache(skeletonRoot);

        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var renderer in renderers)
        {
            RemapRendererBones(renderer);
        }
    }

    /// <summary>
    /// 수동으로 Skeleton과 BoneNameMapping을 지정하여 재매핑
    /// </summary>
    public void RemapBonesTo(Transform skeletonRoot, BoneNameMapping mapping)
    {
        boneNameMapping = mapping;
        RemapBonesTo(skeletonRoot);
    }

    /// <summary>
    /// BoneNameMapping 설정 (런타임)
    /// </summary>
    public void SetBoneNameMapping(BoneNameMapping mapping)
    {
        boneNameMapping = mapping;
    }
}
