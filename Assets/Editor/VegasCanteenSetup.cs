using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Valikko 394: Vegasin työmaakonttorin kanttiini (alakerta): kaksi kuvaa (10240x1844), ovi kadulta konttorin pariovesta, ikkunoista näkyy yötaivas ja hotellit,
/// tiskin takana Sohvi (kauppa), pöydät ja makkara-annokset (koristeita kuten vankilan ruokalassa). Portaat yläkertaan (Nokkamiehen toimisto) tehdään myöhemmin.
/// Aja valikot 392 ja 393 ensin (konttorin paikka) sekä 376/390 (Sohvin ja tiskin mallit kasinon baarista).</summary>
public static partial class BeatEmUpSetup
{
    const float VegasCanteenX0 = 111000f, VegasOfficeUpX0 = 112000f;

    [MenuItem("Beat em up/394. Las Vegas: työmaakonttorin kanttiini (ovi kadulta, Sohvi tiskillä, pöydät ja makkara-annokset)", false, 394)]
    static void AddVegasCanteen()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        string[] files = { "vegas_kanttiini_huone_1", "vegas_kanttiini_huone_2", "vegas_kanttiini_tiski_1", "vegas_kanttiini_tiski_2", "vegas_taivas", "vegas_hotellit", "vegas_ylakerta_1", "vegas_ylakerta_2" };
        foreach (var f in files)
            if (!File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat)."); return; }
        if (!File.Exists(VegasScenePath)) { Info("Aja ensin valikko 376 (Vegas)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var street = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a != null && a.gameObject.scene == scene && a.areaName == "Vegasin katu");
            var site = Object.FindObjectsByType<SiteLevels>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(s => s != null && s.gameObject.scene == scene && s.gameObject.name == "Rakennustyömaa");
            if (street == null || site == null) { Info("Alue \"Vegasin katu\" tai työmaa puuttuu: aja valikot 378, 392 ja 393."); return; }
            var sohviT = Object.FindObjectsByType<Dancer>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c != null && c.gameObject.scene == scene && c.name == "Sohvi (kasinon baari)");
            var counterT = Object.FindObjectsByType<ShopCounter>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c != null && c.gameObject.scene == scene && c.name == "Kasinon baarin tiski (Sohvi)");
            const string AreaName = "Työmaakonttorin kanttiini";
            foreach (var n in new[] { "Työmaakonttorin kanttiini", "Alue: " + AreaName, "Työmaakonttorin kanttiinin ovet", "Kanttiinin pöydät", "Työmaakonttorin toimisto", "Alue: Työmaakonttorin toimisto" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);

            float H = 2f * CamHalf, cy = CamY, top = cy + CamHalf, X0 = VegasCanteenX0;
            float ppu = 1844f / H, imgW = 5120f / ppu, W = 1672f / (941f / H), totalW = 2f * imgW;
            foreach (var f in files) AssetDatabase.ImportAsset(T + f + ".png", ImportAssetOptions.ForceUpdate);

            var root = new GameObject(AreaName);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, AreaName);
            for (int i = 0; i < 2; i++)
            {
                var sp = ImportShipSprite(T + "vegas_kanttiini_huone_" + (i + 1) + ".png", ppu, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan tuonti epäonnistui: huone " + (i + 1)); return; }
                var sr = new GameObject("Kanttiini " + (i + 1)).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10000;
                sr.transform.position = new Vector3(X0 + imgW * (i + 0.5f), cy, 0f);
            }

            var area = ReuseArea("Alue: " + AreaName);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(area.gameObject, scene);
            area.areaName = AreaName; area.useSidewalk = false;
            area.maxDepthY = top - 0.60f * H; area.minDepthY = top - 0.985f * H;   // lattia alkaa tiskin etureunasta (~54 %), pieni marginaali
            area.walkMinX = X0 + imgW * 0.04f; area.walkMaxX = X0 + totalW - imgW * 0.03f;
            area.camMinX = X0 + W * 0.5f; area.camMaxX = Mathf.Max(area.camMinX, X0 + totalW - W * 0.5f);
            area.camSize = CamHalf; area.camRiseY = 0f; area.camOffsetY = 0f; area.backScale = 1f; area.depthLimits = new Vector2[0];
            area.music = street.music; area.musicGain = street.musicGain;

            // ikkunat läpinäkyviä: takana yötaivas ja hotellit (kuten kappelissa)
            foreach (var (name, file, order, factor, mirrors) in new[] { ("Taivas", "vegas_taivas", -10100, 0.02f, 0), ("Hotellit", "vegas_hotellit", -10050, 0.25f, 1) })
            {
                var sp = ImportShipSprite(T + file + ".png", ppu, false, SpriteAlignment.Center);
                var sr = new GameObject(AreaName + ": " + name).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = order;
                sr.transform.position = new Vector3(area.camMinX - W * 0.5f + sp.bounds.size.x * 0.5f, cy + (file == "vegas_taivas" ? 0f : 0.325f * H), 0f);
                var pl = sr.gameObject.AddComponent<ParallaxLayer>();
                pl.factor = factor; pl.mirrorCopies = mirrors; pl.startCamX = area.camMinX; pl.minX = area.camMinX - 2f; pl.maxX = area.camMaxX + 2f;
            }

            // ovet: katu <-> kanttiini (konttorin pariovi = kuvan x 1960 px 10240 px:stä; kanttiinin vasen kaksoisovi ~ 6,7 % kuvasta)
            float officeX0 = site.siteMaxX, streetX = officeX0 + 1960f / ppu, inX = X0 + imgW * 0.0675f;
            var doorRoot = new GameObject("Työmaakonttorin kanttiinin ovet");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, scene);
            Undo.RegisterCreatedObjectUndo(doorRoot, "Kanttiinin ovet");
            Door MakeDoor(string name, string prompt, Area here, Area target, float x, float y, Vector2 spawn)
            {
                var d = new GameObject(name).AddComponent<Door>();
                d.transform.SetParent(doorRoot.transform, false); d.transform.position = new Vector3(x, y, 0f);
                d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = spawn;
                d.halfWidth = 1.8f; d.maxDistanceFromWall = 2.4f; d.onGround = true; d.groundHalfDepth = 1.6f;
                return d;
            }
            float inY = Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.12f);
            MakeDoor("Vegas: kanttiinin ovi sisään", "Mene kanttiiniin", street, area, streetX, site.streetMax - 0.3f, new Vector2(inX + 2.2f, inY));
            MakeDoor("Vegas: kanttiinin ovi ulos", "Mene ulos kadulle", area, street, inX, area.maxDepthY - 0.3f, new Vector2(streetX, site.streetMax - 0.6f));

            // tiskin etuosa (kuvista leikatut kaistat) Sohvin edessä + Sohvi ja kauppa
            string sohviInfo = "Sohvia ei lisätty (aja valikko 376: kasinon baarin Sohvi ja tiski ovat mallit).";
            float stripCx1 = X0 + (2151f + 2969f * 0.5f - 2560f) / ppu + imgW * 0.5f, stripCx2 = X0 + imgW + (1895f * 0.5f - 2560f) / ppu + imgW * 0.5f;   // kaistojen keskipisteet (kuvan keskipiste = 2560 px)
            float stripCy = cy + (922f - 880f) / ppu;   // kaistan keskirivi 880 px (748–1012)
            foreach (var (nm, file, cx) in new[] { ("Tiskin etu 1", "vegas_kanttiini_tiski_1", stripCx1), ("Tiskin etu 2", "vegas_kanttiini_tiski_2", stripCx2) })
            {
                var sp = ImportShipSprite(T + file + ".png", ppu, false, SpriteAlignment.Center);
                var sr = new GameObject(nm).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -9980;   // Sohvin (-9990) edessä, hahmojen takana
                sr.transform.position = new Vector3(cx, stripCy, 0f);
            }
            if (sohviT != null && counterT != null)
            {
                float hx = X0 + 4650f / ppu, hy = cy + (922f - 770f) / ppu;   // jalat hieman tiskin yläreunan (748 px) alapuolella: etukaista peittää nilkat
                var sg = Object.Instantiate(sohviT.gameObject); sg.name = "Sohvi (kanttiini)";
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sg, scene); sg.transform.SetParent(root.transform, false);
                sg.transform.position = new Vector3(hx, hy, 0f); sg.transform.localScale = Vector3.one * 1.35f;
                var rS = sg.GetComponentInChildren<SpriteRenderer>(); if (rS != null) rS.sortingOrder = -9990;
                var shopGo = Object.Instantiate(counterT.gameObject); shopGo.name = "Kanttiinin tiski (Sohvi)";
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(shopGo, scene); shopGo.transform.SetParent(root.transform, false);
                shopGo.transform.position = new Vector3(hx, area.maxDepthY, 0f);
                var sc = shopGo.GetComponent<ShopCounter>();
                sc.here = area; sc.title = "TYÖMAAN KANTTIINI"; sc.npcName = "Sohvi"; sc.prompt = "Puhu Sohville";
                sc.halfWidth = 3.6f; sc.maxDistanceFromWall = 1.5f; sc.ownItemsOnly = false;
                sc.items = new[]
                {
                    new ShopCounter.Item { name = "Makkara-bagel", price = 7, heal = 55, stamina = 0, comment = "Kuumaa makkaraa pehmeässä bagelissa, raksamiesten suosikki." },
                    new ShopCounter.Item { name = "Kahvi", price = 4, heal = 0, stamina = 60, comment = "Kuumaa ja vahvaa." },
                    new ShopCounter.Item { name = "Limsa", price = 3, heal = 15, stamina = 25, comment = "Kylmää ja kuplivaa." },
                };
                sohviInfo = "Sohvi seisoo tiskin takana (x " + hx.ToString("0.0") + ") ja myy makkara-bageleja, kahvia ja limsaa.";
            }

            // pöydät: kaksi riviä kuudessa sarakkeessa, kullakin kaksi makkara-annosta (koristeita, kuten vankilan ruokalassa)
            var tbl = LoadSprites("poyta").OrderBy(sp => int.TryParse(sp.name.Substring(sp.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
            string tableInfo = "pöytiä ei lisätty (poyta.png: " + tbl.Length + "/7 kuvaa)";
            if (tbl.Length >= 7)
            {
                const string plateP = "Assets/Resources/Makkara/annos.png";
                var plateImp = AssetImporter.GetAtPath(plateP) as TextureImporter;
                if (plateImp != null)
                {
                    var st = new TextureImporterSettings(); plateImp.ReadTextureSettings(st);
                    if (st.spriteAlignment != (int)SpriteAlignment.BottomCenter) { st.spriteAlignment = (int)SpriteAlignment.BottomCenter; plateImp.SetTextureSettings(st); plateImp.SaveAndReimport(); }
                }
                var plateSnd = LoadClips("Assets/Audio/sfx", "posliini");
                var tRoot = new GameObject("Kanttiinin pöydät");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(tRoot, scene);
                Undo.RegisterCreatedObjectUndo(tRoot, "Kanttiinin pöydät");
                const int cols = 6; const float f0 = 0.30f, f1 = 1.78f, tableScale = 1.1f, plateScale = 0.7f;
                float colW = (f1 - f0) / cols; int tables = 0;
                foreach (var d in new[] { 0.22f, 0.78f })
                    for (int col = 0; col < cols; col++)
                    {
                        float x = X0 + (f0 + (col + 0.5f) * colW) * imgW;
                        float yMax = area.MaxDepthAtX(x, area.maxDepthY), yMin = area.MinDepthAtX(x, area.minDepthY);
                        float y = Mathf.Lerp(yMax, yMin, d);
                        var t = new GameObject("Pöytä");
                        t.transform.SetParent(tRoot.transform, false); t.transform.position = new Vector3(x, y, 0f);
                        var vis = new GameObject("Visual").AddComponent<SpriteRenderer>(); vis.transform.SetParent(t.transform, false);
                        var sh = new GameObject("Shadow").AddComponent<SpriteRenderer>(); sh.transform.SetParent(t.transform, false);
                        sh.color = new Color(0f, 0f, 0f, 0.35f);
                        var c = t.AddComponent<Crate>();
                        c.body = vis; c.shadow = sh;
                        c.sprites = new[] { tbl[0], tbl[1], tbl[2] };
                        c.breakSprites = new[] { tbl[3], tbl[4], tbl[5], tbl[6] };
                        c.breakFrameTime = 0.1f; c.hitsToBreak = 3; c.breakable = true; c.footOffset = 0.04f;
                        c.visualScale = tableScale; c.shadowWidth = 2.6f * tableScale / 1.1f;
                        c.carryLower = 0.86f * tableScale; c.plowThrough = true; c.hitRadiusX = 1.4f * tableScale; c.debrisTime = 6f;
                        c.moneyChance = 0.3f; c.energyChance = 0f; c.foodChance = 0f; c.thermosChance = 0f; c.throwDamage = 22;
                        vis.sprite = tbl[0];
                        foreach (var side in new[] { -0.62f, 0.62f })
                        {
                            var p = new GameObject("Makkara-annos");
                            p.transform.SetParent(tRoot.transform, false); p.transform.position = t.transform.position;
                            var fp = p.AddComponent<FishPlate>();
                            fp.spriteSet = "Makkara"; fp.countMul = 0.35f; fp.scale = plateScale;
                            fp.table = c; fp.tableX = side * tableScale; fp.tableTop = 1.28f * tableScale;
                            fp.breakSounds = plateSnd;
                        }
                        tables++;
                    }
                tableInfo = tables + " pöytää, jokaisella 2 makkara-annosta";
            }

            // ---- yläkerta: Nokkamiehen toimisto (kaksi kuvaa), portaat kanttiinista ylös ja rappukäytävästä alas ----
            const string UpName = "Työmaakonttorin toimisto";
            float UX0 = VegasOfficeUpX0;
            var upRoot = new GameObject(UpName);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(upRoot, scene);
            Undo.RegisterCreatedObjectUndo(upRoot, UpName);
            for (int i = 0; i < 2; i++)
            {
                var sp = ImportShipSprite(T + "vegas_ylakerta_" + (i + 1) + ".png", ppu, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan tuonti epäonnistui: yläkerta " + (i + 1)); return; }
                var sr = new GameObject("Toimisto " + (i + 1)).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(upRoot.transform, false); sr.sprite = sp; sr.sortingOrder = -10000;
                sr.transform.position = new Vector3(UX0 + imgW * (i + 0.5f), cy, 0f);
            }
            var up = ReuseArea("Alue: " + UpName);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(up.gameObject, scene);
            up.areaName = UpName; up.useSidewalk = false;
            up.maxDepthY = top - 0.60f * H; up.minDepthY = top - 0.985f * H;
            up.walkMinX = UX0 + imgW * 0.04f; up.walkMaxX = UX0 + totalW - imgW * 0.03f;
            up.camMinX = UX0 + W * 0.5f; up.camMaxX = Mathf.Max(up.camMinX, UX0 + totalW - W * 0.5f);
            up.camSize = CamHalf; up.camRiseY = 0f; up.camOffsetY = 0f; up.backScale = 1f; up.depthLimits = new Vector2[0];
            up.music = area.music; up.musicGain = area.musicGain;
            foreach (var (name, file, order, factor, mirrors) in new[] { ("Taivas", "vegas_taivas", -10100, 0.02f, 0), ("Hotellit", "vegas_hotellit", -10050, 0.25f, 1) })
            {
                var sp = ImportShipSprite(T + file + ".png", ppu, false, SpriteAlignment.Center);
                var sr = new GameObject(UpName + ": " + name).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(upRoot.transform, false); sr.sprite = sp; sr.sortingOrder = order;
                sr.transform.position = new Vector3(up.camMinX - W * 0.5f + sp.bounds.size.x * 0.5f, cy + (file == "vegas_taivas" ? 0f : 0.325f * H), 0f);
                var pl = sr.gameObject.AddComponent<ParallaxLayer>();
                pl.factor = factor; pl.mirrorCopies = mirrors; pl.startCamX = up.camMinX; pl.minX = up.camMinX - 2f; pl.maxX = up.camMaxX + 2f;
            }
            // portaat: kanttiinin oikean kuvan portaat (~89,5 %) <-> toimiston rappukäytävä oikean kuvan oikeassa reunassa (~89 %)
            float stairLowX = X0 + imgW * (1f + 0.895f), stairUpX = UX0 + imgW * (1f + 0.89f);
            float midLow = Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.35f), midUp = Mathf.Lerp(up.maxDepthY, up.minDepthY, 0.35f);
            MakeDoor("Kanttiini: portaat ylös", "Mene yläkertaan", area, up, stairLowX, area.maxDepthY - 0.3f, new Vector2(stairUpX - 3.4f, midUp));
            MakeDoor("Toimisto: portaat alas", "Mene alakertaan", up, area, stairUpX, up.maxDepthY - 0.3f, new Vector2(stairLowX - 3.4f, midLow));
            EditorUtility.SetDirty(up);

            var areasPath = VegasScenePath.Replace(".unity", ".areas.txt");
            var names = File.Exists(areasPath) ? File.ReadAllLines(areasPath).ToList() : new System.Collections.Generic.List<string>();
            if (!names.Contains(AreaName)) names.Add(AreaName);
            if (!names.Contains(UpName)) names.Add(UpName);
            File.WriteAllLines(areasPath, names);
            EditorUtility.SetDirty(area);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Työmaakonttorin kanttiini rakennettu (x " + X0.ToString("0") + "–" + (X0 + totalW).ToString("0") + "): ovi kadulta konttorin pariovesta ja takaisin, " + tableInfo + ". " + sohviInfo + " Portaat vievät yläkertaan (Työmaakonttorin toimisto, x " + UX0.ToString("0") + "), jossa rappukäytävä vie takaisin alas. Nokkamies puuttuu vielä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    /// <summary>Työmaakonttorin kanttiini (valikko 394).</summary>
    public static void RunVegasCanteenBatch()
    {
        batch = true;
        try { Step("394 kanttiini", AddVegasCanteen); }
        finally { batch = false; }
    }
}
