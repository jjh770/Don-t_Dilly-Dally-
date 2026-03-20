using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// 캐릭터 파츠 프리팹에서 SkinnedMeshRenderer만 추출하여 새 프리팹으로 저장하는 에디터 윈도우
///
/// 사용법:
/// 1. 메뉴에서 Tools > Customizing > Skinned Mesh Extractor 선택
/// 2. 소스/타겟 경로 확인 후 "Extract All Meshes" 버튼 클릭
/// 3. 결과 프리팹은 타겟 경로에 폴더 구조 유지하여 저장됨
/// </summary>
public class SkinnedMeshExtractorWindow : EditorWindow
{
    // ===== 경로 설정 (프로젝트에 맞게 수정) =====
    private const string DEFAULT_SOURCE_PATH = "Assets/Characters/Meshes";
    private const string DEFAULT_TARGET_PATH = "Assets/03.Prefabs/Customizing/Items";

    private string _sourcePath = DEFAULT_SOURCE_PATH;
    private string _targetPath = DEFAULT_TARGET_PATH;

    private Vector2 _scrollPosition;
    private List<string> _logMessages = new List<string>();

    // 통계
    private int _processedCount = 0;
    private int _successCount = 0;
    private int _skipCount = 0;
    private int _errorCount = 0;

    [MenuItem("Tools/Customizing/Skinned Mesh Extractor")]
    public static void ShowWindow()
    {
        var window = GetWindow<SkinnedMeshExtractorWindow>("Mesh Extractor");
        window.minSize = new Vector2(500, 400);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Skinned Mesh Extractor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "원본 프리팹에서 SkinnedMeshRenderer가 붙은 오브젝트만 추출하여 새 프리팹으로 저장합니다.\n" +
            "새 프리팹에는 SkinnedMeshBoneRemapper 컴포넌트가 자동으로 추가되어,\n" +
            "CustomizingPlayer 하위에 배치하면 자동으로 본이 재매핑됩니다.",
            MessageType.Info);

        EditorGUILayout.Space(10);

        // 경로 설정
        EditorGUILayout.LabelField("Path Settings", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        _sourcePath = EditorGUILayout.TextField("Source Path", _sourcePath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string selected = EditorUtility.OpenFolderPanel("Select Source Folder", "Assets", "");
            if (!string.IsNullOrEmpty(selected))
            {
                _sourcePath = "Assets" + selected.Replace(Application.dataPath, "");
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        _targetPath = EditorGUILayout.TextField("Target Path", _targetPath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string selected = EditorUtility.OpenFolderPanel("Select Target Folder", "Assets", "");
            if (!string.IsNullOrEmpty(selected))
            {
                _targetPath = "Assets" + selected.Replace(Application.dataPath, "");
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 버튼들
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Extract All Meshes", GUILayout.Height(30)))
        {
            ExtractAllMeshes();
        }
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("Preview (Dry Run)", GUILayout.Height(30)))
        {
            PreviewExtraction();
        }

        if (GUILayout.Button("Clear Log", GUILayout.Height(30)))
        {
            _logMessages.Clear();
            _processedCount = _successCount = _skipCount = _errorCount = 0;
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 통계
        if (_processedCount > 0)
        {
            EditorGUILayout.LabelField($"Processed: {_processedCount} | Success: {_successCount} | Skipped: {_skipCount} | Errors: {_errorCount}");
        }

        // 로그 출력
        EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));

        foreach (var msg in _logMessages)
        {
            if (msg.StartsWith("[ERROR]"))
                EditorGUILayout.HelpBox(msg, MessageType.Error);
            else if (msg.StartsWith("[WARN]"))
                EditorGUILayout.HelpBox(msg, MessageType.Warning);
            else if (msg.StartsWith("[SUCCESS]"))
                EditorGUILayout.LabelField(msg, EditorStyles.wordWrappedLabel);
            else
                EditorGUILayout.LabelField(msg, EditorStyles.wordWrappedMiniLabel);
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 실제 추출 실행 (프리팹 생성)
    /// </summary>
    private void ExtractAllMeshes()
    {
        _logMessages.Clear();
        _processedCount = _successCount = _skipCount = _errorCount = 0;

        if (!AssetDatabase.IsValidFolder(_sourcePath))
        {
            Log("[ERROR] Source path does not exist: " + _sourcePath);
            return;
        }

        // 타겟 폴더 생성
        EnsureFolderExists(_targetPath);

        // 모든 프리팹 검색
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { _sourcePath });
        Log($"Found {prefabGuids.Length} prefabs in {_sourcePath}");

        try
        {
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);

                EditorUtility.DisplayProgressBar(
                    "Extracting Meshes",
                    $"Processing: {Path.GetFileName(prefabPath)}",
                    (float)i / prefabGuids.Length);

                ProcessPrefab(prefabPath, false);
                _processedCount++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        Log($"\n===== Extraction Complete =====");
        Log($"Total: {_processedCount} | Success: {_successCount} | Skipped: {_skipCount} | Errors: {_errorCount}");
    }

    /// <summary>
    /// 미리보기 (실제 파일 생성 없이 로그만 출력)
    /// </summary>
    private void PreviewExtraction()
    {
        _logMessages.Clear();
        _processedCount = _successCount = _skipCount = _errorCount = 0;

        if (!AssetDatabase.IsValidFolder(_sourcePath))
        {
            Log("[ERROR] Source path does not exist: " + _sourcePath);
            return;
        }

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { _sourcePath });
        Log($"[Preview Mode] Found {prefabGuids.Length} prefabs");

        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            ProcessPrefab(prefabPath, true);
            _processedCount++;
        }

        Log($"\n===== Preview Complete =====");
        Log($"Would process: {_processedCount} | Would create: {_successCount} | Would skip: {_skipCount}");
    }

    /// <summary>
    /// 단일 프리팹 처리
    /// </summary>
    private void ProcessPrefab(string prefabPath, bool dryRun)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Log($"[ERROR] Failed to load prefab: {prefabPath}");
            _errorCount++;
            return;
        }

        // SkinnedMeshRenderer 찾기
        SkinnedMeshRenderer[] renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        if (renderers.Length == 0)
        {
            Log($"[SKIP] No SkinnedMeshRenderer found: {prefabPath}");
            _skipCount++;
            return;
        }

        // 상대 경로 계산 (폴더 구조 유지)
        string relativePath = prefabPath.Replace(_sourcePath, "").TrimStart('/');
        string targetPrefabPath = Path.Combine(_targetPath, relativePath);
        string targetFolder = Path.GetDirectoryName(targetPrefabPath);

        if (dryRun)
        {
            Log($"[Preview] Would create: {targetPrefabPath}");
            Log($"  - Found {renderers.Length} SkinnedMeshRenderer(s)");
            foreach (var r in renderers)
            {
                Log($"    - {r.name} (bones: {r.bones?.Length ?? 0})");
            }
            _successCount++;
            return;
        }

        // 폴더 생성
        EnsureFolderExists(targetFolder);

        // 새 프리팹 생성
        try
        {
            CreateExtractedPrefab(prefab, renderers, targetPrefabPath);
            Log($"[SUCCESS] Created: {targetPrefabPath}");
            _successCount++;
        }
        catch (System.Exception e)
        {
            Log($"[ERROR] Failed to create prefab: {targetPrefabPath}\n  {e.Message}");
            _errorCount++;
        }
    }

    /// <summary>
    /// SkinnedMeshRenderer만 포함하는 새 프리팹 생성
    /// </summary>
    private void CreateExtractedPrefab(GameObject sourcePrefab, SkinnedMeshRenderer[] renderers, string savePath)
    {
        // 루트 오브젝트 생성
        GameObject root = new GameObject(sourcePrefab.name);
        root.layer = 7; // 레이어 7번으로 설정

        // SkinnedMeshBoneRemapper 컴포넌트 추가 (런타임 본 재매핑용)
        var remapper = root.AddComponent<SkinnedMeshBoneRemapper>();

        // 각 SkinnedMeshRenderer 복제
        List<SkinnedMeshRenderer> newRenderers = new List<SkinnedMeshRenderer>();

        foreach (var sourceRenderer in renderers)
        {
            // 새 자식 오브젝트 생성
            GameObject meshObj = new GameObject(sourceRenderer.name);
            meshObj.layer = 7; // 레이어 7번으로 설정
            meshObj.transform.SetParent(root.transform);
            meshObj.transform.localPosition = Vector3.zero;
            meshObj.transform.localRotation = Quaternion.identity;
            meshObj.transform.localScale = Vector3.one;

            // SkinnedMeshRenderer 복제
            SkinnedMeshRenderer newRenderer = meshObj.AddComponent<SkinnedMeshRenderer>();

            // 메쉬와 머티리얼 복사
            newRenderer.sharedMesh = sourceRenderer.sharedMesh;
            newRenderer.sharedMaterials = sourceRenderer.sharedMaterials;

            // 본 이름 저장 (나중에 재매핑에 사용)
            // bones는 null로 두고, BoneRemapper가 런타임에 재매핑

            // 기타 설정 복사
            newRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
            newRenderer.receiveShadows = sourceRenderer.receiveShadows;
            newRenderer.lightProbeUsage = sourceRenderer.lightProbeUsage;
            newRenderer.reflectionProbeUsage = sourceRenderer.reflectionProbeUsage;
            newRenderer.quality = sourceRenderer.quality;
            newRenderer.updateWhenOffscreen = sourceRenderer.updateWhenOffscreen;

            // 원본 본 이름 저장을 위한 BoneInfo 컴포넌트 추가
            var boneInfo = meshObj.AddComponent<SkinnedMeshBoneInfo>();
            boneInfo.SaveBoneNames(sourceRenderer);

            newRenderers.Add(newRenderer);
        }

        // 프리팹 저장
        PrefabUtility.SaveAsPrefabAsset(root, savePath);

        // 임시 오브젝트 삭제
        DestroyImmediate(root);
    }

    /// <summary>
    /// 폴더가 없으면 생성
    /// </summary>
    private void EnsureFolderExists(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath) || AssetDatabase.IsValidFolder(folderPath))
            return;

        string parentFolder = Path.GetDirectoryName(folderPath);
        string newFolder = Path.GetFileName(folderPath);

        EnsureFolderExists(parentFolder);

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder(parentFolder, newFolder);
        }
    }

    private void Log(string message)
    {
        _logMessages.Add(message);
        Debug.Log("[MeshExtractor] " + message);
    }
}
