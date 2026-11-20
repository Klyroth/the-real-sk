using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tags ONLY the camera named "MainCamera" in each scene; untags the rest
// (the ScopeCamera must NOT carry the MainCamera tag, or Camera.main
// returns null when it is the first match).
public static class FixCameraTags2
{
    public static void Fix()
    {
        foreach (string scenePath in new[] { "Assets/Scenes/Map1.unity", "Assets/Scenes/Map2.unity", "Assets/Scenes/MainMenu.unity" })
        {
            EditorSceneManager.OpenScene(scenePath);
            Camera[] cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera c in cams)
            {
                c.tag = c.gameObject.name == "MainCamera" ? "MainCamera" : "Untagged";
                Debug.Log(scenePath + " camera '" + c.gameObject.name + "' tag=" + c.tag);
            }
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
        Debug.Log("CAMERA TAGS2 FIXED");
    }
}
