using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Prints per-renderer world bounds for the two weapon models in Map2.
public static class MeasureRenderers
{
    public static void Measure()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Map2.unity");
        GameObject player = GameObject.Find("Player");
        if (player == null) { Debug.Log("NO PLAYER"); return; }
        Renderer[] rs = player.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in rs)
        {
            Debug.Log("RENDERER " + r.gameObject.name + " | " + r.GetType().Name + " | bounds size=" + r.bounds.size + " center=" + r.bounds.center + " | localScale=" + r.transform.localScale + " | parent=" + (r.transform.parent != null ? r.transform.parent.name : "none"));
        }
        Debug.Log("MEASURE RENDERERS DONE");
    }
}
