using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Valikko 396: Vegasin työmaakonttorin kanttiinin väki: 3 kovista, 4 mainaria, rekkamies, 2 puliukkoa, 2 prätkäjätkää, samoalainen, 2 Tankki-Tonya ja Don (16 hahmoa).
/// Rauhallisia kuten kasinon väki (SalsaBrawl): rähinä alkaa, kun hero lyö jotakuta, tai kun aikaa on kulunut ~1 min ja Puliukko vittuilee. Sen jälkeen kaikki ovat kaikkia vastaan (heron kanssa).
/// Mallit kopioidaan: Mainari Viidakosta, loput Thaitownista, Aavikolta, SampleScenestä tai Uccon laivasta (kuten stadionin ravintolassa). Aja valikko 394 ensin.
/// </summary>
public static partial class BeatEmUpSetup
{
    static readonly (string type, float fx, float depth)[] VegasCanteenCrowd =
    {
        ("Kovis", 0.55f, 0.22f), ("Kovis", 1.04f, 0.78f), ("Kovis", 1.53f, 0.50f),
        ("Mainari", 0.30f, 0.50f), ("Mainari", 0.80f, 0.50f), ("Mainari", 1.29f, 0.22f), ("Mainari", 1.78f, 0.78f),
        ("Rekkamies", 1.04f, 0.45f),
        ("Puliukko", 0.55f, 0.78f), ("Puliukko", 1.53f, 0.22f),
        ("Prätkäjätkä", 0.80f, 0.85f), ("Prätkäjätkä", 1.29f, 0.55f),
        ("Samoalainen", 0.20f, 0.20f),
        ("Tankki-Tony", 0.42f, 0.35f), ("Tankki-Tony", 1.67f, 0.62f), ("Don", 1.17f, 0.65f),
    };

    [MenuItem("Beat em up/396. Las Vegas: kanttiinin väki (3 kovista, 4 mainaria, rekkamies, 2 puliukkoa, 2 prätkäjätkää, samoalainen)", false, 396)]
    static void AddVegasCanteenCrowd()
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
            var area = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a != null && a.gameObject.scene == scene && a.areaName == "Työmaakonttorin kanttiini");
            if (area == null) { Info("Aluetta \"Työmaakonttorin kanttiini\" ei löytynyt: aja valikko 394."); return; }
            foreach (var n in new[] { "Kanttiinin väki", "Kanttiinin tappelu" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);
            float H = 2f * CamHalf, ppu = 1844f / H, imgW = 5120f / ppu, X0 = VegasCanteenX0;

            var srcs = new[] { Src("Assets/Scenes/Areas/Viidakko.unity"), Src(ThaitownScenePath), Src("Assets/Scenes/SampleScene.unity"), Src("Assets/Scenes/Areas/Aavikko.unity"), Src(UccoLaivaScenePath) };
            Enemy Find(UnityEngine.SceneManagement.Scene s, string display, bool noCopies) => !s.IsValid() ? null : Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(en => en != null && en.gameObject.scene == s && en.displayName == display && en.guardOf == null && (!noCopies || (!en.gameObject.name.Contains("(terminaali") && !en.gameObject.name.Contains("(baari") && !en.gameObject.name.Contains("(hotelli") && !en.gameObject.name.Contains("(apterm") && !en.gameObject.name.Contains("(stadion"))));
            var vegasAll = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Enemy VegasTpl(string display, string rootPrefix) => vegasAll.FirstOrDefault(e => e != null && e.gameObject.scene == scene && e.displayName == display && e.transform.parent != null && e.transform.parent.name.StartsWith(rootPrefix) && e.guardOf == null);
            Enemy Template(string display) => display == "Tankki-Tony" ? VegasTpl(display, "Tankki-Tonyt") : display == "Don" ? VegasTpl(display, "Donit") : srcs.Select(s => Find(s, display, true)).FirstOrDefault(x => x != null) ?? srcs.Select(s => Find(s, display, false)).FirstOrDefault(x => x != null);
            var templates = new Dictionary<string, Enemy>();
            foreach (var t in VegasCanteenCrowd.Select(c => c.type).Distinct()) templates[t] = Template(t);

            var crowd = new GameObject("Kanttiinin väki");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(crowd, scene);
            Undo.RegisterCreatedObjectUndo(crowd, "Kanttiinin väki");
            var made = new List<Enemy>(); var counts = new Dictionary<string, int>(); var report = new List<string>();
            foreach (var (type, fx, depth) in VegasCanteenCrowd)
            {
                if (templates[type] == null) { if (!counts.ContainsKey(type + " (puuttuu)")) { counts[type + " (puuttuu)"] = 1; report.Add(type + ": mallia ei löytynyt"); } continue; }
                counts[type] = counts.TryGetValue(type, out int c0) ? c0 + 1 : 1;
                var go = Object.Instantiate(templates[type].gameObject, crowd.transform); go.name = type + " (kanttiini " + counts[type] + ")"; go.transform.SetParent(crowd.transform, false); go.SetActive(true);
                var e = go.GetComponent<Enemy>(); if (e == null) { Object.DestroyImmediate(go); continue; }
                float x = X0 + fx * imgW;
                e.ally = false; e.guardOf = null; e.guards = null;
                e.wakeDistance = -1f; e.wakeOnFightRadius = 0f; e.joinsFightWhenSquadComes = false; e.fightsEveryone = (type == "Puliukko");
                if (e.calmIdleSprites == null || e.calmIdleSprites.Length == 0) e.calmIdleSprites = e.idleSprites;
                if (e.calmWalkSprites == null || e.calmWalkSprites.Length == 0) { e.calmWalkSprites = e.walkSprites; e.calmWalkFrameTime = e.walkFrameTime; }
                if (type == "Puliukko") e.wakeOnFightRadius = 9f;
                e.wanderMinX = x - 2f; e.wanderMaxX = x + 2f;
                float yMax = area.MaxDepthAtX(x, area.maxDepthY), yMin = area.MinDepthAtX(x, area.minDepthY);
                e.transform.position = new Vector3(x, Mathf.Lerp(yMax, yMin, depth), 0f);
                made.Add(e);
            }
            var bgo = new GameObject("Kanttiinin tappelu");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(bgo, scene);
            Undo.RegisterCreatedObjectUndo(bgo, "Kanttiinin tappelu");
            var brawl = bgo.AddComponent<SalsaBrawl>();
            brawl.area = area; brawl.instigator = made.FirstOrDefault(m => m != null && m.displayName == "Puliukko") ?? made.FirstOrDefault();
            brawl.calmSeconds = 54f; brawl.crowd = made.ToArray(); brawl.sailors = null; brawl.rogues = null;
            EditorUtility.SetDirty(brawl);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Kanttiinin väki: " + made.Count + " hahmoa (" + string.Join(", ", counts.Select(kv => kv.Value + " " + kv.Key)) + "). " + string.Join("; ", report) + (report.Count > 0 ? ". " : "") + "Rauhallisia; rähinä alkaa kun hero lyö jotakuta tai ~1 min jälkeen (Puliukko vittuilee).");
        }
        finally
        {
            foreach (var s in opened) if (s.IsValid()) EditorSceneManager.CloseScene(s, true);
            if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
        }
    }

    /// <summary>Kanttiinin väki (valikko 396).</summary>
    public static void RunVegasCanteenCrowdBatch()
    {
        batch = true;
        try { Step("396 kanttiinin väki", AddVegasCanteenCrowd); }
        finally { batch = false; }
    }
}
