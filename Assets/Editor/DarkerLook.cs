using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;

public class DarkerLook
{
    [MenuItem("Tools/Apply Darker Look")]
    public static void Apply()
    {
        string path = "Assets/01.Scenes/GameScene/PostProcessProfile.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null) { Debug.LogError("[Darker] Profile not found!"); return; }

        var subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var sub in subAssets)
            if (sub != profile && sub != null) Object.DestroyImmediate(sub, true);
        profile.components.Clear();

        // Tonemapping
        var tm = ScriptableObject.CreateInstance<Tonemapping>();
        tm.name = "Tonemapping"; tm.active = true;
        tm.mode.Override(TonemappingMode.ACES);
        AssetDatabase.AddObjectToAsset(tm, profile); profile.components.Add(tm);

        // Color Adjustments - DARKER, more contrast
        var ca = ScriptableObject.CreateInstance<ColorAdjustments>();
        ca.name = "ColorAdjustments"; ca.active = true;
        ca.postExposure.Override(-0.25f);        // noticeably darker
        ca.contrast.Override(15f);               // more contrast = shadows pop
        ca.saturation.Override(12f);
        ca.colorFilter.Override(new Color(1f, 0.995f, 0.99f, 1f));
        AssetDatabase.AddObjectToAsset(ca, profile); profile.components.Add(ca);

        // Bloom - minimal
        var bloom = ScriptableObject.CreateInstance<Bloom>();
        bloom.name = "Bloom"; bloom.active = true;
        bloom.threshold.Override(1.1f);          // high threshold = almost no bloom
        bloom.intensity.Override(0.08f);          // barely there
        bloom.scatter.Override(0.5f);
        bloom.tint.Override(new Color(1f, 1f, 1f, 1f));
        AssetDatabase.AddObjectToAsset(bloom, profile); profile.components.Add(bloom);

        // Vignette
        var vig = ScriptableObject.CreateInstance<Vignette>();
        vig.name = "Vignette"; vig.active = true;
        vig.intensity.Override(0.22f);
        vig.smoothness.Override(0.4f);
        AssetDatabase.AddObjectToAsset(vig, profile); profile.components.Add(vig);

        // White Balance - neutral
        var wb = ScriptableObject.CreateInstance<WhiteBalance>();
        wb.name = "WhiteBalance"; wb.active = true;
        wb.temperature.Override(2f);
        wb.tint.Override(0f);
        AssetDatabase.AddObjectToAsset(wb, profile); profile.components.Add(wb);

        // Lift Gamma Gain
        var lgg = ScriptableObject.CreateInstance<LiftGammaGain>();
        lgg.name = "LiftGammaGain"; lgg.active = true;
        lgg.lift.Override(new Vector4(0f, 0f, 0f, 0f));
        lgg.gamma.Override(new Vector4(0f, 0f, 0f, 0f));
        lgg.gain.Override(new Vector4(0f, 0f, 0f, 0f));
        AssetDatabase.AddObjectToAsset(lgg, profile); profile.components.Add(lgg);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        // Light - lower intensity
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.type == LightType.Directional)
            {
                light.color = new Color(1f, 0.98f, 0.95f, 1f);
                light.intensity = 0.8f;             // lower
                light.shadowStrength = 0.7f;         // stronger shadows
                EditorUtility.SetDirty(light);
                break;
            }
        }

        // Environment - slightly darker ambient
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.72f, 0.78f, 0.88f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.78f, 0.77f, 0.76f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.45f, 0.43f, 0.40f, 1f);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Darker] === DARKER + OUTLINE FIX APPLIED ===");
    }
}
