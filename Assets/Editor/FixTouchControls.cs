using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// FixTouchControls — The real SK
// Fixes the three problems from play-test #2 (Build #3 APK, 2026-11-20):
//  1. NO touch controls in-game: adds a TouchControls GameObject to Map1
//     and Map2 (the script builds joystick + FIRE + SCOPE + SWAP buttons
//     and the touch-look area at runtime, on touch devices only).
//  2. Map looked EMPTY from spawn: the player spawned at (0,0,25) at the
//     south edge facing a 59 m-distant building, all 12 buildings sat on
//     the +/-32..34 perimeter, and zombies spawned 50+ m away or behind
//     the player. Fix: spawn moved to (0,0,8), the building ring pulled
//     inward (x0.6, kept off the road cross), and 3 closer zombie spawn
//     points added (~20 m from spawn). NavMesh is rebaked afterwards.
//  3. Scope STUCK showing sky: the scope could only be toggled with a
//     right mouse click (on Android a second-finger touch fires that),
//     there was no touch-look to aim away, and the +/-30 deg pitch clamp
//     meant max pitch filled the whole frame with sky or ground. The
//     code fixes are in ScopeSystem/CameraController; here the serialized
//     pitch clamps are widened to +/-85 and the full-screen scope image
//     is made non-raycastable so it cannot block the touch buttons.
// Idempotent: safe to run twice (existing objects are detected and the
// building move is skipped once the ring is already pulled in).
public static class FixTouchControls
{
    [MenuItem("TheRealSK/Fix Touch Controls + Spawn/Map")]
    public static void Fix()
    {
        FixMap("Assets/Scenes/Map1.unity", true);
        FixMap("Assets/Scenes/Map2.unity", false);
        CheckEventSystem("Assets/Scenes/MainMenu.unity");
        Debug.Log("FixTouchControls: done");
    }

    private static void FixMap(string path, bool isMap1)
    {
        Scene scene = EditorSceneManager.OpenScene(path);
        int moved = 0, spawnsAdded = 0;

        // 1. TouchControls GameObject (builds the touch UI at runtime)
        if (FindGameObject(scene, "TouchControls") == null)
        {
            GameObject tc = new GameObject("TouchControls");
            tc.AddComponent<TouchControls>();
            Debug.Log("FixTouchControls: TouchControls added in " + path);
        }
        else
        {
            Debug.Log("FixTouchControls: TouchControls already present in " + path);
        }

        // 2. Widen pitch clamps on both camera controllers
        GameObject player = FindGameObject(scene, "Player");
        if (player == null)
        {
            Debug.LogError("FixTouchControls: Player not found in " + path);
            return;
        }
        CameraController cc = player.GetComponentInChildren<CameraController>();
        if (cc != null) { cc.minXAngle = -85f; cc.maxXAngle = 85f; EditorUtility.SetDirty(cc); }
        ScopeCameraController scc = player.GetComponentInChildren<ScopeCameraController>();
        if (scc != null) { scc.minXAngle = -85f; scc.maxXAngle = 85f; EditorUtility.SetDirty(scc); }

        // 3. Player spawn: centre of the map, facing down the road (-Z)
        player.transform.position = new Vector3(0f, 0f, 8f);
        player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        // 4. Pull the building ring inward (only if still at the old radius)
        GameObject mapRoot = FindGameObject(scene, "Map");
        if (mapRoot != null)
        {
            string prefix = isMap1 ? "building-" : "Building_";
            float maxDist = 0f;
            foreach (Transform child in mapRoot.transform)
            {
                if (!child.name.StartsWith(prefix)) continue;
                maxDist = Mathf.Max(maxDist, Mathf.Max(Mathf.Abs(child.localPosition.x), Mathf.Abs(child.localPosition.z)));
            }
            if (maxDist > 25f)
            {
                foreach (Transform child in mapRoot.transform)
                {
                    if (!child.name.StartsWith(prefix)) continue;
                    Vector3 p = PullInward(child.localPosition);
                    child.localPosition = p;
                    child.localRotation = Quaternion.Euler(0f, Mathf.Atan2(-p.x, -p.z) * Mathf.Rad2Deg, 0f);
                    moved++;
                }
                Debug.Log("FixTouchControls: moved " + moved + " buildings inward in " + path);
            }
            else
            {
                Debug.Log("FixTouchControls: buildings already pulled in (maxDist=" + maxDist + ") in " + path);
            }
        }

        // 5. Closer zombie spawn points (~20 m from the new player spawn)
        GameObject spawnRoot = FindGameObject(scene, "SpawnPoints");
        ZombieWaveSystem ws = FindInScene<ZombieWaveSystem>(scene);
        if (spawnRoot != null && ws != null)
        {
            Vector3[] extra = { new Vector3(14, 0, -14), new Vector3(-14, 0, -14), new Vector3(0, 0, -18) };
            System.Collections.Generic.List<Transform> all = new System.Collections.Generic.List<Transform>(ws.spawnPoints ?? new Transform[0]);
            for (int i = 0; i < extra.Length; i++)
            {
                string name = "SpawnPoint" + (6 + i);
                if (FindGameObject(scene, name) != null) continue;
                GameObject sp = new GameObject(name);
                sp.transform.SetParent(spawnRoot.transform, false);
                sp.transform.position = extra[i];
                all.Add(sp.transform);
                spawnsAdded++;
            }
            if (spawnsAdded > 0)
            {
                ws.spawnPoints = all.ToArray();
                EditorUtility.SetDirty(ws);
                Debug.Log("FixTouchControls: added " + spawnsAdded + " closer spawn points in " + path);
            }
        }

        // 6. Scope overlay must not eat touch input (it sits above the
        // touch canvas at sortingOrder 10 and covers the whole screen)
        GameObject scopeCanvas = FindGameObject(scene, "ScopeCanvas");
        if (scopeCanvas != null)
        {
            Transform imgT = scopeCanvas.transform.Find("ScopeImage");
            if (imgT != null)
            {
                Image img = imgT.GetComponent<Image>();
                if (img != null) { img.raycastTarget = false; EditorUtility.SetDirty(img); }
            }
        }

        // 7. Rebake the NavMesh for the moved buildings (player hidden
        // during the bake so its weapon meshes are not baked in)
        GameObject env = FindGameObject(scene, "Environment");
        if (env != null)
        {
            NavMeshSurface surface = env.GetComponent<NavMeshSurface>();
            if (surface != null)
            {
                bool wasActive = player.activeSelf;
                player.SetActive(false);
                surface.BuildNavMesh();
                player.SetActive(wasActive);
                Debug.Log("FixTouchControls: NavMesh rebaked in " + path);
            }
        }

        // 8. EventSystem must still be present (menu fix, 2026-11-20)
        bool hasEventSystem = Object.FindFirstObjectByType<EventSystem>() != null;
        Debug.Log("FixTouchControls: " + path + " EventSystem=" + hasEventSystem
            + " buildingsMoved=" + moved + " spawnsAdded=" + spawnsAdded);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("FixTouchControls: saved " + path);
    }

    private static void CheckEventSystem(string path)
    {
        Scene scene = EditorSceneManager.OpenScene(path);
        bool hasEventSystem = Object.FindFirstObjectByType<EventSystem>() != null;
        Debug.Log("FixTouchControls: " + path + " EventSystem=" + hasEventSystem + " (not modified)");
    }

    // Scale the building ring inward; keep buildings off the road cross
    // (roads run along x=0 and z=0 out to +/-28).
    private static Vector3 PullInward(Vector3 p)
    {
        p.x *= 0.6f;
        p.z *= 0.6f;
        if (Mathf.Abs(p.x) < 4f && Mathf.Abs(p.z) > 10f) p.x = p.x < 0f ? -8f : 8f;
        if (Mathf.Abs(p.z) < 4f && Mathf.Abs(p.x) > 10f) p.z = p.z < 0f ? -8f : 8f;
        return p;
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
