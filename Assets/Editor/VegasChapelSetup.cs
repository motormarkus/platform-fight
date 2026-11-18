using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Vegasin hääkappeli ja panttilainaamo sisältä (valikko 389). Kadun kappeli on osa valikkoa 378 (kasinon oikealla puolella).
/// Kappeli: x 109000, kaksi kuvaa (vegas_kappeli_sisa_1: ovi vasemmalla + ikkunat, vegas_kappeli_sisa_2: alttari). Panttilainaamo: x 110000, kaksi kuvaa (vegas_pantti_sisa_1: ovi vasemmalla, vegas_pantti_sisa_2: tiski + oikea pääty).
/// Ovet: kadun kappelin ja panttilainaamon ovet <-> sisäovet. Aja valikot 376 ja 378 ensin.</summary>
public static partial class BeatEmUpSetup
{
    const float VegasChapelX0 = 109000f, VegasPawnX0 = 110000f;

    [MenuItem("Beat em up/389. Las Vegas: hääkappeli ja panttilainaamo (sisätilat ja ovet)", false, 389)]
    static void AddVegasChapelPawn()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        var chapelFiles = new[] { "vegas_kappeli_sisa_1", "vegas_kappeli_sisa_2" };
        var pawnFiles = new[] { "vegas_pantti_sisa_1", "vegas_pantti_sisa_2" };
        foreach (var f in chapelFiles.Concat(pawnFiles).Concat(new[] { "vegas_taivas", "vegas_hotellit" }))
        {
            if (!File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat)."); return; }
            AssetDatabase.ImportAsset(T + f + ".png", ImportAssetOptions.ForceUpdate);
        }
        if (!File.Exists(VegasScenePath)) { Info("Aja ensin valikko 376 (Vegas)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var street = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.gameObject.scene == scene && a.areaName == "Vegasin katu");
            if (street == null) { Info("Alue \"Vegasin katu\" puuttuu: aja valikot 376 ja 378."); return; }
            foreach (var n in new[] { "Vegasin hääkappeli", "Vegasin panttilainaamo", "Vegasin kappeli ja pantti ovet" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);

            float H = 2f * CamHalf, cy = CamY, top = cy + CamHalf;
            float W = 1672f / (941f / H);
            float ppuImg = 1844f / H, imgW = 5120f / ppuImg;
            float totalW = 2f * imgW;
            float casX = VegasStreetX0 + 2f * imgW, chapL = casX + imgW * VegasChapelOverlapFrac;

            var doorRoot = new GameObject("Vegasin kappeli ja pantti ovet");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, scene);
            Undo.RegisterCreatedObjectUndo(doorRoot, "Vegasin kappeli ja pantti ovet");
            Door MakeDoor(string name, string prompt, Area here, Area target, float x, Vector2 spawn)
            {
                var d = new GameObject(name).AddComponent<Door>();
                d.transform.SetParent(doorRoot.transform, false); d.transform.position = new Vector3(x, here.maxDepthY - 0.3f, 0f);
                d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = spawn;
                d.halfWidth = 1.8f; d.maxDistanceFromWall = 2.4f; d.onGround = true; d.groundHalfDepth = 1.6f;
                return d;
            }

            string helviReport = "", violetReport = "", weddingReport = "";
            var areasPath = VegasScenePath.Replace(".unity", ".areas.txt");
            var names = File.Exists(areasPath) ? File.ReadAllLines(areasPath).ToList() : new System.Collections.Generic.List<string>();

            // (alueen nimi, juuriolio, x0, kuvat, kuvan 1 sisäoven x-osuus, kadun oven x, kehote sisään, kehote ulos, ikkunat (taivas + hotellit))
            foreach (var (areaName, rootName, X0, files, inFrac, streetX, promptIn, promptOut, sky) in new[]
            {
                ("Vegasin hääkappeli", "Vegasin hääkappeli", VegasChapelX0, chapelFiles, 0.115f, chapL + imgW * 0.62f, "Mene hääkappeliin", "Mene ulos kadulle", true),
                ("Vegasin panttilainaamo", "Vegasin panttilainaamo", VegasPawnX0, pawnFiles, 0.115f, VegasStreetX0 + imgW * 0.2554f, "Mene panttilainaamoon", "Mene ulos kadulle", false),
            })
            {
                var root = new GameObject(rootName);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
                Undo.RegisterCreatedObjectUndo(root, rootName);

                var area = ReuseArea("Alue: " + areaName);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(area.gameObject, scene);
                area.areaName = areaName; area.useSidewalk = false;
                if (sky) { area.music = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Musiikki/Vegaswedding.mp3"); area.musicGain = 1f; }   // kappelin häämusiikki (oma sovitus)
                area.maxDepthY = top - 0.675f * H; area.minDepthY = top - 0.985f * H;   // lattia alkaa ~65 % korkeudesta
                area.walkMinX = X0 + imgW * 0.04f; area.walkMaxX = X0 + totalW - imgW * 0.03f;
                area.camMinX = X0 + W * 0.5f; area.camMaxX = Mathf.Max(area.camMinX, X0 + totalW - W * 0.5f);
                area.camSize = CamHalf; area.camRiseY = 0f; area.camOffsetY = 0f; area.backScale = 1f; area.depthLimits = new Vector2[0];

                for (int i = 0; i < files.Length; i++)
                {
                    var sp = ImportShipSprite(T + files[i] + ".png", ppuImg, false, SpriteAlignment.Center);
                    if (sp == null) { Info("Kuvan tuonti epäonnistui: " + files[i]); return; }
                    var sr = new GameObject(areaName + " " + (i + 1)).AddComponent<SpriteRenderer>();
                    sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10000;
                    sr.transform.position = new Vector3(X0 + imgW * (i + 0.5f), cy, 0f);
                    sr.transform.localScale = new Vector3(1.003f, 1f, 1f);
                }

                if (sky)
                {   // taulu seinän tyhjälle kohdalle sisäänkäyntikuvan viimeisen pilarin ja alttarikuvan ensimmäisen pilarin väliin (kuvien saumassa)
                    const string paintingPath = T + "vegas_kappeli_taulu.png";
                    if (File.Exists(paintingPath))
                    {
                        AssetDatabase.ImportAsset(paintingPath, ImportAssetOptions.ForceUpdate);
                        float worldW = 0.21f * imgW;                    // leveys ~21 % kuvan leveydestä
                        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(paintingPath);
                        if (tex != null)
                        {
                            var psp = ImportShipSprite(paintingPath, tex.width / worldW, false, SpriteAlignment.Center);
                            if (psp != null)
                            {
                                var psr = new GameObject("Hääkappelin taulu").AddComponent<SpriteRenderer>();
                                psr.transform.SetParent(root.transform, false); psr.sprite = psp; psr.sortingOrder = -9990;
                                psr.transform.position = new Vector3(X0 + imgW * 1.0675f, top - 0.264f * H, 0f);
                            }
                        }
                    }
                }
                if (sky)
                {   // kappelin ikkunat ovat läpinäkyviä: takana Vegasin yötaivas ja hotellit
                    foreach (var (name, file, order, factor, mirrors) in new[] { ("Taivas", "vegas_taivas", -10100, 0.02f, 0), ("Hotellit", "vegas_hotellit", -10050, 0.25f, 1) })
                    {
                        var sp = ImportShipSprite(T + file + ".png", ppuImg, false, SpriteAlignment.Center);
                        if (sp == null) { Info("Kuvan tuonti epäonnistui: " + file); return; }
                        var sr = new GameObject(areaName + ": " + name).AddComponent<SpriteRenderer>();
                        sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = order;
                        sr.transform.position = new Vector3(area.camMinX - W * 0.5f + sp.bounds.size.x * 0.5f, cy + (file == "vegas_taivas" ? 0f : 0.325f * H), 0f);
                        var pl = sr.gameObject.AddComponent<ParallaxLayer>();
                        pl.factor = factor; pl.mirrorCopies = mirrors;
                        pl.startCamX = area.camMinX; pl.minX = area.camMinX - 2f; pl.maxX = area.camMaxX + 2f;
                    }
                }

                float inX = X0 + imgW * inFrac;
                float inY = Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.12f), outY = Mathf.Lerp(street.maxDepthY, street.minDepthY, 0.12f);
                var doorIn = MakeDoor("Vegas: " + areaName + " ovi sisään", promptIn, street, area, streetX, new Vector2(inX + 1.4f, inY));
                MakeDoor("Vegas: " + areaName + " ovi ulos", promptOut, area, street, inX, new Vector2(streetX, outY));
                if (sky)
                {
                    foreach (var oldV in scene.GetRootGameObjects().Where(g => g.name == "Vegasin Violetti (kappeli)").ToArray()) Undo.DestroyObjectImmediate(oldV);   // Violetti on nyt kasinolla (päätehtävän antaja), ei kappelissa
                    weddingReport = AddChapelWedding(scene, area, X0, imgW);
                }
                if (!sky) helviReport = AddVegasHelvi(root.transform, scene, area, street, doorIn, X0, imgW, ppuImg, top, H);
                EditorUtility.SetDirty(area);
                if (!names.Contains(areaName)) names.Add(areaName);
            }
            File.WriteAllLines(areasPath, names);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Vegasin hääkappeli (x " + VegasChapelX0.ToString("0") + ") ja panttilainaamo (x " + VegasPawnX0.ToString("0") + ") rakennettu: sisätilat kahdesta kuvasta, ovet kadun kappelista ja panttilainaamosta sisään sekä vasemmasta sisäovesta ulos. " + helviReport + " " + violetReport + " " + weddingReport);
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    /// <summary>Häät: vihkijänä Melvin alttarilla, vieraina Don + kaksi Tankki-Tonya, kaksi Puliukkoa ja kaksi Jakuzaa katsomassa alttarille. Kaikki rauhassa, kunnes joku lyö (Melvin kuonoon): silloin rähinä alkaa (kaikki kaikkia vastaan, Don ja Tonyt yhdessä), vihkijä huutaa ja häät peruuntuvat.</summary>
    static string AddChapelWedding(UnityEngine.SceneManagement.Scene scene, Area chapel, float X0, float imgW)
    {
        foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Vegasin häävieraat" || g.name == "Vegasin kappelin tappelu").ToArray()) Undo.DestroyObjectImmediate(old);
        var all = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Enemy VegasTpl(string display, string rootPrefix) => all.FirstOrDefault(e => e != null && e.gameObject.scene == scene && e.displayName == display && e.transform.parent != null && e.transform.parent.name.StartsWith(rootPrefix) && e.guardOf == null);
        var tonyT = VegasTpl("Tankki-Tony", "Tankki-Tonyt"); var melvinT = VegasTpl("Melvin", "Melvinit"); var donT = VegasTpl("Don", "Donit");
        if (tonyT == null || melvinT == null || donT == null) return "Häävieraita ei lisätty: Vegasin mallit puuttuvat (aja 380, 382, 383).";

        // Puliukko ja Jakuza kopioidaan Thaitownin scenestä
        var thai = EditorSceneManager.GetSceneByPath(ThaitownScenePath);
        bool openedThai = false;
        if (!thai.IsValid() || !thai.isLoaded) { thai = EditorSceneManager.OpenScene(ThaitownScenePath, OpenSceneMode.Additive); openedThai = true; }
        try
        {
            Enemy FindThai(string display) => Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(en => en != null && en.gameObject.scene == thai && en.displayName == display && !en.gameObject.name.Contains("(terminaali") && !en.gameObject.name.Contains("(baari") && !en.gameObject.name.Contains("(hotelli") && !en.gameObject.name.Contains("(apterm"));
            var puliT = FindThai("Puliukko"); var jakuzaT = FindThai("Jakuza");
            if (puliT == null || jakuzaT == null) return "Häävieraita ei lisätty: Puliukkoa tai Jakuzaa ei löytynyt Thaitownin scenestä.";

            var root = new GameObject("Vegasin häävieraat");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Vegasin häävieraat");
            var crowd = new System.Collections.Generic.List<Enemy>();
            const string DonCrew = "Donin porukka";

            Vector3 Spot(float xFrac, float depth)
            {
                float x = X0 + imgW * xFrac;
                float yMax = chapel.MaxDepthAtX(x, chapel.maxDepthY), yMin = chapel.MinDepthAtX(x, chapel.minDepthY);
                return new Vector3(x, Mathf.Clamp(Mathf.Lerp(yMax, yMin, depth), yMin + 0.05f, yMax - 0.05f), 0f);
            }
            Enemy Spawn(GameObject tpl, string name, Vector3 pos, int facing)
            {
                var go = Object.Instantiate(tpl, root.transform);
                go.name = name; go.transform.SetParent(root.transform, false); go.SetActive(true);
                var e = go.GetComponent<Enemy>();
                e.ally = false; e.guardOf = null; e.guards = null; e.fightsEveryone = false; e.brawlGroup = "";
                e.wanderMinX = e.wanderMaxX = 0f;
                e.wakeDistance = -1f; e.wakeOnFightRadius = 0f; e.joinsFightWhenSquadComes = false;
                if (e.calmIdleSprites == null || e.calmIdleSprites.Length == 0) e.calmIdleSprites = e.idleSprites;
                if (e.calmWalkSprites == null || e.calmWalkSprites.Length == 0) { e.calmWalkSprites = e.walkSprites; e.calmWalkFrameTime = e.walkFrameTime; }
                e.calmFacing = facing;
                e.talksWhenCalm = false; e.tauntOnWakeChance = 0f;   // kappelissa ei puhuta ennen rähinää: vain vihkijä ja Puliukot (SalsaBrawl.chatterers / instigator)
                go.transform.position = pos;
                EditorUtility.SetDirty(e);
                crowd.Add(e);
                return e;
            }

            // vihkijä alttarilla (vasen puoli kasvot vieraisiin), vieraat katsovat alttarille (oikealle)
            var officiant = Spawn(melvinT.gameObject, "Melvin (vihkijä)", Spot(1.68f, 0.12f), -1);
            var don = Spawn(donT.gameObject, "Don (häävieras)", Spot(1.22f, 0.50f), 1);
            // sulhanen: Tankki-Tony alttarin edessä vihkijän vieressä (morsian tulee hänen vasemmalle puolelleen, x ~1,42); Donin porukkaa, ei vartija
            var groom = Spawn(tonyT.gameObject, "Tankki-Tony (sulhanen)", Spot(1.575f, 0.14f), -1);   // sulhanen katsoo morsianta (vasemmalle)
            groom.brawlGroup = DonCrew;
            // yksi Tony Donin vartijana vieraana
            var guard = Spawn(tonyT.gameObject, "Tankki-Tony (häävieras)", Spot(1.22f - 0.07f, 0.72f), 1);
            guard.guardOf = don;
            don.guards = new[] { guard };
            foreach (var e in new[] { don, guard }) e.brawlGroup = DonCrew;
            var puli1 = Spawn(puliT.gameObject, "Puliukko (häävieras 1)", Spot(0.62f, 0.72f), 1); puli1.fightsEveryone = true;
            var puli2 = Spawn(puliT.gameObject, "Puliukko (häävieras 2)", Spot(0.90f, 0.30f), 1); puli2.fightsEveryone = true;
            Spawn(jakuzaT.gameObject, "Jakuza (häävieras 1)", Spot(1.00f, 0.78f), 1);
            Spawn(jakuzaT.gameObject, "Jakuza (häävieras 2)", Spot(1.08f, 0.30f), 1);   // ei kenenkään pariskunnan edessä (alttarin edusta vapaa)

            // portsarit (4) ovat poikkeuksellisesti jo paikalla (rauhassa kuten vieraat): kaksi oven puolella katsoo huoneeseen, kaksi alttarin sivuilla; rähinän alkaessa mukaan
            int bouncers = 0;
            var bouncerT = all.FirstOrDefault(e => e != null && e.gameObject.scene == scene && e.displayName == "Portsari");
            if (bouncerT != null)
            {
                foreach (var (xf, dep, face) in new[] { (0.32f, 0.22f, 1), (0.32f, 0.78f, 1), (1.82f, 0.30f, -1), (1.82f, 0.70f, -1) })
                {
                    var be = Spawn(bouncerT.gameObject, "Portsari (kappeli " + (++bouncers) + ")", Spot(xf, dep), face);
                    be.fightsEveryone = false;   // rauhassa; Begin() asettaa
                }
            }
            // pöytä ja iso hääkakku: pöytä on tavallinen rikottava pöytä (Crate), kakku luhistuu pöytää lyötäessä (WeddingCake)
            string cakeInfo;
            {
                string tp = FindTexture("poyta");
                const string cakePath = "Assets/Sprites/Morsian/haakakku.png";
                var cakeSprites = File.Exists(cakePath) ? SliceAerobicsSheet(cakePath, 512, 512) : null;
                if (tp == null || cakeSprites == null) cakeInfo = "Pöytää tai kakkua ei lisätty (poyta-kuva tai haakakku.png puuttuu).";
                else
                {
                    SetupAndSlice(tp);
                    var table = LoadSprites("poyta").OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
                    var wood = AssetDatabase.FindAssets("t:AudioClip puu", new[] { "Assets/Audio" }).Select(AssetDatabase.GUIDToAssetPath)
                        .Where(q => Path.GetFileNameWithoutExtension(q).ToLowerInvariant().StartsWith("puu")).Select(AssetDatabase.LoadAssetAtPath<AudioClip>).Where(clip => clip != null).ToArray();
                    var tgo = new GameObject("Pöytä (hääkakku)"); tgo.transform.SetParent(root.transform, false);
                    tgo.transform.position = Spot(1.32f, 0.86f);
                    var tvis = new GameObject("Visual").AddComponent<SpriteRenderer>(); tvis.transform.SetParent(tgo.transform, false);
                    var tsh = new GameObject("Shadow").AddComponent<SpriteRenderer>(); tsh.transform.SetParent(tgo.transform, false); tsh.color = new Color(0f, 0f, 0f, 0.35f);
                    var tc = tgo.AddComponent<Crate>();
                    tc.body = tvis; tc.shadow = tsh;
                    tc.sprites = new[] { table[0], table[1], table[2] };
                    tc.breakSprites = new[] { table[3], table[4], table[5], table[6] };
                    tc.breakFrameTime = 0.1f; tc.hitsToBreak = 3; tc.breakable = true; tc.footOffset = 0.04f;
                    tc.visualScale = TableScale; tc.shadowWidth = 2.6f; tc.carryLower = 0.86f * TableScale; tc.plowThrough = true;
                    tc.hitRadiusX = 1.4f * TableScale; tc.debrisTime = 6f;
                    tc.moneyChance = 0.3f; tc.energyChance = 0.15f; tc.throwDamage = 22;
                    tc.breakSounds = wood;
                    tvis.sprite = table[0];
                    // kakku: ehjä leveä (alusta ~433 px kuvan 512:sta) ~2,3 yksikköä leveä pöydän päällä
                    var cgo = new GameObject("Hääkakku"); cgo.transform.SetParent(root.transform, false);
                    cgo.transform.localScale = Vector3.one * (2.3f / 4.33f);
                    var cake = cgo.AddComponent<WeddingCake>();
                    cake.table = tc; cake.sprites = cakeSprites; cake.tableTop = 1.28f * TableScale - 0.04f; cake.disturbToCollapse = 2; cake.sounds = wood;
                    cgo.transform.position = tgo.transform.position + new Vector3(0f, cake.tableTop, 0f);
                    cakeInfo = "Pöytä ja iso hääkakku lisätty (x " + tgo.transform.position.x.ToString("0.0") + "): kakku luhistuu, kun pöytää lyödään.";
                }
            }
            var bgo = new GameObject("Vegasin kappelin tappelu");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(bgo, scene);
            Undo.RegisterCreatedObjectUndo(bgo, "Vegasin kappelin tappelu");
            var brawl = bgo.AddComponent<SalsaBrawl>();
            brawl.area = chapel; brawl.instigator = puli1; brawl.calmSeconds = 1e9f;   // alkaa vain, kun joku lyö (vihkijä kuonoon); Puliukko (instigator) ja vihkijä puhuvat rauhassa
            brawl.chatterers = new[] { officiant, puli2 };
            brawl.chatterEvery = new Vector2(8f, 16f);
            bgo.AddComponent<ChapelAftermath>().brawl = brawl;   // kohtauksen jälkeen: "Puhu Violetille baarissa"
            brawl.crowd = crowd.ToArray(); brawl.sailors = null; brawl.rogues = null; brawl.shoutOnStart = officiant;
            EditorUtility.SetDirty(brawl);

            // morsian sulhasen vasemmalle puolelle; heittää kimpun Puliukon päähän, kun rähinä alkaa, ja pakenee ovelle
            string brideInfo;
            {
                const string BD = "Assets/Sprites/Morsian/";
                var bIdle = SliceAerobicsSheet(BD + "morsian_idle.png", 512, 512); var bThrow = SliceAerobicsSheet(BD + "morsian_heitto.png", 512, 640);
                var bRun = SliceAerobicsSheet(BD + "morsian_pako.png", 512, 512); var bBurst = SliceAerobicsSheet(BD + "kimppu_hajoaa.png", 512, 512);
                var bBouq = ImportShipSprite(BD + "kimppu.png", 100f, false, SpriteAlignment.Center);
                if (bIdle == null || bThrow == null || bRun == null || bBurst == null || bBouq == null) brideInfo = "Morsianta ei lisätty: kuvat puuttuvat (Assets/Sprites/Morsian).";
                else
                {
                    float brideH = DonScale * 6.0f * 0.46f;     // Donin korkeus (600 px / 100 x DonScale) x 0.92
                    float s = brideH / 4.8f;                       // morsiankuvan hahmo ~480 px
                    var bGo = new GameObject("Morsian"); bGo.transform.SetParent(root.transform, false);
                    bGo.transform.position = Spot(1.485f, 0.14f);
                    var vis = new GameObject("Visual").AddComponent<SpriteRenderer>(); vis.transform.SetParent(bGo.transform, false); vis.transform.localScale = Vector3.one * s; vis.sprite = bIdle[0];
                    var shd = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shd.transform.SetParent(bGo.transform, false); shd.transform.localScale = new Vector3(1.6f, 0.48f, 1f);
                    var br = bGo.AddComponent<ChapelBride>();
                    br.body = vis; br.shadow = shd; br.area = chapel; br.brawl = brawl;
                    br.idleSprites = bIdle; br.throwSprites = bThrow; br.runSprites = bRun; br.burstSprites = bBurst; br.bouquetSprite = bBouq;
                    br.releaseFrame = 9;
                    br.handOffset = new Vector2(-0.10f * brideH, 0.78f * brideH); br.bouquetWorldHeight = 0.12f * brideH;
                    br.exitX = X0 + imgW * 0.115f + 0.6f;         // sisäovi vasemmalla
                    EditorUtility.SetDirty(br);
                    brideInfo = "Morsian seisoo sulhasen vieressä (x " + bGo.transform.position.x.ToString("0.0") + ", korkeus " + brideH.ToString("0.0") + ").";
                }
            }
            return cakeInfo + " " + brideInfo + " Häät: Melvin vihkii alttarilla, sulhasena Tankki-Tony (morsiamelle tilaa x ~1,42), vieraina Don + Tony, 2 Puliukkoa ja 2 Jakuzaa" + (bouncers > 0 ? " + 4 portsaria jo paikalla" : " (portsarimallia ei löytynyt: aja 384)") + "; lyönti aloittaa rähinän ja häät peruuntuvat.";
        }
        finally { if (openedThai && thai.IsValid()) EditorSceneManager.CloseScene(thai, true); }
    }

    /// <summary>Violetti vieraaksi hääkappeliin: NpcStroller-kopio Violettista (Muurin juhlahalli, kuten valikossa 386); kävelee penkkien ja alttarin välillä, tanssii ja puhuu; ei nojaa tiskiin.</summary>
    static string AddChapelViolet(UnityEngine.SceneManagement.Scene scene, Area chapel, float X0, float imgW)
    {
        foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Vegasin Violetti (kappeli)").ToArray()) Undo.DestroyObjectImmediate(old);
        var srcPaths = new[] { WallScenePath, WallPartPath(WallFortScene) }.Where(File.Exists).Distinct().ToArray();
        if (srcPaths.Length == 0) return "Violettia ei lisätty: Muuri-scene puuttuu.";
        var opened = new System.Collections.Generic.List<UnityEngine.SceneManagement.Scene>();
        try
        {
            foreach (var sp0 in srcPaths)
            {
                var sc0 = EditorSceneManager.GetSceneByPath(sp0);
                if (!sc0.IsValid() || !sc0.isLoaded) { sc0 = EditorSceneManager.OpenScene(sp0, OpenSceneMode.Additive); opened.Add(sc0); }
            }
            var tmpl = Object.FindObjectsByType<NpcStroller>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(w => w != null && w.gameObject.scene != scene && w.name == "Violetti");
            if (tmpl == null) return "Violettia ei löytynyt Muurin scenestä (aja valikko 336).";
            var go = Object.Instantiate(tmpl.gameObject);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            go.name = "Vegasin Violetti (kappeli)"; go.SetActive(true);
            go.transform.localScale = go.transform.localScale * 1.1f;   // sama koko kuin baarin Violetilla
            var w2 = go.GetComponent<NpcStroller>();
            w2.area = chapel; w2.laneMinX = X0 + imgW * 0.30f; w2.laneMaxX = X0 + imgW * 1.75f;
            w2.leanChance = 0f;   // ei tiskiä kappelissa
            EditorUtility.SetDirty(w2);
            go.transform.position = new Vector3(X0 + imgW * 0.95f, Mathf.Lerp(chapel.maxDepthY, chapel.minDepthY, 0.55f), 0f);
            Undo.RegisterCreatedObjectUndo(go, "Vegasin Violetti (kappeli)");
            return "Violetti on myös hääkappelissa vieraana.";
        }
        finally { foreach (var so in opened) if (so.IsValid()) EditorSceneManager.CloseScene(so, true); }
    }

    /// <summary>Helvi panttilainaamon myyjäksi: seisoo tiskin takana (tiskin etuleike omana spritenä hänen edessään), tiski kopioidaan Thaitownin panttikonttorin tiskistä.</summary>
    static string AddVegasHelvi(Transform root, UnityEngine.SceneManagement.Scene scene, Area pawn, Area street, Door streetDoor, float X0, float imgW, float ppuImg, float top, float H)
    {
        const string stripPath = "Assets/Sprites/Taustat/vegas_pantti_tiski_etu.png";
        var idle = SliceAerobicsSheet(HelviDir + "helvi_idle.png"); var hair = SliceAerobicsSheet(HelviDir + "helvi_hiukset.png");
        if (idle == null || hair == null || idle.Length < 24 || hair.Length < 35) return "Helvin kuvat puuttuvat (Assets/Sprites/Helvi).";
        if (!File.Exists(stripPath)) return "Tiskin etuleike puuttuu (" + stripPath + ").";
        AssetDatabase.ImportAsset(stripPath, ImportAssetOptions.ForceUpdate);

        // tiskin malli Thaitownin panttikonttorista
        var thai = EditorSceneManager.GetSceneByPath(ThaitownScenePath);
        bool openedThai = false;
        if (!thai.IsValid() || !thai.isLoaded) { thai = EditorSceneManager.OpenScene(ThaitownScenePath, OpenSceneMode.Additive); openedThai = true; }
        try
        {
            var all = Object.FindObjectsByType<ShopCounter>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(c => c.gameObject.scene == thai).ToArray();
            var tpl = all.FirstOrDefault(c => c.name == "Thaitownin panttikonttorin tiski") ?? all.FirstOrDefault(c => c.name == "Aseman kanttiini");
            if (tpl == null) return "Tiskin mallia ei löytynyt Thaitownin scenestä (aja valikko 247).";

            // tiskin etuleike (counter x 0-2560 px, rivit 765-1255) Helvin eteen
            var strip = ImportShipSprite(stripPath, ppuImg, false, SpriteAlignment.Center);
            var sgo = new GameObject("Panttilainaamon tiski (etu)");
            sgo.transform.SetParent(root, false);
            float imgCenterX = X0 + imgW * 1.5f;   // vegas_pantti_sisa_2 keskipiste
            sgo.transform.position = new Vector3(imgCenterX - 1280f / ppuImg * 1.003f, top - 1010f / ppuImg, 0f);
            sgo.transform.localScale = new Vector3(1.003f, 1f, 1f);
            var ssr = sgo.AddComponent<SpriteRenderer>(); ssr.sprite = strip;
            ssr.sortingOrder = Mathf.RoundToInt(-(top - 1255f / ppuImg) * 100f);

            var seq = new System.Collections.Generic.List<Sprite>();
            for (int i = 0; i < 3; i++) seq.AddRange(idle.Take(24));
            seq.AddRange(hair.Take(35));
            float hx = X0 + imgW * 1.26f, hy = top - 0.60f * H;   // jalat tiskin takana, tiskin yläreunan alapuolella
            var h = new GameObject("Vegasin panttilainaamon Helvi");
            h.transform.SetParent(root, false);
            h.transform.position = new Vector3(hx, hy, 0f); h.transform.localScale = Vector3.one * HelviScale;
            var hsr = h.AddComponent<SpriteRenderer>(); hsr.sprite = seq[0]; hsr.sortingOrder = Mathf.RoundToInt(-hy * 100f);
            var dancer = h.AddComponent<Dancer>(); dancer.sprites = seq.ToArray(); dancer.frameTime = 0.1f; dancer.keepOrder = true;

            var shopGo = Object.Instantiate(tpl.gameObject, root); shopGo.name = "Vegasin panttilainaamon tiski";
            shopGo.transform.SetParent(root, false);
            shopGo.transform.position = new Vector3(hx, pawn.maxDepthY, 0f);
            var sc = shopGo.GetComponent<ShopCounter>();
            sc.here = pawn; sc.title = "PANTTILAINAAMO"; sc.npcName = "Helvi"; sc.prompt = "Puhu Helville";
            sc.halfWidth = 2.6f; sc.maxDistanceFromWall = 1.2f; sc.keeperHalfWidth = 0f; sc.ownItemsOnly = true;
            sc.hasBoardAt = true; sc.boardDoor = streetDoor;   // rullalauta ilmestyy kaupan eteen kadulle, oven viereen
            sc.boardAt = new Vector2(streetDoor.transform.position.x + 2.2f, Mathf.Lerp(street.maxDepthY, street.minDepthY, 0.25f));
            EditorUtility.SetDirty(sc);
            return "Helvi on panttilainaamon myyjä (tiskin takana x " + hx.ToString("0.0") + ").";
        }
        finally { if (openedThai && thai.IsValid()) EditorSceneManager.CloseScene(thai, true); }
    }
}
