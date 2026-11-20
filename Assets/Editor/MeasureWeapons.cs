using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Measures the WORLD bounds of the weapon instances in the Map2 scene.
public static class MeasureWeapons
{
    public static void Measure()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Map2.unity");
        foreach (string name in new[] { "Weapon_ScarH", "Weapon_Sniper", "FPSArms", "SniperRifle", "Player" })
        {
            GameObject go = GameObject.Find(name);
            if (go == null) { Debug.Log(name + " NOT FOUND"); continue; }
            Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
            Bounds b = new Bounds();
            bool first = true;
            foreach (Renderer r in rs)
            {
                if (first) { b = r.bounds; first = false; }
                else b.Encapsulate(r.bounds);
            }
            Debug.Log(name + " renderers=" + rs.Length + " worldBounds size=" + b.size + " center=" + b.center + " active=" + go.activeInHierarchy);
        }
        Debug.Log("MEASURE DONE");
    }
}
