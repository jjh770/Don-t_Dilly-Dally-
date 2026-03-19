using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// SkinnedMeshRenderer의 원본 본 이름을 저장하는 컴포넌트
/// 추출된 프리팹에 자동으로 추가되어, 나중에 본 재매핑 시 사용됨
/// </summary>
public class SkinnedMeshBoneInfo : MonoBehaviour
{
    [Tooltip("원본 SkinnedMeshRenderer가 참조하던 본들의 이름 목록")]
    [SerializeField]
    private string[] boneNames;

    [Tooltip("원본 rootBone의 이름")]
    [SerializeField]
    private string rootBoneName;

    /// <summary>
    /// 본 이름 배열 반환
    /// </summary>
    public string[] BoneNames => boneNames;

    /// <summary>
    /// 루트 본 이름 반환
    /// </summary>
    public string RootBoneName => rootBoneName;

    /// <summary>
    /// SkinnedMeshRenderer에서 본 이름을 추출하여 저장
    /// (에디터에서 프리팹 생성 시 호출)
    /// </summary>
    public void SaveBoneNames(SkinnedMeshRenderer sourceRenderer)
    {
        if (sourceRenderer == null) return;

        // rootBone 이름 저장
        if (sourceRenderer.rootBone != null)
        {
            rootBoneName = sourceRenderer.rootBone.name;
        }

        // bones[] 이름 저장
        Transform[] sourceBones = sourceRenderer.bones;
        if (sourceBones != null && sourceBones.Length > 0)
        {
            boneNames = new string[sourceBones.Length];
            for (int i = 0; i < sourceBones.Length; i++)
            {
                if (sourceBones[i] != null)
                {
                    boneNames[i] = sourceBones[i].name;
                }
                else
                {
                    boneNames[i] = "";
                    Debug.LogWarning($"[BoneInfo] Null bone at index {i} in {sourceRenderer.name}");
                }
            }
        }
    }

    /// <summary>
    /// 본 이름 목록이 유효한지 확인
    /// </summary>
    public bool IsValid()
    {
        return boneNames != null && boneNames.Length > 0 && !string.IsNullOrEmpty(rootBoneName);
    }

    /// <summary>
    /// 디버그용: 저장된 본 정보 출력
    /// </summary>
    [ContextMenu("Print Bone Info")]
    public void PrintBoneInfo()
    {
        Debug.Log($"[BoneInfo] {gameObject.name}");
        Debug.Log($"  Root Bone: {rootBoneName}");
        Debug.Log($"  Bone Count: {boneNames?.Length ?? 0}");

        if (boneNames != null)
        {
            for (int i = 0; i < Mathf.Min(boneNames.Length, 10); i++)
            {
                Debug.Log($"    [{i}] {boneNames[i]}");
            }
            if (boneNames.Length > 10)
            {
                Debug.Log($"    ... and {boneNames.Length - 10} more");
            }
        }
    }
}
