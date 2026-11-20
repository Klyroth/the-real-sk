using UnityEditor;
using UnityEngine;

// Extracts the zombie FBX "Atlas" materials into real .mat assets (so the
// texture assignment survives FBX reimport), textures them, and points the
// zombie prefabs at the extracted materials.
public static class FixZombieMaterials
{
    public static void Fix()
    {
        Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Zombies/QuaterniusZombieKit/Zombie_Atlas.png");
        if (atlas == null) { Debug.Log("ATLAS NOT FOUND"); return; }
        string matDir = "Assets/Models/Zombies/QuaterniusZombieKit/Characters/FBX/Materials";
        if (!AssetDatabase.IsValidFolder(matDir))
            AssetDatabase.CreateFolder("Assets/Models/Zombies/QuaterniusZombieKit/Characters/FBX", "Materials");
        string[] zombies = { "Zombie_Basic", "Zombie_Chubby", "Zombie_Ribcage", "Zombie_Arm" };
        foreach (string z in zombies)
        {
            string fbx = "Assets/Models/Zombies/QuaterniusZombieKit/Characters/FBX/" + z + ".fbx";
            string matPath = matDir + "/Atlas_" + z + ".mat";
            Material extracted = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (extracted == null)
            {
                Object[] subs = AssetDatabase.LoadAllAssetsAtPath(fbx);
                foreach (Object o in subs)
                {
                    if (o is Material m && m.name == "Atlas")
                    {
                        string err = AssetDatabase.ExtractAsset(m, matPath);
                        if (!string.IsNullOrEmpty(err)) { Debug.Log("EXTRACT FAILED " + z + ": " + err); continue; }
                        extracted = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                        Debug.Log("Extracted " + matPath);
                        break;
                    }
                }
            }
            if (extracted == null) { Debug.Log("NO EXTRACTED MAT FOR " + z); continue; }
            extracted.mainTexture = atlas;
            EditorUtility.SetDirty(extracted);
            // Point the prefab's renderers at the extracted material
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + z + ".prefab");
            if (prefab == null) { Debug.Log("NO PREFAB " + z); continue; }
            bool changed = false;
            foreach (Renderer r in prefab.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] != null && mats[i].name == "Atlas")
                    {
                        mats[i] = extracted;
                        changed = true;
                    }
                }
                if (changed) r.sharedMaterials = mats;
            }
            if (changed)
            {
                PrefabUtility.SavePrefabAsset(prefab);
                Debug.Log("Prefab " + z + " now uses extracted Atlas material");
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("FIX ZOMBIE MATERIALS DONE");
    }
}
