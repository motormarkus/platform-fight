using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Vegasin vihut (valikko 384): kasino, katu ja baari. Mallit: Vegasin omat (Tankki-Tony 380, Melvin 382, Don 383) ja muut kopioidaan muista sceneistä (Alppikylä, Thaitown, SampleScene, Aavikko, UccoLaiva).
/// Kasino: 2 Donia + 4 Tonya vartijoina, 2 Melvinia, Lätkäfani, 2 Puliukkoa, 2 Pensalenkkaria, 2 Rustya. Katu: Melvin, Tankki-Tony, Tanya.
/// Baari: Sohvi tiskillä (sama kauppa kuin Samperi'sin baarissa), pöytiä pulloineen ja laseineen, 2 Puliukkoa, 2 Donia + 4 Tonya, Tom, 2 prätkäjätkää, 2 rokkimimmiä, Lätkäfani; rähinä alkaa minuutin kuluttua tai kun hero lyö, ja 4 portsaria tulee ruudun reunoista.
/// Aja valikot 376, 378, 379, 380, 382 ja 383 ensin. Ajon voi toistaa: vanhat Vegasin vihut korvataan, mallit säilyvät piilotettuina.</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/384. Las Vegas: kaikki vihut (kasino, katu, baari: rähinä, Sohvi, pöydät)", false, 384)]
    static void AddVegasCrowd()
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
            var areas = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == scene).ToArray();
            Area casino = areas.FirstOrDefault(a => a.areaName == "Vegasin kasino"), street = areas.FirstOrDefault(a => a.areaName == "Vegasin katu"), bar = areas.FirstOrDefault(a => a.areaName == "Vegasin baari");
            if (casino == null || street == null || bar == null) { Info("Alueita puuttuu (kasino/katu/baari): aja valikot 376, 378 ja 379."); return; }

            // ---- mallit ----
            var allEnemies = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Enemy VegasTpl(string display, string parentPrefix) => allEnemies.FirstOrDefault(e => e != null && e.gameObject.scene == scene && e.displayName == display && e.transform.parent != null && e.transform.parent.name.StartsWith(parentPrefix) && e.guardOf == null);
            var tonyT = VegasTpl("Tankki-Tony", "Tankki-Tonyt"); var melvinT = VegasTpl("Melvin", "Melvinit"); var donT = VegasTpl("Don", "Donit");
            if (tonyT == null || melvinT == null || donT == null) { Info("Vegasin mallit puuttuvat (Tony " + (tonyT != null) + ", Melvin " + (melvinT != null) + ", Don " + (donT != null) + "): aja valikot 380, 382 ja 383."); return; }

            var alp = Src(AlpineScenePathNow); var thai = Src(ThaitownScenePath); var sample = Src("Assets/Scenes/SampleScene.unity"); var desert = Src("Assets/Scenes/Areas/Aavikko.unity"); var ucco = Src(UccoLaivaScenePath);
            Enemy Find(UnityEngine.SceneManagement.Scene s, string display, bool noCopies) => !s.IsValid() ? null : Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(en => en != null && en.gameObject.scene == s && en.displayName == display && (!noCopies || (!en.gameObject.name.Contains("(terminaali") && !en.gameObject.name.Contains("(baari") && !en.gameObject.name.Contains("(hotelli") && !en.gameObject.name.Contains("(apterm"))));
            Enemy Template(string display) => Find(alp, display, true) ?? Find(thai, display, true) ?? Find(sample, display, true) ?? Find(desert, display, true) ?? Find(ucco, display, true) ?? Find(alp, display, false) ?? Find(thai, display, false);
            var tanyaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Enemies/Tanya.prefab");
            var tpl = new Dictionary<string, GameObject>();
            var missing = new List<string>();
            foreach (var n in new[] { "Lätkäfani", "Puliukko", "Pensalenkkari", "Rusty", "Tom", "Prätkäjätkä", "Rokkimimmi", "Portsari", "Tanya" })
            {
                var t = Template(n);
                GameObject g = t != null ? t.gameObject : n == "Tanya" ? tanyaPrefab : null;
                if (g == null) missing.Add(n); else tpl[n] = g;
            }
            tpl["Tankki-Tony"] = tonyT.gameObject; tpl["Melvin"] = melvinT.gameObject; tpl["Don"] = donT.gameObject;

            // ---- vanhat pois (mallien juuret piiloon) ----
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Vegasin vihut" || g.name == "Vegasin baarin kalusteet" || g.name == "Vegasin baarin tappelu").ToArray()) Undo.DestroyObjectImmediate(old);
            foreach (var rn in new[] { "Tankki-Tonyt", "Melvinit", "Donit" })
                foreach (var rg in scene.GetRootGameObjects().Where(x => x.name == rn)) rg.SetActive(false);

            var root = new GameObject("Vegasin vihut");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Vegasin vihut");
            var counts = new Dictionary<string, int>();
            float H = 2f * CamHalf, top = CamY + CamHalf;
            float imgW = 5120f / (1844f / H);

            Vector3 Spot(Area a, float x, float depth)
            {
                float yMax = a.MaxDepthAtX(x, a.maxDepthY), yMin = a.MinDepthAtX(x, a.minDepthY);
                return new Vector3(x, Mathf.Clamp(Mathf.Lerp(yMax, yMin, depth), yMin + 0.05f, yMax - 0.05f), 0f);
            }
            Enemy Spawn(Transform parent, string type, Vector3 pos, string tag)
            {
                if (!tpl.TryGetValue(type, out var tg)) return null;
                counts[type] = counts.TryGetValue(type, out int c0) ? c0 + 1 : 1;
                var go = Object.Instantiate(tg, parent);
                go.name = type + " (" + tag + " " + counts[type] + ")";
                go.transform.SetParent(parent, false);
                go.SetActive(true);
                var e = go.GetComponent<Enemy>();
                if (e == null) { Object.DestroyImmediate(go); return null; }
                e.ally = false; e.guardOf = null; e.guards = null; e.fightsEveryone = false;
                e.wanderMinX = e.wanderMaxX = 0f;
                go.transform.position = pos;
                EditorUtility.SetDirty(e);
                return e;
            }
            // hostiili vihu: herää kun hero tulee lähelle
            Enemy Hostile(Area a, Transform parent, string type, float frac, float depth, string tag)
            {
                float x = Mathf.Lerp(a.walkMinX, a.walkMaxX, frac);
                var e = Spawn(parent, type, FreeSpot(a, scene, Spot(a, x, depth)), tag);
                if (e == null) return null;
                e.wakeDistance = 13f; e.wakeOnFightRadius = 0f;
                return e;
            }
            // Don ja kaksi Tonya vartijoina
            List<Enemy> Trio(Area a, Transform parent, float x, float depth, string tag)
            {
                var don = Spawn(parent, "Don", FreeSpot(a, scene, Spot(a, x, depth)), tag);
                var list = new List<Enemy>();
                if (don == null) return list;
                list.Add(don);
                var gs = new Enemy[2];
                for (int i = 0; i < 2; i++)
                {
                    var p = Spot(a, x - 2.4f, depth + (i == 0 ? 0.14f : -0.14f));
                    var t = Spawn(parent, "Tankki-Tony", p, tag + " vartija");
                    if (t != null) { t.guardOf = don; gs[i] = t; list.Add(t); }
                }
                don.guards = gs;
                foreach (var e in list) { e.wakeDistance = 13f; e.wakeOnFightRadius = 0f; }
                return list;
            }

            // ---- kasino ----
            var cg = new GameObject("Kasino"); cg.transform.SetParent(root.transform, false);
            float CX(float f) => Mathf.Lerp(casino.walkMinX, casino.walkMaxX, f);
            Trio(casino, cg.transform, CX(0.48f), 0.55f, "kasino"); Trio(casino, cg.transform, CX(0.86f), 0.5f, "kasino");
            foreach (var (type, f, d) in new[] { ("Melvin", 0.30f, 0.6f), ("Melvin", 0.68f, 0.4f), ("Lätkäfani", 0.40f, 0.35f), ("Puliukko", 0.55f, 0.75f), ("Puliukko", 0.78f, 0.7f),
                                                  ("Pensalenkkari", 0.62f, 0.3f), ("Pensalenkkari", 0.93f, 0.7f), ("Rusty", 0.36f, 0.8f), ("Rusty", 0.74f, 0.25f) })
                Hostile(casino, cg.transform, type, f, d, "kasino");

            // ---- katu ----
            var sg = new GameObject("Katu"); sg.transform.SetParent(root.transform, false);
            Hostile(street, sg.transform, "Melvin", 0.50f, 0.6f, "katu"); Hostile(street, sg.transform, "Tankki-Tony", 0.70f, 0.4f, "katu"); Hostile(street, sg.transform, "Tanya", 0.35f, 0.5f, "katu");

            // ---- baari ----
            var bg = new GameObject("Baari"); bg.transform.SetParent(root.transform, false);
            float X0 = VegasBarX0;
            float BX(float u) => X0 + imgW * u;
            var crowd = new List<Enemy>();
            void Calm(Enemy e, bool hasFightAll)
            {
                e.wakeDistance = -1f; e.wakeOnFightRadius = 0f; e.joinsFightWhenSquadComes = false; e.fightsEveryone = hasFightAll;
                if (e.calmIdleSprites == null || e.calmIdleSprites.Length == 0) e.calmIdleSprites = e.idleSprites;
                if (e.calmWalkSprites == null || e.calmWalkSprites.Length == 0) { e.calmWalkSprites = e.walkSprites; e.calmWalkFrameTime = e.walkFrameTime; }
                float x = e.transform.position.x; e.wanderMinX = x - 1.9f; e.wanderMaxX = x + 1.9f;
                if (e.displayName == "Puliukko") e.wakeOnFightRadius = 9f;
                if (e.displayName == "Lätkäfani" || e.displayName == "Pensalenkkari") e.talksWhenCalm = true;
                crowd.Add(e);
            }
            Enemy instigator = null;
            foreach (var (type, u, d) in new[] { ("Puliukko", 0.85f, 0.65f), ("Puliukko", 2.25f, 0.7f), ("Tom", 1.70f, 0.55f), ("Prätkäjätkä", 0.55f, 0.8f), ("Prätkäjätkä", 2.05f, 0.35f),
                                                  ("Rokkimimmi", 1.30f, 0.8f), ("Rokkimimmi", 2.40f, 0.5f), ("Lätkäfani", 1.00f, 0.4f) })
            {
                var e = Spawn(bg.transform, type, FreeSpot(bar, scene, Spot(bar, BX(u), d)), "baari");
                if (e == null) continue;
                Calm(e, true);
                if (type == "Puliukko" && instigator == null) instigator = e;
            }
            foreach (var (u, d) in new[] { (1.85f, 0.5f), (2.60f, 0.7f) })
            {
                var trio = new List<Enemy>();
                var don = Spawn(bg.transform, "Don", FreeSpot(bar, scene, Spot(bar, BX(u), d)), "baari");
                if (don == null) continue;
                var gs = new Enemy[2];
                for (int i = 0; i < 2; i++)
                {
                    var t = Spawn(bg.transform, "Tankki-Tony", Spot(bar, BX(u) - 2.4f, d + (i == 0 ? 0.14f : -0.14f)), "baari vartija");
                    if (t != null) { t.guardOf = don; gs[i] = t; trio.Add(t); }
                }
                don.guards = gs; trio.Add(don);
                foreach (var e in trio) Calm(e, false);   // saattue ei tapa omiaan
            }

            // portsarit (4): tulevat ruudun reunoista rähinän alkaessa
            BouncerSquad squad = null;
            if (tpl.TryGetValue("Portsari", out var bouncerT))
            {
                var sq = new GameObject("Baarin portsarit"); sq.transform.SetParent(root.transform, false);
                squad = sq.AddComponent<BouncerSquad>();
                squad.area = bar; squad.bothSides = true; squad.fromCurrentScreenEdges = true; squad.firstDelay = 0.8f; squad.spawnInterval = 0.6f;
                squad.bouncers = new Enemy[4];
                for (int i = 0; i < 4; i++)
                {
                    var bo = Object.Instantiate(bouncerT, sq.transform);
                    bo.transform.SetParent(sq.transform, false);
                    bo.name = "Portsari (vegas " + (i + 1) + ")";
                    var be = bo.GetComponent<Enemy>();
                    be.wakeDistance = 100f; be.fightsEveryone = true; be.joinsFightWhenSquadComes = false; be.ally = false;
                    bo.transform.position = Spot(bar, BX(1.5f), 0.3f + 0.1f * i);
                    squad.bouncers[i] = be;
                    bo.SetActive(false);
                }
            }
            var bgo = new GameObject("Vegasin baarin tappelu");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(bgo, scene);
            Undo.RegisterCreatedObjectUndo(bgo, "Vegasin baarin tappelu");
            var brawl = bgo.AddComponent<SalsaBrawl>();
            brawl.area = bar; brawl.instigator = instigator;
            brawl.calmSeconds = 54f;   // 54 s + 6 s vittuilu-viive = minuutti, jos hero ei lyö ketään
            brawl.crowd = crowd.ToArray(); brawl.sailors = null; brawl.rogues = squad;
            EditorUtility.SetDirty(brawl);

            // ---- Sohvi tiskille ja pöydät ----
            var furn = new GameObject("Vegasin baarin kalusteet");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(furn, scene);
            Undo.RegisterCreatedObjectUndo(furn, "Vegasin baarin kalusteet");
            string shopInfo = "Sohvia ja tiskiä ei löytynyt SampleScenestä";
            if (sample.IsValid())
            {
                var sohviT = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.gameObject.scene == sample && t.name == "Sohvi");
                var shopT = Resources.FindObjectsOfTypeAll<ShopCounter>().FirstOrDefault(s => s.gameObject.scene == sample && s.name == "Baaritiski");
                if (sohviT != null && shopT != null)
                {
                    float sx = BX(1.10f), counterTopY = top - 0.347f * H;
                    var sh = Object.Instantiate(sohviT.gameObject, furn.transform); sh.name = "Vegasin baarin Sohvi";
                    sh.transform.SetParent(furn.transform, false);
                    var sr = sh.GetComponentInChildren<SpriteRenderer>();
                    float k = sr != null && sr.sprite != null ? 0.21f * H / sr.sprite.bounds.size.y : 1f;
                    sh.transform.localScale = Vector3.one * k; sh.transform.position = new Vector3(sx, counterTopY, 0f);
                    if (sr != null) sr.sortingOrder = -9500;
                    var shop = Object.Instantiate(shopT.gameObject, furn.transform); shop.name = "Vegasin baarin tiski";
                    shop.transform.SetParent(furn.transform, false);
                    shop.transform.position = new Vector3(sx, bar.maxDepthY, 0f);
                    var sc = shop.GetComponent<ShopCounter>();
                    sc.here = bar; sc.title = "BAARI"; sc.halfWidth = 4.5f; sc.maxDistanceFromWall = 1.4f; sc.ownItemsOnly = false;
                    shopInfo = "Sohvi tiskillä (sama kauppa kuin Samperi'sissa)";
                }
            }
            var tablePos = new List<Vector3>();
            foreach (var (u, d) in new[] { (0.45f, 0.55f), (1.05f, 0.82f), (1.65f, 0.80f), (2.20f, 0.85f), (2.85f, 0.55f), (1.60f, 0.35f) })
                tablePos.Add(Spot(bar, BX(u), d));
            var (bottles, glassN, chairs) = AddBottleTables(furn.transform, tablePos, 61);

            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Vegasin vihut asetettu: " + string.Join(", ", counts.Select(kv => kv.Value + " " + kv.Key)) + " (kopioitu mallista; baarissa 4 portsaria rähinän alkaessa).\n" + shopInfo + "; baarissa " + tablePos.Count + " pöytää (" + bottles + " pulloa, " + glassN + " lasia, " + chairs + " tuolia).\nRähinä alkaa minuutin kuluttua tai kun hero lyö jotakuta."
                 + (missing.Count > 0 ? "\nMallia ei löytynyt: " + string.Join(", ", missing) : ""));
        }
        finally
        {
            foreach (var sc in opened) if (sc.IsValid()) EditorSceneManager.CloseScene(sc, true);
            if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
