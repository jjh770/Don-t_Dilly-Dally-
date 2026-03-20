using UnityEngine;
using System.Collections.Generic;

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

    private Dictionary<string, Transform> boneCache;

    private void Awake()
    {
        if (autoRemapOnAwake)
        {
            RemapBones();
        }
    }

    [ContextMenu("Remap Bones")]
    public void RemapBones()
    {
        // 부모에서 Skeleton 찾기
        Transform skeletonRoot = FindSkeletonRoot();
        if (skeletonRoot == null)
        {
            Debug.LogError($"[BoneRemapper] {gameObject.name}의 부모 계층에서 스켈레톤 루트를 찾을 수 없음");
            return;
        }

        if (showDebugLogs)
            Debug.Log($"[BoneRemapper] 스켈레톤 루트 발견: {skeletonRoot.name}");

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
            Debug.Log($"[BoneRemapper] {gameObject.name}에서 {successCount}/{renderers.Length}개 렌더러 재매핑 완료");

        // 완료 후 컴포넌트 제거
        if (destroyAfterRemap && successCount == renderers.Length)
        {
            if (Application.isPlaying)
                Destroy(this);
            else
                DestroyImmediate(this);
        }
    }

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

    private void BuildBoneCache(Transform root)
    {
        boneCache = new Dictionary<string, Transform>();

        // 루트 자체도 캐시에 추가
        boneCache[root.name] = root;

        // 모든 자식 본 캐싱
        CacheBoneRecursive(root);

        if (showDebugLogs)
            Debug.Log($"[BoneRemapper] {boneCache.Count}개 본 캐시됨");
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
                Debug.LogWarning($"[BoneRemapper] 중복된 본 이름: {child.name}");
            }

            CacheBoneRecursive(child);
        }
    }

    private bool RemapRendererBones(SkinnedMeshRenderer renderer)
    {
        // BoneInfo 컴포넌트에서 원본 본 이름 가져오기
        SkinnedMeshBoneInfo boneInfo = renderer.GetComponent<SkinnedMeshBoneInfo>();

        if (boneInfo == null || !boneInfo.IsValid())
        {
            Debug.LogWarning($"[BoneRemapper] {renderer.name}에 유효한 BoneInfo 없음, 건너뜀");
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
            Debug.LogWarning($"[BoneRemapper] 루트 본을 찾을 수 없음: {rootBoneName}");
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
                    Debug.LogWarning($"[BoneRemapper] 본을 찾을 수 없음: {boneName} (인덱스 {i})");
            }
        }

        renderer.bones = newBones;

        if (missingCount > 0)
        {
            Debug.LogWarning($"[BoneRemapper] {renderer.name}: {missingCount}/{boneNames.Length}개 본을 찾을 수 없음");
        }

        if (showDebugLogs)
            Debug.Log($"[BoneRemapper] {renderer.name} 재매핑 성공");

        return missingCount == 0;
    }

    public void RemapBonesTo(Transform skeletonRoot)
    {
        if (skeletonRoot == null)
        {
            Debug.LogError("[BoneRemapper] skeletonRoot가 null입니다");
            return;
        }

        BuildBoneCache(skeletonRoot);

        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var renderer in renderers)
        {
            RemapRendererBones(renderer);
        }
    }

    public void RemapBonesTo(Transform skeletonRoot, BoneNameMapping mapping)
    {
        boneNameMapping = mapping;
        RemapBonesTo(skeletonRoot);
    }

    public void SetBoneNameMapping(BoneNameMapping mapping)
    {
        boneNameMapping = mapping;
    }
}
