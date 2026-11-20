using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// BuildTheRealSK_Scenes — builds Map1, Map2 and MainMenu.
// Run after BuildTheRealSK_Prefabs. Each map gets: ground + buildings +
// roads/props, Player (CharacterController + camera + 2 weapons),
// PlayerUI (health/score/wave/ammo/crosshair/death screen), ScopeCanvas,
// Environment (GameManager + ZombieWaveSystem + NavMeshSurface bake),
// spawn points, directional light.
public static class BuildTheRealSK_Scenes
{
    private static readonly string KenneyInd = "Assets/Models/Maps/KenneyIndustrial/Models/FBX format/";
    private static readonly string KenneyRoads = "Assets/Models/Maps/KenneyRoads/Models/FBX format/";
    private static readonly string Downtown = "Assets/Models/Maps/DowntownCity/Exports/FBX (Unity)/";
    private static readonly string ZombieKit = "Assets/Models/Zombies/QuaterniusZombieKit/";

    public static void Build()
    {
        BuildMap("Map1", true);
        BuildMap("Map2", false);
        BuildMainMenu();
        AssetDatabase.SaveAssets();
        Debug.Log("SCENES BUILD DONE");
    }

    // ---------- helpers ----------
    private static GameObject LoadModel(string path)
    {
        GameObject m = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (m == null) Debug.LogWarning("Model not found: " + path);
        return m;
    }

    private static GameObject Place(string modelPath, Transform parent, Vector3 pos, float rotY, float scale = 1f)
    {
        GameObject m = LoadModel(modelPath);
        if (m == null) return null;
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(m);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
        go.transform.localScale = Vector3.one * scale;
        return go;
    }

    private static void AddMeshColliders(GameObject go)
    {
        if (go == null) return;
        foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.gameObject.GetComponent<MeshCollider>() == null && mf.sharedMesh != null)
            {
                MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.convex = false;
            }
        }
    }

    private static float ModelTileSize(string modelPath)
    {
        GameObject m = LoadModel(modelPath);
        if (m == null) return 4f;
        MeshFilter mf = m.GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return 4f;
        Vector3 s = mf.sharedMesh.bounds.size;
        return Mathf.Max(s.x, s.z);
    }

    // Pull a perimeter building position inward so the city reads from
    // spawn (2026-11-20); keep buildings off the road cross (roads run
    // along x=0 and z=0 out to +/-28).
    private static Vector3 PullBuildingInward(Vector3 p)
    {
        p.x *= 0.6f;
        p.z *= 0.6f;
        if (Mathf.Abs(p.x) < 4f && Mathf.Abs(p.z) > 10f) p.x = p.x < 0f ? -8f : 8f;
        if (Mathf.Abs(p.z) < 4f && Mathf.Abs(p.x) > 10f) p.z = p.z < 0f ? -8f : 8f;
        return p;
    }

    private static Text MakeText(Transform parent, string name, string content, int fontSize, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, TextAnchor align = TextAnchor.MiddleCenter, float width = 400f)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(width, 50);
        Text t = go.AddComponent<Text>();
        t.text = content;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = align;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return t;
    }

    private static Button MakeButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
    {
        GameObject go = DefaultControls.CreateButton(new DefaultControls.Resources());
        go.name = name;
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Text t = go.GetComponentInChildren<Text>();
        if (t != null) { t.text = label; t.fontSize = 22; }
        Image img = go.GetComponent<Image>();
        if (img != null) img.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);
        return go.GetComponent<Button>();
    }

    private static Slider MakeSlider(Transform parent, string name, Vector2 pos, Vector2 size, float min, float max, float value)
    {
        GameObject go = DefaultControls.CreateSlider(new DefaultControls.Resources());
        go.name = name;
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Slider s = go.GetComponent<Slider>();
        s.minValue = min; s.maxValue = max; s.value = value;
        return s;
    }

    private static Canvas MakeCanvas(string name, int sortingOrder)
    {
        GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = sortingOrder;
        CanvasScaler cs = go.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        return c;
    }

    private static AudioClip LoadAudio(string path)
    {
        AudioClip c = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (c == null) Debug.LogWarning("Audio not found: " + path);
        return c;
    }

    // ---------- player ----------
    private class UIRefs
    {
        public Slider healthSlider;
        public Text scoreText, highScoreText, waveNumberText, waveTimerText, ammoText;
        public DeathScreen deathScreen;
        public GameObject scopeCanvas;
    }

    private static UIRefs BuildPlayerUI()
    {
        Canvas canvas = MakeCanvas("PlayerUI", 0);
        Transform t = canvas.transform;
        UIRefs refs = new UIRefs();

        // Health slider (top-left)
        GameObject sliderGo = DefaultControls.CreateSlider(new DefaultControls.Resources());
        sliderGo.name = "HealthSlider";
        sliderGo.transform.SetParent(t, false);
        RectTransform srt = sliderGo.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 1f); srt.anchorMax = new Vector2(0f, 1f);
        srt.anchoredPosition = new Vector2(130, -30);
        srt.sizeDelta = new Vector2(220, 22);
        Slider hs = sliderGo.GetComponent<Slider>();
        hs.minValue = 0; hs.maxValue = 100; hs.value = 100;
        refs.healthSlider = hs;
        MakeText(t, "HealthLabel", "HEALTH", 18, Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(220, -58), TextAnchor.MiddleLeft);

        // Score (top-right)
        refs.scoreText = MakeText(t, "ScoreText", "Score: 0", 24, Color.white, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-230, -30), TextAnchor.MiddleRight);
        refs.highScoreText = MakeText(t, "HighScoreText", "Best: 0", 18, new Color(0.8f, 0.8f, 0.8f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-230, -60), TextAnchor.MiddleRight);

        // Wave (top-center)
        refs.waveNumberText = MakeText(t, "WaveNumberText", "Wave 1", 26, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -30));
        refs.waveTimerText = MakeText(t, "WaveTimerText", "Next wave in 10s", 18, new Color(0.8f, 0.8f, 0.8f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -60));

        // Ammo (bottom-right)
        refs.ammoText = MakeText(t, "AmmoText", "30 / 30", 26, Color.white, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-230, 40), TextAnchor.MiddleRight);

        // Crosshair (center)
        MakeText(t, "CrosshairText", "+", 30, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);

        // Death screen (full-screen, starts transparent)
        GameObject deathGo = new GameObject("DeathScreen", typeof(RectTransform));
        deathGo.transform.SetParent(t, false);
        RectTransform drt = deathGo.GetComponent<RectTransform>();
        drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
        drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
        Image dim = deathGo.AddComponent<Image>();
        dim.color = new Color(0.4f, 0f, 0f, 0f);
        Text dt = MakeText(deathGo.transform, "DeathText", "YOU DIED", 64, new Color(1f, 1f, 1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        DeathScreen ds = deathGo.AddComponent<DeathScreen>();
        ds.targetImage = dim;
        ds.targetText = dt;
        ds.duration = 5f;
        refs.deathScreen = ds;

        // Scope canvas (hidden by default)
        Canvas scope = MakeCanvas("ScopeCanvas", 10);
        scope.gameObject.SetActive(false);
        Sprite scopeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/scope.png");
        GameObject imgGo = new GameObject("ScopeImage", typeof(RectTransform));
        imgGo.transform.SetParent(scope.transform, false);
        RectTransform irt = imgGo.GetComponent<RectTransform>();
        irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
        irt.offsetMin = Vector2.zero; irt.offsetMax = Vector2.zero;
        Image img = imgGo.AddComponent<Image>();
        img.sprite = scopeSprite;
        img.color = Color.white;
        refs.scopeCanvas = scope.gameObject;

        return refs;
    }

    private static GameObject BuildPlayer(Vector3 spawnPos, float spawnRotY, UIRefs ui, GameObject[] weaponsOut)
    {
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = spawnPos;
        player.transform.rotation = Quaternion.Euler(0f, spawnRotY, 0f);

        CharacterController cc = player.AddComponent<CharacterController>();
        cc.radius = 0.5f; cc.height = 2f; cc.center = new Vector3(0f, 1f, 0f);
        cc.slopeLimit = 45f; cc.stepOffset = 0.3f; cc.skinWidth = 0.08f;

        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform, false);
        groundCheck.transform.localPosition = new Vector3(0f, 0.15f, 0f);

        PlayerMovement pm = player.AddComponent<PlayerMovement>();
        pm.movementSpeed = 5f; pm.gravity = -9.81f; pm.jumpForce = 1.5f;
        pm.groundCheck = groundCheck.transform;
        pm.groundMask = LayerMask.GetMask("Ground");
        pm.groundDistance = 0.4f;
        pm.footstepInterval = 0.5f;
        pm.currentHealth = 100;
        pm.healthSlider = ui.healthSlider;
        pm.deathScreen = ui.deathScreen;
        AudioClip[] steps = new AudioClip[5];
        for (int i = 0; i < 5; i++)
            steps[i] = LoadAudio("Assets/Audio/KenneyImpact/Audio/footstep_concrete_00" + i + ".ogg");
        pm.footstepSounds = steps;
        GameObject lf = new GameObject("LeftFoot"); lf.transform.SetParent(player.transform, false);
        pm.leftFootAudioSource = lf.AddComponent<AudioSource>();
        GameObject rf = new GameObject("RightFoot"); rf.transform.SetParent(player.transform, false);
        pm.rightFootAudioSource = rf.AddComponent<AudioSource>();

        // Main camera
        GameObject camGo = new GameObject("MainCamera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(player.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 1.62f, 0f);
        Camera cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 60f; cam.nearClipPlane = 0.01f; cam.farClipPlane = 1000f;
        camGo.AddComponent<AudioListener>();
        CameraController cc2 = camGo.AddComponent<CameraController>();
        cc2.playerTransform = player.transform;
        cc2.sensitivity = 2f; cc2.smoothSpeed = 10f;
        // Pitch clamp widened 2026-11-20 (was -30/+30: at max pitch the
        // whole frame was sky or ground — play-test #2).
        cc2.minXAngle = -85f; cc2.maxXAngle = 85f;

        // Scope camera (child of main camera)
        GameObject scopeCamGo = new GameObject("ScopeCamera");
        scopeCamGo.transform.SetParent(camGo.transform, false);
        scopeCamGo.transform.localPosition = Vector3.zero;
        Camera scopeCam = scopeCamGo.AddComponent<Camera>();
        scopeCam.fieldOfView = 20f; scopeCam.nearClipPlane = 0.01f; scopeCam.farClipPlane = 1000f;
        scopeCam.enabled = false;
        ScopeCameraController scc = scopeCamGo.AddComponent<ScopeCameraController>();
        scc.playerTransform = player.transform;
        scc.mainCameraController = cc2;
        scc.minXAngle = -85f; scc.maxXAngle = 85f;

        ScopeSystem scopeSys = camGo.AddComponent<ScopeSystem>();
        scopeSys.mainCamera = cam;
        scopeSys.scopeCamera = scopeCam;
        scopeSys.scopeImage = ui.scopeCanvas;
        scopeSys.zoomSpeed = 2f;

        // Weapons
        AudioClip gunshot = LoadAudio("Assets/Audio/710084__felix-blume__rifle-gunshot-one-shot.wav");
        GameObject bloodParticle = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BloodParticle.prefab");

        // Weapon 1 — Scar-H (M4 stats)
        GameObject scarModel = LoadModel("Assets/Models/Weapons/ScarH/scene.gltf");
        GameObject w1 = (GameObject)PrefabUtility.InstantiatePrefab(scarModel);
        w1.name = "Weapon_ScarH";
        w1.transform.SetParent(camGo.transform, false);
        w1.transform.localPosition = new Vector3(0.28f, -0.25f, 0.45f);
        // Hide the broken arm mesh (Object_96): its skin renders 39 m wide
        // (bad bind pose in the Sketchfab model; Blender conversion made it
        // worse). The rifle + hands (Object_92/94) render correctly.
        foreach (SkinnedMeshRenderer smr in w1.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr.gameObject.name == "Object_96" || smr.bounds.size.magnitude > 5f)
            {
                smr.gameObject.SetActive(false);
                Debug.Log("Hid broken Scar-H mesh: " + smr.gameObject.name + " bounds=" + smr.bounds.size);
            }
        }
        Animator a1 = w1.GetComponent<Animator>();
        if (a1 == null) a1 = w1.AddComponent<Animator>();
        a1.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/RifleController.controller");
        a1.applyRootMotion = false;
        a1.avatar = BuildTheRealSK_Prefabs.FindAvatar("Assets/Models/Weapons/ScarH/scene.gltf");
        GameObject fp1 = new GameObject("FirePoint");
        fp1.transform.SetParent(w1.transform, false);
        fp1.transform.localPosition = new Vector3(0f, 0.05f, 0.9f);
        ParticleSystem mf1 = BuildTheRealSK_Prefabs.AddMuzzleFlash(w1, new Vector3(0f, 0.05f, 0.9f));
        GameObject snd1 = new GameObject("GunAudio"); snd1.transform.SetParent(w1.transform, false);
        AudioSource as1 = snd1.AddComponent<AudioSource>();
        ShootingController sc1 = w1.AddComponent<ShootingController>();
        sc1.firePoint = fp1.transform;
        sc1.fireRate = 8f; sc1.fireRange = 40f; sc1.isAuto = true;
        sc1.damagePerShot = 60; sc1.maxAmmo = 30; sc1.currentAmmo = 30; sc1.reloadTime = 3f;
        sc1.animator = a1; sc1.muzzleFlash = mf1; sc1.bloodParticlePrefab = bloodParticle;
        sc1.soundAudioSource = as1; sc1.shootingSoundClip = gunshot; sc1.reloadSoundClip = null;
        sc1.currentAmmoText = ui.ammoText;

        // Weapon 2 — Sniper (M82 stats), rifle-only (the FPS Arms model's
        // skin is broken — its mesh renders 112 m wide — so no arms).
        GameObject w2 = new GameObject("Weapon_Sniper");
        w2.transform.SetParent(camGo.transform, false);
        w2.transform.localPosition = new Vector3(0.28f, -0.25f, 0.45f);
        Animator a2 = w2.AddComponent<Animator>();
        a2.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/SniperController.controller");
        a2.applyRootMotion = false;
        GameObject sniperModel = LoadModel("Assets/Models/Weapons/SniperRifle/scene.gltf");
        GameObject sniper = (GameObject)PrefabUtility.InstantiatePrefab(sniperModel);
        sniper.name = "SniperRifle";
        sniper.transform.SetParent(w2.transform, false);
        sniper.transform.localPosition = new Vector3(0.05f, -0.02f, 0.35f);
        sniper.transform.localScale = Vector3.one * 0.35f;
        GameObject fp2 = new GameObject("FirePoint");
        fp2.transform.SetParent(w2.transform, false);
        fp2.transform.localPosition = new Vector3(0.05f, 0f, 1.1f);
        ParticleSystem mf2 = BuildTheRealSK_Prefabs.AddMuzzleFlash(w2, new Vector3(0.05f, 0f, 1.1f));
        GameObject snd2 = new GameObject("GunAudio"); snd2.transform.SetParent(w2.transform, false);
        AudioSource as2 = snd2.AddComponent<AudioSource>();
        ShootingController sc2 = w2.AddComponent<ShootingController>();
        sc2.firePoint = fp2.transform;
        sc2.fireRate = 1f; sc2.fireRange = 100f; sc2.isAuto = false;
        sc2.damagePerShot = 100; sc2.maxAmmo = 30; sc2.currentAmmo = 30; sc2.reloadTime = 4.5f;
        sc2.animator = a2; sc2.muzzleFlash = mf2; sc2.bloodParticlePrefab = bloodParticle;
        sc2.soundAudioSource = as2; sc2.shootingSoundClip = gunshot; sc2.reloadSoundClip = null;
        sc2.currentAmmoText = ui.ammoText;
        w2.SetActive(false);

        weaponsOut[0] = w1;
        weaponsOut[1] = w2;
        return player;
    }

    // ---------- maps ----------
    private static void BuildMap(string sceneName, bool isMap1)
    {
        string scenePath = "Assets/Scenes/" + sceneName + ".unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath);
        foreach (GameObject go in scene.GetRootGameObjects())
            Object.DestroyImmediate(go);

        GameObject mapRoot = new GameObject("Map");
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(mapRoot.transform, false);
        ground.transform.localScale = new Vector3(8f, 1f, 8f);
        ground.layer = LayerMask.NameToLayer("Ground");
        Material groundMat = AssetDatabase.LoadAssetAtPath<Material>(isMap1 ? "Assets/Materials/Asphalt.mat" : "Assets/Materials/Concrete.mat");
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;

        System.Random rng = new System.Random(isMap1 ? 12345 : 777);

        if (isMap1)
        {
            // Perimeter industrial buildings (12)
            string[] b = { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l" };
            Vector3[] pos = {
                new Vector3(-32, 0, -32), new Vector3(0, 0, -34), new Vector3(32, 0, -32),
                new Vector3(-34, 0, 0), new Vector3(34, 0, 0),
                new Vector3(-32, 0, 32), new Vector3(0, 0, 34), new Vector3(32, 0, 32),
                new Vector3(-16, 0, -34), new Vector3(16, 0, -34),
                new Vector3(-34, 0, 16), new Vector3(34, 0, -16),
            };
            for (int i = 0; i < pos.Length; i++)
            {
                // Pull the building ring inward (2026-11-20): at the old
                // +/-32..34 perimeter the map looked empty from spawn.
                Vector3 p = PullBuildingInward(pos[i]);
                float rotY = Mathf.Atan2(-p.x, -p.z) * Mathf.Rad2Deg;
                GameObject g = Place(KenneyInd + "building-" + b[i] + ".fbx", mapRoot.transform, p, rotY);
                AddMeshColliders(g);
            }
            // Roads: cross through the centre
            float tile = ModelTileSize(KenneyRoads + "road-straight.fbx");
            Place(KenneyRoads + "road-crossing.fbx", mapRoot.transform, new Vector3(0, 0.01f, 0), 0);
            for (float x = tile; x <= 28; x += tile)
            {
                Place(KenneyRoads + "road-straight.fbx", mapRoot.transform, new Vector3(x, 0.01f, 0), 0);
                Place(KenneyRoads + "road-straight.fbx", mapRoot.transform, new Vector3(-x, 0.01f, 0), 0);
                Place(KenneyRoads + "road-straight.fbx", mapRoot.transform, new Vector3(0, 0.01f, x), 90);
                Place(KenneyRoads + "road-straight.fbx", mapRoot.transform, new Vector3(0, 0.01f, -x), 90);
            }
            // Props
            for (int i = 0; i < 3; i++)
            {
                Vector3 p = new Vector3((float)(rng.NextDouble() * 40 - 20), 0, (float)(rng.NextDouble() * 40 - 20));
                AddMeshColliders(Place(KenneyRoads + "dumpster.fbx", mapRoot.transform, p, (float)rng.NextDouble() * 360f));
            }
            for (int i = 0; i < 6; i++)
            {
                Vector3 p = new Vector3((float)(rng.NextDouble() * 44 - 22), 0, (float)(rng.NextDouble() * 44 - 22));
                Place(KenneyRoads + "construction-barrier.fbx", mapRoot.transform, p, (float)rng.NextDouble() * 360f);
            }
            for (int i = 0; i < 8; i++)
            {
                Vector3 p = new Vector3((float)(rng.NextDouble() * 44 - 22), 0, (float)(rng.NextDouble() * 44 - 22));
                Place(KenneyRoads + "construction-cone.fbx", mapRoot.transform, p, (float)rng.NextDouble() * 360f);
            }
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = new Vector3((float)(rng.NextDouble() * 44 - 22), 0, (float)(rng.NextDouble() * 44 - 22));
                AddMeshColliders(Place(KenneyRoads + "electricity-pole-single.fbx", mapRoot.transform, p, (float)rng.NextDouble() * 360f));
            }
        }
        else
        {
            // Downtown buildings
            string[] bld = { "Building_Large_2.fbx", "Building_Medium_2_001.fbx", "Building_Small_1.fbx" };
            Vector3[] pos = {
                new Vector3(-30, 0, -30), new Vector3(30, 0, 30), new Vector3(-30, 0, 30),
                new Vector3(30, 0, -30), new Vector3(0, 0, -33), new Vector3(-33, 0, 0), new Vector3(33, 0, 0),
            };
            for (int i = 0; i < pos.Length; i++)
            {
                // Pull the building ring inward (2026-11-20, see Map1).
                Vector3 p = PullBuildingInward(pos[i]);
                float rotY = Mathf.Atan2(-p.x, -p.z) * Mathf.Rad2Deg;
                GameObject g = Place(Downtown + bld[i % bld.Length], mapRoot.transform, p, rotY);
                AddMeshColliders(g);
            }
            // Street tiles along the centre cross
            float tile = ModelTileSize(ZombieKit + "Environment/FBX/Street_Straight.fbx");
            for (float x = 0; x <= 28; x += tile)
            {
                Place(ZombieKit + "Environment/FBX/Street_Straight.fbx", mapRoot.transform, new Vector3(x, 0.01f, 0), 0);
                if (x > 0) Place(ZombieKit + "Environment/FBX/Street_Straight.fbx", mapRoot.transform, new Vector3(-x, 0.01f, 0), 0);
                Place(ZombieKit + "Environment/FBX/Street_Straight.fbx", mapRoot.transform, new Vector3(0, 0.01f, x), 90);
                if (x > 0) Place(ZombieKit + "Environment/FBX/Street_Straight.fbx", mapRoot.transform, new Vector3(0, 0.01f, -x), 90);
            }
            // Sidewalks
            for (float x = -27; x <= 27; x += 6)
            {
                Place(Downtown + "Sidewalk_Straight_3m.fbx", mapRoot.transform, new Vector3(x, 0.01f, 4.5f), 0);
                Place(Downtown + "Sidewalk_Straight_3m.fbx", mapRoot.transform, new Vector3(x, 0.01f, -4.5f), 0);
            }
            // Street props
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = new Vector3((float)(rng.NextDouble() * 44 - 22), 0, (float)(rng.NextDouble() * 44 - 22));
                Place(ZombieKit + "Environment/FBX/StreetLights.fbx", mapRoot.transform, p, (float)rng.NextDouble() * 360f);
            }
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = new Vector3((float)(rng.NextDouble() * 44 - 22), 0, (float)(rng.NextDouble() * 44 - 22));
                Place(ZombieKit + "Environment/FBX/TrafficBarrier_1.fbx", mapRoot.transform, p, (float)rng.NextDouble() * 360f);
            }
            for (int i = 0; i < 6; i++)
            {
                Vector3 p = new Vector3((float)(rng.NextDouble() * 44 - 22), 0, (float)(rng.NextDouble() * 44 - 22));
                Place(ZombieKit + "Environment/FBX/TrafficCone_1.fbx", mapRoot.transform, p, (float)rng.NextDouble() * 360f);
            }
            for (int i = 0; i < 2; i++)
            {
                Vector3 p = new Vector3((float)(rng.NextDouble() * 40 - 20), 0, (float)(rng.NextDouble() * 40 - 20));
                Place(ZombieKit + "Environment/FBX/FireHydrant.fbx", mapRoot.transform, p, (float)rng.NextDouble() * 360f);
            }
        }

        // Directional light
        GameObject lightGo = new GameObject("Directional Light");
        Light l = lightGo.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1.1f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Spawn points (the last three are close to the player spawn so
        // zombies are visible within seconds of a wave starting —
        // 2026-11-20; the old corner-only points were 50+ m away).
        GameObject spawnRoot = new GameObject("SpawnPoints");
        Vector3[] spawnPos = isMap1
            ? new[] { new Vector3(-28, 0, -28), new Vector3(28, 0, -28), new Vector3(-28, 0, 28), new Vector3(28, 0, 28), new Vector3(0, 0, -28), new Vector3(14, 0, -14), new Vector3(-14, 0, -14), new Vector3(0, 0, -18) }
            : new[] { new Vector3(-28, 0, -28), new Vector3(28, 0, 28), new Vector3(0, 0, -28), new Vector3(14, 0, -14), new Vector3(-14, 0, -14), new Vector3(0, 0, -18) };
        Transform[] spawnTransforms = new Transform[spawnPos.Length];
        for (int i = 0; i < spawnPos.Length; i++)
        {
            GameObject sp = new GameObject("SpawnPoint" + (i + 1));
            sp.transform.SetParent(spawnRoot.transform, false);
            sp.transform.position = spawnPos[i];
            spawnTransforms[i] = sp.transform;
        }

        // NavMesh bake (before player/weapons exist, so their meshes aren't baked in)
        GameObject env = new GameObject("Environment");
        NavMeshSurface surface = env.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        surface.layerMask = -1;
        surface.size = new Vector3(80f, 20f, 80f);
        surface.center = Vector3.zero;
        surface.BuildNavMesh();
        Debug.Log(sceneName + " NavMesh baked");

        // Player + UI (spawn near the centre, facing down the road:
        // the old (0,0,25) edge spawn faced a 59 m-distant building).
        UIRefs ui = BuildPlayerUI();
        GameObject[] weapons = new GameObject[2];
        BuildPlayer(new Vector3(0, 0, 8), 180f, ui, weapons);

        // GameManager + WaveSystem
        GameManager gm = env.AddComponent<GameManager>();
        gm.weapons = weapons;
        gm.highScoreText = ui.highScoreText;
        gm.currentScoreText = ui.scoreText;
        ZombieWaveSystem ws = env.AddComponent<ZombieWaveSystem>();
        ws.zombiePrefabs = new GameObject[]
        {
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Zombie_Basic.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Zombie_Chubby.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Zombie_Ribcage.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Zombie_Arm.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SkeletonZombie.prefab"),
        };
        ws.spawnPoints = spawnTransforms;
        ws.timeBetweenWaves = 10f;
        ws.zombiesPerWave = 4;
        ws.WaveNumber = ui.waveNumberText;
        ws.WaveTimer = ui.waveTimerText;

        EditorSceneManager.SaveScene(scene);
        Debug.Log(sceneName + " scene built");
    }

    // ---------- main menu ----------
    private static void BuildMainMenu()
    {
        string scenePath = "Assets/Scenes/MainMenu.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath);
        foreach (GameObject go in scene.GetRootGameObjects())
            Object.DestroyImmediate(go);

        // EventSystem is required for ANY UI button to receive input.
        // (Missing it was why no menu button worked on the phone, 2026-11-20.)
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        GameObject camGo = new GameObject("MainCamera");
        camGo.tag = "MainCamera";
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGo.AddComponent<AudioListener>();

        Canvas canvas = MakeCanvas("MenuCanvas", 0);
        Transform t = canvas.transform;
        GameObject bg = new GameObject("Background", typeof(RectTransform));
        bg.transform.SetParent(t, false);
        RectTransform brt = bg.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
        brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.05f, 0.08f);

        MakeText(t, "Title", "THE REAL SK", 72, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -120));
        MakeText(t, "Subtitle", "FPS ZOMBIE SURVIVAL", 24, new Color(0.8f, 0.2f, 0.2f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -190));

        Button continueBtn = MakeButton(t, "ContinueButton", "PLAY", new Vector2(0, 60), new Vector2(320, 56));
        Button map1Btn = MakeButton(t, "Map1Button", "MAP 1 — INDUSTRIAL CITY", new Vector2(0, -10), new Vector2(320, 56));
        Button map2Btn = MakeButton(t, "Map2Button", "MAP 2 — DOWNTOWN CITY", new Vector2(0, -80), new Vector2(320, 56));
        Button settingsBtn = MakeButton(t, "SettingsButton", "SETTINGS", new Vector2(0, -150), new Vector2(320, 56));
        Button quitBtn = MakeButton(t, "QuitButton", "QUIT", new Vector2(0, -220), new Vector2(320, 56));

        // Settings panel (hidden)
        GameObject panel = new GameObject("SettingsPanel", typeof(RectTransform));
        panel.transform.SetParent(t, false);
        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f); prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = new Vector2(700, 700);
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.1f, 0.1f, 0.14f, 0.98f);
        panel.SetActive(false);

        MakeText(panel.transform, "SettingsTitle", "SETTINGS", 36, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -50));
        MakeText(panel.transform, "GameLabel", "DIFFICULTY", 22, new Color(0.8f, 0.8f, 0.8f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -110));
        Button normalBtn = MakeButton(panel.transform, "NormalButton", "NORMAL", new Vector2(-110, -160), new Vector2(200, 50));
        Button hardBtn = MakeButton(panel.transform, "HardButton", "HARDCORE", new Vector2(110, -160), new Vector2(200, 50));
        MakeText(panel.transform, "ControlsLabel", "CONTROLS", 22, new Color(0.8f, 0.8f, 0.8f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -230));
        MakeText(panel.transform, "SmoothLabel", "Mouse Smooth", 18, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-200, -270), TextAnchor.MiddleLeft, 200f);
        Slider smoothSlider = MakeSlider(panel.transform, "SmoothSlider", new Vector2(80, -270), new Vector2(220, 22), 1f, 10f, 10f);
        MakeText(panel.transform, "SensLabel", "Sensitivity", 18, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-200, -310), TextAnchor.MiddleLeft, 200f);
        Slider sensSlider = MakeSlider(panel.transform, "SensSlider", new Vector2(80, -310), new Vector2(220, 22), 1f, 5f, 2f);
        Button saveBtn = MakeButton(panel.transform, "SaveButton", "SAVE", new Vector2(-110, -280), new Vector2(200, 50));
        Button backBtn = MakeButton(panel.transform, "BackButton", "BACK", new Vector2(110, -280), new Vector2(200, 50));

        MakeText(t, "Credits", "Models: Kenney (CC0)  •  Quaternius (CC0)  •  Scar-H by Jainesh Pathak (CC-BY)  •  Sniper by MasterBozo (CC-BY)  •  FPS Arms by Matt Rafferty (CC-BY)  •  Sounds: felix.blume (CC0)  •  137SOUNDS (CC-BY)", 14, new Color(0.6f, 0.6f, 0.6f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 30));

        GameObject menuGo = new GameObject("Main_Menu");
        SettingsManager sm = menuGo.AddComponent<SettingsManager>();
        sm.normalDifficultyButton = normalBtn;
        sm.hardDifficultyButton = hardBtn;
        sm.saveButton = saveBtn;
        sm.map1Button = map1Btn;
        sm.map2Button = map2Btn;
        sm.continueButton = continueBtn;
        sm.smoothSpeedSlider = smoothSlider;
        sm.sensitivitySlider = sensSlider;

        // NOTE: do NOT wire buttons with AddListener() here — runtime listeners
        // added in editor code are NOT saved into the scene file, so those
        // buttons did nothing at runtime (found 2026-11-20). Assign the
        // fields instead; SettingsManager.Start() wires them at runtime.
        sm.settingsButton = settingsBtn;
        sm.backButton = backBtn;
        sm.quitButton = quitBtn;
        sm.settingsPanel = panel;

        EditorSceneManager.SaveScene(scene);
        Debug.Log("MainMenu scene built");
    }
}
