using UnityEngine;
using UnityEditor;

/// <summary>
/// SkinnedMeshBoneRemapper의 커스텀 인스펙터
/// 에디터에서 본 재매핑을 수동으로 실행할 수 있음
/// </summary>
[CustomEditor(typeof(SkinnedMeshBoneRemapper))]
public class SkinnedMeshBoneRemapperEditor : Editor
{
    private Transform _customSkeletonRoot;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Manual Remap", EditorStyles.boldLabel);

        SkinnedMeshBoneRemapper remapper = (SkinnedMeshBoneRemapper)target;

        // 자동 재매핑 버튼
        if (GUILayout.Button("Remap Bones (Auto-find Skeleton)", GUILayout.Height(30)))
        {
            Undo.RecordObject(remapper, "Remap Bones");
            remapper.RemapBones();
            EditorUtility.SetDirty(remapper);
        }

        EditorGUILayout.Space(10);

        // 수동 Skeleton 지정
        _customSkeletonRoot = (Transform)EditorGUILayout.ObjectField(
            "Custom Skeleton Root",
            _customSkeletonRoot,
            typeof(Transform),
            true);

        GUI.enabled = _customSkeletonRoot != null;
        if (GUILayout.Button("Remap to Custom Skeleton"))
        {
            Undo.RecordObject(remapper, "Remap Bones to Custom");
            remapper.RemapBonesTo(_customSkeletonRoot);
            EditorUtility.SetDirty(remapper);
        }
        GUI.enabled = true;

        EditorGUILayout.Space(10);

        // 하위 SkinnedMeshRenderer 정보 표시
        EditorGUILayout.LabelField("SkinnedMeshRenderers", EditorStyles.boldLabel);

        SkinnedMeshRenderer[] renderers = remapper.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        foreach (var renderer in renderers)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField(renderer.name, EditorStyles.boldLabel);

            // 현재 본 상태
            string rootBoneName = renderer.rootBone != null ? renderer.rootBone.name : "(null)";
            EditorGUILayout.LabelField($"  Root Bone: {rootBoneName}");
            EditorGUILayout.LabelField($"  Bones: {renderer.bones?.Length ?? 0}");

            // BoneInfo 상태
            var boneInfo = renderer.GetComponent<SkinnedMeshBoneInfo>();
            if (boneInfo != null && boneInfo.IsValid())
            {
                EditorGUILayout.LabelField($"  BoneInfo: Valid ({boneInfo.BoneNames.Length} bones)");
            }
            else
            {
                EditorGUILayout.HelpBox("BoneInfo missing or invalid", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }
    }
}
