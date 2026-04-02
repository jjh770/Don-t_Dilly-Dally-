using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;

public class SetupCutsceneVisuals
{
    public enum CutsceneStyle { BrightAnime, CinematicWarm, MoodyGrading }

    [MenuItem("Tools/Cutscene Visual Style/Bright Anime")]
    public static void ApplyBrightAnime() => Apply(CutsceneStyle.BrightAnime);

    [MenuItem("Tools/Cutscene Visual Style/Cinematic Warm")]
    public static void ApplyCinematicWarm() => Apply(CutsceneStyle.CinematicWarm);

    [MenuItem("Tools/Cutscene Visual Style/Moody Grading")]
    public static void ApplyMoodyGrading() => Apply(CutsceneStyle.MoodyGrading);

    static void Apply(CutsceneStyle style)
    {
        string profilePath = "Assets/08.Materials/CutscenePostProcess.asset";

        // Delete old if exists
        if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath) != null)
            AssetDatabase.DeleteAsset(profilePath);

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "CutscenePostProcess";
        AssetDatabase.CreateAsset(profile, profilePath);

        // --- MotionBlur (keep from original) ---
        var mb = ScriptableObject.CreateInstance<MotionBlur>();
        mb.name = "MotionBlur";
        mb.active = true;
        mb.intensity.Override(1f);
        mb.quality.Override(MotionBlurQuality.Low);
        mb.clamp.Override(0.05f);
        AssetDatabase.AddObjectToAsset(mb, profile);
        profile.components.Add(mb);

        // --- ChromaticAberration (keep from original) ---
        var ca = ScriptableObject.CreateInstance<ChromaticAberration>();
        ca.name = "ChromaticAberration";
        ca.active = true;
        ca.intensity.Override(0.15f);
        AssetDatabase.AddObjectToAsset(ca, profile);
        profile.components.Add(ca);

        // --- Style-specific effects ---
        switch (style)
        {
            case CutsceneStyle.BrightAnime:
                AddBrightAnime(profile);
                break;
            case CutsceneStyle.CinematicWarm:
                AddCinematicWarm(profile);
                break;
            case CutsceneStyle.MoodyGrading:
                AddMoodyGrading(profile);
                break;
        }

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // --- Assign to Volume ---
        var volume = Object.FindObjectOfType<Volume>();
        if (volume != null)
        {
            volume.isGlobal = true;
            volume.priority = 1;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            EditorUtility.SetDirty(volume);
            EditorUtility.SetDirty(volume.gameObject);
            Debug.Log($"[Cutscene] Profile assigned to Volume");
        }
        else
        {
            // Create a new Volume
            var go = new GameObject("CutscenePostProcessVolume");
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 1;
            vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            EditorUtility.SetDirty(go);
            Debug.Log($"[Cutscene] Created new Volume GameObject");
        }

        // --- Camera ---
        SetupCameras();

        // --- Lighting ---
        SetupLighting(style);

        // --- Directional Light ---
        SetupDirectionalLight(style);

        // --- Save ---
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log($"[Cutscene] === {style} style applied & saved ===");
    }

    // ===== BRIGHT ANIME =====
    static void AddBrightAnime(VolumeProfile p)
    {
        // Tonemapping
        var tm = ScriptableObject.CreateInstance<Tonemapping>();
        tm.name = "Tonemapping"; tm.active = true;
        tm.mode.Override(TonemappingMode.Neutral);
        AssetDatabase.AddObjectToAsset(tm, p); p.components.Add(tm);

        // Color Adjustments - clean anime, not too bright
        var col = ScriptableObject.CreateInstance<ColorAdjustments>();
        col.name = "ColorAdjustments"; col.active = true;
        col.postExposure.Override(-0.15f);
        col.contrast.Override(10f);
        col.saturation.Override(18f);
        col.colorFilter.Override(new Color(1f, 1f, 1f, 1f));
        AssetDatabase.AddObjectToAsset(col, p); p.components.Add(col);

        // Bloom - subtle, neutral tint to preserve colored lights
        var bloom = ScriptableObject.CreateInstance<Bloom>();
        bloom.name = "Bloom"; bloom.active = true;
        bloom.threshold.Override(0.95f);
        bloom.intensity.Override(0.2f);
        bloom.scatter.Override(0.5f);
        bloom.tint.Override(new Color(1f, 1f, 1f, 1f));
        AssetDatabase.AddObjectToAsset(bloom, p); p.components.Add(bloom);

        // Vignette - subtle
        var vig = ScriptableObject.CreateInstance<Vignette>();
        vig.name = "Vignette"; vig.active = true;
        vig.intensity.Override(0.18f);
        vig.smoothness.Override(0.4f);
        AssetDatabase.AddObjectToAsset(vig, p); p.components.Add(vig);

        // White Balance - neutral (preserve red/color accuracy)
        var wb = ScriptableObject.CreateInstance<WhiteBalance>();
        wb.name = "WhiteBalance"; wb.active = true;
        wb.temperature.Override(0f);
        wb.tint.Override(0f);
        AssetDatabase.AddObjectToAsset(wb, p); p.components.Add(wb);

        // Lift Gamma Gain - clean, minimal bias
        var lgg = ScriptableObject.CreateInstance<LiftGammaGain>();
        lgg.name = "LiftGammaGain"; lgg.active = true;
        lgg.lift.Override(new Vector4(0f, 0f, 0.02f, 0f));
        lgg.gamma.Override(new Vector4(0f, 0f, 0f, 0f));
        lgg.gain.Override(new Vector4(0f, 0f, 0f, 0f));
        AssetDatabase.AddObjectToAsset(lgg, p); p.components.Add(lgg);
    }

    // ===== CINEMATIC WARM =====
    static void AddCinematicWarm(VolumeProfile p)
    {
        var tm = ScriptableObject.CreateInstance<Tonemapping>();
        tm.name = "Tonemapping"; tm.active = true;
        tm.mode.Override(TonemappingMode.Neutral);
        AssetDatabase.AddObjectToAsset(tm, p); p.components.Add(tm);

        var col = ScriptableObject.CreateInstance<ColorAdjustments>();
        col.name = "ColorAdjustments"; col.active = true;
        col.postExposure.Override(-0.2f);
        col.contrast.Override(18f);
        col.saturation.Override(18f);
        col.colorFilter.Override(new Color(1f, 0.97f, 0.92f, 1f));
        AssetDatabase.AddObjectToAsset(col, p); p.components.Add(col);

        var bloom = ScriptableObject.CreateInstance<Bloom>();
        bloom.name = "Bloom"; bloom.active = true;
        bloom.threshold.Override(0.9f);
        bloom.intensity.Override(0.3f);
        bloom.scatter.Override(0.6f);
        bloom.tint.Override(new Color(1f, 1f, 1f, 1f));
        AssetDatabase.AddObjectToAsset(bloom, p); p.components.Add(bloom);

        var vig = ScriptableObject.CreateInstance<Vignette>();
        vig.name = "Vignette"; vig.active = true;
        vig.intensity.Override(0.3f);
        vig.smoothness.Override(0.3f);
        AssetDatabase.AddObjectToAsset(vig, p); p.components.Add(vig);

        var wb = ScriptableObject.CreateInstance<WhiteBalance>();
        wb.name = "WhiteBalance"; wb.active = true;
        wb.temperature.Override(8f);
        wb.tint.Override(2f);
        AssetDatabase.AddObjectToAsset(wb, p); p.components.Add(wb);

        var lgg = ScriptableObject.CreateInstance<LiftGammaGain>();
        lgg.name = "LiftGammaGain"; lgg.active = true;
        lgg.lift.Override(new Vector4(0.03f, 0.01f, -0.03f, 0f));
        lgg.gamma.Override(new Vector4(0.02f, 0.01f, -0.01f, 0f));
        lgg.gain.Override(new Vector4(-0.02f, 0f, 0.02f, 0f));
        AssetDatabase.AddObjectToAsset(lgg, p); p.components.Add(lgg);
    }

    // ===== MOODY GRADING =====
    static void AddMoodyGrading(VolumeProfile p)
    {
        var tm = ScriptableObject.CreateInstance<Tonemapping>();
        tm.name = "Tonemapping"; tm.active = true;
        tm.mode.Override(TonemappingMode.ACES);
        AssetDatabase.AddObjectToAsset(tm, p); p.components.Add(tm);

        var col = ScriptableObject.CreateInstance<ColorAdjustments>();
        col.name = "ColorAdjustments"; col.active = true;
        col.postExposure.Override(-0.3f);
        col.contrast.Override(30f);
        col.saturation.Override(15f);
        col.colorFilter.Override(new Color(0.95f, 0.93f, 1f, 1f));
        AssetDatabase.AddObjectToAsset(col, p); p.components.Add(col);

        var bloom = ScriptableObject.CreateInstance<Bloom>();
        bloom.name = "Bloom"; bloom.active = true;
        bloom.threshold.Override(0.95f);
        bloom.intensity.Override(0.25f);
        bloom.scatter.Override(0.5f);
        bloom.tint.Override(new Color(0.85f, 0.9f, 1f, 1f));
        AssetDatabase.AddObjectToAsset(bloom, p); p.components.Add(bloom);

        var vig = ScriptableObject.CreateInstance<Vignette>();
        vig.name = "Vignette"; vig.active = true;
        vig.intensity.Override(0.45f);
        vig.smoothness.Override(0.25f);
        AssetDatabase.AddObjectToAsset(vig, p); p.components.Add(vig);

        var wb = ScriptableObject.CreateInstance<WhiteBalance>();
        wb.name = "WhiteBalance"; wb.active = true;
        wb.temperature.Override(-5f);
        wb.tint.Override(-2f);
        AssetDatabase.AddObjectToAsset(wb, p); p.components.Add(wb);

        var lgg = ScriptableObject.CreateInstance<LiftGammaGain>();
        lgg.name = "LiftGammaGain"; lgg.active = true;
        lgg.lift.Override(new Vector4(-0.03f, -0.02f, 0.05f, -0.05f));
        lgg.gamma.Override(new Vector4(-0.01f, 0f, 0.02f, 0f));
        lgg.gain.Override(new Vector4(-0.02f, -0.01f, 0.05f, 0f));
        AssetDatabase.AddObjectToAsset(lgg, p); p.components.Add(lgg);
    }

    // ===== CAMERA =====
    static void SetupCameras()
    {
        foreach (var cam in Object.FindObjectsOfType<Camera>())
        {
            var camData = cam.GetUniversalAdditionalCameraData();
            if (camData != null)
            {
                camData.renderPostProcessing = true;
                camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                camData.antialiasingQuality = AntialiasingQuality.High;
                EditorUtility.SetDirty(cam);
                EditorUtility.SetDirty(camData);
            }
        }
        Debug.Log("[Cutscene] All cameras: Post Processing ON, SMAA High");
    }

    // ===== LIGHTING =====
    static void SetupLighting(CutsceneStyle style)
    {
        RenderSettings.fog = false;

        switch (style)
        {
            case CutsceneStyle.BrightAnime:
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.80f, 0.85f, 0.95f, 1f);
                RenderSettings.ambientEquatorColor = new Color(0.85f, 0.82f, 0.80f, 1f);
                RenderSettings.ambientGroundColor = new Color(0.55f, 0.50f, 0.48f, 1f);
                RenderSettings.reflectionIntensity = 0.4f;
                break;
            case CutsceneStyle.CinematicWarm:
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.65f, 0.70f, 0.85f, 1f);
                RenderSettings.ambientEquatorColor = new Color(0.75f, 0.70f, 0.65f, 1f);
                RenderSettings.ambientGroundColor = new Color(0.40f, 0.35f, 0.30f, 1f);
                RenderSettings.reflectionIntensity = 0.25f;
                break;
            case CutsceneStyle.MoodyGrading:
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.50f, 0.55f, 0.70f, 1f);
                RenderSettings.ambientEquatorColor = new Color(0.55f, 0.50f, 0.50f, 1f);
                RenderSettings.ambientGroundColor = new Color(0.30f, 0.28f, 0.30f, 1f);
                RenderSettings.reflectionIntensity = 0.15f;
                break;
        }
        Debug.Log($"[Cutscene] Environment lighting: {style}");
    }

    // ===== DIRECTIONAL LIGHT =====
    static void SetupDirectionalLight(CutsceneStyle style)
    {
        foreach (var light in Object.FindObjectsOfType<Light>())
        {
            if (light.type != LightType.Directional) continue;

            switch (style)
            {
                case CutsceneStyle.BrightAnime:
                    light.color = new Color(1f, 0.98f, 0.95f, 1f);
                    light.intensity = 0.85f;
                    light.shadows = LightShadows.Soft;
                    light.shadowStrength = 0.5f;
                    break;
                case CutsceneStyle.CinematicWarm:
                    light.color = new Color(1f, 0.92f, 0.80f, 1f);
                    light.intensity = 0.7f;
                    light.shadows = LightShadows.Soft;
                    light.shadowStrength = 0.65f;
                    break;
                case CutsceneStyle.MoodyGrading:
                    light.color = new Color(0.85f, 0.88f, 1f, 1f);
                    light.intensity = 0.55f;
                    light.shadows = LightShadows.Soft;
                    light.shadowStrength = 0.8f;
                    break;
            }
            EditorUtility.SetDirty(light);
            EditorUtility.SetDirty(light.gameObject);
            Debug.Log($"[Cutscene] Directional Light: {style}");
            break;
        }
    }
}
