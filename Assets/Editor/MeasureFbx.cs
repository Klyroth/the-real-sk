using UnityEditor;
using UnityEngine;

// Measures the converted FBX models (bounds + clips) after Blender conversion.
public static class MeasureFbx
{
    public static void Measure()
    {
        foreach (string p in new[] { "Assets/Models/Weapons/ScarH/scar-h.fbx", "Assets/Models/Player/FPSArms/fps-arms.fbx" })
        {
            GameObject m = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (m == null) { Debug.Log(p + " NOT IMPORTED YET"); continue; }
            Renderer[] rs = m.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in rs)
            {
                Debug.Log("FBX " + p + " | " + r.gameObject.name + " | " + r.GetType().Name + " | bounds=" + r.bounds.size + " center=" + r.bounds.center);
            }
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(p);
            foreach (Object a in assets)
            {
                if (a is AnimationClip clip)
                    Debug.Log("FBX " + p + " | CLIP " + clip.name + " (" + clip.length.ToString("F2") + "s)");
                if (a is Avatar av)
                    Debug.Log("FBX " + p + " | AVATAR " + av.name);
            }
        }
        Debug.Log("MEASURE FBX DONE");
    }
}
