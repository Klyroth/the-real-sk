using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Sets the MainCamera tag on the player/menu cameras (needed for Camera.main
// and for the render verification). Also fixes the scene-build script's
// omission — BuildTheRealSK_Scenes should set this tag when rebuilding.
public static class FixCameraTags
{
    public static void Fix()
    {
        foreach (string scenePath in new[] { "Assets/Scenes/Map1.unity", "Assets/Scenes/Map2.unity", "Assets/Scenes/MainMenu.unity" })
        {
            EditorSceneManager.OpenScene(scenePath);
            Camera cam = Object.FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                cam.tag = "MainCamera";
                Debug.Log("Tagged camera in " + scenePath);
            }
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
        Debug.Log("CAMERA TAGS FIXED");
    }
}
