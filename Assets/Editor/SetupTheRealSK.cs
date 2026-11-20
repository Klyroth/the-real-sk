using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Phase 0 setup for 'The real SK' (FPS zombie game, Unity 6000.3.28f1 + URP).
// NOTE: the URP asset created by UniversalRenderPipelineAsset.Create() has NO
// renderer data (m_RendererData is null) — after this runs, the renderer data
// is wired by a YAML edit (copy of the URP package default
// UniversalRendererData.asset), same fix as SK First Game.
public static class SetupTheRealSK
{
    public static void Setup()
    {
        // --- URP render pipeline asset ---
        string settingsDir = "Assets/Settings";
        Directory.CreateDirectory(settingsDir);
        string urpPath = settingsDir + "/TheRealSK_RPAsset.asset";
        UniversalRenderPipelineAsset urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(urpPath);
        if (urp == null)
        {
            urp = UniversalRenderPipelineAsset.Create();
            AssetDatabase.CreateAsset(urp, urpPath);
        }
        GraphicsSettings.defaultRenderPipeline = urp;
        Debug.Log("URP asset ready: " + urpPath);

        // --- Player settings ---
        PlayerSettings.productName = "The real SK";
        PlayerSettings.companyName = "Santosh";
        PlayerSettings.bundleVersion = "1.0";
        PlayerSettings.applicationIdentifier = "com.santosh.therealsk";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        Debug.Log("PlayerSettings set: com.santosh.therealsk, LandscapeLeft, minSdk 25");

        // --- Scenes: MainMenu (0), Map1 (1), Map2 (2) ---
        string[] scenes = { "MainMenu", "Map1", "Map2" };
        var buildScenes = new EditorBuildSettingsScene[scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
        {
            string path = "Assets/Scenes/" + scenes[i] + ".unity";
            if (!File.Exists(path))
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, path);
                Debug.Log("Created scene: " + path);
            }
            buildScenes[i] = new EditorBuildSettingsScene(path, true);
        }
        EditorBuildSettings.scenes = buildScenes;

        AssetDatabase.SaveAssets();
        Debug.Log("The real SK project setup done.");
    }
}
