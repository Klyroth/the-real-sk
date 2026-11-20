using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// FixMenuButtons — The real SK
// Fixes the dead main-menu buttons (found 2026-11-20 from the phone play-test):
//  1. No EventSystem existed in ANY scene, so no UI button could receive
//     touch/click input at all.
//  2. SETTINGS / BACK / QUIT were wired with runtime AddListener() calls
//     inside the editor scene-builder (BuildTheRealSK_Scenes.cs). Runtime
//     listeners are NOT serialized into the scene file, so those three
//     buttons had empty onClick lists at runtime.
// This script adds an EventSystem to all three scenes and assigns the
// SettingsManager fields (settingsButton/backButton/quitButton/settingsPanel)
// that SettingsManager.Start() wires at runtime.
public static class FixMenuButtons
{
    [MenuItem("TheRealSK/Fix Menu Buttons + EventSystem")]
    public static void Fix()
    {
        FixMainMenu();
        FixScene("Assets/Scenes/Map1.unity");
        FixScene("Assets/Scenes/Map2.unity");
        Debug.Log("FixMenuButtons: done");
    }

    private static void FixMainMenu()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        EnsureEventSystem();
        SettingsManager sm = FindInScene<SettingsManager>(scene);
        if (sm == null)
        {
            Debug.LogError("FixMenuButtons: SettingsManager not found in MainMenu");
        }
        else
        {
            sm.settingsButton = FindButton(scene, "SettingsButton");
            sm.backButton = FindButton(scene, "BackButton");
            sm.quitButton = FindButton(scene, "QuitButton");
            sm.settingsPanel = FindGameObject(scene, "SettingsPanel");
            EditorUtility.SetDirty(sm);
            Debug.Log("FixMenuButtons: MainMenu wired settings=" + (sm.settingsButton != null)
                + " back=" + (sm.backButton != null)
                + " quit=" + (sm.quitButton != null)
                + " panel=" + (sm.settingsPanel != null));
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void FixScene(string path)
    {
        Scene scene = EditorSceneManager.OpenScene(path);
        EnsureEventSystem();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("FixMenuButtons: EventSystem ensured in " + path);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Debug.Log("FixMenuButtons: EventSystem created");
    }

    private static T FindInScene<T>(Scene scene) where T : Object
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = FindInChildren<T>(root.transform);
            if (found != null) return found;
        }
        return null;
    }

    private static T FindInChildren<T>(Transform t) where T : Object
    {
        T c = t.GetComponent<T>();
        if (c != null) return c;
        foreach (Transform child in t)
        {
            T found = FindInChildren<T>(child);
            if (found != null) return found;
        }
        return null;
    }

    private static Button FindButton(Scene scene, string name)
    {
        GameObject go = FindGameObject(scene, name);
        return go != null ? go.GetComponent<Button>() : null;
    }

    private static GameObject FindGameObject(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            GameObject found = FindGameObjectRecursive(root, name);
            if (found != null) return found;
        }
        return null;
    }

    private static GameObject FindGameObjectRecursive(GameObject go, string name)
    {
        if (go.name == name) return go;
        foreach (Transform child in go.transform)
        {
            GameObject found = FindGameObjectRecursive(child.gameObject, name);
            if (found != null) return found;
        }
        return null;
    }
}
