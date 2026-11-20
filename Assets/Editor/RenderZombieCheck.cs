using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Places one zombie of each prefab in front of the player in Map1 and
// renders a screenshot, so the zombie models can be visually verified.
public static class RenderZombieCheck
{
    public static void Render()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Map1.unity", OpenSceneMode.Single);
        Camera cam = Camera.main;
        string[] prefs = { "Zombie_Basic", "Zombie_Chubby", "Zombie_Ribcage", "Zombie_Arm", "SkeletonZombie" };
        float x = -10f;
        foreach (string p in prefs)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + p + ".prefab");
            if (prefab == null) { Debug.Log("MISSING PREFAB " + p); continue; }
            GameObject z = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            z.transform.position = new Vector3(x, 0, 16);
            z.transform.rotation = Quaternion.Euler(0, 0, 0); // face +Z, toward player at z=25
            // Force the Animator to evaluate the Idle clip (batch mode has no
            // player loop, so without this the mesh renders in its bind pose).
            Animator anim = z.GetComponent<Animator>();
            if (anim != null) { anim.Play("Idle", 0, 0f); anim.Update(0.1f); }
            foreach (Renderer r in z.GetComponentsInChildren<Renderer>(true))
                Debug.Log("ZCHECK " + p + " | " + r.gameObject.name + " bounds=" + r.bounds.size + " center=" + r.bounds.center);
            x += 5f;
        }
        // render from the player camera
        RenderTexture rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        tex.Apply();
        System.IO.File.WriteAllBytes("Assets/Screenshots/map1-zombies.png", tex.EncodeToPNG());
        cam.targetTexture = null;
        RenderTexture.active = null;
        Debug.Log("Saved Assets/Screenshots/map1-zombies.png");
        Debug.Log("ZOMBIE CHECK DONE");
    }
}
