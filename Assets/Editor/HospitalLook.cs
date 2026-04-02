using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;

public class HospitalLook
{
    [MenuItem("Tools/Apply Hospital Look")]
    public static void Apply()
    {
        // ========== 1. UPDATE VOLUME PROFILE ==========
        string path = "Assets/01.Scenes/GameScene/PostProcessProfile.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null) { Debug.LogError("Profile not found!"); return; }

        // Remove all existing sub-assets and components
        var subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var sub in subAssets)
        {
            if (sub != profile && sub != null)
                Object.DestroyImmediate(sub, true);
        }
        profile.components.Clear();

        // --- Tonemapping: ACES ---
        var tm = ScriptableObject.CreateInstance<Tonemapping>();
        tm.name = "Tonemapping"; tm.active = true;
        tm.mode.Override(TonemappingMode.ACES);
        AssetDatabase.AddObjectToAsset(tm, profile);
        profile.components.Add(tm);

        // --- Color Adjustments: clean, slightly cool ---
        var ca = ScriptableObject.CreateInstance<ColorAdjustments>();
        ca.name = "ColorAdjustments"; ca.active = true;
        ca.postExposure.Override(-0.1f);
        ca.contrast.Override(12f);
        ca.saturation.Override(10f);                              // less saturated = clean
        ca.colorFilter.Override(new Color(0.96f, 0.98f, 1f, 1f)); // slight cool tint
        AssetDatabase.AddObjectToAsset(ca, profile);
        profile.components.Add(ca);

        // --- Bloom: cool white, subtle ---
        var bloom = ScriptableObject.CreateInstance<Bloom>();
        bloom.name = "Bloom"; bloom.active = true;
        bloom.threshold.Override(0.95f);
        bloom.intensity.Override(0.2f);
        bloom.scatter.Override(0.6f);
        bloom.tint.Override(new Color(0.92f, 0.96f, 1f, 1f));    // cool blue-white bloom
        AssetDatabase.AddObjectToAsset(bloom, profile);
        profile.components.Add(bloom);

        // --- Vignette: subtle ---
        var vig = ScriptableObject.CreateInstance<Vignette>();
        vig.name = "Vignette"; vig.active = true;
        vig.intensity.Override(0.2f);
        vig.smoothness.Override(0.4f);
        AssetDatabase.AddObjectToAsset(vig, profile);
        profile.components.Add(vig);

        // --- White Balance: COOL ---
        var wb = ScriptableObject.CreateInstance<WhiteBalance>();
        wb.name = "WhiteBalance"; wb.active = true;
        wb.temperature.Override(-8f);   // cool! (negative = blue)
        wb.tint.Override(-3f);          // slight green/teal shift
        AssetDatabase.AddObjectToAsset(wb, profile);
        profile.components.Add(wb);

        // --- Lift Gamma Gain: cool shadows ---
        var lgg = ScriptableObject.CreateInstance<LiftGammaGain>();
        lgg.name = "LiftGammaGain"; lgg.active = true;
        lgg.lift.Override(new Vector4(-0.02f, 0f, 0.04f, 0f));   // blue shadows
        lgg.gamma.Override(new Vector4(0f, 0f, 0.01f, 0f));      // neutral mids
        lgg.gain.Override(new Vector4(0f, 0f, 0f, 0f));          // neutral highlights
        AssetDatabase.AddObjectToAsset(lgg, profile);
        profile.components.Add(lgg);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        Debug.Log("[Hospital] Profile updated: cool/clean look");

        // ========== 2. DIRECTIONAL LIGHT: neutral cool white ==========
        var lights = Object.FindObjectsOfType<Light>();
        foreach (var light in lights)
        {
            if (light.type == LightType.Directional)
            {
                light.color = new Color(0.95f, 0.97f, 1f, 1f);  // cool white
                light.intensity = 0.8f;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.5f;
                EditorUtility.SetDirty(light);
                Debug.Log("[Hospital] Directional Light: cool white, 0.8 intensity");
                break;
            }
        }

        // ========== 3. ENVIRONMENT: clinical cool ambient ==========
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.80f, 0.85f, 0.95f, 1f);     // cool blue sky
        RenderSettings.ambientEquatorColor = new Color(0.82f, 0.84f, 0.88f, 1f);  // neutral cool
        RenderSettings.ambientGroundColor = new Color(0.55f, 0.55f, 0.58f, 1f);   // cool gray
        Debug.Log("[Hospital] Environment: cool clinical ambient");

        // ========== 4. SAVE ==========
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Hospital] === HOSPITAL LOOK APPLIED & SAVED ===");
    }
}
