using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;

public class FullSetup
{
    [MenuItem("Tools/Full Visual Setup")]
    public static void Run()
    {
        // ========== 1. VOLUME PROFILE ==========
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();

        // Tonemapping - ACES for natural highlight compression
        var tm = profile.Add<Tonemapping>(true);
        tm.mode.Override(TonemappingMode.ACES);

        // Color Adjustments - slightly dark, saturated, warm
        var ca = profile.Add<ColorAdjustments>(true);
        ca.postExposure.Override(-0.15f);
        ca.contrast.Override(18f);
        ca.saturation.Override(25f);
        ca.colorFilter.Override(new Color(1f, 0.97f, 0.94f, 1f));

        // Bloom - warm soft glow
        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.9f);
        bloom.intensity.Override(0.3f);
        bloom.scatter.Override(0.65f);
        bloom.tint.Override(new Color(1f, 0.93f, 0.85f, 1f));

        // Vignette
        var vig = profile.Add<Vignette>(true);
        vig.intensity.Override(0.28f);
        vig.smoothness.Override(0.35f);

        // Lift Gamma Gain - warm shadows, cool highlights
        var lgg = profile.Add<LiftGammaGain>(true);
        lgg.lift.Override(new Vector4(0.03f, 0.0f, -0.05f, 0f));
        lgg.gamma.Override(new Vector4(0.02f, 0.01f, -0.01f, 0f));
        lgg.gain.Override(new Vector4(-0.02f, 0f, 0.03f, 0f));

        // White Balance - slightly warm
        var wb = profile.Add<WhiteBalance>(true);
        wb.temperature.Override(8f);
        wb.tint.Override(2f);

        // Save profile
        string path = "Assets/08.Materials/PostProcessProfile.asset";
        // Delete old if exists
        if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(path) != null)
            AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(profile, path);
        AssetDatabase.SaveAssets();
        Debug.Log("[Setup] Volume Profile saved: " + path);

        // ========== 2. ASSIGN TO VOLUME ==========
        var volume = Object.FindObjectOfType<Volume>();
        if (volume != null)
        {
            volume.isGlobal = true;
            volume.priority = 1;
            volume.profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            EditorUtility.SetDirty(volume);
            EditorUtility.SetDirty(volume.gameObject);
            Debug.Log("[Setup] Profile assigned to Volume");
        }
        else
        {
            Debug.LogError("[Setup] No Volume found! Create PostProcessVolume first.");
            return;
        }

        // ========== 3. CAMERA POST PROCESSING ==========
        var cam = Camera.main;
        if (cam != null)
        {
            var camData = cam.GetUniversalAdditionalCameraData();
            if (camData != null)
            {
                camData.renderPostProcessing = true;
                camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                camData.antialiasingQuality = AntialiasingQuality.High;
                EditorUtility.SetDirty(cam);
                EditorUtility.SetDirty(camData);
                Debug.Log("[Setup] Camera: Post Processing ON, SMAA High");
            }
        }

        // ========== 4. DIRECTIONAL LIGHT ==========
        var lights = Object.FindObjectsOfType<Light>();
        foreach (var light in lights)
        {
            if (light.type == LightType.Directional)
            {
                light.color = new Color(1f, 0.96f, 0.88f, 1f);
                light.intensity = 0.7f;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.6f;
                light.transform.eulerAngles = new Vector3(50f, -30f, 0f);
                EditorUtility.SetDirty(light);
                EditorUtility.SetDirty(light.gameObject);
                Debug.Log("[Setup] Directional Light: warm 0.7 intensity, soft shadows");
                break;
            }
        }

        // ========== 5. ENVIRONMENT LIGHTING ==========
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.70f, 0.78f, 0.92f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.80f, 0.78f, 0.75f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.50f, 0.45f, 0.40f, 1f);
        RenderSettings.reflectionIntensity = 0.3f;
        RenderSettings.fog = false;
        Debug.Log("[Setup] Environment: Trilight ambient");

        // ========== 6. MARK SCENE DIRTY ==========
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        // ========== 7. SAVE SCENE ==========
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Setup] Scene saved!");
        Debug.Log("[Setup] === ALL DONE ===");
    }
}
