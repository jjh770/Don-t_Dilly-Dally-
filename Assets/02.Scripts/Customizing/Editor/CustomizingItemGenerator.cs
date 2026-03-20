using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Items 폴더의 프리팹들을 CustomizingItemSO로 자동 생성하는 에디터
/// Body + Ears를 합쳐서 SkinColor 프리팹으로 생성
/// </summary>
public class CustomizingItemGenerator : EditorWindow
{
    private const string ITEMS_PATH = "Assets/03.Prefabs/Customizing/Items";
    private const string CATALOG_PATH = "Assets/03.Prefabs/Customizing/Catalog";
    private const string SKINCOLOR_PREFAB_PATH = "Assets/03.Prefabs/Customizing/Items/SkinColor";

    // 폴더명 -> CustomizingType 매핑
    private static readonly Dictionary<string, CustomizingType> FolderToType = new Dictionary<string, CustomizingType>
    {
        { "Body", CustomizingType.SkinColor },      // Body + Ears 합쳐서 SkinColor
        { "Ears", CustomizingType.SkinColor },      // Body와 함께 처리
        { "Hat", CustomizingType.Hat },
        { "Hairstyle", CustomizingType.HairStyle },
        { "Faces", CustomizingType.Faces },
        { "Face Accessories", CustomizingType.FaceAccessory },
        { "Glasses", CustomizingType.Glasses },
        { "Shoes", CustomizingType.Shoes },
        { "Costumes", CustomizingType.Costumes },
    };

    private Vector2 _scrollPosition;
    private List<string> _logMessages = new List<string>();

    [MenuItem("Tools/Customizing/Generate Item SOs")]
    public static void ShowWindow()
    {
        var window = GetWindow<CustomizingItemGenerator>("Item SO Generator");
        window.minSize = new Vector2(500, 400);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Customizing Item SO Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Items 폴더의 프리팹들을 CustomizingItemSO로 자동 생성합니다.\n" +
            "- Body + Ears → SkinColor (합쳐서 새 프리팹 생성)\n" +
            "- 폴더명에 따라 CustomizingType 자동 설정",
            MessageType.Info);

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField($"Source: {ITEMS_PATH}");
        EditorGUILayout.LabelField($"Target: {CATALOG_PATH}");

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("1. Generate SkinColor Prefabs\n(Body + Ears 합치기)", GUILayout.Height(40)))
        {
            GenerateSkinColorPrefabs();
        }
        GUI.backgroundColor = Color.white;

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("2. Generate All Item SOs", GUILayout.Height(40)))
        {
            GenerateAllItemSOs();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Clear Log"))
        {
            _logMessages.Clear();
        }

        EditorGUILayout.Space(10);

        // 로그 출력
        EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));

        foreach (var msg in _logMessages)
        {
            if (msg.StartsWith("[ERROR]"))
                EditorGUILayout.HelpBox(msg, MessageType.Error);
            else if (msg.StartsWith("[WARN]"))
                EditorGUILayout.HelpBox(msg, MessageType.Warning);
            else
                EditorGUILayout.LabelField(msg, EditorStyles.wordWrappedMiniLabel);
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// Body + Ears를 합쳐서 SkinColor 프리팹 생성
    /// </summary>
    private void GenerateSkinColorPrefabs()
    {
        _logMessages.Clear();
        Log("=== Generating SkinColor Prefabs ===");

        string bodyPath = Path.Combine(ITEMS_PATH, "Body");
        string earsPath = Path.Combine(ITEMS_PATH, "Ears");

        if (!AssetDatabase.IsValidFolder(bodyPath) || !AssetDatabase.IsValidFolder(earsPath))
        {
            Log("[ERROR] Body or Ears folder not found");
            return;
        }

        // SkinColor 폴더 생성
        EnsureFolderExists(SKINCOLOR_PREFAB_PATH);

        // Body 프리팹 목록
        var bodyPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { bodyPath })
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .ToList();

        // Ears 프리팹 목록
        var earsPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { earsPath })
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .ToDictionary(path => ExtractNumber(Path.GetFileNameWithoutExtension(path)), path => path);

        int successCount = 0;

        foreach (string bodyPrefabPath in bodyPrefabs)
        {
            string bodyName = Path.GetFileNameWithoutExtension(bodyPrefabPath);
            string number = ExtractNumber(bodyName);

            if (string.IsNullOrEmpty(number))
            {
                Log($"[WARN] Cannot extract number from: {bodyName}");
                continue;
            }

            // 매칭되는 Ears 찾기
            if (!earsPrefabs.TryGetValue(number, out string earsPrefabPath))
            {
                Log($"[WARN] No matching Ears for: {bodyName}");
                continue;
            }

            // 합친 프리팹 생성
            string skinColorName = $"SkinColor_{number}";
            string savePath = Path.Combine(SKINCOLOR_PREFAB_PATH, $"{skinColorName}.prefab");

            if (CreateCombinedPrefab(bodyPrefabPath, earsPrefabPath, savePath, skinColorName))
            {
                Log($"[SUCCESS] Created: {skinColorName}");
                successCount++;
            }
        }

        AssetDatabase.Refresh();
        Log($"\n=== Complete: {successCount} SkinColor prefabs created ===");
    }

    /// <summary>
    /// Body와 Ears를 합친 프리팹 생성
    /// </summary>
    private bool CreateCombinedPrefab(string bodyPath, string earsPath, string savePath, string prefabName)
    {
        try
        {
            GameObject bodyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bodyPath);
            GameObject earsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(earsPath);

            if (bodyPrefab == null || earsPrefab == null)
            {
                Log($"[ERROR] Failed to load prefabs");
                return false;
            }

            // 루트 오브젝트 생성
            GameObject root = new GameObject(prefabName);
            root.layer = 7;

            // SkinnedMeshBoneRemapper 추가
            root.AddComponent<SkinnedMeshBoneRemapper>();

            // Body 복제
            CopyChildrenWithComponents(bodyPrefab, root);

            // Ears 복제
            CopyChildrenWithComponents(earsPrefab, root);

            // 프리팹 저장
            PrefabUtility.SaveAsPrefabAsset(root, savePath);
            DestroyImmediate(root);

            return true;
        }
        catch (System.Exception e)
        {
            Log($"[ERROR] {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 소스 프리팹의 자식들을 타겟으로 복사
    /// </summary>
    private void CopyChildrenWithComponents(GameObject source, GameObject target)
    {
        foreach (Transform child in source.transform)
        {
            // SkinnedMeshRenderer가 있는 자식만 복사
            var smr = child.GetComponent<SkinnedMeshRenderer>();
            if (smr != null)
            {
                GameObject copy = new GameObject(child.name);
                copy.layer = 7;
                copy.transform.SetParent(target.transform);
                copy.transform.localPosition = Vector3.zero;
                copy.transform.localRotation = Quaternion.identity;
                copy.transform.localScale = Vector3.one;

                // SkinnedMeshRenderer 복사
                var newSmr = copy.AddComponent<SkinnedMeshRenderer>();
                newSmr.sharedMesh = smr.sharedMesh;
                newSmr.sharedMaterials = smr.sharedMaterials;
                newSmr.shadowCastingMode = smr.shadowCastingMode;
                newSmr.receiveShadows = smr.receiveShadows;

                // BoneInfo 복사
                var boneInfo = child.GetComponent<SkinnedMeshBoneInfo>();
                if (boneInfo != null)
                {
                    var newBoneInfo = copy.AddComponent<SkinnedMeshBoneInfo>();
                    EditorUtility.CopySerialized(boneInfo, newBoneInfo);
                }
            }
        }
    }

    /// <summary>
    /// 모든 Item SO 생성
    /// </summary>
    private void GenerateAllItemSOs()
    {
        _logMessages.Clear();
        Log("=== Generating Item SOs ===");

        EnsureFolderExists(CATALOG_PATH);

        int totalCount = 0;

        // SkinColor (합쳐진 프리팹)
        if (AssetDatabase.IsValidFolder(SKINCOLOR_PREFAB_PATH))
        {
            totalCount += GenerateItemSOsForFolder(SKINCOLOR_PREFAB_PATH, "SkinColor", CustomizingType.SkinColor);
        }
        else
        {
            Log("[WARN] SkinColor folder not found. Run 'Generate SkinColor Prefabs' first.");
        }

        // 나머지 폴더들
        foreach (var kvp in FolderToType)
        {
            string folderName = kvp.Key;
            CustomizingType type = kvp.Value;

            // Body, Ears는 SkinColor로 이미 처리됨
            if (folderName == "Body" || folderName == "Ears")
                continue;

            string folderPath = Path.Combine(ITEMS_PATH, folderName);
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                totalCount += GenerateItemSOsForFolder(folderPath, folderName, type);
            }
            else
            {
                Log($"[WARN] Folder not found: {folderName}");
            }
        }

        AssetDatabase.Refresh();
        Log($"\n=== Complete: {totalCount} Item SOs created ===");
    }

    /// <summary>
    /// 특정 폴더의 프리팹들에 대해 Item SO 생성
    /// </summary>
    private int GenerateItemSOsForFolder(string folderPath, string categoryName, CustomizingType type)
    {
        var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        int count = 0;

        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);

            // SO 저장 경로
            string soPath = Path.Combine(CATALOG_PATH, $"{prefabName}.asset");

            // 이미 존재하면 스킵
            if (AssetDatabase.LoadAssetAtPath<CustomizingItemSO>(soPath) != null)
            {
                Log($"[SKIP] Already exists: {prefabName}");
                continue;
            }

            // Item SO 생성
            var itemSO = ScriptableObject.CreateInstance<CustomizingItemSO>();

            // 리플렉션으로 private 필드 설정
            SetPrivateField(itemSO, "_itemId", $"{type}_{prefabName}");
            SetPrivateField(itemSO, "_displayName", prefabName);
            SetPrivateField(itemSO, "_customizingType", type);
            SetPrivateField(itemSO, "_partPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
            SetPrivateField(itemSO, "_isDefault", prefabName.EndsWith("_01"));
            SetPrivateField(itemSO, "_sortOrder", ExtractSortOrder(prefabName));

            AssetDatabase.CreateAsset(itemSO, soPath);
            Log($"[SUCCESS] Created: {prefabName} ({type})");
            count++;
        }

        return count;
    }

    /// <summary>
    /// 파일명에서 숫자 추출 (Body_01 → 01)
    /// </summary>
    private string ExtractNumber(string name)
    {
        var match = Regex.Match(name, @"(\d+)$");
        return match.Success ? match.Groups[1].Value : "";
    }

    /// <summary>
    /// 정렬 순서 추출
    /// </summary>
    private int ExtractSortOrder(string name)
    {
        var match = Regex.Match(name, @"(\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    /// <summary>
    /// 리플렉션으로 private 필드 설정
    /// </summary>
    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(obj, value);
        }
    }

    /// <summary>
    /// 폴더 생성
    /// </summary>
    private void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        string parentFolder = Path.GetDirectoryName(folderPath);
        string newFolder = Path.GetFileName(folderPath);

        EnsureFolderExists(parentFolder);
        AssetDatabase.CreateFolder(parentFolder, newFolder);
    }

    private void Log(string message)
    {
        _logMessages.Add(message);
        Debug.Log("[ItemGenerator] " + message);
    }
}
