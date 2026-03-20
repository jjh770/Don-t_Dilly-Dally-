using UnityEngine;
using UnityEditor;

/// <summary>
/// CustomizingItemSO 커스텀 인스펙터
/// </summary>
[CustomEditor(typeof(CustomizingItemSO))]
public class CustomizingItemSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        CustomizingItemSO item = (CustomizingItemSO)target;

        // Basic Info
        EditorGUILayout.LabelField("Basic Info", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_itemId"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_displayName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_customizingType"));

        EditorGUILayout.Space(10);

        // Visual
        EditorGUILayout.LabelField("Visual", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_previewIcon"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_partPrefab"));

        EditorGUILayout.Space(10);

        // Settings
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_isDefault"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_isLocked"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_sortOrder"));

        serializedObject.ApplyModifiedProperties();

        // 프리뷰
        if (item.PreviewIcon != null)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            GUILayout.Box(item.PreviewIcon.texture, GUILayout.Width(64), GUILayout.Height(64));
        }
    }
}
