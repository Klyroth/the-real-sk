using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// BuildTheRealSK_Anim — step 1 of scene building.
// 1. Sets loop flags on zombie FBX clips (Idle/Walk/Run/Crawl loop).
// 2. Splits the single glTF animation clips into sub-clips
//    (Scar-H: Draw/Idle/Shoot/Reload; FPS Arms: Deploy/Idle/WalkRun/Grab)
//    by copying curve keyframes into new .anim assets.
//    Ranges found by sampling the glTF animation values:
//    Scar-H 11.67s: Draw 0-2 (weapon rises), Idle 2-3.25 (still),
//    Shoot 3.25-4 (trigger pull), Reload 4.5-6 (magazine out/in).
//    FPS Arms 28.33s: Deploy 0-5 (arms rise), Idle 5-13.5 (hold),
//    WalkRun 13.5-23 (slow bob), Grab 23-28.33 (locators active).
// 3. Builds the 5 Animator controllers used by the game.
public static class BuildTheRealSK_Anim
{
    private const string AnimDir = "Assets/Animations";

    public static void Build()
    {
        Directory.CreateDirectory(AnimDir);
        SetZombieLoopFlags();
        BuildSubClips();
        BuildRifleController();
        BuildSniperController();
        BuildZombieControllers();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("ANIM BUILD DONE");
    }

    // ---------- 1. Zombie clip loop flags ----------
    private static void SetZombieLoopFlags()
    {
        var loopClips = new Dictionary<string, string[]>
        {
            { "Assets/Models/Zombies/QuaterniusZombieKit/Characters/FBX/Zombie_Basic.fbx", new[] { "Idle", "Walk", "Run", "Crawl" } },
            { "Assets/Models/Zombies/QuaterniusZombieKit/Characters/FBX/Zombie_Chubby.fbx", new[] { "Idle", "Walk", "Run", "Crawl" } },
            { "Assets/Models/Zombies/QuaterniusZombieKit/Characters/FBX/Zombie_Ribcage.fbx", new[] { "Idle", "Walk", "Run", "Crawl" } },
            { "Assets/Models/Zombies/QuaterniusZombieKit/Characters/FBX/Zombie_Arm.fbx", new[] { "Idle", "Walk", "Run", "Crawl" } },
        };
        foreach (var kv in loopClips)
        {
            var importer = AssetImporter.GetAtPath(kv.Key) as ModelImporter;
            if (importer == null) { Debug.LogWarning("No importer: " + kv.Key); continue; }
            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            bool changed = false;
            foreach (ModelImporterClipAnimation c in clips)
            {
                string shortName = c.name.Contains("|") ? c.name.Substring(c.name.IndexOf('|') + 1) : c.name;
                foreach (string want in kv.Value)
                {
                    if (shortName == want && c.wrapMode != WrapMode.Loop)
                    {
                        c.wrapMode = WrapMode.Loop;
                        changed = true;
                    }
                }
            }
            if (changed)
            {
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
                Debug.Log("Loop flags set: " + kv.Key);
            }
        }
        // Skeleton zombie: clip names have no prefix.
        string skel = "Assets/Models/Zombies/SkeletonZombie/fbx/skeleton-zombie.fbx";
        var skelImporter = AssetImporter.GetAtPath(skel) as ModelImporter;
        if (skelImporter != null)
        {
            ModelImporterClipAnimation[] clips = skelImporter.clipAnimations;
            bool changed = false;
            foreach (ModelImporterClipAnimation c in clips)
            {
                if ((c.name == "ZombieWalk" || c.name == "ZombieRun") && c.wrapMode != WrapMode.Loop)
                {
                    c.wrapMode = WrapMode.Loop;
                    changed = true;
                }
            }
            if (changed) { skelImporter.clipAnimations = clips; skelImporter.SaveAndReimport(); }
        }
    }

    // ---------- 2. Sub-clips from glTF clips ----------
    private static AnimationClip LoadClip(string assetPath, string clipNameContains)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (Object a in assets)
        {
            if (a is AnimationClip clip && clip.name.Contains(clipNameContains))
                return clip;
        }
        Debug.LogWarning("Clip not found: " + clipNameContains + " in " + assetPath);
        return null;
    }

    private static AnimationClip TrimClip(AnimationClip src, float start, float end, string newName, bool loop)
    {
        string path = AnimDir + "/" + newName + ".anim";
        AnimationClip dst = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (dst == null)
        {
            dst = new AnimationClip();
            AssetDatabase.CreateAsset(dst, path);
        }
        dst.ClearCurves();
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(src);
        foreach (EditorCurveBinding b in bindings)
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(src, b);
            if (curve == null || curve.keys.Length == 0) continue;
            var keys = new List<Keyframe>();
            foreach (Keyframe k in curve.keys)
            {
                if (k.time >= start - 0.001f && k.time <= end + 0.001f)
                {
                    Keyframe nk = k;
                    nk.time = Mathf.Max(0f, k.time - start);
                    keys.Add(nk);
                }
            }
            if (keys.Count == 0)
            {
                float v = curve.Evaluate(start);
                keys.Add(new Keyframe(0f, v));
                keys.Add(new Keyframe(Mathf.Max(0.01f, end - start), v));
            }
            else if (keys.Count == 1)
            {
                keys.Add(new Keyframe(Mathf.Max(0.01f, end - start), keys[0].value));
            }
            AnimationUtility.SetEditorCurve(dst, b, new AnimationCurve(keys.ToArray()));
        }
        // Object reference curves (e.g. material swaps) — copy if in range.
        EditorCurveBinding[] objBindings = AnimationUtility.GetObjectReferenceCurveBindings(src);
        foreach (EditorCurveBinding b in objBindings)
        {
            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(src, b);
            var kept = new List<ObjectReferenceKeyframe>();
            foreach (ObjectReferenceKeyframe k in keys)
            {
                if (k.time >= start - 0.001f && k.time <= end + 0.001f)
                {
                    ObjectReferenceKeyframe nk = k;
                    nk.time = Mathf.Max(0f, k.time - start);
                    kept.Add(nk);
                }
            }
            if (kept.Count > 0) AnimationUtility.SetObjectReferenceCurve(dst, b, kept.ToArray());
        }
        dst.wrapMode = loop ? WrapMode.Loop : WrapMode.ClampForever;
        dst.legacy = false;
        EditorUtility.SetDirty(dst);
        return dst;
    }

    private static void BuildSubClips()
    {
        AnimationClip scar = LoadClip("Assets/Models/Weapons/ScarH/scene.gltf", "ScarH_Rig");
        if (scar != null)
        {
            TrimClip(scar, 0f, 2f, "ScarH_Draw", false);
            TrimClip(scar, 2f, 3.25f, "ScarH_Idle", true);
            TrimClip(scar, 3.25f, 4f, "ScarH_Shoot", true);
            TrimClip(scar, 4.5f, 6f, "ScarH_Reload", false);
        }
        AnimationClip arms = LoadClip("Assets/Models/Player/FPSArms/scene.gltf", "Take 001");
        if (arms != null)
        {
            TrimClip(arms, 0f, 5f, "Arms_Deploy", false);
            TrimClip(arms, 6f, 12f, "Arms_Idle", true);
            TrimClip(arms, 13.5f, 23f, "Arms_WalkRun", true);
            TrimClip(arms, 23f, 28.33f, "Arms_Grab", false);
        }
        AssetDatabase.SaveAssets();
    }

    // ---------- 3. Controllers ----------
    private static AnimatorState AddState(AnimatorController ctrl, string layerName, string stateName, AnimationClip clip)
    {
        AnimatorControllerLayer layer = ctrl.layers[0];
        AnimatorStateMachine sm = layer.stateMachine;
        AnimatorState st = sm.AddState(stateName);
        st.motion = clip;
        return st;
    }

    private static AnimatorController FreshController(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
            AssetDatabase.DeleteAsset(path);
        return AnimatorController.CreateAnimatorControllerAtPath(path);
    }

    private static void BuildRifleController()
    {
        string path = AnimDir + "/RifleController.controller";
        AnimatorController ctrl = FreshController(path);
        ctrl.AddParameter("shoot", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("reload", AnimatorControllerParameterType.Trigger);
        AnimatorState draw = AddState(ctrl, "Base Layer", "Draw", AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimDir + "/ScarH_Draw.anim"));
        AnimatorState idle = AddState(ctrl, "Base Layer", "Idle", AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimDir + "/ScarH_Idle.anim"));
        AnimatorState shoot = AddState(ctrl, "Base Layer", "Shoot", AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimDir + "/ScarH_Shoot.anim"));
        AnimatorState reload = AddState(ctrl, "Base Layer", "Reload", AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimDir + "/ScarH_Reload.anim"));
        ctrl.layers[0].stateMachine.defaultState = draw;
        AnimatorStateTransition t;
        t = draw.AddTransition(idle); t.hasExitTime = true; t.exitTime = 1f; t.duration = 0.1f;
        t = idle.AddTransition(shoot); t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.If, 0f, "shoot");
        t = shoot.AddTransition(idle); t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.IfNot, 0f, "shoot");
        t = ctrl.layers[0].stateMachine.AddAnyStateTransition(reload); t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.If, 0f, "reload");
        t = reload.AddTransition(idle); t.hasExitTime = true; t.exitTime = 1f; t.duration = 0.1f;
        EditorUtility.SetDirty(ctrl);
    }

    // Sniper: no animated arms (the FPS Arms model's skin is broken —
    // its mesh renders 112 m wide, and Blender conversion made it worse).
    // The sniper is a rifle-only weapon, so its controller has static
    // (no-motion) states; the muzzle flash + sound + ammo UI sell the shot.
    private static void BuildSniperController()
    {
        string path = AnimDir + "/SniperController.controller";
        AnimatorController ctrl = FreshController(path);
        ctrl.AddParameter("shoot", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("reload", AnimatorControllerParameterType.Trigger);
        AnimatorState draw = AddState(ctrl, "Base Layer", "Draw", null);
        AnimatorState idle = AddState(ctrl, "Base Layer", "Idle", null);
        AnimatorState shoot = AddState(ctrl, "Base Layer", "Shoot", null);
        AnimatorState reload = AddState(ctrl, "Base Layer", "Reload", null);
        ctrl.layers[0].stateMachine.defaultState = draw;
        AnimatorStateTransition t;
        t = draw.AddTransition(idle); t.hasExitTime = true; t.exitTime = 1f; t.duration = 0.1f;
        t = idle.AddTransition(shoot); t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.If, 0f, "shoot");
        t = shoot.AddTransition(idle); t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.IfNot, 0f, "shoot");
        t = ctrl.layers[0].stateMachine.AddAnyStateTransition(reload); t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.If, 0f, "reload");
        t = reload.AddTransition(idle); t.hasExitTime = true; t.exitTime = 1f; t.duration = 0.1f;
        EditorUtility.SetDirty(ctrl);
    }

    private static AnimationClip ZombieClip(string fbxPath, string shortName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (Object a in assets)
        {
            if (a is AnimationClip clip)
            {
                string n = clip.name;
                if (n == shortName || n.EndsWith("|" + shortName)) return clip;
            }
        }
        Debug.LogWarning("Zombie clip not found: " + shortName + " in " + fbxPath);
        return null;
    }

    private static void BuildZombieControllers()
    {
        string basic = "Assets/Models/Zombies/QuaterniusZombieKit/Characters/FBX/Zombie_Basic.fbx";
        string chubby = "Assets/Models/Zombies/QuaterniusZombieKit/Characters/FBX/Zombie_Chubby.fbx";
        string skel = "Assets/Models/Zombies/SkeletonZombie/fbx/skeleton-zombie.fbx";

        // ZombieAI type (capital-I params): Idle / Walk / Attack(Punch) / Dead(Death)
        string path = AnimDir + "/ZombieAIController.controller";
        AnimatorController ctrl = FreshController(path);
        ctrl.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
        AnimatorState idle = AddState(ctrl, "Base Layer", "Idle", ZombieClip(basic, "Idle"));
        AnimatorState walk = AddState(ctrl, "Base Layer", "Walk", ZombieClip(basic, "Walk"));
        AnimatorState attack = AddState(ctrl, "Base Layer", "Attack", ZombieClip(basic, "Punch"));
        AnimatorState dead = AddState(ctrl, "Base Layer", "Dead", ZombieClip(basic, "Death"));
        ctrl.layers[0].stateMachine.defaultState = idle;
        AnimatorStateTransition t;
        t = idle.AddTransition(walk); t.hasExitTime = false; t.duration = 0.2f;
        t.AddCondition(AnimatorConditionMode.If, 0f, "IsWalking");
        t = walk.AddTransition(idle); t.hasExitTime = false; t.duration = 0.2f;
        t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsWalking");
        t = ctrl.layers[0].stateMachine.AddAnyStateTransition(attack); t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.If, 0f, "IsAttacking");
        t = attack.AddTransition(idle); t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsAttacking");
        t = ctrl.layers[0].stateMachine.AddAnyStateTransition(dead); t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.If, 0f, "IsDead");
        EditorUtility.SetDirty(ctrl);

        // WaypointZombieAI type (lowercase-i params): Idle / Chase(Run) / Attack(Punch) / Dead(Death)
        path = AnimDir + "/WaypointZombieController.controller";
        AnimatorController ctrl2 = FreshController(path);
        ctrl2.AddParameter("isChasing", AnimatorControllerParameterType.Bool);
        ctrl2.AddParameter("isAttacking", AnimatorControllerParameterType.Bool);
        ctrl2.AddParameter("isDead", AnimatorControllerParameterType.Bool);
        AnimatorState idle2 = AddState(ctrl2, "Base Layer", "Idle", ZombieClip(chubby, "Idle"));
        AnimatorState chase = AddState(ctrl2, "Base Layer", "Chase", ZombieClip(chubby, "Run"));
        AnimatorState attack2 = AddState(ctrl2, "Base Layer", "Attack", ZombieClip(chubby, "Punch"));
        AnimatorState dead2 = AddState(ctrl2, "Base Layer", "Dead", ZombieClip(chubby, "Death"));
        ctrl2.layers[0].stateMachine.defaultState = idle2;
        t = idle2.AddTransition(chase); t.hasExitTime = false; t.duration = 0.2f;
        t.AddCondition(AnimatorConditionMode.If, 0f, "isChasing");
        t = chase.AddTransition(idle2); t.hasExitTime = false; t.duration = 0.2f;
        t.AddCondition(AnimatorConditionMode.IfNot, 0f, "isChasing");
        t = ctrl2.layers[0].stateMachine.AddAnyStateTransition(attack2); t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.If, 0f, "isAttacking");
        t = attack2.AddTransition(idle2); t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.IfNot, 0f, "isAttacking");
        t = ctrl2.layers[0].stateMachine.AddAnyStateTransition(dead2); t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.If, 0f, "isDead");
        EditorUtility.SetDirty(ctrl2);

        // Skeleton zombie (ZombieAI params, its own clips)
        path = AnimDir + "/SkeletonZombieController.controller";
        AnimatorController ctrl3 = FreshController(path);
        ctrl3.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);
        ctrl3.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
        ctrl3.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
        AnimatorState idle3 = AddState(ctrl3, "Base Layer", "Idle", ZombieClip(skel, "ZombieStumble"));
        AnimatorState walk3 = AddState(ctrl3, "Base Layer", "Walk", ZombieClip(skel, "ZombieWalk"));
        AnimatorState attack3 = AddState(ctrl3, "Base Layer", "Attack", ZombieClip(skel, "ZombieBite"));
        AnimatorState dead3 = AddState(ctrl3, "Base Layer", "Dead", ZombieClip(skel, "ZombieDie"));
        ctrl3.layers[0].stateMachine.defaultState = idle3;
        t = idle3.AddTransition(walk3); t.hasExitTime = false; t.duration = 0.2f;
        t.AddCondition(AnimatorConditionMode.If, 0f, "IsWalking");
        t = walk3.AddTransition(idle3); t.hasExitTime = false; t.duration = 0.2f;
        t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsWalking");
        t = ctrl3.layers[0].stateMachine.AddAnyStateTransition(attack3); t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.If, 0f, "IsAttacking");
        t = attack3.AddTransition(idle3); t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsAttacking");
        t = ctrl3.layers[0].stateMachine.AddAnyStateTransition(dead3); t.hasExitTime = false; t.duration = 0.1f;
        t.AddCondition(AnimatorConditionMode.If, 0f, "IsDead");
        EditorUtility.SetDirty(ctrl3);
    }
}
