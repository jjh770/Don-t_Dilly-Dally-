using UnityEngine;
using System.Collections.Generic;

public class SkinnedMeshBoneRemapper : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("부모에서 Skeleton/Armature를 찾을 때 사용할 이름들")]
    [SerializeField] private string[] _skeletonRootNames = { "Skeleton", "Armature", "Root", "Hips" };

    [Tooltip("자동으로 재매핑 실행 (Awake 시) - CustomizingPlayer 사용 시 false 권장")]
    [SerializeField] private bool _autoRemapOnAwake = false;

    [Tooltip("재매핑 성공 후 이 컴포넌트 제거")]
    [SerializeField] private bool _destroyAfterRemap = false;

    [Header("Bone Name Mapping (Optional)")]
    [Tooltip("본 이름이 다른 경우 사용할 매핑 테이블")]
    [SerializeField] private BoneNameMapping _boneNameMapping;

    [Header("Debug")]
    [SerializeField] private bool _showDebugLogs = false;

    private Dictionary<string, Transform> _boneCache;

    private void Awake()
    {
        if (_autoRemapOnAwake)
        {
            RemapBones();
        }
    }

    [ContextMenu("Remap Bones")]
    public void RemapBones()
    {
        Transform skeletonRoot = FindSkeletonRoot();
        if (skeletonRoot == null)
        {
            Debug.LogError($"[BoneRemapper] {gameObject.name}의 부모 계층에서 스켈레톤 루트를 찾을 수 없음");
            return;
        }

        if (_showDebugLogs)
            Debug.Log($"[BoneRemapper] 스켈레톤 루트 발견: {skeletonRoot.name}");

        BuildBoneCache(skeletonRoot);

        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        int successCount = 0;

        foreach (var renderer in renderers)
        {
            if (RemapRendererBones(renderer))
            {
                successCount++;
            }
        }

        if (_showDebugLogs)
            Debug.Log($"[BoneRemapper] {gameObject.name}에서 {successCount}/{renderers.Length}개 렌더러 재매핑 완료");

        if (_destroyAfterRemap && successCount == renderers.Length)
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
            foreach (string name in _skeletonRootNames)
            {
                Transform skeleton = current.Find(name);
                if (skeleton != null)
                    return skeleton;
            }

            foreach (string name in _skeletonRootNames)
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
        _boneCache = new Dictionary<string, Transform>();
        _boneCache[root.name] = root;
        CacheBoneRecursive(root);

        if (_showDebugLogs)
            Debug.Log($"[BoneRemapper] {_boneCache.Count}개 본 캐시됨");
    }

    private void CacheBoneRecursive(Transform bone)
    {
        foreach (Transform child in bone)
        {
            if (!_boneCache.ContainsKey(child.name))
            {
                _boneCache[child.name] = child;
            }
            else if (_showDebugLogs)
            {
                Debug.LogWarning($"[BoneRemapper] 중복된 본 이름: {child.name}");
            }

            CacheBoneRecursive(child);
        }
    }

    private bool RemapRendererBones(SkinnedMeshRenderer renderer)
    {
        SkinnedMeshBoneInfo boneInfo = renderer.GetComponent<SkinnedMeshBoneInfo>();

        if (boneInfo == null || !boneInfo.IsValid())
        {
            Debug.LogWarning($"[BoneRemapper] {renderer.name}에 유효한 BoneInfo 없음, 건너뜀");
            return false;
        }

        string[] boneNames = boneInfo.BoneNames;
        string rootBoneName = boneInfo.RootBoneName;

        if (_boneNameMapping != null)
        {
            rootBoneName = _boneNameMapping.MapBoneName(rootBoneName);
            boneNames = _boneNameMapping.MapBoneNames(boneNames);
        }

        if (_boneCache.TryGetValue(rootBoneName, out Transform newRootBone))
        {
            renderer.rootBone = newRootBone;
        }
        else
        {
            Debug.LogWarning($"[BoneRemapper] 루트 본을 찾을 수 없음: {rootBoneName}");
            return false;
        }

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

            if (_boneCache.TryGetValue(boneName, out Transform newBone))
            {
                newBones[i] = newBone;
            }
            else
            {
                newBones[i] = null;
                missingCount++;

                if (_showDebugLogs)
                    Debug.LogWarning($"[BoneRemapper] 본을 찾을 수 없음: {boneName} (인덱스 {i})");
            }
        }

        renderer.bones = newBones;

        if (missingCount > 0)
        {
            Debug.LogWarning($"[BoneRemapper] {renderer.name}: {missingCount}/{boneNames.Length}개 본을 찾을 수 없음");
        }

        if (_showDebugLogs)
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
        _boneNameMapping = mapping;
        RemapBonesTo(skeletonRoot);
    }

    public void SetBoneNameMapping(BoneNameMapping mapping)
    {
        _boneNameMapping = mapping;
    }
}
