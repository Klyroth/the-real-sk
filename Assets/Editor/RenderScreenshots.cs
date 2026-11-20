using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Renders screenshots from the player camera (and a top-down view)
// for visual verification of the built scenes.
public static class RenderScreenshots
{
    public static void Render()
    {
        Directory.CreateDirectory("Assets/Screenshots");
        RenderScene("Assets/Scenes/Map1.unity", "map1-player");
        RenderScene("Assets/Scenes/Map2.unity", "map2-player");
        RenderScene("Assets/Scenes/MainMenu.unity", "mainmenu");
        Debug.Log("RENDER DONE");
    }

    private static void SaveShot(Camera cam, string name)
    {
        RenderTexture rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        tex.Apply();
        cam.targetTexture = null;
        RenderTexture.active = null;
        Object.DestroyImmediate(rt);
        string path = "Assets/Screenshots/" + name + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        Debug.Log("Saved " + path);
    }

    private static void RenderScene(string scenePath, string name)
    {
        EditorSceneManager.OpenScene(scenePath);
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("No main camera in " + scenePath);
            return;
        }
        SaveShot(cam, name);
        // Top-down layout view (maps only)
        if (name.StartsWith("map"))
        {
            GameObject topGo = new GameObject("TopCam");
            Camera top = topGo.AddComponent<Camera>();
            top.transform.position = new Vector3(0, 70, 0);
            top.transform.rotation = Quaternion.Euler(90, 0, 0);
            top.orthographic = true;
            top.orthographicSize = 45;
            top.nearClipPlane = 0.1f;
            top.farClipPlane = 200f;
            SaveShot(top, name + "-top");
            Object.DestroyImmediate(topGo);
        }
    }
}
