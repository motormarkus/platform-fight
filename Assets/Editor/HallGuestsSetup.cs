using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Natsilinnakkeen juhlahallin vieraat (oktoberfest): Tom, Lätkäfani, Betty, Rekkamies, 2 Kovista, 2 punkkaria, 2 pensalenkkaria, 2 samoalaista, 2 prätkäjätkää,
/// 2 mongolia, 2 kuurobottia, 2 mainaria ja 2 puliukkoa tepastelevat rauhassa (kuten terminaalin väki); Sohvi myy ruokaa ja juomaa tiskillä. Mallit haetaan muista sceneistä.
/// Tappelu alkaa minuutin kuluttua tai kun hero lyö jotakuta (SalsaBrawl). Gestapot (valikko 334) ja Nadine/Violetti (335/336) ovat erikseen.</summary>
public static partial class BeatEmUpSetup
{
    static readonly string[] HallGuestTypes =
    {
        "Tom", "Kovis", "Mongolisoturi", "Punkkari", "Samoalainen", "Pensalenkkari", "Kuurobotti", "Prätkäjätkä", "Mainari", "Puliukko", "Lätkäfani",
        "Kovis", "Mongolisoturi", "Punkkari", "Samoalainen", "Pensalenkkari", "Kuurobotti", "Prätkäjätkä", "Mainari", "Puliukko", "Betty", "Rekkamies",
    };

    [MenuItem("Beat em up/337. Juhlahalli: vieraat (Tom, Betty, Rekkamies, Kovikset, punkkarit, ...) ja Sohvi tiskille", false, 337)]
    static void AddHallGuests()
    {
        if (PlayModeBlocked()) return;
        if (!File.Exists(WallScenePath)) { Info("Aja ensin valikko 327 (Kiinan muuri)."); return; }
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
            var scene = Src(WallScenePath);
            var hall = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Muurin juhlahalli" && a.gameObject.scene == scene);
            if (hall == null) { Info("Juhlahallia ei löytynyt: aja valikko 330 (tai 327)."); return; }
            var sources = new[] { Src(ThaitownScenePath), Src(UccoLaivaScenePath), Src("Assets/Scenes/Areas/Aavikko.unity"), Src("Assets/Scenes/SampleScene.unity"), Src(JungleScenePath),
                                  Src(AlpineScenePathNow), Src("Assets/Scenes/Areas/Nyrkkeilysali.unity"), scene }.Where(s => s.IsValid()).ToArray();
            var oldRoots = scene.GetRootGameObjects().Where(g => g.name == "Juhlahallin vieraat" || g.name == "Juhlahallin tappelu" || g.name.StartsWith("Juhlahallin portsarit") || g.name == "Juhlahallin Sohvi").ToArray();
            bool Old(Enemy e) { for (var t = e.transform; t != null; t = t.parent) if (t.name == "Juhlahallin vieraat") return true; return false; }
            Enemy Template(string display)
            {
                foreach (var sc in sources)
                {
                    var all = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .Where(en => en != null && en.gameObject.scene == sc && en.displayName == display && !Old(en) && !en.gameObject.name.Contains("Katto") && !en.gameObject.name.Contains("Viem") && !en.gameObject.name.Contains("per)")).ToArray();
                    var pick = all.FirstOrDefault(en => en.gameObject.name == display) ?? all.FirstOrDefault(en => !en.gameObject.name.Contains("(")) ?? all.FirstOrDefault();
                    if (pick != null) return pick;
                }
                return null;
            }
            float ppu = 941f / (2f * CamHalf);
            var root = new GameObject("Juhlahallin vieraat");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Juhlahallin vieraat");
            var made = new List<Enemy>(); var counts = new Dictionary<string, int>(); var tmpls = new Dictionary<string, Enemy>(); var report = new List<string>();
            float[] depths = { 0.1f, 0.85f, 0.45f, 0.97f, 0.25f, 0.65f, 0.05f, 0.75f, 0.35f, 0.9f, 0.55f };   // levittäytyvät koko lattian syvyyteen
            int idx = 0;
            foreach (var type in HallGuestTypes)
            {
                float px = 1050f + idx * 175f, depth = depths[idx % depths.Length]; idx++;
                if (!tmpls.ContainsKey(type)) tmpls[type] = Template(type);
                if (tmpls[type] == null) { if (!counts.ContainsKey(type + " (puuttuu)")) { counts[type + " (puuttuu)"] = 1; report.Add(type + ": mallia ei löytynyt"); } continue; }
                counts[type] = counts.TryGetValue(type, out int c0) ? c0 + 1 : 1;
                var go = Object.Instantiate(tmpls[type].gameObject); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene); go.transform.SetParent(root.transform, false);
                go.name = type + " (halli " + counts[type] + ")"; go.SetActive(true);
                var e = go.GetComponent<Enemy>(); if (e == null) { Object.DestroyImmediate(go); continue; }
                float x = WallHallX0 + px / HallPpu;
                e.wakeDistance = -1f; e.wakeOnFightRadius = 0f; e.joinsFightWhenSquadComes = false; e.fightsEveryone = true; e.ally = false; e.brawlsWithOthers = false;
                if (e.calmIdleSprites == null || e.calmIdleSprites.Length == 0) e.calmIdleSprites = e.idleSprites;
                if (e.calmWalkSprites == null || e.calmWalkSprites.Length == 0) { e.calmWalkSprites = e.walkSprites; e.calmWalkFrameTime = e.walkFrameTime; }
                e.wanderMinX = x - 420f / ppu; e.wanderMaxX = x + 420f / ppu;
                if (e.displayName == "Puliukko") e.wakeOnFightRadius = 9f;
                if (e.displayName == "Lätkäfani") e.talksWhenCalm = true;
                e.transform.position = new Vector3(x, Mathf.Lerp(hall.maxDepthY, hall.minDepthY, depth), 0f);
                made.Add(e);
            }
            var bgo = new GameObject("Juhlahallin tappelu");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(bgo, scene);
            // portsarit: tappelun alkaessa 2 + 2 ruudun molemmista reunoista, 30 s myöhemmin vielä 2 + 2 (kaksi osastoa; toisen ensimmäinen viive 30 s)
            var bouncerT = Template("Portsari");
            BouncerSquad bounceA = null, bounceB = null;
            if (bouncerT != null)
            {
                BouncerSquad Squad(string name, float delay)
                {
                    var sgo = new GameObject(name);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sgo, scene);
                    var sq = sgo.AddComponent<BouncerSquad>();
                    sq.area = hall; sq.bothSides = true; sq.fromCurrentScreenEdges = true; sq.firstDelay = delay; sq.spawnInterval = 0.7f;
                    sq.bouncers = new Enemy[4];
                    for (int i = 0; i < 4; i++)
                    {
                        var bo = Object.Instantiate(bouncerT.gameObject, sgo.transform); bo.name = "Portsari (halli " + name + " " + (i + 1) + ")";
                        var be = bo.GetComponent<Enemy>();
                        be.wakeDistance = 100f; be.fightsEveryone = true; be.joinsFightWhenSquadComes = false; be.ally = false;
                        bo.transform.position = new Vector3(WallHallX0 + 3000f / HallPpu, Mathf.Lerp(hall.maxDepthY, hall.minDepthY, 0.3f + 0.13f * i), 0f);
                        sq.bouncers[i] = be; bo.SetActive(false);
                    }
                    return sq;
                }
                bounceA = Squad("Juhlahallin portsarit 1", 0.8f);
                bounceB = Squad("Juhlahallin portsarit 2", 30f);
                bounceB.transform.SetParent(bounceA.transform.parent, false);
            }
            else report.Add("Portsarin mallia ei löytynyt (Thaitownin scene)");
            var brawl = bgo.AddComponent<SalsaBrawl>();
            brawl.area = hall; brawl.instigator = made.FirstOrDefault(m => m != null && m.displayName == "Puliukko") ?? made.FirstOrDefault();
            brawl.calmSeconds = 54f; brawl.crowd = made.ToArray(); brawl.sailors = bounceA; brawl.rogues = bounceB;   // 54 s + 6 s vittuilu = minuutti
            EditorUtility.SetDirty(brawl);

            // Sohvi tiskille (sama kauppa kuin terminaalissa, tarjoilu oktoberfestin mukaan)
            var alp = Src(AlpineScenePathNow);
            var sohviT = alp.IsValid() ? Object.FindObjectsByType<Dancer>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c.gameObject.scene == alp && c.name == "Sohvi (afterski-baari)") : null;
            var counterT = alp.IsValid() ? Object.FindObjectsByType<ShopCounter>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c.gameObject.scene == alp && c.name == "Afterski-baarin tiski (Sohvi)") : null;
            if (sohviT != null && counterT != null)
            {
                var sroot = new GameObject("Juhlahallin Sohvi");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sroot, scene);
                float sx = WallHallX0 + 1500f / HallPpu, sy = hall.maxDepthY + 0.2f;
                var sg = Object.Instantiate(sohviT.gameObject); sg.name = "Sohvi (juhlahalli)";
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sg, scene); sg.transform.SetParent(sroot.transform, false);
                sg.transform.position = new Vector3(sx, sy, 0f); sg.transform.localScale = Vector3.one * AlpTermSohviScale;
                var rS = sg.GetComponentInChildren<SpriteRenderer>(); if (rS != null) rS.sortingOrder = Mathf.RoundToInt(-sy * 100f);
                var shopGo = Object.Instantiate(counterT.gameObject); shopGo.name = "Juhlahallin tiski (Sohvi)";
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(shopGo, scene); shopGo.transform.SetParent(sroot.transform, false);
                shopGo.transform.position = new Vector3(sx, hall.maxDepthY, 0f);
                var sc = shopGo.GetComponent<ShopCounter>();
                sc.here = hall; sc.title = "OKTOBERFEST"; sc.npcName = "Sohvi"; sc.prompt = "Puhu Sohville"; sc.halfWidth = 3.6f; sc.maxDistanceFromWall = 1.5f; sc.ownItemsOnly = false;
                sc.items = new[]
                {
                    new ShopCounter.Item { name = "Makkara", price = 10, heal = 55, stamina = 0, comment = "Grillistä, sinappia päälle." },
                    new ShopCounter.Item { name = "Rinkeli", price = 5, heal = 30, stamina = 0, comment = "Suolaa riittää." },
                    new ShopCounter.Item { name = "Kahvi", price = 4, heal = 0, stamina = 60, comment = "Kuumaa ja vahvaa." },
                    new ShopCounter.Item { name = "Olut", price = 8, heal = 30, stamina = 999, comment = "Tuoppi kylmää." },
                };
            }
            else report.Add("Sohvin malleja ei löytynyt (aja valikko 279)");

            foreach (var o in oldRoots) Undo.DestroyObjectImmediate(o);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Juhlahalliin " + made.Count + " vierasta (" + string.Join(", ", counts.Select(kv => kv.Value + " " + kv.Key)) + "). Rauhassa, tappelu 60 s kuluttua tai kun hero lyö. " + string.Join("; ", report) + "\nAjamatta Unityssä.");
        }
        finally { foreach (var sc in opened) if (sc.IsValid()) EditorSceneManager.CloseScene(sc, true); }
    }

    [MenuItem("Beat em up/339. Linnake kerralla (vain linnake: 340 halli + 334 gestapot, 335 Nadine, 336 Violetti, 337 vieraat, 338 aloitus linnakkeen edestä)", false, 339)]
    static void BuildWholeFortress()
    {
        if (PlayModeBlocked()) return;
        var steps = new (string name, System.Action run)[]
        {
            ("340 Linnake (halli, ovet, pöydät)", BuildFortressOnly),
            ("334 Gestapot", AddGestapo), ("335 Nadine", AddNadine), ("336 Violetti", AddViolettiHall), ("337 Juhlahallin vieraat ja Sohvi", AddHallGuests),
            ("338 Aloitus linnakkeen edestä", StartAtFortressImpl),
        };
        var summary = new List<string>();
        batch = true; batchLog.Clear();
        try
        {
            foreach (var (name, run) in steps)
            {
                int before = batchLog.Count;
                try { run(); }
                catch (System.Exception ex) { batchLog.Add("VIRHE: " + ex.Message); Debug.LogException(ex); }
                EditorUtility.UnloadUnusedAssetsImmediate(); System.GC.Collect();   // vapauttaa vaiheen tekstuurit: pitkä ajo (esim. 330) loppui muistin täyttymiseen (Unity kaatui)
                var lines = batchLog.Skip(before).ToList();
                string first = lines.Count > 0 ? lines[0].Split('\n')[0] : "(ei ilmoitusta)";
                summary.Add("- " + name + ": " + (first.Length > 150 ? first.Substring(0, 150) + "..." : first));
            }
        }
        finally { batch = false; }
        Info("Linnake valmis (kaikki vaiheet ajettu järjestyksessä); peli alkaa linnakkeen edestä. Yhteenveto:\n" + string.Join("\n", summary) + "\n\nTarkemmat ilmoitukset ovat Consolessa (Beat em up: ...).");
    }
}
