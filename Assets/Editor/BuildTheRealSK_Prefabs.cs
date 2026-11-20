using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// BuildTheRealSK_Prefabs — shared assets for The real SK:
// Ground layer, ground materials, scope texture, blood-screen Volume
// profile + prefab, blood particle prefab, 5 zombie prefabs.
// Run before BuildTheRealSK_Scenes.
public static class BuildTheRealSK_Prefabs
{
    public static void Build()
    {
        AddGroundLayer();
        MakeMaterials();
        MakeScopeTexture();
        MakeBloodScreen();
        MakeBloodParticlePrefab();
        MakeZombiePrefabs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("PREFABS BUILD DONE");
    }

    private static void AddGroundLayer()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets.Length == 0) return;
        var so = new SerializedObject(assets[0]);
        SerializedProperty layers = so.FindProperty("layers");
        for (int i = 8; i < 32; i++)
        {
            SerializedProperty el = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(el.stringValue))
            {
                el.stringValue = "Ground";
                so.ApplyModifiedProperties();
                Debug.Log("Ground layer added at index " + i);
                return;
            }
            if (el.stringValue == "Ground") return;
        }
    }

    private static Material MakeMat(string name, Color color, float metallic = 0f, float smoothness = 0.5f)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Directory.CreateDirectory("Assets/Materials");
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = color;
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(m);
        return m;
    }

    private static void MakeMaterials()
    {
        // Matte ground (smoothness 0.3): a glossy ground reflected the sky
        // and washed the player view white at grazing angles.
        MakeMat("Asphalt", new Color(0.16f, 0.16f, 0.18f), 0f, 0.3f);
        MakeMat("Concrete", new Color(0.23f, 0.23f, 0.24f), 0f, 0.3f);
    }

    // Scope overlay: black screen with a transparent circle + crosshair.
    private static void MakeScopeTexture()
    {
        int s = 512;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        Vector2 c = new Vector2(s / 2f, s / 2f);
        float r = s * 0.36f;
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                Color col;
                if (d > r)
                {
                    col = Color.black;
                }
                else if (d > r - 3f)
                {
                    col = new Color(0f, 0f, 0f, 1f);
                }
                else
                {
                    col = new Color(0f, 0f, 0f, 0f);
                }
                // crosshair lines inside the circle
                if (d <= r && (Mathf.Abs(x - c.x) < 1.5f || Mathf.Abs(y - c.y) < 1.5f))
                {
                    col = new Color(0f, 0f, 0f, 0.85f);
                }
                tex.SetPixel(x, y, col);
            }
        }
        tex.Apply();
        string path = "Assets/Sprites/scope.png";
        Directory.CreateDirectory("Assets/Sprites");
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.SaveAndReimport();
        }
        Debug.Log("Scope texture created");
    }

    // Blood Screen Effect — Volume profile per tutorial video 22:
    // ACES, Bloom 0.43/1, Vignette #930C0C int 3 smooth 0.4 rounded,
    // Gaussian DoF start 78 / end 165 / max radius 1.3, Chromatic Aberration 1.
    private static void MakeBloodScreen()
    {
        string profilePath = "Assets/Settings/BloodScreenProfile.asset";
        Directory.CreateDirectory("Assets/Settings");
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);
        }
        Tonemapping tm;
        if (!profile.TryGet(out tm)) tm = profile.Add<Tonemapping>();
        tm.mode.overrideState = true;
        tm.mode.value = TonemappingMode.ACES;

        Bloom bloom;
        if (!profile.TryGet(out bloom)) bloom = profile.Add<Bloom>();
        bloom.threshold.overrideState = true; bloom.threshold.value = 0.43f;
        bloom.intensity.overrideState = true; bloom.intensity.value = 1f;

        Vignette vig;
        if (!profile.TryGet(out vig)) vig = profile.Add<Vignette>();
        vig.color.overrideState = true; vig.color.value = new Color(0x93 / 255f, 0x0C / 255f, 0x0C / 255f);
        vig.intensity.overrideState = true; vig.intensity.value = 3f;
        vig.smoothness.overrideState = true; vig.smoothness.value = 0.4f;
        vig.rounded.overrideState = true; vig.rounded.value = true;

        DepthOfField dof;
        if (!profile.TryGet(out dof)) dof = profile.Add<DepthOfField>();
        dof.mode.overrideState = true; dof.mode.value = DepthOfFieldMode.Gaussian;
        dof.gaussianStart.overrideState = true; dof.gaussianStart.value = 78f;
        dof.gaussianEnd.overrideState = true; dof.gaussianEnd.value = 165f;
        dof.gaussianMaxRadius.overrideState = true; dof.gaussianMaxRadius.value = 1.3f;

        ChromaticAberration ca;
        if (!profile.TryGet(out ca)) ca = profile.Add<ChromaticAberration>();
        ca.intensity.overrideState = true; ca.intensity.value = 1f;

        EditorUtility.SetDirty(profile);

        GameObject go = new GameObject("Blood Screen Effect");
        Volume v = go.AddComponent<Volume>();
        v.isGlobal = true;
        v.profile = profile;
        v.weight = 1f;
        string prefabPath = "Assets/Prefabs/BloodScreen.prefab";
        Directory.CreateDirectory("Assets/Prefabs");
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        Debug.Log("Blood screen prefab created");
    }

    private static ParticleSystem MakeBurstParticles(GameObject go, Color color, float lifetime, float speed, float size, int count)
    {
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = lifetime;
        main.loop = false;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = color;
        main.playOnAwake = false;
        var em = ps.emission;
        em.rateOverTime = 0;
        em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)count) });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        Material m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        m.SetColor("_BaseColor", color);
        renderer.material = m;
        return ps;
    }

    private static void MakeBloodParticlePrefab()
    {
        GameObject go = new GameObject("BloodParticle");
        MakeBurstParticles(go, new Color(0.6f, 0f, 0f), 0.6f, 3f, 0.08f, 30);
        PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/BloodParticle.prefab");
        Object.DestroyImmediate(go);
        Debug.Log("Blood particle prefab created");
    }

    public static ParticleSystem AddMuzzleFlash(GameObject parent, Vector3 localPos)
    {
        GameObject go = new GameObject("MuzzleFlash");
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        return MakeBurstParticles(go, new Color(1f, 0.75f, 0.2f), 0.12f, 6f, 0.15f, 12);
    }

    public static Avatar FindAvatar(string assetPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (Object a in assets)
        {
            if (a is Avatar av) return av;
        }
        return null;
    }

    private static void MakeZombiePrefabs()
    {
        GameObject bloodScreen = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BloodScreen.prefab");
        var defs = new (string, string, string, float, bool)[]
        {
            ("Zombie_Basic", "Assets/Models/Zombies/QuaterniusZombieKit/Characters/FBX/Zombie_Basic.fbx", "Assets/Animations/ZombieAIController.controller", 0.5f, false),
            ("Zombie_Ribcage", "Assets/Models/Zombies/QuaterniusZombieKit/Characters/FBX/Zombie_Ribcage.fbx", "Assets/Animations/ZombieAIController.controller", 0.5f, false),
            ("Zombie_Chubby", "Assets/Models/Zombies/QuaterniusZombieKit/Characters/FBX/Zombie_Chubby.fbx", "Assets/Animations/WaypointZombieController.controller", 3.5f, true),
            ("Zombie_Arm", "Assets/Models/Zombies/QuaterniusZombieKit/Characters/FBX/Zombie_Arm.fbx", "Assets/Animations/WaypointZombieController.controller", 3.5f, true),
            ("SkeletonZombie", "Assets/Models/Zombies/SkeletonZombie/fbx/skeleton-zombie.fbx", "Assets/Animations/SkeletonZombieController.controller", 0.5f, false),
        };
        foreach (var d in defs)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(d.Item2);
            if (model == null) { Debug.LogWarning("Model not found: " + d.Item2); continue; }
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            go.name = d.Item1;
            Animator anim = go.GetComponent<Animator>();
            if (anim == null) anim = go.AddComponent<Animator>();
            anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(d.Item3);
            anim.applyRootMotion = false;
            Avatar av = FindAvatar(d.Item2);
            if (av != null) anim.avatar = av;
            CapsuleCollider col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1f, 0f);
            col.radius = 0.3f;
            col.height = 2f;
            NavMeshAgent agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.5f;
            agent.height = 2f;
            agent.speed = d.Item4;
            agent.acceleration = 8f;
            agent.angularSpeed = 120f;
            if (d.Item5)
            {
                WaypointZombieAI ai = go.AddComponent<WaypointZombieAI>();
                ai.navAgent = agent;
                ai.bloodScreenEffect = bloodScreen;
            }
            else
            {
                ZombieAI ai = go.AddComponent<ZombieAI>();
                ai.navAgent = agent;
                ai.bloodScreenEffect = bloodScreen;
            }
            string path = "Assets/Prefabs/" + d.Item1 + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log("Zombie prefab: " + d.Item1);
        }
    }
}
