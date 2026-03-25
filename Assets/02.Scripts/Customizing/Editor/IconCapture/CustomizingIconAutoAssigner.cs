using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

/// <summary>
/// CustomizingItemSO 아이콘 자동 연결 담당
/// - CustomizingItemSO 검색
/// - ItemId 기준으로 아이콘 검색
/// - 아이콘 Sprite 로드
/// - SO icon 필드에 자동 할당
/// - 덮어쓰기 옵션 처리
/// - 매칭 실패 로그 출력
/// </summary>
public class CustomizingIconAutoAssigner
{
    // ========================================
    // 필드
    // ========================================

    private readonly string _iconFolderPath;
    private readonly bool _overwriteExisting;
    private readonly bool _dryRunMode;

    // 결과 추적
    private readonly List<AssignResult> _results = new List<AssignResult>();

    // ========================================
    // 프로퍼티
    // ========================================

    public IReadOnlyList<AssignResult> Results => _results;
    public int SuccessCount => _results.FindAll(r => r.status == AssignStatus.Success).Count;
    public int FailedCount => _results.FindAll(r => r.status == AssignStatus.Failed).Count;
    public int SkippedCount => _results.FindAll(r => r.status == AssignStatus.Skipped).Count;
    public int OverwrittenCount => _results.FindAll(r => r.status == AssignStatus.Overwritten).Count;

    // ========================================
    // 생성자
    // ========================================

    /// <summary>
    /// 아이콘 자동 연결기 생성
    /// </summary>
    /// <param name="iconFolderPath">아이콘 폴더 경로 (Assets/...)</param>
    /// <param name="overwriteExisting">기존 아이콘이 있어도 덮어쓸지 여부</param>
    /// <param name="dryRunMode">미리보기 모드 (실제 변경 없이 결과만 확인)</param>
    public CustomizingIconAutoAssigner(string iconFolderPath, bool overwriteExisting = false, bool dryRunMode = false)
    {
        _iconFolderPath = iconFolderPath;
        _overwriteExisting = overwriteExisting;
        _dryRunMode = dryRunMode;
    }

    // ========================================
    // 공개 메서드
    // ========================================

    /// <summary>
    /// 프로젝트의 모든 CustomizingItemSO에 아이콘 자동 연결
    /// </summary>
    public void AssignAll()
    {
        // 모든 CustomizingItemSO 검색
        var allSOs = FindAllCustomizingItemSOs();
        AssignToList(allSOs);
    }

    /// <summary>
    /// 특정 폴더의 CustomizingItemSO에만 아이콘 연결
    /// </summary>
    /// <param name="soFolderPath">SO 폴더 경로</param>
    public void AssignInFolder(string soFolderPath)
    {
        var soList = FindCustomizingItemSOsInFolder(soFolderPath);
        AssignToList(soList);
    }

    /// <summary>
    /// 지정된 SO 목록에 아이콘 연결
    /// </summary>
    /// <param name="soList">CustomizingItemSO 목록</param>
    public void AssignToList(List<CustomizingItemSO> soList)
    {
        if (soList == null || soList.Count == 0)
        {
            Debug.LogWarning("[CustomizingIconAutoAssigner] 처리할 SO가 없습니다.");
            return;
        }

        _results.Clear();

        try
        {
            int total = soList.Count;
            for (int i = 0; i < total; i++)
            {
                var so = soList[i];

                // 진행률 표시
                float progress = (float)i / total;
                string mode = _dryRunMode ? "[Dry Run] " : "";
                bool cancelled = EditorUtility.DisplayCancelableProgressBar(
                    $"{mode}아이콘 연결 중",
                    $"[{i + 1}/{total}] {so.name}",
                    progress);

                if (cancelled)
                {
                    Debug.Log("[CustomizingIconAutoAssigner] 사용자가 작업을 취소했습니다.");
                    break;
                }

                ProcessSingleSO(so);
            }

            // Dry Run이 아닐 때만 저장
            if (!_dryRunMode)
            {
                AssetDatabase.SaveAssets();
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            LogResults();
        }
    }

    /// <summary>
    /// 아이콘이 없는 SO 목록 반환
    /// </summary>
    public List<CustomizingItemSO> FindSOsWithoutIcon()
    {
        var allSOs = FindAllCustomizingItemSOs();
        return allSOs.FindAll(so => so.PreviewIcon == null);
    }

    // ========================================
    // 비공개 메서드
    // ========================================

    /// <summary>
    /// 단일 SO에 아이콘 연결 처리
    /// </summary>
    private void ProcessSingleSO(CustomizingItemSO so)
    {
        if (so == null)
        {
            _results.Add(new AssignResult("null", AssignStatus.Failed, "SO가 null입니다."));
            return;
        }

        string itemId = so.ItemId;
        string soName = so.name;

        // 이미 아이콘이 있는 경우
        if (so.PreviewIcon != null && !_overwriteExisting)
        {
            _results.Add(new AssignResult(soName, AssignStatus.Skipped, "이미 아이콘이 할당되어 있습니다."));
            return;
        }

        // 아이콘 검색
        Sprite icon = FindIconSprite(itemId);

        if (icon == null)
        {
            // 이름으로도 검색 시도
            icon = FindIconSprite(soName);
        }

        if (icon == null)
        {
            _results.Add(new AssignResult(soName, AssignStatus.Failed, $"아이콘을 찾을 수 없습니다. (ItemId: {itemId})"));
            return;
        }

        // Dry Run 모드면 실제 변경 없이 결과만 기록
        if (_dryRunMode)
        {
            var status = so.PreviewIcon != null ? AssignStatus.Overwritten : AssignStatus.Success;
            _results.Add(new AssignResult(soName, status, $"[Dry Run] 아이콘을 찾았습니다: {icon.name}"));
            return;
        }

        // 아이콘 할당
        try
        {
            AssignIconToSO(so, icon);

            var status = so.PreviewIcon != null && _overwriteExisting ? AssignStatus.Overwritten : AssignStatus.Success;
            _results.Add(new AssignResult(soName, status, $"아이콘 할당 완료: {icon.name}"));
        }
        catch (System.Exception e)
        {
            _results.Add(new AssignResult(soName, AssignStatus.Failed, $"할당 중 오류: {e.Message}"));
        }
    }

    /// <summary>
    /// ItemId 기준으로 아이콘 Sprite 검색
    /// </summary>
    private Sprite FindIconSprite(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return null;
        }

        // 검색 우선순위
        string[] searchPatterns = new string[]
        {
            $"Icon_{itemId}",   // Icon_Hat_01
            itemId,              // Hat_01
            $"icon_{itemId}",   // icon_Hat_01 (소문자)
        };

        foreach (var pattern in searchPatterns)
        {
            // GUID로 검색
            string[] guids = AssetDatabase.FindAssets($"{pattern} t:Sprite", new[] { _iconFolderPath });

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);

                // 정확히 일치하는지 확인
                if (fileName.Equals(pattern, System.StringComparison.OrdinalIgnoreCase))
                {
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite != null)
                    {
                        return sprite;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// SO에 아이콘 할당 (SerializedObject 사용)
    /// </summary>
    private void AssignIconToSO(CustomizingItemSO so, Sprite icon)
    {
        SerializedObject serializedSO = new SerializedObject(so);
        SerializedProperty iconProperty = serializedSO.FindProperty("_previewIcon");

        if (iconProperty != null)
        {
            iconProperty.objectReferenceValue = icon;
            serializedSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(so);

            Debug.Log($"[CustomizingIconAutoAssigner] 아이콘 할당: {so.name} <- {icon.name}");
        }
        else
        {
            Debug.LogError($"[CustomizingIconAutoAssigner] _previewIcon 프로퍼티를 찾을 수 없습니다: {so.name}");
        }
    }

    /// <summary>
    /// 프로젝트의 모든 CustomizingItemSO 검색
    /// </summary>
    private List<CustomizingItemSO> FindAllCustomizingItemSOs()
    {
        var result = new List<CustomizingItemSO>();
        string[] guids = AssetDatabase.FindAssets("t:CustomizingItemSO");

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<CustomizingItemSO>(path);
            if (so != null)
            {
                result.Add(so);
            }
        }

        Debug.Log($"[CustomizingIconAutoAssigner] CustomizingItemSO 검색 완료: {result.Count}개");
        return result;
    }

    /// <summary>
    /// 특정 폴더의 CustomizingItemSO 검색
    /// </summary>
    private List<CustomizingItemSO> FindCustomizingItemSOsInFolder(string folderPath)
    {
        var result = new List<CustomizingItemSO>();
        string[] guids = AssetDatabase.FindAssets("t:CustomizingItemSO", new[] { folderPath });

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<CustomizingItemSO>(path);
            if (so != null)
            {
                result.Add(so);
            }
        }

        Debug.Log($"[CustomizingIconAutoAssigner] 폴더 내 SO 검색 완료: {result.Count}개 ({folderPath})");
        return result;
    }

    /// <summary>
    /// 처리 결과 로그 출력
    /// </summary>
    private void LogResults()
    {
        string mode = _dryRunMode ? "[Dry Run] " : "";

        Debug.Log("========================================");
        Debug.Log($"{mode}[CustomizingIconAutoAssigner] 아이콘 연결 결과");
        Debug.Log($"  성공: {SuccessCount}개");
        Debug.Log($"  덮어쓰기: {OverwrittenCount}개");
        Debug.Log($"  스킵: {SkippedCount}개");
        Debug.Log($"  실패: {FailedCount}개");

        // 실패 항목 상세 로그
        var failedItems = _results.FindAll(r => r.status == AssignStatus.Failed);
        if (failedItems.Count > 0)
        {
            Debug.LogWarning("실패 항목:");
            foreach (var item in failedItems)
            {
                Debug.LogWarning($"  - {item.soName}: {item.message}");
            }
        }

        Debug.Log("========================================");
    }
}

/// <summary>
/// 아이콘 연결 결과 상태
/// </summary>
public enum AssignStatus
{
    Success,     // 성공
    Failed,      // 실패
    Skipped,     // 스킵 (이미 존재)
    Overwritten  // 덮어쓰기
}

/// <summary>
/// 아이콘 연결 결과
/// </summary>
public struct AssignResult
{
    public string soName;
    public AssignStatus status;
    public string message;

    public AssignResult(string soName, AssignStatus status, string message)
    {
        this.soName = soName;
        this.status = status;
        this.message = message;
    }
}
