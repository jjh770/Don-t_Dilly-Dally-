using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;

public class SetupPostProcessing
{
    [MenuItem("Tools/Setup Post Processing")]
    public static void Setup()
    {
        // Create Volume Profile asset
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();

        // === Bloom ===
        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.9f);
        bloom.intensity.Override(0.35f);
        bloom.scatter.Override(0.7f);

        // === Color Adjustments ===
        var colorAdj = profile.Add<ColorAdjustments>(true);
        colorAdj.postExposure.Override(0.15f);
        colorAdj.contrast.Override(12f);
        colorAdj.saturation.Override(18f);
        colorAdj.colorFilter.Override(new Color(1f, 0.98f, 0.95f, 1f)); // warm tint

        // === Vignette ===
        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.25f);
        vignette.smoothness.Override(0.4f);

        // === Lift Gamma Gain (subtle warm shift) ===
        var lgg = profile.Add<LiftGammaGain>(true);
        lgg.lift.Override(new Vector4(0f, 0f, -0.03f, 0f));   // shadows slightly warm
        lgg.gamma.Override(new Vector4(0.02f, 0.01f, 0f, 0f));  // mids warm
        lgg.gain.Override(new Vector4(0f, 0f, 0f, 0f));

        // Save the profile
        string profilePath = "Assets/08.Materials/PostProcessProfile.asset";
        AssetDatabase.CreateAsset(profile, profilePath);
        AssetDatabase.SaveAssets();

        // Find the Volume in scene
        var volume = Object.FindObjectOfType<Volume>();
        if (volume != null)
        {
            volume.isGlobal = true;
            volume.priority = 1;
            volume.profile = profile;
            EditorUtility.SetDirty(volume);
            Debug.Log("[PostProcess] Volume Profile assigned to PostProcessVolume");
        }
        else
        {
            Debug.LogWarning("[PostProcess] No Volume found in scene!");
        }

        // Also check camera has Post Processing enabled
        var cam = Camera.main;
        if (cam != null)
        {
            var camData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (camData != null)
            {
                camData.renderPostProcessing = true;
                EditorUtility.SetDirty(camData);
                Debug.Log("[PostProcess] Post Processing enabled on Main Camera");
            }
        }

        Debug.Log("[PostProcess] Setup complete! Profile saved to: " + profilePath);
    }
}
