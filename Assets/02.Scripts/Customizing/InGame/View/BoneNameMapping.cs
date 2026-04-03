using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class BoneNameMappingEntry
{
    [Tooltip("원본 에셋의 본 이름")]
    public string SourceName;

    [Tooltip("대상 Skeleton의 본 이름")]
    public string TargetName;
}

[CreateAssetMenu(fileName = "BoneNameMapping", menuName = "Customizing/Bone Name Mapping")]
public class BoneNameMapping : ScriptableObject
{
    [Header("Mapping Table")]
    [SerializeField] private List<BoneNameMappingEntry> _mappings = new List<BoneNameMappingEntry>();

    [Header("Common Mappings")]
    [Tooltip("일반적인 본 이름 변환 자동 적용")]
    [SerializeField] private bool _useCommonMappings = true;

    private Dictionary<string, string> _mappingDict;

    private void OnEnable()
    {
        BuildMappingDictionary();
    }

    private void BuildMappingDictionary()
    {
        _mappingDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (_useCommonMappings)
        {
            AddCommonMappings();
        }

        foreach (var entry in _mappings)
        {
            if (string.IsNullOrEmpty(entry.SourceName) == false && string.IsNullOrEmpty(entry.TargetName) == false)
            {
                _mappingDict[entry.SourceName] = entry.TargetName;
            }
        }
    }

    private void AddCommonMappings()
    {
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

        AddMapping("hips", "Hips");
        AddMapping("spine", "Spine");
        AddMapping("pelvis", "Hips");
    }

    private void AddMapping(string source, string target)
    {
        if (_mappingDict.ContainsKey(source) == false)
        {
            _mappingDict[source] = target;
        }
    }

    public string MapBoneName(string sourceName)
    {
        if (string.IsNullOrEmpty(sourceName))
            return sourceName;

        if (_mappingDict == null)
            BuildMappingDictionary();

        if (_mappingDict.TryGetValue(sourceName, out string targetName))
        {
            return targetName;
        }

        return sourceName;
    }

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

    public void AddRuntimeMapping(string source, string target)
    {
        if (_mappingDict == null)
            BuildMappingDictionary();

        _mappingDict[source] = target;
    }

    [ContextMenu("Rebuild Mapping Dictionary")]
    public void RebuildMappingDictionary()
    {
        BuildMappingDictionary();
        Debug.Log($"[BoneMapping] 딕셔너리 재구축 완료: {_mappingDict.Count}개 항목");
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        BuildMappingDictionary();
    }
#endif
}
