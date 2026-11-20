using UnityEditor;
using UnityEngine;

// Inspects the zombie FBX models: materials/textures + upright bounds.
public static class InspectZombies
{
    public static void Inspect()
    {
        string[] paths = {
            "Assets/Models/Zombies/QuaterniusZombieKit/Characters/FBX/Zombie_Basic.fbx",
            "Assets/Models/Zombies/QuaterniusZombieKit/Characters/FBX/Zombie_Chubby.fbx",
            "Assets/Models/Zombies/QuaterniusZombieKit/Characters/FBX/Zombie_Ribcage.fbx",
            "Assets/Models/Zombies/QuaterniusZombieKit/Characters/FBX/Zombie_Arm.fbx",
            "Assets/Models/Zombies/SkeletonZombie/skeleton-zombie.fbx"
        };
        foreach (string p in paths)
        {
            GameObject m = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (m == null) { Debug.Log(p + " NOT FOUND"); continue; }
            Renderer[] rs = m.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in rs)
            {
                string mats = "";
                foreach (Material mat in r.sharedMaterials)
                {
                    if (mat == null) { mats += "[null] "; continue; }
                    Texture t = mat.mainTexture;
                    mats += mat.name + "(tex=" + (t != null ? t.name : "NONE") + ",color=" + mat.color + ") ";
                }
                Debug.Log("Z " + p + " | " + r.gameObject.name + " | bounds=" + r.bounds.size + " center=" + r.bounds.center + " | " + mats);
            }
            Debug.Log("Z " + p + " | root rotation=" + m.transform.rotation.eulerAngles + " root scale=" + m.transform.localScale);
        }
        Debug.Log("INSPECT ZOMBIES DONE");
    }
}
