using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Valikko 392: Vegasin rakennustyömaa. Kuvat Assets/Sprites/Taustat/rakennus_maa.png ja rakennus_ylos.png (Tools/RakAssets/build_rak.py, käyttäjän kuvista).
/// Katutaso + kolme kerrosta laattojen päällä, tikkaat kerrosten välillä (SiteLevels.cs), vihuja joka kerroksessa, kaasupulloja (GasCylinder).
/// Portti kadulta (Door) ja takaisin. Aja valikot 376, 378, 380 (Tony), 382 (Melvin) ja 391:n kuvatuonti ensin. Korvaa kadun testipullot.</summary>
public static partial class BeatEmUpSetup
{
    const float VegasSiteX0 = 111000f;

    [MenuItem("Beat em up/392. Las Vegas: rakennustyömaa (4 kerrosta, tikkaat, vihut, kaasupullot)", false, 392)]
    static void AddVegasSite()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        foreach (var f in new[] { "rakennus_maa", "rakennus_ylos", "vegas_taivas", "vegas_hotellit", "vegas_palmut" })
        {
            if (!File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat). Rakennuskuvat: python Tools/RakAssets/build_rak.py"); return; }
            AssetDatabase.ImportAsset(T + f + ".png", ImportAssetOptions.ForceUpdate);
        }
        if (!File.Exists(VegasScenePath)) { Info("Aja ensin valikko 376 (Vegas)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var areas = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == scene).ToArray();
            var street = areas.FirstOrDefault(a => a.areaName == "Vegasin katu");
            if (street == null) { Info("Alue \"Vegasin katu\" puuttuu: aja valikko 378."); return; }
            var allEnemies = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(e => e != null && e.gameObject.scene == scene).ToArray();
            var tonyT = allEnemies.FirstOrDefault(e => e.displayName == "Tankki-Tony" && e.guardOf == null && e.transform.parent != null && !e.transform.parent.name.StartsWith("Donit") && !e.transform.parent.name.StartsWith("Rakennustyömaan"));
            var melvinT = allEnemies.FirstOrDefault(e => e.displayName == "Melvin" && e.transform.parent != null && !e.transform.parent.name.StartsWith("Rakennustyömaan"));
            if (tonyT == null && melvinT == null) { Info("Vegasista ei löytynyt Tankki-Tonya eikä Melvinia malliksi: aja valikot 380 ja 382."); return; }
            foreach (var n in new[] { "Rakennustyömaa", "Rakennustyömaan ovet", "Rakennustyömaan viholliset", "Rakennustyömaan kaasupullot", "Alue: Rakennustyömaa", "Vegasin kaasupullot" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);

            float H = 2f * CamHalf, cy = CamY, top = cy + CamHalf, X0 = VegasSiteX0;
            float ppu = 720f / H, W = 1672f / (941f / H);
            float totalW = 6000f / ppu;
            float YW(float stackPx) => cy + CamHalf + H - stackPx / ppu;   // pinon (ylä + maa, 1440 px) pikseli -> maailman y
            float XW(float px) => X0 + px / ppu;

            var root = new GameObject("Rakennustyömaa");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Rakennustyömaa");

            var area = ReuseArea("Alue: Rakennustyömaa");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(area.gameObject, scene);
            area.areaName = "Rakennustyömaa"; area.useSidewalk = false;
            float streetMax = top - 0.675f * H, streetMin = top - 0.985f * H;
            float[] levelY = { streetMax, YW(830f), YW(505f), YW(188f) };
            area.maxDepthY = streetMax; area.minDepthY = streetMin;
            area.depthLimits = new[] { new Vector2(X0, levelY[3] + 0.3f), new Vector2(X0 + totalW, levelY[3] + 0.3f) }; area.depthLimitsExceedMax = true;
            area.walkMinX = X0 + 1.5f; area.walkMaxX = X0 + totalW - 1.5f;
            area.camMinX = X0 + W * 0.5f; area.camMaxX = Mathf.Max(area.camMinX, X0 + totalW - W * 0.5f);
            area.camSize = CamHalf; area.camRiseY = 0f; area.camOffsetY = 0f; area.backScale = 1f;
            area.music = street.music; area.musicGain = street.musicGain;

            // taustakuvat: maa (katutaso + 1. kerros) ja yläkerrat sen päällä
            foreach (var (name, file, py) in new[] { ("Työmaa: maa", "rakennus_maa", cy), ("Työmaa: yläkerrat", "rakennus_ylos", cy + H) })
            {
                var sp = ImportShipSprite(T + file + ".png", ppu, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan tuonti epäonnistui: " + file); return; }
                var sr = new GameObject(name).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10000;
                sr.transform.position = new Vector3(X0 + totalW * 0.5f, py, 0f);
            }
            // taivas ja kaupunki takana: katutason korkeudella, taivas myös ylempänä (yläkerrat)
            {
                float left = area.camMinX - W * 0.5f;
                foreach (var (name, file, order, factor, mirrors, ys) in new[] {
                    ("Taivas", "vegas_taivas", -10100, 0.02f, 0, new[] { 0f, H, 2f * H }),
                    ("Hotellit", "vegas_hotellit", -10050, 0.25f, 1, new[] { 0.325f * H }),
                    ("Palmut", "vegas_palmut", -10040, 0.5f, 1, new[] { 0.325f * H }) })
                {
                    float ppuImg = 1844f / H;
                    var sp = ImportShipSprite(T + file + ".png", ppuImg, false, SpriteAlignment.Center);
                    if (sp == null) { Info("Kuvan tuonti epäonnistui: " + file); return; }
                    foreach (var dy in ys)
                    {
                        var sr = new GameObject("Työmaa: " + name).AddComponent<SpriteRenderer>();
                        sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = order;
                        sr.transform.position = new Vector3(left + sp.bounds.size.x * 0.5f, cy + dy, 0f);
                        var pl = sr.gameObject.AddComponent<ParallaxLayer>();
                        pl.factor = factor; pl.mirrorCopies = mirrors; pl.startCamX = area.camMinX; pl.minX = area.camMinX - 2f; pl.maxX = area.camMaxX + 2f;
                    }
                }
            }

            // kerrokset ja tikkaat
            var sl = root.AddComponent<SiteLevels>();
            sl.area = area; sl.levelY = levelY; sl.streetMin = streetMin; sl.streetMax = streetMax; sl.bandHalf = 0.28f;
            sl.levelCamOffset = new[] { 0f, 6.5f, 12.2f, 12.2f };
            sl.ladders = new[]
            {
                new SiteLevels.Ladder { x = XW(4670f), lo = 0, hi = 1 }, new SiteLevels.Ladder { x = XW(4670f), lo = 1, hi = 2 }, new SiteLevels.Ladder { x = XW(4670f), lo = 2, hi = 3 },
                new SiteLevels.Ladder { x = XW(2330f), lo = 1, hi = 2 },
                new SiteLevels.Ladder { x = XW(1365f), lo = 2, hi = 3 }, new SiteLevels.Ladder { x = XW(2977f), lo = 2, hi = 3 },
            };

            // portti kadulta: valitaan kadun x, joka on kauimpana muista ovista
            var doorRoot = new GameObject("Rakennustyömaan ovet");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, scene);
            Undo.RegisterCreatedObjectUndo(doorRoot, "Rakennustyömaan ovet");
            float imgW = 5120f / (1844f / H);
            var streetDoors = Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(d => d != null && d.gameObject.scene == scene && d.here == street).ToArray();
            float gateX = VegasStreetX0 + imgW * 3.3f, bestD = -1f;
            foreach (var u in new[] { 2.4f, 2.6f, 2.85f, 3.1f, 3.3f, 3.45f })
            {
                float x = VegasStreetX0 + imgW * u;
                float d = streetDoors.Length == 0 ? 999f : streetDoors.Min(dd => Mathf.Abs(dd.transform.position.x - x));
                if (d > bestD + 0.01f) { bestD = d; gateX = x; }
            }
            float gateY = Mathf.Lerp(street.maxDepthY, street.minDepthY, 0.15f), siteInX = XW(520f), siteY = Mathf.Lerp(streetMax, streetMin, 0.3f);
            var toSite = new GameObject("Katu: työmaan portti").AddComponent<Door>();
            toSite.transform.SetParent(doorRoot.transform, false); toSite.transform.position = new Vector3(gateX, street.maxDepthY - 0.3f, 0f);
            toSite.prompt = "Mene työmaalle"; toSite.here = street; toSite.target = area; toSite.spawnPoint = new Vector2(siteInX + 1.5f, siteY);
            toSite.halfWidth = 1.8f; toSite.maxDistanceFromWall = 2.4f; toSite.onGround = true; toSite.groundHalfDepth = 1.6f;
            var toStreet = new GameObject("Työmaa: takaisin kadulle").AddComponent<Door>();
            toStreet.transform.SetParent(doorRoot.transform, false); toStreet.transform.position = new Vector3(siteInX, streetMax - 0.3f, 0f);
            toStreet.prompt = "Takaisin kadulle"; toStreet.here = area; toStreet.target = street; toStreet.spawnPoint = new Vector2(gateX - 1.5f, gateY);
            toStreet.halfWidth = 1.8f; toStreet.maxDistanceFromWall = 2.4f; toStreet.onGround = true; toStreet.groundHalfDepth = 1.6f;

            // viholliset kerroksittain: (malli, x pikseleinä, kerros, syvyys 0 = etu … 1 = taka)
            var enemyRoot = new GameObject("Rakennustyömaan viholliset");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(enemyRoot, scene);
            Undo.RegisterCreatedObjectUndo(enemyRoot, "Rakennustyömaan viholliset");
            int ne = 0;
            void Foe(Enemy tpl, string nm, float px, int lv, float depth)
            {
                if (tpl == null) return;
                var g = Object.Instantiate(tpl.gameObject, enemyRoot.transform);
                g.name = nm;
                var ge = g.GetComponent<Enemy>(); ge.guardOf = null; ge.guards = null;
                float y = lv == 0 ? Mathf.Lerp(streetMin + 0.3f, streetMax - 0.2f, depth) : levelY[lv] + Mathf.Lerp(-0.2f, 0.2f, depth);
                g.transform.position = new Vector3(XW(px), y, 0f);
                EditorUtility.SetDirty(ge); ne++;
            }
            Foe(tonyT, "Tankki-Tony (katu 1)", 1700f, 0, 0.5f);
            Foe(melvinT, "Melvin (katu)", 3000f, 0, 0.3f);
            Foe(tonyT, "Tankki-Tony (katu 2)", 4200f, 0, 0.6f);
            Foe(tonyT, "Tankki-Tony (1. kerros)", 3500f, 1, 0.5f);
            Foe(melvinT, "Melvin (1. kerros)", 1800f, 1, 0.5f);
            Foe(tonyT, "Tankki-Tony (2. kerros)", 2200f, 2, 0.5f);
            Foe(tonyT, "Tankki-Tony (2. kerros b)", 3900f, 2, 0.5f);
            Foe(melvinT, "Melvin (katto)", 3200f, 3, 0.5f);
            Foe(tonyT, "Tankki-Tony (katto)", 1700f, 3, 0.5f);

            // kaasupullot (kuva sama kuin valikossa 391)
            const float capH = 2.2f;
            var gasSpr = ImportShipSprite("Assets/Sprites/Rekvisiitta/kaasupullo.png", 768f / capH, false, SpriteAlignment.BottomCenter);
            int ng = 0;
            if (gasSpr != null)
            {
                var gasRoot = new GameObject("Rakennustyömaan kaasupullot");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gasRoot, scene);
                Undo.RegisterCreatedObjectUndo(gasRoot, "Rakennustyömaan kaasupullot");
                foreach (var (px, lv, depth) in new[] { (1300f, 0, 0.55f), (2500f, 0, 0.4f), (3600f, 0, 0.6f), (4900f, 0, 0.45f), (1900f, 1, 0.5f), (3300f, 1, 0.5f), (1700f, 2, 0.5f), (4100f, 2, 0.5f), (2700f, 3, 0.5f), (3900f, 3, 0.5f) })
                {
                    float y = lv == 0 ? Mathf.Lerp(streetMin + 0.3f, streetMax - 0.2f, depth) : levelY[lv] + Mathf.Lerp(-0.2f, 0.2f, depth);
                    var go = new GameObject("Kaasupullo " + (++ng));
                    go.transform.SetParent(gasRoot.transform, false);
                    go.transform.position = new Vector3(XW(px), y, 0f);
                    var vis = new GameObject("Visual").AddComponent<SpriteRenderer>(); vis.transform.SetParent(go.transform, false);
                    var sh = new GameObject("Shadow").AddComponent<SpriteRenderer>(); sh.transform.SetParent(go.transform, false);
                    sh.color = new Color(0f, 0f, 0f, 0.35f);
                    var c = go.AddComponent<Crate>();
                    c.body = vis; c.shadow = sh; c.sprites = new[] { gasSpr };
                    c.breakable = false; c.anchored = true; c.visualScale = 1f; c.footOffset = 0f; c.shadowWidth = 1.2f; c.hitRadiusX = 0.8f;
                    c.moneyChance = 0f; c.energyChance = 0f; c.foodChance = 0f; c.thermosChance = 0f;
                    vis.sprite = gasSpr;
                    var gc = go.AddComponent<GasCylinder>(); gc.capHeight = capH;
                    EditorUtility.SetDirty(c); EditorUtility.SetDirty(gc);
                }
            }

            var areasPath = VegasScenePath.Replace(".unity", ".areas.txt");
            var names = File.Exists(areasPath) ? File.ReadAllLines(areasPath).ToList() : new System.Collections.Generic.List<string>();
            if (!names.Contains("Rakennustyömaa")) names.Add("Rakennustyömaa");
            File.WriteAllLines(areasPath, names);
            EditorUtility.SetDirty(area);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Rakennustyömaa rakennettu (x " + X0.ToString("0") + "–" + (X0 + totalW).ToString("0") + "): katutaso + 3 kerrosta, tikkaat (ylös/alas-napilla tikkaan kohdalla; päätikas oikealla, kaksi pidempää keskellä ja vasemmalla), " + ne + " vihua ja " + ng + " kaasupulloa. Portti kadulla x " + gateX.ToString("0") + " ja takaisin työmaan vasemmasta päästä. Kadun vanhat testipullot poistettu.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
