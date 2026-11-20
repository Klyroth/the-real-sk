using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Dumps the Player hierarchy (active state, position, scale, bounds)
// and the model bounds for the two Sketchfab weapons.
public static class InspectPlayerScene
{
    public static void Inspect()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Map2.unity");
        GameObject player = GameObject.Find("Player");
        if (player == null) { Debug.Log("NO PLAYER FOUND"); return; }
        Dump(player.transform, 0);
        Debug.Log("--- MODEL BOUNDS ---");
        foreach (string p in new[] { "Assets/Models/Weapons/ScarH/scene.gltf", "Assets/Models/Player/FPSArms/scene.gltf", "Assets/Models/Weapons/SniperRifle/scene.gltf" })
        {
            GameObject m = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (m == null) { Debug.Log(p + " NOT FOUND"); continue; }
            Renderer[] rs = m.GetComponentsInChildren<Renderer>(true);
            Bounds b = new Bounds();
            bool first = true;
            foreach (Renderer r in rs)
            {
                if (first) { b = r.bounds; first = false; }
                else b.Encapsulate(r.bounds);
            }
            Debug.Log(p + " rendererCount=" + rs.Length + " bounds size=" + b.size + " center=" + b.center);
            Debug.Log(p + " root scale=" + m.transform.localScale + " childCount=" + m.transform.childCount);
        }
        Debug.Log("INSPECT DONE");
    }

    private static void Dump(Transform t, int depth)
    {
        string pad = new string(' ', depth * 2);
        Renderer r = t.GetComponent<Renderer>();
        string extra = "";
        if (r != null) extra = " bounds=" + r.bounds.size;
        Debug.Log(pad + t.name + " active=" + t.gameObject.activeInHierarchy + " pos=" + t.position + " scale=" + t.localScale + extra);
        foreach (Transform c in t) Dump(c, depth + 1);
    }
}
