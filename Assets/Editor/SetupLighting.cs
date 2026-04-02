using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public class SetupLighting
{
    [MenuItem("Tools/Setup Lighting")]
    public static void Setup()
    {
        // Environment Lighting
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.75f, 0.82f, 0.95f, 1f);     // Cool sky blue
        RenderSettings.ambientEquatorColor = new Color(0.85f, 0.82f, 0.78f, 1f);  // Warm neutral
        RenderSettings.ambientGroundColor = new Color(0.55f, 0.50f, 0.45f, 1f);   // Warm dark ground

        // Environment Reflections
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        RenderSettings.reflectionIntensity = 0.5f;

        // Fog disabled (optional for indoor)
        RenderSettings.fog = false;

        Debug.Log("[Lighting] Environment lighting set: Trilight ambient (sky blue / warm neutral / warm ground)");

        // Save scene
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[Lighting] Setup complete!");
    }
}
