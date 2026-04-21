using UnityEngine;
using UnityEditor;

namespace DontDillyDally.EditorTools
{
    public static class SandstormVFXBuilder
    {
        private const string PrefabPath = "Assets/03.Prefabs/Map/MilitaryStage.prefab";
        private const string MaterialPath = "Assets/MapAssets/04.Military/Materials/M_SandFog.mat";
        private const string ContainerName = "SandstormVFX";

        private static readonly Color SandColor = new Color(0.88f, 0.76f, 0.55f, 1f);
        private static readonly Vector3 WindDirection = new Vector3(3f, 0.3f, 1.5f);

        [MenuItem("Tools/VFX/Build Sandstorm VFX")]
        public static void Build()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
            {
                Debug.LogError($"[Sandstorm] Failed to load prefab at {PrefabPath}.");
                return;
            }

            try
            {
                RebuildVFX(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"[Sandstorm] Built SandstormVFX into {PrefabPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RebuildVFX(GameObject root)
        {
            Transform existing = root.transform.Find(ContainerName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            GameObject container = new GameObject(ContainerName);
            container.transform.SetParent(root.transform, false);
            container.transform.localPosition = new Vector3(0f, 5f, 0f);

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                Debug.LogWarning($"[Sandstorm] Material not found at {MaterialPath}.");
            }

            BuildSandFog(container, material);
            BuildSandDrift(container, material);
        }

        private static void BuildSandFog(GameObject parent, Material material)
        {
            GameObject go = new GameObject("SandFog");
            go.transform.SetParent(parent.transform, false);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.duration = 30f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(10f, 16f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(12f, 24f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(SandColor);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            main.gravityModifier = 0f;
            main.playOnAwake = true;
            main.prewarm = true;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 10f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(100f, 10f, 100f);
            shape.randomDirectionAmount = 0.1f;

            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(WindDirection.x * 0.6f, WindDirection.x);
            velocity.y = new ParticleSystem.MinMaxCurve(WindDirection.y * 0.3f, WindDirection.y);
            velocity.z = new ParticleSystem.MinMaxCurve(WindDirection.z * 0.6f, WindDirection.z);

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(BuildFadeGradient(0.35f));

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, BuildSizeCurve());

            ParticleSystem.RotationOverLifetimeModule rotationOverLifetime = ps.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);

            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 1.2f;
            noise.frequency = 0.3f;
            noise.scrollSpeed = 0.5f;
            noise.quality = ParticleSystemNoiseQuality.Medium;

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = material;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.sortingFudge = -10f;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static void BuildSandDrift(GameObject parent, Material material)
        {
            GameObject go = new GameObject("SandDrift");
            go.transform.SetParent(parent.transform, false);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.duration = 10f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSize = new ParticleSystem.MinMaxCurve(2.5f, 5f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(SandColor);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;
            main.gravityModifier = 0f;
            main.playOnAwake = true;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 35f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(120f, 6f, 120f);
            Vector3 windDir = WindDirection.normalized;
            shape.rotation = Quaternion.LookRotation(windDir, Vector3.up).eulerAngles;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(BuildFadeGradient(0.25f));

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, BuildSizeCurve());

            ParticleSystem.RotationOverLifetimeModule rotationOverLifetime = ps.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);

            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 0.6f;
            noise.scrollSpeed = 1f;
            noise.quality = ParticleSystemNoiseQuality.Low;

            ParticleSystem.ForceOverLifetimeModule force = ps.forceOverLifetime;
            force.enabled = true;
            force.space = ParticleSystemSimulationSpace.World;
            force.x = WindDirection.x * 0.5f;
            force.y = 0f;
            force.z = WindDirection.z * 0.5f;

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.1f;
            renderer.lengthScale = 1.8f;
            renderer.sharedMaterial = material;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.sortingFudge = -5f;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static Gradient BuildFadeGradient(float peakAlpha)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(peakAlpha, 0.2f),
                    new GradientAlphaKey(peakAlpha, 0.8f),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }

        private static AnimationCurve BuildSizeCurve()
        {
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.6f);
            curve.AddKey(0.5f, 1f);
            curve.AddKey(1f, 1.2f);
            return curve;
        }
    }
}
