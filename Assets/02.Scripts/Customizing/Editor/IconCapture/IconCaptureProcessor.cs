using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 아이콘 생성 흐름 제어 담당
/// - 프리팹 리스트 순회
/// - 프리팹 인스턴스 생성
/// - 캡처 대상 배치
/// - 카메라 렌더 요청
/// - Texture2D 추출
/// - PNG 저장
/// - 실패 처리
/// </summary>
public class IconCaptureProcessor
{
    // ========================================
    // 필드
    // ========================================

    private IconCaptureCameraController _cameraController;
    private readonly int _resolution;
    private readonly string _outputFolderPath;
    private readonly bool _usePrefix;
    private readonly bool _overwriteExisting;

    // 결과 추적
    private readonly List<string> _successList = new List<string>();
    private readonly List<string> _failedList = new List<string>();
    private readonly List<string> _skippedList = new List<string>();

    // 캡처 위치 (화면 밖 또는 특정 위치)
    private readonly Vector3 _capturePosition = new Vector3(1000f, 0f, 0f);

    // ========================================
    // 프로퍼티
    // ========================================

    public IReadOnlyList<string> SuccessResults => _successList;
    public IReadOnlyList<string> FailedResults => _failedList;
    public IReadOnlyList<string> SkippedResults => _skippedList;
    public int SuccessCount => _successList.Count;
    public int FailedCount => _failedList.Count;
    public int SkippedCount => _skippedList.Count;

    // ========================================
    // 생성자
    // ========================================

    /// <summary>
    /// 아이콘 캡처 프로세서 생성
    /// </summary>
    /// <param name="resolution">아이콘 해상도</param>
    /// <param name="outputFolderPath">출력 폴더 경로 (Assets/...)</param>
    /// <param name="usePrefix">Icon_ 접두사 사용 여부</param>
    /// <param name="overwriteExisting">기존 파일 덮어쓰기 여부</param>
    public IconCaptureProcessor(int resolution, string outputFolderPath, bool usePrefix = true, bool overwriteExisting = false)
    {
        _resolution = resolution;
        _outputFolderPath = outputFolderPath;
        _usePrefix = usePrefix;
        _overwriteExisting = overwriteExisting;
    }

    // ========================================
    // 공개 메서드
    // ========================================

    /// <summary>
    /// 여러 프리팹을 순회하며 아이콘 일괄 생성
    /// </summary>
    /// <param name="prefabInfos">캡처할 프리팹 정보 목록</param>
    public void ProcessAll(List<PrefabCaptureInfo> prefabInfos)
    {
        if (prefabInfos == null || prefabInfos.Count == 0)
        {
            Debug.LogWarning("[IconCaptureProcessor] 처리할 프리팹이 없습니다.");
            return;
        }

        // 결과 초기화
        _successList.Clear();
        _failedList.Clear();
        _skippedList.Clear();

        // 카메라 컨트롤러 초기화
        _cameraController = new IconCaptureCameraController(_resolution);
        _cameraController.Initialize();

        try
        {
            int total = prefabInfos.Count;
            for (int i = 0; i < total; i++)
            {
                var info = prefabInfos[i];

                // 진행률 표시
                float progress = (float)i / total;
                bool cancelled = EditorUtility.DisplayCancelableProgressBar(
                    "아이콘 생성 중",
                    $"[{i + 1}/{total}] {info.prefab.name}",
                    progress);

                if (cancelled)
                {
                    Debug.Log("[IconCaptureProcessor] 사용자가 작업을 취소했습니다.");
                    break;
                }

                ProcessSingle(info);
            }
        }
        finally
        {
            // 정리 작업
            EditorUtility.ClearProgressBar();
            _cameraController.Cleanup();

            // AssetDatabase 갱신
            IconFileExporter.RefreshAssetDatabase();

            // 결과 로그
            LogResults();
        }
    }

    /// <summary>
    /// 단일 프리팹 아이콘 생성
    /// </summary>
    /// <param name="info">프리팹 캡처 정보</param>
    public void ProcessSingle(PrefabCaptureInfo info)
    {
        if (info.prefab == null)
        {
            Debug.LogError("[IconCaptureProcessor] 프리팹이 null입니다.");
            _failedList.Add("null prefab");
            return;
        }

        string prefabName = info.prefab.name;

        // 저장할 파일명 결정 (타입에 따라 변환)
        string saveFileName = ConvertToIconFileName(prefabName, info.customizingType);

        // 기존 파일 확인
        if (!_overwriteExisting && IconFileExporter.FileExists(_outputFolderPath, saveFileName, _usePrefix))
        {
            Debug.Log($"[IconCaptureProcessor] 스킵 (이미 존재): {saveFileName}");
            _skippedList.Add(saveFileName);
            return;
        }

        GameObject instance = null;

        try
        {
            // 1. 프리팹 인스턴스 생성
            instance = InstantiatePrefab(info.prefab);
            if (instance == null)
            {
                _failedList.Add(saveFileName);
                return;
            }

            // 2. 카메라 포커스 조정
            _cameraController.FocusOnTarget(instance, info.customizingType);

            // 3. 렌더링 및 텍스처 추출
            Texture2D capturedTexture = CaptureToTexture();
            if (capturedTexture == null)
            {
                _failedList.Add(saveFileName);
                return;
            }

            // 4. PNG 파일로 저장 (변환된 파일명 사용)
            string savedPath = IconFileExporter.SaveAsPng(capturedTexture, _outputFolderPath, saveFileName, _usePrefix);

            // 텍스처 메모리 해제
            Object.DestroyImmediate(capturedTexture);

            if (string.IsNullOrEmpty(savedPath))
            {
                _failedList.Add(saveFileName);
                return;
            }

            _successList.Add(saveFileName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[IconCaptureProcessor] 캡처 중 오류 발생: {prefabName}\n{e.Message}\n{e.StackTrace}");
            _failedList.Add(prefabName);
        }
        finally
        {
            // 인스턴스 정리
            if (instance != null)
            {
                Object.DestroyImmediate(instance);
            }
        }
    }

    // ========================================
    // 비공개 메서드
    // ========================================

    /// <summary>
    /// 프리팹 인스턴스 생성
    /// </summary>
    private GameObject InstantiatePrefab(GameObject prefab)
    {
        try
        {
            // 캡처 전용 위치에 인스턴스 생성
            GameObject instance = Object.Instantiate(prefab, _capturePosition, Quaternion.identity);
            instance.name = $"CaptureInstance_{prefab.name}";

            // 모든 렌더러가 활성화되어 있는지 확인
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                renderer.gameObject.SetActive(true);
            }

            return instance;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[IconCaptureProcessor] 프리팹 인스턴스 생성 실패: {prefab.name}\n{e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 카메라 렌더링 후 Texture2D로 변환
    /// </summary>
    private Texture2D CaptureToTexture()
    {
        Camera camera = _cameraController.CaptureCamera;
        RenderTexture renderTexture = _cameraController.RenderTexture;

        if (camera == null || renderTexture == null)
        {
            Debug.LogError("[IconCaptureProcessor] 카메라 또는 RenderTexture가 null입니다.");
            return null;
        }

        try
        {
            // 수동으로 카메라 렌더링
            camera.Render();

            // RenderTexture 내용을 Texture2D로 읽기
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = renderTexture;

            Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();

            RenderTexture.active = previousActive;

            return texture;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[IconCaptureProcessor] 텍스처 캡처 실패: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 프리팹 이름을 아이콘 파일명으로 변환
    /// SkinColor 타입: Body_01 -> SkinColor_01
    /// </summary>
    private string ConvertToIconFileName(string prefabName, CustomizingType type)
    {
        // SkinColor 타입은 Body -> SkinColor로 변환
        if (type == CustomizingType.SkinColor)
        {
            // Body_01 -> SkinColor_01
            if (prefabName.StartsWith("Body_"))
            {
                return "SkinColor_" + prefabName.Substring(5);
            }
            // Body01 -> SkinColor01
            if (prefabName.StartsWith("Body"))
            {
                return "SkinColor" + prefabName.Substring(4);
            }
        }

        // 다른 타입은 그대로 사용
        return prefabName;
    }

    /// <summary>
    /// 처리 결과 로그 출력
    /// </summary>
    private void LogResults()
    {
        Debug.Log("========================================");
        Debug.Log("[IconCaptureProcessor] 아이콘 생성 결과");
        Debug.Log($"  성공: {_successList.Count}개");
        Debug.Log($"  실패: {_failedList.Count}개");
        Debug.Log($"  스킵: {_skippedList.Count}개");

        if (_failedList.Count > 0)
        {
            Debug.LogWarning("실패 목록:");
            foreach (var failed in _failedList)
            {
                Debug.LogWarning($"  - {failed}");
            }
        }

        Debug.Log("========================================");
    }
}

/// <summary>
/// 프리팹 캡처 정보
/// </summary>
public struct PrefabCaptureInfo
{
    public GameObject prefab;
    public CustomizingType customizingType;
    public string itemId;

    public PrefabCaptureInfo(GameObject prefab, CustomizingType type, string itemId = null)
    {
        this.prefab = prefab;
        this.customizingType = type;
        this.itemId = string.IsNullOrEmpty(itemId) ? prefab?.name : itemId;
    }
}
