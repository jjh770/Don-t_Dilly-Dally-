using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Flat Kit 툰 머터리얼 일괄 교체 도구
/// Unity 메뉴 > Tools > Toon Material Replacer 에서 실행
/// </summary>
public class ToonMaterialReplacer : EditorWindow
{
    // ──────────────────────────────────────────
    // 교체 대상 머터리얼 경로 매핑
    // Key   : 기존 머터리얼 경로 (Assets/... 상대경로)
    // Value : 대체 Toon 머터리얼 경로
    // ──────────────────────────────────────────
    private static readonly Dictionary<string, string> MaterialMap = new()
    {
        // ── Medical_Pack ──
        ["Assets/Medical_Pack/Materials/Color.mat"]     = "Assets/08.Materials/Toon/Toon_Color.mat",
        ["Assets/Medical_Pack/Materials/Metal.mat"]     = "Assets/08.Materials/Toon/Toon_Metal.mat",
        ["Assets/Medical_Pack/Materials/Glass.mat"]     = "Assets/08.Materials/Toon/Toon_Glass.mat",
        ["Assets/Medical_Pack/Materials/Glass_CAR.mat"] = "Assets/08.Materials/Toon/Toon_Glass_CAR.mat",
        ["Assets/Medical_Pack/Materials/Emission.mat"]  = "Assets/08.Materials/Toon/Toon_Emission.mat",
        ["Assets/Medical_Pack/Materials/Monitors.mat"]  = "Assets/08.Materials/Toon/Toon_Monitors.mat",

        // ── Characters ──
        ["Assets/Characters/Materials/Color.mat"]       = "Assets/08.Materials/Toon/Toon_Char_Color.mat",
        ["Assets/Characters/Materials/Emission.mat"]    = "Assets/08.Materials/Toon/Toon_Char_Emission.mat",
        ["Assets/Characters/Materials/Glass.mat"]       = "Assets/08.Materials/Toon/Toon_Char_Glass.mat",

        // ── Hospital_Interior ──
        ["Assets/Hospital_Interior/Materials/Color.mat"] = "Assets/08.Materials/Toon/Toon_Hospital.mat",
    };

    // ──────────────────────────────────────────
    // 검색 범위 (폴더 경로)
    // ──────────────────────────────────────────
    private static readonly string[] SearchFolders =
    {
        "Assets/01.Scenes",
        "Assets/03.Prefabs",
        "Assets/Medical_Pack/Prefabs",
        "Assets/Hospital_Interior/Prefabs",
        "Assets/Characters/Prefabs",
    };

    // ─── GUI 상태 ───
    private Vector2 _scroll;
    private bool _dryRun = true;
    private string _lastLog = "";
    private int _lastReplaced;
    private int _lastScanned;

    [MenuItem("Tools/Toon Material Replacer")]
    public static void ShowWindow()
    {
        var win = GetWindow<ToonMaterialReplacer>("Toon Material Replacer");
        win.minSize = new Vector2(480, 400);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Flat Kit 툰 머터리얼 일괄 교체 도구", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        // ── 매핑 테이블 미리보기 ──
        EditorGUILayout.LabelField("교체 매핑 목록", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(true))
        {
            foreach (var kv in MaterialMap)
            {
                EditorGUILayout.LabelField(
                    $"  {System.IO.Path.GetFileName(kv.Key),-20} → {System.IO.Path.GetFileName(kv.Value)}",
                    EditorStyles.miniLabel);
            }
        }

        EditorGUILayout.Space(8);

        // ── 옵션 ──
        _dryRun = EditorGUILayout.Toggle(
            new GUIContent("Dry Run (미리보기 전용)",
                "체크 시 실제로 저장하지 않고 교체될 목록만 출력합니다."),
            _dryRun);

        EditorGUILayout.Space(8);

        // ── 실행 버튼 ──
        var btnColor = _dryRun ? Color.cyan : new Color(1f, 0.6f, 0.3f);
        GUI.backgroundColor = btnColor;
        if (GUILayout.Button(_dryRun ? "▶ Dry Run 실행 (미리보기)" : "⚡ 실제 교체 실행", GUILayout.Height(36)))
            Run(_dryRun);
        GUI.backgroundColor = Color.white;

        if (!_dryRun)
        {
            EditorGUILayout.HelpBox(
                "실제 교체 실행 시 씬/프리팹 파일이 변경됩니다.\n" +
                "실행 전 Git 커밋 등으로 백업을 권장합니다.",
                MessageType.Warning);
        }

        // ── 결과 로그 ──
        if (!string.IsNullOrEmpty(_lastLog))
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField($"결과: {_lastScanned}개 검색 / {_lastReplaced}개 교체됨", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(200));
            EditorGUILayout.TextArea(_lastLog, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }
    }

    // ──────────────────────────────────────────
    // 핵심 교체 로직
    // ──────────────────────────────────────────
    private void Run(bool dryRun)
    {
        // 1. Toon 머터리얼 로드 & 유효성 확인
        var resolvedMap = new Dictionary<Material, Material>();
        foreach (var kv in MaterialMap)
        {
            var oldMat = AssetDatabase.LoadAssetAtPath<Material>(kv.Key);
            var newMat = AssetDatabase.LoadAssetAtPath<Material>(kv.Value);

            if (oldMat == null)
            {
                Debug.LogWarning($"[ToonReplacer] 원본 머터리얼 없음: {kv.Key}");
                continue;
            }
            if (newMat == null)
            {
                Debug.LogWarning($"[ToonReplacer] Toon 머터리얼 없음: {kv.Value}");
                continue;
            }
            resolvedMap[oldMat] = newMat;
        }

        if (resolvedMap.Count == 0)
        {
            EditorUtility.DisplayDialog("오류", "유효한 머터리얼 매핑이 없습니다.\n경로를 확인해 주세요.", "확인");
            return;
        }

        // 2. 프리팹 & 씬 파일 수집
        var guids = AssetDatabase.FindAssets("t:Prefab t:Scene", SearchFolders);
        var assetPaths = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Distinct()
            .OrderBy(p => p)
            .ToArray();

        var log = new System.Text.StringBuilder();
        int replaced = 0;
        int scanned  = 0;

        foreach (var path in assetPaths)
        {
            scanned++;

            // 씬은 LoadAssetAtPath<GameObject>가 안 되므로 Prefab만 처리 (씬은 별도)
            if (path.EndsWith(".unity"))
            {
                int count = ProcessScene(path, resolvedMap, dryRun, log);
                replaced += count;
                continue;
            }

            // Prefab 처리
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;

            var renderers = go.GetComponentsInChildren<Renderer>(true);
            bool prefabDirty = false;

            foreach (var rend in renderers)
            {
                var mats = rend.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] != null && resolvedMap.TryGetValue(mats[i], out var toonMat))
                    {
                        log.AppendLine(
                            $"[Prefab] {System.IO.Path.GetFileName(path)} / " +
                            $"{rend.gameObject.name}[{i}]: " +
                            $"{mats[i].name} → {toonMat.name}");

                        if (!dryRun) mats[i] = toonMat;
                        changed = true;
                        replaced++;
                    }
                }

                if (changed && !dryRun)
                {
                    rend.sharedMaterials = mats;
                    EditorUtility.SetDirty(rend);
                    prefabDirty = true;
                }
            }

            if (prefabDirty && !dryRun)
                PrefabUtility.SavePrefabAsset(go);
        }

        // 3. 저장
        if (!dryRun)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        _lastLog      = log.Length > 0 ? log.ToString() : "(교체 대상 없음)";
        _lastReplaced = replaced;
        _lastScanned  = scanned;

        string mode = dryRun ? "[DRY RUN]" : "[실제 교체]";
        Debug.Log($"[ToonReplacer] {mode} 완료: {scanned}개 에셋 검색 / {replaced}개 교체됨");
        Repaint();
    }

    // ──────────────────────────────────────────
    // 씬 파일 처리 (YAML 텍스트 직접 치환)
    // ──────────────────────────────────────────
    private static int ProcessScene(
        string scenePath,
        Dictionary<Material, Material> resolvedMap,
        bool dryRun,
        System.Text.StringBuilder log)
    {
        string text = System.IO.File.ReadAllText(scenePath);
        int count = 0;

        foreach (var kv in resolvedMap)
        {
            string oldGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(kv.Key));
            string newGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(kv.Value));

            if (string.IsNullOrEmpty(oldGuid) || string.IsNullOrEmpty(newGuid)) continue;

            // YAML 내 guid: <oldGuid> 를 guid: <newGuid> 로 치환
            string pattern  = $"guid: {oldGuid}";
            string replaced = $"guid: {newGuid}";

            int occurrences = CountOccurrences(text, pattern);
            if (occurrences > 0)
            {
                log.AppendLine(
                    $"[Scene] {System.IO.Path.GetFileName(scenePath)}: " +
                    $"{kv.Key.name} → {kv.Value.name} ({occurrences}곳)");

                if (!dryRun)
                    text = text.Replace(pattern, replaced);

                count += occurrences;
            }
        }

        if (count > 0 && !dryRun)
            System.IO.File.WriteAllText(scenePath, text);

        return count;
    }

    private static int CountOccurrences(string source, string pattern)
    {
        int count = 0;
        int idx   = 0;
        while ((idx = source.IndexOf(pattern, idx, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += pattern.Length;
        }
        return count;
    }
}
