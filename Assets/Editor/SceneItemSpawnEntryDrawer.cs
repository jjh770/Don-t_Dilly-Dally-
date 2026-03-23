using DontDillyDally.Data;
using UnityEditor;
using UnityEngine;

namespace DontDillyDally.Editor
{
    [CustomPropertyDrawer(typeof(SceneItemSpawnEntry))]
    public class SceneItemSpawnEntryDrawer : PropertyDrawer
    {
        private const float LineSpacing = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect lineRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            property.isExpanded = EditorGUI.Foldout(lineRect, property.isExpanded, label, true);
            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;

            SerializedProperty kindProp = property.FindPropertyRelative("Kind");

            lineRect.y += EditorGUIUtility.singleLineHeight + LineSpacing;
            EditorGUI.PropertyField(lineRect, kindProp);

            SpawnItemKind kind = (SpawnItemKind)kindProp.enumValueIndex;

            switch (kind)
            {
                case SpawnItemKind.MixTool:
                    lineRect.y += EditorGUIUtility.singleLineHeight + LineSpacing;
                    EditorGUI.PropertyField(lineRect, property.FindPropertyRelative("ToolType"));

                    lineRect.y += EditorGUIUtility.singleLineHeight + LineSpacing;
                    EditorGUI.PropertyField(lineRect, property.FindPropertyRelative("SourcePrefabOverride"),
                        new GUIContent("Source 프리팹 오버라이드"));
                    break;

                case SpawnItemKind.BasicMaterial:
                    lineRect.y += EditorGUIUtility.singleLineHeight + LineSpacing;
                    EditorGUI.PropertyField(lineRect, property.FindPropertyRelative("MaterialType"));
                    break;
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float lineHeight = EditorGUIUtility.singleLineHeight + LineSpacing;

            // Foldout + Kind = 2줄
            int lineCount = 2;

            SerializedProperty kindProp = property.FindPropertyRelative("Kind");
            SpawnItemKind kind = (SpawnItemKind)kindProp.enumValueIndex;

            switch (kind)
            {
                case SpawnItemKind.MixTool:
                    lineCount += 2; // ToolType + SourcePrefabOverride
                    break;
                case SpawnItemKind.BasicMaterial:
                    lineCount += 1; // MaterialType
                    break;
            }

            return lineCount * lineHeight;
        }
    }
}
