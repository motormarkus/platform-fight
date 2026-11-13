using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Violetti Vegasin baariin (valikko 386): NpcWalker-kopio Violettista (Muurin juhlahalli): seisoo, kävelee, puhuu, tanssii ja nojaa tiskiin baarin lattialla. Ei taistelija eikä lyötävissä. Aja valikko 379 ensin.</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/386. Las Vegas: Violetti baariin (kävelee, tanssii, nojaa tiskiin)", false, 386)]
    static void AddVegasViolet()
    {
        if (PlayModeBlocked()) return;
        if (!File.Exists(VegasScenePath)) { Info("Aja ensin valikko 376 (Vegas)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        UnityEngine.SceneManagement.Scene wallScene = default; bool wallSceneOpened = false;
        try
        {
            var bar = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.gameObject.scene == scene && a.areaName == "Vegasin baari");
            if (bar == null) { Info("Alue \"Vegasin baari\" puuttuu: aja valikko 379."); return; }
            string dp = WallScenePath;
            if (!File.Exists(dp)) { Info("Muuri-scene puuttuu: aja valikko 327."); return; }
            wallScene = EditorSceneManager.GetSceneByPath(dp);
            if (!wallScene.IsValid() || !wallScene.isLoaded) { wallScene = EditorSceneManager.OpenScene(dp, OpenSceneMode.Additive); wallSceneOpened = true; }
            var tmpl = Object.FindObjectsByType<NpcStroller>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(w => w != null && w.gameObject.scene == wallScene && w.name == "Violetti");
            if (tmpl == null) { Info("Violettia ei löytynyt Muurin scenestä: aja valikko 336."); return; }
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Vegasin Violetti").ToArray()) Undo.DestroyObjectImmediate(old);
            var go = Object.Instantiate(tmpl.gameObject);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            go.name = "Vegasin Violetti"; go.SetActive(true);
            var w2 = go.GetComponent<NpcStroller>();
            float H = 2f * CamHalf, imgW = 5120f / (1844f / H);
            w2.area = bar; w2.laneMinX = VegasBarX0 + imgW * 0.15f; w2.laneMaxX = VegasBarX0 + imgW * 2.4f;
            w2.leanMinX = VegasBarX0 + imgW * 0.80f; w2.leanMaxX = VegasBarX0 + imgW * 1.40f;   // nojaa tiskiin
            float lx = (w2.leanMinX + w2.leanMaxX) * 0.5f; w2.leanY = Mathf.Lerp(bar.MaxDepthAtX(lx, bar.maxDepthY), bar.MinDepthAtX(lx, bar.minDepthY), 0.06f);
            EditorUtility.SetDirty(w2);
            go.transform.position = new Vector3(VegasBarX0 + imgW * 0.55f, Mathf.Lerp(bar.maxDepthY, bar.minDepthY, 0.6f), 0f);
            Undo.RegisterCreatedObjectUndo(go, "Vegasin Violetti");
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Violetti lisätty Vegasin baariin: seisoo, kävelee, puhuu, tanssii ja nojaa tiskiin; ei taistele.");
        }
        finally
        {
            if (wallSceneOpened && wallScene.IsValid()) EditorSceneManager.CloseScene(wallScene, true);
            if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
