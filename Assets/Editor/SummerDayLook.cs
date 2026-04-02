using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;

public class SummerDayLook
{
    [MenuItem("Tools/Apply Summer Day Look")]
    public static void Apply()
    {
        // ========== 1. VOLUME PROFILE ==========
        string path = "Assets/01.Scenes/GameScene/PostProcessProfile.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null) { Debug.LogError("[Summer] Profile not found at " + path); return; }

        var subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var sub in subAssets)
            if (sub != profile && sub != null) Object.DestroyImmediate(sub, true);
        profile.components.Clear();

        // Tonemapping
        var tm = ScriptableObject.CreateInstance<Tonemapping>();
        tm.name = "Tonemapping"; tm.active = true;
        tm.mode.Override(TonemappingMode.ACES);
        AssetDatabase.AddObjectToAsset(tm, profile); profile.components.Add(tm);

        // Color Adjustments - BRIGHT, vivid, clean
        var ca = ScriptableObject.CreateInstance<ColorAdjustments>();
        ca.name = "ColorAdjustments"; ca.active = true;
        ca.postExposure.Override(0.15f);          // brighter!
        ca.contrast.Override(10f);
        ca.saturation.Override(18f);               // vivid summer
        ca.colorFilter.Override(new Color(1f, 1f, 1f, 1f));  // pure neutral
        AssetDatabase.AddObjectToAsset(ca, profile); profile.components.Add(ca);

        // Bloom - bright clean
        var bloom = ScriptableObject.CreateInstance<Bloom>();
        bloom.name = "Bloom"; bloom.active = true;
        bloom.threshold.Override(0.95f);
        bloom.intensity.Override(0.15f);           // very subtle
        bloom.scatter.Override(0.6f);
        bloom.tint.Override(new Color(1f, 1f, 1f, 1f));  // pure white
        AssetDatabase.AddObjectToAsset(bloom, profile); profile.components.Add(bloom);

        // Vignette - very subtle
        var vig = ScriptableObject.CreateInstance<Vignette>();
        vig.name = "Vignette"; vig.active = true;
        vig.intensity.Override(0.15f);
        vig.smoothness.Override(0.4f);
        AssetDatabase.AddObjectToAsset(vig, profile); profile.components.Add(vig);

        // White Balance - neutral, tiny warm (summer sun)
        var wb = ScriptableObject.CreateInstance<WhiteBalance>();
        wb.name = "WhiteBalance"; wb.active = true;
        wb.temperature.Override(3f);    // just slightly warm
        wb.tint.Override(0f);           // neutral
        AssetDatabase.AddObjectToAsset(wb, profile); profile.components.Add(wb);

        // Lift Gamma Gain - neutral clean
        var lgg = ScriptableObject.CreateInstance<LiftGammaGain>();
        lgg.name = "LiftGammaGain"; lgg.active = true;
        lgg.lift.Override(new Vector4(0f, 0f, 0f, 0f));
        lgg.gamma.Override(new Vector4(0f, 0f, 0f, 0f));
        lgg.gain.Override(new Vector4(0f, 0f, 0f, 0f));
        AssetDatabase.AddObjectToAsset(lgg, profile); profile.components.Add(lgg);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        Debug.Log("[Summer] Profile: bright summer day");

        // ========== 2. DIRECTIONAL LIGHT: bright summer sun ==========
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.type == LightType.Directional)
            {
                light.color = new Color(1f, 0.99f, 0.96f, 1f);  // near white, hint warm
                light.intensity = 1.1f;                            // bright!
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.45f;                      // soft shadows
                light.transform.eulerAngles = new Vector3(55f, -30f, 0f);
                EditorUtility.SetDirty(light);
                break;
            }
        }
        Debug.Log("[Summer] Light: bright 1.1, near-white");

        // ========== 3. ENVIRONMENT: bright summer sky ==========
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.85f, 0.92f, 1f, 1f);      // bright sky blue
        RenderSettings.ambientEquatorColor = new Color(0.90f, 0.90f, 0.88f, 1f); // neutral bright
        RenderSettings.ambientGroundColor = new Color(0.65f, 0.63f, 0.60f, 1f);  // warm ground
        Debug.Log("[Summer] Ambient: bright summer sky");

        // ========== 4. SAVE ==========
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Summer] === BRIGHT SUMMER DAY APPLIED ===");
    }
}
