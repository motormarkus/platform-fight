using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Valikko 392: Vegasin rakennustyömaa kadun jatkona (kappelin oikealla puolella, samalla alueella "Vegasin katu").
/// Kuva: yksi 15360x5632 kuva (output/imagegen/vegas-tyomaa-yhtena), jaettu kolmeen palaan Assets/Sprites/Taustat/rakennus_1..3.png (Tools/RakAssets/build_rak2.py).
/// Katutaso + kolme laattaa (SiteLevels.cs: tikkaat, vihut seuraavat), vihuja ja kaasupulloja jokaisessa kerroksessa. Aja valikot 376, 378, 380, 382 ensin.
/// Huom: valikon 378 uudelleenajo palauttaa kadun alkuperäiseksi; aja sitten 392 uudelleen.</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/392. Las Vegas: rakennustyömaa kadun jatkoksi (4 kerrosta, tikkaat, vihut, kaasupullot)", false, 392)]
    static void AddVegasSite()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        foreach (var f in new[] { "rakennus_1", "rakennus_2", "rakennus_3", "rakennus_4" })
            if (!File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat). Aja python Tools/RakAssets/build_rak2.py"); return; }
        if (!File.Exists(VegasScenePath)) { Info("Aja ensin valikko 376 (Vegas)."); return; }
        var openedScenes = new System.Collections.Generic.List<UnityEngine.SceneManagement.Scene>();
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var areas = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == scene).ToArray();
            var street = areas.FirstOrDefault(a => a.areaName == "Vegasin katu");
            if (street == null) { Info("Alue \"Vegasin katu\" puuttuu: aja valikko 378."); return; }
            // mallit muista scenistä (työmaalle sopivat: mainarit, koviksat, rekkamies); scenet avataan hetkeksi ja suljetaan lopuksi
            Enemy Template(string sceneName, string display)
            {
                string path = "Assets/Scenes/Areas/" + sceneName + ".unity";
                if (!File.Exists(path)) return null;
                var sc = EditorSceneManager.GetSceneByPath(path);
                if (!sc.IsValid() || !sc.isLoaded) { sc = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive); openedScenes.Add(sc); }
                return Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(e => e != null && e.gameObject.scene == sc && e.displayName == display && e.guardOf == null);
            }
            var mainariT = Template("Viidakko", "Mainari");
            var kovisT = Template("Thaitown", "Kovis");
            var rekkaT = Template("Thaitown", "Rekkamies");
            if (mainariT == null && kovisT == null && rekkaT == null) { Info("Malleja ei löytynyt (Viidakko: Mainari, Thaitown: Kovis, Rekkamies)."); return; }
            foreach (var n in new[] { "Rakennustyömaa", "Rakennustyömaan ovet", "Rakennustyömaan viholliset", "Rakennustyömaan kaasupullot", "Alue: Rakennustyömaa", "Vegasin kaasupullot" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);

            float H = 2f * CamHalf, cy = CamY, bottom = cy - CamHalf;
            foreach (var csr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))   // kadun kuvan todellinen alareuna (ei editorin kamerasta)
                if (csr.gameObject.scene == scene && csr.gameObject.name == "Katu: hääkappeli") { bottom = csr.bounds.min.y; cy = bottom + CamHalf; break; }
            float ppuFull = 1844f / H;                                   // alkuperäisen 15360x5632-kuvan pikseliä / yksikkö (sama kuin kadun kuvat)
            float imgW = 5120f / ppuFull, W = 1672f / (941f / H);
            float chapL = VegasStreetX0 + 2f * imgW + imgW * VegasChapelOverlapFrac;
            float siteX0 = chapL + imgW;                                 // kappelikuvan oikea reuna
            float totalW = 3f * imgW;
            float XW(float px) => siteX0 + px / ppuFull;
            float YW(float py) => bottom + (5632f - py) / ppuFull;

            var root = new GameObject("Rakennustyömaa");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Rakennustyömaa");

            // kuvapalat: täysi tarkkuus, 4 x 3840x5632, ppu = ppuFull (yksi palanen = 0,75 kuvanleveyttä)
            for (int i = 0; i < 4; i++)
            {
                var sp = ImportShipSprite(T + "rakennus_" + (i + 1) + ".png", ppuFull, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan tuonti epäonnistui: rakennus_" + (i + 1)); return; }
                var sr = new GameObject("Työmaa " + (i + 1)).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10000 + i;
                sr.transform.position = new Vector3(siteX0 + imgW * 0.75f * (i + 0.5f), bottom + sp.bounds.size.y * 0.5f, 0f);
            }

            // kerrokset: katu + laatat (kuvan pikselirivit: 3484, 2180, 864 + 25 jalkojen kohta)
            float streetMax = street.maxDepthY, streetMin = street.minDepthY;
            float[] levelY = { streetMax, YW(3484f + 25f), YW(2180f + 25f), YW(864f + 25f) };
            float topY = levelY[3] + 0.4f;

            // kadun alue jatkuu työmaalle
            street.walkMaxX = siteX0 + totalW - 1.5f;
            street.camMaxX = Mathf.Max(street.camMinX, siteX0 + totalW - W * 0.5f);
            street.depthLimits = new[] { new Vector2(VegasStreetX0, streetMax), new Vector2(siteX0 - 3f, streetMax), new Vector2(siteX0, topY), new Vector2(siteX0 + totalW, topY) };
            street.depthLimitsExceedMax = true;
            EditorUtility.SetDirty(street);
            var streetRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Vegasin katu");
            if (streetRoot != null)
            {
                foreach (var pl in streetRoot.GetComponentsInChildren<ParallaxLayer>(true))
                {
                    pl.maxX = street.camMaxX + 2f;
                    // korkealla kaukainen kaupunki seuraa kameraa osittain: hotellien tyvi jää alareunan alle, yläosat näkyvät kerrosten välissä
                    if (pl.name == "Katu: Hotellit") { pl.verticalFollow = 0.55f; pl.refCamY = cy; }
                    else if (pl.name == "Katu: Palmut") { pl.verticalFollow = 0.6f; pl.refCamY = cy; }
                    EditorUtility.SetDirty(pl);
                }
                // korkea yötaivas (5120x5632, alareuna = kadun taivaan alareuna): korvaa kadun taivaan työmaan kohdalla näkyvän osan; sama vaakaparallaksi
                var skySp = ImportShipSprite(T + "vegas_taivas_korkea.png", ppuFull * 0.5f, false, SpriteAlignment.Center);
                var streetSky = streetRoot.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r => r.name == "Katu: Taivas");
                if (skySp != null && streetSky != null)
                {
                    var sr = new GameObject("Työmaa: korkea taivas").AddComponent<SpriteRenderer>();
                    sr.transform.SetParent(root.transform, false); sr.sprite = skySp; sr.sortingOrder = streetSky.sortingOrder + 1;
                    float left = street.camMinX - W * 0.5f;
                    sr.transform.position = new Vector3(left + skySp.bounds.size.x * 0.5f, bottom + skySp.bounds.size.y * 0.5f, 0f);
                    var pl = sr.gameObject.AddComponent<ParallaxLayer>();
                    pl.factor = 0.02f; pl.mirrorCopies = 1; pl.verticalFollow = 0.5f; pl.refCamY = cy; pl.startCamX = street.camMinX; pl.minX = street.camMinX - 2f; pl.maxX = street.camMaxX + 2f;
                }
            }

            // kerrokset ja tikkaat
            float Cam(float feetY) => feetY + 0.19f * H - cy;
            var sl = root.AddComponent<SiteLevels>();
            sl.area = street; sl.levelY = levelY; sl.streetMin = streetMin; sl.streetMax = streetMax; sl.bandHalf = 0.28f;
            sl.levelCamOffset = new[] { 0f, Cam(levelY[1]), Cam(levelY[2]), Cam(levelY[3]) };
            sl.siteMinX = siteX0; sl.siteMaxX = siteX0 + totalW;
            sl.slabMinX = XW(1400f); sl.slabMaxX = XW(14050f);
            sl.climbSpeed = 5.5f;
            float lxA = XW(2110f), lxB = XW(7793f), lxC = XW(13344f);   // tikasakselit (tikaskuvat build_rak2.py)
            sl.ladders = new[]
            {
                new SiteLevels.Ladder { x = lxC, lo = 0, hi = 1 }, new SiteLevels.Ladder { x = lxC, lo = 1, hi = 2 }, new SiteLevels.Ladder { x = lxC, lo = 2, hi = 3 },
                new SiteLevels.Ladder { x = lxA, lo = 1, hi = 2 }, new SiteLevels.Ladder { x = lxA, lo = 2, hi = 3 },
                new SiteLevels.Ladder { x = lxB, lo = 1, hi = 2 }, new SiteLevels.Ladder { x = lxB, lo = 2, hi = 3 },
            };

            // viholliset kerroksittain: (malli, x kuvan pikseleinä, kerros, syvyys 0 = etu … 1 = taka)
            var enemyRoot = new GameObject("Rakennustyömaan viholliset");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(enemyRoot, scene);
            Undo.RegisterCreatedObjectUndo(enemyRoot, "Rakennustyömaan viholliset");
            float PosY(int lv, float depth) => lv == 0 ? Mathf.Lerp(streetMin + 0.3f, streetMax - 0.2f, depth) : levelY[lv] + Mathf.Lerp(-0.2f, 0.2f, depth);
            int ne = 0;
            void Foe(Enemy tpl, string nm, float px, int lv, float depth)
            {
                if (tpl == null) return;
                var g = Object.Instantiate(tpl.gameObject, enemyRoot.transform);
                g.name = nm;
                var ge = g.GetComponent<Enemy>(); ge.guardOf = null; ge.guards = null;
                g.transform.position = new Vector3(XW(px), PosY(lv, depth), 0f);
                EditorUtility.SetDirty(ge); ne++;
            }
            Foe(kovisT, "Kovis (katu 1)", 4200f, 0, 0.5f);
            Foe(mainariT, "Mainari (katu)", 8200f, 0, 0.3f);
            Foe(kovisT, "Kovis (katu 2)", 11000f, 0, 0.6f);
            Foe(mainariT, "Mainari (1. laatta)", 6000f, 1, 0.5f);
            Foe(rekkaT, "Rekkamies (1. laatta)", 9800f, 1, 0.5f);
            Foe(mainariT, "Mainari (1. laatta b)", 12000f, 1, 0.5f);
            Foe(kovisT, "Kovis (2. laatta)", 4800f, 2, 0.5f);
            Foe(mainariT, "Mainari (2. laatta)", 10500f, 2, 0.5f);
            Foe(mainariT, "Mainari (katto)", 8800f, 3, 0.5f);
            Foe(rekkaT, "Rekkamies (katto)", 4200f, 3, 0.5f);

            // kaasupullot
            const float capH = 2.2f;
            var gasSpr = ImportShipSprite("Assets/Sprites/Rekvisiitta/kaasupullo.png", 768f / capH, false, SpriteAlignment.BottomCenter);
            int ng = 0;
            if (gasSpr != null)
            {
                var gasRoot = new GameObject("Rakennustyömaan kaasupullot");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gasRoot, scene);
                Undo.RegisterCreatedObjectUndo(gasRoot, "Rakennustyömaan kaasupullot");
                foreach (var (px, lv, depth) in new[] { (3600f, 0, 0.55f), (6500f, 0, 0.4f), (9300f, 0, 0.6f), (12000f, 0, 0.45f), (3500f, 1, 0.5f), (8600f, 1, 0.5f), (11500f, 1, 0.5f), (3300f, 2, 0.5f), (6700f, 2, 0.5f), (12000f, 2, 0.5f), (6000f, 3, 0.5f), (11000f, 3, 0.5f) })
                {
                    var go = new GameObject("Kaasupullo " + (++ng));
                    go.transform.SetParent(gasRoot.transform, false);
                    go.transform.position = new Vector3(XW(px), PosY(lv, depth), 0f);
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
            if (File.Exists(areasPath))
            {
                var names = File.ReadAllLines(areasPath).Where(x => x != "Rakennustyömaa").ToArray();
                File.WriteAllLines(areasPath, names);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Rakennustyömaa rakennettu kadun jatkoksi kappelin oikealle puolelle (x " + siteX0.ToString("0") + "–" + (siteX0 + totalW).ToString("0") + ", alue \"Vegasin katu\"): katutaso + 3 laattaa, tikkaat (ylös/alas-napilla; päätikas oikealla, kaksi muuta keskellä ja vasemmalla), " + ne + " vihua ja " + ng + " kaasupulloa. Kadun jatkoa ei tarvitse portin kautta.");
        }
        finally { foreach (var sc in openedScenes) if (sc.IsValid()) EditorSceneManager.CloseScene(sc, true); if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
