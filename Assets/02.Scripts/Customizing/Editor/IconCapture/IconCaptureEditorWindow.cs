using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 커스터마이징 아이콘 자동 생성 에디터 윈도우
///
/// 기능:
/// - 프리팹 폴더/리스트에서 아이콘 일괄 생성
/// - RenderTexture 기반 캡처
/// - CustomizingItemSO에 아이콘 자동 연결
///
/// 사용법:
/// 1. 메뉴: Tools > Customizing > Icon Capture Tool
/// 2. 프리팹 폴더 또는 개별 프리팹 설정
/// 3. 아이콘 저장 경로 설정
/// 4. "아이콘 생성" 버튼 클릭
/// 5. "SO에 아이콘 연결" 버튼 클릭
/// </summary>
public class IconCaptureEditorWindow : EditorWindow
{
    // ========================================
    // 상수
    // ========================================

    private const string WINDOW_TITLE = "Icon Capture Tool";
    private const string DEFAULT_ICON_OUTPUT_PATH = "Assets/03.Sprites/CustomizingIcons";
    private const string DEFAULT_PREFAB_PATH = "Assets/Characters/Prefabs";
    private const int DEFAULT_RESOLUTION = 256;
    private const int MIN_RESOLUTION = 64;
    private const int MAX_RESOLUTION = 1024;

    // ========================================
    // 필드 - 설정
    // ========================================

    // 프리팹 설정
    private DefaultAsset _prefabFolder;
    private string _prefabFolderPath = DEFAULT_PREFAB_PATH;
    private List<GameObject> _selectedPrefabs = new List<GameObject>();
    private CustomizingType _filterType = CustomizingType.Hat;
    private bool _useTypeFilter = true;

    // 아이콘 설정
    private string _iconOutputPath = DEFAULT_ICON_OUTPUT_PATH;
    private int _iconResolution = DEFAULT_RESOLUTION;
    private bool _useIconPrefix = true;
    private bool _overwriteIcons = false;

    // 카메라 설정
    private bool _useCustomCameraOffset = false;
    private Vector3 _customCameraOffset = new Vector3(0, 0.2f, 1.5f);
    private float _customSizeMultiplier = 0.7f;
    private float _customDefaultOrthoSize = 0.5f;

    // SO 연결 설정
    private string _soFolderPath = "";
    private bool _overwriteSO = false;
    private bool _dryRunMode = false;

    // UI 상태
    private Vector2 _scrollPosition;
    private Vector2 _prefabListScrollPosition;
    private Vector2 _resultScrollPosition;
    private bool _showPrefabList = false;
    private bool _showResults = false;

    // 결과
    private string _lastResultMessage = "";
    private MessageType _lastResultType = MessageType.None;

    // ========================================
    // 메뉴
    // ========================================

    [MenuItem("Tools/Customizing/Icon Capture Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<IconCaptureEditorWindow>(WINDOW_TITLE);
        window.minSize = new Vector2(400, 600);
    }

    // ========================================
    // Unity 이벤트
    // ========================================

    private void OnEnable()
    {
        // 기본 폴더 설정
        if (string.IsNullOrEmpty(_prefabFolderPath))
        {
            _prefabFolderPath = DEFAULT_PREFAB_PATH;
        }

        LoadPrefabFolder();
    }

    private void OnGUI()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        EditorGUILayout.Space(10);
        DrawHeader();

        EditorGUILayout.Space(10);
        DrawPrefabSettings();

        EditorGUILayout.Space(10);
        DrawIconSettings();

        EditorGUILayout.Space(10);
        DrawCameraSettings();

        EditorGUILayout.Space(10);
        DrawCaptureButtons();

        EditorGUILayout.Space(20);
        DrawSOAssignmentSection();

        EditorGUILayout.Space(10);
        DrawResultsSection();

        EditorGUILayout.EndScrollView();
    }

    // ========================================
    // UI 그리기 - 헤더
    // ========================================

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };

        EditorGUILayout.LabelField("Customizing Icon Capture Tool", titleStyle);
        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "프리팹에서 아이콘을 자동 생성하고 CustomizingItemSO에 연결합니다.\n" +
            "1. 프리팹 폴더 설정 -> 2. 아이콘 생성 -> 3. SO 연결",
            MessageType.Info);

        EditorGUILayout.EndVertical();
    }

    // ========================================
    // UI 그리기 - 프리팹 설정
    // ========================================

    private void DrawPrefabSettings()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("프리팹 설정", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // 폴더 선택
        EditorGUI.BeginChangeCheck();
        _prefabFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "프리팹 폴더",
            _prefabFolder,
            typeof(DefaultAsset),
            false);

        if (EditorGUI.EndChangeCheck() && _prefabFolder != null)
        {
            _prefabFolderPath = AssetDatabase.GetAssetPath(_prefabFolder);
            LoadPrefabFolder();
        }

        // 경로 직접 입력
        EditorGUI.BeginChangeCheck();
        _prefabFolderPath = EditorGUILayout.TextField("폴더 경로", _prefabFolderPath);
        if (EditorGUI.EndChangeCheck())
        {
            LoadPrefabFolder();
        }

        EditorGUILayout.Space(5);

        // 타입 필터
        _useTypeFilter = EditorGUILayout.Toggle("타입별 필터링", _useTypeFilter);
        if (_useTypeFilter)
        {
            EditorGUI.indentLevel++;
            _filterType = (CustomizingType)EditorGUILayout.EnumPopup("커스터마이징 타입", _filterType);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5);

        // 프리팹 검색 버튼
        if (GUILayout.Button("프리팹 검색", GUILayout.Height(25)))
        {
            SearchPrefabs();
        }

        // 프리팹 리스트 표시
        if (_selectedPrefabs.Count > 0)
        {
            _showPrefabList = EditorGUILayout.Foldout(_showPrefabList, $"검색된 프리팹 ({_selectedPrefabs.Count}개)");
            if (_showPrefabList)
            {
                DrawPrefabList();
            }
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 검색된 프리팹 리스트 표시
    /// </summary>
    private void DrawPrefabList()
    {
        EditorGUI.indentLevel++;
        _prefabListScrollPosition = EditorGUILayout.BeginScrollView(
            _prefabListScrollPosition,
            GUILayout.MaxHeight(150));

        for (int i = 0; i < _selectedPrefabs.Count; i++)
        {
            EditorGUILayout.ObjectField(_selectedPrefabs[i], typeof(GameObject), false);
        }

        EditorGUILayout.EndScrollView();
        EditorGUI.indentLevel--;
    }

    // ========================================
    // UI 그리기 - 아이콘 설정
    // ========================================

    private void DrawIconSettings()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("아이콘 설정", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // 저장 경로
        EditorGUILayout.BeginHorizontal();
        _iconOutputPath = EditorGUILayout.TextField("저장 경로", _iconOutputPath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("아이콘 저장 폴더 선택", "Assets", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                // 절대 경로를 Assets 상대 경로로 변환
                if (selectedPath.Contains(Application.dataPath))
                {
                    _iconOutputPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }
                else
                {
                    _iconOutputPath = selectedPath;
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        // 해상도
        _iconResolution = EditorGUILayout.IntSlider("해상도", _iconResolution, MIN_RESOLUTION, MAX_RESOLUTION);

        // 옵션
        _useIconPrefix = EditorGUILayout.Toggle("Icon_ 접두사 사용", _useIconPrefix);
        _overwriteIcons = EditorGUILayout.Toggle("기존 파일 덮어쓰기", _overwriteIcons);

        EditorGUILayout.EndVertical();
    }

    // ========================================
    // UI 그리기 - 카메라 설정
    // ========================================

    private void DrawCameraSettings()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("카메라 설정", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        _useCustomCameraOffset = EditorGUILayout.Toggle("커스텀 각도 사용", _useCustomCameraOffset);

        if (_useCustomCameraOffset)
        {
            EditorGUI.indentLevel++;

            // 타입 기본값 불러오기 버튼
            if (GUILayout.Button("현재 타입 기본값 불러오기", GUILayout.Height(20)))
            {
                LoadDefaultOffsetForCurrentType();
            }

            EditorGUILayout.Space(3);

            // 카메라 오프셋 (위치)
            EditorGUILayout.LabelField("카메라 오프셋 (타겟 기준 상대 위치)", EditorStyles.miniLabel);
            _customCameraOffset.x = EditorGUILayout.Slider("좌/우 (X)", _customCameraOffset.x, -3f, 360f);
            _customCameraOffset.y = EditorGUILayout.Slider("상/하 (Y)", _customCameraOffset.y, -2f, 5f);
            _customCameraOffset.z = EditorGUILayout.Slider("거리 (Z)", _customCameraOffset.z, 0.3f, 10f);

            EditorGUILayout.Space(3);

            // 크기 설정
            _customSizeMultiplier = EditorGUILayout.Slider("크기 배율", _customSizeMultiplier, 0.1f, 5f);
            _customDefaultOrthoSize = EditorGUILayout.Slider("기본 OrthoSize", _customDefaultOrthoSize, 0.1f, 3f);

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 현재 선택된 타입의 기본 카메라 오프셋을 슬라이더에 로드
    /// </summary>
    private void LoadDefaultOffsetForCurrentType()
    {
        var tempController = new IconCaptureCameraController(256);
        var defaultOffset = tempController.GetDefaultOffsetForType(_filterType);
        _customCameraOffset = defaultOffset.cameraOffset;
        _customSizeMultiplier = defaultOffset.sizeMultiplier;
        _customDefaultOrthoSize = defaultOffset.defaultOrthoSize;
    }

    // ========================================
    // UI 그리기 - 캡처 버튼
    // ========================================

    private void DrawCaptureButtons()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("아이콘 생성", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        GUI.enabled = _selectedPrefabs.Count > 0;

        // 선택된 타입만 캡처
        if (GUILayout.Button($"선택된 프리팹 캡처 ({_selectedPrefabs.Count}개)", GUILayout.Height(30)))
        {
            CaptureSelectedPrefabs();
        }

        EditorGUILayout.Space(5);

        // 모든 타입 일괄 캡처
        GUI.enabled = true;
        if (GUILayout.Button("전체 타입 일괄 캡처", GUILayout.Height(30)))
        {
            CaptureAllTypes();
        }

        EditorGUILayout.EndVertical();
    }

    // ========================================
    // UI 그리기 - SO 연결 섹션
    // ========================================

    private void DrawSOAssignmentSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("SO 아이콘 연결", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // SO 폴더 경로 (선택적)
        EditorGUILayout.BeginHorizontal();
        _soFolderPath = EditorGUILayout.TextField("SO 폴더 (선택)", _soFolderPath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("SO 폴더 선택", "Assets", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (selectedPath.Contains(Application.dataPath))
                {
                    _soFolderPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }
                else
                {
                    _soFolderPath = selectedPath;
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox("비워두면 프로젝트 전체의 CustomizingItemSO를 검색합니다.", MessageType.Info);

        // 옵션
        _overwriteSO = EditorGUILayout.Toggle("기존 아이콘 덮어쓰기", _overwriteSO);
        _dryRunMode = EditorGUILayout.Toggle("Dry Run (미리보기)", _dryRunMode);

        EditorGUILayout.Space(5);

        // 연결 버튼
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("SO에 아이콘 연결", GUILayout.Height(30)))
        {
            AssignIconsToSOs();
        }

        if (GUILayout.Button("아이콘 없는 SO 확인", GUILayout.Height(30)))
        {
            CheckSOsWithoutIcon();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    // ========================================
    // UI 그리기 - 결과 섹션
    // ========================================

    private void DrawResultsSection()
    {
        if (string.IsNullOrEmpty(_lastResultMessage))
        {
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        _showResults = EditorGUILayout.Foldout(_showResults, "실행 결과");

        if (_showResults)
        {
            _resultScrollPosition = EditorGUILayout.BeginScrollView(
                _resultScrollPosition,
                GUILayout.MaxHeight(200));

            EditorGUILayout.HelpBox(_lastResultMessage, _lastResultType);

            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("결과 지우기"))
            {
                _lastResultMessage = "";
            }
        }

        EditorGUILayout.EndVertical();
    }

    // ========================================
    // 기능 - 프리팹 검색
    // ========================================

    /// <summary>
    /// 폴더에서 프리팹 로드
    /// </summary>
    private void LoadPrefabFolder()
    {
        if (!string.IsNullOrEmpty(_prefabFolderPath) && Directory.Exists(GetAbsolutePath(_prefabFolderPath)))
        {
            _prefabFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(_prefabFolderPath);
        }
    }

    /// <summary>
    /// 설정에 따라 프리팹 검색
    /// </summary>
    private void SearchPrefabs()
    {
        _selectedPrefabs.Clear();

        if (string.IsNullOrEmpty(_prefabFolderPath))
        {
            ShowResult("프리팹 폴더를 설정해주세요.", MessageType.Warning);
            return;
        }

        string searchPath = _prefabFolderPath;

        // 타입 필터링 시 하위 폴더 지정
        if (_useTypeFilter)
        {
            string typeFolderName = GetFolderNameForType(_filterType);
            if (_filterType != CustomizingType.None)
            {
                searchPath = Path.Combine(_prefabFolderPath, typeFolderName).Replace("\\", "/");
            }
        }

        // 프리팹 검색
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { searchPath });

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                _selectedPrefabs.Add(prefab);
            }
        }

        _showPrefabList = true;
        ShowResult($"프리팹 {_selectedPrefabs.Count}개를 찾았습니다.", MessageType.Info);
    }

    /// <summary>
    /// CustomizingType에 해당하는 폴더명 반환
    /// </summary>
    private string GetFolderNameForType(CustomizingType type)
    {
        return type switch
        {
            CustomizingType.Hat => "Hat",
            CustomizingType.HairStyle => "Hairstyle",
            CustomizingType.Faces => "Faces",
            CustomizingType.FaceAccessory => "Face Accessories",
            CustomizingType.Glasses => "Glasses",
            CustomizingType.Shoes => "Shoes",
            CustomizingType.Costumes => "Costumes",
            CustomizingType.SkinColor => "Body",
            _ => type.ToString()
        };
    }

    // ========================================
    // 기능 - 아이콘 캡처
    // ========================================

    /// <summary>
    /// 선택된 프리팹들 캡처
    /// </summary>
    private void CaptureSelectedPrefabs()
    {
        if (_selectedPrefabs.Count == 0)
        {
            ShowResult("캡처할 프리팹이 없습니다. 먼저 프리팹을 검색해주세요.", MessageType.Warning);
            return;
        }

        // 출력 폴더 확인
        EnsureOutputDirectory();

        // 프리팹 정보 생성
        var captureInfos = new List<PrefabCaptureInfo>();
        foreach (var prefab in _selectedPrefabs)
        {
            captureInfos.Add(new PrefabCaptureInfo(prefab, _filterType, prefab.name));
        }

        // 캡처 프로세서 실행
        var processor = CreateProcessor();
        processor.ProcessAll(captureInfos);

        // Sprite 임포트 설정 적용
        ApplySpriteImportSettings();

        // 결과 표시
        ShowResult(
            $"아이콘 생성 완료\n" +
            $"- 성공: {processor.SuccessCount}개\n" +
            $"- 실패: {processor.FailedCount}개\n" +
            $"- 스킵: {processor.SkippedCount}개",
            processor.FailedCount > 0 ? MessageType.Warning : MessageType.Info);
    }

    /// <summary>
    /// 모든 타입의 프리팹 일괄 캡처
    /// </summary>
    private void CaptureAllTypes()
    {
        // 출력 폴더 확인
        EnsureOutputDirectory();

        var allCaptureInfos = new List<PrefabCaptureInfo>();

        // 모든 타입 순회
        var types = System.Enum.GetValues(typeof(CustomizingType));
        foreach (CustomizingType type in types)
        {
            // SkinColor는 Body 폴더의 프리팹을 사용
            string typeFolderName = GetFolderNameForType(type);
            string searchPath = Path.Combine(_prefabFolderPath, typeFolderName).Replace("\\", "/");

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { searchPath });

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab != null)
                {
                    allCaptureInfos.Add(new PrefabCaptureInfo(prefab, type, prefab.name));
                }
            }
        }

        if (allCaptureInfos.Count == 0)
        {
            ShowResult("캡처할 프리팹이 없습니다.", MessageType.Warning);
            return;
        }

        // 캡처 프로세서 실행
        var processor = CreateProcessor();
        processor.ProcessAll(allCaptureInfos);

        // Sprite 임포트 설정 적용
        ApplySpriteImportSettings();

        // 결과 표시
        ShowResult(
            $"전체 타입 아이콘 생성 완료\n" +
            $"- 성공: {processor.SuccessCount}개\n" +
            $"- 실패: {processor.FailedCount}개\n" +
            $"- 스킵: {processor.SkippedCount}개",
            processor.FailedCount > 0 ? MessageType.Warning : MessageType.Info);
    }

    /// <summary>
    /// 생성된 PNG에 Sprite 임포트 설정 적용
    /// </summary>
    private void ApplySpriteImportSettings()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { _iconOutputPath });

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            IconFileExporter.ConfigureSpriteImportSettings(path);
        }
    }

    // ========================================
    // 기능 - SO 연결
    // ========================================

    /// <summary>
    /// SO에 아이콘 자동 연결
    /// </summary>
    private void AssignIconsToSOs()
    {
        var assigner = new CustomizingIconAutoAssigner(
            _iconOutputPath,
            _overwriteSO,
            _dryRunMode);

        if (string.IsNullOrEmpty(_soFolderPath))
        {
            assigner.AssignAll();
        }
        else
        {
            assigner.AssignInFolder(_soFolderPath);
        }

        string mode = _dryRunMode ? "[Dry Run] " : "";
        ShowResult(
            $"{mode}SO 아이콘 연결 완료\n" +
            $"- 성공: {assigner.SuccessCount}개\n" +
            $"- 덮어쓰기: {assigner.OverwrittenCount}개\n" +
            $"- 스킵: {assigner.SkippedCount}개\n" +
            $"- 실패: {assigner.FailedCount}개",
            assigner.FailedCount > 0 ? MessageType.Warning : MessageType.Info);
    }

    /// <summary>
    /// 아이콘이 없는 SO 확인
    /// </summary>
    private void CheckSOsWithoutIcon()
    {
        var assigner = new CustomizingIconAutoAssigner(_iconOutputPath, false, true);
        var sosWithoutIcon = assigner.FindSOsWithoutIcon();

        if (sosWithoutIcon.Count == 0)
        {
            ShowResult("모든 SO에 아이콘이 할당되어 있습니다.", MessageType.Info);
        }
        else
        {
            string message = $"아이콘이 없는 SO: {sosWithoutIcon.Count}개\n\n";
            foreach (var so in sosWithoutIcon)
            {
                message += $"- {so.name} (ItemId: {so.ItemId})\n";
            }

            ShowResult(message, MessageType.Warning);
        }
    }

    // ========================================
    // 프로세서 생성
    // ========================================

    /// <summary>
    /// 현재 설정에 맞는 캡처 프로세서 생성
    /// </summary>
    private IconCaptureProcessor CreateProcessor()
    {
        var processor = new IconCaptureProcessor(
            _iconResolution,
            GetAbsolutePath(_iconOutputPath),
            _useIconPrefix,
            _overwriteIcons);

        if (_useCustomCameraOffset)
        {
            processor.SetCustomCameraOffset(new IconCaptureCameraController.CameraOffset
            {
                cameraOffset = _customCameraOffset,
                sizeMultiplier = _customSizeMultiplier,
                defaultOrthoSize = _customDefaultOrthoSize
            });
        }

        return processor;
    }

    // ========================================
    // 유틸리티
    // ========================================

    /// <summary>
    /// 출력 디렉토리 확인 및 생성
    /// </summary>
    private void EnsureOutputDirectory()
    {
        string absolutePath = GetAbsolutePath(_iconOutputPath);

        if (!Directory.Exists(absolutePath))
        {
            Directory.CreateDirectory(absolutePath);
            AssetDatabase.Refresh();
            Debug.Log($"[IconCaptureEditorWindow] 출력 폴더 생성: {absolutePath}");
        }
    }

    /// <summary>
    /// Assets 상대 경로를 절대 경로로 변환
    /// </summary>
    private string GetAbsolutePath(string assetsPath)
    {
        if (assetsPath.StartsWith("Assets/") || assetsPath.StartsWith("Assets\\"))
        {
            return Path.Combine(Application.dataPath, assetsPath.Substring(7)).Replace("\\", "/");
        }

        return assetsPath;
    }

    /// <summary>
    /// 결과 메시지 표시
    /// </summary>
    private void ShowResult(string message, MessageType type)
    {
        _lastResultMessage = message;
        _lastResultType = type;
        _showResults = true;
        Debug.Log($"[IconCaptureEditorWindow] {message}");
    }
}
