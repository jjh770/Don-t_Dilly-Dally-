using UnityEngine;
using UnityEditor;

/// <summary>
/// CustomizingSlotManager의 커스텀 인스펙터
/// 에디터에서 파츠 장착/해제를 테스트할 수 있음
/// </summary>
[CustomEditor(typeof(CustomizingSlotManager))]
public class CustomizingSlotManagerEditor : Editor
{
    private CustomizingSlotType selectedSlot = CustomizingSlotType.Body;
    private GameObject partPrefab;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Editor Testing", EditorStyles.boldLabel);

        CustomizingSlotManager manager = (CustomizingSlotManager)target;

        // 슬롯 선택
        selectedSlot = (CustomizingSlotType)EditorGUILayout.EnumPopup("Slot", selectedSlot);

        // 파츠 프리팹 선택
        partPrefab = (GameObject)EditorGUILayout.ObjectField("Part Prefab", partPrefab, typeof(GameObject), false);

        EditorGUILayout.BeginHorizontal();

        // 장착 버튼
        GUI.enabled = partPrefab != null;
        if (GUILayout.Button("Equip"))
        {
            Undo.RecordObject(manager, "Equip Part");
            manager.EquipPart(selectedSlot, partPrefab);
        }
        GUI.enabled = true;

        // 해제 버튼
        if (GUILayout.Button("Unequip"))
        {
            Undo.RecordObject(manager, "Unequip Slot");
            manager.UnequipSlot(selectedSlot);
        }

        EditorGUILayout.EndHorizontal();

        // 전체 해제 버튼
        if (GUILayout.Button("Unequip All"))
        {
            Undo.RecordObject(manager, "Unequip All");
            manager.UnequipAll();
        }

        EditorGUILayout.Space(10);

        // 현재 장착 상태 표시
        EditorGUILayout.LabelField("Current Equipment", EditorStyles.boldLabel);

        foreach (CustomizingSlotType slotType in System.Enum.GetValues(typeof(CustomizingSlotType)))
        {
            GameObject equipped = manager.GetEquippedPart(slotType);
            if (equipped != null)
            {
                EditorGUILayout.LabelField($"  {slotType}: {equipped.name}");
            }
        }
    }
}
