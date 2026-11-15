using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Valikko 398: Aavikon juna-aseman (alue "Juna-asema") lisä-NPC:t: Violetti, Nadine ja lentoemäntä kävelevät, seisovat, puhuvat ja tanssivat asemahallissa (NpcStroller-kopiot).
/// Eivät taistele eivätkä ole lyötävissä. Mallit: Violetti ja Nadine Linnakkeesta (Linnake.unity / Muuri.unity), lentoemäntä Thaitownin terminaalista.
/// Asemalla on jo Jade, Devo ja Violet (rata-asema); nämä tulevat niiden lisäksi.
/// </summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/398. Aavikko: juna-aseman NPC:t (Violetti, Nadine, lentoemäntä kävelevät)", false, 398)]
    static void AddStationNpcs()
    {
        if (PlayModeBlocked()) return;
        const string scenePath = "Assets/Scenes/Areas/Aavikko.unity";
        if (!File.Exists(scenePath)) { Info("Aavikon scenea ei löytynyt."); return; }
        var scene = EditorSceneManager.GetSceneByPath(scenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive); openedHere = true; }
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
            var area = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a != null && a.gameObject.scene == scene && a.areaName == LokkiAreaName);
            if (area == null) { Info("Aluetta \"" + LokkiAreaName + "\" ei löytynyt Aavikon scenestä."); return; }
            foreach (var o in scene.GetRootGameObjects().Where(g => g.name == "Aseman NPC:t").ToArray()) Undo.DestroyObjectImmediate(o);
            var srcs = new[] { Src("Assets/Scenes/Areas/Linnake.unity"), Src("Assets/Scenes/Areas/Muuri.unity"), Src(ThaitownScenePath) };
            var all = Object.FindObjectsByType<NpcStroller>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            NpcStroller Tmpl(System.Func<string, bool> match) => all.FirstOrDefault(w => w != null && w.gameObject.scene != scene && srcs.Any(s => s.IsValid() && s == w.gameObject.scene) && match(w.name));
            var root = new GameObject("Aseman NPC:t");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Aseman NPC:t");
            float x0 = area.walkMinX, x1 = area.walkMaxX, w = x1 - x0;
            var report = new List<string>();
            // (nimi, mallin haku, x-osuus, syvyys, koko)
            foreach (var (label, match, fx, depth, scale) in new (string, System.Func<string, bool>, float, float, float)[]
            {
                ("Violetti", n => n == "Violetti", 0.30f, 0.45f, 1.1f),
                ("Nadine", n => n == "Nadine", 0.62f, 0.60f, 1f),
                ("Lentoemäntä", n => n.StartsWith("Lentoem") && n.Contains("terminaalissa 1"), 0.82f, 0.40f, 1f),
            })
            {
                var t = Tmpl(match);
                if (t == null) { report.Add(label + ": mallia ei löytynyt"); continue; }
                var go = Object.Instantiate(t.gameObject, root.transform);
                go.name = label + " (asema)"; go.SetActive(true); go.transform.SetParent(root.transform, false);
                go.transform.localScale = t.transform.localScale * scale;
                var st = go.GetComponent<NpcStroller>();
                st.area = area; st.laneMinX = x0 + 2f; st.laneMaxX = x1 - 2f;
                st.leanChance = 0f;   // ei tiskiä asemalla
                float x = x0 + w * fx;
                go.transform.position = new Vector3(x, Mathf.Lerp(area.MaxDepthAtX(x, area.maxDepthY), area.MinDepthAtX(x, area.minDepthY), depth), 0f);
                EditorUtility.SetDirty(st);
                report.Add(label + " (x " + x.ToString("0") + ")");
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Juna-aseman NPC:t lisätty: " + string.Join(", ", report) + ". Kävelevät, seisovat, puhuvat ja tanssivat koko aseman matkalla; eivät taistele.");
        }
        finally
        {
            foreach (var s in opened) if (s.IsValid()) EditorSceneManager.CloseScene(s, true);
            if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
        }
    }

    /// <summary>Juna-aseman NPC:t (valikko 398).</summary>
    public static void RunStationNpcBatch()
    {
        batch = true;
        try { Step("398 aseman NPC:t", AddStationNpcs); }
        finally { batch = false; }
    }
}

public static partial class BeatEmUpSetup
{
    /// <summary>Valikko 399: juna-aseman kioski: puinen tiski (junatiski_ylaosa + junatiski_alaosa, kuten ravintolavaunussa) lattialla keskimmäisen lippuluukun edessä, Sohvi tiskin takana ja kauppa (ShopCounter) tiskin edessä.
    /// Tiski estää kävelyn sen taakse (depthLimits). Mallit: Dinerin Sohvi ja tiski (Aavikon scene).</summary>
    [MenuItem("Beat em up/399. Aavikko: juna-aseman tiski, Sohvi sen takana ja kioski", false, 399)]
    static void AddStationKiosk()
    {
        if (PlayModeBlocked()) return;
        const string topPath = "Assets/Sprites/Rekvisiitta/junatiski_ylaosa.png", bottomPath = "Assets/Sprites/Rekvisiitta/junatiski_alaosa.png";
        const string scenePath = "Assets/Scenes/Areas/Aavikko.unity";
        var scene = EditorSceneManager.GetSceneByPath(scenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var area = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a != null && a.gameObject.scene == scene && a.areaName == LokkiAreaName);
            if (area == null) { Info("Aluetta \"" + LokkiAreaName + "\" ei löytynyt."); return; }
            var sohviT = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t => t.name == "Dinerin Sohvi");
            var counterT = Object.FindObjectsByType<ShopCounter>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c != null && c.gameObject.scene == scene && c.name == "Dinerin tiski");
            if (sohviT == null || counterT == null) { Info("Dinerin Sohvi tai tiski puuttuu Aavikon scenestä (mallit): aja diner-valikko."); return; }
            foreach (var n in new[] { "Aseman Sohvi", "Aseman kioski", "Aseman tiski" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);
            const float X0 = 94000f, cropW = 1010f, cropH = 639f, splitRow = 330f, cs = 0.558f;   // 0,62 * 0,9: tiski 10 % pienemmäksi
            float ppu = 941f / (2f * CamHalf);                  // aseman kuva: 941 px = 2 * CamHalf
            float spritePpu = ppu / cs;                         // tiskikuvan pikseli: sama koko kuin ravintolavaunussa (leveys ~8 yks.)
            float xC = X0 + 1980f / ppu;                        // keskimmäisen lippuluukun kohdalla
            float floorY = area.MaxDepthAtX(xC, area.maxDepthY), baseY = floorY - 0.12f;   // alareuna lähes samalla tasolla kuin lippuluukkujen alareuna (seinän ja lattian raja)
            float RowToY(float cropRow) => baseY + (cropH - cropRow) * cs / ppu;
            var root = new GameObject("Aseman tiski");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Aseman tiski");
            AssetDatabase.ImportAsset(topPath, ImportAssetOptions.ForceUpdate); AssetDatabase.ImportAsset(bottomPath, ImportAssetOptions.ForceUpdate);
            var topSpr = ImportShipSprite(topPath, spritePpu, false, SpriteAlignment.BottomCenter);
            var botSpr = ImportShipSprite(bottomPath, spritePpu, false, SpriteAlignment.BottomCenter);
            if (topSpr == null || botSpr == null) { Info("Tiskin kuvien tuonti epäonnistui."); return; }
            var back = new GameObject("Takabaari").AddComponent<SpriteRenderer>();
            back.transform.SetParent(root.transform, false); back.sprite = topSpr; back.sortingOrder = -9600;
            back.transform.position = new Vector3(xC, RowToY(splitRow), 0f);
            var front = new GameObject("Tiskilevy").AddComponent<SpriteRenderer>();
            front.transform.SetParent(root.transform, false); front.sprite = botSpr; front.sortingOrder = -9400;   // hahmojen takana, Sohvin edessä
            front.transform.position = new Vector3(xC, baseY, 0f);

            // pullot hyllyille (koristeita, kuten ravintolavaunussa): kaksi hyllyriviä, vasen ja oikea puolisko (keskipylväs välissä)
            float ColToX(float cropCol) => xC + (cropCol - cropW * 0.5f) * cs / ppu;
            var kinds = new List<(Sprite sp, float rows, float wpx)>();   // wpx = pullon näkyvä leveys kuvaruudussa (128 x 96 px), jotta hyllyt täyttyvät tiiviisti
            foreach (var (n, rows, wpx) in new[] { ("pullo_olut", 52f, 19f), ("pullo_sininen", 58f, 27f), ("pullo_likoori", 64f, 26f), ("pullo_vodka", 70f, 19f) })
            {
                string bp = FindTexture(n); if (bp == null) continue;
                SetupAndSlice(bp);
                var sp = LoadSprites(n).OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).FirstOrDefault();
                if (sp != null) kinds.Add((sp, rows, wpx));
            }
            int bottleCount = 0;
            if (kinds.Count > 0)
            {
                var rnd = new System.Random(135);
                foreach (float shelfRow in new[] { 182f, 312f })
                    foreach (var (c0, c1) in new[] { (125f, 480f), (530f, 890f) })
                    {
                        float c = c0 + 14f;
                        while (true)
                        {
                            var kd = kinds[rnd.Next(kinds.Count)];
                            float hUnits = kd.rows * (0.92f + 0.16f * (float)rnd.NextDouble()) / ppu;
                            float scale = hUnits / kd.sp.bounds.size.y;
                            float wCols = (kd.wpx / 96f) * hUnits * ppu / cs;   // pullon näkyvä leveys tiskikuvan sarakkeina
                            if (c + wCols > c1) break;
                            var bGo = new GameObject("Hyllypullo").AddComponent<SpriteRenderer>();
                            bGo.transform.SetParent(root.transform, false);
                            bGo.sprite = kd.sp; bGo.sortingOrder = -9560;           // takabaarin edessä, Sohvin takana
                            bGo.transform.localScale = new Vector3(scale, scale, 1f);
                            bGo.transform.position = new Vector3(ColToX(c + wCols * 0.5f), RowToY(shelfRow) - kd.sp.bounds.min.y * scale - (5f / 96f) * hUnits, 0f);   // pullon jalka (5 px kuvaruudun alareunan yläpuolella) hyllylaudalle
                            bottleCount++;
                            c += wCols + 3f + 8f * (float)rnd.NextDouble();
                            if (rnd.Next(7) == 0) c += 30f;                          // välillä aukko
                        }
                    }
            }

            // Sohvi tiskin takana: rintakuva, alareuna tiskilevyn yläreunan tasalle
            var sg = Object.Instantiate(sohviT.gameObject); sg.name = "Aseman Sohvi";
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sg, scene); sg.transform.SetParent(root.transform, true); sg.SetActive(true);
            sg.transform.position = new Vector3(xC, baseY + 0.05f, 0f);
            sg.transform.localScale = sohviT.localScale * 0.9f;
            var rS = sg.GetComponentInChildren<SpriteRenderer>();
            if (rS != null) { rS.sortingOrder = -9500; sg.transform.position += Vector3.up * ((RowToY(splitRow) - 0.08f) - rS.bounds.min.y); }

            // tiski estää kävelyn sen taakse (kuten ravintolavaunussa)
            float halfW = cropW * 0.5f * cs / ppu, cl = xC - halfW, cr = xC + halfW;
            Undo.RecordObject(area, "Aseman tiski");
            area.depthLimits = new[]
            {
                new Vector2(area.walkMinX, floorY), new Vector2(cl - 0.15f, floorY), new Vector2(cl, Mathf.Min(floorY, baseY + 0.2f)), new Vector2(cr, Mathf.Min(floorY, baseY + 0.2f)), new Vector2(cr + 0.15f, floorY), new Vector2(area.walkMaxX, floorY),
            };
            EditorUtility.SetDirty(area);

            var shopGo = Object.Instantiate(counterT.gameObject, root.transform); shopGo.name = "Aseman kioski";
            shopGo.SetActive(true);
            shopGo.transform.position = new Vector3(xC, baseY + 0.2f, 0f);
            var sc = shopGo.GetComponent<ShopCounter>();
            sc.here = area; sc.title = "ASEMAN KIOSKI"; sc.npcName = "Sohvi"; sc.prompt = "Puhu Sohville";
            sc.halfWidth = halfW; sc.maxDistanceFromWall = 1.5f; sc.ownItemsOnly = false;
            // sama valikoima kuin Deacon's Denissä (Aavikkobaarin "Baarin tiski"): kopioidaan kohteet kenttä kentältä
            var barShop = Object.FindObjectsByType<ShopCounter>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c != null && c.gameObject.scene == scene && c.name == "Baarin tiski");
            string menu;
            if (barShop != null && barShop.items != null && barShop.items.Length > 0)
            {
                sc.items = barShop.items.Select(it => { var c = new ShopCounter.Item(); foreach (var f in typeof(ShopCounter.Item).GetFields()) f.SetValue(c, f.GetValue(it)); return c; }).ToArray();
                menu = string.Join(", ", sc.items.Select(i => i.name + " " + i.price + " mk"));
            }
            else
            {
                sc.items = new[] { new ShopCounter.Item { name = "Kahvi", price = 4, heal = 0, stamina = 60, comment = "Kuumaa ja vahvaa." } };
                menu = "Kahvi (Deacon's Denin tiskiä ei löytynyt)";
            }
            EditorUtility.SetDirty(sc);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info(bottleCount + " pulloa hyllyillä. Juna-aseman tiski (puinen, " + (2f * halfW).ToString("0.0") + " yks. leveä) lattialla keskimmäisen lippuluukun edessä (x " + xC.ToString("0.0") + "), Sohvi sen takana ja kauppa tiskin edessä: " + menu + ". Tiski estää kävelyn sen taakse.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    /// <summary>Juna-aseman kioski (valikko 399).</summary>
    public static void RunStationKioskBatch()
    {
        batch = true;
        try { Step("399 aseman kioski", AddStationKiosk); }
        finally { batch = false; }
    }
}
