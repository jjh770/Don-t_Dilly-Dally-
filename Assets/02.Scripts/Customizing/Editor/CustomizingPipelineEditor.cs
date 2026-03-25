using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// 커스터마이징 파츠 추출 → BoneRemapper 적용 → SO 자동 생성까지 한 번에 처리하는 통합 에디터
///
/// 사용법:
/// 1. Tools > Customizing > Pipeline Editor 선택
/// 2. "Run Full Pipeline" 버튼 클릭
/// </summary>
public class CustomizingPipelineEditor : EditorWindow
{
    // ===== 경로 설정 =====
    private const string ITEMS_PATH = "Assets/03.Prefabs/Customizing/Items";
    private const string CATALOG_PATH = "Assets/03.Prefabs/Customizing/Catalog";
    private const string SKINCOLOR_PATH = "Assets/03.Prefabs/Customizing/Items/SkinColor";
    private const string BASE_EQUIPMENT_PATH = "Assets/03.Prefabs/Customizing/Catalog/BaseEquipment";

    // 폴더명 → BaseEquipmentType 매핑
    private static readonly Dictionary<string, BaseEquipmentType> FolderToBaseEquipment = new Dictionary<string, BaseEquipmentType>(System.StringComparer.OrdinalIgnoreCase)
    {
        { "Outfit", BaseEquipmentType.Outfit },
        { "Gloves", BaseEquipmentType.Gloves },
        { "Pants", BaseEquipmentType.Pants },
    };

    // 폴더명 → CustomizingType 매핑 (대소문자 무시)
    private static readonly Dictionary<string, CustomizingType> FolderToType = new Dictionary<string, CustomizingType>(System.StringComparer.OrdinalIgnoreCase)
    {
        { "SkinColor", CustomizingType.SkinColor },
        { "Body", CustomizingType.SkinColor },
        { "Ears", CustomizingType.SkinColor },
        { "Hat", CustomizingType.Hat },
        { "Hats", CustomizingType.Hat },
        { "Hairstyle", CustomizingType.HairStyle },
        { "Hairstyles", CustomizingType.HairStyle },
        { "Hair", CustomizingType.HairStyle },
        { "Faces", CustomizingType.Faces },
        { "Face", CustomizingType.Faces },
        { "Face Accessories", CustomizingType.FaceAccessory },
        { "FaceAccessory", CustomizingType.FaceAccessory },
        { "FaceAccessories", CustomizingType.FaceAccessory },
        { "Glasses", CustomizingType.Glasses },
        { "Shoes", CustomizingType.Shoes },
        { "Costumes", CustomizingType.Costumes },
        { "Costume", CustomizingType.Costumes },
        { "Outfit", CustomizingType.Costumes },
        { "Outfits", CustomizingType.Costumes },
        { "Outwear", CustomizingType.Costumes },
        { "Pants", CustomizingType.Costumes },
        { "Shorts", CustomizingType.Costumes },
        { "Gloves", CustomizingType.FaceAccessory },
        { "Socks", CustomizingType.Shoes },
    };

    // 프리팹 이름 패턴 → CustomizingType 매핑
    private static readonly Dictionary<string, CustomizingType> PrefabNameToType = new Dictionary<string, CustomizingType>(System.StringComparer.OrdinalIgnoreCase)
    {
        { "SkinColor", CustomizingType.SkinColor },
        { "Body", CustomizingType.SkinColor },
        { "Ears", CustomizingType.SkinColor },
        { "Hat", CustomizingType.Hat },
        { "Hair", CustomizingType.HairStyle },
        { "Hairstyle", CustomizingType.HairStyle },
        { "Face", CustomizingType.Faces },
        { "Emotion", CustomizingType.Faces },
        { "Beard", CustomizingType.FaceAccessory },
        { "Mustache", CustomizingType.FaceAccessory },
        { "Mask", CustomizingType.FaceAccessory },
        { "Earrings", CustomizingType.FaceAccessory },
        { "Headphones", CustomizingType.FaceAccessory },
        { "Pacifier", CustomizingType.FaceAccessory },
        { "Clown_nose", CustomizingType.FaceAccessory },
        { "Glasses", CustomizingType.Glasses },
        { "Bandage", CustomizingType.Glasses },
        { "Shoes", CustomizingType.Shoes },
        { "Socks", CustomizingType.Shoes },
        { "Costume", CustomizingType.Costumes },
        { "Outfit", CustomizingType.Costumes },
        { "Pants", CustomizingType.Costumes },
        { "Shorts", CustomizingType.Costumes },
        { "Gloves", CustomizingType.FaceAccessory },
    };

    // 제외할 폴더 (Body, Ears는 SkinColor로 병합)
    private static readonly HashSet<string> ExcludedFolders = new HashSet<string>
    {
        "Body", "Ears", "Player", "Customizing"
    };

    private Vector2 _scrollPosition;
    private List<string> _logMessages = new List<string>();

    // 옵션
    private bool _overwriteExisting = false;
    private bool _generateSkinColor = true;
    private bool _generateSOs = true;
    private bool _linkPrefabsToSOs = true;

    // 통계
    private int _prefabsCreated = 0;
    private int _sosCreated = 0;
    private int _sosUpdated = 0;
    private int _skipped = 0;
    private int _errors = 0;

    [MenuItem("Tools/Customizing/Pipeline Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<CustomizingPipelineEditor>("Customizing Pipeline");
        window.minSize = new Vector2(550, 500);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Customizing Pipeline Editor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "커스터마이징 파츠 처리 파이프라인:\n" +
            "1. Body + Ears → SkinColor 프리팹 병합\n" +
            "2. 모든 파츠 프리팹에 BoneRemapper/BoneInfo 확인\n" +
            "3. CustomizingItemSO 자동 생성 및 Part Prefab 연결",
            MessageType.Info);

        EditorGUILayout.Space(10);

        // 경로 표시
        EditorGUILayout.LabelField("Paths", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Items: {ITEMS_PATH}");
        EditorGUILayout.LabelField($"Catalog: {CATALOG_PATH}");

        EditorGUILayout.Space(10);

        // 옵션
        EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
        _generateSkinColor = EditorGUILayout.Toggle("1. Generate SkinColor Prefabs", _generateSkinColor);
        _generateSOs = EditorGUILayout.Toggle("2. Generate Item SOs", _generateSOs);
        _linkPrefabsToSOs = EditorGUILayout.Toggle("3. Link Prefabs to SOs", _linkPrefabsToSOs);
        _overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing Assets", _overwriteExisting);

        EditorGUILayout.Space(10);

        // 버튼들
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Run Full Pipeline", GUILayout.Height(40)))
        {
            RunFullPipeline();
        }
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("Preview", GUILayout.Height(40)))
        {
            PreviewPipeline();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 개별 실행 버튼
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("1. SkinColor Only", GUILayout.Height(25)))
        {
            ResetStats();
            GenerateSkinColorPrefabs();
            ShowSummary();
        }

        GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button("2. Generate SOs Only", GUILayout.Height(25)))
        {
            ResetStats();
            GenerateAllItemSOs();
            ShowSummary();
        }

        GUI.backgroundColor = Color.magenta;
        if (GUILayout.Button("3. Link Prefabs Only", GUILayout.Height(25)))
        {
            ResetStats();
            LinkAllPrefabsToSOs();
            ShowSummary();
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // BaseEquipment SO 생성 버튼
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.5f, 0.8f, 1f); // Light Blue
        if (GUILayout.Button("Generate BaseEquipment SOs", GUILayout.Height(25)))
        {
            ResetStats();
            GenerateBaseEquipmentSOs();
            ShowSummary();
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 추가 도구 버튼들 - 첫 번째 줄
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Fix Missing BoneInfo", GUILayout.Height(25)))
        {
            ResetStats();
            FixMissingBoneInfoInPrefabs();
            ShowSummary();
        }

        GUI.backgroundColor = new Color(1f, 0.5f, 0f); // Orange
        if (GUILayout.Button("Fix SO Types", GUILayout.Height(25)))
        {
            ResetStats();
            FixSOCustomizingTypes();
            ShowSummary();
        }

        EditorGUILayout.EndHorizontal();

        // 추가 도구 버튼들 - 두 번째 줄
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = Color.white;
        if (GUILayout.Button("Clear Log", GUILayout.Height(25)))
        {
            _logMessages.Clear();
            ResetStats();
        }

        if (GUILayout.Button("List All Prefabs", GUILayout.Height(25)))
        {
            ListAllPrefabsWithTypes();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 통계
        if (_prefabsCreated > 0 || _sosCreated > 0 || _errors > 0)
        {
            EditorGUILayout.LabelField($"Prefabs: {_prefabsCreated} | SOs Created: {_sosCreated} | Updated: {_sosUpdated} | Skipped: {_skipped} | Errors: {_errors}");
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
            {
                var style = new GUIStyle(EditorStyles.label) { richText = true };
                EditorGUILayout.LabelField($"<color=green>{msg}</color>", style);
            }
            else
                EditorGUILayout.LabelField(msg, EditorStyles.wordWrappedMiniLabel);
        }

        EditorGUILayout.EndScrollView();
    }

    private void ResetStats()
    {
        _prefabsCreated = 0;
        _sosCreated = 0;
        _sosUpdated = 0;
        _skipped = 0;
        _errors = 0;
    }

    private void ShowSummary()
    {
        Log($"\n===== Summary =====");
        Log($"Prefabs Created: {_prefabsCreated}");
        Log($"SOs Created: {_sosCreated}");
        Log($"SOs Updated: {_sosUpdated}");
        Log($"Skipped: {_skipped}");
        Log($"Errors: {_errors}");
    }

    /// <summary>
    /// 전체 파이프라인 실행
    /// </summary>
    private void RunFullPipeline()
    {
        _logMessages.Clear();
        ResetStats();

        Log("===== Starting Full Pipeline =====\n");

        try
        {
            // Step 1: SkinColor 프리팹 생성
            if (_generateSkinColor)
            {
                Log("--- Step 1: Generate SkinColor Prefabs ---");
                GenerateSkinColorPrefabs();
            }

            // Step 2: Item SO 생성
            if (_generateSOs)
            {
                Log("\n--- Step 2: Generate Item SOs ---");
                GenerateAllItemSOs();
            }

            // Step 3: 프리팹 연결
            if (_linkPrefabsToSOs)
            {
                Log("\n--- Step 3: Link Prefabs to SOs ---");
                LinkAllPrefabsToSOs();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ShowSummary();
            Log("\n===== Pipeline Complete =====");
        }
        catch (System.Exception e)
        {
            Log($"[ERROR] Pipeline failed: {e.Message}");
            _errors++;
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    /// <summary>
    /// 미리보기
    /// </summary>
    private void PreviewPipeline()
    {
        _logMessages.Clear();
        Log("===== Preview Mode =====\n");

        // SkinColor 미리보기
        string bodyPath = Path.Combine(ITEMS_PATH, "Body");
        string earsPath = Path.Combine(ITEMS_PATH, "Ears");

        if (AssetDatabase.IsValidFolder(bodyPath) && AssetDatabase.IsValidFolder(earsPath))
        {
            var bodyPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { bodyPath });
            Log($"[Preview] Would create {bodyPrefabs.Length} SkinColor prefabs from Body + Ears");
        }

        // SO 미리보기
        int totalPrefabs = 0;
        foreach (var kvp in FolderToType)
        {
            if (ExcludedFolders.Contains(kvp.Key)) continue;

            string folderPath = Path.Combine(ITEMS_PATH, kvp.Key);
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
                if (prefabs.Length > 0)
                {
                    Log($"[Preview] {kvp.Key}: {prefabs.Length} prefabs → {kvp.Value}");
                    totalPrefabs += prefabs.Length;
                }
            }
        }

        Log($"\n[Preview] Total: ~{totalPrefabs} Item SOs would be created/updated");
    }

    #region Step 1: SkinColor Prefabs

    /// <summary>
    /// Body + Ears를 합쳐서 SkinColor 프리팹 생성
    /// </summary>
    private void GenerateSkinColorPrefabs()
    {
        string bodyPath = Path.Combine(ITEMS_PATH, "Body");
        string earsPath = Path.Combine(ITEMS_PATH, "Ears");

        if (!AssetDatabase.IsValidFolder(bodyPath))
        {
            Log("[WARN] Body folder not found, skipping SkinColor generation");
            return;
        }

        if (!AssetDatabase.IsValidFolder(earsPath))
        {
            Log("[WARN] Ears folder not found, skipping SkinColor generation");
            return;
        }

        EnsureFolderExists(SKINCOLOR_PATH);

        // Body 프리팹 목록
        var bodyPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { bodyPath })
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .ToList();

        // Ears 프리팹을 번호로 인덱싱
        var earsPrefabsByNumber = AssetDatabase.FindAssets("t:Prefab", new[] { earsPath })
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .ToDictionary(path => ExtractNumber(Path.GetFileNameWithoutExtension(path)), path => path);

        foreach (string bodyPrefabPath in bodyPrefabs)
        {
            string bodyName = Path.GetFileNameWithoutExtension(bodyPrefabPath);
            string number = ExtractNumber(bodyName);

            if (string.IsNullOrEmpty(number))
            {
                Log($"[WARN] Cannot extract number from: {bodyName}");
                continue;
            }

            string skinColorName = $"SkinColor_{number}";
            string savePath = Path.Combine(SKINCOLOR_PATH, $"{skinColorName}.prefab");

            // 이미 존재하고 덮어쓰기 비활성화면 스킵
            if (!_overwriteExisting && File.Exists(savePath.Replace("Assets", Application.dataPath)))
            {
                _skipped++;
                continue;
            }

            // 매칭되는 Ears 찾기
            earsPrefabsByNumber.TryGetValue(number, out string earsPrefabPath);

            if (CreateSkinColorPrefab(bodyPrefabPath, earsPrefabPath, savePath, skinColorName))
            {
                Log($"[SUCCESS] Created: {skinColorName}");
                _prefabsCreated++;
            }
        }

        AssetDatabase.Refresh();
    }

    /// <summary>
    /// Body(필수) + Ears(선택)를 합친 SkinColor 프리팹 생성
    /// </summary>
    private bool CreateSkinColorPrefab(string bodyPath, string earsPath, string savePath, string prefabName)
    {
        GameObject bodyInstance = null;
        GameObject earsInstance = null;

        try
        {
            GameObject bodyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bodyPath);
            if (bodyPrefab == null)
            {
                Log($"[ERROR] Failed to load body prefab: {bodyPath}");
                _errors++;
                return false;
            }

            // 프리팹을 임시로 인스턴스화 (bones 참조를 유효하게 만들기 위해)
            bodyInstance = PrefabUtility.InstantiatePrefab(bodyPrefab) as GameObject;
            if (bodyInstance == null)
            {
                bodyInstance = Object.Instantiate(bodyPrefab);
            }

            // 루트 오브젝트 생성
            GameObject root = new GameObject(prefabName);
            root.layer = 7;

            // SkinnedMeshBoneRemapper 추가
            root.AddComponent<SkinnedMeshBoneRemapper>();

            // Body 복제 (인스턴스화된 오브젝트에서)
            CopySkinnedMeshChildren(bodyInstance, root);

            // Ears 복제 (있는 경우)
            if (!string.IsNullOrEmpty(earsPath))
            {
                GameObject earsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(earsPath);
                if (earsPrefab != null)
                {
                    earsInstance = PrefabUtility.InstantiatePrefab(earsPrefab) as GameObject;
                    if (earsInstance == null)
                    {
                        earsInstance = Object.Instantiate(earsPrefab);
                    }
                    CopySkinnedMeshChildren(earsInstance, root);
                }
            }

            // 프리팹 저장
            PrefabUtility.SaveAsPrefabAsset(root, savePath);
            DestroyImmediate(root);

            return true;
        }
        catch (System.Exception e)
        {
            Log($"[ERROR] Failed to create {prefabName}: {e.Message}");
            _errors++;
            return false;
        }
        finally
        {
            // 임시 인스턴스 정리
            if (bodyInstance != null) DestroyImmediate(bodyInstance);
            if (earsInstance != null) DestroyImmediate(earsInstance);
        }
    }

    /// <summary>
    /// 소스에서 SkinnedMeshRenderer가 있는 자식들을 타겟으로 복사
    /// </summary>
    private void CopySkinnedMeshChildren(GameObject source, GameObject target)
    {
        // 모든 자식에서 SkinnedMeshRenderer 찾기 (재귀적으로)
        var allSMRs = source.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        foreach (var smr in allSMRs)
        {
            CopySkinnedMeshObject(smr.gameObject, smr, target);
        }

        // 루트에 직접 붙은 SkinnedMeshRenderer도 처리
        var rootSmr = source.GetComponent<SkinnedMeshRenderer>();
        if (rootSmr != null && !System.Array.Exists(allSMRs, x => x == rootSmr))
        {
            CopySkinnedMeshObject(source, rootSmr, target);
        }
    }

    /// <summary>
    /// 단일 SkinnedMeshRenderer 오브젝트 복사
    /// </summary>
    private void CopySkinnedMeshObject(GameObject sourceObj, SkinnedMeshRenderer smr, GameObject target)
    {
        GameObject copy = new GameObject(sourceObj.name);
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

        // BoneInfo 처리 - 3단계로 시도
        bool boneInfoCreated = false;

        // 1단계: 기존 BoneInfo가 있고 유효하면 복사
        var existingBoneInfo = sourceObj.GetComponent<SkinnedMeshBoneInfo>();
        if (existingBoneInfo != null && existingBoneInfo.IsValid())
        {
            var newBoneInfo = copy.AddComponent<SkinnedMeshBoneInfo>();
            EditorUtility.CopySerialized(existingBoneInfo, newBoneInfo);
            boneInfoCreated = true;
            Log($"  - {sourceObj.name}: 기존 BoneInfo 복사됨 ({existingBoneInfo.BoneNames.Length}개 본)");
        }

        // 2단계: 원본 SMR의 bones 배열에서 생성 시도
        if (!boneInfoCreated && smr.bones != null && smr.bones.Length > 0)
        {
            // bones 배열에 유효한 본이 있는지 확인
            int validBoneCount = 0;
            foreach (var bone in smr.bones)
            {
                if (bone != null) validBoneCount++;
            }

            if (validBoneCount > 0)
            {
                var newBoneInfo = copy.AddComponent<SkinnedMeshBoneInfo>();
                newBoneInfo.SaveBoneNames(smr);

                if (newBoneInfo.IsValid())
                {
                    boneInfoCreated = true;
                    Log($"  - {sourceObj.name}: BoneInfo 생성됨 ({validBoneCount}/{smr.bones.Length}개 유효한 본)");
                }
                else
                {
                    // 생성 실패시 컴포넌트 제거
                    DestroyImmediate(newBoneInfo);
                }
            }
        }

        // 3단계: FBX에서 bone 정보 추출 시도
        if (!boneInfoCreated && smr.sharedMesh != null)
        {
            boneInfoCreated = TryExtractBoneInfoFromFBX(smr, copy);
        }

        // 4단계: 모두 실패시 경고
        if (!boneInfoCreated)
        {
            int expectedBoneCount = smr.sharedMesh != null ? smr.sharedMesh.bindposes.Length : 0;
            Log($"[WARN] {sourceObj.name}: BoneInfo를 생성할 수 없음 (예상 본 개수: {expectedBoneCount})");
            Log($"       → 원본 FBX 또는 프리팹에서 bone 정보를 찾을 수 없습니다");

            // 디버그 정보 출력
            if (smr.bones == null)
            {
                Log($"       bones 배열: null");
            }
            else
            {
                Log($"       bones 배열 길이: {smr.bones.Length}");
                int nullCount = 0;
                foreach (var bone in smr.bones)
                {
                    if (bone == null) nullCount++;
                }
                Log($"       null 본 개수: {nullCount}");
            }

            if (smr.rootBone != null)
            {
                Log($"       rootBone: {smr.rootBone.name}");
            }
            else
            {
                Log($"       rootBone: null");
            }
        }
    }

    /// <summary>
    /// FBX에서 bone 정보를 추출하여 BoneInfo 생성
    /// </summary>
    private bool TryExtractBoneInfoFromFBX(SkinnedMeshRenderer smr, GameObject targetObj)
    {
        if (smr.sharedMesh == null) return false;

        // 메쉬의 에셋 경로 찾기
        string meshPath = AssetDatabase.GetAssetPath(smr.sharedMesh);
        if (string.IsNullOrEmpty(meshPath)) return false;

        // FBX 파일인지 확인
        if (!meshPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase) &&
            !meshPath.EndsWith(".FBX", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        GameObject fbxInstance = null;
        try
        {
            // FBX 로드 및 인스턴스화
            GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(meshPath);
            if (fbxAsset == null) return false;

            fbxInstance = Object.Instantiate(fbxAsset);

            // FBX 내에서 같은 메쉬를 사용하는 SkinnedMeshRenderer 찾기
            var fbxRenderers = fbxInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            SkinnedMeshRenderer matchingRenderer = null;

            foreach (var renderer in fbxRenderers)
            {
                if (renderer.sharedMesh == smr.sharedMesh ||
                    (renderer.sharedMesh != null && renderer.sharedMesh.name == smr.sharedMesh.name))
                {
                    matchingRenderer = renderer;
                    break;
                }
            }

            if (matchingRenderer == null || matchingRenderer.bones == null || matchingRenderer.bones.Length == 0)
            {
                return false;
            }

            // bone 정보 추출
            var boneInfo = targetObj.AddComponent<SkinnedMeshBoneInfo>();
            boneInfo.SaveBoneNames(matchingRenderer);

            if (boneInfo.IsValid())
            {
                Log($"  - {targetObj.name}: FBX에서 BoneInfo 추출됨 ({matchingRenderer.bones.Length}개 본)");
                return true;
            }
            else
            {
                DestroyImmediate(boneInfo);
                return false;
            }
        }
        catch (System.Exception e)
        {
            Log($"[WARN] FBX에서 bone 추출 실패: {e.Message}");
            return false;
        }
        finally
        {
            if (fbxInstance != null)
            {
                DestroyImmediate(fbxInstance);
            }
        }
    }

    #endregion

    #region Step 2: Generate Item SOs

    /// <summary>
    /// 모든 Item SO 생성
    /// </summary>
    private void GenerateAllItemSOs()
    {
        EnsureFolderExists(CATALOG_PATH);

        // 처리할 폴더 목록 수집
        var foldersToProcess = new List<(string path, string name, CustomizingType type)>();

        // SkinColor 폴더 우선 처리
        if (AssetDatabase.IsValidFolder(SKINCOLOR_PATH))
        {
            foldersToProcess.Add((SKINCOLOR_PATH, "SkinColor", CustomizingType.SkinColor));
        }

        // 나머지 폴더들
        foreach (var kvp in FolderToType)
        {
            if (ExcludedFolders.Contains(kvp.Key)) continue;
            if (kvp.Key == "SkinColor") continue; // 이미 처리됨

            string folderPath = Path.Combine(ITEMS_PATH, kvp.Key);
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                foldersToProcess.Add((folderPath, kvp.Key, kvp.Value));
            }
        }

        // 각 폴더 처리
        foreach (var (path, name, type) in foldersToProcess)
        {
            GenerateItemSOsForFolder(path, name, type);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 특정 폴더의 프리팹들에 대해 Item SO 생성
    /// </summary>
    private void GenerateItemSOsForFolder(string folderPath, string categoryName, CustomizingType defaultType)
    {
        var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);

            // 프리팹 경로/이름에서 타입 자동 추론
            CustomizingType type = InferCustomizingType(prefabPath, prefabName, defaultType);

            // SO 저장 경로 (타입별 서브폴더 생성)
            string typeFolderPath = Path.Combine(CATALOG_PATH, type.ToString());
            EnsureFolderExists(typeFolderPath);
            string soPath = Path.Combine(typeFolderPath, $"{prefabName}.asset");

            // 기존 위치에 있는 SO도 확인 (이전 버전 호환)
            string oldSoPath = Path.Combine(CATALOG_PATH, $"{prefabName}.asset");
            var existingSO = AssetDatabase.LoadAssetAtPath<CustomizingItemSO>(soPath);
            if (existingSO == null)
            {
                existingSO = AssetDatabase.LoadAssetAtPath<CustomizingItemSO>(oldSoPath);
            }

            if (existingSO != null && !_overwriteExisting)
            {
                _skipped++;
                continue;
            }

            // 프리팹 로드
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Log($"[ERROR] Failed to load prefab: {prefabPath}");
                _errors++;
                continue;
            }

            if (existingSO != null)
            {
                // 기존 SO 업데이트
                UpdateItemSO(existingSO, prefabName, type, prefab);
                EditorUtility.SetDirty(existingSO);
                _sosUpdated++;
            }
            else
            {
                // 새 SO 생성
                CreateItemSO(soPath, prefabName, type, prefab);
                _sosCreated++;
            }
        }
    }

    /// <summary>
    /// 프리팹 경로와 이름에서 CustomizingType 추론
    /// </summary>
    private CustomizingType InferCustomizingType(string prefabPath, string prefabName, CustomizingType defaultType)
    {
        // 1. 경로에서 폴더명 추출하여 매칭
        string[] pathParts = prefabPath.Replace("\\", "/").Split('/');
        for (int i = pathParts.Length - 2; i >= 0; i--) // 파일명 제외하고 역순 탐색
        {
            string folderName = pathParts[i];
            if (FolderToType.TryGetValue(folderName, out CustomizingType folderType))
            {
                return folderType;
            }
        }

        // 2. 프리팹 이름 패턴으로 매칭
        foreach (var kvp in PrefabNameToType)
        {
            if (prefabName.StartsWith(kvp.Key, System.StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Value;
            }
            // 언더스코어로 분리된 첫 단어도 체크
            string[] nameParts = prefabName.Split('_');
            if (nameParts.Length > 0 && kvp.Key.Equals(nameParts[0], System.StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Value;
            }
        }

        // 3. 특수 패턴 처리
        if (prefabName.Contains("Emotion") || prefabName.Contains("Female_") || prefabName.Contains("Male_"))
        {
            return CustomizingType.Faces;
        }

        return defaultType;
    }

    /// <summary>
    /// 새 CustomizingItemSO 생성
    /// </summary>
    private void CreateItemSO(string soPath, string prefabName, CustomizingType type, GameObject prefab)
    {
        var itemSO = ScriptableObject.CreateInstance<CustomizingItemSO>();

        SetPrivateField(itemSO, "_itemId", GenerateItemId(type, prefabName));
        SetPrivateField(itemSO, "_displayName", GenerateDisplayName(prefabName));
        SetPrivateField(itemSO, "_customizingType", type);
        SetPrivateField(itemSO, "_partPrefab", prefab);
        SetPrivateField(itemSO, "_isDefault", IsDefaultItem(prefabName));
        SetPrivateField(itemSO, "_sortOrder", ExtractSortOrder(prefabName));

        AssetDatabase.CreateAsset(itemSO, soPath);
        Log($"[SUCCESS] Created SO: {prefabName} → {type}");
    }

    /// <summary>
    /// 기존 CustomizingItemSO 업데이트
    /// </summary>
    private void UpdateItemSO(CustomizingItemSO itemSO, string prefabName, CustomizingType type, GameObject prefab)
    {
        var serializedObject = new SerializedObject(itemSO);

        serializedObject.FindProperty("_itemId").stringValue = GenerateItemId(type, prefabName);
        serializedObject.FindProperty("_displayName").stringValue = GenerateDisplayName(prefabName);
        serializedObject.FindProperty("_customizingType").enumValueIndex = (int)type;
        serializedObject.FindProperty("_partPrefab").objectReferenceValue = prefab;
        serializedObject.FindProperty("_isDefault").boolValue = IsDefaultItem(prefabName);
        serializedObject.FindProperty("_sortOrder").intValue = ExtractSortOrder(prefabName);

        serializedObject.ApplyModifiedProperties();
        Log($"[SUCCESS] Updated SO: {prefabName}");
    }

    /// <summary>
    /// Item ID 생성 (예: SkinColor_SkinColor_01, Hat_FrogHat_01)
    /// </summary>
    private string GenerateItemId(CustomizingType type, string prefabName)
    {
        return $"{type}_{prefabName}";
    }

    /// <summary>
    /// Display Name 생성 (사람이 읽기 쉬운 이름)
    /// </summary>
    private string GenerateDisplayName(string prefabName)
    {
        // 언더스코어를 공백으로, 숫자 앞에 공백 추가
        return prefabName.Replace("_", " ").Trim();
    }

    /// <summary>
    /// 기본 아이템인지 확인 (_01로 끝나는지)
    /// </summary>
    private bool IsDefaultItem(string prefabName)
    {
        return prefabName.EndsWith("_01") || prefabName.EndsWith("_1");
    }

    /// <summary>
    /// Addressable 레퍼런스 설정
    /// </summary>
    private void SetAddressableReference(CustomizingItemSO itemSO, GameObject prefab)
    {
        var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Log("[ERROR] Addressables not initialized");
            return;
        }

        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        string prefabGuid = AssetDatabase.AssetPathToGUID(prefabPath);

        // Addressable 그룹에 등록
        var group = settings.FindGroup("Customizing") ?? settings.DefaultGroup;
        var entry = settings.FindAssetEntry(prefabGuid);
        if (entry == null)
        {
            entry = settings.CreateOrMoveEntry(prefabGuid, group, false, false);
            entry.address = prefab.name;
        }

        // SO에 AssetReference 설정
        var serializedObject = new SerializedObject(itemSO);
        var refProperty = serializedObject.FindProperty("_partPrefabRef");
        if (refProperty != null)
        {
            var guidProperty = refProperty.FindPropertyRelative("m_AssetGUID");
            if (guidProperty != null)
            {
                guidProperty.stringValue = prefabGuid;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(itemSO);
            }
        }
    }

    #endregion

    #region BaseEquipment SO Generation

    /// <summary>
    /// BaseEquipment SO 생성 (Outfit, Gloves, Pants)
    /// </summary>
    private void GenerateBaseEquipmentSOs()
    {
        Log("===== Generate BaseEquipment SOs =====\n");

        EnsureFolderExists(BASE_EQUIPMENT_PATH);

        // 각 BaseEquipmentType에 대해 처리
        foreach (var kvp in FolderToBaseEquipment)
        {
            string folderName = kvp.Key;
            BaseEquipmentType equipType = kvp.Value;

            // Items 폴더에서 해당 타입의 프리팹 찾기
            string folderPath = Path.Combine(ITEMS_PATH, folderName);
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                // 중첩 폴더도 검색
                folderPath = Path.Combine(ITEMS_PATH, "Customizing/Items", folderName);
            }

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                Log($"[WARN] Folder not found: {folderName}");
                continue;
            }

            GenerateBaseEquipmentSOsForFolder(folderPath, equipType);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 특정 폴더의 프리팹들에 대해 BaseEquipmentItemSO 생성
    /// </summary>
    private void GenerateBaseEquipmentSOsForFolder(string folderPath, BaseEquipmentType equipType)
    {
        var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        Log($"\n--- {equipType}: {prefabGuids.Length}개 프리팹 ---");

        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);

            // SO 저장 경로
            string soPath = Path.Combine(BASE_EQUIPMENT_PATH, $"{prefabName}.asset");

            // 기존 SO 확인
            var existingSO = AssetDatabase.LoadAssetAtPath<BaseEquipmentItemSO>(soPath);

            if (existingSO != null && !_overwriteExisting)
            {
                _skipped++;
                continue;
            }

            // 프리팹 로드
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Log($"[ERROR] Failed to load prefab: {prefabPath}");
                _errors++;
                continue;
            }

            if (existingSO != null)
            {
                // 기존 SO 업데이트
                UpdateBaseEquipmentSO(existingSO, prefabName, equipType, prefab);
                EditorUtility.SetDirty(existingSO);
                _sosUpdated++;
            }
            else
            {
                // 새 SO 생성
                CreateBaseEquipmentSO(soPath, prefabName, equipType, prefab);
                _sosCreated++;
            }
        }
    }

    /// <summary>
    /// 새 BaseEquipmentItemSO 생성
    /// </summary>
    private void CreateBaseEquipmentSO(string soPath, string prefabName, BaseEquipmentType equipType, GameObject prefab)
    {
        var itemSO = ScriptableObject.CreateInstance<BaseEquipmentItemSO>();

        SetPrivateField(itemSO, "_itemId", $"BaseEquipment_{equipType}_{prefabName}");
        SetPrivateField(itemSO, "_displayName", GenerateDisplayName(prefabName));
        SetPrivateField(itemSO, "_equipmentType", equipType);
        SetPrivateField(itemSO, "_partPrefab", prefab);

        AssetDatabase.CreateAsset(itemSO, soPath);
        Log($"[SUCCESS] Created BaseEquipment SO: {prefabName} → {equipType}");
    }

    /// <summary>
    /// 기존 BaseEquipmentItemSO 업데이트
    /// </summary>
    private void UpdateBaseEquipmentSO(BaseEquipmentItemSO itemSO, string prefabName, BaseEquipmentType equipType, GameObject prefab)
    {
        var serializedObject = new SerializedObject(itemSO);

        serializedObject.FindProperty("_itemId").stringValue = $"BaseEquipment_{equipType}_{prefabName}";
        serializedObject.FindProperty("_displayName").stringValue = GenerateDisplayName(prefabName);
        serializedObject.FindProperty("_equipmentType").enumValueIndex = (int)equipType;
        serializedObject.FindProperty("_partPrefab").objectReferenceValue = prefab;

        serializedObject.ApplyModifiedProperties();
        Log($"[SUCCESS] Updated BaseEquipment SO: {prefabName}");
    }

    /// <summary>
    /// 프리팹 경로/이름에서 BaseEquipmentType 추론
    /// </summary>
    private BaseEquipmentType InferBaseEquipmentType(string prefabPath, string prefabName)
    {
        // 경로에서 폴더명 추출
        string[] pathParts = prefabPath.Replace("\\", "/").Split('/');
        for (int i = pathParts.Length - 2; i >= 0; i--)
        {
            string folderName = pathParts[i];
            if (FolderToBaseEquipment.TryGetValue(folderName, out BaseEquipmentType folderType))
            {
                return folderType;
            }
        }

        // 프리팹 이름으로 추론
        if (prefabName.StartsWith("Outfit", System.StringComparison.OrdinalIgnoreCase))
            return BaseEquipmentType.Outfit;
        if (prefabName.StartsWith("Gloves", System.StringComparison.OrdinalIgnoreCase))
            return BaseEquipmentType.Gloves;
        if (prefabName.StartsWith("Pants", System.StringComparison.OrdinalIgnoreCase))
            return BaseEquipmentType.Pants;

        return BaseEquipmentType.Outfit; // 기본값
    }

    #endregion

    #region Step 3: Link Prefabs to SOs

    /// <summary>
    /// 모든 SO의 Part Prefab 필드 연결
    /// </summary>
    private void LinkAllPrefabsToSOs()
    {
        // 모든 프리팹 수집
        var prefabMap = new Dictionary<string, GameObject>();
        var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { ITEMS_PATH });

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null && !prefabMap.ContainsKey(name))
            {
                prefabMap[name] = prefab;
            }
        }

        Log($"Found {prefabMap.Count} prefabs in Items folder");

        // 모든 CustomizingItemSO 찾기
        var soGuids = AssetDatabase.FindAssets("t:CustomizingItemSO", new[] { CATALOG_PATH });

        foreach (string guid in soGuids)
        {
            string soPath = AssetDatabase.GUIDToAssetPath(guid);
            var itemSO = AssetDatabase.LoadAssetAtPath<CustomizingItemSO>(soPath);

            if (itemSO == null) continue;

            string soName = Path.GetFileNameWithoutExtension(soPath);

            // 이미 Addressable이 연결되어 있으면 스킵 (덮어쓰기 아닌 경우)
            if (itemSO.HasAssetRef && !_overwriteExisting)
            {
                continue;
            }

            // 매칭되는 프리팹 찾기
            if (prefabMap.TryGetValue(soName, out GameObject prefab))
            {
                // Addressable로 등록 및 연결
                SetAddressableReference(itemSO, prefab);

                Log($"[SUCCESS] Linked: {soName} ← {prefab.name}");
                _sosUpdated++;
            }
            else
            {
                Log($"[WARN] No prefab found for SO: {soName}");
            }
        }

        AssetDatabase.SaveAssets();
    }

    #endregion

    #region Fix BoneInfo

    /// <summary>
    /// 모든 프리팹에서 누락된 BoneInfo를 FBX에서 추출하여 채움
    /// </summary>
    private void FixMissingBoneInfoInPrefabs()
    {
        Log("===== Fix Missing BoneInfo =====\n");

        var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { ITEMS_PATH });
        int fixedCount = 0;
        int alreadyValidCount = 0;

        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);

            // 프리팹을 인스턴스화
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null) continue;

            GameObject instance = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
            if (instance == null)
            {
                instance = Object.Instantiate(prefabAsset);
            }

            bool needsSave = false;

            try
            {
                var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);

                foreach (var smr in renderers)
                {
                    var boneInfo = smr.GetComponent<SkinnedMeshBoneInfo>();

                    // BoneInfo가 없거나 유효하지 않으면 수정 필요
                    if (boneInfo == null)
                    {
                        // BoneInfo 컴포넌트 추가하고 FBX에서 추출
                        if (TryExtractBoneInfoFromFBX(smr, smr.gameObject))
                        {
                            needsSave = true;
                            fixedCount++;
                            Log($"[SUCCESS] Added BoneInfo: {prefabPath} / {smr.name}");
                        }
                    }
                    else if (!boneInfo.IsValid())
                    {
                        // 기존 BoneInfo가 유효하지 않으면 FBX에서 다시 추출
                        DestroyImmediate(boneInfo);
                        if (TryExtractBoneInfoFromFBX(smr, smr.gameObject))
                        {
                            needsSave = true;
                            fixedCount++;
                            Log($"[SUCCESS] Fixed BoneInfo: {prefabPath} / {smr.name}");
                        }
                    }
                    else
                    {
                        alreadyValidCount++;
                    }
                }

                // 변경사항 저장
                if (needsSave)
                {
                    PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                }
            }
            finally
            {
                DestroyImmediate(instance);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Log($"\n===== Results =====");
        Log($"Fixed: {fixedCount}");
        Log($"Already Valid: {alreadyValidCount}");
        _sosUpdated = fixedCount;
    }

    #endregion

    #region Fix SO Types

    /// <summary>
    /// 모든 SO의 CustomizingType을 프리팹 경로/이름에서 추론하여 수정
    /// </summary>
    private void FixSOCustomizingTypes()
    {
        Log("===== Fix SO CustomizingTypes =====\n");

        // 모든 CustomizingItemSO 찾기
        var soGuids = AssetDatabase.FindAssets("t:CustomizingItemSO");
        int fixedCount = 0;
        int alreadyCorrectCount = 0;

        foreach (string guid in soGuids)
        {
            string soPath = AssetDatabase.GUIDToAssetPath(guid);
            var itemSO = AssetDatabase.LoadAssetAtPath<CustomizingItemSO>(soPath);

            if (itemSO == null) continue;

            // 연결된 프리팹에서 타입 추론
            if (!itemSO.HasAssetRef || itemSO.PartPrefabRef == null)
            {
                Log($"[WARN] No prefab linked: {soPath}");
                continue;
            }
            GameObject prefab = itemSO.PartPrefabRef.editorAsset as GameObject;
            if (prefab == null)
            {
                Log($"[WARN] Cannot load prefab: {soPath}");
                continue;
            }

            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            string prefabName = prefab.name;

            CustomizingType inferredType = InferCustomizingType(prefabPath, prefabName, itemSO.Category);
            CustomizingType currentType = itemSO.Category;

            if (inferredType != currentType)
            {
                var serializedObject = new SerializedObject(itemSO);
                serializedObject.FindProperty("_customizingType").enumValueIndex = (int)inferredType;
                serializedObject.FindProperty("_itemId").stringValue = GenerateItemId(inferredType, prefabName);
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(itemSO);

                Log($"[SUCCESS] {prefabName}: {currentType} → {inferredType}");
                fixedCount++;
            }
            else
            {
                alreadyCorrectCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Log($"\n===== Results =====");
        Log($"Fixed: {fixedCount}");
        Log($"Already Correct: {alreadyCorrectCount}");
        _sosUpdated = fixedCount;
    }

    /// <summary>
    /// 모든 프리팹과 추론된 타입을 로그로 출력
    /// </summary>
    private void ListAllPrefabsWithTypes()
    {
        _logMessages.Clear();
        Log("===== All Prefabs with Inferred Types =====\n");

        var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { ITEMS_PATH });

        // 타입별로 그룹화
        var typeGroups = new Dictionary<CustomizingType, List<string>>();
        foreach (CustomizingType type in System.Enum.GetValues(typeof(CustomizingType)))
        {
            typeGroups[type] = new List<string>();
        }

        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);

            CustomizingType inferredType = InferCustomizingType(prefabPath, prefabName, CustomizingType.SkinColor);
            typeGroups[inferredType].Add(prefabName);
        }

        // 타입별로 출력
        foreach (var kvp in typeGroups)
        {
            if (kvp.Value.Count > 0)
            {
                Log($"\n=== {kvp.Key} ({kvp.Value.Count}개) ===");
                foreach (string name in kvp.Value.Take(10))
                {
                    Log($"  - {name}");
                }
                if (kvp.Value.Count > 10)
                {
                    Log($"  ... 외 {kvp.Value.Count - 10}개");
                }
            }
        }

        Log($"\n총 {prefabGuids.Length}개 프리팹");
    }

    #endregion

    #region Utility Methods

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
        Debug.Log("[Pipeline] " + message);
    }

    #endregion
}
