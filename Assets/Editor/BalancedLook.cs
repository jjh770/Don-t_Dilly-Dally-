using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;

public class BalancedLook
{
    [MenuItem("Tools/Apply Balanced Look")]
    public static void Apply()
    {
        string path = "Assets/01.Scenes/GameScene/PostProcessProfile.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null) { Debug.LogError("[Balanced] Profile not found!"); return; }

        var subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var sub in subAssets)
            if (sub != profile && sub != null) Object.DestroyImmediate(sub, true);
        profile.components.Clear();

        // Tonemapping
        var tm = ScriptableObject.CreateInstance<Tonemapping>();
        tm.name = "Tonemapping"; tm.active = true;
        tm.mode.Override(TonemappingMode.ACES);
        AssetDatabase.AddObjectToAsset(tm, profile); profile.components.Add(tm);

        // Color Adjustments - neutral, not too bright, not too dark
        var ca = ScriptableObject.CreateInstance<ColorAdjustments>();
        ca.name = "ColorAdjustments"; ca.active = true;
        ca.postExposure.Override(-0.05f);        // just a hair below neutral
        ca.contrast.Override(12f);
        ca.saturation.Override(15f);              // moderate vivid
        ca.colorFilter.Override(new Color(1f, 0.995f, 0.99f, 1f));  // barely warm
        AssetDatabase.AddObjectToAsset(ca, profile); profile.components.Add(ca);

        // Bloom - very subtle
        var bloom = ScriptableObject.CreateInstance<Bloom>();
        bloom.name = "Bloom"; bloom.active = true;
        bloom.threshold.Override(1f);
        bloom.intensity.Override(0.1f);
        bloom.scatter.Override(0.6f);
        bloom.tint.Override(new Color(1f, 1f, 1f, 1f));
        AssetDatabase.AddObjectToAsset(bloom, profile); profile.components.Add(bloom);

        // Vignette
        var vig = ScriptableObject.CreateInstance<Vignette>();
        vig.name = "Vignette"; vig.active = true;
        vig.intensity.Override(0.2f);
        vig.smoothness.Override(0.4f);
        AssetDatabase.AddObjectToAsset(vig, profile); profile.components.Add(vig);

        // White Balance - barely warm (between 노을+8 and 병원-8)
        var wb = ScriptableObject.CreateInstance<WhiteBalance>();
        wb.name = "WhiteBalance"; wb.active = true;
        wb.temperature.Override(2f);    // very slightly warm
        wb.tint.Override(0f);
        AssetDatabase.AddObjectToAsset(wb, profile); profile.components.Add(wb);

        // Lift Gamma Gain - clean neutral
        var lgg = ScriptableObject.CreateInstance<LiftGammaGain>();
        lgg.name = "LiftGammaGain"; lgg.active = true;
        lgg.lift.Override(new Vector4(0f, 0f, 0f, 0f));
        lgg.gamma.Override(new Vector4(0f, 0f, 0f, 0f));
        lgg.gain.Override(new Vector4(0f, 0f, 0f, 0f));
        AssetDatabase.AddObjectToAsset(lgg, profile); profile.components.Add(lgg);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        // ========== LIGHT: moderate bright ==========
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.type == LightType.Directional)
            {
                light.color = new Color(1f, 0.98f, 0.95f, 1f);  // very slight warm
                light.intensity = 0.9f;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.65f;                     // STRONGER shadows!
                light.transform.eulerAngles = new Vector3(50f, -30f, 0f);
                EditorUtility.SetDirty(light);
                break;
            }
        }

        // ========== ENVIRONMENT ==========
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.78f, 0.84f, 0.95f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.84f, 0.83f, 0.82f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.55f, 0.53f, 0.50f, 1f);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Balanced] === BALANCED LOOK APPLIED ===");
    }
}
