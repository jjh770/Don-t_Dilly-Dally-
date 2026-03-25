#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// CustomizingItemSO의 기존 PartPrefab을 Addressables로 자동 마이그레이션하는 도구
///
/// 사용법:
/// 1. Tools > Customizing > Addressable Migrator 선택
/// 2. "Migrate All" 버튼 클릭
/// </summary>
public class CustomizingAddressableMigrator : EditorWindow
{
    private const string CATALOG_PATH = "Assets/03.Prefabs/Customizing/Catalog";
    private const string ADDRESSABLE_GROUP_NAME = "Customizing";

    private Vector2 _scrollPosition;
    private List<string> _logMessages = new();
    private List<MigrationItem> _migrationItems = new();

    private bool _createGroupIfMissing = true;
    private bool _overwriteExisting = false;

    private class MigrationItem
    {
        public CustomizingItemSO SO;
        public GameObject Prefab;
        public bool HasAddressableRef;
        public bool Selected;
        public string Status;
    }

    [MenuItem("Tools/Customizing/Addressable Migrator")]
    public static void ShowWindow()
    {
        var window = GetWindow<CustomizingAddressableMigrator>("Addressable Migrator");
        window.minSize = new Vector2(500, 400);
    }

    private void OnEnable()
    {
        ScanItems();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Customizing Addressable Migration Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // 옵션
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        _createGroupIfMissing = EditorGUILayout.Toggle("Create Group If Missing", _createGroupIfMissing);
        _overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing Refs", _overwriteExisting);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // 버튼들
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Scan Items", GUILayout.Height(30)))
        {
            ScanItems();
        }
        if (GUILayout.Button("Select All Pending", GUILayout.Height(30)))
        {
            SelectAllPending();
        }
        if (GUILayout.Button("Migrate Selected", GUILayout.Height(30)))
        {
            MigrateSelected();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 통계
        int total = _migrationItems.Count;
        int pending = _migrationItems.Count(x => !x.HasAddressableRef && x.Prefab != null);
        int done = _migrationItems.Count(x => x.HasAddressableRef);
        int noPrefab = _migrationItems.Count(x => x.Prefab == null);

        EditorGUILayout.LabelField($"Total: {total} | Pending: {pending} | Done: {done} | No Prefab: {noPrefab}");

        EditorGUILayout.Space(10);

        // 아이템 목록
        EditorGUILayout.LabelField("Migration Items", EditorStyles.boldLabel);
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200));

        foreach (var item in _migrationItems)
        {
            EditorGUILayout.BeginHorizontal();

            bool canMigrate = !item.HasAddressableRef && item.Prefab != null;
            GUI.enabled = canMigrate || _overwriteExisting;
            item.Selected = EditorGUILayout.Toggle(item.Selected, GUILayout.Width(20));
            GUI.enabled = true;

            string statusIcon = item.HasAddressableRef ? "✓" : (item.Prefab == null ? "✗" : "○");
            EditorGUILayout.LabelField(statusIcon, GUILayout.Width(20));
            EditorGUILayout.ObjectField(item.SO, typeof(CustomizingItemSO), false, GUILayout.Width(200));
            EditorGUILayout.LabelField(item.Status, GUILayout.Width(150));

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        // 로그
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(100));
        foreach (var log in _logMessages.TakeLast(20))
        {
            EditorGUILayout.LabelField(log);
        }
        EditorGUILayout.EndScrollView();
    }

    private void ScanItems()
    {
        _migrationItems.Clear();
        _logMessages.Clear();

        string[] guids = AssetDatabase.FindAssets("t:CustomizingItemSO", new[] { CATALOG_PATH });
        Log($"Found {guids.Length} CustomizingItemSO assets");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<CustomizingItemSO>(path);

            if (so == null) continue;

            var item = new MigrationItem
            {
                SO = so,
                Prefab = so.PartPrefab,
                HasAddressableRef = so.HasAddressableRef,
                Selected = false,
            };

            if (item.HasAddressableRef)
                item.Status = "Already migrated";
            else if (item.Prefab == null)
                item.Status = "No prefab";
            else
                item.Status = "Pending";

            _migrationItems.Add(item);
        }

        _migrationItems = _migrationItems
            .OrderBy(x => x.HasAddressableRef)
            .ThenBy(x => x.Prefab == null)
            .ThenBy(x => x.SO.name)
            .ToList();
    }

    private void SelectAllPending()
    {
        foreach (var item in _migrationItems)
        {
            if (!item.HasAddressableRef && item.Prefab != null)
            {
                item.Selected = true;
            }
        }
        Repaint();
    }

    private void MigrateSelected()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Log("ERROR: Addressables not initialized. Go to Window > Asset Management > Addressables > Groups and create settings.");
            return;
        }

        // 그룹 찾기 또는 생성
        var group = settings.FindGroup(ADDRESSABLE_GROUP_NAME);
        if (group == null)
        {
            if (_createGroupIfMissing)
            {
                group = settings.CreateGroup(ADDRESSABLE_GROUP_NAME, false, false, true, settings.DefaultGroup.Schemas);
                Log($"Created Addressable group: {ADDRESSABLE_GROUP_NAME}");
            }
            else
            {
                Log($"ERROR: Addressable group '{ADDRESSABLE_GROUP_NAME}' not found.");
                return;
            }
        }

        int migratedCount = 0;
        var selectedItems = _migrationItems.Where(x => x.Selected).ToList();

        foreach (var item in selectedItems)
        {
            if (item.Prefab == null) continue;

            try
            {
                // 프리팹을 Addressable로 등록
                string prefabPath = AssetDatabase.GetAssetPath(item.Prefab);
                string prefabGuid = AssetDatabase.AssetPathToGUID(prefabPath);

                var entry = settings.FindAssetEntry(prefabGuid);
                if (entry == null)
                {
                    entry = settings.CreateOrMoveEntry(prefabGuid, group, false, false);
                    entry.address = item.Prefab.name;
                    Log($"Registered Addressable: {item.Prefab.name}");
                }

                // SO에 AssetReference 설정
                SetAssetReference(item.SO, prefabGuid);

                item.HasAddressableRef = true;
                item.Status = "Migrated";
                item.Selected = false;
                migratedCount++;
            }
            catch (System.Exception e)
            {
                Log($"ERROR: {item.SO.name} - {e.Message}");
                item.Status = "Error";
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Log($"Migration complete: {migratedCount} items");
        ScanItems();
    }

    private void SetAssetReference(CustomizingItemSO so, string prefabGuid)
    {
        var serializedObject = new SerializedObject(so);
        var refProperty = serializedObject.FindProperty("_partPrefabRef");

        if (refProperty != null)
        {
            // AssetReference의 m_AssetGUID 설정
            var guidProperty = refProperty.FindPropertyRelative("m_AssetGUID");
            if (guidProperty != null)
            {
                guidProperty.stringValue = prefabGuid;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(so);
            }
        }
    }

    private void Log(string message)
    {
        _logMessages.Add($"[{System.DateTime.Now:HH:mm:ss}] {message}");
        Debug.Log($"[AddressableMigrator] {message}");
        Repaint();
    }
}
#endif
