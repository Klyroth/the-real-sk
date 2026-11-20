using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Dumps model contents (animation clips, hierarchy) to model-inspection.txt
// so the scene-build script can wire Animator controllers correctly.
public static class InspectModels
{
    public static void Inspect()
    {
        var sb = new StringBuilder();
        string[] paths =
        {
            "Assets/Models/Zombies/QuaterniusZombieKit/Characters/FBX/Zombie_Basic.fbx",
            "Assets/Models/Zombies/QuaterniusZombieKit/Characters/FBX/Zombie_Chubby.fbx",
            "Assets/Models/Zombies/QuaterniusZombieKit/Characters/FBX/Zombie_Ribcage.fbx",
            "Assets/Models/Zombies/QuaterniusZombieKit/Characters/FBX/Zombie_Arm.fbx",
            "Assets/Models/Zombies/SkeletonZombie/fbx/skeleton-zombie.fbx",
            "Assets/Models/Weapons/ScarH/scene.gltf",
            "Assets/Models/Weapons/SniperRifle/scene.gltf",
            "Assets/Models/Player/FPSArms/scene.gltf",
        };

        foreach (string path in paths)
        {
            sb.AppendLine("=== " + path + " ===");
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets == null || assets.Length == 0)
            {
                sb.AppendLine("  (no assets — not imported?)");
                continue;
            }
            foreach (Object a in assets)
            {
                if (a is AnimationClip clip)
                {
                    sb.AppendLine("  CLIP: " + clip.name + " (" + clip.length.ToString("F2") + "s, loop=" + clip.isLooping + ")");
                }
                else if (a is GameObject go)
                {
                    sb.AppendLine("  ROOT: " + go.name + " children=" + go.transform.childCount);
                    foreach (Transform c in go.transform)
                    {
                        sb.AppendLine("    - " + c.name + " (children=" + c.childCount + ")");
                    }
                    SkinnedMeshRenderer[] smr = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    sb.AppendLine("    skinnedMeshes=" + smr.Length + " animators=" + go.GetComponentsInChildren<Animator>(true).Length);
                }
                else if (a is Mesh mesh)
                {
                    sb.AppendLine("  MESH: " + mesh.name);
                }
            }
            sb.AppendLine();
        }

        // Map model counts (FBX only)
        sb.AppendLine("=== MAP FBX COUNTS ===");
        string[] mapDirs =
        {
            "Assets/Models/Maps/KenneyIndustrial/Models/FBX format",
            "Assets/Models/Maps/KenneyRoads/Models/FBX format",
            "Assets/Models/Maps/DowntownCity/Exports/FBX (Unity)",
        };
        foreach (string dir in mapDirs)
        {
            string[] fbx = Directory.GetFiles(dir, "*.fbx", SearchOption.AllDirectories);
            sb.AppendLine("  " + dir + " : " + fbx.Length + " FBX");
            for (int i = 0; i < System.Math.Min(15, fbx.Length); i++)
            {
                sb.AppendLine("    - " + Path.GetFileName(fbx[i]));
            }
        }

        string outPath = "Assets/model-inspection.txt";
        File.WriteAllText(outPath, sb.ToString());
        Debug.Log("MODEL INSPECTION DONE\n" + sb.ToString());
    }
}
