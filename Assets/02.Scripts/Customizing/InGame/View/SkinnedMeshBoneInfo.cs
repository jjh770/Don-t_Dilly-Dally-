using UnityEngine;

public class SkinnedMeshBoneInfo : MonoBehaviour
{
    [Tooltip("원본 SkinnedMeshRenderer가 참조하던 본들의 이름 목록")]
    [SerializeField] private string[] _boneNames;

    [Tooltip("원본 rootBone의 이름")]
    [SerializeField] private string _rootBoneName;

    public string[] BoneNames => _boneNames;
    public string RootBoneName => _rootBoneName;

    public void SaveBoneNames(SkinnedMeshRenderer sourceRenderer)
    {
        if (sourceRenderer == null) return;

        if (sourceRenderer.rootBone != null)
        {
            _rootBoneName = sourceRenderer.rootBone.name;
        }

        Transform[] sourceBones = sourceRenderer.bones;
        if (sourceBones != null && sourceBones.Length > 0)
        {
            _boneNames = new string[sourceBones.Length];
            for (int i = 0; i < sourceBones.Length; i++)
            {
                if (sourceBones[i] != null)
                {
                    _boneNames[i] = sourceBones[i].name;
                }
                else
                {
                    _boneNames[i] = "";
                    Debug.LogWarning($"[BoneInfo] {sourceRenderer.name}의 인덱스 {i}에 본이 없음");
                }
            }
        }
    }

    public bool IsValid()
    {
        return _boneNames != null && _boneNames.Length > 0 && !string.IsNullOrEmpty(_rootBoneName);
    }

    [ContextMenu("Print Bone Info")]
    public void PrintBoneInfo()
    {
        Debug.Log($"[BoneInfo] {gameObject.name}");
        Debug.Log($"  루트 본: {_rootBoneName}");
        Debug.Log($"  본 개수: {_boneNames?.Length ?? 0}");

        if (_boneNames != null)
        {
            for (int i = 0; i < Mathf.Min(_boneNames.Length, 10); i++)
            {
                Debug.Log($"    [{i}] {_boneNames[i]}");
            }
            if (_boneNames.Length > 10)
            {
                Debug.Log($"    ... 외 {_boneNames.Length - 10}개");
            }
        }
    }
}
