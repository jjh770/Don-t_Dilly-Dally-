using UnityEngine;
using UnityEditor;

public class DustParticlePrefabCreator : Editor
{
    [MenuItem("GameObject/Effects/Create Dust Particle", false, 10)]
    public static void CreateDustParticle()
    {
        // 새 게임 오브젝트 생성
        GameObject dustObj = new GameObject("DustParticle");

        // ParticleSystem 컴포넌트 추가
        ParticleSystem ps = dustObj.AddComponent<ParticleSystem>();

        // Main Module 설정
        var main = ps.main;
        main.duration = 1f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        main.startColor = new Color(1f, 1f, 1f, 0.3f);
        main.maxParticles = 500;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.01f; // 약간 위로 떠오르는 효과

        // Emission Module 설정
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 50f;

        // Shape Module 설정 (Box 모양으로 맵 전체에 분포)
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(30f, 10f, 30f); // 맵 크기에 맞게 조절

        // Velocity over Lifetime 설정 (바람 효과)
        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-0.05f, 0.1f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);

        // Noise Module 설정 (자연스러운 움직임)
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.3f;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.2f;
        noise.damping = true;

        // Color over Lifetime 설정
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0.0f),
                new GradientColorKey(Color.white, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0.0f),
                new GradientAlphaKey(0.3f, 0.2f),
                new GradientAlphaKey(0.3f, 0.8f),
                new GradientAlphaKey(0f, 1.0f)
            }
        );
        colorOverLifetime.color = gradient;

        // Size over Lifetime 설정
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.5f, 1f);
        sizeCurve.AddKey(1f, 0.5f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Renderer 설정
        var renderer = dustObj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.Distance;

        // 기본 파티클 머티리얼 찾기 또는 생성
        Material particleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Effects/Dust/DustMaterial.mat");
        if (particleMat == null)
        {
            // 기본 Particles/Standard Unlit 셰이더로 머티리얼 생성
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader != null)
            {
                particleMat = new Material(shader);
                particleMat.SetFloat("_Mode", 2); // Fade mode
                particleMat.color = new Color(1f, 1f, 1f, 0.5f);

                string matPath = "Assets/Effects/Dust/DustMaterial.mat";
                AssetDatabase.CreateAsset(particleMat, matPath);
                AssetDatabase.SaveAssets();
            }
        }

        if (particleMat != null)
        {
            renderer.material = particleMat;
        }

        // 프리팹으로 저장
        string prefabPath = "Assets/Effects/Dust/DustParticle.prefab";

        // 기존 프리팹이 있으면 삭제
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            AssetDatabase.DeleteAsset(prefabPath);
        }

        // 새 프리팹 생성
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(dustObj, prefabPath);

        // 씬에서 임시 오브젝트 제거하고 프리팹 인스턴스로 교체
        DestroyImmediate(dustObj);

        // 프리팹 인스턴스를 씬에 배치
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        instance.transform.position = Vector3.zero;

        // 생성된 오브젝트 선택
        Selection.activeGameObject = instance;

        Debug.Log("DustParticle 프리팹이 생성되었습니다: " + prefabPath);
    }

    [MenuItem("GameObject/Effects/Create Dust Particle (Light)", false, 11)]
    public static void CreateLightDustParticle()
    {
        // 밝은 환경용 먼지 파티클
        GameObject dustObj = new GameObject("DustParticle_Light");
        ParticleSystem ps = dustObj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.duration = 1f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 10f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.04f);
        main.startColor = new Color(0.9f, 0.85f, 0.7f, 0.2f); // 따뜻한 톤
        main.maxParticles = 300;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.005f;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 30f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(20f, 8f, 20f);

        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.2f;
        noise.frequency = 0.3f;
        noise.scrollSpeed = 0.1f;
        noise.damping = true;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.95f, 0.85f), 0.0f),
                new GradientColorKey(new Color(1f, 0.95f, 0.85f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0.0f),
                new GradientAlphaKey(0.2f, 0.3f),
                new GradientAlphaKey(0.2f, 0.7f),
                new GradientAlphaKey(0f, 1.0f)
            }
        );
        colorOverLifetime.color = gradient;

        var renderer = dustObj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        Material particleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Effects/Dust/DustMaterial.mat");
        if (particleMat != null)
        {
            renderer.material = particleMat;
        }

        string prefabPath = "Assets/Effects/Dust/DustParticle_Light.prefab";

        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            AssetDatabase.DeleteAsset(prefabPath);
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(dustObj, prefabPath);
        DestroyImmediate(dustObj);

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        instance.transform.position = Vector3.zero;
        Selection.activeGameObject = instance;

        Debug.Log("DustParticle_Light 프리팹이 생성되었습니다: " + prefabPath);
    }
}
