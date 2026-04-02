using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;

public class FixPostProcess
{
    [MenuItem("Tools/Fix Post Process Profile")]
    public static void Fix()
    {
        string path = "Assets/08.Materials/PostProcessProfile.asset";

        // Delete old broken profile
        if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(path) != null)
            AssetDatabase.DeleteAsset(path);

        // Create new profile
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "PostProcessProfile";
        AssetDatabase.CreateAsset(profile, path);

        // === Tonemapping ===
        var tm = ScriptableObject.CreateInstance<Tonemapping>();
        tm.name = "Tonemapping";
        tm.active = true;
        tm.mode.Override(TonemappingMode.ACES);
        AssetDatabase.AddObjectToAsset(tm, profile);
        profile.components.Add(tm);

        // === Color Adjustments ===
        var ca = ScriptableObject.CreateInstance<ColorAdjustments>();
        ca.name = "ColorAdjustments";
        ca.active = true;
        ca.postExposure.Override(-0.15f);
        ca.contrast.Override(18f);
        ca.saturation.Override(25f);
        ca.colorFilter.Override(new Color(1f, 0.97f, 0.94f, 1f));
        AssetDatabase.AddObjectToAsset(ca, profile);
        profile.components.Add(ca);

        // === Bloom ===
        var bloom = ScriptableObject.CreateInstance<Bloom>();
        bloom.name = "Bloom";
        bloom.active = true;
        bloom.threshold.Override(0.9f);
        bloom.intensity.Override(0.3f);
        bloom.scatter.Override(0.65f);
        bloom.tint.Override(new Color(1f, 0.93f, 0.85f, 1f));
        AssetDatabase.AddObjectToAsset(bloom, profile);
        profile.components.Add(bloom);

        // === Vignette ===
        var vig = ScriptableObject.CreateInstance<Vignette>();
        vig.name = "Vignette";
        vig.active = true;
        vig.intensity.Override(0.28f);
        vig.smoothness.Override(0.35f);
        AssetDatabase.AddObjectToAsset(vig, profile);
        profile.components.Add(vig);

        // === White Balance ===
        var wb = ScriptableObject.CreateInstance<WhiteBalance>();
        wb.name = "WhiteBalance";
        wb.active = true;
        wb.temperature.Override(8f);
        wb.tint.Override(2f);
        AssetDatabase.AddObjectToAsset(wb, profile);
        profile.components.Add(wb);

        // === Lift Gamma Gain ===
        var lgg = ScriptableObject.CreateInstance<LiftGammaGain>();
        lgg.name = "LiftGammaGain";
        lgg.active = true;
        lgg.lift.Override(new Vector4(0.03f, 0f, -0.05f, 0f));
        lgg.gamma.Override(new Vector4(0.02f, 0.01f, -0.01f, 0f));
        lgg.gain.Override(new Vector4(-0.02f, 0f, 0.03f, 0f));
        AssetDatabase.AddObjectToAsset(lgg, profile);
        profile.components.Add(lgg);

        // Save everything
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Assign to Volume
        var volume = Object.FindObjectOfType<Volume>();
        if (volume != null)
        {
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            EditorUtility.SetDirty(volume);
            Debug.Log("[Fix] Profile assigned to Volume via sharedProfile");
        }

        // Camera post processing
        var cam = Camera.main;
        if (cam != null)
        {
            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camData.antialiasingQuality = AntialiasingQuality.High;
            EditorUtility.SetDirty(cam);
        }

        // Save scene
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("[Fix] === POST PROCESS PROFILE FIXED & SAVED ===");
    }
}
