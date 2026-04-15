using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public class ConvertMapMaterials
{
    [MenuItem("Tools/Convert Map Materials to FlatKit")]
    public static void Convert()
    {
        // Reference: use Medical_Pack_Mat as template
        var refMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/08.Materials/Medical_Pack_Mat.mat");
        if (refMat == null) { Debug.LogError("[Convert] Reference material not found!"); return; }
        var flatKitShader = refMat.shader;

        string[] matPaths = new string[]
        {
            // Winter
            "Assets/MapAssets/02.Winter/Materials/Color.mat",
            "Assets/MapAssets/02.Winter/Materials/Emission.mat",
            // Halloween
            "Assets/MapAssets/03.Halloween/Materials/Color.mat",
            "Assets/MapAssets/03.Halloween/Materials/Color 1.mat",
            "Assets/MapAssets/03.Halloween/Materials/Emissive.mat",
            "Assets/MapAssets/03.Halloween/Materials/Glass.mat",
            // Military
            "Assets/MapAssets/04.Military/Materials/Main_Texture.mat",
        };

        foreach (var path in matPaths)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { Debug.LogWarning("[Convert] Not found: " + path); continue; }

            // Save original properties
            var origTex = mat.GetTexture("_BaseMap");
            var origColor = mat.GetColor("_BaseColor");
            bool isTransparent = mat.GetFloat("_Surface") > 0.5f;
            bool hasEmission = mat.IsKeywordEnabled("_EMISSION");
            var origEmissionColor = mat.GetColor("_EmissionColor");
            string origName = mat.name;

            // Copy all properties from reference
            mat.shader = flatKitShader;
            mat.CopyPropertiesFromMaterial(refMat);

            // Restore original unique properties
            mat.name = origName;
            mat.SetTexture("_BaseMap", origTex);
            mat.SetColor("_BaseColor", origColor);

            // Handle Transparent (Glass)
            if (isTransparent)
            {
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_SrcBlend", 5);  // SrcAlpha
                mat.SetFloat("_DstBlend", 10); // OneMinusSrcAlpha
                mat.SetFloat("_ZWrite", 0);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = 3000;
            }
            else
            {
                mat.SetFloat("_Surface", 0);
                mat.SetFloat("_SrcBlend", 1);
                mat.SetFloat("_DstBlend", 0);
                mat.SetFloat("_ZWrite", 1);
                mat.SetOverrideTag("RenderType", "Opaque");
                mat.renderQueue = 2000;
            }

            // Handle Emission
            if (hasEmission)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", origEmissionColor);
            }

            EditorUtility.SetDirty(mat);
            Debug.Log("[Convert] Done: " + origName + " (" + path + ")" +
                (isTransparent ? " [Transparent]" : "") +
                (hasEmission ? " [Emission]" : ""));
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[Convert] === ALL 7 MAP MATERIALS CONVERTED ===");
    }
}
