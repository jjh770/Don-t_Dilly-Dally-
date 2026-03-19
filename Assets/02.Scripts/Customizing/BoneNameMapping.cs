using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 본 이름 매핑 엔트리
/// </summary>
[Serializable]
public class BoneNameMappingEntry
{
    [Tooltip("원본 에셋의 본 이름")]
    public string sourceName;

    [Tooltip("대상 Skeleton의 본 이름")]
    public string targetName;
}

/// <summary>
/// 본 이름이 다른 에셋 간의 매핑 테이블
/// ScriptableObject로 생성하여 재사용 가능
///
/// 사용 사례:
/// - 서로 다른 에셋 팩을 섞어 쓸 때
/// - 본 이름 규칙이 다른 경우 (예: Hips vs pelvis, Spine vs spine1)
///
/// 생성 방법:
/// Assets > Create > Customizing > Bone Name Mapping
/// </summary>
[CreateAssetMenu(fileName = "BoneNameMapping", menuName = "Customizing/Bone Name Mapping")]
public class BoneNameMapping : ScriptableObject
{
    [Header("Mapping Table")]
    [SerializeField]
    private List<BoneNameMappingEntry> mappings = new List<BoneNameMappingEntry>();

    [Header("Common Mappings")]
    [Tooltip("일반적인 본 이름 변환 자동 적용")]
    [SerializeField]
    private bool useCommonMappings = true;

    // 캐시된 딕셔너리
    private Dictionary<string, string> mappingDict;

    private void OnEnable()
    {
        BuildMappingDictionary();
    }

    private void BuildMappingDictionary()
    {
        mappingDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // 일반적인 매핑 추가
        if (useCommonMappings)
        {
            AddCommonMappings();
        }

        // 사용자 정의 매핑 추가 (우선순위 높음)
        foreach (var entry in mappings)
        {
            if (!string.IsNullOrEmpty(entry.sourceName) && !string.IsNullOrEmpty(entry.targetName))
            {
                mappingDict[entry.sourceName] = entry.targetName;
            }
        }
    }

    /// <summary>
    /// 일반적으로 다른 이름으로 사용되는 본들의 매핑
    /// </summary>
    private void AddCommonMappings()
    {
        // Humanoid 본 이름 변환 (예시)
        // Mixamo -> Unity Humanoid
        AddMapping("mixamorig:Hips", "Hips");
        AddMapping("mixamorig:Spine", "Spine");
        AddMapping("mixamorig:Spine1", "Spine1");
        AddMapping("mixamorig:Spine2", "Spine2");
        AddMapping("mixamorig:Neck", "Neck");
        AddMapping("mixamorig:Head", "Head");
        AddMapping("mixamorig:LeftShoulder", "LeftShoulder");
        AddMapping("mixamorig:LeftArm", "LeftUpperArm");
        AddMapping("mixamorig:LeftForeArm", "LeftLowerArm");
        AddMapping("mixamorig:LeftHand", "LeftHand");
        AddMapping("mixamorig:RightShoulder", "RightShoulder");
        AddMapping("mixamorig:RightArm", "RightUpperArm");
        AddMapping("mixamorig:RightForeArm", "RightLowerArm");
        AddMapping("mixamorig:RightHand", "RightHand");
        AddMapping("mixamorig:LeftUpLeg", "LeftUpperLeg");
        AddMapping("mixamorig:LeftLeg", "LeftLowerLeg");
        AddMapping("mixamorig:LeftFoot", "LeftFoot");
        AddMapping("mixamorig:LeftToeBase", "LeftToes");
        AddMapping("mixamorig:RightUpLeg", "RightUpperLeg");
        AddMapping("mixamorig:RightLeg", "RightLowerLeg");
        AddMapping("mixamorig:RightFoot", "RightFoot");
        AddMapping("mixamorig:RightToeBase", "RightToes");

        // 소문자/대문자 변환
        AddMapping("hips", "Hips");
        AddMapping("spine", "Spine");
        AddMapping("pelvis", "Hips");
    }

    private void AddMapping(string source, string target)
    {
        if (!mappingDict.ContainsKey(source))
        {
            mappingDict[source] = target;
        }
    }

    /// <summary>
    /// 본 이름 변환
    /// </summary>
    /// <param name="sourceName">원본 본 이름</param>
    /// <returns>변환된 본 이름 (매핑이 없으면 원본 반환)</returns>
    public string MapBoneName(string sourceName)
    {
        if (string.IsNullOrEmpty(sourceName))
            return sourceName;

        if (mappingDict == null)
            BuildMappingDictionary();

        if (mappingDict.TryGetValue(sourceName, out string targetName))
        {
            return targetName;
        }

        return sourceName;
    }

    /// <summary>
    /// 여러 본 이름 일괄 변환
    /// </summary>
    public string[] MapBoneNames(string[] sourceNames)
    {
        if (sourceNames == null)
            return null;

        string[] result = new string[sourceNames.Length];
        for (int i = 0; i < sourceNames.Length; i++)
        {
            result[i] = MapBoneName(sourceNames[i]);
        }
        return result;
    }

    /// <summary>
    /// 매핑 추가 (런타임)
    /// </summary>
    public void AddRuntimeMapping(string source, string target)
    {
        if (mappingDict == null)
            BuildMappingDictionary();

        mappingDict[source] = target;
    }

    /// <summary>
    /// 매핑 테이블 초기화
    /// </summary>
    [ContextMenu("Rebuild Mapping Dictionary")]
    public void RebuildMappingDictionary()
    {
        BuildMappingDictionary();
        Debug.Log($"[BoneMapping] Rebuilt dictionary with {mappingDict.Count} entries");
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        BuildMappingDictionary();
    }
#endif
}
