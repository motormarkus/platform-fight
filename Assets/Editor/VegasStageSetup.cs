using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Vegasin baarin lava (valikko 385): bändi lavalle (kopio Aavikkobaarin bändistä) ja Mary ja Liz tankoineen lavan toiseen päähän
/// (kopiot Korvatunturin afterski-baarin yläkerran / Thaitownin tanssijoista). Aja valikko 379 ensin.</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/385. Las Vegas: baarin lava (bändi, Mary ja Liz tangoissa)", false, 385)]
    static void AddVegasStage()
    {
        if (PlayModeBlocked()) return;
        if (!File.Exists(VegasScenePath)) { Info("Aja ensin valikko 376 (Vegas)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        var opened = new List<UnityEngine.SceneManagement.Scene>();
        UnityEngine.SceneManagement.Scene Src(string path)
        {
            if (!File.Exists(path)) return default;
            var s = EditorSceneManager.GetSceneByPath(path);
            if (!s.IsValid() || !s.isLoaded) { s = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive); opened.Add(s); }
            return s;
        }
        try
        {
            var bar = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.gameObject.scene == scene && a.areaName == "Vegasin baari");
            if (bar == null) { Info("Alue \"Vegasin baari\" puuttuu: aja valikko 379."); return; }
            var srcScenes = new[] { Src(AlpineScenePathNow), Src(ThaitownScenePath), Src("Assets/Scenes/Areas/Aavikko.unity"), Src("Assets/Scenes/SampleScene.unity") }.Where(s => s.IsValid()).ToArray();
            T FindIn<T>(string name) where T : Component => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c != null && srcScenes.Contains(c.gameObject.scene) && c.name == name);
            var band = FindIn<Dancer>("Aavikkobaarin bändi") ?? FindIn<Dancer>("Bändi");
            var mary = FindIn<Dancer>("Yläkerran Mary") ?? FindIn<Dancer>("Hotellin Mary ja tanko");
            var liz = FindIn<SpriteLoop>("Yläkerran Liz") ?? FindIn<SpriteLoop>("Thaitownin klubin Liz");
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Vegasin lava").ToArray()) Undo.DestroyObjectImmediate(old);
            var root = new GameObject("Vegasin lava");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Vegasin lava");

            float H = 2f * CamHalf, top = CamY + CamHalf, imgW = 5120f / (1844f / H);
            float X0 = VegasBarX0, feetY = top - 0.455f * H;   // lavan pinta
            var report = new List<string>();
            if (band != null)
            {
                var bg = Object.Instantiate(band.gameObject, root.transform); bg.name = "Vegasin bändi";
                bg.transform.SetParent(root.transform, false);
                var sr = bg.GetComponent<SpriteRenderer>();
                float h = sr != null && sr.sprite != null ? sr.sprite.bounds.size.y : 3f, w = sr != null && sr.sprite != null ? sr.sprite.bounds.size.x : 6f;
                float k = Mathf.Min(0.34f * H / h, 0.34f * imgW / w);
                bg.transform.localScale = Vector3.one * k;
                bg.transform.position = new Vector3(X0 + imgW * 1.72f, feetY, 0f);
                if (sr != null) sr.sortingOrder = -9000;
                report.Add("bändi lavan keskellä");
            }
            else report.Add("bändiä ei löytynyt (aja valikko 113 tai 40)");
            float poleH = 3.4f, baseY = feetY;
            if (mary != null)
            {
                var mg = Object.Instantiate(mary.gameObject, root.transform); mg.name = "Vegasin Mary";
                mg.transform.SetParent(root.transform, false);
                float sc = poleH / 2.78f * AlpUpMaryShrink;
                mg.transform.localScale = Vector3.one * sc;
                mg.transform.position = new Vector3(X0 + imgW * 2.08f + 0.46f * sc, baseY - 0.11f * sc, 0f);
                var sr = mg.GetComponent<SpriteRenderer>(); if (sr != null) sr.sortingOrder = -9000;
                report.Add("Mary tangossa");
            }
            else report.Add("Mary puuttuu (aja 282/289)");
            if (liz != null)
            {
                var lg = Object.Instantiate(liz.gameObject, root.transform); lg.name = "Vegasin Liz";
                lg.transform.SetParent(root.transform, false);
                float impPpu = ClubPpu / (ClubSpriteScale * 1.1f), effPpu = 345f / poleH, sc = impPpu / effPpu;
                lg.transform.localScale = Vector3.one * sc;
                lg.transform.position = new Vector3(X0 + imgW * 2.22f + 3.5f / impPpu * sc, baseY - 7f / impPpu * sc, 0f);
                var sr = lg.GetComponent<SpriteRenderer>(); if (sr != null) sr.sortingOrder = -9000;
                report.Add("Liz tangossa");
            }
            else report.Add("Liz puuttuu (aja 282/289)");
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Vegasin lava: " + string.Join(", ", report) + ". Jos koko tai paikka ei täsmää, säädä Vegasin lava -objektin lapsia.");
        }
        finally
        {
            foreach (var sc in opened) if (sc.IsValid()) EditorSceneManager.CloseScene(sc, true);
            if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
