using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Gloves, Outfit, Pants 프리팹을 BaseEquipmentItemSO로 생성하는 에디터
/// </summary>
public class BaseEquipmentItemGenerator : EditorWindow
{
    private const string ITEMS_PATH = "Assets/03.Prefabs/Customizing/Items";
    private const string OUTPUT_PATH = "Assets/03.Prefabs/Customizing/Catalog/BaseEquipment";

    private static readonly Dictionary<string, BaseEquipmentType> FolderToType = new Dictionary<string, BaseEquipmentType>
    {
        { "Gloves", BaseEquipmentType.Gloves },
        { "Outfit", BaseEquipmentType.Outfit },
        { "Pants", BaseEquipmentType.Pants }
    };

    private Vector2 _scrollPosition;
    private List<PrefabEntry> _prefabEntries = new List<PrefabEntry>();
    private BaseEquipmentCatalogSO _targetCatalog;

    private class PrefabEntry
    {
        public bool IsSelected;
        public string Path;
        public string Name;
        public BaseEquipmentType Type;
        public GameObject Prefab;
        public BaseEquipmentItemSO ExistingSO;
    }

    [MenuItem("Tools/Customizing/Generate BaseEquipment SOs")]
    public static void ShowWindow()
    {
        var window = GetWindow<BaseEquipmentItemGenerator>("BaseEquipment Generator");
        window.minSize = new Vector2(600, 500);
    }

    private void OnEnable()
    {
        RefreshPrefabList();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("BaseEquipment Item SO Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Gloves, Outfit, Pants 폴더의 프리팹을 BaseEquipmentItemSO로 생성합니다.\n" +
            "생성된 SO를 BaseEquipmentCatalog에 할당할 수 있습니다.",
            MessageType.Info);

        EditorGUILayout.Space(10);

        // 카탈로그 참조
        _targetCatalog = (BaseEquipmentCatalogSO)EditorGUILayout.ObjectField(
            "Target Catalog", _targetCatalog, typeof(BaseEquipmentCatalogSO), false);

        EditorGUILayout.Space(5);

        // 버튼들
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Refresh", GUILayout.Width(80)))
        {
            RefreshPrefabList();
        }

        if (GUILayout.Button("Select All", GUILayout.Width(80)))
        {
            foreach (var entry in _prefabEntries)
                entry.IsSelected = true;
        }

        if (GUILayout.Button("Deselect All", GUILayout.Width(80)))
        {
            foreach (var entry in _prefabEntries)
                entry.IsSelected = false;
        }

        if (GUILayout.Button("Select New Only", GUILayout.Width(100)))
        {
            foreach (var entry in _prefabEntries)
                entry.IsSelected = entry.ExistingSO == null;
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 프리팹 목록
        EditorGUILayout.LabelField($"Prefabs ({_prefabEntries.Count})", EditorStyles.boldLabel);

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));

        BaseEquipmentType? lastType = null;

        foreach (var entry in _prefabEntries)
        {
            // 타입별 구분선
            if (lastType != entry.Type)
            {
                if (lastType != null)
                    EditorGUILayout.Space(5);

                EditorGUILayout.LabelField($"── {entry.Type} ──", EditorStyles.centeredGreyMiniLabel);
                lastType = entry.Type;
            }

            EditorGUILayout.BeginHorizontal();

            // 체크박스
            entry.IsSelected = EditorGUILayout.Toggle(entry.IsSelected, GUILayout.Width(20));

            // 이름
            EditorGUILayout.LabelField(entry.Name, GUILayout.Width(150));

            // 타입
            EditorGUILayout.LabelField(entry.Type.ToString(), GUILayout.Width(80));

            // 상태
            if (entry.ExistingSO != null)
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField("SO Exists", GUILayout.Width(80));
                GUI.color = Color.white;

                if (GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    Selection.activeObject = entry.ExistingSO;
                }
            }
            else
            {
                GUI.color = Color.yellow;
                EditorGUILayout.LabelField("New", GUILayout.Width(80));
                GUI.color = Color.white;
            }

            // 프리팹 선택
            if (GUILayout.Button("Prefab", GUILayout.Width(50)))
            {
                Selection.activeObject = entry.Prefab;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);

        // 생성 버튼
        int selectedCount = _prefabEntries.Count(e => e.IsSelected);
        GUI.enabled = selectedCount > 0;
        GUI.backgroundColor = Color.green;

        if (GUILayout.Button($"Generate {selectedCount} BaseEquipmentItemSO(s)", GUILayout.Height(35)))
        {
            GenerateSelectedSOs();
        }

        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
    }

    private void RefreshPrefabList()
    {
        _prefabEntries.Clear();

        foreach (var kvp in FolderToType)
        {
            string folderPath = Path.Combine(ITEMS_PATH, kvp.Key);
            BaseEquipmentType type = kvp.Value;

            if (!AssetDatabase.IsValidFolder(folderPath))
                continue;

            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                // 기존 SO 확인
                string soPath = Path.Combine(OUTPUT_PATH, $"{name}.asset");
                BaseEquipmentItemSO existingSO = AssetDatabase.LoadAssetAtPath<BaseEquipmentItemSO>(soPath);

                _prefabEntries.Add(new PrefabEntry
                {
                    IsSelected = false,
                    Path = path,
                    Name = name,
                    Type = type,
                    Prefab = prefab,
                    ExistingSO = existingSO
                });
            }
        }

        // 타입별, 이름별 정렬
        _prefabEntries = _prefabEntries
            .OrderBy(e => e.Type)
            .ThenBy(e => ExtractSortOrder(e.Name))
            .ToList();
    }

    private void GenerateSelectedSOs()
    {
        EnsureFolderExists(OUTPUT_PATH);

        int successCount = 0;
        List<BaseEquipmentItemSO> createdSOs = new List<BaseEquipmentItemSO>();

        foreach (var entry in _prefabEntries.Where(e => e.IsSelected))
        {
            string soPath = Path.Combine(OUTPUT_PATH, $"{entry.Name}.asset");

            // 이미 존재하면 업데이트
            BaseEquipmentItemSO itemSO = entry.ExistingSO;

            if (itemSO == null)
            {
                itemSO = ScriptableObject.CreateInstance<BaseEquipmentItemSO>();
                AssetDatabase.CreateAsset(itemSO, soPath);
            }

            // 필드 설정
            SetPrivateField(itemSO, "_itemId", $"BaseEquipment_{entry.Type}_{entry.Name}");
            SetPrivateField(itemSO, "_displayName", entry.Name);
            SetPrivateField(itemSO, "_equipmentType", entry.Type);
            SetPrivateField(itemSO, "_partPrefab", entry.Prefab);

            EditorUtility.SetDirty(itemSO);
            createdSOs.Add(itemSO);
            successCount++;

            Debug.Log($"[BaseEquipmentGenerator] Created: {entry.Name} ({entry.Type})");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        RefreshPrefabList();

        EditorUtility.DisplayDialog("Complete",
            $"{successCount} BaseEquipmentItemSO(s) generated.\n\nOutput: {OUTPUT_PATH}",
            "OK");

        // 첫 번째 생성된 SO 선택
        if (createdSOs.Count > 0)
        {
            Selection.activeObject = createdSOs[0];
        }
    }

    private int ExtractSortOrder(string name)
    {
        var match = Regex.Match(name, @"(\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
    }

    private void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        string parentFolder = Path.GetDirectoryName(folderPath);
        string newFolder = Path.GetFileName(folderPath);

        EnsureFolderExists(parentFolder);
        AssetDatabase.CreateFolder(parentFolder, newFolder);
    }
}
