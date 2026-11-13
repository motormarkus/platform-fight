using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Las Vegas (valikko 376): oma scene Assets/Scenes/Areas/Vegas.unity. Ensimmäinen vaihe: alue "Vegasin yötaivas" (kasinon katto) ja kolme parallaksitasoa
/// (vegas_taivas, vegas_hotellit, vegas_palmut; hotellit ja palmut jatkuvat peilikopioina) sekä kopteri laskeutumispaikalla.
/// Katon hissi vie alueelle "Vegasin kasino" (X0 99000; etuala vegas_kasino_etu = pelitaso, keskitaso vegas_kasino_keski 0,4 ja taka vegas_kasino_taka 0,15, vasen jatko-osa uusista kuvista). Kopterin lentokohtaus ja vihut tulevat myöhemmin.</summary>
public static partial class BeatEmUpSetup
{
    const string VegasScenePath = "Assets/Scenes/Areas/Vegas.unity";
    const float VegasX0 = 98000f, VegasCasinoX0 = 99000f;

    [MenuItem("Beat em up/376. Las Vegas: oma scene, parallaksit (taivas, hotellit, palmut) ja kopteri", false, 376)]
    static void AddVegas()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        foreach (var f in new[] { "vegas_taivas", "vegas_hotellit", "vegas_palmut", "vegas_katto_1", "vegas_katto_2", "vegas_kasino_etu", "vegas_kasino_keski", "vegas_kasino_taka", "vegas_kasino_etu_vasen", "vegas_kasino_keski_vasen", "vegas_kasino_taka_vasen", "vegas_kasino_etu_baari", "vegas_kasino_keski_baari", "vegas_kasino_taka_baari", "vegas_kasino_etu_lattia", "vegas_kasino_etu_ovet", "vegas_kasino_matto_ovet", "vegas_kasino_matto_lattia", "vegas_kasino_matto_baari", "vegas_kasino_matto_vasen", "vegas_kasino_matto_oikea" })
        {
            if (!File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat)."); return; }
            AssetDatabase.ImportAsset(T + f + ".png", ImportAssetOptions.ForceUpdate);
        }
        UnityEngine.SceneManagement.Scene scene;
        bool openedHere = false;
        if (File.Exists(VegasScenePath))
        {
            scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
            if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        }
        else
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            openedHere = true;
            if (!EditorSceneManager.SaveScene(scene, VegasScenePath)) { Info("Scenen luonti epäonnistui: " + VegasScenePath); EditorSceneManager.CloseScene(scene, true); return; }
        }
        try
        {
            foreach (var n in new[] { "Vegas", "Vegasin kasino", "Vegasin ovet", "Alue: Vegasin yötaivas", "Alue: Vegasin kasino" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);

            float H = 2f * CamHalf, cy = CamY, top = cy + CamHalf, X0 = VegasX0;
            float W = 1672f / (941f / H);
            float ppuImg = 1844f / H, imgW = 5120f / ppuImg;    // kaikki kuvat 5120 x 1844
            float totalW = 2f * imgW;
            var root = new GameObject("Vegas");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Vegas");

            var area = ReuseArea("Alue: Vegasin yötaivas");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(area.gameObject, scene);
            area.areaName = "Vegasin yötaivas"; area.useSidewalk = false;
            area.maxDepthY = top - 0.745f * H; area.minDepthY = top - 0.985f * H;   // arvaus: katon lattia, säädetään kun katon kuva tulee
            area.walkMinX = X0 + imgW * 0.06f; area.walkMaxX = X0 + imgW * 1.88f;   // oikealla hissirakennus ja seinä
            area.camMinX = X0 + W * 0.5f; area.camMaxX = Mathf.Max(area.camMinX, X0 + totalW - W * 0.5f);
            area.camSize = CamHalf; area.camRiseY = 0f; area.camOffsetY = 0f; area.backScale = 1f; area.depthLimits = new Vector2[0];

            // katto: kaksi kuvaa (1: helikopterikenttä, 2: ilmastointi ja hissi)
            for (int i = 0; i < 2; i++)
            {
                var sp = ImportShipSprite(T + "vegas_katto_" + (i + 1) + ".png", ppuImg, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan tuonti epäonnistui (katto " + (i + 1) + ")."); return; }
                var sr = new GameObject("Katto " + (i + 1)).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10000;
                sr.transform.position = new Vector3(X0 + imgW * (i + 0.5f), cy, 0f);
            }

            // taso: nimi, kuva, järjestys, kerroin, peilikopioita (0 = ei)
            var layers = new[]
            {
                ("Taivas", "vegas_taivas", -10100, 0.02f, 0),
                ("Hotellit", "vegas_hotellit", -10050, 0.25f, 1),
                ("Palmut", "vegas_palmut", -10040, 0.5f, 1),
            };
            float left = area.camMinX - W * 0.5f;
            foreach (var (name, file, order, factor, mirrors) in layers)
            {
                var sp = ImportShipSprite(T + file + ".png", ppuImg, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan tuonti epäonnistui: " + file); return; }
                var sr = new GameObject("Vegas: " + name).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = order;
                sr.transform.position = new Vector3(left + sp.bounds.size.x * 0.5f, cy, 0f);
                var pl = sr.gameObject.AddComponent<ParallaxLayer>();
                pl.factor = factor; pl.mirrorCopies = mirrors;
                pl.startCamX = area.camMinX; pl.minX = area.camMinX - 2f; pl.maxX = area.camMaxX + 2f;
            }

            // kopteri laskeutumispaikalla (sama roottorisilmukka kuin vankilan katolla); paikka ja koko ovat arvauksia
            {
                const string dir = "Assets/Sprites/Rekvisiitta/KatonKopteri/";
                var frames = new List<Sprite>();
                for (int i = 0; ; i++)
                {
                    string fp = $"{dir}katon_kopteri_{i:00}.png";
                    if (!File.Exists(fp)) break;
                    var fs = ImportShipSprite(fp, 720f / RoofHeliWidth, false, SpriteAlignment.BottomCenter);
                    if (fs != null) frames.Add(fs);
                }
                if (frames.Count > 0)
                {
                    float hx = X0 + imgW * 0.385f, hy = Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.5f);   // H-merkki ~38,5 % kuvan 1 leveydestä
                    var heli = new GameObject("Vegasin kopteri");
                    heli.transform.SetParent(root.transform, false);
                    heli.transform.position = new Vector3(hx, hy, 0f);
                    heli.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
                    var hull = new GameObject("Runko").AddComponent<SpriteRenderer>();
                    hull.transform.SetParent(heli.transform, false);
                    hull.sprite = frames[0]; hull.sortingOrder = Mathf.RoundToInt(-hy * 100f);
                    var loop = hull.gameObject.AddComponent<RotorLoop>();
                    loop.frames = frames.ToArray(); loop.fps = 24f; loop.reverse = false;
                }
            }

            // ---- kasino (hissin takana): etuala (pelitaso), keskitaso ja taka-seinä parallakseina; kaikki peilataan jatkoksi ----
            float cX0 = VegasCasinoX0;
            // vasen jatko-osa (uudet kuvat, korkeus sovitetaan ruudun korkeuteen) + alkuperäinen osa oikealla (hissi)
            var leftTex = AssetDatabase.LoadAssetAtPath<Texture2D>(T + "vegas_kasino_etu_vasen.png");
            float leftW = leftTex != null ? leftTex.width / (leftTex.height / H) : imgW;
            var barTex = AssetDatabase.LoadAssetAtPath<Texture2D>(T + "vegas_kasino_etu_baari.png");
            float barW = barTex != null ? barTex.width / (barTex.height / H) : imgW;     // uloin vasen osa: baari (tiski)
            float lead = 2f * imgW;                                                         // vasemmassa päässä kaksi lisäpalaa: ovet (uloin) ja avoin lattia
            float barX = cX0 + lead;
            float casTotalW = lead + barW + leftW + imgW;
            float leftX = barX + barW, rightX = leftX + leftW;                              // keskiosan ja oikean osan vasen reuna
            float extra = 0.4f * H;                                                         // lattiaa lisää alaspäin: kamera seuraa pelaajaa pystysuunnassa
            var casino = ReuseArea("Alue: Vegasin kasino");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(casino.gameObject, scene);
            casino.areaName = "Vegasin kasino"; casino.useSidewalk = false;
            casino.maxDepthY = top - 0.67f * H; casino.minDepthY = top - 0.985f * H - extra;   // matto alkaa 0,65 korkeudesta (mitattu)
            casino.walkMinX = cX0 + imgW * 0.03f; casino.walkMaxX = rightX + imgW * 0.94f;
            casino.camMinX = cX0 + W * 0.5f; casino.camMaxX = Mathf.Max(casino.camMinX, cX0 + casTotalW - W * 0.5f);
            casino.camSize = CamHalf; casino.camRiseY = extra; casino.camOffsetY = -extra; casino.backScale = 1f; casino.depthLimits = new Vector2[0];   // kamera: edessä alhaalla (extra), takaseinällä normaalikorkeudella
            var croot = new GameObject("Vegasin kasino");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(croot, scene);
            Undo.RegisterCreatedObjectUndo(croot, "Vegasin kasino");
            {
                float Ppu(string f) { var t = AssetDatabase.LoadAssetAtPath<Texture2D>(T + f + ".png"); return t.height / H; }
                var frontL = ImportShipSprite(T + "vegas_kasino_etu_vasen.png", Ppu("vegas_kasino_etu_vasen"), false, SpriteAlignment.Center);
                var front = ImportShipSprite(T + "vegas_kasino_etu.png", ppuImg, false, SpriteAlignment.Center);
                if (front == null || frontL == null) { Info("Kuvan tuonti epäonnistui: vegas_kasino_etu(_vasen)"); return; }
                // pelitaso: vasen jatko-osa ja oikealla alkuperäinen (hissi oikeassa päässä = tulopaikka katolta)
                var frontB = ImportShipSprite(T + "vegas_kasino_etu_baari.png", Ppu("vegas_kasino_etu_baari"), false, SpriteAlignment.Center);
                if (frontB == null) { Info("Kuvan tuonti epäonnistui: vegas_kasino_etu_baari"); return; }
                var sB = new GameObject("Kasino etuala (baari)").AddComponent<SpriteRenderer>();
                sB.transform.SetParent(croot.transform, false); sB.sprite = frontB; sB.sortingOrder = -10000;
                sB.transform.position = new Vector3(barX + barW * 0.5f, cy, 0f);
                // kaksi lisäpalaa baarin vasemmalle puolelle: avoin lattia (baarin vieressä) ja uloimpana ovet
                foreach (var (pn, pf, px) in new[] { ("Kasino etuala (lattia)", "vegas_kasino_etu_lattia", cX0 + imgW * 1.5f), ("Kasino etuala (ovet)", "vegas_kasino_etu_ovet", cX0 + imgW * 0.5f) })
                {
                    var psp = ImportShipSprite(T + pf + ".png", ppuImg, false, SpriteAlignment.Center);
                    if (psp == null) { Info("Kuvan tuonti epäonnistui: " + pf); return; }
                    var psr = new GameObject(pn).AddComponent<SpriteRenderer>();
                    psr.transform.SetParent(croot.transform, false); psr.sprite = psp; psr.sortingOrder = -10000;
                    psr.transform.position = new Vector3(px, cy, 0f);
                }
                // matto alas: jokaisen palan alareunan rivit venytettynä kuvan alle (kamera laskee lattian mukana); 0,6 ruudun korkeutta
                {
                    var mattoPieces = new[] { ("ovet", cX0 + imgW * 0.5f), ("lattia", cX0 + imgW * 1.5f), ("baari", barX + barW * 0.5f), ("vasen", leftX + leftW * 0.5f), ("oikea", rightX + imgW * 0.5f) };
                    foreach (var (mn, mx) in mattoPieces)
                    {
                        var msp = ImportShipSprite(T + "vegas_kasino_matto_" + mn + ".png", ppuImg, false, SpriteAlignment.Center);
                        if (msp == null) continue;
                        var msr = new GameObject("Kasino matto alas (" + mn + ")").AddComponent<SpriteRenderer>();
                        msr.transform.SetParent(croot.transform, false); msr.sprite = msp; msr.sortingOrder = -10005;
                        msr.transform.position = new Vector3(mx, cy - H * 0.5f - msp.bounds.size.y * 0.5f, 0f);
                    }
                }
                var sL = new GameObject("Kasino etuala (vasen)").AddComponent<SpriteRenderer>();
                sL.transform.SetParent(croot.transform, false); sL.sprite = frontL; sL.sortingOrder = -10000;
                sL.transform.position = new Vector3(leftX + leftW * 0.5f, cy, 0f);
                var sR = new GameObject("Kasino etuala").AddComponent<SpriteRenderer>();
                sR.transform.SetParent(croot.transform, false); sR.sprite = front; sR.sortingOrder = -10000;
                sR.transform.position = new Vector3(rightX + imgW * 0.5f, cy, 0f);
                var cLayers = new[] { ("Kasino taka", "vegas_kasino_taka", -10100, 0.2f), ("Kasino keskitaso", "vegas_kasino_keski", -10050, 0.5f) };
                foreach (var (name, file, order, factor) in cLayers)
                {
                    var spB = ImportShipSprite(T + file + "_baari.png", Ppu(file + "_baari"), false, SpriteAlignment.Center);
                    var spL = ImportShipSprite(T + file + "_vasen.png", Ppu(file + "_vasen"), false, SpriteAlignment.Center);
                    var sp = ImportShipSprite(T + file + ".png", ppuImg, false, SpriteAlignment.Center);
                    if (sp == null || spL == null || spB == null) { Info("Kuvan tuonti epäonnistui: " + file); return; }
                    var sr = new GameObject(name).AddComponent<SpriteRenderer>();
                    sr.transform.SetParent(croot.transform, false); sr.sprite = spB; sr.sortingOrder = order;
                    sr.transform.position = new Vector3(casino.camMinX - W * 0.5f + spB.bounds.size.x * 0.5f, cy, 0f);
                    var pl = sr.gameObject.AddComponent<ParallaxLayer>();
                    pl.factor = factor; pl.extraSprites = new[] { spL }; pl.mirrorEnd = file == "vegas_kasino_keski";   // keskitason loppuun peilattu kopio: riittää kahdelle lisäpalalle etualalla   // baari + baccarat/ruletti (3. kuva, hissipala, ei ehdi näkyä kertoimilla 0,2 / 0,5)
                    pl.startCamX = casino.camMinX; pl.minX = casino.camMinX - 2f; pl.maxX = casino.camMaxX + 2f;
                }
            }

            // Sohvi baaritiskin takana (kuvan baari-osa vasemmassa päässä) myymässä; mallit kopioidaan Alppikylän afterski-baarista (valikko 279)
            string sohviInfo;
            {
                const string alpPath = "Assets/Scenes/Areas/Alppikyla.unity";
                var alpOpened = false; UnityEngine.SceneManagement.Scene alp = default;
                try
                {
                    if (File.Exists(alpPath))
                    {
                        alp = EditorSceneManager.GetSceneByPath(alpPath);
                        if (!alp.IsValid() || !alp.isLoaded) { alp = EditorSceneManager.OpenScene(alpPath, OpenSceneMode.Additive); alpOpened = true; }
                    }
                    var sohviT = alp.IsValid() ? Object.FindObjectsByType<Dancer>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c.gameObject.scene == alp && c.name == "Sohvi (afterski-baari)") : null;
                    var counterT = alp.IsValid() ? Object.FindObjectsByType<ShopCounter>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c.gameObject.scene == alp && c.name == "Afterski-baarin tiski (Sohvi)") : null;
                    if (sohviT == null || counterT == null) sohviInfo = "Sohvia ei lisätty: aja valikko 279 (Alppikylän afterski-baari), sen Sohvi ja tiski ovat mallit, ja sitten 376 uudelleen.";
                    else
                    {
                        float hx = barX + barW * 0.73f, hy = top - 0.46f * H;   // tiskin pinta ~44-47 % korkeudesta, vapaa kohta tarjottimen oikealla puolella (tiski päättyy ~77 %)
                        var sg = Object.Instantiate(sohviT.gameObject); sg.name = "Sohvi (kasinon baari)";
                        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sg, scene);
                        sg.transform.SetParent(croot.transform, false);
                        sg.transform.position = new Vector3(hx, hy, 0f);
                        sg.transform.localScale = Vector3.one * 1.25f;
                        foreach (var r in sg.GetComponentsInChildren<SpriteRenderer>(true)) r.sortingOrder = -10001;   // pelitason (tiski) takana, parallaksien edessä
                        var shopGo = Object.Instantiate(counterT.gameObject); shopGo.name = "Kasinon baarin tiski (Sohvi)";
                        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(shopGo, scene);
                        shopGo.transform.SetParent(croot.transform, false);
                        shopGo.transform.position = new Vector3(hx, casino.maxDepthY, 0f);
                        var sc = shopGo.GetComponent<ShopCounter>();
                        sc.here = casino; sc.title = "KASINON BAARI"; sc.npcName = "Sohvi"; sc.prompt = "Puhu Sohville";
                        sc.halfWidth = 3.4f; sc.maxDistanceFromWall = 1.4f; sc.ownItemsOnly = false;
                        sc.items = new[]
                        {
                            new ShopCounter.Item { name = "Kahvi", price = 4, heal = 0, stamina = 60, comment = "Kuumaa ja vahvaa." },
                            new ShopCounter.Item { name = "Voileipä", price = 6, heal = 45, stamina = 0, comment = "Tuore ja täyttävä." },
                            new ShopCounter.Item { name = "Cocktail", price = 8, heal = 30, stamina = 50, comment = "Hedelmäinen ja jäinen." },
                            new ShopCounter.Item { name = "Limsa", price = 3, heal = 15, stamina = 25, comment = "Kylmää ja kuplivaa." },
                        };
                        sohviInfo = "Sohvi seisoo baaritiskin takana (x " + hx.ToString("0.0") + ") ja myy kahvia, voileipiä, cocktaileja ja limsaa.";
                    }
                }
                finally { if (alpOpened && alp.IsValid()) EditorSceneManager.CloseScene(alp, true); }
            }

            // ovet: katon hissi <-> kasinon oikea hissi (tulopaikka). Kasino jatkuu vasemmalle (uudet kuvat), vasemman pään kohde päätetään myöhemmin.
            var doorRoot = new GameObject("Vegasin ovet");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, scene);
            Undo.RegisterCreatedObjectUndo(doorRoot, "Vegasin ovet");
            {
                float roofElevX = X0 + imgW * 1.80f, casElevX = rightX + imgW * 0.885f;
                float roofY = Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.45f), casY = Mathf.Lerp(casino.maxDepthY, casino.minDepthY, 0.45f);
                var down = new GameObject("Vegas: katon hissi").AddComponent<Door>();
                down.transform.SetParent(doorRoot.transform, false); down.transform.position = new Vector3(roofElevX, area.maxDepthY - 0.3f, 0f);
                down.prompt = "Mene hissillä kasinolle"; down.here = area; down.target = casino; down.spawnPoint = new Vector2(casElevX - 2.6f, casY);
                down.halfWidth = 1.6f; down.maxDistanceFromWall = 2.4f; down.onGround = true; down.groundHalfDepth = 1.6f;
                var up = new GameObject("Vegas: kasinon hissi").AddComponent<Door>();
                up.transform.SetParent(doorRoot.transform, false); up.transform.position = new Vector3(casElevX, casino.maxDepthY - 0.3f, 0f);
                up.prompt = "Mene hissillä katolle"; up.here = casino; up.target = area; up.spawnPoint = new Vector2(roofElevX - 2.6f, roofY);
                up.halfWidth = 1.6f; up.maxDistanceFromWall = 2.4f; up.onGround = true; up.groundHalfDepth = 1.6f;
            }
            EditorUtility.SetDirty(casino);

            File.WriteAllLines(VegasScenePath.Replace(".unity", ".areas.txt"), new[] { "Vegasin yötaivas", "Vegasin kasino" });
            EditorUtility.SetDirty(area);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            var list = EditorBuildSettings.scenes.ToList();
            if (!list.Any(s => s.path == VegasScenePath)) { list.Add(new EditorBuildSettingsScene(VegasScenePath, true)); EditorBuildSettings.scenes = list.ToArray(); }
            Info("Vegas rakennettu omaan sceneensä (" + VegasScenePath + ", x " + X0.ToString("0") + "–" + (X0 + totalW).ToString("0") + "): alue \"Vegasin yötaivas\", parallaksit taivas 0,02 / hotellit 0,25 / palmut 0,5 (hotellit ja palmut peilikopioilla) ja kopteri.\n" +
                 sohviInfo + "\n" + "Katon hissi vie alueelle \"Vegasin kasino\" (etuala, keskitaso ja taka-seinä parallakseina, vasen jatko-osa uusista kuvista) ja takaisin; kasino jatkuu vasemmalle baarin, avoimen lattian ja ovien (x ~ " + (cX0 + imgW * 0.155f).ToString("0") + ") luo: ovella ei ole vielä kohdetta (päätetään myöhemmin). Lattiaa on lisätty alaspäin (kamera seuraa pelaajaa pystysuunnassa, matto venytetty kuvan alareunasta). Puuttuu: kasinon vihut, pöydät/pokeri, kopterin lentokohtaus ja yhteys vankilan katolta. Lattia (0,745–0,985) ja kopterin paikka ovat arvauksia. Ajamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
