using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Natsilinnake: halli, ovet ja pöydät omana vaiheenaan (valikko 340), jotta koko muuria ei tarvitse rakentaa joka kerta. Valikko 327 kutsuu samaa koodia (BuildFortressHall).</summary>
public static partial class BeatEmUpSetup
{
    class FortressCtx
    {
        public UnityEngine.SceneManagement.Scene jungle, alp, ucco;
        public float ppu, H, W, bottom, top, cy;
        public string T;
        public Area surf;
        public System.Func<string, GameObject> NewRoot;
        public System.Func<Transform, string, Sprite, Vector3, int, SpriteRenderer> Put;
        public System.Func<string, string, float, float, float, Area> MakeArea;
        public System.Func<Transform, Area, string, string, float, int, float, SpriteRenderer> Para;
        public System.Func<string, string, Area, Area, Vector3, Vector2, Door> MakeDoor;
    }

    static void BuildFortressHall(FortressCtx fc)
    {
        var jungle = fc.jungle; var alp = fc.alp; var ucco = fc.ucco; float ppu = fc.ppu, H = fc.H, W = fc.W, bottom = fc.bottom, top = fc.top, cy = fc.cy; string T = fc.T; var surf = fc.surf;
        var NewRoot = fc.NewRoot; var Put = fc.Put; var MakeArea = fc.MakeArea; var Para = fc.Para; var MakeDoor = fc.MakeDoor;
            // ---- linnakkeen juhlahalli (oktoberfest): viisi kuvaa (1672 px) peräkkäin; vasemmalla ovi, tiski, lava, kaarikäytävä ja ikkunat, oikealla tutka- ja karttahuone (ikkunat läpinäkyviä: taakse avaruus) ----
            const float HallWallPx = 700f, HallDoorPx = 235f, SurfFortDoorPx = 640f;
            var hallRoot = NewRoot("Muurin juhlahalli");
            // sali: kuvat alkuperäisessä tarkkuudessa (1 kuvapikseli = 1 ruutupikseli), kamera kauempana (HallCamK): hahmot, pöydät ja esineet näyttävät pienemmiltä ja lattiaa on enemmän
            float K = HallCamK, Zs = HallZoom, Zw = Zs * K;   // Zw = kuvan skaala maailmayksiköissä (kuva tuodaan ppu:lla)
            float Ch = CamHalf * K, ppuH = 941f / (2f * Ch), topH = CamY + Ch, bottomH = CamY - Ch;
            float hzShift = HallFloorRow * Zs - HallWallPx;   // kuvan nosto (px); 0, kun lattiaraja pysyy rivillä 700
            float hzDy = (hzShift - 941f * 0.5f * (Zs - 1f)) / ppuH;
            SpriteRenderer HallImage(string name, string file, float leftPx)
            {
                var sp = ImportShipSprite(T + file + ".png", ppu, false, SpriteAlignment.Center);
                if (sp == null) { Debug.LogWarning("Kiinan muuri: kuvan tuonti epäonnistui: " + file); return null; }
                var sr = Put(hallRoot.transform, name, sp, new Vector3(WallHallX0 + leftPx * Zw / ppu + sp.bounds.size.x * Zw * 0.5f, cy + hzDy, 0f), -10000);
                sr.transform.localScale = new Vector3(Zw, Zw, 1f);
                return sr;
            }
            HallImage("Juhlahalli 1", "muuri_juhlahalli_a", 0f);
            HallImage("Juhlahalli 2", "muuri_juhlahalli_b", 1672f);
            HallImage("Juhlahalli 3", "muuri_juhlahalli_c", 2f * 1672f);
            HallImage("Juhlahalli 4", "muuri_juhlahalli_d", 3f * 1672f);
            var hallImgE = HallImage("Juhlahalli 5 (käytävä)", "muuri_juhlahalli_e", 4f * 1672f);   // oikeassa päässä suuri ovi: johtaa myöhemmin pomohuoneeseen
            float HallPxOrig = 4f * 1672f + (hallImgE != null ? hallImgE.sprite.rect.width : 1672f);
            {   // tiskin etuosa (kuvan oma tiski, px 762–2093 x 614–698) piirretään Sohvin eteen: hän seisoo tiskin takana, jalat piilossa
                var csp2 = ImportShipSprite(T + "muuri_juhlahalli_tiski.png", ppu, false, SpriteAlignment.Center);
                if (csp2 != null)
                {
                    float cw2 = csp2.rect.width, ch2 = csp2.rect.height;
                    float cxp = WallHallX0 + (762f + cw2 * 0.5f) * Zw / ppu;
                    float cyp = topH - (((614f + ch2 * 0.5f) * Zs - hzShift)) / ppuH;
                    var cov = Put(hallRoot.transform, "Tiskin etu (halli)", csp2, new Vector3(cxp, cyp, 0f), Mathf.RoundToInt(-(topH - HallWallPx / ppuH + 0.1f) * 100f));
                    cov.transform.localScale = new Vector3(Zw, Zw, 1f);
                }
            }
            var hall = MakeArea("Alue: Muurin juhlahalli", "Muurin juhlahalli", WallHallX0, HallWallPx, HallPxOrig);
            {   // kamera kauempana: alueen mitat lasketaan kameran koon mukaan (MakeArea laski ne normaalikoolla)
                float widthU = HallPxOrig * Zw / ppu, Wh = 1672f / ppuH;
                hall.camSize = Ch;
                hall.maxDepthY = topH - HallWallPx / ppuH; hall.minDepthY = bottomH + 0.9f;
                hall.walkMinX = WallHallX0 + 100f / HallPpu; hall.walkMaxX = WallHallX0 + widthU - 1.2f;
                hall.camMinX = WallHallX0 + Wh * 0.5f; hall.camMaxX = Mathf.Max(hall.camMinX, WallHallX0 + widthU - Wh * 0.5f);
                var spc = ImportShipSprite(T + "muuri_avaruus.png", ppu, false, SpriteAlignment.Center);   // ikkunoista näkyy avaruus (kameran koon mukaan suurennettuna)
                if (spc != null)
                {
                    var srs = Put(hallRoot.transform, "Ikkunoiden avaruus", spc, new Vector3(hall.camMinX - Wh * 0.5f + spc.bounds.size.x * K * 0.5f, cy, 0f), -10150);
                    srs.transform.localScale = new Vector3(K, K, 1f);
                    var pl = srs.gameObject.AddComponent<ParallaxLayer>();
                    pl.factor = 0.02f; pl.startCamX = hall.camMinX; pl.minX = hall.camMinX - 2f; pl.maxX = hall.camMaxX + 2f;
                }
            }
            MakeDoor("Kuun pinta: linnakkeeseen", "Mene linnakkeeseen", surf, hall, new Vector3(WallSurfX0 + SurfFortDoorPx / ppu, surf.maxDepthY, 0f), new Vector2(WallHallX0 + (HallDoorPx + 130f) / HallPpu, Mathf.Lerp(hall.maxDepthY, hall.minDepthY, 0.3f)));
            MakeDoor("Juhlahalli: ulos", "Ulos kuun pinnalle", hall, surf, new Vector3(WallHallX0 + HallDoorPx / HallPpu, hall.maxDepthY, 0f), new Vector2(WallSurfX0 + (SurfFortDoorPx + 130f) / ppu, Mathf.Lerp(surf.maxDepthY, surf.minDepthY, 0.3f)));


            // pöytämallit (muista sceneistä): annos, pullo, pöytä ja tuoli
            var allCrates = Object.FindObjectsByType<Crate>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var allFish = Object.FindObjectsByType<FishPlate>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var allBottles = Object.FindObjectsByType<Bottle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            // pöytä = sama pöytä, jolla kala-annokset ovat (El Loipparin "Pöytä"); ei wok-kojua, vaikka nuudeliannokset ovat sen päällä
            bool IsTable(Crate cr) => cr != null && cr.name.StartsWith("P" + (char)0xF6 + "yt");
            var fishT = allFish.Where(fp => fp != null && fp.spriteSet == "Kala" && IsTable(fp.table)).OrderBy(fp => fp.gameObject.scene == ucco ? 0 : 1).FirstOrDefault();
            var plateT = fishT != null ? fishT : allFish.FirstOrDefault(fp => fp != null && IsTable(fp.table));
            var tableT = fishT != null ? fishT.table : allCrates.FirstOrDefault(c2 => c2 != null && IsTable(c2) && c2.transform.parent != null && c2.transform.parent.name.StartsWith("El Loipparin"));
            var beerT = tableT != null ? (allBottles.FirstOrDefault(b => b != null && b.table == tableT) ?? allBottles.FirstOrDefault(b => b != null && b.table != null && IsTable(b.table))) : null;
            var chairT = ucco.IsValid() ? Object.FindObjectsByType<Chair>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c2 => c2.gameObject.scene == ucco) : null;
            // ---- juhlahallin pöydät: makkara-annoksia (Resources/Makkara: annos, kala_=makkara, ranska_=hapankaali, sitruuna_=rinkeli, sirpale_=sinappi); hajotessa ainekset lentelevät kuten kala-annoksessa ----
            foreach (string mp in System.IO.Directory.GetFiles("Assets/Resources/Makkara", "*.png"))
            {
                AssetDatabase.ImportAsset(mp, ImportAssetOptions.ForceSynchronousImport);
                var mi = AssetImporter.GetAtPath(mp) as TextureImporter; if (mi == null) continue;
                mi.textureType = TextureImporterType.Sprite; mi.spriteImportMode = SpriteImportMode.Single;
                mi.spritePixelsPerUnit = 100; mi.alphaIsTransparency = true; mi.mipmapEnabled = false;
                mi.textureCompression = TextureImporterCompression.Uncompressed; mi.maxTextureSize = 2048;
                var ms = new TextureImporterSettings(); mi.ReadTextureSettings(ms); ms.spriteAlignment = (int)SpriteAlignment.Center; mi.SetTextureSettings(ms); mi.SaveAndReimport();
            }
            // kaljatuoppi (pullo_tuoppi_olut: ehjä, säröt, sirpaleet, lätty; Tools/MakkaraAssets/make_tuoppi_sheet.py); hajoaa lasiäänillä ja jättää olutläiskän
            Sprite[] mugSprites = null;
            if (File.Exists("Assets/Sprites/Rekvisiitta/pullo_tuoppi_olut.png"))
            {
                SetupAndSlice("Assets/Sprites/Rekvisiitta/pullo_tuoppi_olut.png");
                mugSprites = LoadSprites("pullo_tuoppi_olut").OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int mk) ? mk : 0).ToArray();
                if (mugSprites.Length < 5) mugSprites = null;
            }
            var mugSnd = LoadClips("Assets/Audio/sfx", "glass");
            var hallTableT = tableT;
            if (hallTableT != null)
            {
                var hallProps = new GameObject("Juhlahallin pöydät ja tuolit"); hallProps.transform.SetParent(hallRoot.transform, false);
                var hallPlate = plateT != null ? plateT.gameObject : null;
                var wurstSnd = LoadClips("Assets/Audio/sfx", "posliini");
                int ht = 0;
                foreach (var v in new[] { new Vector2(1500f, 0.8f), new Vector2(2300f, 0.45f), new Vector2(2900f, 0.85f), new Vector2(3700f, 0.5f), new Vector2(4300f, 0.85f), new Vector2(4800f, 0.45f) })
                {
                    float hx2 = WallHallX0 + v.x / HallPpu, hy2 = Mathf.Lerp(hall.maxDepthY - 0.2f, hall.minDepthY + 0.4f, v.y); ht++;
                    var tg = Object.Instantiate(hallTableT.gameObject); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(tg, jungle); tg.transform.SetParent(hallProps.transform, false);
                    tg.name = "Juhlapöytä " + ht; tg.transform.position = new Vector3(hx2, hy2, 0f); tg.SetActive(true);
                    var tc = tg.GetComponent<Crate>(); tc.stackedOn = null;
                    if (hallPlate != null)
                    {
                        var fg = Object.Instantiate(hallPlate); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fg, jungle); fg.transform.SetParent(hallProps.transform, false);
                        fg.name = "Makkara-annos " + ht; fg.transform.position = tg.transform.position; fg.SetActive(true);
                        var fp = fg.GetComponent<FishPlate>(); fp.table = tc; fp.spriteSet = "Makkara"; fp.scale = 0.75f; fp.countMul = 0.6f; fp.breakSounds = wurstSnd; fp.tableX = (ht % 2 == 0 ? 0.25f : -0.25f) * 1.5f;
                        // toinen annos pöydän toiseen päähän
                        var fg2 = Object.Instantiate(fg); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fg2, jungle); fg2.transform.SetParent(hallProps.transform, false);
                        fg2.name = "Makkara-annos " + ht + "b"; var fp2 = fg2.GetComponent<FishPlate>(); fp2.table = tc; fp2.tableX = -fp.tableX * 1.6f;
                        if (beerT != null)
                            foreach (float bx in new[] { -0.3f * (ht % 2 == 0 ? -1f : 1f), 1.2f })
                            {
                                var bg = Object.Instantiate(beerT.gameObject); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(bg, jungle); bg.transform.SetParent(hallProps.transform, false);
                                bg.name = "Tuoppi"; bg.transform.position = tg.transform.position; bg.SetActive(true);
                                var bb = bg.GetComponent<Bottle>(); bb.table = tc; bb.tableX = bx * 1.5f;
                                if (mugSprites != null) { bb.sprites = mugSprites; bb.stainKind = "olut"; bb.pivotY = 0.25f; bb.scale = 0.65f; bb.breakSounds = mugSnd; }
                            }
                    }
                    if (chairT != null)
                    {
                        var cg = Object.Instantiate(chairT.gameObject); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cg, jungle); cg.transform.SetParent(hallProps.transform, false);
                        cg.name = "Juhlatuoli " + ht; cg.transform.position = new Vector3(hx2 + (ht % 2 == 0 ? 1.7f : -1.7f), hy2 - 0.05f, 0f);
                        var vis = cg.GetComponentInChildren<SpriteRenderer>(); if (vis != null) vis.flipX = ht % 2 == 0;
                    }
                }
            }

    }

    [MenuItem("Beat em up/340. Linnake: vain halli, ovet ja pöydät (ei koko muuria)", false, 340)]
    static void BuildFortressOnly()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        foreach (var f in new[] { "muuri_avaruus", "muuri_juhlahalli_a", "muuri_juhlahalli_b", "muuri_juhlahalli_c", "muuri_juhlahalli_d", "muuri_juhlahalli_e", "muuri_juhlahalli_tiski" })
            if (!File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat)."); return; }
        if (!File.Exists(WallScenePath)) { Info("Muuri-scenea ei ole: aja valikko 330 kerran."); return; }
        var opened = new List<UnityEngine.SceneManagement.Scene>();
        UnityEngine.SceneManagement.Scene Open(string path)
        {
            if (!File.Exists(path)) return default;
            var sc = EditorSceneManager.GetSceneByPath(path);
            if (!sc.IsValid() || !sc.isLoaded) { sc = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive); opened.Add(sc); }
            return sc;
        }
        try
        {
            var jungle = Open(WallScenePath);
            var alp = Open(AlpineScenePathNow); var ucco = Open(UccoLaivaScenePath); Open(ThaitownScenePath); Open("Assets/Scenes/SampleScene.unity");
            var surf = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Muurin kuun pinta" && a.gameObject.scene == jungle);
            if (surf == null) { Info("Kuun pintaa ei löytynyt: aja valikko 330 kerran."); return; }
            foreach (var n in new[] { "Muurin juhlahalli", "Alue: Muurin juhlahalli" })
                foreach (var o in jungle.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);
            foreach (var d in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(d => d != null && d.gameObject.scene == jungle && (d.name == "Kuun pinta: linnakkeeseen" || d.name == "Juhlahalli: ulos")).ToArray())
                Undo.DestroyObjectImmediate(d.gameObject);
            float ppu = 941f / (2f * CamHalf), H = 941f / ppu, W = 1672f / ppu;
            float bottom = CamY - CamHalf, top = bottom + H, cy = CamY;
            GameObject NewRoot(string n)
            {
                var g = new GameObject(n);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(g, jungle);
                Undo.RegisterCreatedObjectUndo(g, "Linnake");
                return g;
            }
            SpriteRenderer Put(Transform parent, string name, Sprite sp, Vector3 pos, int order)
            {
                var sr = new GameObject(name).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(parent, false);
                sr.sprite = sp; sr.sortingOrder = order; sr.transform.position = pos;
                return sr;
            }
            Area MakeArea(string goName, string areaName, float x0, float wallPx, float widthPx)
            {
                var a = ReuseArea(goName);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(a.gameObject, jungle);
                a.areaName = areaName; a.useSidewalk = false;
                a.maxDepthY = top - wallPx / ppu; a.minDepthY = bottom + 0.9f;
                a.walkMinX = x0 + 1.5f; a.walkMaxX = x0 + widthPx / ppu - 1.2f;
                a.camMinX = x0 + W * 0.5f; a.camMaxX = Mathf.Max(a.camMinX, x0 + widthPx / ppu - W * 0.5f);
                a.camSize = CamHalf; a.camRiseY = 0f; a.camOffsetY = 0f; a.backScale = 1f; a.depthLimits = new Vector2[0];
                a.music = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Musiikki/Viidakko.mp3"); a.musicGain = a.music != null ? JungleMusicGain : 1f;
                return a;
            }
            SpriteRenderer Para(Transform parent, Area area, string name, string file, float factor, int order, float dy)
            {
                var sp = ImportShipSprite(T + file + ".png", ppu, false, SpriteAlignment.Center);
                if (sp == null) return null;
                float left = area.camMinX - W * 0.5f;
                var sr = Put(parent, name, sp, new Vector3(left + sp.bounds.size.x * 0.5f, cy + dy, 0f), order);
                var pl = sr.gameObject.AddComponent<ParallaxLayer>();
                pl.factor = factor; pl.startCamX = area.camMinX; pl.minX = area.camMinX - 2f; pl.maxX = area.camMaxX + 2f;
                return sr;
            }
            var doorRoot = jungle.GetRootGameObjects().FirstOrDefault(g => g.name == "Muurin ovet") ?? NewRoot("Muurin ovet");
            Door MakeDoor(string name, string prompt, Area here, Area target, Vector3 pos, Vector2 spawn)
            {
                var d = new GameObject(name).AddComponent<Door>();
                d.transform.SetParent(doorRoot.transform, false); d.transform.position = pos;
                d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = spawn; d.halfWidth = 1.6f; d.maxDistanceFromWall = 2.2f;
                return d;
            }
            BuildFortressHall(new FortressCtx { jungle = jungle, alp = alp, ucco = ucco, ppu = ppu, H = H, W = W, bottom = bottom, top = top, cy = cy, T = T, surf = surf, NewRoot = NewRoot, Put = Put, MakeArea = MakeArea, Para = Para, MakeDoor = MakeDoor });
            EditorSceneManager.MarkSceneDirty(jungle); EditorSceneManager.SaveScene(jungle);
            Info("Linnake rakennettu uudelleen (halli, ovet, pöydät) koskematta muuhun muuriin. Aja tarvittaessa 334–337 (gestapot, Nadine, Violetti, vieraat), sillä halli luotiin uudelleen.");
        }
        finally { foreach (var sc in opened) if (sc.IsValid()) EditorSceneManager.CloseScene(sc, true); }
    }
}
