using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Jerrygallonan lisäys scenen näkymän keskikohtaan (aseta x ja y käsin jälkeenpäin). Tallenna scene itse.
public static class JerrycanSetup
{
    [MenuItem("Beat em up/Yksittäiset valikot/314. Lisää jerrygallona (scene-näkymän keskelle)", false, 314)]
    static void AddJerrycan()
    {
        var sv = SceneView.lastActiveSceneView;
        Vector3 at = sv != null ? sv.pivot : Vector3.zero;
        at.z = 0f;
        var go = new GameObject("Jerrygallona");
        go.transform.position = at;
        go.AddComponent<Jerrycan>();
        Undo.RegisterCreatedObjectUndo(go, "Jerrygallona");
        Selection.activeGameObject = go;
        Debug.Log("Beat em up: jerrygallona lisätty kohtaan " + at + ". Siirrä sen y lattiatasolle ja tallenna scene (Ctrl+S).");
    }

    const string JungleScenePath = "Assets/Scenes/Areas/Viidakko.unity";

    [MenuItem("Beat em up/Yksittäiset valikot/315. Viidakko: jerrygallonat kiitotielle, halliin ja kadulle (poistaa kiitotien amatsonit)", false, 315)]
    internal static void PlaceJungleJerrycans()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Beat em up: lopeta pelitila ensin."); return; }
        if (!System.IO.File.Exists(JungleScenePath)) { Debug.LogWarning("Beat em up: aja ensin valikko 305 (Viidakko)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(JungleScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(JungleScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            Area Find(string n) => Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == n && a.gameObject.scene == scene);
            var runway = Find("Viidakon kiitotie"); var hall = Find("Viidakon hangaari"); var street = Find("Viidakon katu");
            if (runway == null || hall == null || street == null) { Debug.LogWarning("Beat em up: Viidakon alueita ei löytynyt (kiitotie/hangaari/katu): aja valikko 305."); return; }

            // kiitotien amatsonit pois (piilotettu malli, jolla ei ole vanhempaa ja joka on pois päältä, jää)
            int removed = 0;
            foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToArray())
            {
                if (e == null || e.displayName != "Amatsoni" || e.gameObject.scene != scene) continue;
                float x = e.transform.position.x;
                if (x < runway.walkMinX - 2f || x > runway.walkMaxX + 2f) continue;
                if (!e.gameObject.activeSelf) continue;   // piilossa olevat (malli ja hallin varaamat amatsonit) jäävät
                Undo.DestroyObjectImmediate(e.gameObject); removed++;
            }

            // yksi amatsoni kiitotielle (lahjottavaksi otsapannalla): kopio piilotetusta mallista (valikko 306)
            int ama = 0;
            var tmplA = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(e => e != null && e.displayName == "Amatsoni" && e.gameObject.scene == scene && e.transform.parent == null && !e.gameObject.activeSelf);
            if (tmplA != null)
            {
                var ca = Object.Instantiate(tmplA.gameObject);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(ca, scene);
                ca.name = "Kiitotien amatsoni";
                ca.transform.position = new Vector3(Mathf.Lerp(runway.walkMinX, runway.walkMaxX, 0.5f), Mathf.Lerp(runway.maxDepthY, runway.minDepthY, 0.5f), 0f);
                ca.SetActive(true); Undo.RegisterCreatedObjectUndo(ca, "Kiitotien amatsoni"); ama = 1;
            }
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Viidakon jerrygallonat").ToArray()) Undo.DestroyObjectImmediate(old);
            var root = new GameObject("Viidakon jerrygallonat");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Viidakon jerrygallonat");
            int n = 0;
            void Put(Area a, string label, float x01, float depth01)
            {
                var go = new GameObject(label);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(Mathf.Lerp(a.walkMinX, a.walkMaxX, x01), Mathf.Lerp(a.maxDepthY, a.minDepthY, depth01), 0f);
                go.AddComponent<Jerrycan>();
                n++;
            }
            // kiitotie: pari kaukana koneesta ja hallin ovesta
            Put(runway, "Kiitotien gallona 1", 0.22f, 0.45f); Put(runway, "Kiitotien gallona 2", 0.62f, 0.7f);
            // halli: viisi eri puolille (vihollisten välissä)
            float[] hx = { 0.10f, 0.30f, 0.50f, 0.68f, 0.86f }, hd = { 0.6f, 0.35f, 0.75f, 0.5f, 0.3f };
            for (int i = 0; i < hx.Length; i++) Put(hall, "Hallin gallona " + (i + 1), hx[i], hd[i]);
            // katu: kuusi pitkin katua
            float[] sx = { 0.10f, 0.27f, 0.44f, 0.60f, 0.76f, 0.92f }, sd = { 0.45f, 0.7f, 0.35f, 0.6f, 0.4f, 0.75f };
            for (int i = 0; i < sx.Length; i++) Put(street, "Kadun gallona " + (i + 1), sx[i], sd[i]);

            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("Beat em up: Viidakkoon lisätty " + n + " jerrygallonaa (kiitotie 2, halli 5, katu 6); kiitotien amatsoneja poistettu " + removed + ", kiitotielle lisätty " + ama + " (lahjottava otsapannalla). Scene tallennettu. Ajamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
