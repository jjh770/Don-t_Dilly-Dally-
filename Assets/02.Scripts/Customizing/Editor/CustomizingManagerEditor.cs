using UnityEngine;
using UnityEditor;

/// <summary>
/// CustomizingManager 커스텀 인스펙터
/// 에디터에서 테스트용 기능 제공
/// </summary>
[CustomEditor(typeof(CustomizingManager))]
public class CustomizingManagerEditor : Editor
{
    private CustomizingType _testType = CustomizingType.SkinColor;
    private string _testItemId = "";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CustomizingManager manager = (CustomizingManager)target;

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Editor Testing", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Play mode required for testing", MessageType.Info);
            return;
        }

        if (!manager.IsInitialized)
        {
            EditorGUILayout.HelpBox("Manager not initialized", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space(10);

        // 저장/로드 버튼
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save"))
        {
            manager.Save();
        }
        if (GUILayout.Button("Load"))
        {
            manager.Load();
        }
        if (GUILayout.Button("Reset All"))
        {
            manager.ResetAll();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 아이템 선택 테스트
        EditorGUILayout.LabelField("Item Selection Test", EditorStyles.boldLabel);

        _testType = (CustomizingType)EditorGUILayout.EnumPopup("Type", _testType);
        _testItemId = EditorGUILayout.TextField("Item ID", _testItemId);

        if (GUILayout.Button("Select Item By ID"))
        {
            manager.SelectItemById(_testItemId);
        }

        EditorGUILayout.Space(10);

        // 현재 장착 상태 표시
        EditorGUILayout.LabelField("Current Equipment", EditorStyles.boldLabel);

        foreach (CustomizingType type in System.Enum.GetValues(typeof(CustomizingType)))
        {
            var equipped = manager.GetEquipped(type);
            string itemName = equipped != null ? equipped.DisplayName : "(none)";
            EditorGUILayout.LabelField($"  {type}: {itemName}");
        }
    }
}
