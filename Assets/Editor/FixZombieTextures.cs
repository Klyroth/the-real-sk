using UnityEditor;
using UnityEngine;

// Assigns the Quaternius atlas texture to the zombie FBX materials
// (Unity's importer only auto-links textures next to the FBX; the atlas
// lives at the kit root, so the materials were left texture-less/white).
public static class FixZombieTextures
{
    public static void Fix()
    {
        Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Zombies/QuaterniusZombieKit/Zombie_Atlas.png");
        if (atlas == null) { Debug.Log("ATLAS NOT FOUND"); return; }
        string[] fbx = {
            "Assets/Models/Zombies/QuaterniusZombieKit/Characters/FBX/Zombie_Basic.fbx",
            "Assets/Models/Zombies/QuaterniusZombieKit/Characters/FBX/Zombie_Chubby.fbx",
            "Assets/Models/Zombies/QuaterniusZombieKit/Characters/FBX/Zombie_Ribcage.fbx",
            "Assets/Models/Zombies/QuaterniusZombieKit/Characters/FBX/Zombie_Arm.fbx"
        };
        foreach (string p in fbx)
        {
            Object[] subs = AssetDatabase.LoadAllAssetsAtPath(p);
            foreach (Object o in subs)
            {
                if (o is Material m && m.mainTexture == null)
                {
                    m.mainTexture = atlas;
                    EditorUtility.SetDirty(m);
                    Debug.Log("Textured " + p + " material " + m.name);
                }
            }
        }
        // Skeleton zombie: verify its materials (textures sit in fbx/textures/)
        GameObject skel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Zombies/SkeletonZombie/fbx/skeleton-zombie.fbx");
        if (skel == null) { Debug.Log("SKELETON FBX NOT FOUND"); }
        else
        {
            foreach (Renderer r in skel.GetComponentsInChildren<Renderer>(true))
                foreach (Material m in r.sharedMaterials)
                    Debug.Log("Skeleton " + r.gameObject.name + " mat=" + (m != null ? m.name : "null") + " tex=" + (m != null && m.mainTexture != null ? m.mainTexture.name : "NONE"));
        }
        AssetDatabase.SaveAssets();
        Debug.Log("FIX ZOMBIE TEXTURES DONE");
    }
}
