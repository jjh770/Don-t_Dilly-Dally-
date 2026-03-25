using System.IO;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 아이콘 파일 저장 담당
/// - Texture2D를 PNG로 저장
/// - 경로 생성
/// - 파일명 규칙 적용
/// - AssetDatabase.Refresh 호출
/// </summary>
public static class IconFileExporter
{
    // ========================================
    // 상수
    // ========================================

    private const string DEFAULT_ICON_PREFIX = "Icon_";
    private const string PNG_EXTENSION = ".png";

    // ========================================
    // 공개 메서드
    // ========================================

    /// <summary>
    /// Texture2D를 PNG 파일로 저장
    /// </summary>
    /// <param name="texture">저장할 텍스처</param>
    /// <param name="folderPath">저장 폴더 경로 (Assets/...)</param>
    /// <param name="fileName">파일 이름 (확장자 제외)</param>
    /// <param name="usePrefix">파일명에 Icon_ 접두사 사용 여부</param>
    /// <returns>저장된 파일의 Assets 상대 경로, 실패 시 null</returns>
    public static string SaveAsPng(Texture2D texture, string folderPath, string fileName, bool usePrefix = true)
    {
        if (texture == null)
        {
            Debug.LogError("[IconFileExporter] 저장할 텍스처가 null입니다.");
            return null;
        }

        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogError("[IconFileExporter] 폴더 경로가 비어있습니다.");
            return null;
        }

        if (string.IsNullOrEmpty(fileName))
        {
            Debug.LogError("[IconFileExporter] 파일 이름이 비어있습니다.");
            return null;
        }

        try
        {
            // 폴더 경로 확인 및 생성
            EnsureDirectoryExists(folderPath);

            // 파일명 생성
            string finalFileName = usePrefix ? $"{DEFAULT_ICON_PREFIX}{fileName}" : fileName;
            string fullPath = Path.Combine(folderPath, finalFileName + PNG_EXTENSION);

            // PNG 바이트 배열로 인코딩
            byte[] pngData = texture.EncodeToPNG();

            if (pngData == null)
            {
                Debug.LogError($"[IconFileExporter] PNG 인코딩 실패: {fileName}");
                return null;
            }

            // 파일 저장
            File.WriteAllBytes(fullPath, pngData);

            Debug.Log($"[IconFileExporter] 아이콘 저장 완료: {fullPath}");

            // Assets 상대 경로 반환
            return ConvertToAssetsPath(fullPath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[IconFileExporter] 파일 저장 중 오류 발생: {e.Message}\n{e.StackTrace}");
            return null;
        }
    }

    /// <summary>
    /// 여러 아이콘 저장 후 AssetDatabase 갱신
    /// </summary>
    public static void RefreshAssetDatabase()
    {
        AssetDatabase.Refresh();
        Debug.Log("[IconFileExporter] AssetDatabase 갱신 완료");
    }

    /// <summary>
    /// 저장된 PNG 파일에 Sprite 임포트 설정 적용
    /// </summary>
    /// <param name="assetPath">Assets 상대 경로</param>
    public static void ConfigureSpriteImportSettings(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning("[IconFileExporter] 임포트 설정 적용 실패: 경로가 비어있습니다.");
            return;
        }

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[IconFileExporter] TextureImporter를 찾을 수 없습니다: {assetPath}");
            return;
        }

        // Sprite 설정 적용
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        // 설정 저장 및 리임포트
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        Debug.Log($"[IconFileExporter] Sprite 임포트 설정 적용: {assetPath}");
    }

    /// <summary>
    /// 저장 경로에서 기존 파일 확인
    /// </summary>
    /// <param name="folderPath">저장 폴더 경로</param>
    /// <param name="fileName">파일 이름</param>
    /// <param name="usePrefix">접두사 사용 여부</param>
    /// <returns>파일 존재 여부</returns>
    public static bool FileExists(string folderPath, string fileName, bool usePrefix = true)
    {
        string finalFileName = usePrefix ? $"{DEFAULT_ICON_PREFIX}{fileName}" : fileName;
        string fullPath = Path.Combine(folderPath, finalFileName + PNG_EXTENSION);
        return File.Exists(fullPath);
    }

    /// <summary>
    /// ItemId 기반으로 아이콘 파일 경로 생성
    /// </summary>
    /// <param name="folderPath">저장 폴더 경로</param>
    /// <param name="itemId">아이템 ID</param>
    /// <returns>예상 파일 경로</returns>
    public static string GetExpectedIconPath(string folderPath, string itemId)
    {
        // 우선순위 1: Icon_{ItemId}.png
        string path1 = Path.Combine(folderPath, $"{DEFAULT_ICON_PREFIX}{itemId}{PNG_EXTENSION}");
        if (File.Exists(path1))
        {
            return ConvertToAssetsPath(path1);
        }

        // 우선순위 2: {ItemId}.png
        string path2 = Path.Combine(folderPath, $"{itemId}{PNG_EXTENSION}");
        if (File.Exists(path2))
        {
            return ConvertToAssetsPath(path2);
        }

        // 파일이 없는 경우 기본 경로 반환
        return ConvertToAssetsPath(path1);
    }

    // ========================================
    // 비공개 메서드
    // ========================================

    /// <summary>
    /// 디렉토리가 없으면 생성
    /// </summary>
    private static void EnsureDirectoryExists(string folderPath)
    {
        // 절대 경로로 변환
        string absolutePath = folderPath;
        if (!Path.IsPathRooted(folderPath))
        {
            absolutePath = Path.Combine(Application.dataPath,
                folderPath.StartsWith("Assets/") ? folderPath.Substring(7) : folderPath);
        }

        if (!Directory.Exists(absolutePath))
        {
            Directory.CreateDirectory(absolutePath);
            Debug.Log($"[IconFileExporter] 폴더 생성: {absolutePath}");
        }
    }

    /// <summary>
    /// 절대 경로를 Assets 상대 경로로 변환
    /// </summary>
    private static string ConvertToAssetsPath(string fullPath)
    {
        // 경로 구분자 통일
        fullPath = fullPath.Replace("\\", "/");
        string dataPath = Application.dataPath.Replace("\\", "/");

        // Assets 폴더부터 시작하는 상대 경로로 변환
        if (fullPath.StartsWith(dataPath))
        {
            return "Assets" + fullPath.Substring(dataPath.Length);
        }

        // 이미 Assets로 시작하면 그대로 반환
        if (fullPath.StartsWith("Assets/") || fullPath.StartsWith("Assets\\"))
        {
            return fullPath;
        }

        return fullPath;
    }
}
