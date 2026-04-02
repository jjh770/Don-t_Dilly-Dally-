using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;

public class UpdatePostProcess
{
    [MenuItem("Tools/Update Post Process")]
    public static void Update()
    {
        var volume = Object.FindObjectOfType<Volume>();
        if (volume == null || volume.profile == null)
        {
            Debug.LogError("[PP] No Volume or profile found!");
            return;
        }

        var profile = volume.profile;

        // Update Color Adjustments - darken slightly, more saturation
        if (profile.TryGet<ColorAdjustments>(out var colorAdj))
        {
            colorAdj.postExposure.Override(-0.1f);      // slightly darker
            colorAdj.contrast.Override(15f);              // more contrast
            colorAdj.saturation.Override(22f);            // more vivid
            colorAdj.colorFilter.Override(new Color(1f, 0.97f, 0.93f, 1f)); // warm tint
            Debug.Log("[PP] Color Adjustments updated");
        }

        // Update Bloom - subtle warm glow
        if (profile.TryGet<Bloom>(out var bloom))
        {
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(0.4f);
            bloom.scatter.Override(0.65f);
            bloom.tint.Override(new Color(1f, 0.95f, 0.88f, 1f)); // warm bloom
            Debug.Log("[PP] Bloom updated");
        }

        // Update Vignette
        if (profile.TryGet<Vignette>(out var vignette))
        {
            vignette.intensity.Override(0.3f);
            vignette.smoothness.Override(0.35f);
            Debug.Log("[PP] Vignette updated");
        }

        // Add Tonemapping if not present
        if (!profile.TryGet<Tonemapping>(out var tm))
        {
            tm = profile.Add<Tonemapping>(true);
        }
        tm.mode.Override(TonemappingMode.ACES);
        Debug.Log("[PP] ACES Tonemapping enabled");

        // Update Lift Gamma Gain - warm shadows
        if (profile.TryGet<LiftGammaGain>(out var lgg))
        {
            lgg.lift.Override(new Vector4(0.02f, 0.0f, -0.04f, 0f));    // warm shadows
            lgg.gamma.Override(new Vector4(0.02f, 0.01f, -0.01f, 0f));  // warm mids
            Debug.Log("[PP] Lift Gamma Gain updated");
        }

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        Debug.Log("[PP] All post-processing updates saved!");
    }
}
