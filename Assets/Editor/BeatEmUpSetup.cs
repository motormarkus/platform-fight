using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// Valikko "Beat em up": asettaa sprite-tiedostojen tuontiasetukset ja rakentaa testiscenen
/// (pelaaja, varjo, kamera ja katu) yhdellä klikkauksella.
/// </summary>
public static class BeatEmUpSetup
{
    const string IdleName = "idle";
    const string ActionName = "hahmo_spritesheet";

    const int CellW = 512, CellH = 384;
    const string SpriteFolder = "Assets/Sprites";
    // Kombon iskujen sprite sheetit (tiedostonimi ilman .png)
    const string JabName = "jab", CrossName = "takasuora", UppercutName = "uppercut";

    static bool batch;   // koko kadun rakennus: ilmoitukset lokiin eikä ikkunoihin

    /// Sceneä ei voi muokata pelin ollessa käynnissä: ilmoitus ja keskeytys.
    static bool PlayModeBlocked()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode) return false;
        EditorUtility.DisplayDialog("Beat em up", "Peli on käynnissä. Pysäytä peli (Play-nappi) ja aja valikko uudelleen.", "OK");
        return true;
    }

    static void Info(string msg)
    {
        if (batch) Debug.Log("Beat em up: " + msg.Replace("\n", " "));
        else EditorUtility.DisplayDialog("Beat em up", msg, "OK");
    }

    [MenuItem("Beat em up/29. Rakenna koko katu uudestaan (tausta, ovet, rekvisiitta, viholliset)")]
    static void RebuildStreet()
    {
        if (PlayModeBlocked()) return;
        batch = true;
        try
        {
            SetupBackground();      // talo, baari, S-Club kerran
            CreateClub();           // S-Clubin ovi ja sisätila
            AddBarDoor();           // baarin avautuva ovi (videosta, ääni mukana)
            AddDancers();
            AddBand();
            CreateShop();
            AddClubNpc();           // nainen baaritiskillä
            AddBikes();
            AddStreetExtension();   // liikerakennus ja pelihalli
            AddKovisGroups();
            AddPunks();
            AddLippis();
            AddSkaters();
            AddBouncers();          // portsarit S-Clubissa
            AddClubBikers();        // S-Clubiin kaksi prätkäjätkää (ei skeittareita sisätiloihin)
            CreateRoof();           // palotikkaat kadun lopussa ja katto (viholliset ja pomo)
            AddCrates();            // koko kadun matkalle (ei ovien, palotikkaiden eikä pyörien eteen)
            AddBarProps();          // S-Clubin pöydät ja pullot (baaritappelu)
            AddStreetTvs();         // muutama sammunut telkkari kadulla
            SetEnemyTactics();      // juoksu, kiertäminen, perääntyminen, torjunta
            AddBarrels();           // tynnyrit katolle ja satunnaisesti kadulle
            CreateBackAlley();      // katolta alas takakujalle, prätkä parkkiruudussa
            CreateHighway();        // kujan lopusta prätkällä valtatielle
            CreateUccopulco();      // valtatien lopusta Uccopulcon rantakadulle
            CreateLoippari();       // El Loippari -baarin sisätila
            CreateShipDeck();       // risteilyaluksen kansi (sataman varaston ovesta), rullaava meri
            AddShipFight();         // laivan tappelu: rosvot (toistaiseksi Lippikset) ja seilorit hyttiovelta
            AddSamoans();           // samoalaiset Uccopulcossa
            AddLoipparTables();     // El Loipparin pöydät: kala-annokset, pullot ja lasit
            AddUccoProps();         // laatikot ja tynnyrit Uccopulcoon ja El Loippariin, Sohvi Loipparin tiskille
            AddLoipparFighters();   // Lippikset, samoalaiset ja portsarit El Loippariin
            AddLoipparStage();      // mariachi-bändi ja tanssijat El Loipparin lavalle
            AddChairs();            // tuolit El Loippariin (hero ottaa käteen, lyö ja heittää)
            SetupDropKick();        // heron pudotuspotku juoksusta
            AddShipProps();         // laivan kannelle pöydät, kala-annokset, pullot, lasit ja tuolit (ei tanssipaikalle)
            AddRingStands();        // pelastusrenkaat telineineen laivan kannelle
            SetupBigHook();         // heron iso koukku (alas, eteen + lyönti)
            CreateShipInterior();   // laivan sisätilat: käytävä, hytti ja sali
            AddDrunk();             // puliukko tulee käytävän hyttiovesta
            AddSalonTables();       // salin pöydät: hummeriannokset ja shamppanjapullot
            AddSalonLadies();       // salin naiset ja Sohvi baaritiskin takana
            AddSalonFight();        // salin tappelu: rosvot käytävän ovelta, seilorit salin perältä
            CreateStreetBar();      // kadun baari: BAR-ovesta sisään (ovi aukeaa)
            CreateBarRoom();        // pokerihuone baarin takahuoneena (oikean nurkan teräsovesta)
            AddStreetBarFurniture(); // kadun baariin pöydät, tuolit ja tappelijat
            ApplyWoodBreakSounds(); // puu1/puu2 kaikille hajoaville pöydille ja laatikoille
            SetupHeroine();         // toinen pelattava hahmo (Ruby), valitaan alkuvalikossa
        }
        finally { batch = false; }
        Info("Koko katu rakennettu: talo, baari, S-Club ja kadun jatko, ovet, moottoripyörät, laatikot ja viholliset.\nYksityiskohdat Console-ikkunassa.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/1. Aseta ja leikkaa kaikki spritet")]
    static void FixImportSettings()
    {
        var report = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == "valkoinen") continue;
            int count = SetupAndSlice(path);
            if (count >= 0) report.Add($"{Path.GetFileName(path)}: {count} kuvaa");
        }
        Info(
            report.Count == 0 ? "Sprites-kansiosta ei löytynyt sprite sheetejä." :
            "Leikattu (512 × 384, jalat alareunassa):\n\n" + string.Join("\n", report));
    }

    /// Asettaa tuontiasetukset ja leikkaa kuvan 512 × 384 ruutuihin (tyhjät ruudut ohitetaan).
    /// Säilyttää vanhojen palojen tunnisteet nimen perusteella, jotta viittaukset eivät katkea.
    /// Palauttaa palojen määrän, tai -1 jos kuva ei ole ruudukon kokoinen.
    /// Alue uudelleenrakennuksessa: vanha komponentti säilyy (muiden kohtien viittaukset pysyvät), kentät oletusarvoihin.
    static Area ReuseArea(string goName)
    {
        var go = GameObject.Find(goName);
        var a = go != null ? go.GetComponent<Area>() : null;
        if (a == null)
        {
            if (go != null) Undo.DestroyObjectImmediate(go);
            a = new GameObject(goName).AddComponent<Area>();
            Undo.RegisterCreatedObjectUndo(a.gameObject, "Alue");
            return a;
        }
        Undo.RecordObject(a, "Alue");
        var tmp = new GameObject("tmp").AddComponent<Area>();
        EditorUtility.CopySerialized(tmp, a);
        Object.DestroyImmediate(tmp.gameObject);
        EditorUtility.SetDirty(a);
        return a;
    }

    static int SetupAndSlice(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2);
        tex.LoadImage(bytes);
        int w = tex.width, h = tex.height;
        string baseName0 = Path.GetFileNameWithoutExtension(path);
        // tanssijan kuvat ovat kapeampia (256 × 384), muut 512 × 384
        int CellW = baseName0.StartsWith("romuukko_") ? 768 : baseName0.StartsWith("rekkamies_") ? 512 : baseName0.StartsWith("elaine_") || baseName0.StartsWith("shanice_") ? 384 : baseName0.StartsWith("linsey_piirakka") || baseName0.StartsWith("linsey_heitto") ? 384 : baseName0.StartsWith("linsey_") ? 320 : baseName0.StartsWith("rusty_lyonti") || baseName0.StartsWith("rusty_syoksy") || baseName0.StartsWith("rusty_potku") ? 640 : baseName0.StartsWith("rusty_kaatuminen") || baseName0.StartsWith("rusty_ylosnousu") || baseName0.StartsWith("rusty_heitetty") || baseName0.StartsWith("rusty_piirakka") || baseName0.StartsWith("rusty_heitto") ? 640 : baseName0.StartsWith("rusty_nykaisy") ? 2048 : baseName0.StartsWith("rusty_ruoska") ? 1664 : baseName0.StartsWith("horhe_potkut") ? 1024 : baseName0 == "horhe_heitetty" || baseName0 == "kapteeni_heitetty" ? 640 : baseName0.StartsWith("kapteeni_iskut") || baseName0.StartsWith("kapteeni_liuku") ? 768 : baseName0.StartsWith("kapteeni_kaatuminen") || baseName0.StartsWith("kapteeni_ylosnousu") ? 640 : baseName0.StartsWith("horhe_ote") || baseName0.StartsWith("horhe_takakoukku") || baseName0.StartsWith("horhe_koukku") || baseName0.StartsWith("horhe_juoksu") || baseName0.StartsWith("horhe_lapsy") || baseName0.StartsWith("horhe_vasara") || baseName0.StartsWith("horhe_harkko") || baseName0.StartsWith("horhe_kaatuminen") || baseName0.StartsWith("horhe_ylosnousu") ? 640 : baseName0.StartsWith("vihu_haymaker") || baseName0.StartsWith("rokkari2_") || baseName0.StartsWith("portsari_heittaa") || baseName0.StartsWith("sankaritar_pudotuspotku") ? 768 : baseName0.StartsWith("sankaritar_keppi_lyonti") || baseName0.StartsWith("sankaritar_keppi_heitto") ? 1024 : baseName0.StartsWith("sankaritar_keppi") ? 512 : baseName0.StartsWith("sankaritar_tuolilyonti") || baseName0.StartsWith("tuolilyonti") ? 1024 : baseName0.StartsWith("baarinainen_suudelma") || baseName0.StartsWith("sankaritar_ylosnousu") || baseName0.StartsWith("sankaritar_heiluripotku") ? 768 : baseName0.StartsWith("motoristi_potku") ? 640 : baseName0.StartsWith("baari_poker_loppu") ? 854 : baseName0.StartsWith("baari_poker_idle") ? 896 : baseName0.StartsWith("katu_bar_ovi") ? 360 : baseName0.StartsWith("skeittari_") ? 640 : baseName0.StartsWith("keppi_lyonti") || baseName0.StartsWith("keppi_heitto") ? 1024 : baseName0.StartsWith("laiva_kaytava_ovi") ? 330 : baseName0.StartsWith("laiva_ovi") ? 372 : baseName0.StartsWith("rengas_kuvat") ? 192 : baseName0.StartsWith("turisti_tanssi") ? 384 : baseName0.StartsWith("tuoli_") || baseName0.StartsWith("sankaritar_tuoli") || baseName0.StartsWith("sankaritar_pratka") || baseName0.StartsWith("rengas_") ? 768 : baseName0.StartsWith("klubitanssija_b3") ? 512 : baseName0.StartsWith("tanssija") || baseName0.StartsWith("klubitanssija") ? 256 : baseName0.StartsWith("pratka") || baseName0.StartsWith("vihu_pratka") || baseName0.StartsWith("vihu_pyora") || baseName0.StartsWith("bandi") ? 768
                  : baseName0.StartsWith("poyta") ? 448 : baseName0.StartsWith("pullo_") ? 128 : baseName0.StartsWith("telkkari") ? 256 : BeatEmUpSetup.CellW;
        // saksipotkun ilmakuvat ja pomon nyrkki pään yllä tarvitsevat enemmän korkeutta (512 × 512)
        int CellH = baseName0.StartsWith("romuukko_") || baseName0.StartsWith("motoristi_poyta") || baseName0.StartsWith("motoristi_kanto") ? 448 : baseName0.StartsWith("rekkamies_kanto") ? 512 : baseName0.StartsWith("rekkamies_") ? 448 : baseName0.StartsWith("elaine_") || baseName0.StartsWith("shanice_") ? 448 : baseName0.StartsWith("linsey_") ? 384 : baseName0.StartsWith("rusty_kaatuminen") || baseName0.StartsWith("rusty_ylosnousu") || baseName0.StartsWith("rusty_heitetty") || baseName0.StartsWith("rusty_piirakka") || baseName0.StartsWith("rusty_heitto") ? 448 : baseName0 == "horhe_heitetty" || baseName0 == "kapteeni_heitetty" ? 448 : baseName0.StartsWith("horhe_takakoukku") || baseName0.StartsWith("kapteeni_iskut") ? 512 : baseName0.StartsWith("kapteeni_kaatuminen") || baseName0.StartsWith("kapteeni_ylosnousu") || baseName0.StartsWith("kapteeni_liuku") ? 448 : baseName0.StartsWith("horhe_vasara") || baseName0.StartsWith("horhe_koukku") || baseName0.StartsWith("horhe_potkut") || baseName0.StartsWith("horhe_juoksu") || baseName0.StartsWith("horhe_harkko") || baseName0.StartsWith("horhe_osuma") || baseName0.StartsWith("horhe_kaatuminen") || baseName0.StartsWith("horhe_ylosnousu") ? 448 : baseName0.StartsWith("rokkari2_") || baseName0.StartsWith("sankaritar_keppi") ? 512 : baseName0.StartsWith("sankaritar_heitto") || baseName0.StartsWith("sankaritar_ylosnousu") || baseName0.StartsWith("sankaritar_pullonosto") || baseName0.StartsWith("sankaritar_iso") || baseName0.StartsWith("sankaritar_rinnus_iso") || baseName0.StartsWith("sankaritar_voimalyonti") || baseName0.StartsWith("sankaritar_heiluripotku") ? 512 : baseName0.StartsWith("baari_poker_loppu") ? 506 : baseName0.StartsWith("baari_poker_idle") ? 504 : baseName0.StartsWith("katu_bar_ovi") ? 600 : baseName0.StartsWith("laiva_kaytava_ovi") ? 445 : baseName0.StartsWith("laiva_ovi") ? 600 : baseName0.StartsWith("rengas_kuvat") ? 192 : baseName0.StartsWith("turisti_aurora") ? 768 : baseName0.StartsWith("tuoli_") || baseName0.StartsWith("tuolilyonti") || baseName0.StartsWith("sankaritar_tuoli") || baseName0.StartsWith("rengas_") || baseName0.StartsWith("turisti_") ? 512 : baseName0.StartsWith("saksipotku") || baseName0.StartsWith("koukku_iso") || baseName0.StartsWith("pomo_lyonti") || baseName0.StartsWith("vihu_lento") ? 512
                  : baseName0.StartsWith("vihu_pyora_kaatuu") ? 640 : baseName0.StartsWith("skeittari_") || baseName0.StartsWith("pratka") || baseName0.StartsWith("sankaritar_pratka") || baseName0.StartsWith("vihu_pratka") || baseName0.StartsWith("vihu_pyora") || baseName0.StartsWith("bandi") ? 448
                  : baseName0.StartsWith("poyta") ? 256 : baseName0.StartsWith("pullo_") ? 96 : baseName0.StartsWith("telkkari") ? 192 : BeatEmUpSetup.CellH;   // prätkä: 768 × 448
        // myyjä on piirretty tarkemmin (kaksinkertainen resoluutio)
        int ppu = baseName0.StartsWith("myyja") || baseName0.StartsWith("laatikko") || baseName0.StartsWith("tynnyri") ? 200 : 100;
        if (w % CellW != 0 || h % CellH != 0) { Object.DestroyImmediate(tex); return -1; }

        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Multiple;
        ti.spritePixelsPerUnit = ppu;
        ti.filterMode = FilterMode.Bilinear;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        int big = Mathf.Max(w, h);
        ti.maxTextureSize = big > 4096 ? 8192 : big > 2048 ? 4096 : 2048;   // isot sheetit (esim. videosta) täysikokoisina
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.SaveAndReimport();

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(ti);
        dp.InitSpriteEditorDataProvider();
        var oldIds = dp.GetSpriteRects().GroupBy(r => r.name).ToDictionary(g => g.Key, g => g.First().spriteID);

        string baseName = Path.GetFileNameWithoutExtension(path);
        var rects = new List<SpriteRect>();
        int cols = w / CellW, rows = h / CellH;
        for (int r = 0; r < rows; r++)          // ylärivi ensin
            for (int c = 0; c < cols; c++)
            {
                int x = c * CellW, y = h - (r + 1) * CellH;
                if (IsEmpty(tex, x, y, CellW, CellH)) continue;
                string n = $"{baseName}_{rects.Count}";
                rects.Add(new SpriteRect
                {
                    name = n,
                    rect = new Rect(x, y, CellW, CellH),
                    alignment = SpriteAlignment.BottomCenter,
                    pivot = new Vector2(0.5f, 0f),
                    spriteID = oldIds.TryGetValue(n, out GUID id) ? id : GUID.Generate(),
                });
            }
        Object.DestroyImmediate(tex);

        dp.SetSpriteRects(rects.ToArray());
        var ids = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        ids?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        dp.Apply();
        ti.SaveAndReimport();
        return rects.Count;
    }

    static bool IsEmpty(Texture2D tex, int x, int y, int cellW, int cellH)
    {
        Color[] px = tex.GetPixels(x, y, cellW, cellH);
        for (int i = 0; i < px.Length; i += 7)
            if (px[i].a > 0.1f) return false;
        return true;
    }

    // ---------------- Tausta ----------------
    // katu_sarja.png = kerrostalo + baari + S-Club yhdessä kuvassa (saumat liukuvärjätty), toistetaan peräkkäin
    const string BackgroundPath = "Assets/Sprites/Taustat/katu_sarja.png";
    const float SingleImageWidth = 1536f;   // yhden alkuperäisen kuvan leveys pikseleinä
    // Kuvassa oven korkeus on n. 210 px; ovi ~4 yksikköä (ukko 3.3) → 52 px per yksikkö.
    const float BackgroundPPU = 52f;
    // Seinän ja jalkakäytävän rajan korkeus kuvassa (osuus ylhäältä), ja mihin y-kohtaan se asetetaan pelissä.
    const float WallBaseFromTop = 590f / 1024f;
    const float WallBaseWorldY = -0.3f;
    const int BackgroundRepeats = 1;   // kolmen kuvan sarja (talo, baari, S-Club) kerran, sen jälkeen kadun jatko
    // Mitattu kuvasta: jalkakäytävän pinta riveillä 592–615, kynnyksen etureuna 615–637.
    const float CurbTopRow = 615f, CurbBottomRow = 637f, WallBaseRow = 590f;

    [MenuItem("Beat em up/4. Aseta katutausta")]
    static void SetupBackground()
    {
        var ti = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
        if (ti == null)
        {
            Info( $"Taustakuvaa ei löytynyt:\n{BackgroundPath}");
            return;
        }
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = BackgroundPPU;
        ti.filterMode = FilterMode.Bilinear;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.maxTextureSize = 8192;                           // kolmen kuvan sarja on n. 4500 px leveä
        ti.mipmapEnabled = false;
        ti.wrapMode = TextureWrapMode.Repeat;               // toistoa varten
        var settings = new TextureImporterSettings();
        ti.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;  // Tiled-tila vaatii tämän
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        ti.SetTextureSettings(settings);
        ti.SaveAndReimport();

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        float wU = sprite.rect.width / BackgroundPPU, hU = sprite.rect.height / BackgroundPPU;

        var old = GameObject.Find("Tausta");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var go = new GameObject("Tausta");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = new Vector2(wU * BackgroundRepeats, hU);
        sr.sortingOrder = -10000;
        float top = WallBaseWorldY + WallBaseFromTop * hU;
        // ukko aloittaa ensimmäisen kuvan (kerrostalo) keskeltä, katu jatkuu oikealle
        float startLeft = -SingleImageWidth / BackgroundPPU * 0.5f;
        go.transform.position = new Vector3(startLeft + wU * BackgroundRepeats * 0.5f, top - hU * 0.5f, 0f);
        Undo.RegisterCreatedObjectUndo(go, "Luo tausta");

        // testikatu ja -seinä pois näkyvistä (ei poisteta)
        foreach (var n in new[] { "Katu", "Seinä" })
        {
            var t = GameObject.Find(n);
            if (t != null) { Undo.RecordObject(t, "Piilota"); t.SetActive(false); }
        }

        // kamera ei saa mennä taustan ulkopuolelle
        var follow = Object.FindFirstObjectByType<CameraFollow>();
        var cam = Camera.main;
        if (follow != null && cam != null)
        {
            Undo.RecordObject(follow, "Kameran rajat");
            float halfW = cam.orthographicSize * cam.aspect;
            float left = go.transform.position.x - wU * BackgroundRepeats * 0.5f;
            float right = go.transform.position.x + wU * BackgroundRepeats * 0.5f;
            follow.minX = left + halfW;
            follow.maxX = right - halfW;
        }

        // jalkakäytävän korkeus ja kävelyalueen yläraja pelaajalle
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            Undo.RecordObject(pc, "Jalkakäytävä");
            float curbHeight = (CurbBottomRow - CurbTopRow) / BackgroundPPU;
            pc.useSidewalk = true;
            pc.sidewalkHeight = curbHeight;
            pc.curbDepthY = WallBaseWorldY - (CurbBottomRow - WallBaseRow) / BackgroundPPU;
            // seinän juuri ajoradan tasossa = seinän juuri ruudulla miinus kynnyksen korkeus, pieni väli seinään
            pc.maxDepthY = WallBaseWorldY - curbHeight - 0.08f;
            // alaraja: jalat ja varjo pysyvät kuvassa (StreetBottomMargin kameran alareunan yläpuolella)
            var mainCam = Camera.main;
            if (mainCam != null)
                pc.minDepthY = mainCam.transform.position.y - mainCam.orthographicSize + StreetBottomMargin;
            EditorUtility.SetDirty(pc);
        }

        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        Info(
            $"Tausta lisätty ({BackgroundRepeats} × {wU:0.0} yksikköä). Testikatu piilotettu.\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/5. Päivitä kävely ja juoksu")]
    static void UpdateWalk()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { Info( "Scenessä ei ole pelaajaa. Tee ensin kohta 2."); return; }
        Sprite[] walk = LoadSprites("kavely")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0)
            .ToArray();
        if (walk.Length == 0) { Info( "kavely.png ei löytynyt tai sitä ei ole leikattu. Tee ensin kohta 1."); return; }
        Sprite[] run = LoadSprites("juoksu")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0)
            .ToArray();
        Undo.RecordObject(pc, "Päivitä kävely");
        pc.walkSprites = walk;
        pc.runSprites = run;
        // koko askelsykli (kaksi askelta) kestää 0.82 s kuvamäärästä riippumatta (videon oma tahti)
        pc.walkFrameTime = 0.82f / walk.Length;
        // juoksusykli 0.73 s (videon oma tahti)
        if (run.Length > 0) pc.runSpriteFrameTime = 0.73f / run.Length;
        EditorUtility.SetDirty(pc);
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        Info( $"Kävely: {walk.Length} kuvaa ({pc.walkFrameTime:0.000} s/kuva)\nJuoksu: {run.Length} kuvaa ({pc.runSpriteFrameTime:0.000} s/kuva) (Shift)\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/6. Päivitä äänet")]
    static void UpdateSounds()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { Info( "Scenessä ei ole pelaajaa. Tee ensin kohta 2."); return; }
        AudioClip[] grunts = AssetDatabase.FindAssets("t:AudioClip grunt", new[] { "Assets/Audio/Pelaaja" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(p => p)
            .Select(AssetDatabase.LoadAssetAtPath<AudioClip>)
            .Where(c => c != null)
            .ToArray();
        Undo.RecordObject(pc, "Päivitä äänet");
        pc.attackGrunts = grunts;
        pc.hurtSounds = LoadClips("Assets/Audio/Pelaaja", "gasphero");
        EditorUtility.SetDirty(pc);
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        Info(
            $"Iskujen gruntit: {grunts.Length} kpl\nOsuman gaspit (gasphero): {pc.hurtSounds.Length} kpl\n(Assets/Audio/Pelaaja)\n\nTallenna scene (Ctrl+S).");
    }

    // Pelin taustamusiikki (tiedostonimi ilman päätettä kansiossa Assets/Audio/Musiikki)
    const string MusicName = "Turpaan vaan";

    [MenuItem("Beat em up/7. Aseta taustamusiikki")]
    static void SetupMusic()
    {
        var paths = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/Musiikki" })
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToList();
        // valittu kappale, tai jos sitä ei löydy, ensimmäinen kansiosta
        string path = paths.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == MusicName) ?? paths.FirstOrDefault();
        if (path == null)
        {
            Info( "Kansiosta Assets/Audio/Musiikki ei löytynyt äänitiedostoa.");
            return;
        }
        // musiikki kannattaa striimata levyltä eikä ladata kokonaan muistiin
        var ai = (AudioImporter)AssetImporter.GetAtPath(path);
        var st = ai.defaultSampleSettings;
        st.loadType = AudioClipLoadType.Streaming;
        st.compressionFormat = AudioCompressionFormat.Vorbis;
        st.quality = 0.7f;
        ai.defaultSampleSettings = st;
        ai.SaveAndReimport();
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);

        var mp = Object.FindFirstObjectByType<MusicPlayer>();
        if (mp == null)
        {
            var go = new GameObject("Musiikki");
            go.AddComponent<AudioSource>();
            mp = go.AddComponent<MusicPlayer>();
            Undo.RegisterCreatedObjectUndo(go, "Luo musiikki");
        }
        Undo.RecordObject(mp, "Aseta musiikki");
        mp.music = clip;
        mp.volume = 1f;
        EditorUtility.SetDirty(mp);
        EditorSceneManager.MarkSceneDirty(mp.gameObject.scene);
        Info(
            $"Taustamusiikki: {Path.GetFileName(path)}\nSoitetaan silmukkana, voimakkuus 1.0 (säädä Musiikki-objektista).\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/70. Toinen pelattava hahmo (Ruby)")]
    static void SetupHeroine()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { Info("Scenessä ei ole pelaajaa."); return; }
        var missing = new List<string>();
        Sprite[] L(string part)
        {
            string file = "sankaritar_" + part;
            string path = FindTexture(file);
            if (path == null) { missing.Add(file + ".png"); return new Sprite[0]; }
            SetupAndSlice(path);
            return LoadSprites(file)
                .OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        }
        Undo.RecordObject(pc, "Ruby");
        // Roccon tuolilyönti videosta (sama valikko, ettei tarvitse ajaa tuolivalikkoa uudelleen)
        foreach (var rn in new[] { "tuolilyonti", "tuolilyonti_tyhja" }) { string rp = FindTexture(rn); if (rp != null) SetupAndSlice(rp); }
        pc.chairSwingSprites = LoadSprites("tuolilyonti").OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
        pc.chairSwingBareSprites = LoadSprites("tuolilyonti_tyhja").OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
        var a = pc.heroine ?? new PlayerController.CharacterArt();
        a.name = "Ruby";
        a.idle = L("idle"); a.walk = L("kavely"); a.run = L("juoksu");
        a.jab = L("lyonti"); a.cross = L("suora"); a.leadHook = L("etukoukku"); a.rearHook = L("takakoukku");
        a.lowKick = L("matalapotku"); a.midKick = L("keskipotku"); a.highKick = L("korkeapotku");
        a.jump = L("hyppy"); a.jumpKick = L("ilmapotku"); a.flip = L("voltti");
        a.special = L("tornado"); a.cartwheel = L("karrynpyora");
        a.counterThrow = L("heitto"); a.block = L("suojaus");
        a.thrown = L("heitetty"); a.fall = L("kaatuminen"); a.getup = L("ylosnousu");
        a.smallItem = L("pullonosto"); a.bigLift = L("isonosto"); a.bigThrow = L("isoheitto"); a.bigCarry = L("isokanto"); a.pummel = L("rinnus"); a.pummelBig = L("rinnus_iso"); a.frontKick = L("etupotku"); a.backKick = L("takapotku"); a.power = L("voimalyonti"); a.pendulum = L("heiluripotku"); a.dropKick = L("pudotuspotku");
        a.chairPick = L("tuoli_nosto"); a.chairThrow = L("tuoli_heitto"); a.chairSwing = L("tuolilyonti"); a.chairSwingBare = L("tuolilyonti_tyhja"); a.chairWalk = L("tuoli_kavely");
        a.bikeRide = L("pratka_ajo"); a.bikeMount = L("pratka_nousu"); a.bikeGrab = L("pratka_kiskaisu");
        a.cueIdle = L("keppi_idle"); a.cueWalk = L("keppi_kavely"); a.cueSwingA = L("keppi_lyonti_a"); a.cueSwingB = L("keppi_lyonti_b"); a.cueThrow = L("keppi_heitto");
        a.hurtSounds = LoadClips("Assets/Audio/Ruby", "rubygasp");
        a.attackGrunts = LoadClips("Assets/Audio/Ruby", "rubyhit");
        a.specialGrunts = LoadClips("Assets/Audio/Ruby", "rubyspecial");
        a.gulpSounds = LoadClips("Assets/Audio/Ruby", "rubygulp");
        var aah = LoadClips("Assets/Audio/Ruby", "rubyaah"); a.aahSound = aah.Length > 0 ? aah[0] : null;
        var curse = LoadClips("Assets/Audio/Ruby", "rubyfallrise"); a.riseCurse = curse.Length > 0 ? curse[0] : null;
        pc.heroine = a;
        AssignBooniArt(pc);
        pc.characterName = "Rocco";
        EditorUtility.SetDirty(pc);
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        if (batch) { Debug.Log($"Ruby: {a.idle.Length} idle, {a.getup.Length} ylösnousu" + (missing.Count > 0 ? ", puuttuu: " + string.Join(", ", missing) : "")); return; }
        Info("Ruby lisätty toiseksi pelattavaksi hahmoksi.\n\n" +
             "Alkuvalikko: Aloita peli → valitse hahmo (Rocco / Ruby) → vaikeustaso.\n" +
             "Lyönnit: jab, takasuora, etukoukku, takakoukku. Potkut: matala, keski, korkea.\n" +
             "Kaksi kertaa eteen + lyönti (myös kombon keskellä): tarttuu rinnuksista ja lyö 5 kertaa.\n" +
             "Alas, eteen + lyönti: voimalyönti, lennättää vihun korkealle.\n" +
             "Alas, eteen + potku: heiluripotku, lennättää vihut (myös kombon keskellä).\n" +
             "Eteen + potku: etupotku. Taakse + potku: takapotku (kaataa).\n" +
             "Erikoisliike (L / LB): tornadopotku. Pusku (U / RT): kärrynpyörä. Vastaheitto (O): kuperkeikka.\n" +
             "Ruby nostaa ja heittää pulloja, laseja, laatikoita, tynnyreitä ja pöytiä (ei tuoleja eikä renkaita).\n" +
             (missing.Count > 0 ? "\nPuuttuu: " + string.Join(", ", missing) + "\n" : "") +
             "\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/3. Päivitä lyöntikombo")]
    static void UpdateCombo()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { Info( "Scenessä ei ole pelaajaa. Tee ensin kohta 2."); return; }

        var combo = new List<PlayerController.ComboHit>();
        var found = new List<string>();

        combo.Add(MakeHit("Jab", JabName, impactFrame: 2, frameTime: 0.045f, impactHold: 0.09f, lunge: 0.18f,
            fallbackFrame: 8, found, damage: 6, reach: 1.6f, knockdown: false));
        combo.Add(MakeHit("Takasuora", CrossName, impactFrame: 3, frameTime: 0.05f, impactHold: 0.11f, lunge: 0.26f,
            fallbackFrame: 8, found, damage: 8, reach: 1.7f, knockdown: false));
        combo.Add(MakeHit("Jab", JabName, impactFrame: 2, frameTime: 0.045f, impactHold: 0.09f, lunge: 0.18f,
            fallbackFrame: 8, found, damage: 6, reach: 1.6f, knockdown: false));
        // uppercut: kyykky (kuva 1), nousu ja isku ylös (kuva 4), kaataa
        combo.Add(MakeHit("Uppercut", UppercutName, impactFrame: 4, frameTime: 0.055f, impactHold: 0.16f, lunge: 0.30f,
            fallbackFrame: 8, found, damage: 14, reach: 1.7f, knockdown: true));

        Undo.RecordObject(pc, "Päivitä lyöntikombo");
        pc.punchCombo = combo.ToArray();
        EditorUtility.SetDirty(pc);
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);

        Info(
            "Kombo päivitetty: Jab → Takasuora → Jab → Uppercut.\n\n" + string.Join("\n", found) +
            "\n\nTallenna scene (Ctrl+S).");
    }

    static PlayerController.ComboHit MakeHit(string label, string sheet, int impactFrame, float frameTime, float impactHold,
        float lunge, int fallbackFrame, List<string> found, int damage, float reach, bool knockdown)
    {
        Sprite[] sprites = LoadSprites(sheet)
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0)
            .ToArray();
        found.Add(sprites.Length > 0
            ? $"{label}: {sheet}.png, {sprites.Length} kuvaa"
            : $"{label}: ei vielä omaa kuvaa ({sheet}.png puuttuu) → käytetään varakuvaa");
        return new PlayerController.ComboHit
        {
            name = label,
            sprites = sprites,
            frameTime = frameTime,
            impactFrame = Mathf.Min(impactFrame, Mathf.Max(0, sprites.Length - 1)),
            impactHold = impactHold,
            windupTime = 0.05f,
            frame = fallbackFrame,
            duration = 0.2f,
            lunge = lunge,
            damage = damage,
            reach = reach,
            knockdown = knockdown,
        };
    }

    [MenuItem("Beat em up/2. Luo pelaaja ja testikatu sceneen")]
    static void CreateScene()
    {
        Sprite[] idle = LoadSprites(IdleName);
        Sprite[] action = LoadSprites(ActionName);
        if (idle.Length == 0 || action.Length == 0)
        {
            Info(
                $"Spritejä ei löytynyt (idle: {idle.Length}, hahmo_spritesheet: {action.Length}).\n\n" +
                "Tee ensin kohta 1 ja leikkaa kuvat Sprite Editorissa (512 × 384).");
            return;
        }

        // --- pelaaja ---
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0f, -2f, 0f);
        var visual = new GameObject("Visual").AddComponent<SpriteRenderer>();
        visual.transform.SetParent(player.transform, false);
        var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>();
        shadow.transform.SetParent(player.transform, false);

        var pc = player.AddComponent<PlayerController>();
        pc.idleSprites = idle;
        pc.actionSprites = action;
        pc.body = visual;
        pc.shadow = shadow;
        visual.sprite = idle[0];
        Undo.RegisterCreatedObjectUndo(player, "Luo pelaaja");

        // --- kamera ---
        var cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.transform.position = new Vector3(0f, 1.8f, -10f);
            cam.backgroundColor = new Color(0.13f, 0.14f, 0.18f);
            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = player.transform;
        }

        // --- testikatu: jalkakäytävä ja seinä ---
        Sprite block = GetWhiteBlockSprite();
        var street = MakeBlock("Katu", block, new Vector3(0f, -2f, 0f), new Vector3(80f, 3.6f, 1f), new Color(0.32f, 0.33f, 0.37f), -10000);
        var wall = MakeBlock("Seinä", block, new Vector3(0f, 2.8f, 0f), new Vector3(80f, 6f, 1f), new Color(0.22f, 0.2f, 0.24f), -10001);
        Undo.RegisterCreatedObjectUndo(street, "Luo katu");
        Undo.RegisterCreatedObjectUndo(wall, "Luo seinä");

        Selection.activeGameObject = player;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Pelaaja luotu. Paina Play. Ohjaus: WASD/nuolet, Space = hyppy, J = lyönti, K = potku (ilmassa = hyppypotku), U = pusku, I (pidä) = suojaus, O = vastaheitto.");
    }

    const string EnemyFolder = "Assets/Sprites/Viholliset";

    [MenuItem("Beat em up/8. Lisää vihollinen (Kovis)")]
    static void AddEnemy()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { Info( "Scenessä ei ole pelaajaa. Tee ensin kohta 2."); return; }

        var report = new List<string>();
        Sprite[] Sheet(string name)
        {
            string path = AssetDatabase.FindAssets("t:Texture2D " + name, new[] { EnemyFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == name);
            Sprite[] sp = path == null ? new Sprite[0] :
                AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                    .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0)
                    .ToArray();
            report.Add(sp.Length > 0 ? $"{name}.png: {sp.Length} kuvaa" : $"{name}.png: puuttuu (käytetään varaliikettä)");
            return sp;
        }

        Sprite[] idle = Sheet("vihu_idle");
        if (idle.Length == 0)
        {
            Info( "vihu_idle.png ei löytynyt kansiosta " + EnemyFolder + " tai sitä ei ole leikattu. Tee ensin kohta 1.");
            return;
        }

        // päivitetään olemassa oleva Kovis, muuten luodaan uusi pelaajan eteen
        var e = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).FirstOrDefault(x => x.gameObject.name == "Kovis");
        if (e == null)
        {
            var go = new GameObject("Kovis");
            Vector3 pp = pc.transform.position;
            go.transform.position = new Vector3(pp.x + 6f, Mathf.Clamp(-2f, pc.minDepthY, pc.maxDepthY), 0f);
            var visual = new GameObject("Visual").AddComponent<SpriteRenderer>();
            visual.transform.SetParent(go.transform, false);
            var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>();
            shadow.transform.SetParent(go.transform, false);
            e = go.AddComponent<Enemy>();
            e.body = visual;
            e.shadow = shadow;
            visual.sprite = idle[0];
            Undo.RegisterCreatedObjectUndo(go, "Luo vihollinen");
        }
        Undo.RecordObject(e, "Päivitä vihollinen");
        e.idleSprites = idle;
        e.walkSprites = Sheet("vihu_kavely");
        e.walkFrameTime = 0.085f;                  // 12 kuvaa, askelsykli ~1 s
        e.punchSprites = Sheet("vihu_lyonti");
        e.hurtSprites = Sheet("vihu_osuma");
        e.knockdownSprites = Sheet("vihu_kaatuminen");
        e.getUpSprites = Sheet("vihu_ylosnousu");
        e.grabSprites = Sheet("vihu_heitto");      // tarttuu ja heittää välillä, kun pääsee viereen
        e.hurtSounds = LoadClips("Assets/Audio/big thug", "gasp");
        e.hurtVolume = 0.99f;
        e.bigBody = true;                          // pelaaja heittää kuperkeikalla
        e.flipThrownSprites = Sheet("vihu_kuperkeikka");
        { string hp = FindTexture("vihu_haymaker"); if (hp != null) SetupAndSlice(hp); }
        SetKovisHaymaker(e, Sheet("vihu_haymaker"));
        report.Add($"Osumaäänet (gasp): {e.hurtSounds.Length} kpl");
        EditorUtility.SetDirty(e);

        // energiapalkit ja iskujen tuntuma
        if (Object.FindFirstObjectByType<GameHUD>() == null)
        {
            var hud = new GameObject("HUD").AddComponent<GameHUD>();
            hud.player = pc;
            Undo.RegisterCreatedObjectUndo(hud.gameObject, "Luo HUD");
        }
        var fx = Object.FindFirstObjectByType<HitFx>();
        if (fx == null)
        {
            fx = new GameObject("HitFx").AddComponent<HitFx>();
            Undo.RegisterCreatedObjectUndo(fx.gameObject, "Luo HitFx");
        }
        Undo.RecordObject(fx, "Osumaäänet");
        fx.impactSounds = LoadClips("Assets/Audio/sfx", "hit");
        report.Add($"Iskun osumaäänet (Assets/Audio/sfx/hit*): {fx.impactSounds.Length} kpl");
        // pelaajan osumaäänet samalla kertaa
        Undo.RecordObject(pc, "Pelaajan osumaäänet");
        pc.hurtSounds = LoadClips("Assets/Audio/Pelaaja", "gasphero");
        report.Add($"Pelaajan osumaäänet (gasphero): {pc.hurtSounds.Length} kpl");
        EditorUtility.SetDirty(pc);
        EditorUtility.SetDirty(fx);

        Selection.activeGameObject = e.gameObject;
        EditorSceneManager.MarkSceneDirty(e.gameObject.scene);
        Info( "Kovis valmis.\n\n" + string.Join("\n", report) + "\n\nTallenna scene (Ctrl+S).");
    }

    // Koviksien ryhmät kadun varrella: (x, syvyys y). Ruudun leveys on n. 29.5 yksikköä.
    static readonly Vector2[] KovisSpawns =
    {
        new Vector2(9f, -2.0f), new Vector2(12f, -3.3f),                               // kerrostalo
        new Vector2(30f, -1.6f), new Vector2(33f, -3.6f),                              // baari
        new Vector2(60f, -2.0f), new Vector2(63f, -3.4f),                              // S-Club
        new Vector2(95f, -2.2f), new Vector2(98f, -3.2f),                              // jatko: mainostaulu
        new Vector2(138f, -1.8f), new Vector2(141f, -3.0f), new Vector2(144f, -2.4f), // lähikauppa
        new Vector2(160f, -2.0f), new Vector2(163f, -3.3f), new Vector2(166f, -2.6f), // pelihalli: kadun loppu
    };

    [MenuItem("Beat em up/9. Lisää Koviksia kadulle")]
    static void AddKovisGroups()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        var template = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).FirstOrDefault(x => x.gameObject.name == "Kovis");
        if (pc == null || template == null)
        {
            Info( "Tee ensin kohta 8 (Kovis), sitten tämä.");
            return;
        }
        // vanhat kopiot pois, alkuperäinen Kovis siirretään ensimmäiseen kohtaan
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            if (e.gameObject.name.StartsWith("Kovis_")) Undo.DestroyObjectImmediate(e.gameObject);

        float x0 = pc.transform.position.x;
        Undo.RecordObject(template, "Kovis kestävyys");
        template.maxHealth = 85;           // ennen 60: kaatuivat liian helposti
        template.attackCooldown = 1.4f;    // ennen 1.6: lyö useammin
        // heitto: nopeampi tarttuminen ja nosto, heilautus kiihtyy ja pelaaja lentää kovempaa
        template.grabReachTime = 0.22f;    // ennen 0.3
        template.grabLiftTime = 0.55f;     // ennen 0.8
        template.throwSpeed = 11f;         // korkea ja pitkä lento
        template.throwUp = 10f;
        EditorUtility.SetDirty(template);
        Undo.RecordObject(template.transform, "Siirrä Kovis");
        template.transform.position = new Vector3(x0 + KovisSpawns[0].x, Mathf.Clamp(KovisSpawns[0].y, pc.minDepthY, pc.maxDepthY), 0f);
        for (int i = 1; i < KovisSpawns.Length; i++)
        {
            var go = Object.Instantiate(template.gameObject);
            go.name = "Kovis_" + (i + 1);
            go.transform.position = new Vector3(x0 + KovisSpawns[i].x, Mathf.Clamp(KovisSpawns[i].y, pc.minDepthY, pc.maxDepthY), 0f);
            Undo.RegisterCreatedObjectUndo(go, "Lisää Kovis");
        }
        EditorSceneManager.MarkSceneDirty(template.gameObject.scene);
        Info(
            $"Kadulla on nyt {KovisSpawns.Length} Kovista ryhmissä koko kadun matkalla.\n" +
            "Ne heräävät, kun tulet n. 9 yksikön päähän. Kaksi lyö kerrallaan, muut odottavat vuoroaan.\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- S-Club sisältä ----------------
    const string ClubInteriorPath = "Assets/Sprites/Taustat/klubi_sisa.png";
    const float ClubPPU = 85f;             // kuva täyttää ruudun korkeussuunnassa
    const float ClubFloorRow = 665f;       // sohvien ja tiskin juuri kuvassa (ylhäältä), jalat tämän alapuolella
    const float ClubX0 = 1000f;            // sisätila on kaukana kadusta samassa scenessä
    // Kadun kuvasarjassa S-Clubin oven keskikohta (pikseleinä sarjan vasemmasta reunasta) ja sarjan leveys
    const float ClubDoorPx = 2992f + 777f, StreetSetPx = 4488f;

    [MenuItem("Beat em up/10. Luo S-Club sisätila ja ovet")]
    static void CreateClub()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        var street = GameObject.Find("Tausta");
        var cam = Camera.main;
        var follow = Object.FindFirstObjectByType<CameraFollow>();
        var ti = AssetImporter.GetAtPath(ClubInteriorPath) as TextureImporter;
        if (pc == null || street == null || cam == null || follow == null || ti == null)
        {
            Info( "Tarvitaan pelaaja, katutausta (kohta 4), kamera ja kuva " + ClubInteriorPath);
            return;
        }

        // kuvan tuonti
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = ClubPPU;
        ti.filterMode = FilterMode.Bilinear;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.maxTextureSize = 8192;   // klubi levennetty (4356 px)
        ti.mipmapEnabled = false;
        ti.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ClubInteriorPath);
        float wU = sprite.rect.width / ClubPPU, hU = sprite.rect.height / ClubPPU;

        // vanhat pois
        foreach (var n in new[] { "S-Club sisä", "Ovet" })
        {
            var o = GameObject.Find(n);
            if (o != null) Undo.DestroyObjectImmediate(o);
        }

        float camY = cam.transform.position.y;
        float halfW = cam.orthographicSize * 16f / 9f;

        // sisätilan tausta: yläreuna ruudun yläreunassa
        var bg = new GameObject("S-Club sisä");
        var sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -10000;
        bg.transform.position = new Vector3(ClubX0 + wU * 0.5f, camY, 0f);
        Undo.RegisterCreatedObjectUndo(bg, "S-Club");

        // alueet
        var streetArea = ReuseArea("Alue: Katu");   // sama alue: kadun baarin ja katon ovet viittaavat siihen
        streetArea.areaName = "Katu";
        streetArea.minDepthY = pc.minDepthY;
        streetArea.maxDepthY = pc.maxDepthY;
        streetArea.useSidewalk = pc.useSidewalk;
        streetArea.sidewalkHeight = pc.sidewalkHeight;
        streetArea.curbDepthY = pc.curbDepthY;
        streetArea.camMinX = follow.minX;
        streetArea.camMaxX = follow.maxX;

        var clubArea = ReuseArea("Alue: S-Club");
        clubArea.areaName = "S-Club";
        clubArea.music = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Musiikki/S-Club.mp3");   // klubin oma musiikki
        float top = camY + hU * 0.5f;
        clubArea.maxDepthY = top - ClubFloorRow / ClubPPU;
        clubArea.minDepthY = pc.minDepthY;
        clubArea.useSidewalk = false;
        clubArea.camMinX = ClubX0 + halfW;
        clubArea.camMaxX = ClubX0 + wU - halfW;

        // ovet: S-Clubin ovi jokaisessa kadun kuvasarjassa, ja paluuovi sisätilan vasemmassa reunassa
        var doors = new GameObject("Ovet");
        Undo.RegisterCreatedObjectUndo(doors, "Ovet");
        var ssr = street.GetComponent<SpriteRenderer>();
        float left = street.transform.position.x - ssr.size.x * 0.5f;
        float setU = StreetSetPx / BackgroundPPU;
        int count = Mathf.RoundToInt(ssr.size.x / setU);
        var report = new List<string>();
        for (int i = 0; i < count; i++)
        {
            float x = left + ClubDoorPx / BackgroundPPU + i * setU;
            var d = new GameObject("S-Club ovi " + (i + 1)).AddComponent<Door>();
            d.transform.SetParent(doors.transform, false);
            // oven paikka on samalla kohta, johon pelaaja palaa ulos tullessaan (jalkakäytävällä oven edessä)
            d.transform.position = new Vector3(x, streetArea.maxDepthY - 0.25f, 0f);
            d.prompt = "Mene S-Clubiin";
            d.here = streetArea;
            d.target = clubArea;
            d.spawnPoint = new Vector2(ClubX0 + 4.5f, (clubArea.minDepthY + clubArea.maxDepthY) * 0.5f);
            d.halfWidth = 1.4f;
            d.maxDistanceFromWall = 0.9f;
            report.Add($"Ovi {i + 1}: x = {x:0.0}");
        }
        var exit = new GameObject("Uloskäynti").AddComponent<Door>();
        exit.transform.SetParent(doors.transform, false);
        exit.transform.position = new Vector3(ClubX0 + 1.2f, clubArea.minDepthY, 0f);
        exit.prompt = "Ulos kadulle";
        exit.here = clubArea;
        exit.target = streetArea;
        exit.returnToLastDoor = true;
        exit.halfWidth = 2.2f;
        exit.maxDistanceFromWall = 100f;   // koko vasen reuna käy
        exit.spawnPoint = new Vector2(left + ClubDoorPx / BackgroundPPU, streetArea.maxDepthY - 0.25f);

        EditorSceneManager.MarkSceneDirty(bg.scene);
        Selection.activeGameObject = bg;
        Info(
            "S-Club sisätila luotu (stage vasemmalla, baaritiski oikealla).\n\n" + string.Join("\n", report) +
            $"\nSisällä syvyys {clubArea.minDepthY:0.00} … {clubArea.maxDepthY:0.00}\n\n" +
            "Mene oven eteen jalkakäytävälle ja paina E (ohjaimessa B).\nSisältä pääsee ulos vasemmasta reunasta samalla napilla.\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Tanssijat ----------------
    // Lavan kohta S-Clubin sisäkuvassa (pikseleinä): tanko x = 770, jalat lavan pinnalla rivillä ~ 525
    const float StageFeetRow = 527f;
    // toinen lava (lisätty taustakuvaan looshin kohdalle): bändi lavan keskellä
    const float BandStagePx = 1641f;

    [MenuItem("Beat em up/40. S-Clubin toinen lava: bändi")]
    static void AddBand()
    {
        var club = GameObject.Find("S-Club sisä");
        string bp = FindTexture("bandi");
        if (club == null || bp == null) { Info("Tarvitaan S-Clubin sisätila (kohta 10) ja bandi.png."); return; }
        SetupAndSlice(bp);
        var sprites = LoadSprites("bandi")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        if (sprites.Length == 0) { Info("bandi.png: ei kuvia (kohta 1)."); return; }
        var old = GameObject.Find("Bändi");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var bgSr = club.GetComponent<SpriteRenderer>();
        float ppu = bgSr.sprite.pixelsPerUnit;
        float left = club.transform.position.x - bgSr.bounds.size.x * 0.5f;
        float top = club.transform.position.y + bgSr.bounds.size.y * 0.5f;
        var go = new GameObject("Bändi");
        go.transform.position = new Vector3(left + BandStagePx / ppu, top - StageFeetRow / ppu, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprites[0];
        sr.sortingOrder = -9000;                       // taustan edessä, pelaajien takana
        sr.color = new Color(1f, 0.88f, 0.95f);        // lavan valo
        var d = go.AddComponent<Dancer>();
        d.sprites = sprites;
        d.frameTime = 1f / 18f;                        // videon oma tahti (18 fps, toistetut ruudut poistettu)
        Undo.RegisterCreatedObjectUndo(go, "Bändi");
        EditorSceneManager.MarkSceneDirty(go.scene);
        Info($"Bändi toisella lavalla: {sprites.Length} kuvaa.\n\nTallenna scene (Ctrl+S).");
    }
    const float DancerScale = 1.0f;   // tanssijat (ennen 1.2: liian isoja)

    // S-Clubin tanssija tangon vasemmalla puolella (napatanssijat ovat El Loipparissa)
    const float ClubDancerPx = 690f;
    const float ClubDancer2Px = 860f;   // toinen tanssija tangon oikealla puolella

    [MenuItem("Beat em up/11. Lisää tanssija S-Clubin lavalle")]
    static void AddDancers()
    {
        var club = GameObject.Find("S-Club sisä");
        string path = FindTexture("klubitanssija");
        if (path != null) SetupAndSlice(path);
        var sprites = LoadSprites("klubitanssija")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        if (club == null || sprites.Length == 0)
        {
            Info( "Tarvitaan S-Clubin sisätila (kohta 10) ja klubitanssija.png.");
            return;
        }
        var old = GameObject.Find("Tanssijat");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = new GameObject("Tanssijat");
        Undo.RegisterCreatedObjectUndo(root, "Tanssijat");

        var bgSr = club.GetComponent<SpriteRenderer>();
        float ppu = bgSr.sprite.pixelsPerUnit;
        float left = club.transform.position.x - bgSr.bounds.size.x * 0.5f;
        float top = club.transform.position.y + bgSr.bounds.size.y * 0.5f;
        var go = new GameObject("Tanssija");
        go.transform.SetParent(root.transform, false);
        go.transform.position = new Vector3(left + ClubDancerPx / ppu, top - StageFeetRow / ppu, 0f);
        go.transform.localScale = new Vector3(DancerScale, DancerScale, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprites[0];
        sr.sortingOrder = -9000;                       // taustan edessä, pelaajien takana
        sr.color = new Color(1f, 0.88f, 0.96f);        // lavan violetti valo
        var d = go.AddComponent<Dancer>();
        d.sprites = sprites;
        d.frameTime = 0.125f;                          // videon tahti (joka kolmas ruutu, 24 fps)
        // toinen tanssija tangon oikealla puolella: kolme tanssia peräkkäin (kolmannessa spagaatti, leveämmät ruudut)
        var second = new List<Sprite>();
        foreach (var part in new[] { "klubitanssija_b1", "klubitanssija_b2", "klubitanssija_b3" })
        {
            string pp = FindTexture(part);
            if (pp == null) continue;
            SetupAndSlice(pp);
            second.AddRange(LoadSprites(part).OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int n) ? n : 0));
        }
        if (second.Count > 0)
        {
            var go2 = new GameObject("Tanssija 2");
            go2.transform.SetParent(root.transform, false);
            go2.transform.position = new Vector3(left + ClubDancer2Px / ppu, top - StageFeetRow / ppu, 0f);
            go2.transform.localScale = new Vector3(DancerScale, DancerScale, 1f);
            var sr2 = go2.AddComponent<SpriteRenderer>();
            sr2.sprite = second[0];
            sr2.sortingOrder = -9000;
            sr2.color = sr.color;
            var d2 = go2.AddComponent<Dancer>();
            d2.sprites = second.ToArray();
            d2.keepOrder = true;                       // kolme sarjaa peräkkäin: ei lajitella numeron mukaan
            d2.frameTime = 0.125f;
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Info( $"S-Clubin lavalla kaksi tanssijaa: {sprites.Length} + {second.Count} kuvaa, tanssit peräkkäin silmukkana.\nNapatanssijat ovat El Loipparissa (kohta 52).\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Punkkari ----------------
    // Kadulla (x pelaajan aloituskohdasta, syvyys y) ja S-Clubin sisällä (x sisätilan vasemmasta reunasta, syvyys 0 = keskellä)
    static readonly Vector2[] PunkStreet = { new Vector2(20f, -2.8f), new Vector2(45f, -2.2f), new Vector2(85f, -3.0f), new Vector2(115f, -2.4f), new Vector2(150f, -3.1f) };
    static readonly Vector2[] PunkClub = { new Vector2(13f, 0.3f), new Vector2(16f, -0.2f), new Vector2(19f, -0.6f), new Vector2(25f, 0.5f), new Vector2(31f, -0.4f), new Vector2(36f, 0.4f) };

    static Sprite[] EnemySheet(string name, List<string> report)
    {
        string path = AssetDatabase.FindAssets("t:Texture2D " + name, new[] { EnemyFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == name);
        Sprite[] sp = path == null ? new Sprite[0] :
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0)
                .ToArray();
        report.Add(sp.Length > 0 ? $"{name}.png: {sp.Length} kuvaa" : $"{name}.png: puuttuu (varaliike)");
        return sp;
    }

    [MenuItem("Beat em up/12. Lisää Punkkarit (katu ja S-Club)")]
    static void AddPunks()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { Info( "Scenessä ei ole pelaajaa."); return; }
        var report = new List<string>();
        Sprite[] idle = EnemySheet("punk_idle", report);
        if (idle.Length == 0)
        {
            Info( "punk_idle.png puuttuu tai sitä ei ole leikattu (kohta 1).");
            return;
        }

        // vanhat punkkarit pois
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            if (e.gameObject.name.StartsWith("Punkkari")) Undo.DestroyObjectImmediate(e.gameObject);

        // malli
        var go = new GameObject("Punkkari");
        var visual = new GameObject("Visual").AddComponent<SpriteRenderer>();
        visual.transform.SetParent(go.transform, false);
        var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>();
        shadow.transform.SetParent(go.transform, false);
        var t = go.AddComponent<Enemy>();
        t.body = visual; t.shadow = shadow; visual.sprite = idle[0];
        t.displayName = "Punkkari";
        t.idleSprites = idle;
        t.walkSprites = EnemySheet("punk_kavely", report);
        t.punchSprites = EnemySheet("punk_lyonti", report);
        t.altAttackSprites = EnemySheet("punk_pusku", report);
        t.hurtSprites = EnemySheet("punk_osuma", report);
        t.knockdownSprites = EnemySheet("punk_kaatuminen", report);
        t.getUpSprites = EnemySheet("punk_ylosnousu", report);
        t.headlockThrownSprites = EnemySheet("punk_niskalenkki", report);   // pelaajan niskalenkki
        t.headlockFlightSprites = EnemySheet("punk_niskalenkki_lento", report);   // lento, isku maahan, pomppu, makuu
        if (File.Exists(EnemyFolder + "/punk_pullo.png")) SetupAndSlice(EnemyFolder + "/punk_pullo.png");
        t.bottleSprites = EnemySheet("punk_pullo", report);      // hakee lattialta ehjän pullon ja heittää
        t.idleFrameTime = 0.2f;
        t.walkFrameTime = 0.115f;          // videon oma tahti (12 kuvaa, 1.4 s sykli)
        t.moveSpeedX = 2.6f;               // Kovista nopeampi
        t.moveSpeedY = 1.6f;
        t.maxHealth = 65;                  // ennen 45: kaatui liian helposti
        t.punchDamage = 7;
        t.punchImpactFrame = 3;
        t.altImpactFrame = 4;              // pusku osuu kuvassa 5 (tähti)
        t.altDamage = 12;
        t.altChance = 0.4f;
        t.attackCooldown = 1.1f;
        // pusku syöksyy eteen: liukuu n. 2.2 yksikköä, vaarallisempi ja pidempi kantama
        t.altLungeSpeed = 8f;
        t.altLungeTime = 0.28f;
        t.altReach = 1.4f;
        t.hurtSounds = LoadClips("Assets/Audio/big thug", "gasp");
        t.hurtVolume = 0.99f;
        SetPunkSounds(t);                  // omat äänet kansiosta Audio/punkkari, jos ne on lisätty
        Undo.RegisterCreatedObjectUndo(go, "Punkkari");

        var positions = new List<Vector3>();
        float x0 = pc.transform.position.x;
        foreach (var v in PunkStreet)
            positions.Add(new Vector3(x0 + v.x, Mathf.Clamp(v.y, pc.minDepthY, pc.maxDepthY), 0f));
        var club = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "S-Club");
        if (club != null)
        {
            float mid = (club.minDepthY + club.maxDepthY) * 0.5f;
            foreach (var v in PunkClub)
                positions.Add(new Vector3(ClubX0 + v.x, Mathf.Clamp(mid + v.y, club.minDepthY, club.maxDepthY), 0f));
        }
        go.transform.position = positions[0];
        for (int i = 1; i < positions.Count; i++)
        {
            var c = Object.Instantiate(go);
            c.name = "Punkkari_" + (i + 1);
            c.transform.position = positions[i];
            Undo.RegisterCreatedObjectUndo(c, "Punkkari");
        }
        EditorSceneManager.MarkSceneDirty(go.scene);
        Info(
            $"Punkkareita: {PunkStreet.Length} kadulla" + (club != null ? $", {PunkClub.Length} S-Clubissa" : " (S-Club puuttuu, tee kohta 10)") +
            "\n\n" + string.Join("\n", report) + "\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/13. Päivitä erikoisliike (pyörähdyspotku)")]
    static void UpdateSpecial()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { Info( "Scenessä ei ole pelaajaa."); return; }
        Sprite[] sp = LoadSprites("erikoispotku")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        if (sp.Length == 0) { Info( "erikoispotku.png puuttuu tai sitä ei ole leikattu (kohta 1)."); return; }
        Undo.RecordObject(pc, "Erikoisliike");
        pc.specialSprites = sp;
        pc.specialFrameTime = 0.045f;
        // nopea 360: potku eteen kuvissa 7–9, heti perään taakse kuvissa 10–13 (18 kuvaa × 0.045 s)
        pc.specialHitFrom = 0.25f;
        pc.specialHitTo = 0.60f;
        EditorUtility.SetDirty(pc);
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        Info(
            $"Pyörähdyspotku: {sp.Length} kuvaa ({sp.Length * 0.045f:0.00} s).\nNäppäin L, ohjaimessa LB. Osuu molempiin suuntiin ja kaataa.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/38. Erikoisliike: tuulimylly")]
    static void UpdateWindmill()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { Info("Scenessä ei ole pelaajaa."); return; }
        string path = FindTexture("tuulimylly");
        if (path != null) SetupAndSlice(path);
        Sprite[] sp = LoadSprites("tuulimylly")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        if (sp.Length == 0) { Info("tuulimylly.png puuttuu."); return; }
        Undo.RecordObject(pc, "Tuulimylly");
        pc.specialSprites = sp;
        pc.specialFrameTime = 0.045f;           // 20 kuvaa = 0.9 s: veto, kaksi 8-suunnan kierrosta, paluu
        pc.specialHitFrom = 0.09f;
        pc.specialHitTo = 0.81f;
        pc.specialHitEvery = 0.36f;             // osuu joka kierroksella (8 kuvaa)
        pc.specialDamage = 10;                  // kaksi osumaa = 20
        pc.specialReach = 2.6f;                 // kädet ylettyvät kauas molemmin puolin
        EditorUtility.SetDirty(pc);
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        Info($"Tuulimylly: {sp.Length} kuvaa, kaksi kierrosta ({sp.Length * 0.045f:0.00} s).\nNäppäin L, ohjaimessa LB. Osuu molemmin puolin kummallakin kierroksella ja kaataa.\n\nTallenna scene (Ctrl+S).");
    }

    // Baaritiski S-Clubin sisäkuvassa (pikseleinä): n. 1710–2600, keskikohta 2155
    const float BarCounterPx = 2155f + 1740f, BarCounterHalfPx = 410f;   // + kahden lisätyn lavajakson leveys taustakuvaan
    const float CounterTopRow = 402f;   // tiskin yläreuna sisäkuvassa

    [MenuItem("Beat em up/14. Luo baaritiski (kauppa) S-Clubiin")]
    static void CreateShop()
    {
        var club = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "S-Club");
        if (club == null) { Info( "Tee ensin kohta 10 (S-Clubin sisätila)."); return; }
        var old = GameObject.Find("Baaritiski");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var go = new GameObject("Baaritiski");
        go.transform.position = new Vector3(ClubX0 + BarCounterPx / ClubPPU, club.maxDepthY, 0f);
        var shop = go.AddComponent<ShopCounter>();
        shop.here = club;
        shop.halfWidth = BarCounterHalfPx / ClubPPU;
        shop.maxDistanceFromWall = 1.0f;
        // ostoäänet kansiosta Assets/Audio/sfx (sama ääni käy usealle tuotteelle)
        var soundFor = new Dictionary<string, string>
        {
            { "Sipsipussi", "sipsipussi" }, { "Grillimakkara", "grillimakkara" }, { "Lonkero", "lonkero" },
            { "Makkaraperunat", "grillimakkara" }, { "Tuoppi", "lonkero" }, { "Kossupaukku", "kossupaukku" },
        };
        int soundCount = 0;
        foreach (var it in shop.items)
            if (soundFor.TryGetValue(it.name, out string file))
            {
                it.sound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx/" + file + ".wav");
                if (it.sound != null) soundCount++;
            }
        Undo.RegisterCreatedObjectUndo(go, "Baaritiski");

        // HUD tarvitaan rahojen ja elämien näyttämiseen
        if (Object.FindFirstObjectByType<GameHUD>() == null)
        {
            var hud = new GameObject("HUD").AddComponent<GameHUD>();
            Undo.RegisterCreatedObjectUndo(hud.gameObject, "HUD");
        }
        // myyjä Sohvi tiskin taakse: kuvan alareuna = tiskin yläreuna (rivi 402 sisäkuvassa)
        string sohviInfo = "Sohvi: myyja_sohvi.png puuttuu (kohta 1)";
        var oldS = GameObject.Find("Sohvi");
        if (oldS != null) Undo.DestroyObjectImmediate(oldS);
        var sohviSprites = LoadSprites("myyja_sohvi")
            .OrderBy(sp => int.TryParse(sp.name.Substring(sp.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        var clubBg = GameObject.Find("S-Club sisä");
        if (sohviSprites.Length > 0 && clubBg != null)
        {
            var bgSr = clubBg.GetComponent<SpriteRenderer>();
            float top = clubBg.transform.position.y + bgSr.bounds.size.y * 0.5f;
            var sGo = new GameObject("Sohvi");
            sGo.transform.position = new Vector3(ClubX0 + BarCounterPx / ClubPPU, top - CounterTopRow / ClubPPU, 0f);
            var sSr = sGo.AddComponent<SpriteRenderer>();
            sSr.sprite = sohviSprites[0];
            sSr.sortingOrder = -9500;                   // taustan edessä, kaikkien hahmojen takana
            var anim = sGo.AddComponent<Dancer>();      // silmukka-animaatio (hengitys edestakaisin)
            anim.sprites = sohviSprites;
            anim.frameTime = 0.18f;
            Undo.RegisterCreatedObjectUndo(sGo, "Sohvi");
            shop.transform.position = new Vector3(sGo.transform.position.x, shop.transform.position.y, 0f);
            sohviInfo = $"Sohvi tiskin takana ({sohviSprites.Length} kuvaa)";
        }
        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        Info(
            sohviInfo + "\n" + $"Baaritiski luotu S-Clubin oikealle puolelle ({soundCount}/{shop.items.Length} tuotteella ääni).\nMene tiskin eteen ja paina E: juomat ja snackit maksavat markkoja,\njoita putoaa kaatuneista vihollisista.\n\nValikoimaa ja hintoja voi muuttaa Baaritiski-objektista.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/15. Aseta rahapudotusten ja energiajuoman kuvat")]
    static void SetupPickups()
    {
        var report = new List<string>();
        foreach (var n in new[] { "seteli", "setelitukku", "energiajuoma" })
        {
            string path = "Assets/Resources/Pickups/" + n + ".png";
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) { report.Add(n + ".png: puuttuu"); continue; }
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 200;                      // n. 0.65 yksikköä leveä maassa
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            var st = new TextureImporterSettings();
            ti.ReadTextureSettings(st);
            st.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            ti.SetTextureSettings(st);
            ti.SaveAndReimport();
            report.Add(n + ".png: OK");
        }
        // keräysääni HitFx-objektiin
        var fx = Object.FindFirstObjectByType<HitFx>();
        if (fx == null)
        {
            fx = new GameObject("HitFx").AddComponent<HitFx>();
            Undo.RegisterCreatedObjectUndo(fx.gameObject, "HitFx");
        }
        Undo.RecordObject(fx, "Keräysääni");
        fx.pickupSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx/setelin nosto.wav");
        EditorUtility.SetDirty(fx);
        EditorSceneManager.MarkSceneDirty(fx.gameObject.scene);
        report.Add("Keräysääni (setelin nosto.wav): " + (fx.pickupSound != null ? "OK" : "puuttuu"));
        Info(
            "Rahapudotukset:\n" + string.Join("\n", report) + "\n\nSeteli 1 mk, setelitukku 50 mk (1/25).\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Rekvisiitta: moottoripyörät ----------------
    const string BikePath = "Assets/Sprites/Rekvisiitta/moottoripyora.png";             // keula vasemmalle
    const string BikeRightPath = "Assets/Sprites/Rekvisiitta/moottoripyora_oikea.png";  // keula oikealle (pakoputket oikealla puolella)
    const string OldBikePath = "Assets/Sprites/Rekvisiitta/vanha_pyora.png";             // vanha punainen, rekvisiitta
    const string OldBikeRightPath = "Assets/Sprites/Rekvisiitta/vanha_pyora_oikea.png";
    // Ovet kadun kuvasarjassa (pikseliä sarjan vasemmasta reunasta): baari 2260, S-Club 3769.
    // Pyörät oven molemmin puolin niin, ettei oviaukko peity. (x sarjan alusta, keula oikealle?)
    // Kuvia ei peilata, koska pakoputket olisivat silloin väärällä puolella.
    static readonly (float px, bool flip)[] BikeSpots =
    {
        (2260f - 470f, false), (2260f + 300f, true),     // baarin edessä (vasen roskisten vasemmalla puolella)
        (3769f - 330f, true),  (3769f + 320f, false),    // S-Clubin edessä
    };

    [MenuItem("Beat em up/16. Lisää moottoripyörät jalkakäytävälle")]
    static void AddBikes()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        var street = GameObject.Find("Tausta");
        var sprite = ImportProp(BikePath);
        var spriteRight = ImportProp(BikeRightPath);
        var oldBike = ImportProp(OldBikePath);
        var oldBikeRight = ImportProp(OldBikeRightPath);
        if (pc == null || street == null || sprite == null)
        {
            Info( "Tarvitaan pelaaja, katutausta (kohta 4) ja kuva " + BikePath);
            return;
        }

        var old = GameObject.Find("Moottoripyörät");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = new GameObject("Moottoripyörät");
        Undo.RegisterCreatedObjectUndo(root, "Moottoripyörät");

        // kadun arvot: käytetään katualueen asetuksia, jos pelaaja on juuri klubissa
        var streetArea = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Katu");
        float curb = streetArea != null ? streetArea.curbDepthY : pc.curbDepthY;
        float wall = streetArea != null ? streetArea.maxDepthY : pc.maxDepthY;
        float kerb = streetArea != null ? streetArea.sidewalkHeight : pc.sidewalkHeight;
        float y = (curb + wall) * 0.5f;                       // keskellä jalkakäytävää

        // ajettavat pyörät: nousu- ja ajokuvat sekä käynnistysääni
        Sprite[] ride = new Sprite[0], mount = new Sprite[0];
        foreach (var n in new[] { "pratka_ajo", "pratka_nousu", "pratka_kiskaisu" })
        {
            string tp = FindTexture(n);
            if (tp != null) SetupAndSlice(tp);
        }
        ride = LoadSprites("pratka_ajo").OrderBy(sp => int.TryParse(sp.name.Substring(sp.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
        var grabR = LoadSprites("pratka_kiskaisu").OrderBy(sp => int.TryParse(sp.name.Substring(sp.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
        mount = LoadSprites("pratka_nousu").OrderBy(sp => int.TryParse(sp.name.Substring(sp.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
        var startClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx/pratka_kaynnistys.mp3");
        var engineClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx/pratka_kaynti.wav");
        var boostClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx/pratka_kiihdytys.mp3");
        // erilliset vanteet (pyörivät koodilla)
        Sprite LoadWheel(string n)
        {
            string wp = FindTexture(n);
            if (wp == null) return null;
            var wti = AssetImporter.GetAtPath(wp) as TextureImporter;
            wti.textureType = TextureImporterType.Sprite;
            wti.spriteImportMode = SpriteImportMode.Single;
            wti.spritePixelsPerUnit = 100;
            wti.filterMode = FilterMode.Bilinear;
            wti.textureCompression = TextureImporterCompression.Uncompressed;
            wti.mipmapEnabled = false;
            wti.alphaIsTransparency = true;
            var wst = new TextureImporterSettings(); wti.ReadTextureSettings(wst);
            wst.spriteAlignment = (int)SpriteAlignment.Center; wti.SetTextureSettings(wst);
            wti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(wp);
        }
        var rearW = LoadWheel("pratka_vanne_taka");
        var frontW = LoadWheel("pratka_vanne_etu");
        var rearT = LoadWheel("pratka_kumi_taka");
        var frontT = LoadWheel("pratka_kumi_etu");

        var ssr = street.GetComponent<SpriteRenderer>();
        float left = street.transform.position.x - ssr.size.x * 0.5f;
        float setU = StreetSetPx / BackgroundPPU;
        int sets = Mathf.RoundToInt(ssr.size.x / setU);
        int count = 0;
        for (int s = 0; s < sets; s++)
            foreach (var spot in BikeSpots)
            {
                var go = new GameObject("Moottoripyörä " + (++count));
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(left + spot.px / BackgroundPPU + s * setU, y, 0f);
                var vis = new GameObject("Visual");
                vis.transform.SetParent(go.transform, false);
                vis.transform.localPosition = new Vector3(0f, kerb, 0f);   // jalkakäytävän pinnalla
                var sr = vis.AddComponent<SpriteRenderer>();
                sr.sortingOrder = Mathf.RoundToInt(-y * 100f);            // sama syvyysjärjestys kuin hahmoilla
                // joka toinen kadun pyörä on vanha punainen (pelkkä rekvisiitta)
                if (count % 2 == 0 && oldBike != null)
                {
                    sr.sprite = spot.flip && oldBikeRight != null ? oldBikeRight : oldBike;
                    sr.flipX = spot.flip && oldBikeRight == null;
                    continue;
                }
                // oikealle osoittava pyörä omasta kuvastaan; jos sitä ei ole, peilataan varalta
                sr.sprite = spot.flip && spriteRight != null ? spriteRight : sprite;
                sr.flipX = spot.flip && spriteRight == null;
                if (ride.Length > 0)
                {
                    var mb = go.AddComponent<Motorbike>();
                    mb.parked = sr;
                    mb.parkedLeft = sprite;
                    mb.parkedRight = spriteRight;
                    mb.rideSprites = ride;
                    mb.mountSprites = mount;
                    mb.startSound = startClip;
                    mb.engineLoop = engineClip; mb.boostSound = boostClip;
                    mb.rearWheel = rearW; mb.frontWheel = frontW; mb.rearTyre = rearT; mb.frontTyre = frontT;
                    mb.maxSpeed = 27.5f; mb.acceleration = 18f;   // kaksinkertainen huippunopeus
                    mb.rearWheelPos = new Vector2(-2.176f, 0.824f); mb.frontWheelPos = new Vector2(2.133f, 0.837f);   // uusi chopper (3D-malli) + kuski
                    mb.grabSprites = grabR;
                    mb.rideable = false;              // kadulla vain rekvisiittaa; ajettava on takakujan parkkipaikalla
                }
            }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Info(
            $"{count} moottoripyörää jalkakäytävällä baarien ja S-Clubien edessä (rekvisiittaa, joka toinen vanha punainen).\nPelaaja kulkee niiden edestä ja takaa syvyyden mukaan.\n" +
            $"Ajettavat: ajokuvat {ride.Length}, nousukuvat {mount.Length}, käynnistysääni {(startClip != null ? "OK" : "puuttuu")}.\nPyörän vieressä E: nouse kyytiin.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/17. Päivitä sivupotku")]
    static void UpdateSideKick()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { Info( "Scenessä ei ole pelaajaa."); return; }
        Sprite[] sp = LoadSprites("sivupotku")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        if (sp.Length == 0) { Info( "sivupotku.png puuttuu tai sitä ei ole leikattu (kohta 1)."); return; }
        Undo.RecordObject(pc, "Sivupotku");
        pc.sideKickSprites = sp;
        pc.sideKickFrameTime = 0.06f;
        pc.sideKickImpactFrame = 3;
        pc.sideKickImpactHold = 0.14f;
        pc.sideKickArtOffset = 0.43f;     // sheetin kuvat siirretty 43 px vasemmalle
        EditorUtility.SetDirty(pc);
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        Info(
            $"Sivupotku: {sp.Length} kuvaa.\nK-nappi vuorottelee tavallisen potkun ja sivupotkun välillä.\n\nTallenna scene (Ctrl+S).");
    }

    /// Rekvisiittakuvan tuonti: yksi sprite, 100 px/yksikkö, kiinnitys alareunan keskelle.
    static Sprite ImportProp(string path)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return null;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 100;
        ti.filterMode = FilterMode.Bilinear;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        var st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        st.spriteAlignment = (int)SpriteAlignment.BottomCenter;
        ti.SetTextureSettings(st);
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    /// Kuten ImportProp, mutta pivot keskellä (pallot, keppi pöydän päällä).
    static Sprite ImportPropCentered(string path)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return null;
        ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 100; ti.filterMode = FilterMode.Bilinear;
        ti.textureCompression = TextureImporterCompression.Uncompressed; ti.mipmapEnabled = false; ti.alphaIsTransparency = true;
        var st = new TextureImporterSettings(); ti.ReadTextureSettings(st);
        st.spriteAlignment = (int)SpriteAlignment.Center; ti.SetTextureSettings(st);
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    [MenuItem("Beat em up/18. Päivitä heron heittokuvat")]
    static void UpdateHeroThrown()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { Info( "Scenessä ei ole pelaajaa."); return; }
        Sprite[] sp = LoadSprites("hero_heitetty")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        if (sp.Length < 8) { Info( $"hero_heitetty.png: {sp.Length}/8 kuvaa. Tee ensin kohta 1."); return; }
        Undo.RecordObject(pc, "Heittokuvat");
        pc.thrownSprites = sp;
        pc.kipUpSprites = LoadSprites("hero_kipup")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        EditorUtility.SetDirty(pc);
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        Info( $"Heron heittokuvat: {sp.Length} kuvaa.\nKip-up-nousu: {pc.kipUpSprites.Length} kuvaa.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/19. Päivitä pusku ja suojaus")]
    static void UpdatePushAndBlock()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { Info( "Scenessä ei ole pelaajaa."); return; }
        Sprite[] push = LoadSprites("pusku")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        Sprite[] block = LoadSprites("suojaus")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        if (push.Length < 6 || block.Length < 5)
        {
            Info( $"pusku.png: {push.Length}/6 kuvaa, suojaus.png: {block.Length}/5 kuvaa. Tee ensin kohta 1.");
            return;
        }
        Undo.RecordObject(pc, "Pusku ja suojaus");
        pc.pushSprites = push;
        pc.pushFrameTime = 0.06f;
        pc.pushImpactFrame = 3;          // kuva 4: olkapää edellä, täysi syöksy
        pc.pushImpactHold = 0.14f;
        pc.blockSprites = block;
        EditorUtility.SetDirty(pc);
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        Info(
            $"Pusku: {push.Length} kuvaa (U, ohjaimessa RT, tai lyönti suojauksesta). Kaataa vihollisen.\n" +
            $"Suojaus: {block.Length} kuvaa (pidä I, ohjaimessa LT). Torjuu edestä tulevat lyönnit, ei heittoja.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/118. Romuttamo: alue ja portti Aavikkokadulta (avain tulee piirakkasodan voitosta)")]
    static void CreateJunkyard()
    {
        if (PlayModeBlocked()) return;
        const string path = "Assets/Sprites/Taustat/romuttamo_panoraama.png";   // 5536 x 1110: neljä kuvaa limitettynä (romuttamo_1-4.png)
        const string skyPath = "Assets/Sprites/Taustat/aavikko_parallax_ylempi_loop.png";
        var street = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Aavikkokatu");
        if (street == null || AssetImporter.GetAtPath(path) == null)
        { Info("Tee ensin Aavikkokatu (95). Tarvitaan romuttamo_panoraama.png (Sprites/Taustat)."); return; }
        const float ppu = 941f / 12f;
        var bgSprite = ImportShipSprite(path, ppu, false, SpriteAlignment.Center);
        float panoH = bgSprite.rect.height;
        const float left = 92000f, camCenterY = 1.4f, camSize = 6.4f, windowTopRow = 92f;   // 91000 on Aavikkobaarin alue: oma alue ettei kalusteet ja tausta näy päällekkäin
        float width = bgSprite.bounds.size.x, centerX = left + width * 0.5f;
        float windowH = camSize * 2f * ppu;
        // kuvan rivi -> maailman y (ikkunan keskirivi on kameran keskellä)
        float Y(float row) => camCenterY + (windowTopRow + windowH * 0.5f - row) / ppu;
        float PanoX(float px) => left + px / ppu;

        var yard = ReuseArea("Alue: Romuttamo");
        Undo.RecordObject(yard, "Romuttamo");
        yard.areaName = "Romuttamo"; yard.useSidewalk = false;
        yard.minDepthY = Y(1010f); yard.maxDepthY = Y(815f);
        float halfScreen = camSize * 16f / 9f;
        yard.camMinX = left + halfScreen; yard.camMaxX = left + width - halfScreen; yard.camSize = camSize;
        yard.camOffsetY = camCenterY - CamY; yard.camRiseY = 0f;
        yard.walkMinX = left + 0.8f; yard.walkMaxX = left + width - 0.8f;
        yard.depthLimits = new Vector2[0];
        yard.backScale = 1f;
        yard.music = street.music; yard.ambience = street.ambience; yard.ambienceVolume = street.ambienceVolume;
        foreach (var name in new[] { "Romuttamon sisalto", "Aavikkokadulta romuttamolle" })
        { var old = GameObject.Find(name); if (old != null) Undo.DestroyObjectImmediate(old); }
        var root = new GameObject("Romuttamon sisalto");
        Undo.RegisterCreatedObjectUndo(root, "Romuttamo");
        var bg = new GameObject("Romuttamon tausta").AddComponent<SpriteRenderer>();
        bg.transform.SetParent(root.transform, false);
        bg.transform.position = new Vector3(centerX, Y(panoH * 0.5f), 0f);
        bg.sprite = bgSprite; bg.sortingOrder = -10000;
        // taivas: aavikon ylempi parallax kuvan takana (kuvan taivas on läpinäkyvä)
        var sky = ImportShipSprite(skyPath, 100f, true, SpriteAlignment.Center);
        if (sky != null)
        {
            var s = new GameObject("Romuttamon taivas").AddComponent<SpriteRenderer>();
            s.transform.SetParent(root.transform, false);
            s.transform.position = new Vector3(left + width * 0.5f, camCenterY + 4.4f, 0f);   // mesat rakennusten yläpuolelle (kuten Aavikkokadulla: kameran keskikohdan yläpuolella 4,4)
            s.sprite = sky; s.sortingOrder = -10200;
            float skyK = camSize * 2f * 1.15f / sky.bounds.size.y;   // kuvan suhteet säilytetään
            s.transform.localScale = new Vector3(skyK, skyK, 1f);
            s.drawMode = SpriteDrawMode.Tiled; s.tileMode = SpriteTileMode.Continuous;
            s.size = new Vector2((2f * halfScreen + (yard.camMaxX - yard.camMinX) * 0.04f + 4f) / skyK, sky.bounds.size.y);
            var par = s.gameObject.AddComponent<ParallaxLayer>();
            par.factor = 0.04f;
            par.startCamX = yard.camMinX; par.minX = left; par.maxX = left + width;
        }
        // kaukainen hiekka mesojen alla (kuten Aavikkokadulla: horisontti kameran keskikohdan yläpuolella 0,8)
        var sand = ImportShipSprite("Assets/Sprites/Taustat/aavikko_parallax_alempi_loop.png", 100f, true, SpriteAlignment.Center);
        if (sand != null)
        {
            var d = new GameObject("Romuttamon kaukainen hiekka").AddComponent<SpriteRenderer>();
            d.transform.SetParent(root.transform, false);
            d.transform.position = new Vector3(left + width * 0.5f, camCenterY + 0.8f, 0f);
            d.sprite = sand; d.sortingOrder = -10100;
            float sandK = 12f / sand.bounds.size.y;
            d.transform.localScale = new Vector3(sandK, sandK, 1f);
            d.drawMode = SpriteDrawMode.Tiled; d.tileMode = SpriteTileMode.Continuous;
            d.size = new Vector2((2f * halfScreen + (yard.camMaxX - yard.camMinX) * 0.08f + 4f) / sandK, sand.bounds.size.y);
            var dpar = d.gameObject.AddComponent<ParallaxLayer>();
            dpar.factor = 0.08f;
            dpar.startCamX = yard.camMinX; dpar.minX = left; dpar.maxX = left + width;
        }

        // portti Aavikkokadulla: jatkokuva 3 (aavikkokatu3), portin keskikohta kuvassa n. 1084 px
        const float gatePx = 1084f, streetPpu = 887f / 12f;
        float streetGateX = 86000f + (2917f + gatePx) / streetPpu;
        Vector2 arrival = new Vector2(PanoX(560f), Y(900f));
        var enter = new GameObject("Aavikkokadulta romuttamolle").AddComponent<Door>();
        Undo.RegisterCreatedObjectUndo(enter.gameObject, "Romuttamon portti");
        enter.here = street; enter.target = yard; enter.spawnPoint = arrival;
        enter.prompt = "Mene romuttamolle"; enter.halfWidth = 2.6f; enter.maxDistanceFromWall = 1.2f;
        enter.needsJunkyardKey = true;   // lukossa, kunnes piirakkasota on voitettu (Linsey antaa avaimen)
        enter.transform.position = new Vector3(streetGateX, street.maxDepthY, 0f);

        // uloskäynti: romuttamon oma portti kuvan vasemmassa reunassa (keskikohta n. 205 px)
        var back = new GameObject("Romuttamolta kadulle").AddComponent<Door>();
        back.transform.SetParent(root.transform, false);
        float exitX = PanoX(205f);
        back.transform.position = new Vector3(exitX, yard.MaxDepthAtX(exitX, yard.maxDepthY), 0f);
        back.here = yard; back.target = street; back.returnToLastDoor = false;
        back.prompt = "Mene ulos kadulle"; back.halfWidth = 2.4f; back.maxDistanceFromWall = 2.0f;
        back.spawnPoint = new Vector2(streetGateX, street.maxDepthY - 0.3f);
        EditorUtility.SetDirty(yard); EditorSceneManager.MarkSceneDirty(root.scene);
        Info("Romuttamo lisätty. Portti Aavikkokadulla (jatkokuva 3, JUNKYARD-kyltti) on lukossa, kunnes piirakkasota on voitettu: sen jälkeen Linsey antaa avaimen ja E avaa portin. Sisällä ulos pääsee vasemmasta portista.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/117. Roccon päät yhteen (O + U, vihut molemmin puolin)")]
    static void SetupHeadCrush()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { Info("Scenessä ei ole pelaajaa."); return; }
        const string path = "Assets/Sprites/paat_yhteen.png";
        if (AssetImporter.GetAtPath(path) == null) { Info("Puuttuu: " + path); return; }
        int cut = SetupAndSlice(path);   // 5 kuvaa, 512 × 384
        Sprite[] sp = LoadSprites("paat_yhteen")
            .OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        if (sp.Length < 5) { Info($"paat_yhteen.png: {sp.Length}/5 kuvaa (leikkaus: {cut})."); return; }
        Undo.RecordObject(pc, "Päät yhteen");
        pc.headCrushSprites = sp;
        EditorUtility.SetDirty(pc);
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        Info("Päät yhteen (Rocco): " + sp.Length + " kuvaa.\nPaina O + U yhtä aikaa (ohjaimella LT + RT). Kun vihut ovat molemmin puolin lähellä, Rocco ottaa kiinni ja kolauttaa päät yhteen. Ilman vihuja liike jää tyhjäksi.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/20. Päivitä vastaheitto")]
    static void UpdateCounterThrow()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { Info( "Scenessä ei ole pelaajaa."); return; }
        Sprite[] sp = LoadSprites("heitto")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        if (sp.Length < 7) { Info( $"heitto.png: {sp.Length}/8 kuvaa. Tee ensin kohta 1."); return; }
        Undo.RecordObject(pc, "Vastaheitto");
        pc.counterThrowSprites = sp;
        EditorUtility.SetDirty(pc);
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        Info(
            $"Vastaheitto: {sp.Length} kuvaa.\nPaina O (ohjaimessa oikean tatin painallus) juuri kun vihollinen lyö:\n" +
            "ukko nappaa kädestä, vetää olan yli ja heittää selän taakse.\nOhi mennyt kurotus jättää hetkeksi alttiiksi.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/21. Päivitä vastaheitot (Kovis kuperkeikka, Punkkari niskalenkki)")]
    static void UpdateMonkeyFlip()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { Info( "Scenessä ei ole pelaajaa."); return; }
        Sprite[] sp = LoadSprites("kuperkeikka")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        if (sp.Length < 8) { Info( $"kuperkeikka.png: {sp.Length}/8 kuvaa. Tee ensin kohta 1."); return; }
        Undo.RecordObject(pc, "Kuperkeikkaheitto");
        pc.monkeyFlipSprites = sp;
        EditorUtility.SetDirty(pc);
        // Koviksille iso-merkintä (kuperkeikka), punkkareille ei (niskalenkki)
        int big = 0, small = 0;
        var sheetReport = new List<string>();
        Sprite[] kovisFlip = EnemySheet("vihu_kuperkeikka", sheetReport);
        Sprite[] punkHeadlock = EnemySheet("punk_niskalenkki", sheetReport);
        Sprite[] punkFlight = EnemySheet("punk_niskalenkki_lento", sheetReport);
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            Undo.RecordObject(e, "Iso vastus");
            e.bigBody = e.gameObject.name.StartsWith("Kovis");
            if (e.bigBody) e.flipThrownSprites = kovisFlip;   // Koviksen oma lento ja alastulo
            else if (e.gameObject.name.StartsWith("Punkkari")) { e.headlockThrownSprites = punkHeadlock; e.headlockFlightSprites = punkFlight; }
            if (e.bigBody) big++; else small++;
            EditorUtility.SetDirty(e);
        }
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        Info(
            $"Kuperkeikkaheitto: {sp.Length} kuvaa (+ kip-up lopuksi).\n" +
            $"Isoja vastuksia (Kovis, kuperkeikka): {big}\nMuita (niskalenkki): {small}\n" +
            $"Vihollisten omat kuvat: {string.Join(", ", sheetReport)}\n\n" +
            "Sama nappi O: ohjelma valitsee heiton vastuksen koon mukaan.\nIson vastuksen voi merkitä myös käsin Enemy-komponentin Big Body -ruudulla.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/22. Päivitä hyppy ja heittojen lento")]
    static void UpdateJumpAndFlip()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { Info( "Scenessä ei ole pelaajaa."); return; }
        Undo.RecordObject(pc, "Hyppy ja heitto");
        pc.jumpVelocity = 12.5f;     // n. 2.6 yksikköä korkea hyppy (ennen 2.0)
        pc.monkeyFlipSpeed = 12f;    // korkea ja pitkä lento
        pc.monkeyFlipUp = 10f;
        pc.counterThrowSpeed = 9f;   // niskalenkki: korkea kaari
        pc.counterThrowUp = 9f;
        EditorUtility.SetDirty(pc);
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        float h = pc.jumpVelocity * pc.jumpVelocity / (2f * pc.gravity);
        Info(
            $"Hypyn korkeus n. {h:0.0} yksikköä (Jump Velocity {pc.jumpVelocity}).\n" +
            $"Kuperkeikka: nopeus {pc.monkeyFlipSpeed}, nousu {pc.monkeyFlipUp}.\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Puulaatikot ----------------
    const string CratePath = "Assets/Sprites/Rekvisiitta/laatikko.png";
    const string CrateBurstPath = "Assets/Sprites/Rekvisiitta/laatikko_sirpaleet.png";
    const float CrateSpacing = 9f;   // laatikoiden väli kävelykadulla (yksikköä)

    [MenuItem("Beat em up/23. Lisää puulaatikot kävelykadulle")]
    static void AddCrates()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        var street = GameObject.Find("Tausta");
        if (pc == null || street == null || AssetImporter.GetAtPath(CratePath) == null)
        {
            Info( "Tarvitaan pelaaja, katutausta (kohta 4) ja kuva " + CratePath);
            return;
        }
        SetupAndSlice(CratePath);
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(CratePath).OfType<Sprite>()
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        Sprite burst = null;
        var bti = AssetImporter.GetAtPath(CrateBurstPath) as TextureImporter;
        if (bti != null)
        {
            bti.textureType = TextureImporterType.Sprite;
            bti.spriteImportMode = SpriteImportMode.Single;
            bti.spritePixelsPerUnit = 200;
            bti.filterMode = FilterMode.Bilinear;
            bti.textureCompression = TextureImporterCompression.Uncompressed;
            bti.mipmapEnabled = false;
            bti.alphaIsTransparency = true;
            var st = new TextureImporterSettings();
            bti.ReadTextureSettings(st);
            st.spriteAlignment = (int)SpriteAlignment.Center;
            bti.SetTextureSettings(st);
            bti.SaveAndReimport();
            burst = AssetDatabase.LoadAssetAtPath<Sprite>(CrateBurstPath);
        }

        // pelaajan nosto- ja heittokuvat, jos ne on jo lisätty
        Sprite[] carry = LoadSprites("nosto_heitto")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        foreach (var n in new[] { "kanto", "kanto_kavely" })
        {
            string p = FindTexture(n);
            if (p != null) SetupAndSlice(p);
        }
        Sprite carryPose = LoadSprites("kanto").FirstOrDefault();
        Sprite[] carryWalk = LoadSprites("kanto_kavely")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        Undo.RecordObject(pc, "Nostokuvat");
        pc.carrySprites = carry;
        pc.carryWalkSprites = carryWalk;
        pc.carryPoseSprite = carryPose;
        pc.carryHeight = 3.45f;          // laatikko kämmenten päällä (kanto.png)
        EditorUtility.SetDirty(pc);

        var old = GameObject.Find("Laatikot");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = new GameObject("Laatikot");
        Undo.RegisterCreatedObjectUndo(root, "Laatikot");

        // kävelykadun keskikohta syvyyssuunnassa (kuten moottoripyörillä)
        var streetArea = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Katu");
        float curb = streetArea != null ? streetArea.curbDepthY : pc.curbDepthY;
        float wall = streetArea != null ? streetArea.maxDepthY : pc.maxDepthY;
        float y = (curb + wall) * 0.5f;

        // vältetään moottoripyörät ja ovet
        var blocked = StreetObstacles(false);
        const float crateHalf = 0.95f;     // laatikon puolileveys 20 % isompana

        var ssr = street.GetComponent<SpriteRenderer>();
        float left = street.transform.position.x - ssr.size.x * 0.5f;
        float right = street.transform.position.x + ssr.size.x * 0.5f;
        var ext = GameObject.Find("Tausta jatko");      // laatikot myös kadun jatkolle
        if (ext != null) right = Mathf.Max(right, ext.GetComponent<SpriteRenderer>().bounds.max.x);
        int count = 0;
        for (float x = left + 7f; x < right - 4f; x += CrateSpacing)
        {
            float cx = x;
            // siirretään vähän sivuun, jos kohdalla on pyörä tai ovi
            for (int tries = 0; tries < 6 && Blocked(blocked, cx, crateHalf); tries++) cx += 1.2f;
            if (Blocked(blocked, cx, crateHalf)) continue;

            var go = new GameObject("Laatikko " + (++count));
            go.transform.SetParent(root.transform, false);
            go.transform.position = new Vector3(cx, y, 0f);
            var vis = new GameObject("Visual").AddComponent<SpriteRenderer>();
            vis.transform.SetParent(go.transform, false);
            var sh = new GameObject("Shadow").AddComponent<SpriteRenderer>();
            sh.transform.SetParent(go.transform, false);
            var c = go.AddComponent<Crate>();
            c.sprites = sprites;
            c.burstSprite = burst;
            c.visualScale = 1.2f;          // 20 % isompi
            c.hitRadiusX = 1.05f;
            c.body = vis;
            c.shadow = sh;
            vis.sprite = sprites.Length > 0 ? sprites[0] : null;
            vis.transform.localPosition = new Vector3(0f, pc.sidewalkHeight - c.footOffset, 0f);
        }
        // välillä kaksi laatikkoa päällekkäin
        var bases = root.GetComponentsInChildren<Crate>().ToArray();
        for (int i = 1; i < bases.Length; i += 3)
        {
            var top = Object.Instantiate(bases[i].gameObject, root.transform);
            top.name = bases[i].gameObject.name + " (päällä)";
            top.transform.position = bases[i].transform.position + new Vector3(0.08f, -0.02f, 0f);   // hieman edessä: piirretään päälle
            top.GetComponent<Crate>().stackedOn = bases[i];
            count++;
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Info(
            $"{count} puulaatikkoa kävelykadulla {CrateSpacing:0} yksikön välein ({sprites.Length} kuvaa, sirpaleet: {(burst != null ? "OK" : "puuttuu")}).\n" +
            $"Pelaajan nosto- ja heittokuvat (nosto_heitto.png): {(carry.Length > 0 ? carry.Length + " kuvaa" : "puuttuu, käytetään varakuvia")}\n" +
            $"Kantoasento (kanto.png): {(carryPose != null ? "OK" : "puuttuu")}, kantokävely (kanto_kavely.png): {(carryWalk.Length > 0 ? carryWalk.Length + " kuvaa" : "puuttuu")}\n\n" +
            "O laatikon vieressä nostaa, lyönti/potku heittää. Kolme iskua hajottaa.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/24. Päivitä potkukombo (matala, etupotku, korkea)")]
    static void UpdateKickCombo()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { Info( "Scenessä ei ole pelaajaa."); return; }
        string path = FindTexture("korkea_potku");
        if (path != null) SetupAndSlice(path);
        Sprite[] sp = LoadSprites("korkea_potku")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        if (sp.Length < 4) { Info( "korkea_potku.png puuttuu."); return; }
        Undo.RecordObject(pc, "Potkukombo");
        pc.hiKickSprites = sp;
        pc.hiKickFrameTime = 0.05f;
        pc.hiKickImpactFrame = 3;
        pc.hiKickImpactHold = 0.14f;
        pc.hiKickArtOffset = 0.64f;     // sheetin kuvat siirretty 64 px vasemmalle
        EditorUtility.SetDirty(pc);
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        Info(
            $"Korkea potku: {sp.Length} kuvaa.\nHakkaa K: matala potku → etupotku → korkea potku (kaataa).\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/25. Päivitä iskujen tuntuma")]
    static void UpdateHitFeel()
    {
        var fx = Object.FindFirstObjectByType<HitFx>();
        if (fx == null) { fx = new GameObject("HitFx").AddComponent<HitFx>(); Undo.RegisterCreatedObjectUndo(fx.gameObject, "HitFx"); }
        Undo.RecordObject(fx, "Iskujen tuntuma");
        fx.lightHitstop = 0.08f;   // ennen 0.05
        fx.heavyHitstop = 0.16f;   // ennen 0.10
        fx.lightShake = 0.08f;
        fx.heavyShake = 0.22f;
        EditorUtility.SetDirty(fx);
        // lyöntikombon liuku eteenpäin pidemmäksi
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null && pc.punchCombo != null)
        {
            Undo.RecordObject(pc, "Liuku");
            float[] lunges = { 0.18f, 0.26f, 0.18f, 0.30f };
            for (int i = 0; i < pc.punchCombo.Length && i < lunges.Length; i++) pc.punchCombo[i].lunge = lunges[i];
            EditorUtility.SetDirty(pc);
        }
        EditorSceneManager.MarkSceneDirty(fx.gameObject.scene);
        Info(
            "Osumapysäytys: kevyt 0.08 s, raskas 0.16 s.\nTärähdys: kevyt 0.08, raskas 0.22.\nLyöntikombon liuku: 0.18 / 0.26 / 0.18 / 0.30.\nPotkujen liuku ja osumaläiskät ovat koodissa.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/26. Päivitä saksipotku (eteen-ylös + K)")]
    static void UpdateScissorKick()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { Info( "Scenessä ei ole pelaajaa."); return; }
        string path = FindTexture("saksipotku");
        if (path != null) SetupAndSlice(path);
        Sprite[] sp = LoadSprites("saksipotku")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        if (sp.Length < 8) { Info( $"saksipotku.png: {sp.Length}/8 kuvaa."); return; }
        Undo.RecordObject(pc, "Saksipotku");
        pc.scissorSprites = sp;
        EditorUtility.SetDirty(pc);
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        Info(
            "Saksipotku: eteen-ylös + K (maasta) tai hyppy + K (ilmassa).\nKaksi potkua vuorojaloin, toinen kaataa. Hyppy + J = vanha hyppypotku.\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Lippis (uusi vihollinen) ----------------
    // Kadulla (x pelaajan aloituskohdasta, syvyys y)
    static readonly Vector2[] LippisStreet = { new Vector2(26f, -2.6f), new Vector2(50f, -3.1f), new Vector2(80f, -2.2f), new Vector2(105f, -3.0f), new Vector2(125f, -2.0f), new Vector2(155f, -2.8f) };

    [MenuItem("Beat em up/27. Lisää Lippikset (uusi vihollinen)")]
    static void AddLippis()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { Info( "Scenessä ei ole pelaajaa."); return; }
        foreach (var n in new[] { "lippis_idle", "lippis_kavely", "lippis_lyonti", "lippis_potku", "lippis_osuma", "lippis_kaatuminen", "lippis_ylosnousu", "lippis_niskalenkki" })
        {
            string path = FindTexture(n);
            if (path != null) SetupAndSlice(path);
        }
        var report = new List<string>();
        Sprite[] idle = EnemySheet("lippis_idle", report);
        if (idle.Length == 0) { Info( "lippis_idle.png puuttuu."); return; }

        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            if (e.gameObject.name.StartsWith("Lippis")) Undo.DestroyObjectImmediate(e.gameObject);

        var go = new GameObject("Lippis");
        var visual = new GameObject("Visual").AddComponent<SpriteRenderer>();
        visual.transform.SetParent(go.transform, false);
        var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>();
        shadow.transform.SetParent(go.transform, false);
        var t = go.AddComponent<Enemy>();
        t.body = visual; t.shadow = shadow; visual.sprite = idle[0];
        t.displayName = "Lippis";
        t.idleSprites = idle;
        t.walkSprites = EnemySheet("lippis_kavely", report);
        t.walkFrameTime = 0.083f;          // videon oma tahti: 11 kuvaa, 0.92 s askelsykli
        t.punchSprites = EnemySheet("lippis_lyonti", report);
        t.altAttackSprites = EnemySheet("lippis_potku", report);   // potku toisena hyökkäyksenä
        t.knockdownSprites = EnemySheet("lippis_kaatuminen", report);   // pyörähtää ja kaatuu kasvoilleen
        t.getUpSprites = EnemySheet("lippis_ylosnousu", report);
        t.hurtSprites = EnemySheet("lippis_osuma", report);
        t.headlockThrownSprites = EnemySheet("lippis_niskalenkki", report);   // pelaajan niskalenkki (kun kuvat on lisätty)
        t.idleFrameTime = 0.14f;
        // askelpituus kävelykuvissa n. 1.5 yksikköä, kaksi askelta 0.92 s:ssa -> 3.2 yks/s (jalat eivät liu'u)
        t.moveSpeedX = 3.2f;
        t.moveSpeedY = 1.9f;
        t.maxHealth = 75;                  // ennen 55
        t.punchDamage = 8;
        t.punchImpactFrame = 3;            // käsi suorana kuvassa 4
        t.altImpactFrame = 4;              // potku ojennettuna kuvassa 5
        t.altDamage = 13;
        t.altChance = 0.4f;
        t.altReach = 2.4f;
        t.attackCooldown = 1.2f;
        t.hurtSounds = LoadClips("Assets/Audio/big thug", "gasp");
        t.hurtVolume = 0.99f;
        Undo.RegisterCreatedObjectUndo(go, "Lippis");

        float x0 = pc.transform.position.x;
        for (int i = 0; i < LippisStreet.Length; i++)
        {
            var v = LippisStreet[i];
            var c = i == 0 ? go : Object.Instantiate(go);
            if (i > 0) { c.name = "Lippis_" + (i + 1); Undo.RegisterCreatedObjectUndo(c, "Lippis"); }
            c.transform.position = new Vector3(x0 + v.x, Mathf.Clamp(v.y, pc.minDepthY, pc.maxDepthY), 0f);
        }
        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        Info(
            $"Lippiksiä kadulla: {LippisStreet.Length}\n\n" + string.Join("\n", report) +
            "\n\nLyö ja potkaisee. Puuttuvat kuvat korvataan varaliikkeillä.\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Portsari ----------------

    /// Portsarin heitto (portsari_heittaa.png, 8 kuvaa): tarttuu rinnuksista, vetää lähelle, kääntyy ja viskaa olan yli selän taakse kauas.
    /// Heittää kaikkia (heron ja vihut); isot hahmot lentävät lyhyemmälle.
    static void SetBouncerThrow(Enemy e, List<string> report)
    {
        string path = FindTexture("portsari_heittaa");
        if (path == null) return;
        SetupAndSlice(path);
        var g = EnemySheet("portsari_heittaa", report);
        if (g.Length < 8) return;
        e.grabSprites = g;                 // 0 kurotus, 1 ote, 2–3 veto, 4 pito, 5 olan yli, 6 heitto, 7 heiton jälkeen (katse heittosuuntaan)
        e.grabChance = 0.3f; e.grabRange = 1.4f;
        e.grabReachTime = 0.25f; e.grabLiftTime = 0.6f;
        e.throwDamage = 16;
        e.throwForward = false; e.throwTurnsAround = true;
        e.throwSpeed = 10f; e.throwUp = 6f;          // lentää kauas
        e.grabsAnyone = true; e.heavyThrowScale = 0.55f;
        e.collarThrow = true;              // rinnuksista, ei nostoa pään yli: viskaa olan yli taakse
    }

    [MenuItem("Beat em up/74. Portsarin heitto kaikille portsareille")]
    static void BouncerThrowAll()
    {
        var report = new List<string>();
        int n = 0;
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (e == null || e.displayName != "Portsari") continue;
            Undo.RecordObject(e, "Portsarin heitto");
            SetBouncerThrow(e, report);
            EditorUtility.SetDirty(e); n++;
        }
        if (n > 0) EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Info($"Portsarin heitto: {n} portsaria.\n\n" + string.Join("\n", report.Distinct()) + "\n\nTallenna scene (Ctrl+S).");
    }
    const int BouncerCount = 6;   // tulevat molemmista suunnista (ovelta ja oikeasta reunasta), kun klubissa alkaa ensimmäinen tappelu

    [MenuItem("Beat em up/43. Portsarit S-Clubiin")]
    static void AddBouncers()
    {
        var club = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "S-Club");
        if (club == null) { Info("Tarvitaan S-Clubin sisätila (kohta 10)."); return; }
        foreach (var n in new[] { "portsari_idle", "portsari_kavely", "portsari_lyonnit", "portsari_potku", "portsari_osuma", "portsari_kaatuminen", "portsari_ylosnousu", "portsari_heitto" })
        {
            string path = FindTexture(n);
            if (path != null) SetupAndSlice(path);
        }
        var report = new List<string>();
        Sprite[] idle = EnemySheet("portsari_idle", report);
        if (idle.Length == 0) { Info("portsari_idle.png puuttuu."); return; }
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (e != null && e.gameObject.name.StartsWith("Portsari")) Undo.DestroyObjectImmediate(e.gameObject);
        var oldSquad = Object.FindObjectsByType<BouncerSquad>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var sq in oldSquad) Undo.DestroyObjectImmediate(sq.gameObject);

        var go = new GameObject("Portsari");
        var visual = new GameObject("Visual").AddComponent<SpriteRenderer>();
        visual.transform.SetParent(go.transform, false);
        var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>();
        shadow.transform.SetParent(go.transform, false);
        var t = go.AddComponent<Enemy>();
        t.body = visual; t.shadow = shadow; visual.sprite = idle[0];
        t.displayName = "Portsari";
        t.visualScale = 1f;
        // taistelukuvissa hahmo on piirretty tanakammaksi kuin kävelyssä: koon voi hienosäätää pelin aikana Inspectorissa
        t.attackArtScale = 1f;
        t.knockArtScale = 1f;
        t.walkArtScale = 1f;
        t.idleSprites = idle;                                  // 8 kuvaa
        t.idleFrameTime = 0.16f;
        t.walkSprites = EnemySheet("portsari_kavely", report); // 12 kuvaa videosta, 1 s askelsykli
        t.walkFrameTime = 0.083f;
        t.punchSprites = EnemySheet("portsari_lyonnit", report);   // jab (0–4) ja heti perään takasuora (5–9)
        t.punchImpactFrame = 3;            // jab ojennettuna
        t.secondImpactFrame = 7;           // takasuora ojennettuna
        t.windupTime = 0.25f;
        t.punchRecoverTime = 0.55f;
        t.punchDamage = 9;
        t.altAttackSprites = EnemySheet("portsari_potku", report);  // etupotku (kuva 4 ojennettuna)
        t.altImpactFrame = 3;
        t.altDamage = 15;
        t.altKnockdown = true;             // potku kaataa
        t.altChance = 0.35f;
        t.altReach = 2.5f;
        t.attackRange = 2.0f;
        t.moveSpeedX = 2.3f;               // iso ja hidas, mutta kestää
        t.moveSpeedY = 1.4f;
        t.maxHealth = 130;
        t.attackCooldown = 1.3f;
        t.hurtSounds = LoadClips("Assets/Audio/big thug", "gasp");
        t.hurtVolume = 0.99f;
        t.hurtSprites = EnemySheet("portsari_osuma", report);             // 3 kuvaa: isku leukaan, pää taakse
        t.knockdownSprites = EnemySheet("portsari_kaatuminen", report);   // 7 kuvaa: horjuu, kaatuu selälleen (viimeinen makuu)
        t.getUpSprites = EnemySheet("portsari_ylosnousu", report);        // 5 kuvaa: kyljeltä konttaus, polvi, asento
        t.getUpTime = 0.9f;
        t.bigBody = true;                  // pelaaja heittää kuperkeikalla kuten Koviksen
        // kuperkeikka omilla kuvilla: 0–2 ote ja veto, 3–13 lento (pyörii, piirretty), 14–16 maahan ja makuu
        t.flipThrownSprites = EnemySheet("portsari_heitto", report);
        t.flipFlightFrames = 11; t.flipFlightFrameTime = 0.05f; t.flipLandFrameTime = 0.1f;
        // huudot: Audio/portsari (poke1, poke2), välillä, ei koskaan yhtä aikaa
        string voice = AssetDatabase.GetSubFolders("Assets/Audio").FirstOrDefault(f => Path.GetFileName(f).ToLowerInvariant().StartsWith("portsari"));
        t.tauntSounds = voice != null ? LoadClips(voice, "poke") : new AudioClip[0];
        report.Add($"Huudot: {t.tauntSounds.Length} (Audio/portsari/poke*)");
        t.fightsEveryone = true;           // lähimmän kimppuun: hero tai punkkarit
        t.wakeDistance = 100f;
        // järkälemäinen: ei juokse karkuun eikä kierrä, tulee suoraan päälle
        t.runSpeedMultiplier = 1.1f; t.flankChance = 0.05f; t.retreatChance = 0f; t.blockChance = 0f;
        SetBouncerThrow(t, report);        // tarttuu ja heittää olan yli kauas (kaikkia)

        // portsarit tulevat klubin ovesta (sama kohta, johon pelaaja ilmestyy)
        var door = Object.FindObjectsByType<Door>(FindObjectsSortMode.None).FirstOrDefault(d => d.target == club && !d.returnToLastDoor);
        Vector2 entry = door != null ? door.spawnPoint : new Vector2(ClubX0 + 2f, (club.minDepthY + club.maxDepthY) * 0.5f);
        var root = new GameObject("Portsarit");
        Undo.RegisterCreatedObjectUndo(root, "Portsarit");
        var squad = root.AddComponent<BouncerSquad>();
        squad.area = club;
        squad.bouncers = new Enemy[BouncerCount];
        go.transform.SetParent(root.transform, false);
        for (int i = 0; i < BouncerCount; i++)
        {
            var c = i == 0 ? go : Object.Instantiate(go, root.transform);
            if (i > 0) c.name = "Portsari_" + (i + 1);
            float y = Mathf.Clamp(entry.y + (((i / 2) % 3) - 1) * 0.6f, club.minDepthY, club.maxDepthY);   // parit: ovi ja oikea reuna
            c.transform.position = new Vector3(entry.x - 0.4f * i, y, 0f);
            squad.bouncers[i] = c.GetComponent<Enemy>();
            c.SetActive(false);            // piilossa, kunnes tappelu alkaa
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Info($"S-Clubiin {BouncerCount} portsaria: tulevat molemmista suunnista (ovelta ja oikeasta reunasta), kun klubissa alkaa ensimmäinen tappelu, ja käyvät lähimmän kimppuun (myös punkkareiden).\n\n" + string.Join("\n", report) +
             "\n\nJab + suora ja kaatava potku.\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- S-Clubin baaripöydät ja pullot ----------------
    // pöytien paikat: x klubin vasemmasta reunasta (yks), syvyys 0 = seinän vieressä, 1 = edessä
    // kaksi riviä lomittain: takarivi seinän puolella, eturivi edessä; keskelle jää tilaa tappelulle
    static readonly Vector2[] BarTables = {
        new Vector2(5f, 0.25f), new Vector2(9f, 0.25f), new Vector2(13f, 0.25f), new Vector2(17f, 0.25f), new Vector2(21f, 0.25f),
        new Vector2(25f, 0.25f), new Vector2(29f, 0.25f), new Vector2(33f, 0.25f), new Vector2(37f, 0.25f),
        new Vector2(7f, 0.88f), new Vector2(20f, 0.88f), new Vector2(33f, 0.88f) };
    // eturivissä kaksi snookerpöytää pullopöytien välissä (x klubin vasemmasta reunasta, etujalkojen syvyys)
    static readonly Vector2[] ClubSnooker = { new Vector2(13.5f, 0.88f), new Vector2(26.5f, 0.88f) };
    // telkkarit pyöreillä pöydillä seinän vieressä vain lavojen välisissä sohvanurkkauksissa (ei strippareiden eikä bändin edessä)
    static readonly float[] TvTablesX = { 15f, 23f, 35f };

    const float TableScale = 1.3f;   // suorakaidepöytä 30 % isompi, pullot 5 % (Bottle.scale)
    const float TvTableScale = 1.3f; // pyöreä telkkaripöytä 30 % isompi, telkkari myös (TvSet.scale)

    [MenuItem("Beat em up/41. S-Clubin baaripöydät, pullot ja snookerpöydät")]
    static void AddBarProps()
    {
        var club = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "S-Club");
        string tp = FindTexture("poyta");
        if (club == null || tp == null) { Info("Tarvitaan S-Clubin sisätila (kohta 10) ja poyta.png."); return; }
        SetupAndSlice(tp);
        var table = LoadSprites("poyta").OrderBy(sp => int.TryParse(sp.name.Substring(sp.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
        // heron pullon nosto ja heitto (pullonosto.png, 12 kuvaa)
        string hp = FindTexture("pullonosto");
        var heroPc = Object.FindFirstObjectByType<PlayerController>();
        if (hp != null && heroPc != null)
        {
            SetupAndSlice(hp);
            Undo.RecordObject(heroPc, "Pullon nosto");
            heroPc.smallItemSprites = LoadSprites("pullonosto").OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
            EditorUtility.SetDirty(heroPc);
        }
        var kinds = new List<Sprite[]>();
        var kindNames = new List<string>();
        foreach (var n in new[] { "pullo_olut", "pullo_sininen", "pullo_likoori", "pullo_vodka" })
        {
            string bp = FindTexture(n);
            if (bp == null) continue;
            SetupAndSlice(bp);
            var sp = LoadSprites(n).OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
            if (sp.Length >= 7) { kinds.Add(sp); kindNames.Add(n.Substring(6)); }
        }
        // lasit: (sheet, läiskä); tyhjät eivät jätä läiskää
        var glasses = new List<(Sprite[] sp, string stain, bool tall)>();
        foreach (var (n, st, tall) in new[] { ("pullo_lasi_tumbler", "-", false), ("pullo_lasi_viski", "likoori", false), ("pullo_lasi_olut", "olut", true), ("pullo_lasi_tuoppi", "-", true) })
        {
            string gp = FindTexture(n);
            if (gp == null) continue;
            SetupAndSlice(gp);
            var sp = LoadSprites(n).OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
            if (sp.Length >= 5) glasses.Add((sp, st, tall));
        }
        // läiskät (Resources/Tahrat): yksittäisiä spritejä, keskikohta
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Tahrat" }))
        {
            var ti = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100; ti.alphaIsTransparency = true; ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }
        var glass = LoadClips("Assets/Audio/sfx", "glass");
        // telkkari takarivin keskimmäiselle pöydälle (jääkiekko pyörii)
        Sprite[] tvSprites = new Sprite[0];
        string tvp = FindTexture("telkkari");
        if (tvp != null)
        {
            SetupAndSlice(tvp);
            tvSprites = LoadSprites("telkkari").OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
        }
        if (table.Length < 7) { Info("poyta.png: kuvia " + table.Length + "/7 (kohta 1)."); return; }
        var old = GameObject.Find("Baaripöydät");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = new GameObject("Baaripöydät");
        Undo.RegisterCreatedObjectUndo(root, "Baaripöydät");
        var rnd = new System.Random(7);
        int bottles = 0;
        // pyöreät telkkaripöydät
        Sprite[] round = new Sprite[0];
        string rp = FindTexture("poyta_tv");
        if (rp != null)
        {
            SetupAndSlice(rp);
            round = LoadSprites("poyta_tv").OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
        }
        int tvs = 0;
        if (round.Length >= 14 && tvSprites.Length >= 11)
            foreach (float tx in TvTablesX)
            {
                float y = Mathf.Lerp(club.maxDepthY - 0.3f, club.minDepthY + 0.4f, 0.05f);
                var go = new GameObject("Telkkaripöytä");
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(ClubX0 + tx, y, 0f);
                var vis = new GameObject("Visual").AddComponent<SpriteRenderer>(); vis.transform.SetParent(go.transform, false);
                var sh = new GameObject("Shadow").AddComponent<SpriteRenderer>(); sh.transform.SetParent(go.transform, false);
                sh.color = new Color(0f, 0f, 0f, 0.35f);
                var c = go.AddComponent<Crate>();
                c.body = vis; c.shadow = sh;
                c.sprites = new[] { round[0], round[1], round[2] };
                c.breakSprites = round.Skip(3).ToArray();          // kansi halkeaa, jalka sirpaloituu, romukasa
                c.breakFrameTime = 0.07f;
                c.hitsToBreak = 3; c.breakable = true; c.footOffset = 0.04f;
                c.visualScale = TvTableScale;              // isompi pyöreä pöytä
                c.shadowWidth = 1.3f; c.hitRadiusX = 0.8f * TvTableScale; c.debrisTime = 6f;
                c.carryLower = 0.84f * TvTableScale; c.plowThrough = true;          // kannossa kansi käsissä, jalat eivät jää ilmaan
                c.moneyChance = 0.2f; c.energyChance = 0.1f; c.throwDamage = 20;
                vis.sprite = round[0];
                var tvGo = new GameObject("Telkkari");
                tvGo.transform.SetParent(root.transform, false);
                tvGo.transform.position = go.transform.position;
                var tv = tvGo.AddComponent<TvSet>();
                tv.sprites = tvSprites; tv.table = c; tv.tableTop = 1.16f * TvTableScale - 0.04f; tv.breakSounds = glass;
                tvs++;
            }
        int snooker = 0;
        foreach (var v in ClubSnooker)
            if (AddSnookerTable(root.transform, new Vector2(ClubX0 + v.x, Mathf.Lerp(club.maxDepthY - 0.3f, club.minDepthY + 0.4f, v.y)), PokerPoolWidth) != null) snooker++;
        foreach (var v in BarTables)
        {
            float y = Mathf.Lerp(club.maxDepthY - 0.3f, club.minDepthY + 0.4f, v.y);
            var go = new GameObject("Pöytä");
            go.transform.SetParent(root.transform, false);
            go.transform.position = new Vector3(ClubX0 + v.x, y, 0f);
            var vis = new GameObject("Visual").AddComponent<SpriteRenderer>(); vis.transform.SetParent(go.transform, false);
            var sh = new GameObject("Shadow").AddComponent<SpriteRenderer>(); sh.transform.SetParent(go.transform, false);
            sh.color = new Color(0f, 0f, 0f, 0.35f);
            var c = go.AddComponent<Crate>();
            c.body = vis; c.shadow = sh;
            c.sprites = new[] { table[0], table[1], table[2] };      // ehjä, nitkahtaa, halkeaa
            c.breakSprites = new[] { table[3], table[4], table[5], table[6] };   // katkeaa, kaatuu, romu, romukasa
            c.breakFrameTime = 0.1f;
            c.hitsToBreak = 3;
            c.breakable = true;
            c.footOffset = 0.04f;
            c.visualScale = TableScale;                 // isompi pöytä
            c.shadowWidth = 2.6f;
            c.carryLower = 0.86f * TableScale; c.plowThrough = true;          // kannossa kansi käsissä (jalkojen pituus)
            c.hitRadiusX = 1.4f * TableScale;
            c.debrisTime = 6f;
            c.moneyChance = 0.3f; c.energyChance = 0.15f;
            c.throwDamage = 22;
            vis.sprite = table[0];
            // pulloja ja laseja reilusti, eri merkkejä
            if (kinds.Count == 0) continue;
            bool hasTv = false;
            int n = 7 + rnd.Next(3);                 // 7–9 pulloa pöydällä
            int g = glasses.Count > 0 ? 2 + rnd.Next(2) : 0;   // 2–3 lasia pullojen sekaan
            int total = n + g;
            var isGlass = new bool[total];
            for (int k = 0; k < g; k++) { int at; do at = rnd.Next(total); while (isGlass[at]); isGlass[at] = true; }
            for (int i = 0; i < total; i++)
            {
                var bGo = new GameObject("Pullo");
                bGo.transform.SetParent(root.transform, false);
                bGo.transform.position = go.transform.position;
                var b = bGo.AddComponent<Bottle>();
                if (isGlass[i])
                {
                    var gl = glasses[rnd.Next(glasses.Count)];
                    b.sprites = gl.sp; b.stainKind = gl.stain; b.pivotY = gl.tall ? 0.2f : 0.15f;
                    bGo.name = "Lasi";
                }
                else
                {
                    int ki = rnd.Next(kinds.Count);
                    b.sprites = kinds[ki];
                    b.stainKind = kindNames[ki];
                    // eri kokoisia: vodka ja likööri isoja pulloja, olut pieni, lisäksi vaihtelua ja välillä iso pullo
                    float ks = kindNames[ki] == "vodka" ? 1.4f : kindNames[ki] == "likoori" ? 1.25f : kindNames[ki] == "sininen" ? 1.12f : 1f;
                    b.scale = 1.05f * ks * (0.94f + 0.12f * (float)rnd.NextDouble()) * (rnd.Next(5) == 0 ? 1.15f : 1f);
                }
                b.breakSounds = glass;
                b.table = c;
                b.tableX = hasTv ? (i % 2 == 0 ? -1f : 1f) * (0.85f + 0.15f * (i / 2)) : Mathf.Lerp(-1.1f, 1.1f, (i + 0.5f) / total) + (float)(rnd.NextDouble() - 0.5) * 0.06f;
                b.tableTop = 1.28f * TableScale - 0.04f;   // pöydän pinta isommassa pöydässä
                b.tableX *= TableScale;
                bottles++;
            }
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info($"S-Clubiin {BarTables.Length} pöytää, {snooker} snookerpöytää, {tvs} telkkaria ja {bottles} pulloa ({kinds.Count} pullomerkkiä), lasiääniä {glass.Length}.\n" +
             "Lyönti pöytään: nitkahtaa, pullot lentävät tai kaatuvat. Kolmas isku tai lentävä vihu hajottaa pöydän.\n" +
             "Ehjä pullo lattialla: kiinniottonappi poimii käteen, lyöntinappi heittää.\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Kadun telkkarit ----------------
    static readonly float[] StreetTvX = { 0.14f, 0.37f, 0.61f, 0.86f };   // osuus kadun pituudesta

    [MenuItem("Beat em up/42. Telkkareita kadulle (ruutu pimeänä)")]
    static void AddStreetTvs()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        var street = GameObject.Find("Tausta");
        string tvp = FindTexture("telkkari");
        if (pc == null || street == null || tvp == null) { Info("Tarvitaan pelaaja, katutausta ja telkkari.png."); return; }
        SetupAndSlice(tvp);
        var sprites = LoadSprites("telkkari").OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
        if (sprites.Length < 11) { Info("telkkari.png: kuvia " + sprites.Length + " (kohta 1)."); return; }
        var old = GameObject.Find("Kadun telkkarit");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = new GameObject("Kadun telkkarit");
        Undo.RegisterCreatedObjectUndo(root, "Kadun telkkarit");
        var streetArea = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Katu");
        float curb = streetArea != null ? streetArea.curbDepthY : pc.curbDepthY;
        float wall = streetArea != null ? streetArea.maxDepthY : pc.maxDepthY;
        float y = (curb + wall) * 0.5f;
        var ssr = street.GetComponent<SpriteRenderer>();
        float left = street.transform.position.x - ssr.size.x * 0.5f;
        float right = street.transform.position.x + ssr.size.x * 0.5f;
        var ext = GameObject.Find("Tausta jatko");
        if (ext != null) right = Mathf.Max(right, ext.GetComponent<SpriteRenderer>().bounds.max.x);
        float kerb = streetArea != null ? streetArea.sidewalkHeight : pc.sidewalkHeight;
        var blocked = StreetObstacles(true);
        var glass = LoadClips("Assets/Audio/sfx", "glass");
        int count = 0;
        foreach (float tx in StreetTvX)
        {
            float cx = Mathf.Lerp(left + 6f, right - 6f, tx);
            for (int tries = 0; tries < 8 && Blocked(blocked, cx, 0.7f); tries++) cx += 1.2f;
            if (Blocked(blocked, cx, 0.7f)) continue;
            var go = new GameObject("Telkkari " + (++count));
            go.transform.SetParent(root.transform, false);
            go.transform.position = new Vector3(cx, y - 0.1f, 0f);
            var tv = go.AddComponent<TvSet>();
            tv.sprites = sprites; tv.onGround = true; tv.screenOff = true; tv.tableTop = 0f;
            tv.breakSounds = glass; tv.groundOffset = kerb;   // jalkakäytävällä
            blocked.Add(new Vector2(cx, 0.7f));
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info($"Kadulle {count} telkkaria (ruutu pimeänä). Potku lennättää, lyönti hajottaa, kiinniottonappi nostaa.\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Skettari ----------------
    // Liikkuu rullalaudalla: potkii vauhtia ja liukuu, lyö laudalta ja syöksyy kauempaa lyönti edellä.
    static readonly Vector2[] SkaterStreet = { new Vector2(38f, -2.9f), new Vector2(68f, -2.1f), new Vector2(98f, -3.3f), new Vector2(140f, -2.5f) };

    [MenuItem("Beat em up/39. Lisää Skettarit (uusi vihollinen)")]
    static void AddSkaters()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { Info( "Scenessä ei ole pelaajaa."); return; }
        foreach (var n in new[] { "skettari_ajo", "skettari_vauhti", "skettari_lyonti", "skettari_kaatuminen", "skettari_ylosnousu", "skettari_idle", "skettari_hyppypotku", "skettari_lyonnit", "skettari_kavely", "skettari_polvi" })
        {
            string path = FindTexture(n);
            if (path != null) SetupAndSlice(path);
        }
        var report = new List<string>();
        Sprite[] ride = EnemySheet("skettari_ajo", report);
        if (ride.Length == 0) { Info( "skettari_ajo.png puuttuu."); return; }
        Sprite[] push = EnemySheet("skettari_vauhti", report);
        Sprite[] punch = EnemySheet("skettari_lyonti", report);

        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            if (e.gameObject.name.StartsWith("Skettari")) Undo.DestroyObjectImmediate(e.gameObject);

        var go = new GameObject("Skettari");
        var visual = new GameObject("Visual").AddComponent<SpriteRenderer>();
        visual.transform.SetParent(go.transform, false);
        var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>();
        shadow.transform.SetParent(go.transform, false);
        var t = go.AddComponent<Enemy>();
        t.body = visual; t.shadow = shadow; visual.sprite = ride[0];
        t.displayName = "Skettari";
        t.idleSprites = ride;                 // seisoo laudalla ja keinuu
        t.idleFrameTime = 0.15f;
        // liikkuessa: potku vauhtia (6 kuvaa) ja liuku (ajokuvat), vuorotellen
        // yksi potku (jalka taakse, potku maahan, jalka takaisin) ja pitkä liuku ajokuvilla
        // potku näkyy: jokainen potkukuva kahdesti (n. 0.7 s), sitten liuku
        t.walkSprites = push.Length >= 6 ? new[] { push[0], push[1], push[1], push[2], push[2], push[4], push[4], push[5] }.Concat(ride).ToArray() : ride;
        t.walkFrameTime = 0.09f;
        t.punchSprites = punch;
        t.punchImpactFrame = 3;               // käsi suorana kuvassa 4
        t.windupTime = 0.15f;
        // syöksylyönti: liukuu laudalla kauempaa lyönti edellä
        t.altAttackSprites = punch;
        t.altImpactFrame = 3;
        t.altChance = 0.5f;
        t.altDamage = 12;
        t.altReach = 2.2f;
        t.altLungeSpeed = 12f;                // vauhti jatkuu lyönnissä
        t.altLungeTime = 0.35f;
        t.altExtraWindup = 0.05f;
        t.chargeRange = 7f;
        t.chargeMinRange = 2.6f;
        t.moveSpeedX = 7.5f;                  // laudalla todella nopea: ajaa ruudun poikki edestakaisin
        t.moveSpeedY = 2.2f;
        t.runSpeedMultiplier = 1.3f;
        t.skatePass = true; t.passOvershoot = 6.5f;
        t.flankChance = 0.4f;                 // kiertää usein selän taakse
        t.retreatChance = 0.45f;              // iske ja liu'u pois
        t.maxHealth = 110;                    // kestävä
        t.punchDamage = 8;
        t.attackCooldown = 1.1f;
        // kaatuminen (11 kuvaa): 0 osuma, 1–4 ilmassa, 5–10 kierähdys maassa ja makaa; lauta irtoaa ja jatkaa matkaa
        var fall = EnemySheet("skettari_kaatuminen", report);
        if (fall.Length >= 11)
        {
            t.knockdownSprites = new[] { fall[0], fall[1], fall[2], fall[3], fall[4], fall[10] };
            t.landSprites = fall.Skip(5).Take(6).ToArray();
            t.footHurtSprites = new[] { fall[0] };      // ilman lautaa osumakuva
        }
        var getUp = EnemySheet("skettari_ylosnousu", report);   // 12 kuvaa: makaa -> tappeluasento
        if (getUp.Length > 0) { t.getUpSprites = getUp; t.getUpTime = 1.0f; }
        t.looseBoardSprite = ImportProp("Assets/Sprites/Rekvisiitta/skettari_lauta.png");
        // vastaliike: hero ottaa kiinni ja vetää polvella päähän (skettari_polvi.png 10 kuvaa + vanhan kaatumisen loppu)
        var knee = EnemySheet("skettari_polvi", report);
        if (knee.Length >= 10 && fall.Length >= 11)
        {
            t.kneeHeldSprites = knee.Take(5).ToArray();
            t.kneeFlightSprites = new[] { knee[5], knee[6], knee[7], knee[8], knee[9], fall[9], fall[10] };
        }
        string kp = FindTexture("polvi");
        if (kp != null)
        {
            SetupAndSlice(kp);
            var heroKnee = LoadSprites("polvi").OrderBy(sp => int.TryParse(sp.name.Substring(sp.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
            Undo.RecordObject(pc, "Polvi");
            pc.kneeSprites = heroKnee;
            // jab + polvi -kombon oma polvi-isku (videosta)
            string kip = FindTexture("polvi_isku");
            if (kip != null)
            {
                SetupAndSlice(kip);
                pc.kneeStrikeSprites = LoadSprites("polvi_isku").OrderBy(sp => int.TryParse(sp.name.Substring(sp.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
                report.Add($"Heron polvi-isku: {pc.kneeStrikeSprites.Length} kuvaa");
            }
            EditorUtility.SetDirty(pc);
            report.Add($"Heron polvi: {heroKnee.Length} kuvaa");
        }
        report.Add("Irtolauta: " + (t.looseBoardSprite != null ? "OK" : "puuttuu"));
        // noustua tappelee jalan (omat kävely- ja lyöntikuvat myöhemmin; siihen asti tappeluasento)
        t.footMoveSpeedX = 3.3f; t.footMoveSpeedY = 1.9f;   // nopea kuin Lippis
        var footWalk = EnemySheet("skettari_kavely", report);   // 12 kuvaa Viggle-videosta, 24 fps
        if (footWalk.Length > 0) { t.footWalkSprites = footWalk; t.footWalkFrameTime = 0.045f; }
        var footIdle = EnemySheet("skettari_idle", report);
        if (footIdle.Length > 0) t.footIdleSprites = footIdle;
        var jumpKick = EnemySheet("skettari_hyppypotku", report);   // 12 kuvaa, potku ojennettuna kuvassa 6
        // suora jalka näkyy pidempään: potkukuvat 6–7 kahdesti/kolmesti; hyppy koodilla (1.3 yks)
        if (jumpKick.Length >= 12)
        {
            t.footKickSprites = new[] { 0, 1, 2, 3, 4, 5, 5, 6, 6, 6, 7, 8, 9, 10, 11 }.Select(i => jumpKick[i]).ToArray();
            t.footKickImpactFrame = 5; t.footKickLunge = 6f; t.footKickJump = 1.3f;
        }
        // jab–suora-kombo (12 kuvaa): jab osuu kuvassa 4, suora kuvassa 7; nopea veto
        var combo = EnemySheet("skettari_lyonnit", report);
        if (combo.Length > 0)
        {
            t.footPunchSprites = combo; t.footPunchImpactFrame = 3; t.footSecondImpactFrame = 6;
            t.footWindupTime = 0.15f; t.footPunchRecoverTime = 0.65f;
        }
        t.hurtSounds = LoadClips("Assets/Audio/big thug", "gasp");
        t.hurtVolume = 0.95f;
        Undo.RegisterCreatedObjectUndo(go, "Skettari");

        float x0 = pc.transform.position.x;
        for (int i = 0; i < SkaterStreet.Length; i++)
        {
            var v = SkaterStreet[i];
            var c = i == 0 ? go : Object.Instantiate(go);
            if (i > 0) { c.name = "Skettari_" + (i + 1); Undo.RegisterCreatedObjectUndo(c, "Skettari"); }
            c.transform.position = new Vector3(x0 + v.x, Mathf.Clamp(v.y, pc.minDepthY, pc.maxDepthY), 0f);
        }
        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        Info(
            $"Skettareita kadulla: {SkaterStreet.Length}\n\n" + string.Join("\n", report) +
            "\n\nPotkii vauhtia, liukuu ja lyö laudalta; syöksyy kauempaa lyönti edellä.\nKaatuessa lauta irtoaa ja jää maahan; noustuaan tappelee jalan.\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Kadun jatko ----------------
    // katu_jatko.png: tiilitalo loppuu -> aita ja mainostaulu -> liikerakennus -> pelihalli.
    // Sama korkeus (1024 px) ja mittakaava kuin katu_sarja.png: seinän juuri rivillä 590, reunakivi 615–637.
    const string StreetExtPath = "Assets/Sprites/Taustat/katu_jatko.png";

    [MenuItem("Beat em up/28. Lisää kadun jatko (liikerakennus ja pelihalli)")]
    static void AddStreetExtension()
    {
        var street = GameObject.Find("Tausta");
        var ti = AssetImporter.GetAtPath(StreetExtPath) as TextureImporter;
        if (street == null || ti == null)
        {
            Info( "Tarvitaan katutausta (kohta 4) ja kuva " + StreetExtPath);
            return;
        }
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = BackgroundPPU;
        ti.filterMode = FilterMode.Bilinear;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.maxTextureSize = 8192;
        ti.mipmapEnabled = false;
        var st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        st.spriteMeshType = SpriteMeshType.FullRect;
        st.spriteAlignment = (int)SpriteAlignment.Center;
        ti.SetTextureSettings(st);
        ti.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(StreetExtPath);
        float wU = sprite.rect.width / BackgroundPPU;

        var old = GameObject.Find("Tausta jatko");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var ssr = street.GetComponent<SpriteRenderer>();
        float right = street.transform.position.x + ssr.size.x * 0.5f;   // nykyisen kadun oikea reuna
        var go = new GameObject("Tausta jatko");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -10000;
        // sama korkeus ja keskikohta kuin kadulla, joten jalkakäytävä ja reunakivi jatkuvat suoraan
        go.transform.position = new Vector3(right + wU * 0.5f, street.transform.position.y, 0f);
        Undo.RegisterCreatedObjectUndo(go, "Kadun jatko");

        // kamera saa kulkea jatkon loppuun asti (myös katualueen asetuksissa, joita ovet käyttävät)
        float halfW = Camera.main != null ? Camera.main.orthographicSize * 16f / 9f : 10.7f;
        float newMax = right + wU - halfW;
        var follow = Object.FindFirstObjectByType<CameraFollow>();
        if (follow != null) { Undo.RecordObject(follow, "Kameran rajat"); follow.maxX = newMax; EditorUtility.SetDirty(follow); }
        var streetArea = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Katu");
        if (streetArea != null) { Undo.RecordObject(streetArea, "Kameran rajat"); streetArea.camMaxX = newMax; EditorUtility.SetDirty(streetArea); }

        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        Info(
            $"Kadun jatko lisätty: {wU:0.0} yksikköä (x {right:0.0} … {right + wU:0.0}).\nKamera kulkee nyt loppuun asti.\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Palotikkaat ja katto ----------------
    // Palotikkaiden kohta katu_jatko.png:ssä (pikseleinä vasemmasta reunasta). katto.png (1024 px korkea) täyttää ruudun
    // kuten S-Clubin sisäkuva, katon takareuna rivillä RoofFloorRow.
    const float FireEscapePx = 5362f;
    const string RoofPath = "Assets/Sprites/Taustat/katto.png";
    const float RoofPPU = 85f, RoofFloorRow = 574f, RoofX0 = 2000f;
    // katon viholliset: (malli, x katon vasemmasta reunasta, syvyys 0 = takareuna … 1 = etureuna)
    static readonly (string template, float x, float depth)[] RoofEnemies =
    {
        ("Punkkari", 11f, 0.3f), ("Lippis", 13f, 0.7f),
        ("Kovis", 19f, 0.4f), ("Punkkari", 21f, 0.8f), ("Lippis", 22f, 0.2f),
    };
    const float RoofBossX = 31f;

    [MenuItem("Beat em up/30. Palotikkaat ja katto (viholliset ja pomo)")]
    static void CreateRoof()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        var cam = Camera.main;
        var ext = GameObject.Find("Tausta jatko");
        var streetArea = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Katu");
        var ti = AssetImporter.GetAtPath(RoofPath) as TextureImporter;
        if (pc == null || cam == null || ext == null || streetArea == null || ti == null)
        {
            Info("Tarvitaan pelaaja, kamera, kadun jatko (kohta 28), S-Clubin alueet (kohta 10) ja kuva " + RoofPath);
            return;
        }
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = RoofPPU;
        ti.filterMode = FilterMode.Bilinear;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.maxTextureSize = 8192;
        ti.mipmapEnabled = false;
        ti.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoofPath);
        float wU = sprite.rect.width / RoofPPU, hU = sprite.rect.height / RoofPPU;

        foreach (var n in new[] { "Katto", "Alue: Katto", "Palotikkaat" })
        {
            var o = GameObject.Find(n);
            if (o != null) Undo.DestroyObjectImmediate(o);
        }
        DestroyEnemyGroups("Katon viholliset", false);   // vain kadun katon ryhmä (laivan katon ryhmä x >= 50000 jää)

        float camY = cam.transform.position.y;
        float halfW = cam.orthographicSize * 16f / 9f;

        var bg = new GameObject("Katto");
        var sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -10000;
        bg.transform.position = new Vector3(RoofX0 + wU * 0.5f, camY, 0f);
        Undo.RegisterCreatedObjectUndo(bg, "Katto");

        var roof = new GameObject("Alue: Katto").AddComponent<Area>();
        roof.areaName = "Katto";
        roof.maxDepthY = camY + hU * 0.5f - RoofFloorRow / RoofPPU;
        roof.minDepthY = pc.minDepthY;
        roof.useSidewalk = false;
        roof.camMinX = RoofX0 + halfW;
        roof.camMaxX = RoofX0 + wU - halfW;
        Undo.RegisterCreatedObjectUndo(roof.gameObject, "Alue");

        // tikkaat kadulla ja paluu katolta
        var esr = ext.GetComponent<SpriteRenderer>();
        float extLeft = ext.transform.position.x - esr.sprite.rect.width / BackgroundPPU * 0.5f;
        float ladderX = extLeft + FireEscapePx / BackgroundPPU;
        var group = new GameObject("Palotikkaat");
        Undo.RegisterCreatedObjectUndo(group, "Palotikkaat");
        var up = new GameObject("Tikkaat ylös").AddComponent<Door>();
        up.transform.SetParent(group.transform, false);
        up.transform.position = new Vector3(ladderX, streetArea.maxDepthY - 0.25f, 0f);
        up.prompt = "Kiipeä katolle";
        up.here = streetArea;
        up.target = roof;
        up.spawnPoint = new Vector2(RoofX0 + 3.4f, Mathf.Lerp(roof.maxDepthY, roof.minDepthY, 0.75f));   // tikkaiden juurelta
        up.halfWidth = 1.2f;
        up.maxDistanceFromWall = 0.9f;
        up.climbHeight = 3f;
        var down = new GameObject("Tikkaat alas").AddComponent<Door>();
        down.transform.SetParent(group.transform, false);
        down.transform.position = new Vector3(RoofX0 + 2.2f, roof.maxDepthY, 0f);   // tikkaat katon vasemmassa etukulmassa
        down.prompt = "Laskeudu kadulle";
        down.here = roof;
        down.target = streetArea;
        down.returnToLastDoor = true;
        down.halfWidth = 1.6f;
        down.maxDistanceFromWall = 100f;   // tikkaiden juuri on edessä, koko syvyys käy
        down.climbHeight = -1.5f;
        down.spawnPoint = new Vector2(ladderX, streetArea.maxDepthY - 0.25f);

        // viholliset ja pomo: kopiot kadun malleista
        var report = BuildRoofEnemies(roof);

        EditorSceneManager.MarkSceneDirty(bg.scene);
        Info(
            $"Palotikkaat kadun lopussa (x = {ladderX:0.0}): mene tikkaiden eteen jalkakäytävälle ja paina E (ohjaimessa ympyrä).\n" +
            $"Katolla {RoofEnemies.Length} vihollista ja pomo. Takaisin alas vasemman reunan tikkailta.\n" +
            (report.Count > 0 ? string.Join("\n", report) + "\n" : "") +
            "\nKaton oikea puoli on kattokuvan 1 peilikuva, kunnes kuvat 2 ja 3 valmistuvat.\n\nTallenna scene (Ctrl+S).");
    }

    /// Poistaa kaikki nimetyt juuriryhmät: ship = true vain laivan katon (x >= 50000), false vain muut (kadun katto).
    static void DestroyEnemyGroups(string name, bool ship)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null || t.name != name || t.parent != null) continue;
            bool isShip = t.childCount > 0 && t.GetChild(0).position.x >= 50000f;
            if (isShip == ship) Undo.DestroyObjectImmediate(t.gameObject);
        }
    }

    /// Kadun katon viholliset ja pomo (Metsuri): kopiot kadun malleista. Ryhmän nimi "Katon viholliset".
    static List<string> BuildRoofEnemies(Area roof)
    {
        var enemies = new GameObject("Katon viholliset");
        Undo.RegisterCreatedObjectUndo(enemies, "Katon viholliset");
        var all = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        var report = new List<string>();
        float Depth(float k) => Mathf.Lerp(roof.maxDepthY - 0.2f, roof.minDepthY + 0.3f, k);
        for (int i = 0; i < RoofEnemies.Length; i++)
        {
            var (name, x, depth) = RoofEnemies[i];
            var t = all.FirstOrDefault(e => e.gameObject.name == name);
            if (t == null) { report.Add("Puuttuu malli: " + name); continue; }
            var go = Object.Instantiate(t.gameObject, enemies.transform);
            go.name = "Katto " + name + " " + (i + 1);
            go.transform.position = new Vector3(RoofX0 + x, Depth(depth), 0f);
        }
        var kovis = all.FirstOrDefault(e => e.gameObject.name == "Kovis");
        if (kovis != null)
        {
            // pomo: metsuri, Koviksen kokoinen. Pohjana Koviksen kopio (varjo, äänet), omat kuvat päälle.
            foreach (var f in new[] { "pomo_idle", "pomo_juoksu", "pomo_heitto", "pomo_taklaus", "pomo_lyonti", "pomo_maha", "pomo_kaatuu" })
                SetupAndSlice(EnemyFolder + "/" + f + ".png");
            var idle = EnemySheet("pomo_idle", report);
            var run = EnemySheet("pomo_juoksu", report);
            var grab = EnemySheet("pomo_heitto", report);
            var tackle = EnemySheet("pomo_taklaus", report);
            var slam = EnemySheet("pomo_lyonti", report);
            var belly = EnemySheet("pomo_maha", report);
            var fall = EnemySheet("pomo_kaatuu", report);
            var go = Object.Instantiate(kovis.gameObject, enemies.transform);
            go.name = "Pomo";
            go.transform.position = new Vector3(RoofX0 + RoofBossX, Depth(0.5f), 0f);
            go.transform.localScale = Vector3.one * 1.15f;   // 15 % Kovista isompi
            var b = go.GetComponent<Enemy>();
            b.displayName = "Metsuri";
            b.appearAfterOthers = true;    // tulee vasta, kun katon muut vihut on voitettu
            b.bigBody = true;
            var none = new Sprite[0];
            if (idle.Length > 0) { b.idleSprites = idle; b.body.sprite = idle[0]; b.idleFrameTime = 0.16f; }   // 6 kuvaa, suu liikkuu (puhuu)
            if (run.Length > 0) { b.walkSprites = run; b.walkFrameTime = 0.07f; }   // pomo ei kävele, se juoksee
            // lyönti ylhäältä alas: lataa nyrkkiä 0.3 s (ehtii alta pois), heilautus ja isku maahan kaataa.
            // Varalla kahden käden töytäisy heittokuvista.
            if (slam.Length >= 4) { b.punchSprites = slam; b.punchImpactFrame = 2; b.windupTime = 0.3f; b.punchKnockdown = true; b.punchShake = 0.18f; }
            else { b.punchSprites = grab.Length >= 8 ? new[] { grab[1], grab[0], grab[7], grab[1] } : none; b.punchImpactFrame = 1; }
            // taklaus: juoksee matkan päästä pelaajaa kohti ja kaataa
            b.altAttackSprites = tackle;
            b.altChance = tackle.Length > 0 ? 0.5f : 0f;
            // Viggle-videon taklaus (8 kuvaa): 0–1 vauhti, 2 lataus, 3 olkataklaus (liu'un ajan), 4–7 palautuminen
            b.altImpactFrame = tackle.Length >= 8 ? 3 : 2;
            b.altDamage = 18;
            b.altReach = 1.6f;
            b.altExtraWindup = 0.15f;
            b.altTimeScale = 1.4f;          // selvä ennakkovaroitus ja hidas palautuminen: rangaistuksen paikka
            b.altLungeSpeed = 17f;          // pitkä ja nopea liuku (n. 13 yksikköä)
            b.altLungeTime = 0.75f;
            b.altKnockdown = true;
            b.altKnockSpeed = 8f;
            b.altKnockUp = 6f;
            b.chargeRange = 10f;
            b.chargeMinRange = 2.6f;
            // vatsatöytäisy: lähellä osa hyökkäyksistä, ja kombon keskellä torjuu ja töytäisee pelaajan kauas
            b.bellySprites = belly.Length >= 8 ? belly : none;
            b.bellyChance = 0.4f;
            b.bellyRange = 1.7f;
            b.bellyCounterChance = 0.35f;
            b.bellyCounterAfterHits = 2;
            b.bellyDamage = 14;
            b.bellyKnockSpeed = 14f;
            b.bellyKnockUp = 5.5f;
            // kaatuminen (10 kuvaa): 1 osuma, 1–4 ilmassa/maahan, 5 makaa, 6–9 nousee
            if (fall.Length >= 10)
            {
                b.hurtSprites = new[] { fall[1] };
                b.knockdownSprites = new[] { fall[1], fall[2], fall[3], fall[4], fall[5] };
                b.getUpSprites = new[] { fall[6], fall[7], fall[8], fall[9] };
                b.getUpTime = 0.6f;
            }
            else { b.hurtSprites = none; b.knockdownSprites = none; b.getUpSprites = none; }
            // äänet (Assets/Audio/pomo1): grunt osumasta ja lyödessä, nauru vatsatöytäisyn jälkeen
            var grunts = LoadClips("Assets/Audio/pomo1", "pomogrunt");
            if (grunts.Length > 0)
            {
                b.hurtSounds = grunts; b.hurtVolume = 0.95f;
                b.attackSounds = grunts; b.attackVolume = 0.9f; b.attackSoundChance = 0.8f;
            }
            b.laughSound = LoadClips("Assets/Audio/pomo1", "pomonauru").FirstOrDefault();
            report.Add($"Pomon äänet: {grunts.Length} gruntia, nauru {(b.laughSound != null ? "ok" : "puuttuu (Assets/Audio/pomo1/pomonauru)")}");
            // taklauksen juoksu: töminä ja ruudun tärinä
            b.stompSounds = LoadClips("Assets/Audio/sfx", "tomina");
            b.stompVolume = 0.85f;
            b.stompInterval = 0.2f;
            b.stompShake = 0.07f;
            b.flipThrownSprites = none; b.headlockThrownSprites = none;
            b.grabSprites = grab;
            b.grabChance = 0.1f;
            b.grabRange = 1.4f;
            b.grabWhenCloseTime = 1.0f;     // liian kauan vieressä -> nappaa ja viskaa ruudun poikki
            b.throwForward = true;
            b.throwSpeed = 15f;             // pomo viskaa ruudun poikki korkealla kaarella
            b.throwUp = 11f;
            b.throwDamage = 22;
            b.moveSpeedX = 3.4f;
            b.moveSpeedY = 1.9f;
            b.maxHealth = 260;
            b.punchDamage = 16;
            b.attackCooldown = 1.0f;
            b.wakeDistance = 12f;
            b.throwsBarrels = true;         // hakee tynnyrin ja paiskaa sen kovaa
            b.barrelThrowSpeed = 16f;
            b.barrelThrowUp = 4f;
            b.barrelDamage = 20;
        }

        return report;
    }

    [MenuItem("Beat em up/30b. Kadun katon viholliset ja Metsuri uudelleen (ei muuta kattoa)")]
    static void RebuildRoofEnemiesOnly()
    {
        if (PlayModeBlocked()) return;
        var roof = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Katto");
        if (roof == null) { Info("Kadun katon aluetta ei löytynyt: tee ensin kohta 30."); return; }
        DestroyEnemyGroups("Katon viholliset", false);
        var report = BuildRoofEnemies(roof);
        EditorSceneManager.MarkSceneDirty(roof.gameObject.scene);
        Info($"Kadun katon {RoofEnemies.Length} vihollista ja pomo (Metsuri) luotu uudelleen.\n" +
             (report.Count > 0 ? string.Join("\n", report) + "\n" : "") +
             "Aja sen jälkeen vielä kohta 31 (vihujen taktiikka), jotta vihut torjuvat ja kiertävät.\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Vihujen taktiikka ----------------
    [MenuItem("Beat em up/31. Vihujen taktiikka (juoksu, kiertäminen, torjunta)")]
    static void SetEnemyTactics()
    {
        var report = new List<string>();
        var lippisBlock = EnemySheet("lippis_torjunta", report);
        int count = 0;
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            string n = e.gameObject.name.Replace("Katto ", "");
            Undo.RecordObject(e, "Taktiikka");
            if (n == "Pomo")
            {
                e.runSpeedMultiplier = 1.2f; e.flankChance = 0.3f; e.retreatChance = 0.2f;
                e.blockChance = 0.25f; e.maxBlocksInRow = 2;
            }
            else if (n.StartsWith("Lippis"))
            {
                // nopea ja ovela: juoksee, kiertää selän taakse, iskee ja vetäytyy, torjuu
                e.runSpeedMultiplier = 1.5f; e.flankChance = 0.5f; e.retreatChance = 0.4f;
                e.blockChance = 0.35f; e.maxBlocksInRow = 2;
                if (lippisBlock.Length > 0) { e.blockSprites = lippisBlock; e.blockTime = 0.5f; }   // 10 kuvaa: kädet ylös, suoja, paluu
                // potku: lähes kaksi kertaa nopeampi ja liukuu eteen
                e.altTimeScale = 0.55f; e.altLungeSpeed = 7f; e.altLungeTime = 0.22f;
            }
            else if (n.StartsWith("Punkkari"))
            {
                SetPunkSounds(e);
                e.runSpeedMultiplier = 1.6f; e.flankChance = 0.35f; e.retreatChance = 0.3f; e.blockChance = 0f;
            }
            else if (n.StartsWith("Kovis"))
            {
                e.runSpeedMultiplier = 1.15f; e.flankChance = 0.1f; e.retreatChance = 0f; e.blockChance = 0f;
                e.throwSpeed = 11f; e.throwUp = 10f;   // viskaa heron korkealle ja kauas
            }
            else continue;
            EditorUtility.SetDirty(e);
            count++;
        }
        // heron heitot: korkeampi ja pidempi lento
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            Undo.RecordObject(pc, "Heittojen lento");
            pc.counterThrowSpeed = 9f; pc.counterThrowUp = 9f;     // niskalenkki (ennen 7 / 1.5)
            pc.monkeyFlipSpeed = 12f; pc.monkeyFlipUp = 10f;       // kuperkeikka (ennen 13 / 6)
            pc.kneeFlySpeed = 8f; pc.kneeFlyUp = 8f;               // polvi (ennen 6 / 4.5)
            pc.crateThrowSpeed = 11f; pc.crateThrowUp = 5f;        // laatikko/tynnyri (ennen 9 / 3)
            EditorUtility.SetDirty(pc);
        }
        if (count > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Info(
            $"Taktiikka asetettu {count} viholliselle.\n\n" +
            "Lippis: juoksee, kiertää selän taakse, iskee ja vetäytyy, torjuu (2 peräkkäin, sitten suoja murtuu). Potku nopeampi ja liukuu.\n" +
            "Punkkari: nopea, kiertää välillä.\nKovis: hidas ja suoraviivainen.\nPomo: torjuu ja kiertää.\n\n" +
            string.Join("\n", report) + "\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Tynnyrit ----------------
    const string BarrelPath = "Assets/Sprites/Rekvisiitta/tynnyri.png";
    const string BarrelRollPath = "Assets/Sprites/Rekvisiitta/tynnyri_pyorii.png";
    // katolla: (x katon vasemmasta reunasta, syvyys 0 = takareuna … 1 = etureuna)
    static readonly Vector2[] RoofBarrels = { new Vector2(8f, 0.15f), new Vector2(15f, 0.6f), new Vector2(24f, 0.2f), new Vector2(27.5f, 0.75f), new Vector2(33f, 0.3f) };

    [MenuItem("Beat em up/32. Tynnyrit (katto ja katu)")]
    static void AddBarrels()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        var street = GameObject.Find("Tausta");
        if (pc == null || street == null || AssetImporter.GetAtPath(BarrelPath) == null)
        {
            Info("Tarvitaan pelaaja, katutausta (kohta 4) ja kuva " + BarrelPath);
            return;
        }
        SetupAndSlice(BarrelPath);
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(BarrelPath).OfType<Sprite>().ToArray();
        // kyljellään: pyöriminen ja kanto
        Sprite[] roll = new Sprite[0];
        if (AssetImporter.GetAtPath(BarrelRollPath) != null)
        {
            SetupAndSlice(BarrelRollPath);
            roll = AssetDatabase.LoadAllAssetsAtPath(BarrelRollPath).OfType<Sprite>()
                .OrderBy(sp => int.TryParse(sp.name.Substring(sp.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        }
        var old = GameObject.Find("Tynnyrit");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = new GameObject("Tynnyrit");
        Undo.RegisterCreatedObjectUndo(root, "Tynnyrit");

        int count = 0;
        void Make(Vector3 pos)
        {
            var go = new GameObject("Tynnyri " + (++count));
            go.transform.SetParent(root.transform, false);
            go.transform.position = pos;
            var vis = new GameObject("Visual").AddComponent<SpriteRenderer>();
            vis.transform.SetParent(go.transform, false);
            var sh = new GameObject("Shadow").AddComponent<SpriteRenderer>();
            sh.transform.SetParent(go.transform, false);
            var c = go.AddComponent<Crate>();
            c.sprites = sprites;
            c.breakable = false;
            c.rollSprites = roll;
            c.visualScale = 1.2f;          // 20 % isompi
            c.throwDamage = 20;
            c.hitRadiusX = 0.95f;
            c.moneyChance = 0f; c.energyChance = 0f;
            c.body = vis;
            c.shadow = sh;
            vis.sprite = sprites.Length > 0 ? sprites[0] : null;
        }

        // katu: satunnaisesti (aina sama järjestys), ei ovien, pyörien eikä laatikoiden kohdalle
        var streetArea = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Katu");
        float curb = streetArea != null ? streetArea.curbDepthY : pc.curbDepthY;
        float wall = streetArea != null ? streetArea.maxDepthY : pc.maxDepthY;
        var blocked = StreetObstacles(true);
        const float barrelHalf = 0.65f;    // tynnyrin puolileveys 20 % isompana
        var ssr = street.GetComponent<SpriteRenderer>();
        float left = street.transform.position.x - ssr.size.x * 0.5f;
        float right = street.transform.position.x + ssr.size.x * 0.5f;
        var ext = GameObject.Find("Tausta jatko");
        if (ext != null) right = Mathf.Max(right, ext.GetComponent<SpriteRenderer>().bounds.max.x);
        var rnd = new System.Random(7);
        int streetCount = 0;
        for (float x = left + 14f; x < right - 8f; x += 18f + (float)rnd.NextDouble() * 22f)
        {
            float bx = x;
            for (int tries = 0; tries < 6 && Blocked(blocked, bx, barrelHalf); tries++) bx += 1.2f;
            if (Blocked(blocked, bx, barrelHalf)) continue;
            float k = (float)rnd.NextDouble();
            Make(new Vector3(bx, Mathf.Lerp(wall - 0.15f, curb + 0.2f, k), 0f));   // jalkakäytävällä
            streetCount++;
        }

        // katto
        var roof = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Katto");
        int roofCount = 0;
        if (roof != null)
            foreach (var v in RoofBarrels)
            {
                Make(new Vector3(RoofX0 + v.x, Mathf.Lerp(roof.maxDepthY - 0.2f, roof.minDepthY + 0.3f, v.y), 0f));
                roofCount++;
            }

        EditorSceneManager.MarkSceneDirty(root.scene);
        Info(
            $"Tynnyreitä: kadulla {streetCount}, katolla {roofCount}{(roof == null ? " (katto puuttuu, tee kohta 30)" : "")}.\n\n" +
            $"Pyörimiskuvat (tynnyri_pyorii.png): {(roll.Length > 0 ? roll.Length + " kuvaa" : "puuttuu")}\n\n" +
            "O tynnyrin vieressä nostaa (kannetaan vaakatasossa), lyönti/potku heittää. Heitetty tynnyri pyörii, vierii ja kaataa kaikki tieltään.\n" +
            "Lyönti tai potku maassa olevaan tynnyriin kaataa sen vierimään.\n" +
            "Pomo hakee tynnyrin, kun olet kaukana, ja paiskaa sen kovaa sinua kohti (hyppää yli tai väistä sivulle).\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- S-Clubin NPC ----------------
    [MenuItem("Beat em up/33. NPC baaritiskille (S-Club)")]
    static void AddClubNpc()
    {
        var club = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "S-Club");
        if (club == null) { Info("Tee ensin kohta 10 (S-Clubin sisätila)."); return; }
        string path = FindTexture("npc_nainen");
        if (path == null) { Info("npc_nainen.png puuttuu."); return; }
        SetupAndSlice(path);
        Sprite[] sp = LoadSprites("npc_nainen")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        var old = GameObject.Find("NPC: nainen");
        if (old != null) Undo.DestroyObjectImmediate(old);

        // tiskin vasempaan päähän asiakkaaksi, katse tiskin keskelle (oikealle)
        float counterX = ClubX0 + BarCounterPx / ClubPPU;
        var shop = GameObject.Find("Baaritiski");
        if (shop != null) counterX = shop.transform.position.x;
        float depth = club.maxDepthY - 0.15f;
        var go = new GameObject("NPC: nainen");
        go.transform.position = new Vector3(counterX - BarCounterHalfPx / ClubPPU * 0.6f, depth, 0f);
        var vis = new GameObject("Visual");
        vis.transform.SetParent(go.transform, false);
        vis.transform.localPosition = new Vector3(0f, -0.1f, 0f);   // kuvassa 10 px tyhjää jalkojen alla
        var sr = vis.AddComponent<SpriteRenderer>();
        sr.sprite = sp.Length > 0 ? sp[0] : null;
        sr.sortingOrder = Mathf.RoundToInt(-depth * 100f);
        // Viggle-videosta tehty idle (62 kuvaa, 12 fps): soitetaan silmukkana
        var npc = vis.AddComponent<Dancer>();
        npc.sprites = sp;
        npc.frameTime = 1f / 12f;
        var sh = new GameObject("Shadow").AddComponent<SpriteRenderer>();
        sh.transform.SetParent(go.transform, false);
        sh.sprite = PlayerController.CreateShadowSprite();
        sh.color = new Color(0f, 0f, 0f, 0.35f);
        sh.transform.localScale = new Vector3(1.4f, 0.42f, 1f);
        sh.sortingOrder = sr.sortingOrder - 1;
        Undo.RegisterCreatedObjectUndo(go, "NPC");
        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        Info($"NPC baaritiskillä ({sp.Length} kuvaa, idle-silmukka 12 kuvaa/s).\nSiirrä tarvittaessa Scene-näkymässä.\n\nTallenna scene (Ctrl+S).");
    }

    /// Punkkarin äänet: Assets/Audio/punkkari (nimi isoilla tai pienillä kirjaimilla): gasp* osumiin, attack* iskuihin.
    static void SetPunkSounds(Enemy e)
    {
        string folder = AssetDatabase.GetSubFolders("Assets/Audio")
            .FirstOrDefault(f => Path.GetFileName(f).ToLowerInvariant().StartsWith("punk"));
        if (folder == null) return;
        var gasps = LoadClips(folder, "gasp");
        var attacks = LoadClips(folder, "attack");
        if (gasps.Length > 0) { e.hurtSounds = gasps; e.hurtVolume = 0.79f; }        // 20 % hiljempaa (ennen 0.99)
        if (attacks.Length > 0) { e.attackSounds = attacks; e.attackVolume = 0.72f; } // 20 % hiljempaa (ennen 0.9)
        Debug.Log($"{e.name}: punkkarin äänet {folder}: gasp {gasps.Length}, attack {attacks.Length}");
    }

    /// Kadun esteet, joiden eteen rekvisiittaa ei laiteta: (keskikohta x, puolileveys).
    /// Moottoripyörät kuvan leveyden mukaan, ovet ja palotikkaat oven leveyden mukaan, halutessa myös laatikot.
    static List<Vector2> StreetObstacles(bool includeCrates)
    {
        var list = new List<Vector2>();
        var bikes = GameObject.Find("Moottoripyörät");
        if (bikes != null)
            foreach (Transform b in bikes.transform)
            {
                var rs = b.GetComponentsInChildren<SpriteRenderer>();
                float half = rs.Length > 0 ? rs.Max(r => r.bounds.extents.x) : 1.8f;
                list.Add(new Vector2(b.position.x, half));
            }
        foreach (var d in Object.FindObjectsByType<Door>(FindObjectsSortMode.None))
            list.Add(new Vector2(d.transform.position.x, Mathf.Max(d.halfWidth, 1.2f)));
        if (includeCrates)
            foreach (var c in Object.FindObjectsByType<Crate>(FindObjectsSortMode.None))
                list.Add(new Vector2(c.transform.position.x, 1.0f * c.visualScale));
        return list;
    }

    static bool Blocked(List<Vector2> obstacles, float x, float myHalf)
        => obstacles.Any(o => Mathf.Abs(o.x - x) < o.y + myHalf + 0.3f);

    // ---------------- Takakuja (katolta alas, prätkä) ----------------
    // takakuja.png: sama mittakaava ja korkeus kuin katu (seinän juuri 590, reunakivi 637, PPU 52).
    const string AlleyPath = "Assets/Sprites/Taustat/takakuja.png";
    const float AlleyX0 = 3000f;
    const float AlleyLadderPx = 285f, AlleyBikePx = 1446f;   // palotikkaiden juuri ja prätkän parkkiruutu kuvassa

    [MenuItem("Beat em up/34. Takakuja ja prätkä (katolta alas)")]
    static void CreateBackAlley()
    {
        var street = GameObject.Find("Tausta");
        var roofBg = GameObject.Find("Katto");
        var streetArea = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Katu");
        var roof = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Katto");
        var ti = AssetImporter.GetAtPath(AlleyPath) as TextureImporter;
        if (street == null || roofBg == null || streetArea == null || roof == null || ti == null)
        {
            Info("Tarvitaan katu (kohta 29), katto (kohta 30) ja kuva " + AlleyPath);
            return;
        }
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = BackgroundPPU;
        ti.filterMode = FilterMode.Bilinear;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.maxTextureSize = 4096;
        ti.mipmapEnabled = false;
        var st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        st.spriteMeshType = SpriteMeshType.FullRect;
        st.spriteAlignment = (int)SpriteAlignment.Center;
        ti.SetTextureSettings(st);
        ti.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AlleyPath);
        float wU = sprite.rect.width / BackgroundPPU;

        foreach (var n in new[] { "Takakuja", "Alue: Takakuja", "Takakujan ovet", "Takakujan prätkä", "Takakujan vanha pyörä" })
        {
            var o = GameObject.Find(n);
            if (o != null) Undo.DestroyObjectImmediate(o);
        }
        var bg = new GameObject("Takakuja");
        var sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -10000;
        bg.transform.position = new Vector3(AlleyX0 + wU * 0.5f, street.transform.position.y, 0f);   // sama korkeus kuin katu
        Undo.RegisterCreatedObjectUndo(bg, "Takakuja");

        float halfW = Camera.main != null ? Camera.main.orthographicSize * 16f / 9f : 10.7f;
        var alley = new GameObject("Alue: Takakuja").AddComponent<Area>();
        alley.areaName = "Takakuja";
        alley.minDepthY = streetArea.minDepthY;
        alley.maxDepthY = streetArea.maxDepthY;
        alley.useSidewalk = streetArea.useSidewalk;
        alley.sidewalkHeight = streetArea.sidewalkHeight;
        alley.curbDepthY = streetArea.curbDepthY;
        alley.camMinX = AlleyX0 + halfW;
        alley.camMaxX = AlleyX0 + wU - halfW;
        Undo.RegisterCreatedObjectUndo(alley.gameObject, "Alue");

        // ovet: katon oikeasta päästä alas, ja kujan palotikkailta takaisin ylös
        var doors = new GameObject("Takakujan ovet");
        Undo.RegisterCreatedObjectUndo(doors, "Ovet");
        float roofRight = roofBg.GetComponent<SpriteRenderer>().bounds.max.x;
        float ladderX = AlleyX0 + AlleyLadderPx / BackgroundPPU;
        var down = new GameObject("Katolta alas kujalle").AddComponent<Door>();
        down.transform.SetParent(doors.transform, false);
        down.transform.position = new Vector3(roofRight - 2.2f, roof.maxDepthY, 0f);
        down.prompt = "Laskeudu kujalle";
        down.here = roof; down.target = alley;
        down.spawnPoint = new Vector2(ladderX + 1.2f, alley.maxDepthY - 0.25f);
        down.halfWidth = 1.8f; down.maxDistanceFromWall = 100f; down.climbHeight = -1.5f;
        var up = new GameObject("Kujalta katolle").AddComponent<Door>();
        up.transform.SetParent(doors.transform, false);
        up.transform.position = new Vector3(ladderX, alley.maxDepthY - 0.25f, 0f);
        up.prompt = "Kiipeä katolle";
        up.here = alley; up.target = roof;
        up.spawnPoint = new Vector2(roofRight - 3.4f, Mathf.Lerp(roof.maxDepthY, roof.minDepthY, 0.75f));
        up.halfWidth = 1.2f; up.maxDistanceFromWall = 0.9f; up.climbHeight = 3f;

        // prätkä parkkiruutuun: kopio kadun ajettavasta pyörästä, nokka oikealle
        string bikeInfo = "prätkä puuttuu (tee kohta 16/29 ensin)";
        var template = Object.FindObjectsByType<Motorbike>(FindObjectsSortMode.None).FirstOrDefault();
        if (template != null)
        {
            var go = Object.Instantiate(template.gameObject);
            go.name = "Takakujan prätkä";
            float y = (streetArea.curbDepthY + streetArea.maxDepthY) * 0.5f;
            go.transform.position = new Vector3(AlleyX0 + AlleyBikePx / BackgroundPPU, y, 0f);
            var mb = go.GetComponent<Motorbike>();
            mb.rideable = true;
            if (mb.parkedRight != null) { mb.parked.sprite = mb.parkedRight; mb.parked.flipX = false; }
            else mb.parked.flipX = true;
            mb.parked.sortingOrder = Mathf.RoundToInt(-y * 100f);
            Undo.RegisterCreatedObjectUndo(go, "Prätkä");
            bikeInfo = $"prätkä parkkiruudussa (x = {go.transform.position.x:0.0})";
            // vanha punainen pyörä viereen (rekvisiittaa)
            var red = ImportProp(OldBikeRightPath) ?? ImportProp(OldBikePath);
            if (red != null)
            {
                var rGo = new GameObject("Takakujan vanha pyörä");
                rGo.transform.SetParent(go.transform.parent, false);
                rGo.transform.position = new Vector3(go.transform.position.x - 6f, y, 0f);
                var vis = new GameObject("Visual"); vis.transform.SetParent(rGo.transform, false);
                vis.transform.localPosition = new Vector3(0f, streetArea.sidewalkHeight, 0f);
                var rsr = vis.AddComponent<SpriteRenderer>();
                rsr.sprite = red;
                rsr.sortingOrder = Mathf.RoundToInt(-y * 100f);
                Undo.RegisterCreatedObjectUndo(rGo, "Vanha pyörä");
            }
        }
        EditorSceneManager.MarkSceneDirty(bg.scene);
        Info($"Takakuja luotu ({wU:0.0} yksikköä). Katon oikeasta päästä E: alas kujalle.\n{bikeInfo}.\nKujan palotikkailta pääsee takaisin katolle.\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Valtatie ----------------
    // valtatie_tie.png (1526 × 1024, toistuu) ja valtatie_maisema.png (kaukana, liikkuu hitaasti, ei toistu).
    const string HighwayRoadPath = "Assets/Sprites/Taustat/valtatie_tie.png";
    const string HighwayViewPath = "Assets/Sprites/Taustat/valtatie_maisema.png";
    const float HighwayX0 = 6000f, HighwayLength = 2500f, HighwayPPU = 85f;   // n. 3 min ajoa täydellä vauhdilla (13.75 yks/s): kaupungista saarelle
    const float HighwayRoadTopPx = 440f, HighwayRoadBottomPx = 990f;   // ajettava tie kuvassa
    const float HighwayViewAnchorPx = 560f, HighwayViewAtRoadPx = 330f; // maiseman rivi 560 tien rivin 330 kohdalle (kaiteen taakse)

    static Sprite ImportBg(string path, float ppu)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return null;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = ppu;
        ti.filterMode = FilterMode.Bilinear;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.maxTextureSize = 8192;
        ti.mipmapEnabled = false;
        ti.wrapMode = TextureWrapMode.Repeat;
        ti.alphaIsTransparency = true;
        var st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        st.spriteMeshType = SpriteMeshType.FullRect;          // tarvitaan toistuvaan (Tiled) piirtoon
        st.spriteAlignment = (int)SpriteAlignment.Center;
        ti.SetTextureSettings(st);
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    [MenuItem("Beat em up/35. Valtatie (kujan lopusta prätkällä)")]
    static void CreateHighway()
    {
        var cam = Camera.main;
        var alleyBg = GameObject.Find("Takakuja");
        var road = ImportBg(HighwayRoadPath, HighwayPPU);
        var view = ImportBg(HighwayViewPath, HighwayPPU);
        if (cam == null || alleyBg == null || road == null || view == null)
        {
            Info("Tarvitaan kamera, takakuja (kohta 34) ja kuvat " + HighwayRoadPath + " ja " + HighwayViewPath);
            return;
        }
        foreach (var n in new[] { "Valtatie", "Alue: Valtatie", "Kujan loppu" })
        {
            var o = GameObject.Find(n);
            if (o != null) Undo.DestroyObjectImmediate(o);
        }
        float camY = cam.transform.position.y;
        float halfW = cam.orthographicSize * 16f / 9f;
        float roadH = road.rect.height / HighwayPPU;
        float top = camY + roadH * 0.5f;

        var root = new GameObject("Valtatie");
        Undo.RegisterCreatedObjectUndo(root, "Valtatie");
        // tie: toistuu koko matkan
        var rGo = new GameObject("Tie");
        rGo.transform.SetParent(root.transform, false);
        var rs = rGo.AddComponent<SpriteRenderer>();
        rs.sprite = road;
        rs.drawMode = SpriteDrawMode.Tiled;
        rs.size = new Vector2(HighwayLength, roadH);
        rs.sortingOrder = -10000;
        rGo.transform.position = new Vector3(HighwayX0 + HighwayLength * 0.5f, camY, 0f);
        // maisema: kaukana kaiteen takana, liikkuu hitaasti
        var vGo = new GameObject("Maisema");
        vGo.transform.SetParent(root.transform, false);
        var vs = vGo.AddComponent<SpriteRenderer>();
        vs.sprite = view;
        vs.sortingOrder = -10001;
        float viewH = view.rect.height / HighwayPPU;
        float anchorY = top - HighwayViewAtRoadPx / HighwayPPU;                 // tien kuvan rivi 330
        float viewCenterY = anchorY + (HighwayViewAnchorPx - view.rect.height * 0.5f) / HighwayPPU;
        vGo.transform.position = new Vector3(HighwayX0, viewCenterY, 0f);
        var px = vGo.AddComponent<ParallaxLayer>();
        // maisema riittää koko matkalle: liukuu alusta loppuun tien ajon aikana (enintään 12 % tien vauhdista)
        float viewW = view.rect.width / HighwayPPU;
        px.factor = Mathf.Clamp((viewW - 2f * halfW) / Mathf.Max(1f, HighwayLength - 2f * halfW), 0.01f, 0.2f);
        px.startCamX = HighwayX0 + halfW;
        px.minX = HighwayX0;
        px.maxX = HighwayX0 + HighwayLength;

        var area = new GameObject("Alue: Valtatie").AddComponent<Area>();
        area.areaName = "Valtatie";
        area.maxDepthY = top - HighwayRoadTopPx / HighwayPPU;
        area.minDepthY = top - HighwayRoadBottomPx / HighwayPPU;
        area.useSidewalk = false;
        area.camMinX = HighwayX0 + halfW;
        area.camMaxX = HighwayX0 + HighwayLength - halfW;
        Undo.RegisterCreatedObjectUndo(area.gameObject, "Alue");

        // kujan lopussa siirtymä (prätkällä oikeasta reunasta ulos)
        float alleyRight = alleyBg.GetComponent<SpriteRenderer>().bounds.max.x;
        var exit = new GameObject("Kujan loppu").AddComponent<RideExit>();
        exit.transform.position = new Vector3(alleyRight - 4f, 0f, 0f);
        exit.target = area;
        exit.spawnPoint = new Vector2(HighwayX0 + 3f, (area.minDepthY + area.maxDepthY) * 0.5f);
        exit.title = "Valtatie";
        Undo.RegisterCreatedObjectUndo(exit.gameObject, "Kujan loppu");

        // vihuprätkät: malli (piilossa) ja lähettäjä
        string vp = FindTexture("vihu_pratka");
        if (vp != null)
        {
            SetupAndSlice(vp);
            var vs2 = LoadSprites("vihu_pratka").OrderBy(sp => int.TryParse(sp.name.Substring(sp.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
            var tGo = new GameObject("Vihuprätkä (malli)");
            tGo.transform.SetParent(root.transform, false);
            var b = new GameObject("Visual").AddComponent<SpriteRenderer>(); b.transform.SetParent(tGo.transform, false);
            var sh = new GameObject("Shadow").AddComponent<SpriteRenderer>(); sh.transform.SetParent(tGo.transform, false);
            sh.color = new Color(0f, 0f, 0f, 0.4f);
            var eb = tGo.AddComponent<EnemyBike>();
            eb.yankSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx/pratka_jarru_kolari.wav");
            eb.body = b; eb.shadow = sh; eb.sprites = vs2;
            eb.engineLoop = AssetDatabase.FindAssets("t:AudioClip sportbike", new[] { "Assets/Audio" })
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<AudioClip>).FirstOrDefault(clip => clip != null);
            Debug.Log("Vihuprätkän moottoriääni: " + (eb.engineLoop != null ? eb.engineLoop.name : "ei löytynyt (Assets/Audio/.../sportbike*)"));
            string vl = FindTexture("vihu_lento");
            if (vl != null) { SetupAndSlice(vl); eb.flySprites = LoadSprites("vihu_lento").OrderBy(sp => int.TryParse(sp.name.Substring(sp.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray(); }
            string ve = FindTexture("vihu_pyora_tyhja");
            if (ve != null) { SetupAndSlice(ve); eb.emptyBike = LoadSprites("vihu_pyora_tyhja").FirstOrDefault(); }
            string vk = FindTexture("vihu_pyora_kaatuu");
            if (vk != null) { SetupAndSlice(vk); eb.crashSprites = LoadSprites("vihu_pyora_kaatuu").OrderBy(sp => int.TryParse(sp.name.Substring(sp.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray(); }
            b.sprite = vs2.Length > 0 ? vs2[0] : null;
            tGo.SetActive(false);
            var spGo = new GameObject("Vihuprätkien lähettäjä");
            spGo.transform.SetParent(root.transform, false);
            var spn = spGo.AddComponent<EnemyBikeSpawner>();
            spn.template = eb; spn.minX = HighwayX0; spn.maxX = HighwayX0 + HighwayLength;
            AddSkaterHitch(spn);
        }

        EditorSceneManager.MarkSceneDirty(root.scene);
        Info($"Valtatie luotu ({HighwayLength:0} yksikköä). Aja prätkällä kujan oikeaan reunaan: pimennys, otsikko ja valtatie.\n" +
             $"Maisema liikkuu {px.factor * 100:0} % tien vauhdista.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/71. Valtatie: skeittari roikkuu vihuprätkän perässä")]
    static void AddSkaterHitchMenu()
    {
        var spn = Object.FindFirstObjectByType<EnemyBikeSpawner>(FindObjectsInactive.Include);
        if (spn == null) { Info("Valtatietä (vihuprätkien lähettäjää) ei löytynyt. Tee ensin kohta 35."); return; }
        string msg = AddSkaterHitch(spn);
        EditorSceneManager.MarkSceneDirty(spn.gameObject.scene);
        Info(msg + "\n\nTallenna scene (Ctrl+S).");
    }

    /// Skeittari vihuprätkän perässä: malli (piilossa) lähettäjän alle; lähettäjä liittää sen osaan prätkistä.
    static string AddSkaterHitch(EnemyBikeSpawner spn)
    {
        var report = new List<string>();
        Sprite[] Load(string n)
        {
            string tp = FindTexture(n);
            if (tp == null) { report.Add(n + ".png: puuttuu"); return new Sprite[0]; }
            SetupAndSlice(tp);
            var sp = LoadSprites(n).OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
            report.Add($"{n}.png: {sp.Length} kuvaa");
            return sp;
        }
        var tow = Load("skeittari_roikkuu"); var reach = Load("skeittari_kurotus"); var hold = Load("skeittari_jarrutus");
        var fall = EnemySheet("skettari_kaatuminen", report);
        var old = spn.transform.Find("Skeittari (malli)");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        var go = new GameObject("Skeittari (malli)");
        go.transform.SetParent(spn.transform, false);
        var b = new GameObject("Visual").AddComponent<SpriteRenderer>(); b.transform.SetParent(go.transform, false);
        var sh = new GameObject("Shadow").AddComponent<SpriteRenderer>(); sh.transform.SetParent(go.transform, false);
        sh.color = new Color(0f, 0f, 0f, 0.4f);
        var h = go.AddComponent<SkaterHitch>();
        h.body = b; h.shadow = sh;
        h.towSprites = tow; h.reachSprites = reach; h.holdSprites = hold; h.fallSprites = fall;
        if (spn.template != null) h.visualScale = spn.template.visualScale;
        b.sprite = tow.Length > 0 ? tow[0] : null;
        go.SetActive(false);
        Undo.RegisterCreatedObjectUndo(go, "Skeittari");
        Undo.RecordObject(spn, "Skeittari");
        spn.skaterTemplate = h;
        return "Skeittari lisätty valtatielle (n. 40 % vihuprätkistä).\n" + string.Join("\n", report);
    }

    // ---------------- Uccopulco (valtatien jälkeen) ----------------
    const string UccoPath = "Assets/Sprites/Taustat/uccopulco_katu.png";   // 4 kuvaa yhdistettynä, 5430 × 887
    const string UccoHarborPath = "Assets/Sprites/Taustat/uccopulco_satama.png";   // satamaosuus (3 Gemini-jatkokuvaa kohdistettuina kadun perään, vihreä tausta läpinäkyväksi)
    const string UccoSeaPath = "Assets/Sprites/Taustat/uccopulco_meri.png";
    const string UccoShipPath = "Assets/Sprites/Taustat/uccopulco_laiva.png";
    const float UccoHarborHorizonPx = 360f;   // meren horisontti satamakuvan rivillä (kaiteiden yläpuolella)
    const float UccoSeaHorizonPx = 490f;      // horisontti merikuvassa
    const float UccoShipQuayBottomPx = 724f, UccoShipQuayAtPx = 478f;   // laivakuvan laiturin yläreuna -> satamakuvan rivi (kaiteen aukon laiturinreuna)
    const float UccoShipScale = 0.7f, UccoShipOpeningPx = 3151f;          // laivan koko; kaiteen aukon keskikohta satamakuvassa
    const float UccoShipOffsetX = 4f;                                      // laivaa oikealle aukon keskeltä (yksikköä)
    const float UccoShipStretchX = 1.35f;
    const float UccoQuayEdgePx = 4772f;                                    // sataman lopun laiturin reuna (jalkakäytävän rivillä)                                  // laiva pidemmäksi (Geminin kuva on liian lyhyt)
    const float UccoX0 = 9000f;
    // Uccopulcon ja El Loipparin kuvat mahtuvat koko korkeudeltaan kameran ruutuun (ylhäällä kyltit, alhaalla kävelyalue)
    static float CamY => Camera.main != null ? Camera.main.transform.position.y : 1.8f;
    /// Mittakaava vanhaan (kadun kuvien) mittakaavaan nähden: kaikki yksikköinä annetut x-paikat kerrotaan tällä.
    const float UccoK = 0.8f;   // Uccopulcon katu 80 %: alareuna ruudun alareunaan (kävelyalue näkyy), yläosa rajautuu pois
    static float CamHalf => Camera.main != null ? Camera.main.orthographicSize : 6f;
    /// El Loippari: koko kuva ruudun korkuiseksi (kyltit ja lattia näkyvät), kamera ennallaan.
    static float LoipK => 2f * CamHalf / (1024f / BackgroundPPU);
    // kuvan rivit: jalkakäytävän pinta seinän vieressä, reunakiven alareuna (ajotien taso), ajotien alareuna
    const float UccoWallPx = 506f, UccoCurbPx = 552f, UccoBottomPx = 860f, UccoKerbPx = 30f;

    [MenuItem("Beat em up/44. Uccopulco (valtatien jälkeen)")]
    static void CreateUccopulco()
    {
        var street = GameObject.Find("Tausta");
        var highway = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Valtatie");
        var ti = AssetImporter.GetAtPath(UccoPath) as TextureImporter;
        if (street == null || highway == null || ti == null)
        {
            Info("Tarvitaan katu (kohta 29), valtatie (kohta 35) ja kuva " + UccoPath);
            return;
        }
        foreach (var n in new[] { "Uccopulco", "Alue: Uccopulco", "Valtatien loppu", "Uccopulco satama", "Uccopulco meri", "Uccopulco laiva", "Uccopulcon prätkä" })
        {
            var o = GameObject.Find(n);
            if (o != null) Undo.DestroyObjectImmediate(o);
        }
        // sama kuvakulma kuin kadun kuvissa: sama korkeus maailmassa (kadun kuva 1024 px / BackgroundPPU)
        // alkuperäinen korkeus (ei tuodun, mahdollisesti pienennetyn tekstuurin): sama korkeus maailmassa kuin kadun kuvissa
        ti.GetSourceTextureWidthAndHeight(out int srcW, out int srcH);
        float ppu = srcH / (1024f / BackgroundPPU * UccoK);   // tausta 80 %
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = ppu;
        ti.filterMode = FilterMode.Bilinear;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.maxTextureSize = 8192;
        ti.mipmapEnabled = false;
        var st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        st.spriteMeshType = SpriteMeshType.FullRect;
        st.spriteAlignment = (int)SpriteAlignment.Center;
        ti.SetTextureSettings(st);
        ti.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UccoPath);
        float wU = sprite.rect.width / ppu, hU = sprite.rect.height / ppu;

        var bg = new GameObject("Uccopulco");
        var sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -10000;
        float cy = CamY - CamHalf + hU * 0.5f;   // kuvan alareuna ruudun alareunaan
        bg.transform.position = new Vector3(UccoX0 + wU * 0.5f, cy, 0f);
        Undo.RegisterCreatedObjectUndo(bg, "Uccopulco");
        float top = cy + hU * 0.5f;

        float halfW = CamHalf * 16f / 9f;
        var area = new GameObject("Alue: Uccopulco").AddComponent<Area>();
        area.areaName = "Uccopulco";
        area.music = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Musiikki/Uccopulco.mp3");
        area.useSidewalk = true;
        area.sidewalkHeight = UccoKerbPx / ppu;
        area.curbDepthY = top - UccoCurbPx / ppu;
        area.maxDepthY = top - UccoWallPx / ppu - area.sidewalkHeight;   // jalat jalkakäytävällä seinän vieressä
        area.minDepthY = Mathf.Max(top - UccoBottomPx / ppu, CamY - CamHalf + 1.2f);   // jalat ja varjo pysyvät kuvassa
        area.camRiseY = Mathf.Min(2f, top - (CamY + CamHalf));   // kamera nousee seinän vieressä: rakennusten yläosa näkyviin
        area.camMinX = UccoX0 + halfW;
        area.camMaxX = UccoX0 + wU - halfW;
        Undo.RegisterCreatedObjectUndo(area.gameObject, "Alue");

        // satamaosuus kadun jatkeena: katu (läpinäkyvä taivas), takana risteilyalus ja meri parallaksina
        Sprite ImportBg(string path, float bgPpu)
        {
            var bti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (bti == null) return null;
            bti.textureType = TextureImporterType.Sprite;
            bti.spriteImportMode = SpriteImportMode.Single;
            bti.spritePixelsPerUnit = bgPpu;
            bti.filterMode = FilterMode.Bilinear;
            bti.textureCompression = TextureImporterCompression.Uncompressed;
            bti.maxTextureSize = 8192;
            bti.mipmapEnabled = false;
            bti.alphaIsTransparency = true;
            var bst = new TextureImporterSettings(); bti.ReadTextureSettings(bst);
            bst.spriteMeshType = SpriteMeshType.FullRect; bst.spriteAlignment = (int)SpriteAlignment.Center;
            bti.SetTextureSettings(bst);
            bti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        var harbor = File.Exists(UccoHarborPath) ? ImportBg(UccoHarborPath, ppu) : null;   // satama vain, jos kuva on olemassa
        if (harbor != null)
        {
            float wS = harbor.rect.width / ppu;
            var hGo = new GameObject("Uccopulco satama");
            var hSr = hGo.AddComponent<SpriteRenderer>(); hSr.sprite = harbor; hSr.sortingOrder = -10000;
            hGo.transform.position = new Vector3(UccoX0 + wU + wS * 0.5f, cy, 0f);
            Undo.RegisterCreatedObjectUndo(hGo, "Satama");
            area.camMaxX = UccoX0 + wU + wS - halfW;
            float sx = UccoX0 + wU;                                   // sataman alku
            area.walkMaxX = sx + UccoQuayEdgePx / ppu - 0.6f;         // laiturin reuna: ei kävellä veteen
            // risteilyalus laiturissa: vähän nopeampi (40 %), laiturin muuri kadun kaiteiden taakse
            float shipPpu = ppu / UccoShipScale;
            var ship = File.Exists(UccoShipPath) ? ImportBg(UccoShipPath, shipPpu) : null;
            if (ship != null)
            {
                var lGo = new GameObject("Uccopulco laiva");
                var lSr = lGo.AddComponent<SpriteRenderer>(); lSr.sprite = ship; lSr.sortingOrder = -10050;
                float shipH = ship.rect.height / shipPpu;
                float shipTop = top - UccoShipQuayAtPx / ppu + UccoShipQuayBottomPx / shipPpu;
                // laiva kaiteen aukon kohdalla, kun kamera on aukon keskellä
                float openX = sx + UccoShipOpeningPx / ppu;
                var pl = lGo.AddComponent<AnchoredParallax>();
                pl.speed = 0.55f; pl.camRef = new Vector2(Mathf.Clamp(openX, area.camMinX, area.camMaxX), CamY);
                pl.anchor = new Vector2(openX + UccoShipOffsetX, shipTop - shipH * 0.5f);
                lGo.transform.position = new Vector3(pl.anchor.x, pl.anchor.y, 0f);
                lGo.transform.localScale = new Vector3(UccoShipStretchX, 1f, 1f);
                Undo.RegisterCreatedObjectUndo(lGo, "Laiva");
            }
        }
        // meri koko Uccopulcon taakse (vanhan kadun taivas ja meri on leikattu pois): hidas (15 %), horisontti kaiteiden yläpuolelle
        {
            float seaScale = 1.15f;
            var sea = File.Exists(UccoSeaPath) ? ImportBg(UccoSeaPath, ppu / seaScale) : null;
            if (sea != null)
            {
                var sGo = new GameObject("Uccopulco meri");
                var sSr = sGo.AddComponent<SpriteRenderer>(); sSr.sprite = sea; sSr.sortingOrder = -10100;
                float seaH = sea.rect.height / (ppu / seaScale);
                float horizonY = top - UccoHarborHorizonPx / ppu;
                float seaTop = horizonY + UccoSeaHorizonPx * seaScale / ppu;
                var camRef = new Vector2((area.camMinX + area.camMaxX) * 0.5f, CamY);   // kameran keskikohta koko kadulla
                var pl = sGo.AddComponent<AnchoredParallax>();
                pl.speed = 0.15f; pl.camRef = camRef; pl.anchor = new Vector2(camRef.x, seaTop - seaH * 0.5f);
                sGo.transform.position = new Vector3(pl.anchor.x, pl.anchor.y, 0f);
                Undo.RegisterCreatedObjectUndo(sGo, "Meri");
            }
        }

        // valtatien lopussa siirtymä prätkällä
        var exit = new GameObject("Valtatien loppu").AddComponent<RideExit>();
        exit.transform.position = new Vector3(highway.camMaxX + halfW - 6f, 0f, 0f);
        exit.target = area;
        exit.spawnPoint = new Vector2(UccoX0 + 4f, Mathf.Lerp(area.curbDepthY, area.minDepthY, 0.4f));
        exit.title = "Uccopulco";
        exit.parkBike = true;                                   // pyörä jää parkkiin, peli jatkuu jalan
        exit.parkPoint = new Vector2(UccoX0 + 6f, area.curbDepthY + 0.35f);   // jalkakäytävän reunaan
        Undo.RegisterCreatedObjectUndo(exit.gameObject, "Valtatien loppu");

        // ajettava prätkä kadun alkuun (testaukseen: kentän läpi nopeasti), kopio takakujan pyörästä, nokka oikealle
        var bikeT = Object.FindObjectsByType<Motorbike>(FindObjectsSortMode.None).FirstOrDefault(m => m.gameObject.name == "Takakujan prätkä");
        if (bikeT != null)
        {
            var go = Object.Instantiate(bikeT.gameObject);
            go.name = "Uccopulcon prätkä";
            float by = area.curbDepthY + 0.35f;
            go.transform.position = new Vector3(UccoX0 + 12f, by, 0f);
            var mb = go.GetComponent<Motorbike>();
            mb.rideable = true;
            if (mb.parkedRight != null) { mb.parked.sprite = mb.parkedRight; mb.parked.flipX = false; }
            else mb.parked.flipX = true;
            mb.parked.sortingOrder = Mathf.RoundToInt(-by * 100f);
            Undo.RegisterCreatedObjectUndo(go, "Prätkä");
        }

        EditorSceneManager.MarkSceneDirty(bg.scene);
        Info($"Uccopulco luotu ({wU:0} yksikköä, kuva {ppu:0.0} px/yks).\nAja valtatien loppuun: pimennys, otsikko ja rantakatu.\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- El Loippari (Uccopulcon baari) ----------------
    // ---------------- Laivan kannen pöydät ja tuolit ----------------
    // kannen kuvan x (px), syvyys 0 = kaide … 1 = edessä; tanssipaikka (x 1950–2600) jätetään tyhjäksi
    static readonly Vector2[] ShipTableSpots = {
        new Vector2(800f, 0.45f), new Vector2(1150f, 0.8f), new Vector2(1500f, 0.5f),
        new Vector2(2800f, 0.75f), new Vector2(3150f, 0.45f), new Vector2(3500f, 0.8f) };
    static readonly Vector2[] ShipChairSpots = {
        new Vector2(700f, 0.55f), new Vector2(920f, 0.5f), new Vector2(1280f, 0.85f), new Vector2(1640f, 0.6f),
        new Vector2(2700f, 0.85f), new Vector2(3260f, 0.5f), new Vector2(3400f, 0.85f) };

    [MenuItem("Beat em up/57. Laivan kannelle pöydät, kala-annokset, pullot ja tuolit")]
    static void AddShipProps()
    {
        var deck = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Laivan kansi");
        var deckBg = GameObject.Find("Laivan kansi");
        var loipTables = GameObject.Find("El Loipparin pöydät");
        if (deck == null || deckBg == null || loipTables == null) { Info("Tarvitaan laivan kansi (55) ja El Loipparin pöydät (48)."); return; }
        var old = GameObject.Find("Laivan pöydät");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = new GameObject("Laivan pöydät");
        Undo.RegisterCreatedObjectUndo(root, "Laivan pöydät");
        var bsr = deckBg.GetComponent<SpriteRenderer>();
        float ppu = bsr.sprite.pixelsPerUnit, left = bsr.bounds.min.x;
        // mallit: El Loipparin suorakaidepöydät (kala-annoksineen, pulloineen ja laseineen)
        var tables = loipTables.GetComponentsInChildren<Crate>().Where(c => c.gameObject.name == "Pöytä").ToArray();
        var plates = loipTables.GetComponentsInChildren<FishPlate>();
        var bottles = loipTables.GetComponentsInChildren<Bottle>();
        int nt = 0, np = 0, nb = 0;
        for (int i = 0; i < ShipTableSpots.Length && tables.Length > 0; i++)
        {
            var src = tables[(i * 3 + 1) % tables.Length];
            var v = ShipTableSpots[i];
            var pos = new Vector3(left + v.x / ppu, Mathf.Lerp(deck.maxDepthY - 0.3f, deck.minDepthY + 0.4f, v.y), 0f);
            var t = Object.Instantiate(src.gameObject, root.transform);
            t.name = "Pöytä"; t.transform.position = pos;
            var tc = t.GetComponent<Crate>();
            nt++;
            foreach (var f in plates.Where(f => f.table == src))
            {
                var g = Object.Instantiate(f.gameObject, root.transform); g.transform.position = pos;
                g.GetComponent<FishPlate>().table = tc; np++;
            }
            foreach (var b in bottles.Where(b => b.table == src))
            {
                var g = Object.Instantiate(b.gameObject, root.transform); g.transform.position = pos;
                g.GetComponent<Bottle>().table = tc; nb++;
            }
        }
        // tuolit: kopiot El Loipparin tuoleista
        var chairs = GameObject.Find("Tuolit");
        int nc = 0;
        if (chairs != null)
        {
            var cs = chairs.GetComponentsInChildren<Chair>();
            for (int i = 0; i < ShipChairSpots.Length && cs.Length > 0; i++)
            {
                var v = ShipChairSpots[i];
                var g = Object.Instantiate(cs[i % cs.Length].gameObject, root.transform);
                g.name = "Tuoli " + (++nc);
                g.transform.position = new Vector3(left + v.x / ppu, Mathf.Lerp(deck.maxDepthY - 0.3f, deck.minDepthY + 0.4f, v.y), 0f);
                var vis = g.GetComponentInChildren<SpriteRenderer>(); if (vis != null) vis.flipX = i % 2 == 1;
            }
        }
        // yösävy myös näille
        var night = deckBg.GetComponent<ShipNight>();
        if (night != null) night.tinted = (night.tinted ?? new SpriteRenderer[0]).Concat(root.GetComponentsInChildren<SpriteRenderer>()).ToArray();
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info($"Laivan kannelle {nt} pöytää ({np} kala-annosta, {nb} pulloa ja lasia) ja {nc} tuolia.\nTanssipaikka baarin edessä on vapaana.\n\nTallenna scene (Ctrl+S).");
    }


    // ---------------- Laivan sisätilat ----------------
    const float ShipCorridorX0 = 22000f, ShipCabinX0 = 30000f, ShipSalonX0 = 34000f;
    // käytävä (laiva_kaytava.png, 3 jaksoa à 1568 px): seinän juuri rivillä 525; avautuva ovi toisen jakson keskellä
    const float CorridorFloorPx = 528f, CorridorTile = 1568f, CorridorDoorRoiX = 680f - 52f, CorridorDoorRoiBottom = 545f, CorridorDoorCenterPx = 811f - 52f;
    const float CabinFloorPx = 548f;
    // sali (laiva_sali.png 2584 × 941): baaritiski 0–690 (etureuna rivillä 472), seinän juuri 420, oikea seinä viistoon
    const float SalonBarEndPx = 690f, SalonBarFrontPx = 474f, SalonWallPx = 420f, SalonCornerPx = 1890f, SalonRightBottomPx = 812f, SalonHorizonPx = 190f;

    [MenuItem("Beat em up/60. Laivan sisätilat: käytävä, hytti ja sali")]
    static void CreateShipInterior()
    {
        var deck = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Laivan kansi");
        var deckBg = GameObject.Find("Laivan kansi");
        if (deck == null || deckBg == null) { Info("Tee ensin kohta 55 (laivan kansi)."); return; }
        foreach (var n in new[] { "Laivan käytävä", "Laivan hytti", "Laivan sali",
                                  "Laivan sisäovet", "Salin meri kaukana", "Salin meri lähellä", "Käytävän hyttiovi" })
        {
            var o = GameObject.Find(n);
            if (o != null) Undo.DestroyObjectImmediate(o);
        }
        float halfW = CamHalf * 16f / 9f;
        float cy = CamY;
        Sprite Import(string path, float ppu, bool tiled)
        {
            var t = AssetImporter.GetAtPath(path) as TextureImporter;
            if (t == null) return null;
            t.textureType = TextureImporterType.Sprite; t.spriteImportMode = SpriteImportMode.Single;
            t.spritePixelsPerUnit = ppu; t.filterMode = FilterMode.Bilinear;
            t.textureCompression = TextureImporterCompression.Uncompressed; t.maxTextureSize = 8192;
            t.mipmapEnabled = false; t.alphaIsTransparency = true;
            t.wrapMode = tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            var st = new TextureImporterSettings(); t.ReadTextureSettings(st);
            st.spriteMeshType = SpriteMeshType.FullRect; st.spriteAlignment = (int)SpriteAlignment.Center;
            t.SetTextureSettings(st); t.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        // koko kuva ruudun korkuiseksi (kuten El Loippari)
        (GameObject bg, Area area, float ppu, float left, float top) Room(string name, string path, float x0, float floorPx)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return (null, null, 0f, 0f, 0f);
            ti.GetSourceTextureWidthAndHeight(out int w, out int h);
            float ppu = h / (2f * CamHalf);
            var spr = Import(path, ppu, false);
            float wU = w / ppu, hU = h / ppu;
            var bg = new GameObject(name);
            var sr = bg.AddComponent<SpriteRenderer>(); sr.sprite = spr; sr.sortingOrder = -10000;
            bg.transform.position = new Vector3(x0 + wU * 0.5f, cy, 0f);
            Undo.RegisterCreatedObjectUndo(bg, name);
            float top = cy + hU * 0.5f;
            var area = ReuseArea("Alue: " + name);   // sama alue: salin tappelu, kauppa ja naiset viittaavat siihen
            area.areaName = name;
            area.music = deck.music;
            area.useSidewalk = false;
            area.maxDepthY = top - floorPx / ppu;
            area.minDepthY = CamY - CamHalf + 1.2f;
            area.camMinX = x0 + halfW;
            area.camMaxX = Mathf.Max(area.camMinX, x0 + wU - halfW);
            return (bg, area, ppu, x0, top);
        }
        var cor = Room("Laivan käytävä", "Assets/Sprites/Taustat/laiva_kaytava.png", ShipCorridorX0, CorridorFloorPx);
        var cab = Room("Laivan hytti", "Assets/Sprites/Taustat/laiva_hytti.png", ShipCabinX0, CabinFloorPx);
        var sal = Room("Laivan sali", "Assets/Sprites/Taustat/laiva_sali.png", ShipSalonX0, SalonWallPx);
        if (cor.area == null || cab.area == null || sal.area == null) { Info("Sisätilojen kuvia puuttuu (laiva_kaytava, laiva_hytti, laiva_sali)."); return; }
        float CX(float px) => cor.left + px / cor.ppu;
        float SX(float px) => sal.left + px / sal.ppu;
        float SY(float row) => sal.top - row / sal.ppu;
        cor.area.walkMinX = CX(60f); cor.area.walkMaxX = CX(CorridorTile * 3f - 60f);
        cab.area.walkMinX = cab.left + 1.2f;
        // salin takaraja: baaritiskin etureuna, seinän juuri ja viisto oikea seinä
        sal.area.depthLimits = new[] {
            new Vector2(SX(0f), SY(SalonBarFrontPx)), new Vector2(SX(SalonBarEndPx), SY(SalonBarFrontPx)),
            new Vector2(SX(SalonBarEndPx + 40f), SY(SalonWallPx + 12f)), new Vector2(SX(SalonCornerPx), SY(SalonWallPx + 12f)),
            new Vector2(SX(2584f), SY(SalonRightBottomPx)) };
        sal.area.walkMaxX = SX(2584f) - 1.0f;

        // salin ikkunoista näkyy rullaava meri (samat kuvat kuin kannella)
        void Sea(string name, string path, float scaleRows, float topRow, int order, float speed, float parallax)
        {
            float p = sal.ppu / scaleRows;
            var spr = Import(path, p, true);
            if (spr == null) return;
            var go = new GameObject(name);
            var s = go.AddComponent<SpriteRenderer>(); s.sprite = spr; s.sortingOrder = order; s.drawMode = SpriteDrawMode.Tiled;
            float w = spr.rect.width / p, h = spr.rect.height / p;
            s.size = new Vector2(Mathf.Ceil((2f * halfW) / w + 2f) * w, h);
            go.transform.position = new Vector3(ShipSalonX0 + 10f, SY(topRow) - h * 0.5f, 0f);
            var sl = go.AddComponent<ScrollingLayer>(); sl.autoSpeed = speed; sl.parallax = parallax; sl.area = sal.area;
            sl.bobAmplitude = 0.15f; sl.bobPeriod = 7f;
            Undo.RegisterCreatedObjectUndo(go, name);
        }
        // yö: kuunvalossa välkehtivä meri (samat yökuvat kuin kannen tanssissa)
        string farNight = File.Exists("Assets/Sprites/Taustat/laiva_meri_kauko_yo.png") ? "Assets/Sprites/Taustat/laiva_meri_kauko_yo.png" : ShipSeaFarPath;
        string nearNight = File.Exists("Assets/Sprites/Taustat/laiva_meri_lahi_yo.png") ? "Assets/Sprites/Taustat/laiva_meri_lahi_yo.png" : ShipSeaNearPath;
        Sea("Salin meri kaukana", farNight, 0.6f, SalonHorizonPx - 291f * 0.6f, -10100, 0.35f, 0.04f);
        Sea("Salin meri lähellä", nearNight, 0.35f, SalonHorizonPx + 30f, -10090, 3f, 0.4f);
        // sali hämärämmäksi ja ikkunoista lattialle lankeava kuunvalo (lattian päällä, hahmojen ja pöytien alla)
        sal.bg.GetComponent<SpriteRenderer>().color = new Color(0.8f, 0.82f, 0.95f);
        var moonSpr = Import("Assets/Sprites/Taustat/laiva_sali_kuunvalo.png", sal.ppu, false);
        if (moonSpr != null)
        {
            var mGo = new GameObject("Salin kuunvalo");
            mGo.transform.SetParent(sal.bg.transform, false);
            var msr = mGo.AddComponent<SpriteRenderer>(); msr.sprite = moonSpr; msr.sortingOrder = -9990;
            var sh = mGo.AddComponent<MoonlightShimmer>(); sh.baseAlpha = 0.5f; sh.variation = 0.3f;   // himmeämpi, vaihtelee hillitysti
        }

        // käytävän avautuva hyttiovi (videosta)
        AnimatedDoor cabinAnim = null;
        string op = FindTexture("laiva_kaytava_ovi");
        if (op != null)
        {
            SetupAndSlice(op);
            var fr = LoadSprites("laiva_kaytava_ovi").OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
            if (fr.Length >= 2)
            {
                var go = new GameObject("Käytävän hyttiovi");
                float k100 = fr[0].pixelsPerUnit / cor.ppu;
                go.transform.position = new Vector3(CX(CorridorTile + CorridorDoorRoiX + 165f), cor.top - CorridorDoorRoiBottom / cor.ppu, 0f);
                go.transform.localScale = new Vector3(k100, k100, 1f);
                var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = fr[0]; sr.sortingOrder = -9990;
                cabinAnim = go.AddComponent<AnimatedDoor>(); cabinAnim.body = sr; cabinAnim.frames = fr; cabinAnim.frameTime = 0.06f;
                Undo.RegisterCreatedObjectUndo(go, "Hyttiovi");
            }
        }

        // ovet
        var doors = new GameObject("Laivan sisäovet");
        Undo.RegisterCreatedObjectUndo(doors, "Ovet");
        Door MakeDoor(string name, string prompt, Area here, Area target, Vector3 pos, Vector2 spawn, float halfWidth = 1.4f, float wall = 0.9f)
        {
            var d = new GameObject(name).AddComponent<Door>();
            d.transform.SetParent(doors.transform, false);
            d.transform.position = pos; d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = spawn;
            d.halfWidth = halfWidth; d.maxDistanceFromWall = wall;
            return d;
        }
        // kannen hyttiovi: viistossa seinässä, joten seinän juuri on oven kohdalla lähempänä
        var dbsr = deckBg.GetComponent<SpriteRenderer>();
        float dppu = dbsr.sprite.pixelsPerUnit, dleft = dbsr.bounds.min.x;
        float doorX = dleft + ShipDoorXPx / dppu;
        float tWall = Mathf.InverseLerp(ShipWallTopXPx, ShipWallBottomXPx, ShipDoorXPx);
        float wallYAtDoor = Mathf.Lerp(deck.maxDepthY, deck.minDepthY, tWall);
        float corMid = Mathf.Lerp(cor.area.maxDepthY, cor.area.minDepthY, 0.3f);
        var inDoor = MakeDoor("Kannelta käytävään", "Mene sisään", deck, cor.area, new Vector3(doorX - 0.6f, deck.maxDepthY, 0f),
                              new Vector2(CX(420f), corMid), 1.8f, deck.maxDepthY - wallYAtDoor + 1.0f);
        inDoor.blockedDuringFight = true;
        var deckAnimGo = GameObject.Find("Hyttiovi");
        if (deckAnimGo != null) inDoor.anim = deckAnimGo.GetComponent<AnimatedDoor>();
        MakeDoor("Käytävästä kannelle", "Kannelle", cor.area, deck, new Vector3(CX(140f), cor.area.maxDepthY, 0f),
                 new Vector2(doorX - 1.6f, wallYAtDoor - 0.4f), 1.6f, 1.2f);
        float cabDoorX = CX(CorridorTile + CorridorDoorCenterPx);
        var toCabin = MakeDoor("Käytävästä hyttiin", "Mene hyttiin", cor.area, cab.area, new Vector3(cabDoorX, cor.area.maxDepthY, 0f),
                               new Vector2(cab.left + 4.5f, Mathf.Lerp(cab.area.maxDepthY, cab.area.minDepthY, 0.35f)), 1.3f, 1.0f);
        toCabin.anim = cabinAnim;
        MakeDoor("Hytistä käytävään", "Käytävään", cab.area, cor.area, new Vector3(cab.left + 1.6f, cab.area.maxDepthY, 0f),
                 new Vector2(cabDoorX, cor.area.maxDepthY - 0.3f), 1.8f, 1.5f);
        MakeDoor("Käytävästä saliin", "Saliin", cor.area, sal.area, new Vector3(CX(CorridorTile * 3f - 140f), cor.area.maxDepthY, 0f),
                 new Vector2(SX(160f) + 1.5f, Mathf.Lerp(SY(SalonBarFrontPx), sal.area.minDepthY, 0.3f)), 1.6f, 1.2f);
        MakeDoor("Salista käytävään", "Käytävään", sal.area, cor.area, new Vector3(SX(80f), SY(SalonBarFrontPx), 0f),
                 new Vector2(CX(CorridorTile * 3f - 300f), corMid), 1.6f, 1.5f);
        EditorSceneManager.MarkSceneDirty(doors.scene);
        Info("Laivan sisätilat luotu:\n- Käytävä (3 jaksoa), kannen hyttiovesta (E, ei tappelun aikana)\n- Hytti: käytävän keskimmäisestä ovesta (ovi aukeaa)\n- Sali: käytävän oikeasta päästä; ikkunoista näkyy meri\n\nTallenna scene (Ctrl+S).");
    }


    // salin vasen jatke (laiva_sali_portaat.png 793 × 941): portaat komentosillalle ja VAIN MIEHISTÖLLE -kyltti.
    // Kuva liittyy salin vasempaan reunaan kohdassa 767 px (viimeiset 26 px häivyttyvät salin päälle).
    const float SalonStairsJoinPx = 767f, SalonStairsDoorPx = 520f, SalonStairsFrontPx = 495f;
    const string BridgeAreaName = "Komentosilta";

    [MenuItem("Beat em up/83. Laivan saliin portaat komentosillalle (vasen jatke)")]
    static void AddSalonStairs()
    {
        const string path = "Assets/Sprites/Taustat/laiva_sali_portaat.png";
        var sal = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Laivan sali");
        var salBg = GameObject.Find("Laivan sali");
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (sal == null || salBg == null || ti == null) { Info("Tarvitaan laivan sali (kohta 60) ja " + path); return; }
        foreach (var n in new[] { "Salin portaat", "Salista komentosillalle" })
        {
            var o = GameObject.Find(n);
            if (o != null) Undo.DestroyObjectImmediate(o);
        }
        var salSr = salBg.GetComponent<SpriteRenderer>();
        float ppu = salSr.sprite.pixelsPerUnit;
        float left0 = salSr.bounds.min.x, top = salSr.bounds.max.y;
        ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = ppu; ti.filterMode = FilterMode.Bilinear; ti.alphaIsTransparency = true;
        ti.textureCompression = TextureImporterCompression.Uncompressed; ti.maxTextureSize = 8192; ti.mipmapEnabled = false;
        var st = new TextureImporterSettings(); ti.ReadTextureSettings(st);
        st.spriteMeshType = SpriteMeshType.FullRect; st.spriteAlignment = (int)SpriteAlignment.TopLeft;
        ti.SetTextureSettings(st); ti.SaveAndReimport();
        var spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        float extLeft = left0 - SalonStairsJoinPx / ppu;
        float EX(float px) => extLeft + px / ppu;
        float SY(float row) => top - row / ppu;

        // kuva salin vasemmalle puolelle; häivytetty reuna salin kuvan päälle, sama hämärä sävy
        var go = new GameObject("Salin portaat");
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = spr; sr.sortingOrder = -9999; sr.color = salSr.color;
        go.transform.position = new Vector3(extLeft, top, 0f);
        Undo.RegisterCreatedObjectUndo(go, "Salin portaat");

        // kamera ja kävely ulottuvat jatkeeseen; takaraja portaiden ja kyltin edessä, liittyy baaritiskin etureunaan
        Undo.RecordObject(sal, "Salin jatke");
        float halfW = CamHalf * 16f / 9f;
        sal.camMinX = extLeft + halfW;
        sal.walkMinX = extLeft + 0.6f;
        var lim = (sal.depthLimits ?? new Vector2[0]).Where(v => v.x >= left0 - 0.01f).ToList();
        lim.Insert(0, new Vector2(EX(SalonStairsJoinPx - 10f), SY(SalonStairsFrontPx)));
        lim.Insert(0, new Vector2(EX(0f), SY(SalonStairsFrontPx)));
        sal.depthLimits = lim.ToArray();
        EditorUtility.SetDirty(sal);

        // portaat ylös: ovi komentosillalle, kun komentosilta on tehty
        string doorInfo = SalonBridgeDoors() ? "Portaista pääsee komentosillalle (ei tappelun aikana)."
            : "Komentosiltaa ei vielä ole: portaiden ovi lisätään, kun teet komentosillan (kohta 84).";
        EditorSceneManager.MarkSceneDirty(go.scene);
        Info($"Salin vasen jatke ja portaat lisätty ({spr.rect.width / ppu:0.0} yksikköä). Kamera ja kävely ulottuvat portaille asti.\n{doorInfo}\n\nTallenna scene (Ctrl+S).");
    }

    /// Ovet salin portailta komentosillalle ja takaisin (kun molemmat on tehty). Palauttaa, onnistuiko.
    static bool SalonBridgeDoors()
    {
        var sal = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Laivan sali");
        var bridge = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == BridgeAreaName);
        var stairs = GameObject.Find("Salin portaat");
        var bridgeBg = GameObject.Find(BridgeAreaName);
        if (sal == null || bridge == null || stairs == null || bridgeBg == null) return false;
        foreach (var n in new[] { "Salista komentosillalle", "Komentosillalta saliin" })
        {
            var o = GameObject.Find(n);
            if (o != null) Undo.DestroyObjectImmediate(o);
        }
        var ssr = stairs.GetComponent<SpriteRenderer>(); var bsr = bridgeBg.GetComponent<SpriteRenderer>();
        float sppu = ssr.sprite.pixelsPerUnit, bppu = bsr.sprite.pixelsPerUnit;
        float extLeft = ssr.bounds.min.x, sTop = ssr.bounds.max.y, bLeft = bsr.bounds.min.x, bTop = bsr.bounds.max.y;
        float stairX = extLeft + SalonStairsDoorPx / sppu, stairY = sTop - SalonStairsFrontPx / sppu;
        float bDoorX = bLeft + BridgeDoorPx / bppu, bDoorY = bTop - BridgeWallPx / bppu;
        var up = new GameObject("Salista komentosillalle").AddComponent<Door>();
        up.transform.position = new Vector3(stairX, stairY, 0f);
        up.prompt = "Komentosillalle"; up.here = sal; up.target = bridge;
        up.spawnPoint = new Vector2(bDoorX + 1.6f, Mathf.Lerp(bridge.maxDepthY, bridge.minDepthY, 0.25f));
        up.halfWidth = 1.5f; up.maxDistanceFromWall = 1.2f; up.blockedDuringFight = true;
        Undo.RegisterCreatedObjectUndo(up.gameObject, "Ovi");
        var down = new GameObject("Komentosillalta saliin").AddComponent<Door>();
        down.transform.position = new Vector3(bDoorX, bDoorY, 0f);
        down.prompt = "Saliin"; down.here = bridge; down.target = sal;
        down.spawnPoint = new Vector2(stairX + 1.4f, stairY - 0.4f);
        down.halfWidth = 1.4f; down.maxDistanceFromWall = 1.2f; down.blockedDuringFight = true;
        Undo.RegisterCreatedObjectUndo(down.gameObject, "Ovi");
        return true;
    }

    // komentosilta (laiva_komentosilta.png 2240 × 887, kahdesta kuvasta yhdistetty): ovi vasemmalla, ruori keskellä,
    // karttapöytä oikealla ja oikea seinä viistoon. Ikkunat läpinäkyviä: takana rullaa yöllinen meri.
    const float ShipBridgeX0 = 70000f, BridgeWallPx = 505f, BridgeDoorPx = 90f, BridgeHorizonPx = 180f;
    static readonly Vector2[] BridgeBackLine = {   // (x px, rivi px): takaraja ruorin ja karttapöydän edessä, oikealla viisto seinä
        new Vector2(0f, 505f), new Vector2(700f, 505f), new Vector2(725f, 535f), new Vector2(1000f, 535f), new Vector2(1020f, 505f),
        new Vector2(1320f, 505f), new Vector2(1345f, 555f), new Vector2(1840f, 555f), new Vector2(1880f, 515f), new Vector2(1950f, 505f),
        new Vector2(2240f, 690f) };

    [MenuItem("Beat em up/84. Laivan komentosilta (salin portaista ylös)")]
    static void CreateShipBridge()
    {
        const string path = "Assets/Sprites/Taustat/laiva_komentosilta.png";
        var sal = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Laivan sali");
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (sal == null || ti == null) { Info("Tarvitaan laivan sali (kohta 60) ja " + path); return; }
        foreach (var n in new[] { BridgeAreaName, "Komentosillan meri kaukana", "Komentosillan meri lähellä" })
        {
            var o = GameObject.Find(n);
            if (o != null) Undo.DestroyObjectImmediate(o);
        }
        ti.GetSourceTextureWidthAndHeight(out int w, out int h);
        float ppu = h / (2f * CamHalf);
        ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = ppu; ti.filterMode = FilterMode.Bilinear; ti.alphaIsTransparency = true;
        ti.textureCompression = TextureImporterCompression.Uncompressed; ti.maxTextureSize = 8192; ti.mipmapEnabled = false;
        var st = new TextureImporterSettings(); ti.ReadTextureSettings(st);
        st.spriteMeshType = SpriteMeshType.FullRect; st.spriteAlignment = (int)SpriteAlignment.Center;
        ti.SetTextureSettings(st); ti.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        float wU = w / ppu, hU = h / ppu, halfW = CamHalf * 16f / 9f;
        var bg = new GameObject(BridgeAreaName);
        var sr = bg.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = -10000;
        sr.color = new Color(0.85f, 0.86f, 0.96f);   // yö: hieman hämärämpi kuten sali
        bg.transform.position = new Vector3(ShipBridgeX0 + wU * 0.5f, CamY, 0f);
        Undo.RegisterCreatedObjectUndo(bg, BridgeAreaName);
        float top = CamY + hU * 0.5f;
        float BX(float px) => ShipBridgeX0 + px / ppu;
        float BY(float row) => top - row / ppu;

        var area = ReuseArea("Alue: " + BridgeAreaName);
        area.areaName = BridgeAreaName;
        area.music = sal.music;
        area.useSidewalk = false;
        area.maxDepthY = BY(BridgeWallPx);
        area.minDepthY = CamY - CamHalf + 1.2f;
        area.camMinX = ShipBridgeX0 + halfW;
        area.camMaxX = Mathf.Max(area.camMinX, ShipBridgeX0 + wU - halfW);
        area.walkMinX = BX(40f); area.walkMaxX = BX(w - 60f);
        area.depthLimits = BridgeBackLine.Select(v => new Vector2(BX(v.x), BY(v.y))).ToArray();
        EditorUtility.SetDirty(area);

        // ikkunoista näkyy yöllinen meri (samat kuvat kuin salissa)
        string far = File.Exists("Assets/Sprites/Taustat/laiva_meri_kauko_yo.png") ? "Assets/Sprites/Taustat/laiva_meri_kauko_yo.png" : ShipSeaFarPath;
        string near = File.Exists("Assets/Sprites/Taustat/laiva_meri_lahi_yo.png") ? "Assets/Sprites/Taustat/laiva_meri_lahi_yo.png" : ShipSeaNearPath;
        // samat merikuvat kuin salissa (tuotu salin mittakaavaan; komentosillan kuva on lähes samaa korkeutta)
        void Sea(string name, string spath, float topRow, int order, float speed, float parallax)
        {
            var spr = AssetDatabase.LoadAssetAtPath<Sprite>(spath);
            if (spr == null) return;
            var go = new GameObject(name);
            var s2 = go.AddComponent<SpriteRenderer>(); s2.sprite = spr; s2.sortingOrder = order; s2.drawMode = SpriteDrawMode.Tiled;
            float sw = spr.rect.width / spr.pixelsPerUnit, sh = spr.rect.height / spr.pixelsPerUnit;
            s2.size = new Vector2(Mathf.Ceil((2f * halfW) / sw + 2f) * sw, sh);
            go.transform.position = new Vector3(ShipBridgeX0 + 10f, BY(topRow) - sh * 0.5f, 0f);
            var sl = go.AddComponent<ScrollingLayer>(); sl.autoSpeed = speed; sl.parallax = parallax; sl.area = area;
            sl.bobAmplitude = 0.15f; sl.bobPeriod = 7f;
            Undo.RegisterCreatedObjectUndo(go, name);
        }
        Sea("Komentosillan meri kaukana", far, BridgeHorizonPx - 291f * 0.6f, -10100, 0.35f, 0.04f);
        Sea("Komentosillan meri lähellä", near, BridgeHorizonPx + 30f, -10090, 3f, 0.4f);

        bool doors = SalonBridgeDoors();
        EditorSceneManager.MarkSceneDirty(bg.scene);
        Info($"Komentosilta luotu ({wU:0.0} yksikköä). Ikkunoista näkyy meri.\n" +
             (doors ? "Salin portaista ylös ja ovesta takaisin saliin (ei tappelun aikana)." : "Tee ensin salin portaat (kohta 83), sitten tämä uudelleen.") +
             "\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Merirosvokapteeni (komentosillan pomo) ----------------
    // kapteeni_iskut.png (768 × 512, 34 kuvaa videosta): 0–3 jab, 4–8 suora, 9–13 sivupotku,
    // 14–33 kombon lopetus (pyörähdys, hyppy ja korkea potku alas; isku kuvassa 26 = sarjan 12.)
    [MenuItem("Beat em up/85. Merirosvokapteeni ja rosvot komentosillalle")]
    static void CreateShipCaptain()
    {
        var bridge = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == BridgeAreaName);
        var bridgeBg = GameObject.Find(BridgeAreaName);
        if (bridge == null || bridgeBg == null) { Info("Tee ensin komentosilta (kohta 84)."); return; }
        foreach (var n in new[] { "kapteeni_idle", "kapteeni_iskut", "kapteeni_kavely", "kapteeni_kaatuminen", "kapteeni_ylosnousu", "kapteeni_liuku", "kapteeni_heitetty" })
        {
            string path = FindTexture(n);
            if (path != null) SetupAndSlice(path);
        }
        var report = new List<string>();
        var idle = EnemySheet("kapteeni_idle", report);
        var atk = EnemySheet("kapteeni_iskut", report);
        if (idle.Length < 6 || atk.Length < 34) { Info("kapteeni_idle.png (6 kuvaa) ja kapteeni_iskut.png (34 kuvaa) tarvitaan.\n\n" + string.Join("\n", report)); return; }
        foreach (var n in new[] { "Kapteeni", "Komentosillan rosvot" })
        {
            var o = GameObject.Find(n);
            if (o != null) Undo.DestroyObjectImmediate(o);
        }
        var bsr = bridgeBg.GetComponent<SpriteRenderer>();
        float ppu = bsr.sprite.pixelsPerUnit, left = bsr.bounds.min.x;
        float BX(float px) => left + px / ppu;
        Sprite[] R(int a, int n) => atk.Skip(a).Take(n).ToArray();

        var go = new GameObject("Kapteeni");
        var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
        var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
        var t = go.AddComponent<Enemy>();
        t.body = visual; t.shadow = shadow; visual.sprite = idle[0];
        t.displayName = "Kapteeni";
        t.visualScale = 1f; t.attackArtScale = 1f; t.knockArtScale = 1f; t.walkArtScale = 1f;
        t.idleSprites = idle; t.idleFrameTime = 0.13f;
        var walk = EnemySheet("kapteeni_kavely", report);
        t.walkSprites = walk.Length > 0 ? walk : idle; t.walkFrameTime = 0.07f;   // 16 kuvaa = kaksi askelta
        // osuma (kaatumissarjan 1. kuva), kaatuminen 8 kuvaa (0–6 ilmassa, 7 makaa), ylösnousu 6 kuvaa
        var fall = EnemySheet("kapteeni_kaatuminen", report);
        var rise = EnemySheet("kapteeni_ylosnousu", report);
        if (fall.Length >= 8) { t.hurtSprites = new[] { fall[0] }; t.knockdownSprites = fall.Take(8).ToArray(); }
        if (rise.Length >= 6) { t.getUpSprites = rise.Take(6).ToArray(); t.getUpTime = 1.0f; }
        // juoksu (15 kuvaa) ja liukutaklaus: pudotus + liuku (6) ja ylösnousu (4); ei voi torjua, hypyllä yli
        var slide = EnemySheet("kapteeni_liuku", report);
        if (slide.Length >= 25)
        {
            t.runSprites = slide.Take(15).ToArray(); t.runFrameTime = 0.05f / 1.5f;
            t.slideSprites = slide.Skip(15).Take(6).ToArray(); t.slideUpSprites = slide.Skip(21).Take(4).ToArray();
            t.slideChance = 0.7f; t.slideDamage = 18; t.slideRunSpeed = 10.5f; t.slideCooldown = 3.5f;
        }
        // J: jab, S: suora, P: sivupotku, K: kombon lopetus (vain kombossa)
        t.punchSprites = R(0, 4); t.punchImpactFrame = 2; t.punchDamage = 9;
        t.punch2Sprites = R(4, 5); t.punch2ImpactFrame = 1; t.punch2Damage = 13; t.punch2Chance = 0.35f; t.punch2Knockdown = false;
        t.altAttackSprites = R(9, 5); t.altImpactFrame = 2; t.altDamage = 15; t.altReach = 2.9f; t.altKnockdown = true;
        t.altKnockSpeed = 6f; t.altKnockUp = 4f; t.chargeRange = 0f; t.altChance = 0f; t.altNearChance = 0.3f; t.altExtraWindup = 0.05f;
        t.punch3Sprites = R(14, 20); t.punch3ImpactFrame = 12; t.punch3Damage = 22; t.punch3Knockdown = true; t.punch3Chance = 0f;
        t.punch3WindupTime = 1.1f; t.punch3RecoverTime = 0.6f; t.punch3Reach = 3.0f; t.punch3LaunchUp = 7f; t.punch3LaunchX = 6f;
        t.combos = new[] { "JSPK", "JSPK", "JSK", "JJS" }; t.comboChance = 0.5f; t.comboArmor = true;
        t.windupTime = 0.2f; t.punchRecoverTime = 0.35f; t.attackRange = 2.2f;
        t.moveSpeedX = 2.6f; t.moveSpeedY = 1.6f;
        t.maxHealth = 420; t.attackCooldown = 0.6f;
        t.blockChance = 0.3f; t.maxBlocksInRow = 2;
        t.hurtSounds = LoadClips("Assets/Audio/big thug", "gasp"); t.hurtVolume = 1f;
        t.bigBody = true; t.wakeDistance = 9f;
        SetBossThrown(t, EnemySheet("kapteeni_heitetty", report));
        t.runSpeedMultiplier = 1.95f; t.flankChance = 0.15f; t.retreatChance = 0f;
        // ruorin luona selkä oveen päin (katsoo vasemmalle: hero tulee ovesta)
        go.transform.position = new Vector3(BX(1150f), Mathf.Lerp(bridge.maxDepthY, bridge.minDepthY, 0.3f), 0f);
        t.idleFacing = -1;
        Undo.RegisterCreatedObjectUndo(go, "Kapteeni");

        // pari rosvoa kannelta kopioina (käyvät heron kimppuun)
        var pirates = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(e => e != null && e.displayName == "Rosvo").Take(1).ToList();
        int np = 0;
        if (pirates.Count > 0)
        {
            var root = new GameObject("Komentosillan rosvot");
            Undo.RegisterCreatedObjectUndo(root, "Rosvot");
            for (int i = 0; i < 2; i++)
            {
                var c = Object.Instantiate(pirates[0].gameObject, root.transform);
                c.name = "Rosvo (komentosilta " + (i + 1) + ")";
                c.transform.position = new Vector3(BX(i == 0 ? 700f : 1600f), Mathf.Lerp(bridge.maxDepthY, bridge.minDepthY, i == 0 ? 0.6f : 0.45f), 0f);
                var e = c.GetComponent<Enemy>();
                e.fightsEveryone = false; e.huntsBrawlers = false; e.ally = false; e.wanderMinX = e.wanderMaxX = 0f; e.wakeDistance = 8f;
                c.SetActive(true);
                np++;
            }
        }
        EditorSceneManager.MarkSceneDirty(go.scene);
        Info($"Merirosvokapteeni komentosillalle (pomopalkki) ja {np} rosvoa.\n" +
             "Iskut: jab, suora, sivupotku; kombo jab–suora–sivupotku–hyppypotku (lopetus kaataa); liukutaklaus juoksusta (hyppää yli).\n" +
             string.Join("\n", report) + "\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/85b. Kapteenin juoksu ja hyökkäykset nopeammiksi")]
    static void TuneShipCaptain()
    {
        if (PlayModeBlocked()) return;
        var captain = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(e => e.displayName == "Kapteeni");
        if (captain == null) { Info("Tee ensin kapteeni (kohta 85)."); return; }
        Undo.RecordObject(captain, "Kapteenin tahti");
        captain.runSpeedMultiplier = 1.95f; captain.runFrameTime = 0.05f / 1.5f;
        captain.attackCooldown = 0.6f; captain.slideRunSpeed = 10.5f;
        captain.slideChance = 0.7f; captain.slideCooldown = 3.5f;
        EditorUtility.SetDirty(captain);
        EditorSceneManager.MarkSceneDirty(captain.gameObject.scene);
        Info("Kapteenin juoksu 50 % nopeammaksi ja hyökkäykset useammiksi.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/91. Böönin juontikuvat molemmille hahmoille")]
    static void SetupBooniArt()
    {
        if (PlayModeBlocked()) return;
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { Info("Scenessä ei ole pelaajaa."); return; }
        if (!AssignBooniArt(pc)) { Info("Resources/Termari-kansiosta tarvitaan rocco_juo_00…15 ja ruby_juo_00…15."); return; }
        Info("Molempien hahmojen Böönin juontikuvat asetettu. Juonnin jälkeen sama hehku ja nopeutus.\n\nTallenna scene (Ctrl+S).");
    }

    static bool AssignBooniArt(PlayerController pc)
    {
        foreach (var prefix in new[] { "rocco", "ruby" })
            for (int i = 0; i < 16; i++)
                if (!File.Exists($"Assets/Resources/Termari/{prefix}_juo_{i:00}.png")) return false;
        Sprite[] Frames(string prefix)
        {
            var frames = new Sprite[16];
            for (int i = 0; i < frames.Length; i++)
                frames[i] = ImportShipSprite($"Assets/Resources/Termari/{prefix}_juo_{i:00}.png", 100f, false, SpriteAlignment.BottomCenter);
            return frames;
        }
        Undo.RecordObject(pc, "Böönin juontikuvat");
        pc.booniDrinkSprites = Frames("rocco");
        if (pc.heroine == null) pc.heroine = new PlayerController.CharacterArt();
        pc.heroine.drink = Frames("ruby");
        EditorUtility.SetDirty(pc);
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        return true;
    }

    // ---------------- Laivan katto (kapteenin jälkeen) ----------------
    // laiva_katto.png (1672 × 941, yksi ruutu, ei vieritä): chinook-kopteri laivan katolla. Taivas on läpinäkyvä:
    // takana rullaa kuunvalotaivas (laiva_katto_taivas.png = kauko-yökuvan yläosa), vain horisontti näkyy kaiteen raoista.
    // Kun kapteeni kaatuu, komentosillan kattoon putoavat tikkaat (laiva_tikkaat.png), joista pääsee tänne.
    const string RoofAreaName = "Laivan katto";
    const float ShipRoofX0 = 74000f, RoofBackPx = 540f;
    const float RoofImageScale = 1.1f;                         // katon kuva vielä 10 % suuremmaksi
    const float RoofHorizonPx = 450f, RoofSkyScale = 1.6f;       // horisontti (taivaskuvan rivi 291) kaiteen raon korkeudelle
    const float RoofMoonPx = 900f, RoofSkyMoonSrcPx = 887f;      // kuu alkaa rotorien välistä (kuvan x), kuun x taivaskuvassa
    const float RoofSkySpeed = 0.35f;                            // sama kuin laivan muiden ruutujen kaukaisella merellä
    static readonly Color RoofTint = new Color(0.72f, 0.78f, 0.95f);   // kullanhohtoinen kansi kuunvaloon
    // chinook: runko yhtenä kuvana (maassa, ei heilu) ja kaksi roottoria omina silmukkinaan (Kopteri/-kansio, videosta)
    const string HeliDir = "Assets/Sprites/Rekvisiitta/Kopteri/";
    const float HeliScale = 1.05f * 1.15f;                               // kopteri 15 % suuremmaksi
    const float HeliLeftPx = 332f, HeliWheelPx = 485f;           // rungon nokka (x) ja pyörien alareuna (rivi) katon kuvassa
    static readonly Vector2 HeliFrontRotorSrc = new Vector2(-286f, 216f), HeliRearRotorSrc = new Vector2(482f, 238f);   // roottorin vasen alakulma rungon vasemmasta alakulmasta (videon px)
    const float HeliDoorSrcPx = 647f;                            // sivuovi rungon vasemmasta reunasta (videon px)
    const float HeliRotorFps = 12f;                              // roottorien nopeus (pienempi = hitaampi)
    const float BridgeLadderXPx = 1160f, BridgeLadderFootPx = 520f;    // tikkaiden jalka komentosillan kuvassa (ruorin ja karttapöydän välissä)

    static Sprite ImportShipSprite(string path, float ppu, bool tiled, SpriteAlignment align)
    {
        var t = AssetImporter.GetAtPath(path) as TextureImporter;
        if (t == null) return null;
        t.textureType = TextureImporterType.Sprite; t.spriteImportMode = SpriteImportMode.Single;
        t.spritePixelsPerUnit = ppu; t.filterMode = FilterMode.Bilinear; t.alphaIsTransparency = true;
        t.textureCompression = TextureImporterCompression.Uncompressed; t.maxTextureSize = 8192; t.mipmapEnabled = false;
        t.wrapMode = tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        var st = new TextureImporterSettings(); t.ReadTextureSettings(st);
        st.spriteMeshType = SpriteMeshType.FullRect; st.spriteAlignment = (int)align;
        t.SetTextureSettings(st); t.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    [MenuItem("Beat em up/87. Laivan katto: chinook, kuunvalotaivas ja tikkaat komentosillalta (kapteenin jälkeen)")]
    static void CreateShipRoof()
    {
        if (PlayModeBlocked()) return;
        const string roofPath = "Assets/Sprites/Taustat/laiva_katto.png";
        const string skyPath = "Assets/Sprites/Taustat/laiva_katto_taivas.png";
        const string ladderPath = "Assets/Sprites/Rekvisiitta/laiva_tikkaat.png";
        var bridge = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == BridgeAreaName);
        var bridgeBg = GameObject.Find(BridgeAreaName);
        var captainGo = GameObject.Find("Kapteeni");
        var roofTi = AssetImporter.GetAtPath(roofPath) as TextureImporter;
        var ladTi = AssetImporter.GetAtPath(ladderPath) as TextureImporter;
        if (bridge == null || bridgeBg == null) { Info("Tee ensin komentosilta (kohta 84)."); return; }
        if (captainGo == null || captainGo.GetComponent<Enemy>() == null) { Info("Tee ensin kapteeni (kohta 85): tikkaat putoavat, kun hän kaatuu."); return; }
        if (roofTi == null || ladTi == null || AssetImporter.GetAtPath(skyPath) == null) { Info("Tarvitaan kuvat:\n" + roofPath + "\n" + skyPath + "\n" + ladderPath); return; }
        foreach (var n in new[] { "Laivan katto (kuva)", "Laivan katon taivas", "Laivan katon tikkaat", "Laivan katon paluuovi", "Kopteri", "Laivan katon viholliset", "Katon taivas", "Katon tikkaat" })   // alue säilyy (ReuseArea); kaksi viimeistä ovat vanhoja nimiä
        {
            var o = GameObject.Find(n);
            if (o != null) Undo.DestroyObjectImmediate(o);
        }
        DestroyEnemyGroups("Katon viholliset", true);   // vanha nimi: vain laivan katon ryhmä (x >= 50000), kadun katon ryhmää ei kosketa

        // kuva: yksi ruutu, ei vieritä
        roofTi.GetSourceTextureWidthAndHeight(out int w, out int h);
        float ppu = h / (2f * CamHalf * RoofImageScale);
        var roofSpr = ImportShipSprite(roofPath, ppu, false, SpriteAlignment.Center);
        float wU = w / ppu, hU = h / ppu, halfW = CamHalf * 16f / 9f;
        var bg = new GameObject("Laivan katto (kuva)");
        var sr = bg.AddComponent<SpriteRenderer>(); sr.sprite = roofSpr; sr.sortingOrder = -10000; sr.color = RoofTint;
        bg.transform.position = new Vector3(ShipRoofX0 + wU * 0.5f, CamY, 0f);
        Undo.RegisterCreatedObjectUndo(bg, "Laivan katto");
        float top = CamY + hU * 0.5f;
        float RX(float px) => ShipRoofX0 + px / ppu;
        float RY(float row) => top - row / ppu;

        var area = ReuseArea("Alue: " + RoofAreaName);
        area.areaName = RoofAreaName;
        area.music = bridge.music;
        area.useSidewalk = false;
        area.maxDepthY = RY(RoofBackPx);
        area.minDepthY = CamY - CamHalf + 1.2f;
        area.camMinX = ShipRoofX0 + halfW;
        area.camMaxX = Mathf.Max(area.camMinX, ShipRoofX0 + wU - halfW);
        area.walkMinX = RX(60f); area.walkMaxX = RX(w - 60f);
        area.ambience = Resources.Load<AudioClip>("Ambienssi/laivan_katto");
        area.ambienceVolume = 0.6f;   // chinookin ääni (Resources/Ambienssi/laivan_katto.wav) soi katolla taustalla
        EditorUtility.SetDirty(area);

        // kuunvalotaivas takana: hidas rullaus ja keinunta kuten laivan muilla kaukaisilla meren kerroksilla
        float sp = ppu / RoofSkyScale;
        var skySpr = ImportShipSprite(skyPath, sp, true, SpriteAlignment.Center);
        float sw = skySpr.rect.width / sp, sh = skySpr.rect.height / sp;
        int tiles = Mathf.CeilToInt(2f * halfW / sw) + 2; if (tiles % 2 == 0) tiles++;   // pariton: yksi kuva on keskellä
        var sky = new GameObject("Laivan katon taivas");
        var ssr = sky.AddComponent<SpriteRenderer>(); ssr.sprite = skySpr; ssr.sortingOrder = -10100; ssr.drawMode = SpriteDrawMode.Tiled;
        ssr.size = new Vector2(tiles * sw, sh);
        sky.transform.position = new Vector3(ShipRoofX0 + wU * 0.5f, RY(RoofHorizonPx - 291f * RoofSkyScale) - sh * 0.5f, 0f);
        var sl = sky.AddComponent<ScrollingLayer>();
        sl.autoSpeed = RoofSkySpeed; sl.parallax = 0f; sl.area = area; sl.bobAmplitude = 0.15f; sl.bobPeriod = 7f;
        sl.startPhase = (RoofMoonPx - w * 0.5f) / ppu - (RoofSkyMoonSrcPx - skySpr.rect.width * 0.5f) / sp;   // kuu alkaa kohdasta RoofMoonPx
        Undo.RegisterCreatedObjectUndo(sky, "Laivan katon taivas");

        // tikkaat komentosillalle: piilossa, kunnes kapteeni kaatuu
        var bsr = bridgeBg.GetComponent<SpriteRenderer>();
        float bppu = bsr.sprite.pixelsPerUnit;
        float lx = bsr.bounds.min.x + BridgeLadderXPx / bppu, ly = bsr.bounds.max.y - BridgeLadderFootPx / bppu;
        ladTi.GetSourceTextureWidthAndHeight(out _, out int lh);
        float lp = lh / ((BridgeLadderFootPx + 60f) / bppu);   // katosta (rivi -60) lattiaan
        var ladSpr = ImportShipSprite(ladderPath, lp, false, SpriteAlignment.BottomCenter);
        var root = new GameObject("Laivan katon tikkaat");
        root.transform.position = new Vector3(lx, ly, 0f);
        Undo.RegisterCreatedObjectUndo(root, "Laivan katon tikkaat");
        var vis = new GameObject("Tikkaat").AddComponent<SpriteRenderer>();
        vis.transform.SetParent(root.transform, false);
        vis.sprite = ladSpr; vis.sortingOrder = -9000; vis.color = bsr.color;   // hahmojen takana, myös kiivetessä
        var door = new GameObject("Ovi katolle").AddComponent<Door>();
        door.transform.SetParent(root.transform, false);
        door.prompt = "Kiipeä katolle"; door.here = bridge; door.target = area;
        door.spawnPoint = new Vector2(RX(330f), Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.45f));
        door.halfWidth = 1.3f; door.maxDistanceFromWall = 1.6f; door.blockedDuringFight = false;
        door.climbHeight = (BridgeLadderFootPx - 120f) / bppu; door.climbTime = 1.4f;
        var bl = root.AddComponent<BossLadder>();
        bl.boss = captainGo.GetComponent<Enemy>(); bl.ladder = vis; bl.door = door;
        bl.dropHeight = (BridgeLadderFootPx + 40f) / bppu;

        // kopteri maassa: runko paikallaan, roottorit pyörivät vastakkain (takaroottori soitetaan takaperin)
        string heliInfo;
        float hp = ppu / HeliScale;   // videon pikseleitä yksikössä
        var hullSpr = ImportShipSprite(HeliDir + "kopteri_runko.png", hp, false, SpriteAlignment.BottomLeft);
        Sprite[] ImportRotor(string prefix)
        {
            var list = new List<Sprite>();
            for (int i = 0; ; i++)
            {
                string fp = $"{HeliDir}{prefix}_{i:00}.png";
                if (!File.Exists(fp)) break;
                var fs = ImportShipSprite(fp, hp, false, SpriteAlignment.BottomLeft);
                if (fs != null) list.Add(fs);
            }
            return list.ToArray();
        }
        var front = ImportRotor("kopteri_etu"); var rear = ImportRotor("kopteri_taka");
        float heliY = RY(HeliWheelPx);
        if (hullSpr != null && front.Length > 0 && rear.Length > 0)
        {
            var heli = new GameObject("Kopteri");
            heli.transform.position = new Vector3(RX(HeliLeftPx), heliY, 0f);
            Undo.RegisterCreatedObjectUndo(heli, "Kopteri");
            int heliOrder = Mathf.RoundToInt(-heliY * 100f);   // hahmot pyörien edessä (takaraja rivillä RoofBackPx) piirtyvät päälle
            var hullR = new GameObject("Runko").AddComponent<SpriteRenderer>();
            hullR.transform.SetParent(heli.transform, false);
            hullR.sprite = hullSpr; hullR.sortingOrder = heliOrder; hullR.color = RoofTint;
            void Rotor(string name, Sprite[] frames, Vector2 src, bool reverse)
            {
                var r = new GameObject(name).AddComponent<SpriteRenderer>();
                r.transform.SetParent(heli.transform, false);
                r.transform.localPosition = new Vector3(src.x / hp, src.y / hp, 0f);
                r.sprite = frames[0]; r.sortingOrder = heliOrder + 1; r.color = RoofTint;
                var loop = r.gameObject.AddComponent<RotorLoop>();
                loop.frames = frames; loop.fps = HeliRotorFps; loop.reverse = reverse;
            }
            Rotor("Etuoroottori", front, HeliFrontRotorSrc, false);
            Rotor("Takaroottori", rear, HeliRearRotorSrc, true);
            heliInfo = $"Kopteri maassa, roottorit pyörivät vastakkain ({front.Length} + {rear.Length} kuvaa).";
        }
        else heliInfo = "Kopterin kuvia ei löytynyt kansiosta " + HeliDir;

        // viholliset: 3 rosvoa ja 2 koviksta, valppaina heti (kopteriin ei pääse ennen kuin ne on voitettu)
        var all = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var pirateTmpl = all.FirstOrDefault(e => e != null && e.displayName == "Rosvo");
        var kovisTmpl = all.FirstOrDefault(e => e != null && e.gameObject.name == "Kovis");
        var foes = new GameObject("Laivan katon viholliset");
        Undo.RegisterCreatedObjectUndo(foes, "Laivan katon viholliset");
        int foeCount = 0;
        void Foe(Enemy tmpl, string label, float px, float depth01)
        {
            if (tmpl == null) return;
            var c = Object.Instantiate(tmpl.gameObject, foes.transform);
            c.name = label;
            c.transform.position = new Vector3(RX(px), Mathf.Lerp(area.maxDepthY, area.minDepthY, depth01), 0f);
            var e = c.GetComponent<Enemy>();
            e.fightsEveryone = false; e.huntsBrawlers = false; e.ally = false; e.wanderMinX = e.wanderMaxX = 0f; e.wakeDistance = 40f;
            c.SetActive(true);
            foeCount++;
        }
        Foe(kovisTmpl, "Katto Kovis 1", 620f, 0.35f);
        Foe(kovisTmpl, "Katto Kovis 2", 1560f, 0.5f);
        Foe(pirateTmpl, "Katto Rosvo 1", 900f, 0.5f);
        Foe(pirateTmpl, "Katto Rosvo 2", 1150f, 0.25f);
        Foe(pirateTmpl, "Katto Rosvo 3", 1400f, 0.6f);
        string foeInfo = foeCount == 5 ? "Katolla 3 rosvoa ja 2 koviksta."
            : $"Vihollisia luotu {foeCount}/5: malli puuttuu (rosvo kohdasta 56, kovis kohdasta 7).";

        // kopterin ovi avautuu kattotaistelun jälkeen
        var heliDoor = new GameObject("Ovi kopteriin").AddComponent<Door>();
        heliDoor.transform.SetParent(foes.transform, false);
        float doorX = RX(HeliLeftPx + HeliDoorSrcPx * HeliScale);
        heliDoor.transform.position = new Vector3(doorX, heliY, 0f);
        heliDoor.prompt = "Nouse kopteriin"; heliDoor.here = area;
        heliDoor.target = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == HeliAreaName);
        heliDoor.enabled = heliDoor.target != null;
        heliDoor.spawnPoint = HeliSpawnPoint();
        heliDoor.halfWidth = 1.6f; heliDoor.maxDistanceFromWall = 2.0f; heliDoor.blockedDuringFight = true; heliDoor.fightBlockRange = wU + 2f;

        var back = new GameObject("Laivan katon paluuovi").AddComponent<Door>();
        Undo.RegisterCreatedObjectUndo(back.gameObject, "Laivan katon paluuovi");
        back.transform.position = new Vector3(RX(110f), area.maxDepthY, 0f);
        back.prompt = "Palaa komentosillalle"; back.here = area; back.target = bridge;
        back.spawnPoint = new Vector2(lx, Mathf.Clamp(ly, bridge.minDepthY, bridge.maxDepthY));
        back.halfWidth = 1.5f; back.maxDistanceFromWall = 3f; back.blockedDuringFight = true; back.fightBlockRange = wU + 2f;
        back.climbHeight = -2f;
        var exit = GameObject.Find("Kopterin paluuovi");
        var exitDoor = exit != null ? exit.GetComponent<Door>() : null;
        if (exitDoor != null)
        {
            Undo.RecordObject(exitDoor, "Kopterin paluuovi");
            exitDoor.spawnPoint = new Vector2(doorX, area.maxDepthY - 0.5f);
            EditorUtility.SetDirty(exitDoor);
        }

        EditorSceneManager.MarkSceneDirty(bg.scene);
        Info($"Laivan katto luotu ({wU:0.0} yksikköä, yksi ruutu) kuunvalotaivaalla.\n" +
             "Kun kapteeni kaatuu, komentosillan kattoon putoavat tikkaat: E kiipeää katolle.\n" +
             heliInfo + "\n" + foeInfo + "\nKopterin ovi (E) aukeaa, kun viholliset on voitettu.\nTee kopterin sisätila kohdasta 89.\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Kopterin rahtitila ----------------
    const string HeliAreaName = "Kopterin rahtitila";
    const float HeliInteriorX0 = 76000f;
    static Vector2 HeliSpawnPoint() => new Vector2(HeliInteriorX0 + 3f, CamY - 2f);

    [MenuItem("Beat em up/89. Kopterin sisätila, ohjaamo ja kaksi aaltoa")]
    static void CreateHeliInterior()
    {
        if (PlayModeBlocked()) return;
        const string path = "Assets/Sprites/Taustat/kopteri_sisatilat.png";
        const string skyPath = "Assets/Sprites/Taustat/kopteri_sisataivas.png";
        const string extensionPath = "Assets/Sprites/Taustat/kopteri_sisatilat_jatko.png";
        const string cockpitPath = "Assets/Sprites/Taustat/kopteri_ohjaamo.png";
        const float overlapPx = 64f, cockpitOverlapPx = 432f, cockpitScaleX = 1f;
        var roof = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == RoofAreaName);
        var entry = GameObject.Find("Ovi kopteriin")?.GetComponent<Door>();
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        var shader = Shader.Find("Beat em up/Kopterin ikkunat");
        if (roof == null || entry == null) { Info("Tee ensin laivan katto (kohta 87)."); return; }
        if (ti == null || AssetImporter.GetAtPath(skyPath) == null || AssetImporter.GetAtPath(extensionPath) == null || AssetImporter.GetAtPath(cockpitPath) == null || shader == null) { Info("Kopterin sisäkuva, taivas tai ikkunashader puuttuu."); return; }
        var models = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var pirate = models.FirstOrDefault(e => e.displayName == "Rosvo" && !e.name.StartsWith("Kopterin "));
        var tough = models.FirstOrDefault(e => e.name == "Kovis");
        var crateSprites = LoadSprites("laatikko");
        if (pirate == null || tough == null || crateSprites.Length == 0)
        { Info("Tarvitaan Rosvo (kohta 56), Kovis (kohta 7) ja puulaatikon kuvat (kohta 23)."); return; }
        foreach (var name in new[] { "Kopterin rahtitila (kuva)", "Kopterin rahtitila (jatko)", "Kopterin ohjaamo (kuva)", "Kopterin kuunvalotaivas", "Kopterin paluuovi", "Kopterin puulaatikot", "Kopterin taistelu", "Kopterista Aavikkoon" })
        {
            var old = GameObject.Find(name);
            if (old != null) Undo.DestroyObjectImmediate(old);
        }
        ti.GetSourceTextureWidthAndHeight(out int w, out int h);
        float ppu = h / (2f * CamHalf), panelWidth = w / ppu, halfW = CamHalf * 16f / 9f;
        var extensionTi = (TextureImporter)AssetImporter.GetAtPath(extensionPath);
        extensionTi.GetSourceTextureWidthAndHeight(out int ew, out int eh);
        var cockpitTi = (TextureImporter)AssetImporter.GetAtPath(cockpitPath);
        cockpitTi.GetSourceTextureWidthAndHeight(out int cw, out int ch);
        float cockpitStartPx = w - overlapPx + ew - cockpitOverlapPx;
        float width = (cockpitStartPx + cw * cockpitScaleX) / ppu;
        float HX(float px) => HeliInteriorX0 + px / ppu;
        var sprite = ImportShipSprite(path, ppu, false, SpriteAlignment.Center);
        var area = ReuseArea("Alue: " + HeliAreaName);
        area.areaName = HeliAreaName; area.music = roof.music; area.useSidewalk = false;
        area.minDepthY = CamY - CamHalf + 0.6f;
        area.maxDepthY = CamY + CamHalf - 590f / ppu;
        area.camMinX = HeliInteriorX0 + halfW;
        area.camMaxX = Mathf.Max(area.camMinX, HeliInteriorX0 + width - halfW);
        area.walkMinX = HeliInteriorX0 + 0.8f; area.walkMaxX = HeliInteriorX0 + width - 0.8f;
        area.ambience = roof.ambience != null ? roof.ambience : Resources.Load<AudioClip>("Ambienssi/laivan_katto");
        area.ambienceVolume = 0.35f;
        EditorUtility.SetDirty(area);
        var bg = new GameObject("Kopterin rahtitila (kuva)");
        Undo.RegisterCreatedObjectUndo(bg, "Kopterin rahtitila");
        bg.transform.position = new Vector3(HeliInteriorX0 + panelWidth * 0.5f, CamY, 0f);
        var sr = bg.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = -10000;
        Material WindowMaterial(string materialPath, int panel, int imageWidth, int imageHeight)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, materialPath); }
            else Undo.RecordObject(mat, "Kopterin ikkunat");
            mat.shader = shader;
            mat.SetFloat("_Continuation", panel);
            mat.SetVector("_ImageSize", new Vector4(imageWidth, imageHeight, 0f, 0f));
            mat.SetFloat("_FloorRow", panel == 2 ? 730f : panel == 1 ? 634f : 590f);
            mat.SetVector("_SeamTint", panel == 2 ? new Vector4(0.9f, 0.9f, 0.9f, 1f) : Vector4.one);
            mat.SetFloat("_SeamWallWidth", panel == 2 ? 40f : overlapPx);
            mat.SetFloat("_SeamStart", panel == 2 ? 85f : 0f);
            mat.SetFloat("_SeamWidth", panel == 2 ? 95f : panel == 1 ? overlapPx : 0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }
        sr.sharedMaterial = WindowMaterial("Assets/Sprites/Taustat/kopteri_ikkunat.mat", 0, w, h);
        var extensionSprite = ImportShipSprite(extensionPath, ppu, false, SpriteAlignment.Center);
        var extension = new GameObject("Kopterin rahtitila (jatko)");
        Undo.RegisterCreatedObjectUndo(extension, "Kopterin rahtitilan jatko");
        extension.transform.position = new Vector3(HeliInteriorX0 + (w - overlapPx + ew * 0.5f) / ppu, CamY, 0f);
        extension.transform.localScale = new Vector3(1f, (float)h / eh, 1f);
        var extensionR = extension.AddComponent<SpriteRenderer>();
        extensionR.sprite = extensionSprite; extensionR.sortingOrder = sr.sortingOrder + 1;
        extensionR.sharedMaterial = WindowMaterial("Assets/Sprites/Taustat/kopteri_jatko_ikkunat.mat", 1, ew, eh);
        var cockpitSprite = ImportShipSprite(cockpitPath, ppu, false, SpriteAlignment.Center);
        var cockpit = new GameObject("Kopterin ohjaamo (kuva)");
        Undo.RegisterCreatedObjectUndo(cockpit, "Kopterin ohjaamo");
        cockpit.transform.position = new Vector3(HX(cockpitStartPx + cw * cockpitScaleX * 0.5f), CamY, 0f);
        cockpit.transform.localScale = new Vector3(cockpitScaleX, (float)h / ch, 1f);
        var cockpitR = cockpit.AddComponent<SpriteRenderer>();
        cockpitR.sprite = cockpitSprite; cockpitR.sortingOrder = sr.sortingOrder + 2;
        cockpitR.sharedMaterial = WindowMaterial("Assets/Sprites/Taustat/kopteri_ohjaamo_ikkunat.mat", 2, cw, ch);
        var skySprite = ImportShipSprite(skyPath, ppu / RoofSkyScale, true, SpriteAlignment.Center);
        var sky = new GameObject("Kopterin kuunvalotaivas");
        Undo.RegisterCreatedObjectUndo(sky, "Kopterin kuunvalotaivas");
        sky.transform.position = new Vector3(bg.transform.position.x, CamY + CamHalf - 305f / ppu, 0f);
        var skyR = sky.AddComponent<SpriteRenderer>(); skyR.sprite = skySprite; skyR.sortingOrder = -10100;
        skyR.drawMode = SpriteDrawMode.Tiled;
        skyR.size = new Vector2(skySprite.bounds.size.x * 3f, skySprite.bounds.size.y);
        var scroll = sky.AddComponent<ScrollingLayer>(); scroll.area = area; scroll.parallax = 0.08f;
        scroll.autoSpeed = RoofSkySpeed; scroll.bobAmplitude = 0.08f;
        Undo.RecordObject(entry, "Ovi kopteriin");
        entry.target = area; entry.spawnPoint = HeliSpawnPoint(); entry.enabled = true;
        entry.fightBlockRange = roof.walkMaxX - roof.walkMinX + 2f;
        EditorUtility.SetDirty(entry);
        var back = new GameObject("Kopterin paluuovi").AddComponent<Door>();
        Undo.RegisterCreatedObjectUndo(back.gameObject, "Kopterin paluuovi");
        back.transform.position = new Vector3(HeliInteriorX0 + 1.7f, area.maxDepthY, 0f);
        back.here = area; back.target = roof; back.prompt = "Palaa katolle";
        back.spawnPoint = new Vector2(entry.transform.position.x, roof.maxDepthY - 0.5f);
        back.halfWidth = 1.5f; back.maxDistanceFromWall = 2f;
        back.blockedDuringFight = true; back.fightBlockRange = width + 2f;
        var crates = new GameObject("Kopterin puulaatikot");
        Undo.RegisterCreatedObjectUndo(crates, "Kopterin puulaatikot");
        var burst = AssetDatabase.LoadAssetAtPath<Sprite>(CrateBurstPath);
        var woodSounds = LoadClips("Assets/Audio/sfx", "puu");
        int crateCount = 0;
        foreach (var spot in new[] { new Vector2(450f, 0.25f), new Vector2(1050f, 0.7f), new Vector2(1800f, 0.3f),
            new Vector2(2240f, 0.75f), new Vector2(2600f, 0.25f), new Vector2(cockpitStartPx + 1080f * cockpitScaleX, 0.7f) })
        {
            var crateGo = new GameObject("Kopterin puulaatikko " + (++crateCount));
            crateGo.transform.SetParent(crates.transform, false);
            crateGo.transform.position = new Vector3(HX(spot.x), Mathf.Lerp(area.maxDepthY, area.minDepthY, spot.y), 0f);
            var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(crateGo.transform, false);
            var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(crateGo.transform, false);
            var crate = crateGo.AddComponent<Crate>();
            crate.sprites = crateSprites; crate.burstSprite = burst; crate.body = visual; crate.shadow = shadow;
            crate.visualScale = 1.2f; crate.hitRadiusX = 1.05f; crate.breakSounds = woodSounds;
            visual.sprite = crateSprites[0]; visual.transform.localPosition = Vector3.down * crate.footOffset;
        }
        var fightRoot = new GameObject("Kopterin taistelu");
        Undo.RegisterCreatedObjectUndo(fightRoot, "Kopterin taistelu");
        var fight = fightRoot.AddComponent<HelicopterFight>();
        fight.area = area;
        GameObject Group(string name)
        {
            var group = new GameObject(name); group.transform.SetParent(fightRoot.transform, false); return group;
        }
        fight.cargoGroup = Group("Kopterin ensimmäinen aalto");
        fight.cockpitGroup = Group("Kopterin toinen aalto");
        Enemy Foe(Enemy model, GameObject group, string name, float px, float depth)
        {
            var clone = Object.Instantiate(model.gameObject, group.transform); clone.name = name;
            clone.transform.position = new Vector3(HX(px), Mathf.Lerp(area.maxDepthY, area.minDepthY, depth), 0f);
            var enemy = clone.GetComponent<Enemy>();
            enemy.fightsEveryone = false; enemy.huntsBrawlers = false; enemy.ally = false;
            enemy.appearAfterOthers = false; enemy.joinsFightWhenSquadComes = false;
            enemy.wanderMinX = enemy.wanderMaxX = 0f; enemy.wakeDistance = -1f;
            clone.SetActive(true);
            return enemy;
        }
        fight.cargoEnemies = new[] {
            Foe(pirate, fight.cargoGroup, "Kopterin rosvo 1", 600f, 0.45f),
            Foe(pirate, fight.cargoGroup, "Kopterin rosvo 2", 950f, 0.2f),
            Foe(pirate, fight.cargoGroup, "Kopterin rosvo 3", 1350f, 0.65f),
            Foe(tough, fight.cargoGroup, "Kopterin kovis 1", 800f, 0.7f),
            Foe(tough, fight.cargoGroup, "Kopterin kovis 2", 1450f, 0.35f) };
        fight.cockpitEnemies = new[] {
            Foe(pirate, fight.cockpitGroup, "Kopterin ohjaamon rosvo 1", cockpitStartPx + 650f * cockpitScaleX, 0.3f),
            Foe(pirate, fight.cockpitGroup, "Kopterin ohjaamon rosvo 2", cockpitStartPx + 1050f * cockpitScaleX, 0.65f),
            Foe(tough, fight.cockpitGroup, "Kopterin ohjaamon kovis", cockpitStartPx + 1390f * cockpitScaleX, 0.4f) };
        fight.cargoRightX = HX(w) - 1f;
        fight.cockpitTriggerX = HX(cockpitStartPx + 300f * cockpitScaleX);
        fight.cockpitLeftX = HX(cockpitStartPx + 200f * cockpitScaleX);
        fight.cargoGroup.SetActive(false); fight.cockpitGroup.SetActive(false);
        var desert = EnsureDesertArea();
        var exit = new GameObject("Kopterista Aavikkoon").AddComponent<Door>();
        Undo.RegisterCreatedObjectUndo(exit.gameObject, "Kopterista Aavikkoon");
        exit.transform.position = new Vector3(HX(cockpitStartPx + 680f * cockpitScaleX), area.maxDepthY, 0f);
        exit.here = area; exit.target = desert; exit.prompt = "Siirry Aavikkoon";
        exit.spawnPoint = new Vector2(desert.walkMinX != 0f ? desert.walkMinX + 2f : desert.camMinX, Mathf.Lerp(desert.maxDepthY, desert.minDepthY, 0.45f));
        exit.halfWidth = 1.5f; exit.maxDistanceFromWall = 2f;
        exit.blockedDuringFight = true; exit.fightBlockRange = width + 2f; exit.enabled = false;
        fight.exitDoor = exit;
        EditorSceneManager.MarkSceneDirty(bg.scene);
        Info("Kopterin sisätila ja ohjaamo luotu: 6 puulaatikkoa, ensin 3 rosvoa + 2 kovista, ohjaamossa 2 rosvoa + 1 kovis.\nOhjaamon toisesta ovesta E vie Aavikkoon, kun molemmat aallot on voitettu.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/90. Aavikko: Chinookin ulkopuoli ja parallaxit")]
    static void CreateDesert()
    {
        if (PlayModeBlocked()) return;
        const string dir = "Assets/Sprites/Taustat/";
        string[] files = { "aavikko_panorama.png", "aavikko_parallax_ylempi.png", "aavikko_parallax_alempi.png" };
        if (files.Any(f => AssetImporter.GetAtPath(dir + f) == null))
        { Info("Aavikon panoraama tai parallax-kuvat puuttuvat."); return; }
        var sprites = files.Select(f => {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + f);
            return ImportShipSprite(dir + f, texture.height / (CamHalf * 2f), false, SpriteAlignment.Center);
        }).ToArray();
        var area = EnsureDesertArea();
        Undo.RecordObject(area, "Aavikon rajat");
        foreach (var name in new[] { "Aavikon väliaikainen taivas", "Aavikon väliaikainen hiekka", "Aavikon maisemat" })
        {
            var old = GameObject.Find(name);
            if (old != null) Undo.DestroyObjectImmediate(old);
        }
        const float left = 78000f;
        const float foregroundDrop = 0.9f, sandDrop = 0.9f;
        float cameraSize = CamHalf * (16f / 15f);
        float width = sprites[0].bounds.size.x, halfScreen = cameraSize * 16f / 9f;
        area.minDepthY = CamY - CamHalf + 0.6f;
        // Kävely päättyy asfaltin takareunaan, rakennusten ja kopterin eteen.
        area.maxDepthY = CamY + CamHalf - 0.63f * CamHalf * 2f - foregroundDrop;
        area.useSidewalk = false;
        area.camMinX = left + halfScreen;
        area.camMaxX = left + width - halfScreen;
        area.walkMinX = left + 0.8f; area.walkMaxX = left + width - 0.8f;
        area.camSize = cameraSize; area.camRiseY = 0f; area.camOffsetY = 0f;
        area.music = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Musiikki/Aavikko.mp3");
        area.ambience = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Ambienssi/aavikko.wav");
        area.ambienceVolume = 1.6f;
        const string tumbleweedPath = "Assets/Sprites/Rekvisiitta/aavikko_risupallo.png";
        if (AssetImporter.GetAtPath(tumbleweedPath) != null)
        {
            var tumbleweeds = area.GetComponent<DesertTumbleweeds>();
            if (tumbleweeds == null) tumbleweeds = Undo.AddComponent<DesertTumbleweeds>(area.gameObject);
            Undo.RecordObject(tumbleweeds, "Aavikon risupallot");
            tumbleweeds.area = area;
            tumbleweeds.sprite = ImportShipSprite(tumbleweedPath, 256f, false, SpriteAlignment.Center);
            tumbleweeds.rollSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx/tumbleweed.wav");
            tumbleweeds.rollVolume = 0.08f;
            tumbleweeds.count = 4;
            tumbleweeds.minSpeed = 2.4f; tumbleweeds.maxSpeed = 3.8f;
            tumbleweeds.kickUpSpeed = 8f;
            tumbleweeds.breakSprites = LoadTumbleweedBreakSprites();
            EditorUtility.SetDirty(tumbleweeds);
        }
        var root = new GameObject("Aavikon maisemat");
        Undo.RegisterCreatedObjectUndo(root, "Aavikon maisemat");
        {
            var go = new GameObject("Aavikko (yhtenainen panoraama)");
            go.transform.SetParent(root.transform);
            go.transform.position = new Vector3(left + width * 0.5f, CamY - foregroundDrop, 0f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprites[0]; renderer.sortingOrder = -10000;
        }
        for (int i = 1; i < 3; i++)
        {
            var go = new GameObject(i == 1 ? "Aavikon taivas ja vuoret" : "Aavikon kaukainen hiekka");
            go.transform.SetParent(root.transform);
            // Hiekkakerros peitti vuoret: sen horisontti jää vuorten alapuolelle.
            go.transform.position = new Vector3(left + width * 0.5f, CamY - (i == 2 ? sandDrop : 0f), 0f);
            go.transform.localScale = new Vector3(width / sprites[i].bounds.size.x, i == 1 ? 1.15f : 1f, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprites[i]; renderer.sortingOrder = i == 1 ? -10200 : -10100;
            var parallax = go.AddComponent<ParallaxLayer>();
            parallax.factor = i == 1 ? 0.08f : 0.25f;
            parallax.startCamX = area.camMinX;
            parallax.minX = left; parallax.maxX = left + width;
        }
        var exit = GameObject.Find("Kopterista Aavikkoon")?.GetComponent<Door>();
        if (exit != null)
        {
            Undo.RecordObject(exit, "Aavikon sisääntulo");
            exit.target = area;
            // Pelaaja ilmestyy ensimmäisen kuvan avoimen lastausrampin eteen.
            exit.spawnPoint = new Vector2(left + width * 0.38f, CamY + CamHalf - 0.69f * CamHalf * 2f - foregroundDrop);
            EditorUtility.SetDirty(exit);
        }
        EditorUtility.SetDirty(area);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info("Aavikko korjattu: yhtenäinen panoraama ja kaksi parallax-kerrosta. Chinookin ovi vie lastausrampin eteen. Tallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/91. Peltihalli: ikkunaparallax, tappelijat ja baaripoydat")]
    static void CreateMetalHall()
    {
        if (PlayModeBlocked()) return;
        const string path = "Assets/Sprites/Taustat/peltihalli_panorama.png";
        var desert = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Aavikko");
        if (desert == null || AssetImporter.GetAtPath(path) == null)
        { Info("Tee ensin Aavikko (90). Tarvitaan peltihalli_panorama.png."); return; }
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var bgSprite = ImportShipSprite(path, texture.height / 12f, false, SpriteAlignment.Center);
        const float left = 80000f, centerY = 1.4f;
        float width = bgSprite.bounds.size.x, centerX = left + width * 0.5f;
        var hall = ReuseArea("Alue: Peltihalli");
        Undo.RecordObject(hall, "Peltihalli");
        hall.areaName = "Peltihalli"; hall.useSidewalk = false;
        hall.minDepthY = -4f; hall.maxDepthY = centerY + 6f - 637f / (941f / 12f);
        hall.camMinX = left + 6f * 16f / 9f;
        hall.camMaxX = left + width - 6f * 16f / 9f; hall.camSize = 6f;
        hall.camOffsetY = 0f; hall.camRiseY = 0f;
        hall.walkMinX = left + 0.8f; hall.walkMaxX = left + width - 0.8f;
        hall.depthLimits = new Vector2[0]; hall.backScale = 1f;
        hall.music = desert.music; hall.ambience = desert.ambience; hall.ambienceVolume = 0.25f;
        foreach (var name in new[] { "Peltihallin sisalto", "Aavikolta peltihalliin" })
        { var old = GameObject.Find(name); if (old != null) Undo.DestroyObjectImmediate(old); }
        var root = new GameObject("Peltihallin sisalto");
        Undo.RegisterCreatedObjectUndo(root, "Peltihalli");
        var bg = new GameObject("Peltihallin tausta").AddComponent<SpriteRenderer>();
        bg.transform.SetParent(root.transform, false);
        bg.transform.position = new Vector3(centerX, centerY, 0f);
        bg.sprite = bgSprite; bg.sortingOrder = -10000;
        bg.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Sprites/Taustat/peltihalli_ikkunat.mat");
        Vector2 arrival = new Vector2(left + width - 2.2f, -1.5f);
        for (int i = 0; i < 2; i++)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Taustat/" +
                (i == 0 ? "aavikko_parallax_ylempi.png" : "aavikko_parallax_alempi.png"));
            if (sprite == null) continue;
            var view = new GameObject(i == 0 ? "Peltihallin ikkunavuoret" : "Peltihallin ikkunahiekka").AddComponent<SpriteRenderer>();
            view.transform.SetParent(root.transform, false);
            view.sprite = sprite; view.sortingOrder = i == 0 ? -10200 : -10100;
            view.transform.localScale = new Vector3(2f, 1f, 1f);
            var parallax = view.gameObject.AddComponent<InteriorWindowParallax>();
            parallax.area = hall; parallax.anchor = new Vector2(centerX, i == 0 ? 4.1f : 3f);
            parallax.cameraReference = new Vector2(hall.camMaxX, centerY); parallax.factor = i == 0 ? 0.06f : 0.15f;
            view.transform.position = parallax.anchor;
        }
        for (int i = 0; i < 3; i++)
        {
            var biker = MakeBiker(root.transform);
            if (biker != null)
            {
                biker.name = "Peltihallin motoristi " + (i + 1);
                biker.transform.position = new Vector3(left + width * (0.78f - i * 0.25f), -1.25f, 0f);
                biker.wakeDistance = 100f; biker.gameObject.SetActive(true);
            }
            var rocker = MakeRocker(root.transform);
            if (rocker != null)
            {
                rocker.name = "Peltihallin rokkimimmi " + (i + 1);
                rocker.transform.position = new Vector3(left + width * (0.68f - i * 0.25f), -2.25f, 0f);
                rocker.wakeDistance = 100f; rocker.wanderMinX = rocker.wanderMaxX = 0f;
                rocker.gameObject.SetActive(true);
            }
        }
        AddBottleTables(root.transform, new[] { new Vector3(left + width * 0.2f, -3.25f, 0f),
            new Vector3(left + width * 0.64f, -3.25f, 0f) }, 91);
        AddSnookerTable(root.transform, new Vector2(left + width * 0.42f, -3.8f), PokerPoolWidth);
        var sohviModel = GameObject.Find("Sohvi");
        Vector2 counter = new Vector2(left + width * 0.24f, centerY + 6f - 0.46f * 12f);
        if (sohviModel != null)
        {
            var sohvi = Object.Instantiate(sohviModel, root.transform);
            sohvi.name = "Peltihallin Sohvi";
            sohvi.transform.position = counter;
            sohvi.transform.localScale = sohviModel.transform.localScale * 0.85f;
        }
        var shopModel = GameObject.Find("Baaritiski");
        if (shopModel != null)
        {
            var shop = Object.Instantiate(shopModel, root.transform);
            shop.name = "Peltihallin baaritiski";
            shop.transform.position = new Vector3(counter.x, hall.maxDepthY, 0f);
            var service = shop.GetComponent<ShopCounter>();
            service.here = hall; service.title = "PELTIHALLIN BAARI";
            service.halfWidth = 3.3f; service.maxDistanceFromWall = 1.1f;
        }
        var bikeModel = Object.FindObjectsByType<Motorbike>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(b => b.name == "Takakujan prätkä");
        if (bikeModel != null)
        {
            var bike = Object.Instantiate(bikeModel.gameObject, root.transform);
            bike.name = "Peltihallin ajettava prätkä";
            bike.transform.position = new Vector3(left + 3.2f, -2.4f, 0f);
            var riding = bike.GetComponent<Motorbike>(); riding.rideable = true;
            if (riding.parkedRight != null) { riding.parked.sprite = riding.parkedRight; riding.parked.flipX = false; }
            else riding.parked.flipX = true;
            riding.parked.sortingOrder = 240;
            var rideExit = new GameObject("Peltihallista ajo Aavikolle").AddComponent<RideExit>();
            rideExit.transform.SetParent(root.transform, false);
            rideExit.transform.position = new Vector3(hall.walkMaxX - 1.7f, -2.4f, 0f);
            rideExit.here = hall; rideExit.target = desert; rideExit.exitToLeft = false;
            rideExit.turnBikeRight = true; rideExit.spawnPoint = new Vector2(desert.walkMinX + 3.7f, -2.4f);
            rideExit.title = "Aavikko"; rideExit.triggerWidth = 4f; rideExit.titleTime = 0.7f;
        }
        if (GameObject.Find("Aavikolta tulevalle tielle") == null)
        {
            var futureRoad = new GameObject("Aavikolta tulevalle tielle").AddComponent<RideExit>();
            futureRoad.transform.position = new Vector3(desert.walkMaxX - 1.7f, -2.4f, 0f);
            futureRoad.here = desert; futureRoad.title = "Valtatie";
            // Kohde kytketään, kun käyttäjän tuleva tie on valmis.
            futureRoad.enabled = false;
            Undo.RegisterCreatedObjectUndo(futureRoad.gameObject, "Tulevan tien liitoskohta");
        }
        var enter = new GameObject("Aavikolta peltihalliin").AddComponent<Door>();
        Undo.RegisterCreatedObjectUndo(enter.gameObject, "Peltihallin ovi");
        enter.here = desert; enter.target = hall; enter.spawnPoint = arrival;
        enter.faceLeftOnArrival = true;
        enter.prompt = "Mene peltihalliin"; enter.halfWidth = 1.4f; enter.maxDistanceFromWall = 1.1f;
        enter.transform.position = new Vector3(desert.walkMinX + 1.4f, desert.maxDepthY, 0f);
        var back = new GameObject("Peltihallista Aavikkoon").AddComponent<Door>();
        back.transform.SetParent(root.transform, false);
        back.transform.position = new Vector3(arrival.x, hall.maxDepthY, 0f);
        back.here = hall; back.target = desert; back.returnToLastDoor = true;
        back.prompt = "Palaa Aavikkoon"; back.halfWidth = 1.4f; back.maxDistanceFromWall = 1.1f;
        back.spawnPoint = new Vector2(enter.transform.position.x, desert.maxDepthY - 0.3f);
        EditorUtility.SetDirty(hall); EditorSceneManager.MarkSceneDirty(root.scene);
        if (AssetImporter.GetAtPath("Assets/Sprites/Taustat/aavikkotie.png") != null)
        {
            bool wasBatch = batch;
            batch = true;
            try { CreateDesertRoad(); }
            finally { batch = wasBatch; }
        }
        SetupHallBrawl();
        Info("Peltihalli lisätty: E-sisäänkäynti Aavikon vasemmalla, ikkunaparallax, 3 motorista, 3 rokkimimmiä, 2 baaripöytää pulloineen ja snookerpöytä. Tallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/93. Aavikkotie ja ajosiirtyma Aavikon oikealta reunalta")]
    static void CreateDesertRoad()
    {
        if (PlayModeBlocked()) return;
        var desert = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Aavikko");
        const string path = "Assets/Sprites/Taustat/aavikkotie.png";
        if (desert == null || AssetImporter.GetAtPath(path) == null)
        { Info("Tarvitaan Aavikko (90) ja aavikkotie.png."); return; }
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var road = ImportShipSprite(path, texture.height / 12f, true, SpriteAlignment.Center);
        const float left = 82000f, centerY = 1.4f;
        float length = road.bounds.size.x * 24f, halfScreen = 6f * 16f / 9f;
        var area = ReuseArea("Alue: Aavikkotie");
        Undo.RecordObject(area, "Aavikkotie");
        area.areaName = "Aavikkotie"; area.useSidewalk = false;
        area.minDepthY = -3.6f; area.maxDepthY = 1.8f;
        area.camMinX = left + halfScreen; area.camMaxX = left + length - halfScreen;
        area.walkMinX = left + 0.8f; area.walkMaxX = left + length - 0.8f;
        area.camSize = 6f; area.camRiseY = 0f; area.camOffsetY = 0f;
        area.depthLimits = new Vector2[0]; area.backScale = 1f;
        area.music = desert.music; area.ambience = desert.ambience; area.ambienceVolume = desert.ambienceVolume;
        var old = GameObject.Find("Aavikkotien taustat");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = new GameObject("Aavikkotien taustat");
        Undo.RegisterCreatedObjectUndo(root, "Aavikkotie");
        var asphalt = new GameObject("Aavikkotie (toistuva)").AddComponent<SpriteRenderer>();
        asphalt.transform.SetParent(root.transform, false);
        asphalt.transform.position = new Vector3(left + length * 0.5f, centerY, 0f);
        asphalt.sprite = road; asphalt.sortingOrder = -10000;
        asphalt.drawMode = SpriteDrawMode.Tiled; asphalt.size = new Vector2(length, road.bounds.size.y);
        for (int i = 0; i < 2; i++)
        {
            string backgroundPath = "Assets/Sprites/Taustat/" + (i == 0 ? "aavikko_parallax_ylempi.png" : "aavikko_parallax_alempi.png");
            var sprite = ImportShipSprite(backgroundPath, 941f / 12f, true, SpriteAlignment.Center);
            if (sprite == null) continue;
            var view = new GameObject(i == 0 ? "Aavikkotien taivas ja vuoret" : "Aavikkotien hiekka").AddComponent<SpriteRenderer>();
            view.transform.SetParent(root.transform, false);
            view.transform.position = new Vector3(area.camMinX, i == 0 ? 5.1f : 3.7f, 0f);
            view.sprite = sprite; view.sortingOrder = i == 0 ? -10200 : -10100;
            view.drawMode = SpriteDrawMode.Tiled;
            view.size = new Vector2(sprite.bounds.size.x * 3f, sprite.bounds.size.y);
            view.transform.localScale = new Vector3(36f / sprite.bounds.size.x, i == 0 ? 1.15f : 1f, 1f);
            var moving = view.gameObject.AddComponent<ScrollingLayer>();
            moving.area = area; moving.autoSpeed = 0f; moving.parallax = i == 0 ? 0.28f : 0.65f;
        }
        var exitGo = GameObject.Find("Aavikolta tulevalle tielle");
        if (exitGo == null) { exitGo = new GameObject("Aavikolta tulevalle tielle"); Undo.RegisterCreatedObjectUndo(exitGo, "Ajosiirtymä"); }
        var exit = exitGo.GetComponent<RideExit>();
        if (exit == null) exit = Undo.AddComponent<RideExit>(exitGo);
        Undo.RecordObject(exit, "Ajosiirtymä");
        exit.transform.position = new Vector3(desert.walkMaxX - 1.7f, -2.4f, 0f);
        exit.here = desert; exit.target = area; exit.spawnPoint = new Vector2(left + 6f, -2.4f);
        exit.exitToLeft = false; exit.turnBikeRight = true; exit.parkBike = false;
        exit.title = "Aavikkotie"; exit.titleTime = 0f; exit.fadeTime = 0.4f; exit.enabled = true;
        EditorUtility.SetDirty(area); EditorUtility.SetDirty(exit);
        BuildDesertPairs(false);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info("Aavikkotie lisätty: Aavikon oikealta reunalta musta häivytys vain prätkällä, sama musiikki ja tuuli, nopeasti vierivät parallaxit. Tallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/94. Aavikkotie: motoristi ja rokkimimmi heron pyoralla")]
    static void AddDesertPairs()
    {
        if (PlayModeBlocked()) return;
        BuildDesertPairs(true);
    }

    static void BuildDesertPairs(bool report)
    {
        var road = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Aavikkotie");
        var heroBike = Object.FindObjectsByType<Motorbike>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(b => b.rideable && b.parkedRight != null);
        var frames = Resources.LoadAll<Sprite>("AjoPari");
        var idle = PariRider.Frames(frames, "ajo_idle_");
        var punch = PariRider.Frames(frames, "lyonti_");
        if (road == null || heroBike == null || idle.Length != 63 || punch.Length != 31)
        { if (report) Info("Tarvitaan Aavikkotie (93), heron pyörä ja AjoPari-kuvat (63 ajoa, 31 lyöntiä)."); return; }
        var old = GameObject.Find("Aavikkotien ajoparit");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = new GameObject("Aavikkotien ajoparit");
        Undo.RegisterCreatedObjectUndo(root, "Aavikon ajopari");
        var model = new GameObject("Ajopari (malli)"); model.transform.SetParent(root.transform, false);
        var pair = model.AddComponent<PariRider>(); pair.area = road;
        pair.idleSprites = idle; pair.punchSprites = punch; pair.fps = 15f;
        pair.riderScale = 0.72f; pair.motorcycleScale = 0.576f;
        pair.riderOffset = new Vector2(-1.296f, 0.576f);
        pair.rearRim = Resources.Load<Sprite>("AjoPariBike/rear_wheel");
        pair.frontRim = Resources.Load<Sprite>("AjoPariBike/front_wheel");
        pair.chassisSprite = Resources.Load<Sprite>("AjoPariBike/chassis");
        pair.body = new GameObject("Motoristi ja rokkimimmi").AddComponent<SpriteRenderer>();
        pair.body.transform.SetParent(model.transform, false); pair.body.sprite = idle[0];
        pair.motorcycle = new GameObject("Heron pyörä (tumma)").AddComponent<SpriteRenderer>();
        pair.motorcycle.transform.SetParent(model.transform, false); pair.motorcycle.sprite = heroBike.parkedRight;
        pair.shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); pair.shadow.transform.SetParent(model.transform, false);
        pair.shadow.color = new Color(0f, 0f, 0f, 0.3f);
        pair.engineLoop = heroBike.engineLoop; pair.engineVolume = 0.2f;
        pair.overtakeSpeed = 10f; pair.depthSpeed = 1.2f;
        var enemyModel = Object.FindObjectsByType<EnemyBike>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(b => b.GetType() == typeof(EnemyBike));
        if (enemyModel != null) pair.yankSound = enemyModel.yankSound;
        model.SetActive(false);
        var spawner = root.AddComponent<EnemyBikeSpawner>(); spawner.template = pair;
        spawner.minX = road.walkMinX; spawner.maxX = road.walkMaxX;
        spawner.interval = new Vector2(12f, 18f); spawner.maxAlive = 1; spawner.density = 1f;
        spawner.skaterTemplate = null; spawner.skaterChance = 0f;
        EditorSceneManager.MarkSceneDirty(root.scene);
        if (report) Info("Ajopari lisätty Aavikkotielle: heron tumma pyörä, 15 fps ja rokkimimmin lyönti. Heron riutaisu väläyttää pyörän punaiseksi ja hidastaa sen. Tallenna scene (Ctrl+S).");
    }

    /// Risupallon hajoamiskuvat (risupallo_hajoaa_1…4, 512 × 512): hajoaa kolmannessa osumassa, viimeinen kuva häipyy.
    static Sprite[] LoadTumbleweedBreakSprites()
    {
        var list = new List<Sprite>();
        for (int i = 1; i <= 4; i++)
        {
            string path = $"Assets/Sprites/Rekvisiitta/risupallo_hajoaa_{i}.png";
            if (AssetImporter.GetAtPath(path) == null) return null;
            list.Add(ImportShipSprite(path, 100f, false, SpriteAlignment.Center));
        }
        return list.ToArray();
    }

    [MenuItem("Beat em up/102. Risupallojen hajoaminen kolmannessa osumassa (kaikille sceneen)")]
    static void UpdateTumbleweedBreak()
    {
        var sprites = LoadTumbleweedBreakSprites();
        if (sprites == null) { Info("Tarvitaan risupallo_hajoaa_1…4.png (Sprites/Rekvisiitta)."); return; }
        int n = 0;
        foreach (var w in Object.FindObjectsByType<DesertTumbleweeds>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(w, "Risupallon hajoaminen");
            w.breakSprites = sprites; w.breakHits = 3;
            EditorUtility.SetDirty(w); n++;
        }
        if (n > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Info($"Risupallot hajoavat kolmannessa potkuosumassa ({n} aluetta). Tallenna scene (Ctrl+S).");
    }

    /// Kermapiirakka pöydälle: poimittava, heitettävä (kääntyy kerma edellä) ja läsähtää osuessaan.
    static Bottle MakePie(Transform parent, Crate table, float tableX, float tableTop, Vector3 pos = default, float restHeight = 0f, SpriteRenderer restOn = null)
    {
        Sprite Imp(string name, bool bottom)
        {
            string path = $"Assets/Sprites/Rekvisiitta/{name}.png";
            return AssetImporter.GetAtPath(path) == null ? null : ImportShipSprite(path, 100f, false, bottom ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
        }
        var frames = new Sprite[7];
        for (int i = 0; i < 7; i++) frames[i] = Imp($"piirakka_{i}", i == 0 || i == 6);
        var tumble = new[] { Imp("piirakka_pyorii_0", false), Imp("piirakka_pyorii_1", false), Imp("piirakka_pyorii_2", false), Imp("piirakka_pyorii_3", false) };
        if (frames.Any(s => s == null) || tumble.Any(s => s == null)) return null;
        var go = new GameObject("Piirakka");
        go.transform.SetParent(parent, false);
        go.transform.position = table != null ? table.transform.position : pos;
        var b = go.AddComponent<Bottle>();
        b.restHeight = restHeight; b.restOnRenderer = restOn;   // tiskillä: lepää tiskin pinnalla (taustan päällä)
        b.sprites = frames; b.tumbleSprites = tumble; b.flightSprites = new[] { tumble[0], tumble[1], frames[1] };
        b.pie = true; b.keepDebris = true; b.frameTime = 0.07f; b.stainKind = "-";
        b.scale = 1f; b.pivotY = 0.4f; b.throwDamage = 8; b.throwSpeed = 13f; b.pickRangeX = 1.3f;
        b.breakSounds = LoadClips("Assets/Audio/sfx", "piirakka");   // osumaääni (piirakka1, piirakka2)
        b.table = table; b.tableX = tableX; b.tableTop = tableTop;
        return b;
    }

    /// Uusi vihu saa piirakan osuma- ja heittokuvat heti (sama kuin valikko 105, mutta vain tälle hahmolle).
    static void ApplyPieArt(Enemy e, List<string> report)
    {
        if (e == null) return;
        string first = e.idleSprites != null && e.idleSprites.Length > 0 && e.idleSprites[0] != null ? e.idleSprites[0].name : "";
        foreach (var (prefix, hit) in new[] { ("rusty_", "rusty_piirakka"), ("rokkari2_", "rokkari2_piirakka"), ("motoristi_", "motoristi_piirakka"), ("puliukko_", "puliukko_piirakka"), ("rekkamies_", "rekkamies_piirakka") })
        {
            if (!first.Contains(prefix)) continue;
            string hp = FindTexture(hit); if (hp != null) SetupAndSlice(hp);
            var hs = EnemySheet(hit, report);
            if (hs.Length >= 3) e.pieSprites = hs.Take(3).ToArray();
        }
        foreach (var (prefix, sheet, grips) in PieThrowData)
        {
            if (!first.Contains(prefix)) continue;
            string tp = FindTexture(sheet); if (tp != null) SetupAndSlice(tp);
            var ts = EnemySheet(sheet, report);
            if (ts.Length >= 5) { e.pieThrowSprites = ts.Take(5).ToArray(); e.pieGrips = grips; }
        }
    }

    // piirakan poiminta ja heitto: hahmo (idle-kuvan alku), kuvasarja, piirakan paikka kuvissa 0–4
    static readonly (string prefix, string sheet, Vector3[] grips)[] PieThrowData = {
        ("puliukko_", "puliukko_heitto", new[] { Vector3.zero, new Vector3(0.32f, 1.93f, 0f), new Vector3(-0.72f, 3.39f, 0f), new Vector3(0.97f, 2.84f, 0f), Vector3.zero }),
        ("rokkari2_", "rokkari2_heitto", new[] { Vector3.zero, new Vector3(0.36f, 2.72f, 0f), new Vector3(-0.82f, 3.40f, 0f), new Vector3(1.69f, 2.73f, 0f), Vector3.zero }),
        ("motoristi_", "motoristi_heitto", new[] { Vector3.zero, new Vector3(0.69f, 1.88f, 0f), new Vector3(-0.53f, 2.87f, 0f), new Vector3(1.88f, 2.84f, 0f), Vector3.zero }),
        ("rusty_", "rusty_heitto", new[] { Vector3.zero, new Vector3(0.49f, 2.30f, 0f), new Vector3(-0.62f, 3.24f, 0f), new Vector3(1.38f, 2.68f, 0f), Vector3.zero }),
        ("rekkamies_", "rekkamies_heitto", new[] { Vector3.zero, new Vector3(0.62f, 1.78f, 0f), new Vector3(-1.22f, 2.85f, 0f), new Vector3(1.42f, 2.75f, 0f), Vector3.zero }),
    };

    [MenuItem("Beat em up/106. Linsey dineriin (kävelee ja pysähtyy tiskille)")]
    static void AddLinsey()
    {
        var diner = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Diner");
        if (diner == null) { Info("Tee ensin Diner (103)."); return; }
        var sets = new Dictionary<string, Sprite[]>();
        foreach (var n in new[] { "linsey_idle", "linsey_kavely", "linsey_tiskille", "linsey_tiskilla", "linsey_piirakka", "linsey_heitto" })
        {
            string path = FindTexture(n);
            if (path == null) { if (n == "linsey_piirakka" || n == "linsey_heitto") { sets[n] = new Sprite[0]; continue; } Info("Puuttuu " + n + ".png (Sprites)."); return; }
            SetupAndSlice(path);
            sets[n] = LoadSprites(n).OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
        }
        var old = GameObject.Find("Linsey");
        if (old != null) Undo.DestroyObjectImmediate(old);
        const float left = 90000f; float ppu = DinerPpu;
        var go = new GameObject("Linsey");
        Undo.RegisterCreatedObjectUndo(go, "Linsey");
        var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
        visual.transform.localPosition = new Vector3(0f, -0.06f, 0f);   // jalat 6 px solun alareunasta
        visual.sprite = sets["linsey_idle"][0];
        var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
        shadow.color = new Color(0f, 0f, 0f, 0.3f);
        shadow.transform.localScale = new Vector3(1.2f, 0.4f, 1f);
        var l = go.AddComponent<DinerLinsey>();
        l.area = diner; l.body = visual; l.shadow = shadow;
        l.idle = sets["linsey_idle"]; l.walk = sets["linsey_kavely"]; l.toCounter = sets["linsey_tiskille"]; l.atCounter = sets["linsey_tiskilla"];
        if (sets["linsey_piirakka"].Length >= 4) l.pieHit = sets["linsey_piirakka"].Take(4).ToArray();
        // piirakan poiminta ja heitto (5 kuvaa) ja piirakan paikka kuvissa (x eteen, y korkeus)
        if (sets["linsey_heitto"].Length >= 5)
        {
            l.pieThrow = sets["linsey_heitto"].Take(5).ToArray();
            l.pieGrips = new[] { Vector3.zero, new Vector3(0.46f, 2.08f, 0f), new Vector3(0.29f, 2.09f, 0f), new Vector3(-0.93f, 2.73f, 0f), new Vector3(1.2f, 2.75f, 0f) };
        }
        // äänet (Assets/Audio/linsey): osuma, heitto ja puhe
        l.hitSounds = LoadClips("Assets/Audio/linsey", "linsey_hit");
        l.throwSound = LoadClips("Assets/Audio/linsey", "linsey_throw").FirstOrDefault();
        l.speakSounds = LoadClips("Assets/Audio/linsey", "linsey_speak");
        l.maxY = 1.4f + DinerHalfH - 545f / ppu; l.laneMinX = left + 400f / ppu; l.laneMaxX = left + 4000f / ppu;
        l.counterXs = new[] { 1600f, 2300f, 2650f, 3150f }.Select(px => left + px / ppu).ToArray();
        go.transform.position = new Vector3(left + 1300f / ppu, diner.maxDepthY - 0.8f, 0f);
        EditorSceneManager.MarkSceneDirty(go.scene);
        Info("Linsey dineriin: " + l.idle.Length + " idle-, " + l.walk.Length + " kävely-, " + l.toCounter.Length + " tiskille- ja " + l.atCounter.Length + " tiskillä-kuvaa. "
             + l.pieHit.Length + " piirakkakuvaa, heittokuvat " + l.pieThrow.Length + ", äänet " + (l.hitSounds.Length + l.speakSounds.Length + (l.throwSound != null ? 1 : 0)) + " kpl. Heittää piirakoita vihuihin ja osuu piirakasta. Hän ei ole taistelija eikä lyötävissä: kävelee lattialla ja pysähtyy välillä tiskille nojaamaan.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/105. Piirakka naamaan: Rusty ja rokkimimmi (kaikille sceneen)")]
    static void AssignPieHits()
    {
        var report = new List<string>();
        foreach (var n in new[] { "rusty_piirakka", "rokkari2_piirakka", "motoristi_piirakka", "puliukko_piirakka", "rocco_piirakka", "sankaritar_piirakka", "rekkamies_piirakka", "rekkamies_heitto", "rekkamies_piirakkakavely", "puliukko_heitto", "rokkari2_heitto", "rusty_heitto", "motoristi_heitto" })
        {
            string path = FindTexture(n);
            if (path != null) SetupAndSlice(path);
        }
        var rusty = EnemySheet("rusty_piirakka", report);
        var rocker = EnemySheet("rokkari2_piirakka", report);
        var biker = EnemySheet("motoristi_piirakka", report);
        var drunk = EnemySheet("puliukko_piirakka", report);
        var trucker = EnemySheet("rekkamies_piirakka", report);
        var truckerCarry = EnemySheet("rekkamies_piirakkakavely", report);
        var rubyPie = LoadSprites("sankaritar_piirakka").OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
        var rocco = LoadSprites("rocco_piirakka").OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
        int nr = 0, nk = 0, nb = 0, nd = 0, ns = 0, nt = 0;
        var pieSnd = LoadClips("Assets/Audio/sfx", "piirakka");
        foreach (var pb in Object.FindObjectsByType<Bottle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (pb != null && pb.pie && pieSnd.Length > 0) { Undo.RecordObject(pb, "Piirakan ääni"); pb.breakSounds = pieSnd; EditorUtility.SetDirty(pb); ns++; }
        var hero = Object.FindFirstObjectByType<PlayerController>();
        if (hero != null && rocco.Length >= 3)
        {
            Undo.RecordObject(hero, "Piirakkaosuma");
            hero.pieHitSprites = rocco.Take(3).ToArray();
            EditorUtility.SetDirty(hero);
        }
        if (hero != null && rubyPie.Length >= 3)
        {
            Undo.RecordObject(hero, "Rubyn piirakkaosuma");
            hero.rubyPieHitSprites = rubyPie.Take(3).ToArray();   // Ruby saa oman piirakkaosuman (heittää samalla logiikalla kuin Rocco)
            EditorUtility.SetDirty(hero);
        }
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string first = e.idleSprites != null && e.idleSprites.Length > 0 && e.idleSprites[0] != null ? e.idleSprites[0].name : "";
            if (e.calmIdleSprites != null && e.calmIdleSprites.Length > 0 && e.calmIdleSprites[0] != null) first += "|" + e.calmIdleSprites[0].name;
            Sprite[] set = first.StartsWith("rusty_") && rusty.Length >= 3 ? rusty : first.Contains("rokkari2_") && rocker.Length >= 3 ? rocker
                : first.StartsWith("motoristi_") && biker.Length >= 3 ? biker : first.StartsWith("puliukko_") && drunk.Length >= 3 ? drunk
                : first.StartsWith("rekkamies_") && trucker.Length >= 3 ? trucker : null;
            // piirakan poiminta ja heitto (5 kuvaa) ja piirakan paikka kuvissa (x eteen, y korkeus, kallistus)
            foreach (var (prefix, sheet, grips) in PieThrowData)
            {
                if (!(prefix == "rusty_" ? first.StartsWith(prefix) : first.Contains(prefix))) continue;
                var ts = EnemySheet(sheet, report);
                if (ts.Length < 5) continue;
                Undo.RecordObject(e, "Piirakan heitto");
                e.pieThrowSprites = ts.Take(5).ToArray(); e.pieGrips = grips;
                EditorUtility.SetDirty(e); nt++;
            }
            if (set == null) continue;
            Undo.RecordObject(e, "Piirakkaosuma");
            e.pieSprites = set.Take(3).ToArray();
            if (set == trucker && truckerCarry.Length >= 8) { e.pieCarrySprites = truckerCarry; e.pieCarryGrip = new Vector3(0.72f, 1.82f, 0f); e.pieCarryFrameTime = 0.08f; }   // kantaa piirakkaa kädet alhaalla (kävely videosta)
            EditorUtility.SetDirty(e);
            if (set == rusty) nr++; else if (set == rocker) nk++; else if (set == biker) nb++; else nd++;
        }
        if (nr + nk > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        string rep = string.Join("\n", report);
        Info("Piirakka naamaan: Rocco " + (hero != null && rocco.Length >= 3 ? "kyllä" : "ei") + ", Ruby " + (hero != null && rubyPie.Length >= 3 ? "kyllä" : "ei") + ", " + nr + " Rustya, " + nk + " rokkimimmiä, " + nb + " motoristia ja " + nd + " puliukkoa, piirakan heitto " + nt + " vihulle, osumaääni " + ns + " piirakalle (muut ja Ruby läsäyttävät piirakan omilla kuvillaan). Heitetty piirakka katoaa osumahetkellä ja vihun oma sarja soi.\n" + rep + "\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/104. Diner: pöydät, tuolit, olutpullot ja snookerpöytä oikeaan päätyyn")]
    static void AddDinerFurniture()
    {
        var diner = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Diner");
        if (diner == null) { Info("Tee ensin Diner (103)."); return; }
        var old = GameObject.Find("Dinerin kalusteet");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = new GameObject("Dinerin kalusteet");
        Undo.RegisterCreatedObjectUndo(root, "Dinerin kalusteet");
        const float left = 90000f; float ppu = DinerPpu;
        // pöydät lattialle boksien ja tiskin eteen (kuvapikselit panoraamassa → x; syvyys y)
        var tables = DinerTableSpots(diner, left, ppu);
        var info = AddBottleTables(root.transform, tables, 41, true);
        var snookerPos = DinerClamp(diner, new Vector3(left + 3500f / ppu, -4.4f, 0f), PokerPoolWidth * 0.5f);
        var snooker = AddSnookerTable(root.transform, new Vector2(snookerPos.x, snookerPos.y), PokerPoolWidth);
        // aamiaisannokset pöydille (Resources/Aamiainen): kaksi annosta pöytää kohti, kuten El Loipparin kala-annokset
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Aamiainen" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100; ti.alphaIsTransparency = true; ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            var st = new TextureImporterSettings(); ti.ReadTextureSettings(st);
            st.spriteAlignment = (int)(Path.GetFileNameWithoutExtension(path) == "annos" ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
            ti.SetTextureSettings(st);
            ti.SaveAndReimport();
        }
        var plateSnd = LoadClips("Assets/Audio/sfx", "posliini");
        float top = 1.28f * TableScale - 0.04f;
        int plates = 0;
        int tableNo = 0, pies = 0;
        foreach (var table in root.GetComponentsInChildren<Crate>().Where(c => c.gameObject.name == "Pöytä"))
        {
            bool piePlate = ++tableNo > 0;   // jokaisella pöydällä aamiaisannos ja kermapiirakka
            if (piePlate && MakePie(root.transform, table, 0.62f * TableScale, top) != null) pies++;
            foreach (float px in new[] { -0.62f, 0.62f })
            {
                if (piePlate && px > 0f) continue;
                var pGo = new GameObject("Aamiaisannos");
                pGo.transform.SetParent(root.transform, false);
                pGo.transform.position = table.transform.position;
                var fp = pGo.AddComponent<FishPlate>();
                fp.spriteSet = "Aamiainen"; fp.countMul = 0.35f;
                fp.table = table; fp.tableX = px * TableScale; fp.tableTop = top + 0.04f;
                fp.breakSounds = plateSnd;
                plates++;
            }
        }
        // tiskille ilmestyy piirakoita poimittavaksi; Sohvi tiskin taakse ja kauppa tiskin eteen (energia ja stamina)
        var dinerBg = GameObject.Find("Dinerin tausta");
        var bgSr = dinerBg != null ? dinerBg.GetComponent<SpriteRenderer>() : null;
        float counterTop = 1.4f + DinerHalfH - 405f / ppu;    // tiskin pinnan rivi (px 405) maailman y:ssä
        float counterHeight = counterTop - diner.maxDepthY;   // tiskin pinnan korkeus lattiarajasta ylöspäin (positiivinen)
        var tmpl = MakePie(root.transform, null, 0f, 0f, new Vector3(left, diner.maxDepthY, 0f), counterHeight, bgSr);
        string counterInfo = "";
        if (tmpl != null)
        {
            tmpl.name = "Piirakkamalli";
            tmpl.gameObject.SetActive(false);
            var pc = root.AddComponent<PieCounter>();
            pc.area = diner; pc.template = tmpl; pc.y = diner.maxDepthY - 0.05f; pc.interval = 3f; pc.initial = 6;
            pc.xs = new[] { 1480f, 1640f, 1800f, 1960f, 2120f, 2280f, 2440f, 2600f, 2760f, 3000f, 3140f, 3280f }.Select(px => left + px / ppu).ToArray();
            counterInfo = "tiskille ilmestyy piirakoita (6 paikkaa)";
        }
        var sohvi = GameObject.Find("Sohvi");
        if (sohvi != null)
        {
            var s2 = Object.Instantiate(sohvi, root.transform);
            s2.name = "Dinerin Sohvi";
            s2.transform.position = new Vector3(left + 1850f / ppu, counterTop - 0.1f, 0f);
            s2.transform.localScale = sohvi.transform.localScale * 0.85f;
            counterInfo += ", Sohvi tiskin takana";
        }
        var clubShop = GameObject.Find("Baaritiski");
        if (clubShop != null)
        {
            var sh = Object.Instantiate(clubShop, root.transform);
            sh.name = "Dinerin tiski";
            sh.transform.position = new Vector3(left + 1850f / ppu, diner.maxDepthY, 0f);
            var shop = sh.GetComponent<ShopCounter>();
            if (shop != null)
            {
                shop.here = diner; shop.title = "DINERIN TISKI"; shop.halfWidth = 3.3f; shop.maxDistanceFromWall = 1.1f;
                shop.items = new[] {
                    new ShopCounter.Item { name = "Kahvi",              price = 3,  heal = 10,  stamina = 60,  comment = "Vahvaa ja mustaa." },
                    new ShopCounter.Item { name = "Voileipä",           price = 5,  heal = 40,  stamina = 20,  comment = "Tuoretta." },
                    new ShopCounter.Item { name = "Kermapiirakka",      price = 6,  heal = 30,  stamina = 40,  comment = "Kerma edellä!" },
                    new ShopCounter.Item { name = "Aamiaislautanen",    price = 9,  heal = 70,  stamina = 30,  comment = "Pekonia, kananmuna ja paahtoleipä." },
                    new ShopCounter.Item { name = "Dinerin erikoinen",  price = 15, heal = 999, stamina = 999, comment = "Täydet voimat!" },
                };
            }
            counterInfo += ", kauppa tiskin edessä";
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info($"Dineriin {tables.Length} pöytää ({info.bottles} olutpulloa, {plates} aamiaisannosta, {pies} piirakkaa), {info.chairs} tuolia ja " + (snooker != null ? "snookerpöytä oikeaan päätyyn" : "EI snookerpöytää (biljardipoyta.png puuttuu)") + ".\nAamiaisannokset hajoavat pöytään lyödessä tai potkiessa, ja ainekset sinkoilevat.\n" + counterInfo + "\n\nTallenna scene (Ctrl+S).");
    }

    // Dinerin sisäkuva pienennetty: tiski ja jakkarat olivat liian isoja hahmoihin nähden (0,75 = 25 % pienemmät)
    const float DinerScale = 0.75f;
    static float DinerPpu => 941f / 12f / DinerScale;
    static float DinerHalfH => 941f / DinerPpu * 0.5f;   // kuvan puolikorkeus yksikköinä

    /// Spriten todellinen (tiukka) leveys maailman yksikköinä: solun läpinäkyvät reunat eivät lasketa.
    static float TightSpriteWidth(Sprite sp)
    {
        if (sp == null) return 0.3f;
        var v = sp.vertices;
        if (v == null || v.Length == 0) return sp.bounds.size.x;
        float mn = float.MaxValue, mx = float.MinValue;
        foreach (var q in v) { mn = Mathf.Min(mn, q.x); mx = Mathf.Max(mx, q.x); }
        return Mathf.Max(0.05f, mx - mn);
    }

    /// Pitää dinerin esineen kävelyalueen sisällä (halfW = esineen puolileveys): ei seinän, reunan tai tiskin läpi.
    static Vector3 DinerClamp(Area d, Vector3 p, float halfW)
    {
        float xMin = d.walkMinX + halfW + 0.4f, xMax = d.walkMaxX - halfW - 0.4f;
        p.x = Mathf.Clamp(p.x, xMin, Mathf.Max(xMin, xMax));
        float yMax = Mathf.Min(d.MaxDepthAtX(p.x, d.maxDepthY), d.maxDepthY) - 0.5f;
        p.y = Mathf.Clamp(p.y, d.minDepthY + 0.3f, yMax);
        return p;
    }

    /// Dinerin pöytäpaikat (kuvapikseli x, y): ylärivi pöytien ja tiskin edessä, alarivi alalattialla.
    static Vector3[] DinerTableSpots(Area d, float left, float ppu)
    {
        var raw = new[] { new Vector2(520f, -3.1f), new Vector2(1150f, -2.5f), new Vector2(1950f, -3.3f), new Vector2(2750f, -2.7f),
                          new Vector2(450f, -5.5f), new Vector2(1250f, -6.0f), new Vector2(2050f, -5.4f), new Vector2(2850f, -6.0f), new Vector2(3450f, -5.5f) };
        return raw.Select(v => DinerClamp(d, new Vector3(left + v.x / ppu, v.y, 0f), 1.6f)).ToArray();
    }
    [MenuItem("Beat em up/103. Diner: sisätila (3 kuvaa) ja E-ovi Aavikkokadulta")]
    static void CreateDiner()
    {
        if (PlayModeBlocked()) return;
        const string path = "Assets/Sprites/Taustat/diner_sisa.png";
        var street = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Aavikkokatu");
        if (street == null || AssetImporter.GetAtPath(path) == null)
        { Info("Tee ensin Aavikkokatu (95). Tarvitaan diner_sisa.png (Sprites/Taustat)."); return; }
        float ppu = DinerPpu;
        var bgSprite = ImportShipSprite(path, ppu, false, SpriteAlignment.Center);
        const float left = 90000f, centerY = 1.4f;
        float width = bgSprite.bounds.size.x, centerX = left + width * 0.5f;
        float DepthAt(float py) => centerY + DinerHalfH - py / ppu;
        var diner = ReuseArea("Alue: Diner");
        Undo.RecordObject(diner, "Diner");
        diner.areaName = "Diner"; diner.useSidewalk = false;
        diner.minDepthY = -7f; diner.maxDepthY = DepthAt(640f);   // lattia jatkuu alas: kävelytilaa ja paikkaa pöydille, vihuille diner.depthLimitsExceedMax = true;   // takarajan käyrä saa ylittää maxDepthY:n (EXIT-kolo)
        // kamera loitonnettu (7,2: kuva on 6:n korkuinen): yläreuna pysyy kuvan yläreunassa, alle jatkuu peilattu lattia (diner_lattia.png)
        const float dinerCamSize = 7.2f;
        diner.camMinX = left + dinerCamSize * 16f / 9f; diner.camMaxX = left + width - dinerCamSize * 16f / 9f; diner.camSize = dinerCamSize;
        diner.camOffsetY = (centerY + DinerHalfH - dinerCamSize) - CamY; diner.camRiseY = 0f;
        diner.walkMinX = left + 0.8f; diner.walkMaxX = left + width - 0.8f;
        // Takaraja: boksit vasemmalla, tiski keskellä, korotettu boksialue oikealla (kuvapikselit panoraamassa).
        diner.depthLimits = new[] {
            new Vector2(left + 0f, DepthAt(610f)), new Vector2(left + 1330f / ppu, DepthAt(610f)),
            new Vector2(left + 1420f / ppu, DepthAt(640f)), new Vector2(left + 2950f / ppu, DepthAt(640f)),
            new Vector2(left + 3010f / ppu, DepthAt(545f)), new Vector2(left + 3130f / ppu, DepthAt(545f)),
            // STAFF-oven kohdalla (EXIT) lattia jatkuu ylös ovelle asti: hero pääsee kävelemään oven eteen
            new Vector2(left + 3150f / ppu, DepthAt(440f)), new Vector2(left + 3420f / ppu, DepthAt(440f)),
            new Vector2(left + 3440f / ppu, DepthAt(545f)), new Vector2(left + width, DepthAt(545f)) };
        diner.backScale = 1f;
        diner.music = street.music; diner.ambience = street.ambience; diner.ambienceVolume = 0.2f;
        foreach (var name in new[] { "Dinerin sisalto", "Aavikkokadulta dineriin" })
        { var old = GameObject.Find(name); if (old != null) Undo.DestroyObjectImmediate(old); }
        var root = new GameObject("Dinerin sisalto");
        Undo.RegisterCreatedObjectUndo(root, "Diner");
        var bg = new GameObject("Dinerin tausta").AddComponent<SpriteRenderer>();
        bg.transform.SetParent(root.transform, false);
        bg.transform.position = new Vector3(centerX, centerY, 0f);
        bg.sprite = bgSprite; bg.sortingOrder = -10000;
        const string floorPath = "Assets/Sprites/Taustat/diner_lattia.png";
        if (AssetImporter.GetAtPath(floorPath) != null)
        {
            var floorSprite = ImportShipSprite(floorPath, ppu, false, SpriteAlignment.Center);
            var floorExt = new GameObject("Dinerin lattian jatko").AddComponent<SpriteRenderer>();
            floorExt.transform.SetParent(root.transform, false);
            floorExt.transform.position = new Vector3(centerX, centerY - DinerHalfH - floorSprite.bounds.size.y * 0.5f, 0f);
            floorExt.sprite = floorSprite; floorExt.sortingOrder = -10000;
        }
        // Keskimmäinen kuva (tiski) alkaa pikselistä 1362 ja on 1672 leveä: sisääntulo sen keskelle.
        Vector2 arrival = new Vector2(left + (1362f + 836f) / ppu, -2.4f);
        const float doorPx = 867f;   // pariovi aavikkokadujatke2-kuvassa (diner suurennettu kuvassa: leveys 1,06 ja korkeus 1,20)
        float doorX = 86000f + 5896.992f / (887f / 12f) + doorPx * 1.157184f / (887f / 12f);
        var enter = new GameObject("Aavikkokadulta dineriin").AddComponent<Door>();
        Undo.RegisterCreatedObjectUndo(enter.gameObject, "Dinerin ovi");
        enter.here = street; enter.target = diner; enter.spawnPoint = arrival;
        enter.prompt = "Mene dineriin"; enter.halfWidth = 1.4f; enter.maxDistanceFromWall = 1.1f;
        enter.transform.position = new Vector3(doorX, street.maxDepthY, 0f);
        var back = new GameObject("Dinerista kadulle").AddComponent<Door>();
        back.transform.SetParent(root.transform, false);
        // uloskäynti: STAFF ONLY -ovi kuvan oikeassa osassa (ei vahingossa ulos); oven päälle EXIT-kyltti
        const float exitPx = 3285f;
        float exitX = left + exitPx / ppu;
        back.transform.position = new Vector3(exitX, diner.MaxDepthAtX(exitX, diner.maxDepthY), 0f);
        back.here = diner; back.target = street; back.returnToLastDoor = false;   // paluu aina kadulle dinerin oven eteen (ei riipu siitä, mistä pelaaja tuli)
        back.prompt = "Mene ulos (EXIT)"; back.halfWidth = 1.7f; back.maxDistanceFromWall = 0.9f;   // käytettävissä vasta ylhäällä oven edessä
        const string exitSignPath = "Assets/Sprites/Rekvisiitta/exit_kyltti.png";   // 2172 x 724, kuvan oma kyltti
        if (AssetImporter.GetAtPath(exitSignPath) != null)
        {
            var sign = new GameObject("Dinerin EXIT-kyltti").AddComponent<SpriteRenderer>();
            sign.transform.SetParent(root.transform, false);
            sign.sprite = ImportShipSprite(exitSignPath, 1150f / DinerScale, false, SpriteAlignment.Center);   // ~1,9 yksikköä leveä
            sign.transform.position = new Vector3(exitX, centerY + DinerHalfH - 128f / ppu, 0f);   // oven yläpuolelle
            sign.sortingOrder = -9990;
        }
        back.spawnPoint = new Vector2(doorX, street.maxDepthY - 0.3f);
        EditorUtility.SetDirty(diner); EditorSceneManager.MarkSceneDirty(root.scene);
        Info("Diner lisätty: E-ovi Aavikkokadun dinerin pääovesta sisään keskikuvaan (tiski), jatko vasemmalle (boksit) ja oikealle (kassa, ikkuna). Ulos pääsee STAFF-oven kohdalta (EXIT-kyltti) E:llä. Tallenna scene (Ctrl+S).");
    }

    // Aavikkobaarin uusi panoraama (3733 x 1031, kolme Codexin kuvaa): 71,7 px/yksikkö; vanhojen (4102 px) kuvakoordinaattien muunnos uuteen
    const float BarPpu = 71.7f, BarTopY = 7.4f;
    static readonly float[,] BarAnchors = { { 0f, 0f }, { 805f, 731f }, { 1395f, 1270f }, { 2510f, 2312f }, { 2665f, 2440f }, { 2850f, 2590f }, { 3350f, 3015f }, { 3900f, 3440f }, { 4102f, 3733f } };
    static float BarPx(float oldPx)
    {
        for (int i = 1; i < BarAnchors.GetLength(0); i++)
            if (oldPx <= BarAnchors[i, 0] || i == BarAnchors.GetLength(0) - 1)
                return Mathf.Lerp(BarAnchors[i - 1, 1], BarAnchors[i, 1], (oldPx - BarAnchors[i - 1, 0]) / (BarAnchors[i, 0] - BarAnchors[i - 1, 0]));
        return oldPx;
    }
    static float BarWx(float oldPx) => BarPx(oldPx) / BarPpu;   // x-etäisyys panoraaman vasemmasta reunasta (yksikköä)

    [MenuItem("Beat em up/110. Aavikkobaari: sisätila (3 kuvaa), ovi kadulta ja portaat yläkertaan")]
    static void CreateDesertBar()
    {
        if (PlayModeBlocked()) return;
        const string path = "Assets/Sprites/Taustat/aavikkobaari_sisa.png";   // kolme kuvaa yhdistettynä (4102 × 941): ovi, tiski, lava ja portaat
        var street = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Aavikkokatu");
        if (street == null || AssetImporter.GetAtPath(path) == null)
        { Info("Tee ensin Aavikkokatu (95). Tarvitaan aavikkobaari_sisa.png (Sprites/Taustat)."); return; }
        float ppu = BarPpu;
        var bgSprite = ImportShipSprite(path, ppu, false, SpriteAlignment.Center);
        const float left = 91000f, centerY = 1.4f;
        float width = bgSprite.bounds.size.x, centerX = left + width * 0.5f;
        float DepthAt(float py) => BarTopY - py / ppu;
        var bar = ReuseArea("Alue: Aavikkobaari");
        Undo.RecordObject(bar, "Aavikkobaari");
        bar.areaName = "Aavikkobaari"; bar.useSidewalk = false;
        bar.minDepthY = DepthAt(1000f); bar.maxDepthY = DepthAt(548f);
        const float camSize = 7.2f;   // loitonnettu: tiski ja lava mahtuvat ruutuun, alle jatkuu peilattu lattia (aavikkobaari_lattia.png)
        bar.camMinX = left + camSize * 16f / 9f; bar.camMaxX = left + width - camSize * 16f / 9f; bar.camSize = camSize;
        bar.camOffsetY = (centerY + 6f - camSize) - CamY; bar.camRiseY = 0f;
        bar.walkMinX = left + 0.8f; bar.walkMaxX = left + width - 0.8f;
        // Takaraja kuvapikseleinä: seinä, tiski (edempänä), portaiden juuri, lava ja oikean seinän vinous.
        bar.depthLimits = new[] {
            new Vector2(left + 0f, DepthAt(625f)), new Vector2(left + 75f / ppu, DepthAt(548f)),
            new Vector2(left + 1260f / ppu, DepthAt(548f)), new Vector2(left + 1272f / ppu, DepthAt(598f)),
            new Vector2(left + 2310f / ppu, DepthAt(598f)), new Vector2(left + 2322f / ppu, DepthAt(560f)),
            new Vector2(left + 2575f / ppu, DepthAt(560f)), new Vector2(left + 2587f / ppu, DepthAt(472f)),
            new Vector2(left + 3440f / ppu, DepthAt(472f)), new Vector2(left + width, DepthAt(690f)) };
        bar.backScale = 1f;
        var barMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Musiikki/Aavikkobaari.mp3");   // bändin blues
        bar.music = barMusic != null ? barMusic : street.music; bar.ambience = street.ambience; bar.ambienceVolume = barMusic != null ? 0.08f : 0.2f;
        foreach (var name in new[] { "Aavikkobaarin sisalto", "Aavikkokadulta baariin" })
        { var old = GameObject.Find(name); if (old != null) Undo.DestroyObjectImmediate(old); }
        var root = new GameObject("Aavikkobaarin sisalto");
        Undo.RegisterCreatedObjectUndo(root, "Aavikkobaari");
        var bg = new GameObject("Aavikkobaarin tausta").AddComponent<SpriteRenderer>();
        bg.transform.SetParent(root.transform, false);
        bg.transform.position = new Vector3(centerX, BarTopY - bgSprite.bounds.size.y * 0.5f, 0f);
        bg.sprite = bgSprite; bg.sortingOrder = -10000;
        // Sisään: pariovi Aavikkokadun viimeisessä kuvassa (jatkokuva 6, ovi kuvan x 990).
        float streetDoorX = 86000f + (11629.3f + 990f * 1.0095f) / (887f / 12f);
        var enter = new GameObject("Aavikkokadulta baariin").AddComponent<Door>();
        Undo.RegisterCreatedObjectUndo(enter.gameObject, "Baarin ovi");
        float doorX = left + 731f / ppu;   // sisäpuolen pariovi ensimmäisessä kuvassa
        enter.here = street; enter.target = bar; enter.spawnPoint = new Vector2(doorX, -1.2f);
        enter.prompt = "Mene baariin"; enter.halfWidth = 1.4f; enter.maxDistanceFromWall = 1.1f;
        enter.transform.position = new Vector3(streetDoorX, street.maxDepthY, 0f);
        var back = new GameObject("Baarista kadulle").AddComponent<Door>();
        back.transform.SetParent(root.transform, false);
        back.transform.position = new Vector3(doorX, bar.MaxDepthAtX(doorX, bar.maxDepthY), 0f);
        back.here = bar; back.target = street; back.returnToLastDoor = true;
        back.prompt = "Mene ulos"; back.halfWidth = 1.5f; back.maxDistanceFromWall = 1.1f;
        back.spawnPoint = new Vector2(streetDoorX, street.maxDepthY - 0.3f);
        // Portaat yläkertaan: ovi portaiden juurella (takaraja on siinä portaiden alareunassa).
        float stairsX = left + 2440f / ppu;
        var upstairs = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Aavikkobaarin yläkerta");
        string stairInfo;
        if (upstairs != null)
        {
            var up = new GameObject("Baarin portaat ylös").AddComponent<Door>();
            up.transform.SetParent(root.transform, false);
            up.transform.position = new Vector3(stairsX, bar.MaxDepthAtX(stairsX, bar.maxDepthY), 0f);
            up.here = bar; up.target = upstairs; up.spawnPoint = new Vector2(upstairs.walkMinX + 2f, upstairs.maxDepthY - 0.5f);
            up.prompt = "Mene yläkertaan"; up.halfWidth = 1.3f; up.maxDistanceFromWall = 0.9f;
            stairInfo = "Portaat vievät alueelle Aavikkobaarin yläkerta.";
        }
        else stairInfo = "Portaiden ovi tehdään, kun yläkerran kuvat on tehty (alue \"Aavikkobaarin yläkerta\"); portaiden juurelle (x = " + stairsX.ToString("0.0") + ") pääsee jo kävelemään.";
        EditorUtility.SetDirty(bar); EditorSceneManager.MarkSceneDirty(root.scene);
        Info("Aavikkobaari lisätty: E-ovi Aavikkokadun viimeisen kuvan pariovesta sisään, ulos sisäoven kohdalta. Tiski ja portaat estävät kävelyn takaa.\n" + stairInfo + "\nAja ensin 95 (katu pitenee), sitten tämä. Tallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/111. Aavikkobaari: pöydät, tuolit, pullot ja kaksi snookerpöytää (tanssitila lavan edessä)")]
    static void AddDesertBarFurniture()
    {
        var bar = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Aavikkobaari");
        if (bar == null) { Info("Tee ensin Aavikkobaari (110)."); return; }
        var old = GameObject.Find("Aavikkobaarin kalusteet");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = new GameObject("Aavikkobaarin kalusteet");
        Undo.RegisterCreatedObjectUndo(root, "Aavikkobaarin kalusteet");
        const float left = 91000f; float ppu = 941f / 12f;
        // kuvapikselit panoraamassa → x; syvyys y. Lavan edusta (x 2850-3900) jätetään tanssitilaksi, ovi (x 805) ja portaat (x 2665) vapaiksi.
        var tables = new[] { new Vector3(left + BarWx(300f), -0.9f, 0f), new Vector3(left + BarWx(1100f), -1.8f, 0f),
            new Vector3(left + BarWx(2300f), -1.9f, 0f), new Vector3(left + BarWx(2620f), -3.3f, 0f),
            new Vector3(left + BarWx(4000f), -2.6f, 0f),
            new Vector3(left + BarWx(1000f), -3.2f, 0f), new Vector3(left + BarWx(1700f), -2.4f, 0f),
            // pari pöytää lavan eteen: reunoille, keskelle jää tanssilattia (Elaine ja Rekkamies tanssivat x 3250-3450)
            new Vector3(left + BarWx(3000f), -0.9f, 0f), new Vector3(left + BarWx(3750f), -1.0f, 0f),
            new Vector3(left + BarWx(2950f), -3.6f, 0f), new Vector3(left + BarWx(3700f), -3.6f, 0f),
            // alas peilatulle lattialle lisää pöytiä (kamera loitonnettu)
            new Vector3(left + BarWx(450f), -5.5f, 0f), new Vector3(left + BarWx(1250f), -5.7f, 0f), new Vector3(left + BarWx(2000f), -5.4f, 0f),
            new Vector3(left + BarWx(2800f), -5.6f, 0f), new Vector3(left + BarWx(3550f), -5.4f, 0f), new Vector3(left + BarWx(3950f), -4.9f, 0f) };
        var info = AddBottleTables(root.transform, tables, 57, false, ti => ti % 3 == 2 ? float.NaN : (ti % 2 == 0 ? -1f : 1f) * 0.62f);   // sama lautasen paikka kuin pekoniannoksilla alla
        int snooker = 0;
        foreach (float px in new[] { 600f, 1800f })
            if (AddSnookerTable(root.transform, new Vector2(left + BarWx(px), -3.5f), PokerPoolWidth) != null) snooker++;
        // pekoniannokset (Resources/Aamiainen) joillekin pöydille sinne tänne
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Aamiainen" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100; ti.alphaIsTransparency = true; ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            var st = new TextureImporterSettings(); ti.ReadTextureSettings(st);
            st.spriteAlignment = (int)(Path.GetFileNameWithoutExtension(path) == "annos" ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
            ti.SetTextureSettings(st);
            ti.SaveAndReimport();
        }
        var plateSnd = LoadClips("Assets/Audio/sfx", "posliini");
        float plateTop = 1.28f * TableScale - 0.04f;
        int plates = 0, tableNo = 0;
        foreach (var table in root.GetComponentsInChildren<Crate>().Where(c => c.gameObject.name == "Pöytä"))
        {
            tableNo++;
            if (tableNo % 3 == 2) continue;   // noin kahdella kolmesta pöydästä on annos
            float px = (tableNo % 2 == 0 ? -1f : 1f) * 0.62f;
            var pGo = new GameObject("Pekoniannos");
            pGo.transform.SetParent(root.transform, false);
            pGo.transform.position = table.transform.position;
            var fp = pGo.AddComponent<FishPlate>();
            fp.spriteSet = "Aamiainen"; fp.countMul = 0.35f;
            fp.table = table; fp.tableX = px * TableScale; fp.tableTop = plateTop - 0.1f;   // annos hieman alemmas pöydällä
            fp.breakSounds = plateSnd;
            plates++;
        }
        // lisätuolit: yksi kunkin pöydän eteen sekä seinien ja lavan sivujen viereen (tanssitila jää tyhjäksi)
        var chairSprite = ImportProp("Assets/Sprites/Rekvisiitta/tuoli.png");
        var wood = AssetDatabase.FindAssets("t:AudioClip puu", new[] { "Assets/Audio" }).Select(AssetDatabase.GUIDToAssetPath)
            .Where(q => Path.GetFileNameWithoutExtension(q).ToLowerInvariant().StartsWith("puu")).Select(AssetDatabase.LoadAssetAtPath<AudioClip>).Where(clip => clip != null).ToArray();
        var extra = tables.Select((t, i) => new Vector3(t.x + (i % 2 == 0 ? 0.5f : -0.5f), Mathf.Max(t.y - 0.8f, -6.4f), 0f))
            .Concat(new[] { 150f, 1000f, 1450f, 2100f, 2350f, 2850f, 3900f }.Select((px, i) => new Vector3(left + BarWx(px), -3.8f + (i % 3) * 0.3f, 0f)))
            .Append(new Vector3(left + BarWx(4040f), -1.4f, 0f)).ToArray();
        int extraChairs = 0;
        if (chairSprite != null)
            for (int i = 0; i < extra.Length; i++)
            {
                var cgo = new GameObject("Tuoli");
                cgo.transform.SetParent(root.transform, false);
                cgo.transform.position = extra[i];
                var cb = new GameObject("Visual").AddComponent<SpriteRenderer>(); cb.transform.SetParent(cgo.transform, false);
                cb.sprite = chairSprite; cb.flipX = i % 2 == 1; cb.transform.localScale = new Vector3(ChairScaleSmall, ChairScaleSmall, 1f);
                var csh = new GameObject("Shadow").AddComponent<SpriteRenderer>(); csh.transform.SetParent(cgo.transform, false);
                csh.color = new Color(0f, 0f, 0f, 0.3f);
                var ch = cgo.AddComponent<Chair>(); ch.body = cb; ch.shadow = csh; ch.breakSounds = wood;
                extraChairs++;
            }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info($"Baariin {tables.Length} pöytää ({info.bottles} pulloa, {info.glasses} lasia, {plates} pekoniannosta), {info.chairs + extraChairs} tuolia ja {snooker} snookerpöytää (pari pöytää myös lavan edessä). Lavan edessä on tyhjää tanssitilaa.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/112. Elaine baariin (seisoskelee, kävelee, nojaa tiskiin ja tanssii lavan edessä)")]
    static void AddElaine()
    {
        var bar = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Aavikkobaari");
        if (bar == null) { Info("Tee ensin Aavikkobaari (110)."); return; }
        var sets = new Dictionary<string, Sprite[]>();
        foreach (var n in new[] { "elaine_idle", "elaine_kavely", "elaine_tiskille", "elaine_tiskilla", "elaine_tanssi" })
        {
            string path = FindTexture(n);
            if (path == null) { Info("Puuttuu " + n + ".png (Sprites)."); return; }
            SetupAndSlice(path);
            sets[n] = LoadSprites(n).OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
        }
        var old = GameObject.Find("Elaine");
        if (old != null) Undo.DestroyObjectImmediate(old);
        const float left = 91000f; float ppu = 941f / 12f;
        var go = new GameObject("Elaine");
        Undo.RegisterCreatedObjectUndo(go, "Elaine");
        var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
        visual.transform.localPosition = new Vector3(0f, -0.06f, 0f);   // jalat 6 px solun alareunasta
        visual.sprite = sets["elaine_idle"][0];
        var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
        shadow.color = new Color(0f, 0f, 0f, 0.3f);
        shadow.transform.localScale = new Vector3(1.2f, 0.4f, 1f);
        var e = go.AddComponent<BarElaine>();
        e.area = bar; e.body = visual; e.shadow = shadow;
        e.idle = sets["elaine_idle"]; e.walk = sets["elaine_kavely"]; e.toCounter = sets["elaine_tiskille"]; e.atCounter = sets["elaine_tiskilla"]; e.dance = sets["elaine_tanssi"];
        e.laneMinX = left + BarWx(300f); e.laneMaxX = left + BarWx(4000f);
        e.counterXs = new[] { 1700f, 2000f, 2300f }.Select(px => left + BarWx(px)).ToArray();
        // tanssilattia lavan edessä (x 2900-3800 px)
        e.danceX = new Vector2(left + BarWx(3000f), left + BarWx(3700f));
        e.danceY = new Vector2(-3.0f, -1.2f);
        go.transform.position = new Vector3(left + BarWx(1000f), -2.2f, 0f);
        EditorSceneManager.MarkSceneDirty(go.scene);
        Info("Elaine baariin: " + e.idle.Length + " idle-, " + e.walk.Length + " kävely-, " + e.toCounter.Length + " tiskille-, " + e.atCounter.Length + " tiskillä- ja " + e.dance.Length + " tanssikuvaa. Hän seisoskelee, kävelee, nojaa välillä tiskiin ja tanssii lavan edessä. Ei taistelija eikä lyötävissä.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/113. Aavikkobaari: bändi soittaa lavalla (silmukka)")]
    static void AddDesertBarBand()
    {
        var bar = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Aavikkobaari");
        string bp = FindTexture("bandi_aavikkobaari");
        if (bar == null || bp == null) { Info("Tee ensin Aavikkobaari (110). Tarvitaan bandi_aavikkobaari.png (Sprites)."); return; }
        SetupAndSlice(bp);
        var sprites = LoadSprites("bandi_aavikkobaari")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        if (sprites.Length == 0) { Info("bandi_aavikkobaari.png: ei kuvia."); return; }
        var old = GameObject.Find("Aavikkobaarin bändi");
        if (old != null) Undo.DestroyObjectImmediate(old);
        const float left = 91000f, centerY = 1.4f; float ppu = 941f / 12f;
        var go = new GameObject("Aavikkobaarin bändi");
        // lavan keskelle (kuvapikseli x 3350), jalat lavan pinnalla (rivi 405)
        go.transform.position = new Vector3(left + BarWx(3350f), BarTopY - 375f / BarPpu, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprites[0];
        sr.sortingOrder = -9000;                       // taustan edessä, pelaajien takana
        var d = go.AddComponent<Dancer>();
        d.sprites = sprites;
        d.frameTime = 0.077f;                          // videon oma tahti (74 ruutua 24 fps, 40 kuvaa)
        Undo.RegisterCreatedObjectUndo(go, "Bändi");
        EditorSceneManager.MarkSceneDirty(go.scene);
        Info($"Bändi soittaa lavalla: {sprites.Length} kuvaa silmukkana.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/114. Shanice baariin (kävelee, juttelee Elainen kanssa, kuuntelee ja tanssii lavan edessä)")]
    static void AddShanice()
    {
        var bar = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Aavikkobaari");
        if (bar == null) { Info("Tee ensin Aavikkobaari (110)."); return; }
        var sets = new Dictionary<string, Sprite[]>();
        foreach (var n in new[] { "shanice_idle", "shanice_kavely", "shanice_puhe", "shanice_tanssi" })
        {
            string path = FindTexture(n);
            if (path == null) { Info("Puuttuu " + n + ".png (Sprites)."); return; }
            SetupAndSlice(path);
            sets[n] = LoadSprites(n).OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
        }
        var old = GameObject.Find("Shanice");
        if (old != null) Undo.DestroyObjectImmediate(old);
        const float left = 91000f; float ppu = 941f / 12f;
        var go = new GameObject("Shanice");
        Undo.RegisterCreatedObjectUndo(go, "Shanice");
        var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
        visual.transform.localPosition = new Vector3(0f, -0.06f, 0f);   // jalat 6 px solun alareunasta
        visual.sprite = sets["shanice_idle"][0];
        var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
        shadow.color = new Color(0f, 0f, 0f, 0.3f);
        shadow.transform.localScale = new Vector3(1.2f, 0.4f, 1f);
        var e = go.AddComponent<BarElaine>();
        e.area = bar; e.body = visual; e.shadow = shadow;
        e.idle = sets["shanice_idle"]; e.walk = sets["shanice_kavely"]; e.talk = sets["shanice_puhe"]; e.dance = sets["shanice_tanssi"];
        e.idleFrameTime = 0.083f; e.walkFrameTime = 0.0417f; e.talkFrameTime = 0.125f; e.danceFrameTime = 0.146f;
        e.dancePingPong = true;
        e.counterChance = 0f; e.danceChance = 0.3f; e.talkChance = 0.35f;
        e.laneMinX = left + BarWx(300f); e.laneMaxX = left + BarWx(4000f);
        e.danceX = new Vector2(left + BarWx(3000f), left + BarWx(3700f));
        e.danceY = new Vector2(-3.0f, -1.2f);
        var elaine = GameObject.Find("Elaine");
        if (elaine != null) e.partner = elaine.GetComponent<BarElaine>();
        go.transform.position = new Vector3(left + BarWx(1500f), -2.6f, 0f);
        EditorSceneManager.MarkSceneDirty(go.scene);
        Info("Shanice baariin: " + e.idle.Length + " idle-, " + e.walk.Length + " kävely-, " + e.talk.Length + " puhe- ja " + e.dance.Length + " tanssikuvaa. Hän kävelee, käy juttelemassa Elainen vieressä (" + (e.partner != null ? "Elaine löytyi" : "EI Elainea: aja 112 ja sitten tämä uudelleen") + "), seisoskelee kuuntelemassa ja tanssii lavan edessä. Ei taistelija eikä lyötävissä.\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Rekkamies ----------------
    // rekkamies_calm (12 kuvaa, rauhallinen idle), rekkamies_taistelu (6, kamppailuidle), rekkamies_juoksu (15, silmukka), rekkamies_kavely (21, silmukka),
    // rekkamies_taklaus (6: 0 juoksuasento, 1-5 olkataklaus), rekkamies_kaatuminen (6: 0 osuma, 1-4 ilmassa, 5 makaa),
    // rekkamies_ylosnousu (6), rekkamies_alakoukku (6), rekkamies_heitetty (8: 0-1 ote, 2 nosto, 3-4 lento, 5 alastulo, 6-7 makaa) ja rekkamies_iskut (36): 0-6 jab, 7-13 suora, 14-20 potku, 21-35 korkea potku.
    [MenuItem("Beat em up/115. Rekkamies: kombo, juoksu ja torjumaton taklaus (kaikkia vastaan)")]
    static void CreateTrucker()
    {
        AssetDatabase.Refresh();   // kuvat siirrettiin Viholliset-kansioon: päivitä tietokanta, ettei vanha polku jää voimaan
        foreach (var n in new[] { "rekkamies_calm", "rekkamies_taistelu", "rekkamies_juoksu", "rekkamies_taklaus", "rekkamies_kaatuminen", "rekkamies_ylosnousu", "rekkamies_iskut", "rekkamies_alakoukku", "rekkamies_heitetty", "rekkamies_kavely", "rekkamies_poyta", "rekkamies_kanto" })
        {
            string path = FindTexture(n);
            if (path == null) { Info("Puuttuu " + n + ".png (Sprites)."); return; }
            SetupAndSlice(path);
        }
        var report = new List<string>();
        var all = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var calm = EnemySheet("rekkamies_calm", report);
        var fight = EnemySheet("rekkamies_taistelu", report);
        var run = EnemySheet("rekkamies_juoksu", report);
        var tackle = EnemySheet("rekkamies_taklaus", report);
        var fall = EnemySheet("rekkamies_kaatuminen", report);
        var rise = EnemySheet("rekkamies_ylosnousu", report);
        var atk = EnemySheet("rekkamies_iskut", report);
        var upper = EnemySheet("rekkamies_alakoukku", report);
        var thrown = EnemySheet("rekkamies_heitetty", report);
        var tbl = EnemySheet("rekkamies_poyta", report);
        if (tbl.Length < 6) { Info("rekkamies_poyta (6 kuvaa) puuttuu.\n\n" + string.Join("\n", report)); return; }
        var carry = EnemySheet("rekkamies_kanto", report);
        var walk = EnemySheet("rekkamies_kavely", report);
        if (walk.Length < 8) { Info("rekkamies_kavely (21 kuvaa) puuttuu.\n\n" + string.Join("\n", report)); return; }
        if (upper.Length < 6 || thrown.Length < 8) { Info("rekkamies_alakoukku (6) tai rekkamies_heitetty (8) puuttuu.\n\n" + string.Join("\n", report)); return; }
        if (calm.Length < 1 || fight.Length < 1 || run.Length < 8 || tackle.Length < 6 || fall.Length < 6 || rise.Length < 6 || atk.Length < 36)
        { Info("Kuvia puuttuu.\n\n" + string.Join("\n", report)); return; }
        var old = GameObject.Find("Rekkamies");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var bar = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Aavikkobaari");
        Sprite[] R(int a, int n) => atk.Skip(a).Take(n).ToArray();
        var go = new GameObject("Rekkamies");
        var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
        var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
        var t = go.AddComponent<Enemy>();
        t.body = visual; t.shadow = shadow; visual.sprite = calm[0];
        t.displayName = "Rekkamies";
        t.visualScale = 1f; t.attackArtScale = 1f; t.knockArtScale = 1f; t.walkArtScale = 1f;
        t.calmIdleSprites = calm; t.idleSprites = fight; t.idleFrameTime = 0.2f;
        t.walkSprites = walk; t.walkFrameTime = 0.0667f; t.calmWalkSprites = walk; t.calmWalkFrameTime = 0.0667f;
        t.runSprites = run; t.runFrameTime = 0.042f;
        // osuma = kaatumissarjan 1. kuva; kaatuminen 6 kuvaa (0-4 ilmassa, 5 makaa); ylösnousu 6 kuvaa
        t.hurtSprites = new[] { fall[0] }; t.knockdownSprites = fall.Take(6).ToArray();
        t.getUpSprites = rise.Take(6).ToArray(); t.getUpTime = 1.0f;
        // erikoisliike: juoksu kohti ja olkataklaus (ei voi torjua; hypyllä yli). Ylösnousuna taklauskuvat takaperin.
        t.slideSprites = tackle.Take(6).ToArray();
        t.slideUpSprites = new[] { tackle[3], tackle[2], tackle[1], tackle[0] };
        t.slideChance = 0.8f; t.slideDamage = 26; t.slideRunSpeed = 11f; t.slideSpeed = 12f; t.slideTime = 0.6f;
        t.slideMinRange = 3.5f; t.slideMaxRange = 14f; t.slideCooldown = 3f; t.slideStartDist = 3.6f;
        // J: jab, S: suora, P: etupotku, K: korkea potku (kombon lopetus)
        t.punchSprites = R(0, 7); t.punchImpactFrame = 2; t.punchDamage = 11;
        t.punch2Sprites = R(7, 7); t.punch2ImpactFrame = 1; t.punch2Damage = 15; t.punch2Chance = 0.4f; t.punch2Knockdown = false;
        t.altAttackSprites = R(14, 7); t.altImpactFrame = 2; t.altDamage = 17; t.altReach = 3.0f; t.altKnockdown = true;
        t.altKnockSpeed = 6f; t.altKnockUp = 4f; t.chargeRange = 0f; t.altChance = 0f; t.altNearChance = 0.3f; t.altExtraWindup = 0.05f;
        t.punch3Sprites = R(21, 15); t.punch3ImpactFrame = 8; t.punch3Damage = 24; t.punch3Knockdown = true; t.punch3Chance = 0f;
        t.punch3WindupTime = 0.7f; t.punch3RecoverTime = 0.5f; t.punch3Reach = 3.2f; t.punch3LaunchUp = 7f; t.punch3LaunchX = 6f;
        // U: alakoukku (kuvat 0-2 kyykky ja veto, 3-5 nousu): lennättää kaikki ruudun yläreunaan, kevyet yli ruudun
        t.uppercutSprites = upper.Take(6).ToArray(); t.uppercutImpactFrame = 3; t.uppercutDamage = 22; t.uppercutChance = 0.15f;
        t.uppercutReach = 2.4f; t.uppercutWindupTime = 0.5f; t.uppercutRecoverTime = 0.5f;
        t.uppercutHeroUp = 24f; t.uppercutLaunchUp = 21f; t.uppercutLaunchX = 1.5f;
        t.combos = new[] { "JSPK", "JSJSK", "JSU", "JJSU", "JSK" }; t.comboChance = 0.6f; t.comboArmor = true;
        SetBossThrown(t, thrown);   // heitettynä (hero kuperkeikkaheitto)
        // pöydän heitto: 0 kurkotus, 1 nosto, 2 pään yli, 3 veto taakse, 4 heitto, 5 jatko. Samat kuvat kevyen vihun nostoon ja heittoon:
        // grabSprites (8): 0 kurkotus, 1-4 nosto, 5-6 heitto, 7 paluu. Pienempiä esineitä ei heitetä, heroa ei napata.
        t.grabSprites = new[] { tbl[0], tbl[1], tbl[2], tbl[2], tbl[3], tbl[4], tbl[4], tbl[5] };
        t.throwsTables = true; t.tableRange = 7f; t.barrelThrowSpeed = 24.2f; t.barrelThrowUp = 1.5f;   // sama lentomatka kuin heron baaripöydän heitolla (11 x 2,2; nousu 5 x 0,3)
        t.throwSpeed = 24.2f; t.throwUp = 4f;   // heitetty vihu lentää yhtä kauas (~12 yks) t.barrelDamage = 24; t.barrelRate = 0.5f;
        t.carryWalkSprites = carry; t.carryWalkFrameTime = 0.0417f; t.carryThrowRange = 6f;   // kantaa nostetun heroa kohti (28 kuvaa, silmukka) ja heittää lähempää
        t.grabsHero = false; t.grabsAnyone = true; t.grabsLightOnly = true; t.grabChance = 0.25f;
        t.windupTime = 0.2f; t.punchRecoverTime = 0.35f; t.attackRange = 2.4f;
        t.moveSpeedX = 2.7f; t.moveSpeedY = 1.6f; t.runSpeedMultiplier = 2f;
        t.maxHealth = 520; t.attackCooldown = 0.5f;
        t.blockChance = 0.35f; t.maxBlocksInRow = 2;
        t.hurtSounds = LoadClips("Assets/Audio/big thug", "gasp"); t.hurtVolume = 1f;
        t.bigBody = true; t.wakeDistance = 10f; t.fightsEveryone = true;
        // taisteluäänet samat kuin Koviksella: kopioidaan Koviksen ääniasetukset, muuten big thug -kansion äänet
        var kovis = all.FirstOrDefault(en => en != null && en.gameObject.name == "Kovis");
        t.punch3WindupSounds = LoadClips("Assets/Audio/big thug", "haymaker_lataus"); t.punch3SwingSounds = LoadClips("Assets/Audio/big thug", "haymaker_isku");
        if (kovis != null)
            foreach (var fn in new[] { "hurtSounds", "hurtVolume", "attackSounds", "attackVolume", "attackSoundChance", "fallVoice", "fallVoiceVolume", "getUpVoice", "deathVoice", "deathVoiceVolume", "launchVoice", "launchVoiceVolume", "punch3WindupSounds", "punch3SwingSounds", "punch3Volume", "altSwingSounds", "altWindupSounds", "altWindupVolume", "slapSounds", "tauntSounds" })
            {
                var fi = typeof(Enemy).GetField(fn);
                if (fi != null) fi.SetValue(t, fi.GetValue(kovis));
            }
        t.flankChance = 0.15f; t.retreatChance = 0f;
        float x = bar != null ? bar.walkMinX + 18f : 0f;
        go.transform.position = new Vector3(x, -2.6f, 0f);
        Undo.RegisterCreatedObjectUndo(go, "Rekkamies");
        EditorSceneManager.MarkSceneDirty(go.scene);
        Info("Rekkamies luotu " + (bar != null ? "Aavikkobaariin" : "origoon (Aavikkobaaria ei löytynyt: siirrä hänet käsin)") + ". Rauhallinen idle, kamppailuasento, kombot (jab, suora, etupotku, korkea potku, alakoukku), osuma, kaatuminen ja ylösnousu sekä juoksu ja torjumaton olkataklaus (hyppy väistää). Tappelee kaikkien kanssa, 520 elämää.\nPuuttuu vielä: torjunta. Alakoukku lennättää heron kattoon ja muut vihut ruudun yläreunaan (kevyet yli ruudun).\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/116. Aavikkobaari: väki (3 motoristia, 3 rokkimimmiä, 2 Rustya, puliukko, rekkamies), Sohvi ja tiski, tappelu 2 min jälkeen")]
    static void AddDesertBarCrowd()
    {
        var bar = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Aavikkobaari");
        if (bar == null) { Info("Tee ensin Aavikkobaari (110)."); return; }
        var old = GameObject.Find("Aavikkobaarin tappelu");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var all = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var root = new GameObject("Aavikkobaarin tappelu");
        Undo.RegisterCreatedObjectUndo(root, "Aavikkobaarin tappelu");
        const float left = 91000f, centerY = 1.4f; float ppu = 941f / 12f;
        float X(float px) => left + BarWx(px);
        var members = new List<Enemy>();
        var names = new List<string>();
        // väki odottaa rauhallisena (ei heräämistä läheisyydestä): herää, kun joku saa osuman tai 2 min on kulunut
        void Add(Enemy e, string label, float px, float y, int facing, float wander)
        {
            if (e == null) return;
            e.gameObject.name = label;
            e.transform.position = new Vector3(X(px), y, 0f);
            e.wakeDistance = -1f; e.joinsFightWhenSquadComes = false; e.huntsBrawlers = true;
            e.wanderMinX = X(px - wander); e.wanderMaxX = X(px + wander); e.idleFacing = facing;
            e.gameObject.SetActive(true);
            members.Add(e); names.Add(label);
        }
        Add(MakeBiker(root.transform), "Baarin motoristi 1", 900f, -2.4f, 1, 160f);
        Add(MakeBiker(root.transform), "Baarin motoristi 2", 1550f, -1.6f, -1, 120f);
        Add(MakeBiker(root.transform), "Baarin motoristi 3", 2050f, -1.7f, 1, 120f);
        Add(MakeRocker(root.transform), "Baarin rokkimimmi 1", 420f, -2.0f, 1, 100f);
        Add(MakeRocker(root.transform), "Baarin rokkimimmi 2", 1300f, -2.9f, -1, 140f);
        Add(MakeRocker(root.transform), "Baarin rokkimimmi 3", 3300f, -2.2f, -1, 160f);
        Add(MakeRusty(root.transform), "Baarin Rusty 1", 2250f, -2.5f, -1, 140f);
        Add(MakeRusty(root.transform), "Baarin Rusty 2", 3900f, -1.5f, -1, 100f);
        var puli = all.FirstOrDefault(en => en != null && en.gameObject.name == "Puliukko");
        if (puli != null)
        {
            var pc = Object.Instantiate(puli.gameObject, root.transform);
            Add(pc.GetComponent<Enemy>(), "Baarin puliukko", 1700f, -2.7f, 1, 140f);
        }
        else names.Add("(puliukko puuttuu: aja 1-2 vihujen kohdat)");
        if (puli != null)
        {
            var pc2 = Object.Instantiate(puli.gameObject, root.transform);
            Add(pc2.GetComponent<Enemy>(), "Baarin puliukko 2", 2900f, -4.2f, -1, 180f);   // toinen puliukko alalattialle
        }
        // alalattialle käppäilemään kaksi Kovista ja kaksi punkkaria (punkkari heittää pikkutavaraa)
        foreach (var (who, px, y) in new[] { ("Kovis", 700f, -5.4f), ("Punkkari", 1500f, -5.6f), ("Kovis", 2900f, -5.3f), ("Punkkari", 3700f, -5.5f),
            ("Prätkäjätkä", 1000f, -4.6f), ("Rokkimimmi tappelija", 2200f, -5.9f), ("Lippis", 2600f, -4.7f), ("Punkkari", 3300f, -6.1f), ("Kovis", 400f, -6.0f) })
        {
            Enemy made = null;
            if (who == "Rokkimimmi tappelija") made = MakeRocker(root.transform);
            else if (who == "Prätkäjätkä") made = MakeBiker(root.transform);
            else
            {
                var tmpl = all.FirstOrDefault(en => en != null && en.gameObject.name == who);
                if (tmpl != null) made = Object.Instantiate(tmpl.gameObject, root.transform).GetComponent<Enemy>();
            }
            if (made == null) { names.Add("(" + who + " puuttuu)"); continue; }
            Add(made, "Baarin " + who + " (alalattia) " + (int)px, px, y, px < 2000f ? 1 : -1, 260f);
        }
        // rekkamies (tehty kohdassa 115): siirretään baarin oikeaan päätyyn lavan viereen
        var trucker = GameObject.Find("Rekkamies");
        var tEnemy = trucker != null ? trucker.GetComponent<Enemy>() : null;
        if (tEnemy != null)
        {
            Undo.RecordObject(tEnemy, "Rekkamies baariin");
            // tanssii Elainen kanssa tanssilattialla (rauhallinen idle = hänen tanssinsa), kunnes tappelu alkaa
            tEnemy.transform.position = new Vector3(X(3450f), -2.2f, 0f);
            tEnemy.wakeDistance = -1f; tEnemy.joinsFightWhenSquadComes = false;
            tEnemy.wanderMinX = tEnemy.wanderMaxX = 0f; tEnemy.idleFacing = -1;
            members.Add(tEnemy); names.Add("rekkamies");
        }
        else names.Add("(rekkamies puuttuu: aja 115 ja sitten tämä uudelleen)");
        // Sohvi tiskin taakse ja kauppa tiskin eteen (kuten dinerissä)
        string shopInfo = "Sohvi puuttuu";
        float counterX = X(1965f), counterTop = BarTopY - 425f / BarPpu;
        var sohvi = GameObject.Find("Sohvi");
        if (sohvi != null)
        {
            var s2 = Object.Instantiate(sohvi, root.transform);
            s2.name = "Baarin Sohvi";
            s2.transform.position = new Vector3(counterX, counterTop - 0.1f, 0f);
            s2.transform.localScale = sohvi.transform.localScale * 0.85f;
            shopInfo = "Sohvi tiskin takana";
        }
        var clubShop = GameObject.Find("Baaritiski");
        if (clubShop != null)
        {
            var sh = Object.Instantiate(clubShop, root.transform);
            sh.name = "Baarin tiski";
            sh.transform.position = new Vector3(counterX, bar.MaxDepthAtX(counterX, bar.maxDepthY), 0f);
            var shop = sh.GetComponent<ShopCounter>();
            if (shop != null) { shop.here = bar; shop.title = "BAARIN TISKI"; shop.halfWidth = 4.5f; shop.maxDistanceFromWall = 1.1f; }
            shopInfo += ", kauppa tiskin edessä";
        }
        // portsarit: tulevat sisäovelta, kun tappelu alkaa (2 + 1 aaltona)
        string bouncerInfo = "Portsari-malli puuttuu";
        var bouncerT = all.FirstOrDefault(en => en != null && en.gameObject.name == "Portsari");
        BouncerSquad squad = null;
        if (bouncerT != null)
        {
            var sgo = new GameObject("Baarin portsarit");
            sgo.transform.SetParent(root.transform, false);
            squad = sgo.AddComponent<BouncerSquad>();
            squad.area = bar; squad.bothSides = false; squad.firstDelay = 1.2f; squad.spawnInterval = 1.0f;
            squad.waves = new[] { 3, 2 }; squad.waveNextAt = 1;
            squad.bouncers = new Enemy[5];
            float doorX = X(805f);
            for (int i = 0; i < 5; i++)
            {
                var bgo = Object.Instantiate(bouncerT.gameObject, sgo.transform);
                bgo.name = "Baarin portsari " + (i + 1);
                var be = bgo.GetComponent<Enemy>();
                be.wakeDistance = 100f; be.joinsFightWhenSquadComes = false; be.fightsEveryone = true;
                bgo.transform.position = new Vector3(doorX + (i - 2) * 0.9f, bar.MaxDepthAtX(doorX, bar.maxDepthY) - 0.5f - (i % 3) * 0.3f, 0f);
                bgo.SetActive(false);
                squad.bouncers[i] = be;
            }
            bouncerInfo = "5 portsaria tulee sisäovelta (3 + 2)";
        }
        var brawl = root.AddComponent<BarBrawl>();
        brawl.members = members.ToArray();
        brawl.area = bar; brawl.autoStartSeconds = 120f; brawl.squad = squad;
        // Elaine tanssii tanssilattialla Rekkamiehen kanssa, kunnes tappelu alkaa
        var elaineGo = GameObject.Find("Elaine"); var shaniceGo = GameObject.Find("Shanice");
        var elaineC = elaineGo != null ? elaineGo.GetComponent<BarElaine>() : null;
        var shaniceC = shaniceGo != null ? shaniceGo.GetComponent<BarElaine>() : null;
        if (elaineC != null)
        {
            Undo.RecordObject(elaineC, "Elaine tanssii"); Undo.RecordObject(elaineGo.transform, "Elaine tanssii");
            elaineGo.transform.position = new Vector3(X(3250f), -2.2f, 0f);
            elaineC.stayDancing = true; elaineC.brawl = brawl;
            EditorUtility.SetDirty(elaineC);
        }
        // Rekkamiehen puheet (Assets/Audio/rekkamies)
        if (tEnemy != null)
        {
            var oldV = tEnemy.GetComponent<RekkamiesVoice>();
            var voice = oldV != null ? oldV : Undo.AddComponent<RekkamiesVoice>(tEnemy.gameObject);
            Undo.RecordObject(voice, "Rekkamiehen puhe");
            voice.enemy = tEnemy; voice.area = null; voice.hearRange = 1000f; voice.brawl = brawl; voice.elaine = elaineC; voice.shanice = shaniceC;
            voice.speak = LoadClips("Assets/Audio/rekkamies", "puhe");
            voice.speakShanice = LoadClips("Assets/Audio/rekkamies", "shanice");
            voice.danceTalk = LoadClips("Assets/Audio/rekkamies", "tanssilattialla");
            EditorUtility.SetDirty(voice);
            names.Add("puheet: " + voice.speak.Length + " yleistä, " + voice.speakShanice.Length + " Shanicelle, " + voice.danceTalk.Length + " tanssilattialla");
        }
        AddBarLowerCrowdCore();   // alalattialle 3 motoristia ja 2 punkkaria (sama kuin valikko 127), jotta väki ei katoa tätä ajettaessa
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info("Aavikkobaarin väki: " + string.Join(", ", names) + ".\n" + shopInfo + ". " + bouncerInfo + ".\n"
             + "Kukaan ei aloita tappelua itse: se syttyy, kun joku saa osuman tai kun hero on ollut baarissa 2 minuuttia. Silloin kaikki heräävät ja portsarit tulevat.\n"
             + "Elaine tanssii tanssilattialla Rekkamiehen kanssa, kunnes tappelu alkaa. Shanice (114) kävelee ja juttelee ei-taistelijana.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/95. Aavikkokatu: kuusi kuvaa ja aavikon parallaxit")]
    public static void CreateDesertStreet()
    {
        if (PlayModeBlocked()) return;
        const string dir = "Assets/Sprites/Taustat/";
        var road = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Aavikkotie");
        // Kuvat 2–6: vasen reuna häivytetty (_pehmea), jolloin limitys sulautuu eikä liitos näy.
        string[] files = { "aavikkokatu1.png", "aavikkokatu2_pehmea.png", "aavikkokatu3_pehmea.png",
            "aavikkokadujatke1_pehmea.png", "aavikkokadujatke2_pehmea.png", "aavikkokadujatke3_pehmea.png",
            "aavikkokadujatke4_pehmea.png", "aavikkokadujatke5_pehmea.png", "aavikkokadujatke6_pehmea.png",
            "aavikkokadujatke7_pehmea.png", "aavikkokadujatke8_pehmea.png", "aavikkokadujatke9_pehmea.png" };   // 7 motelli, 8 puisto, 9 baari, 10 talli, 11 aita ja aseman alku, 12 juna-asema
        if (road == null || files.Any(f => AssetImporter.GetAtPath(dir + f) == null))
            throw new System.InvalidOperationException("Tarvitaan Aavikkotie (93), kolme aavikkokatukuvaa ja yhdeksän jatkokuvaa.");
        // Reunoissa on sama rakennus kahdesti: kohdista yhteiset osat limittäin.
        float ppu = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + files[0]).height / 12f;
        var sprites = files.Select(f => ImportShipSprite(dir + f, ppu, false, SpriteAlignment.Center)).ToArray();
        var backgrounds = new[] { "aavikko_parallax_ylempi_loop.png", "aavikko_parallax_alempi_loop.png" }   // saumattomasti toistuvat versiot
            .Select(f => ImportShipSprite(dir + f, 100f, true, SpriteAlignment.Center)).ToArray();
        if (backgrounds.Any(s => s == null))
            throw new System.InvalidOperationException("Aavikon parallax-spritet puuttuvat.");
        const float left = 86000f;
        float[] startPixels = { 0f, 1497f, 2917f, 4386f, 5896.992f, 7586.48064f, 9112.7f, 10421f, 11629.3f, 12905.15f, 14134.32f, 15268.71f };
        float[] imageScales = { 1f, 1f, 1f, 1.008f, 1.157184f, 1.085438592f, 1.0095f, 1.0095f, 1.0095f, 1.023366f, 1.026963f, 1.027588f };
        float[] curbRows = { 560f, 560f, 588f, 607f, 591f, 640f };
        // Kohdista jokaisen kuvan reunakiveys samaan 560 px linjaan myös mittakaavan muuttuessa.
        // Kuvat 7-9 (941 + 48 px peilattua lattiaa alareunassa, ettei alareunaan jää rakoa; korkeus 989 px; nostot laskettu mitatusta reunakiveyksen korkeudesta: kuvan 3 oikea reuna = kuvan 4 vasen reuna) kohdistettu kuvamittauksella jatkokuvan 3 oikeaan reunaan.
        float[] risePixels = Enumerable.Range(0, startPixels.Length).Select(i =>
            i < curbRows.Length ? (curbRows[i] - 443.5f) * imageScales[i] - (560f - 443.5f)
                : new[] { 63.5f, 64.0f, 80.7f, 126.60f, 127.37f, 135.23f }[i - curbRows.Length]).ToArray();   // kuvat 10–12 (talli, aita, asema): kohdistettu kuvamittauksella (SIFT) edelliseen kuvaan
        // Rajaa nostettujen kuvien alapuoliset aukot pois näkymästä.
        const float bottomCropPixels = 60f;
        float cameraSize = 6f - bottomCropPixels * 0.5f / ppu;
        int lastView = sprites.Length - 1;
        float width = startPixels[lastView] / ppu + sprites[lastView].bounds.size.x * imageScales[lastView], halfScreen = cameraSize * 16f / 9f;
        var area = ReuseArea("Alue: Aavikkokatu");
        area.areaName = "Aavikkokatu";
        // Katu laskettu kuvassa alemmas: kamera nousee, taivasta näkyy enemmän ja lattiaa (kävelytilaa) vähemmän.
        const float streetLowering = 1.0f;
        area.minDepthY = CamY - 6f + bottomCropPixels / ppu + 0.6f + streetLowering;
        // Kolmas kuva nostetaan samaan reunakiveyksen korkeuteen.
        area.useSidewalk = true;
        area.sidewalkHeight = 28f / ppu;
        area.curbDepthY = CamY + 6f - 560f / ppu;
        area.maxDepthY = CamY + 6f - 510f / ppu - area.sidewalkHeight;
        area.depthLimits = new Vector2[0];
        area.camMinX = left + halfScreen; area.camMaxX = left + width - halfScreen;
        area.walkMinX = left + 0.8f; area.walkMaxX = left + width - 0.8f;
        area.camSize = cameraSize; area.camRiseY = 0f; area.camOffsetY = bottomCropPixels * 0.5f / ppu + streetLowering; area.backScale = 1f;
        area.music = road.music; area.ambience = road.ambience; area.ambienceVolume = road.ambienceVolume;
        var old = GameObject.Find("Aavikkokadun taustat");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = new GameObject("Aavikkokadun taustat");
        Undo.RegisterCreatedObjectUndo(root, "Aavikkokatu");
        for (int i = 0; i < sprites.Length; i++)
        {
            var view = new GameObject("Aavikkokatu " + (i + 1)).AddComponent<SpriteRenderer>();
            view.transform.SetParent(root.transform, false);
            view.transform.position = new Vector3(left + startPixels[i] / ppu + sprites[i].bounds.size.x * imageScales[i] * 0.5f,
                CamY + risePixels[i] / ppu, 0f);
            view.transform.localScale = Vector3.one * imageScales[i];
            view.sprite = sprites[i]; view.sortingOrder = -10000 + i;
        }
        for (int i = 0; i < backgrounds.Length; i++)
        {
            var view = new GameObject(i == 0 ? "Aavikkokadun taivas ja vuoret" : "Aavikkokadun kaukainen hiekka").AddComponent<SpriteRenderer>();
            view.transform.SetParent(root.transform, false);
            view.transform.position = new Vector3(left + width * 0.5f, CamY + (i == 0 ? 5.8f : 2.2f), 0f);   // taivas ja mesat nostettu (5,8 yks.): mesat näkyvät rakennusten yläpuolella; kaukainen hiekka (horisontti n. +2,4) mesojen alla
            view.sprite = backgrounds[i]; view.sortingOrder = i == 0 ? -10200 : -10100;
            // kuvan suhteet säilytetään ja kuva toistuu vaakasuunnassa (saumaton), kerroksen leveys riittää parallax-liukuun
            float bgK = 12f / backgrounds[i].bounds.size.y * (i == 0 ? 1.15f : 1f);
            float bgFactor = i == 0 ? 0.04f : 0.08f;   // ylempi (mesat) hitaampi kuin alempi (hiekka)
            view.transform.localScale = new Vector3(bgK, bgK, 1f);
            view.drawMode = SpriteDrawMode.Tiled; view.tileMode = SpriteTileMode.Continuous;
            view.size = new Vector2((2f * halfScreen + (area.camMaxX - area.camMinX) * bgFactor + 4f) / bgK, backgrounds[i].bounds.size.y);
            var parallax = view.gameObject.AddComponent<ParallaxLayer>();
            parallax.factor = bgFactor; parallax.startCamX = area.camMinX;
            parallax.minX = left; parallax.maxX = left + width;
        }
        var exitGo = GameObject.Find("Aavikkotieltä Aavikkokadulle");
        if (exitGo == null) { exitGo = new GameObject("Aavikkotieltä Aavikkokadulle"); Undo.RegisterCreatedObjectUndo(exitGo, "Aavikkokadun ajosiirtymä"); }
        var exit = exitGo.GetComponent<RideExit>();
        if (exit == null) exit = Undo.AddComponent<RideExit>(exitGo);
        Undo.RecordObject(exit, "Aavikkokadun ajosiirtymä");
        exit.transform.position = new Vector3(road.walkMaxX - 1.7f, -2.4f, 0f);
        exit.here = road; exit.target = area;
        exit.spawnPoint = new Vector2(left + 6f, Mathf.Lerp(area.minDepthY, area.maxDepthY, 0.45f));
        exit.exitToLeft = false; exit.turnBikeRight = true; exit.parkBike = false;
        exit.title = "Aavikkokatu"; exit.titleTime = 0f; exit.fadeTime = 0.4f;
        exit.parkBike = true; exit.preventParkedRide = true;
        exit.parkPoint = new Vector2(left + 6f, area.maxDepthY + 0.1f);
        var weeds = area.GetComponent<DesertTumbleweeds>();
        if (weeds == null) weeds = Undo.AddComponent<DesertTumbleweeds>(area.gameObject);
        weeds.area = area;
        weeds.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Rekvisiitta/aavikko_risupallo.png");
        weeds.rollSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx/tumbleweed.wav");
        weeds.count = 6; weeds.minSpeed = 1.5f; weeds.maxSpeed = 4.8f;
        weeds.breakSprites = LoadTumbleweedBreakSprites();
        void ReturnExit(string name, Area here, Area target, Vector2 spawn, bool onFoot)
        {
            var go = GameObject.Find(name);
            if (go == null) { go = new GameObject(name); Undo.RegisterCreatedObjectUndo(go, name); }
            var back = go.GetComponent<RideExit>();
            if (back == null) back = Undo.AddComponent<RideExit>(go);
            Undo.RecordObject(back, name);
            back.here = here; back.target = target; back.spawnPoint = spawn;
            back.transform.position = new Vector3(here.walkMinX + 1.2f, spawn.y, 0f);
            back.exitToLeft = true; back.turnBikeLeft = true; back.turnBikeRight = false;
            back.returnOnFoot = onFoot; back.titleTime = 0f; back.title = target.areaName;
            EditorUtility.SetDirty(back);
        }
        ReturnExit("Aavikkokadulta takaisin tielle", area, road, new Vector2(road.walkMaxX - 6f, -2.4f), true);
        var desert = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Aavikko");
        if (desert != null)
            ReturnExit("Aavikkotieltä takaisin Aavikolle", road, desert, new Vector2(desert.walkMaxX - 6f, -2.4f), false);
        EditorUtility.SetDirty(weeds);
        var tires = area.GetComponent<DesertTires>();
        if (tires == null) tires = Undo.AddComponent<DesertTires>(area.gameObject);
        Undo.RecordObject(tires, "Aavikkokadun autonrenkaat");
        tires.area = area; tires.count = 5;
        tires.floorSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Rekvisiitta/autonrengas_maassa.png");
        tires.rollingSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Rekvisiitta/autonrengas_pyorii.png");
        tires.airSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Rekvisiitta/autonrengas_lento.png");
        EditorUtility.SetDirty(tires);
        var crateModel = GameObject.Find("Kopterin puulaatikko 1")?.GetComponent<Crate>();
        if (crateModel != null)
        {
            foreach (var existing in Object.FindObjectsByType<Crate>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (existing.name.StartsWith("Aavikkokadun puulaatikko ")) Undo.DestroyObjectImmediate(existing.gameObject);
            var oldCrates = GameObject.Find("Aavikkokadun puulaatikot");
            if (oldCrates != null) Undo.DestroyObjectImmediate(oldCrates);
            var crates = new GameObject("Aavikkokadun puulaatikot");
            Undo.RegisterCreatedObjectUndo(crates, "Aavikkokadun puulaatikot");
            var spots = new[] { new Vector2(1100f, 0.2f), new Vector2(1750f, 0.65f),
                new Vector2(2400f, 0.35f), new Vector2(3150f, 0.75f),
                new Vector2(3700f, 0.25f), new Vector2(4250f, 0.55f) };
            for (int i = 0; i < spots.Length; i++)
            {
                var clone = Object.Instantiate(crateModel.gameObject, crates.transform);
                clone.name = "Aavikkokadun puulaatikko " + (i + 1);
                clone.transform.position = new Vector3(left + spots[i].x / ppu,
                    Mathf.Lerp(area.maxDepthY - 0.03f, area.curbDepthY + 0.04f, spots[i].y), 0f);
                clone.SetActive(true);
            }
        }
        EditorUtility.SetDirty(area); EditorUtility.SetDirty(exit);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info("Aavikkokatu lisätty: yhdeksän kohdistettua kuvaa (loppuun motelli, puisto ja baarin etuosa), samat aavikon parallaxit ja ajosiirtymä Aavikkotien lopusta. Tallenna scene (Ctrl+S).");
    }

    public static void BuildDesertStreetBatch()
    {
        batch = true;
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        CreateDesertStreet();
        if (GameObject.Find("Aavikkokatu 6") == null) throw new System.InvalidOperationException("Aavikkokadun rakentaminen epäonnistui.");
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
    }

    public static void BuildDesertPairBatch()
    {
        batch = true;
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        BuildDesertPairs(true);
        if (GameObject.Find("Aavikkotien ajoparit") == null) throw new System.InvalidOperationException("Ajoparin rakentaminen epäonnistui.");
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
    }

    public static void BuildDesertBatch()
    {
        batch = true;
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        CreateDesert();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
    }

    static Area EnsureDesertArea()
    {
        var existing = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Aavikko");
        if (existing != null) return existing;
        const float left = 78000f;
        float halfW = CamHalf * 16f / 9f;
        var area = ReuseArea("Alue: Aavikko");
        area.areaName = "Aavikko"; area.useSidewalk = false;
        area.minDepthY = CamY - CamHalf + 0.6f; area.maxDepthY = CamY - 1.2f;
        area.camMinX = area.camMaxX = left + halfW;
        area.walkMinX = left + 0.8f; area.walkMaxX = left + halfW * 2f - 0.8f;
        // Väliaikainen tausta, kunnes Aavikon varsinainen kuva lisätään.
        var sprite = GetWhiteBlockSprite();
        MakeBlock("Aavikon väliaikainen taivas", sprite, new Vector3(left + halfW, CamY, 0f),
            new Vector3(halfW * 2f, CamHalf * 2f, 1f), new Color(0.65f, 0.78f, 0.9f), -10100);
        float bottom = CamY - CamHalf, groundHeight = area.maxDepthY - bottom;
        MakeBlock("Aavikon väliaikainen hiekka", sprite, new Vector3(left + halfW, bottom + groundHeight * 0.5f, 0f),
            new Vector3(halfW * 2f, groundHeight, 1f), new Color(0.72f, 0.56f, 0.32f), -10000);
        Undo.RegisterCreatedObjectUndo(GameObject.Find("Aavikon väliaikainen taivas"), "Aavikko");
        Undo.RegisterCreatedObjectUndo(GameObject.Find("Aavikon väliaikainen hiekka"), "Aavikko");
        EditorUtility.SetDirty(area);
        return area;
    }

    // ---------------- Puliukko (laivan käytävä) ----------------
    [MenuItem("Beat em up/61. Puliukko laivan käytävään (tulee hyttiovesta)")]
    static void AddDrunk()
    {
        var cor = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Laivan käytävä");
        var doorGo = GameObject.Find("Käytävän hyttiovi");
        if (cor == null) { Info("Tee ensin kohta 60 (laivan sisätilat)."); return; }
        var report = new List<string>();
        foreach (var n in new[] { "puliukko_idle", "puliukko_kavely", "puliukko_lyonti", "puliukko_lyonti2", "puliukko_kaatuminen", "puliukko_ylosnousu", "puliukko_heitetty", "puliukko_paapusku" })
        {
            string path = FindTexture(n);
            if (path != null) SetupAndSlice(path);
        }
        var old = GameObject.Find("Puliukot");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var idle = EnemySheet("puliukko_idle", report);
        if (idle.Length == 0) { Info("puliukko_idle.png puuttuu."); return; }
        var root = new GameObject("Puliukot");
        Undo.RegisterCreatedObjectUndo(root, "Puliukot");
        var go = new GameObject("Puliukko");
        go.transform.SetParent(root.transform, false);
        var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
        var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
        var e = go.AddComponent<Enemy>();
        e.body = visual; e.shadow = shadow; visual.sprite = idle[0];
        e.displayName = "Puliukko";
        e.idleSprites = idle; e.idleFrameTime = 0.16f;
        var walk = EnemySheet("puliukko_kavely", report);
        e.walkSprites = walk.Length > 0 ? walk : idle; e.walkFrameTime = 0.12f;   // hidas, huojuva kävely
        // läimäytys: veto korvan taakse, käsi ojennettuna (3–4), takakäsi takaisin (5) = toinen isku
        var slap = EnemySheet("puliukko_lyonti", report);
        if (slap.Length >= 8) { e.punchSprites = slap; e.punchImpactFrame = 3; e.secondImpactFrame = 5; e.windupTime = 0.3f; e.punchRecoverTime = 0.55f; e.punchDamage = 7; }
        var slap2 = EnemySheet("puliukko_lyonti2", report);   // toisella kädellä, kaataa
        if (slap2.Length >= 8) { e.altAttackSprites = slap2; e.altImpactFrame = 3; e.altDamage = 11; e.altKnockdown = true; e.altKnockdownLightOnly = true; e.altChance = 0.35f; e.altReach = 2.2f; }   // takakäsi kaataa kevyet
        var fall = EnemySheet("puliukko_kaatuminen", report);   // 0 asento, 1–3 horjuu, 4 ilmassa, 5 istuu, 6 jalat ilmassa, 7 makaa
        if (fall.Length >= 8)
        {
            e.hurtSprites = new[] { fall[1] };
            e.knockdownSprites = new[] { fall[1], fall[2], fall[3], fall[4], fall[7] };
            e.landSprites = new[] { fall[5], fall[6], fall[7] }; e.landFrameTime = 0.14f;
        }
        var getUp = EnemySheet("puliukko_ylosnousu", report);   // 10 kuvaa: makaa → istuu → polvelle → hoippuen pystyyn
        if (getUp.Length >= 10) { e.getUpSprites = getUp; e.getUpTime = 1.6f; }
        var thrown = EnemySheet("puliukko_heitetty", report);   // heron kuperkeikkaheitto: 0 asento, 1 napattu, 2 kierähdys, 3 lento, 4–7 alastulo ja makuu
        if (thrown.Length >= 8) { e.bigBody = true; e.flipThrownSprites = thrown; e.flipFlightFrames = 1; e.flipFlightFrameTime = 0.12f; e.flipLandFrameTime = 0.11f; }
        var butt = EnemySheet("puliukko_paapusku", report);     // erikoisliike: tarttuu, vetää, pääpusku (kuva 5)
        if (butt.Length >= 8)
        {
            e.punch3Sprites = butt; e.punch3ImpactFrame = 5; e.punch3WindupTime = 0.75f; e.punch3RecoverTime = 0.5f;
            e.punch3Damage = 26; e.punch3Knockdown = true; e.punch3Chance = 0.2f; e.punch3Reach = 2.1f;
        }
        e.moveSpeedX = 1.5f; e.moveSpeedY = 0.9f; e.runSpeedMultiplier = 1f;   // liikkuu hitaasti
        e.attackRange = 1.9f; e.attackCooldown = 1.5f; e.maxHealth = 207;   // portsaria (130) kestävämpi, hauska seura: kestää paljon
        // kombo: läimäytys, takakäden läimäytys ja pääpusku perään; menee loppuun
        e.combos = new[] { "JPK" }; e.comboChance = 0.3f; e.comboArmor = true; e.comboWindupScale = 0.8f; e.comboGap = 0.12f;
        e.fightsEveryone = true;   // käy kaikkien kimppuun (rosvot, seilorit, hero)
        e.hurtSounds = LoadClips("Assets/Audio/big thug", "gasp"); e.hurtVolume = 0.8f;
        e.wakeDistance = 100f; e.blockChance = 0f; e.retreatChance = 0f; e.flankChance = 0f;
        float doorX = doorGo != null ? doorGo.transform.position.x : (cor.camMinX + cor.camMaxX) * 0.5f;
        go.transform.position = new Vector3(doorX, cor.maxDepthY - 0.15f, 0f);
        go.SetActive(false);
        // tulee hyttiovesta, kun hero lähestyy
        var squad = root.AddComponent<BouncerSquad>();
        squad.area = cor; squad.bothSides = false; squad.firstDelay = 0.9f; squad.spawnInterval = 1f;
        squad.bouncers = new[] { e };
        squad.triggerX = doorX - 6f;
        if (doorGo != null) squad.door = doorGo.GetComponent<AnimatedDoor>();
        // toinen puliukko tepastelee kannella ja liittyy tappeluun, kun rosvot hyökkäävät
        var deck = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Laivan kansi");
        var deckBg = GameObject.Find("Laivan kansi");
        if (deck != null && deckBg != null)
        {
            var dsr = deckBg.GetComponent<SpriteRenderer>();
            float dppu = dsr.sprite.pixelsPerUnit, dleft = dsr.bounds.min.x;
            var d2 = Object.Instantiate(go, root.transform);
            d2.name = "Puliukko (kansi)";
            var e2 = d2.GetComponent<Enemy>();
            e2.wakeDistance = -1f;                         // ei herää lähestymisestä, vain tappelusta tai osumasta
            e2.joinsFightWhenSquadComes = true;
            e2.wanderMinX = dleft + 2650f / dppu; e2.wanderMaxX = dleft + 3400f / dppu;   // tanssipaikan oikealla puolella
            d2.transform.position = new Vector3((e2.wanderMinX + e2.wanderMaxX) * 0.5f, Mathf.Lerp(deck.maxDepthY, deck.minDepthY, 0.55f), 0f);
            d2.SetActive(true);
            report.Add("Kannella tepasteleva puliukko (liittyy rosvojen tappeluun)");
            // toinen kannen puliukko on heron puolella: lyö vain rosvoja (kuten seilorit), tepastelee baaritiskin luona
            var d4 = Object.Instantiate(go, root.transform);
            d4.name = "Puliukko (kansi, apuri)";
            var e4 = d4.GetComponent<Enemy>();
            e4.wakeDistance = -1f;
            e4.joinsFightWhenSquadComes = true;
            e4.fightsEveryone = false; e4.ally = true;
            e4.wanderMinX = dleft + 1700f / dppu; e4.wanderMaxX = dleft + 2500f / dppu;
            d4.transform.position = new Vector3(dleft + 2100f / dppu, Mathf.Lerp(deck.maxDepthY, deck.minDepthY, 0.4f), 0f);
            d4.SetActive(true);
            report.Add("Toinen kannen puliukko: heron apuri, lyö vain rosvoja");
        }
        // kolmas puliukko kävelee salissa juhlijoiden seassa (herää vasta, jos häneen osutaan)
        var salArea = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Laivan sali");
        var salBg = GameObject.Find("Laivan sali");
        if (salArea != null && salBg != null)
        {
            var ssr = salBg.GetComponent<SpriteRenderer>();
            float sppu = ssr.sprite.pixelsPerUnit, sleft = ssr.bounds.min.x, stop = ssr.bounds.max.y;
            var d3 = Object.Instantiate(go, root.transform);
            d3.name = "Puliukko (sali)";
            var e3 = d3.GetComponent<Enemy>();
            e3.wakeDistance = -1f;
            e3.wanderMinX = sleft + 800f / sppu; e3.wanderMaxX = sleft + 2100f / sppu;
            d3.transform.position = new Vector3(sleft + 1150f / sppu, stop - 640f / sppu, 0f);
            d3.SetActive(true);
            report.Add("Salissa tepasteleva puliukko");
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info("Puliukko:\n" + string.Join("\n", report) + "\n\nTulee käytävän hyttiovesta, kun hero lähestyy.\n\nTallenna scene (Ctrl+S).");
    }


    // ---------------- Laivan salin pöydät ----------------
    // salin kuvan x (px), syvyys 0 = takaraja … 1 = edessä; baarin ja lavan väliin ja eteen
    static readonly Vector2[] SalonTableSpots = { new Vector2(880f, 0.15f), new Vector2(1250f, 0.55f), new Vector2(2080f, 0.3f),
                                                     new Vector2(1050f, 0.95f), new Vector2(1750f, 0.85f), new Vector2(2400f, 0.9f),
                                                     new Vector2(480f, 0.62f), new Vector2(1400f, 0.99f) };   // kaksi viimeistä: kala-annos ja olutta   // lavan eteen ei pöytää

    [MenuItem("Beat em up/62. Laivan salin pöydät (hummeri ja shamppanja)")]
    static void AddSalonTables()
    {
        var sal = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Laivan sali");
        var salBg = GameObject.Find("Laivan sali");
        var tableSprite = ImportProp("Assets/Sprites/Rekvisiitta/sali_poyta.png");
        Sprite[] tableBreak = new Sprite[0];
        string tbp = FindTexture("poyta_sali");
        if (tbp != null) { SetupAndSlice(tbp); tableBreak = LoadSprites("poyta_sali").OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray(); }
        if (sal == null || salBg == null || tableSprite == null) { Info("Tarvitaan laivan sali (kohta 60) ja sali_poyta.png."); return; }
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Hummeri" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100; ti.alphaIsTransparency = true; ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            var st = new TextureImporterSettings(); ti.ReadTextureSettings(st);
            st.spriteAlignment = (int)(Path.GetFileNameWithoutExtension(path) == "annos" ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
            ti.SetTextureSettings(st);
            ti.SaveAndReimport();
        }
        Sprite[] Strip(string n)
        {
            string pth = FindTexture(n); if (pth == null) return new Sprite[0];
            SetupAndSlice(pth);
            return LoadSprites(n).OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
        }
        var champagne = Strip("pullo_shamppanja");          // 0 ehjä, 1–4 posahdus ilmassa, 5 sirpaleet lattialla
        var champEmpty = Strip("pullo_shamppanja_tyhja");   // 0 tyhjä (avattu), 1–4 posahdus, 5 sirpaleet
        var champSpin = Strip("pullo_shamppanja_pyorii");   // lento: 6 kulmaa (avattu)
        var champSpinFull = Strip("pullo_shamppanja_pyorii_taysi");   // lento: 6 kulmaa (avaamaton)
        var wineFull = Strip("pullo_lasi_shamppanja");         // 0 täysi lasi, 1 sirpaleet lattialla
        var wineEmpty = Strip("pullo_lasi_shamppanja_tyhja");  // 0 tyhjä lasi, 1 sirpaleet
        var wineSpin = Strip("pullo_lasi_shamppanja_pyorii");  // lento (tyhjä): 6 kulmaa
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Tahrat" }))
        {
            var ti = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.spritePixelsPerUnit = 100;
            ti.alphaIsTransparency = true; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }
        var glass = LoadClips("Assets/Audio/sfx", "glass");
        var plateSnd = LoadClips("Assets/Audio/sfx", "posliini");
        var beer = Strip("pullo_olut"); var pint = Strip("pullo_lasi_tuoppi");
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Kala" }))
        {
            var ti = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
            if (ti == null || ti.textureType == TextureImporterType.Sprite) continue;
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.spritePixelsPerUnit = 100;
            ti.alphaIsTransparency = true; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }
        var old = GameObject.Find("Salin pöydät");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = new GameObject("Salin pöydät");
        Undo.RegisterCreatedObjectUndo(root, "Salin pöydät");
        var bsr = salBg.GetComponent<SpriteRenderer>();
        float ppu = bsr.sprite.pixelsPerUnit, left = bsr.bounds.min.x;
        const float K = 1.5f;                   // pöydät ja tavarat 1,5 x (pöytä heron vyötärölle)
        const float sc = 0.64f * K;             // pöytä n. 3 yks leveä
        float top = 1.22f * K;                  // pöydän pinta (annos ja pullo)
        int n = 0;
        foreach (var v in SalonTableSpots)
        {
            float x = left + v.x / ppu;
            // takaraja pöydän koko leveydeltä (viisto seinä: oikea reuna on matalimmalla)
            float back = sal.maxDepthY;
            var dl = sal.depthLimits;
            if (dl != null && dl.Length > 1)
                foreach (float ex in new[] { x - 1.5f, x, x + 1.5f })
                    for (int i = 1; i < dl.Length; i++)
                        if (ex <= dl[i].x) { back = Mathf.Min(back, Mathf.Lerp(dl[i - 1].y, dl[i].y, Mathf.InverseLerp(dl[i - 1].x, dl[i].x, ex))); break; }
            float y = Mathf.Lerp(back - 0.3f, sal.minDepthY + 0.4f, v.y);   // 0 = takana, 1 = edessä
            var go = new GameObject("Salin pöytä " + (++n));
            go.transform.SetParent(root.transform, false);
            go.transform.position = new Vector3(x, y, 0f);
            var vis = new GameObject("Visual").AddComponent<SpriteRenderer>(); vis.transform.SetParent(go.transform, false);
            var sh = new GameObject("Shadow").AddComponent<SpriteRenderer>(); sh.transform.SetParent(go.transform, false);
            sh.color = new Color(0f, 0f, 0f, 0.35f);
            var c = go.AddComponent<Crate>();
            c.body = vis; c.shadow = sh; vis.sprite = tableSprite;
            c.sprites = new[] { tableSprite };
            c.breakable = false;
            float vsc = sc;
            if (tableBreak.Length >= 8)
            {
                // 0 ehjä, 1–2 halkeilee, 3–7 kaatuu ja hajoaa
                c.sprites = new[] { tableBreak[0], tableBreak[1], tableBreak[2] };
                c.breakSprites = new[] { tableBreak[3], tableBreak[4], tableBreak[5], tableBreak[6], tableBreak[7] };
                c.breakable = true; c.hitsToBreak = 3; c.breakFrameTime = 0.09f; c.debrisTime = 6f;
                vis.sprite = tableBreak[0]; vsc = sc * 316f / 304f;
            }
            c.visualScale = vsc;
            c.carryUpsideDown = true; c.carryLower = SalonTableCarryLower;   // kannetaan ylösalaisin pään päällä (kansi käsien varassa)
            c.shadowWidth = 2.0f * K; c.footOffset = 0.04f; c.plowThrough = true;   // heitettynä kaataa kaikki tieltään
            c.hitRadiusX = 1.0f * K; c.throwDamage = 20; c.moneyChance = 0f; c.energyChance = 0f;
            bool fishTable = n > SalonTableSpots.Length - 2;
            if (fishTable)
            {
                // kala-annos ja olutta (kuten El Loipparissa)
                var kGo = new GameObject("Kala-annos");
                kGo.transform.SetParent(root.transform, false); kGo.transform.position = go.transform.position;
                var kf = kGo.AddComponent<FishPlate>();
                kf.spriteSet = "Kala"; kf.scale = 0.72f * K; kf.table = c; kf.tableX = 0.2f * K; kf.tableTop = top; kf.breakSounds = plateSnd.Length > 0 ? plateSnd : glass;
                var bx = new[] { -0.55f, -0.8f, 0.85f };
                for (int bi = 0; bi < bx.Length; bi++)
                {
                    var sp = bi < 2 ? beer : pint;
                    if (sp.Length < 5) continue;
                    var bGo = new GameObject(bi < 2 ? "Olut" : "Tuoppi");
                    bGo.transform.SetParent(root.transform, false); bGo.transform.position = go.transform.position;
                    var b = bGo.AddComponent<Bottle>();
                    b.sprites = sp; b.stainKind = bi < 2 ? "olut" : "-"; b.scale = 1.05f * K; b.pivotY = bi < 2 ? 0.3f : 0.2f;
                    b.breakSounds = glass; b.table = c; b.tableX = bx[bi] * K; b.tableTop = top - 0.05f;
                }
                continue;
            }
            // hummeriannos ja shamppanjapullo pöydälle
            var fGo = new GameObject("Hummeriannos");
            fGo.transform.SetParent(root.transform, false); fGo.transform.position = go.transform.position;
            var f = fGo.AddComponent<FishPlate>();
            f.spriteSet = "Hummeri"; f.scale = 0.72f * K;
            f.table = c; f.tableX = (n % 2 == 0 ? 0.25f : -0.25f) * K; f.tableTop = top; f.breakSounds = plateSnd.Length > 0 ? plateSnd : glass;
            // täysi shamppanja (posahtaa) ja välillä tyhjä avattu pullo
            for (int bi = 0; bi < (n % 2 == 0 ? 2 : 1); bi++)
            {
                bool empty = bi == 1 && champEmpty.Length >= 2;
                var sp = empty ? champEmpty : champagne;
                if (sp.Length < 2) continue;
                var bGo = new GameObject(empty ? "Shamppanja (tyhjä)" : "Shamppanja");
                bGo.transform.SetParent(root.transform, false); bGo.transform.position = go.transform.position;
                var b = bGo.AddComponent<Bottle>();
                b.sprites = sp; b.keepDebris = true; b.frameTime = 0.06f;
                b.stainKind = empty ? "-" : "shamppanja"; b.scale = 1.0f * K; b.pivotY = 0.4f;
                if (empty && champSpin.Length >= 6) b.spinSprites = champSpin;
                if (!empty && champSpinFull.Length >= 6) b.spinSprites = champSpinFull;
                b.breakSounds = glass; b.table = c; b.tableX = ((n % 2 == 0 ? -0.55f : 0.55f) + bi * 0.3f) * K; b.tableTop = top - 0.05f;
            }
            // viinilasit: lähtevät pöydältä aina rikki (täysi kaatuu tyhjäksi lennossa)
            if (wineFull.Length >= 2 && wineEmpty.Length >= 2)
            {
                float side = n % 2 == 0 ? 1f : -1f;   // annoksen puolelle, reunalle
                var xs = n % 2 == 0 ? new[] { 0.85f * side } : new[] { 0.85f * side, -0.85f * side };
                for (int gi = 0; gi < xs.Length; gi++)
                {
                    bool fullGlass = (n + gi) % 2 == 0;
                    var gGo = new GameObject(fullGlass ? "Viinilasi (täysi)" : "Viinilasi");
                    gGo.transform.SetParent(root.transform, false); gGo.transform.position = go.transform.position;
                    var g = gGo.AddComponent<Bottle>();
                    g.sprites = fullGlass ? wineFull : wineEmpty; g.keepDebris = true; g.alwaysBreak = true;
                    if (wineSpin.Length >= 6) g.spinSprites = wineSpin;
                    g.stainKind = fullGlass ? "lasiroiske" : "-"; g.scale = 1f * K; g.pivotY = 0.28f; g.throwDamage = 8;
                    g.breakSounds = glass; g.table = c; g.tableX = xs[gi] * K; g.tableTop = top - 0.05f;
                }
            }
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info($"Saliin {n} pöytää: hummeriannos ja shamppanjapullo kullakin.\nLyönti pöytään: annos valuu lattialle. Potku tai heitetty pöytä: lautanen hajoaa ja hummeri lentää.\nShamppanjapullon voi ottaa ja heittää.\n\nTallenna scene (Ctrl+S).");
    }


    // ---------------- Salin naiset ja Sohvi ----------------
    const float SalonCounterTopPx = 345f, SalonSohviPx = 330f;

    [MenuItem("Beat em up/63. Salin naiset (kolme, keskustelevat ja käyvät Sohvin luona)")]
    static void AddSalonLadies()
    {
        var sal = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Laivan sali");
        var salBg = GameObject.Find("Laivan sali");
        if (sal == null || salBg == null) { Info("Tee ensin kohta 60 (laivan sisätilat)."); return; }
        Sprite[] Sh(string n)
        {
            string pth = FindTexture(n); if (pth == null) return new Sprite[0];
            SetupAndSlice(pth);
            var t2 = (TextureImporter)AssetImporter.GetAtPath(pth);
            t2.mipmapEnabled = true; t2.filterMode = FilterMode.Trilinear; t2.mipMapsPreserveCoverage = true; t2.SaveAndReimport();
            return LoadSprites(n).OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
        }
        foreach (var n in new[] { "Salin naiset", "Salin Sohvi", "Salin tiski" })
        {
            var o = GameObject.Find(n);
            if (o != null) Undo.DestroyObjectImmediate(o);
        }
        var bsr = salBg.GetComponent<SpriteRenderer>();
        float ppu = bsr.sprite.pixelsPerUnit, left = bsr.bounds.min.x, top = bsr.bounds.max.y;
        float X(float px) => left + px / ppu;
        float Y(float row) => top - row / ppu;
        // Sohvi baaritiskin taakse (kopio S-Clubin Sohvista)
        string sohviInfo = "Sohvi puuttuu (kohta 14)";
        var sohvi = GameObject.Find("Sohvi");
        Vector2 sohviPos = new Vector2(X(SalonSohviPx), Y(SalonCounterTopPx));
        if (sohvi != null)
        {
            var s2 = Object.Instantiate(sohvi);
            s2.name = "Salin Sohvi";
            s2.transform.position = new Vector3(sohviPos.x, sohviPos.y, 0f);
            s2.transform.localScale = sohvi.transform.localScale * 0.85f;
            Undo.RegisterCreatedObjectUndo(s2, "Sohvi");
            sohviInfo = "Sohvi baaritiskin takana";
        }
        // kauppa baaritiskin eteen (kopio S-Clubin tiskistä): tiski kuvan vasemmasta reunasta riville SalonBarEndPx
        var clubShop = GameObject.Find("Baaritiski");
        if (clubShop != null)
        {
            var sh = Object.Instantiate(clubShop);
            sh.name = "Salin tiski";
            sh.transform.position = new Vector3(X(SalonBarEndPx * 0.5f), Y(SalonBarFrontPx), 0f);
            var counter = sh.GetComponent<ShopCounter>();
            if (counter != null) { counter.here = sal; counter.halfWidth = SalonBarEndPx * 0.5f / ppu; counter.title = "SALONGIN BAARI"; }
            Undo.RegisterCreatedObjectUndo(sh, "Tiski");
            sohviInfo += ", kauppa tiskin edessä";
        }
        else sohviInfo += " (kauppa puuttuu: tee kohta 14)";
        var root = new GameObject("Salin naiset");
        Undo.RegisterCreatedObjectUndo(root, "Salin naiset");
        // mariachi-bändi lavalle (kopio)
        var band = GameObject.Find("Bändi");
        if (band != null)
        {
            var b = Object.Instantiate(band, root.transform);
            b.name = "Salin bändi";
            b.transform.position = new Vector3(X(1605f), Y(352f), 0f);
            b.transform.localScale = band.transform.localScale * 0.72f;
            var bsr2 = b.GetComponent<SpriteRenderer>(); if (bsr2 != null) { bsr2.color = new Color(0.95f, 0.9f, 0.88f); bsr2.sortingOrder = Mathf.RoundToInt(-Y(352f) * 100f); }
        }
        // yhteiset paikat: kolme baaritiskillä Sohvin edessä, keskustelupaikat lavan edessä ja salin keskellä (pöytien välissä)
        var spots = new[] {
            new Socialite.Spot { pos = new Vector2(X(380f), Y(502f)), atBar = true },
            new Socialite.Spot { pos = new Vector2(X(520f), Y(510f)), atBar = true },
            new Socialite.Spot { pos = new Vector2(X(660f), Y(502f)), atBar = true },
            new Socialite.Spot { pos = new Vector2(X(1460f), Y(500f)) },
            new Socialite.Spot { pos = new Vector2(X(1620f), Y(506f)) },
            new Socialite.Spot { pos = new Vector2(X(1540f), Y(570f)) },
            new Socialite.Spot { pos = new Vector2(X(1480f), Y(700f)) },
            new Socialite.Spot { pos = new Vector2(X(1640f), Y(690f)) },
        };
        int made = 0;
        void Lady(string name, string idleN, string walkN, bool facesRight, bool walkFacesRight, int start)
        {
            var idle = Sh(idleN); var walk = Sh(walkN);
            if (idle.Length == 0 || walk.Length == 0) return;
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            var r = go.AddComponent<SpriteRenderer>(); r.sprite = idle[0];
            var so = go.AddComponent<Socialite>();
            so.idle = idle; so.walk = walk; so.facesRight = facesRight; so.walkFacesRight = walkFacesRight;
            so.spots = spots; so.sohvi = sohviPos; so.startSpot = start;
            so.waitRange = new Vector2(6f + made, 12f + made * 2f);   // eri tahdissa
            go.transform.position = spots[start].pos;
            made++;
        }
        Lady("Nainen punainen", "salinainen_puna_idle", "salinainen_puna_kavely", true, true, 3);
        Lady("Nainen vihreä", "salinainen_vihrea_idle", "salinainen_vihrea_kavely", false, true, 4);
        Lady("Nainen kultainen", "salinainen_kulta_idle", "salinainen_kulta_kavely", true, true, 1);
        Lady("Mies puvussa", "salimies_idle", "salimies_kavely", true, false, 6);   // täytteeksi, sama logiikka
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info($"Saliin {made} juhlijaa (3 naista ja mies) ja {sohviInfo}.\nNaiset kiertelevät satunnaisesti: keskustelevat keskenään ja käyvät tiskillä juttelemassa Sohvin kanssa.\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Kadun baari (BAR-ovi) ----------------
    const string StreetBarPath = "Assets/Sprites/Taustat/baari_sisa.png";   // 3 kuvaa yhdistettynä: ovi ja tiski, sohvat ja tikkataulu, nurkka ja takaovi (3594 × 877)
    const float StreetBarX0 = 40000f;
    const float ChairScaleSmall = 0.72f;      // baarin ja pokerihuoneen tuolit (kuva on 75 % heron pituudesta: liian iso)
    const float StreetBarPPU = 105f;          // kuvapikseliä / yksikkö: tiski n. 70 % heron pituudesta (73 liian iso, 130 liian pieni)
    const string StreetBarExtPath = "Assets/Sprites/Taustat/baari_sisa_reuna.png";
    static readonly Vector2 StreetBarExtCanvas = new Vector2(150f, 1600f);   // baarin kuvan yläreuna kankaalla, kankaan korkeus (leveys sama kuin kuvalla)
    const float StreetBarDoorPx = 2263f;      // BAR-oven keskikohta kadun kuvasarjassa
    const float StreetBarExitPx = 160f;       // sisäkuvan vasen ovi (ulos kadulle)
    const float StreetBarCounterTopRow = 388f, StreetBarSohviPx = 800f;   // tiskin yläreuna ja Sohvin paikka (hanojen vasemmalla)
    static readonly Vector2 StreetBarCounterPx = new Vector2(1085f, 575f);  // tiskin keskikohta ja puolileveys (n. 510–1660)
    static readonly Vector2 StreetBarBackDoorPx = new Vector2(3297f, 557f);   // oikean nurkan teräsovi (pokerihuoneeseen): keskikohta ja alareuna
    // takaraja sisäkuvassa (x px, rivi): oven ja tynnyrin edusta, tiskin etureuna, sohvat seinällä, viisto oikea seinä
    static readonly Vector2[] StreetBarDepthPx = { new Vector2(0f, 612f), new Vector2(515f, 612f), new Vector2(530f, 640f), new Vector2(1712f, 640f),
                                                   new Vector2(1730f, 500f), new Vector2(2990f, 490f), new Vector2(3594f, 690f) };

    [MenuItem("Beat em up/65. Kadun baari (BAR-ovesta sisään)")]
    static void CreateStreetBar()
    {
        var street = GameObject.Find("Tausta");
        var streetArea = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Katu");
        var ti = AssetImporter.GetAtPath(StreetBarPath) as TextureImporter;
        if (street == null || streetArea == null || ti == null) { Info("Tarvitaan katutausta ja katualue (kohdat 4 ja 10) sekä " + StreetBarPath); return; }
        foreach (var n in new[] { "Kadun baari", "Kadun baarin ovet", "Kadun baarin Sohvi", "Kadun baarin tiski" })
        {
            var o = GameObject.Find(n);
            if (o != null) Undo.DestroyObjectImmediate(o);
        }
        ti.GetSourceTextureWidthAndHeight(out int w, out int h);
        float ppu = StreetBarPPU;   // pelin mittakaavassa (tiski n. 60 % heron pituudesta); kuvaa jatkettu ylös ja alas ruudun korkuiseksi
        ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = ppu; ti.filterMode = FilterMode.Bilinear;
        ti.textureCompression = TextureImporterCompression.Uncompressed; ti.maxTextureSize = 8192;
        ti.mipmapEnabled = false; ti.SaveAndReimport();
        var spr = AssetDatabase.LoadAssetAtPath<Sprite>(StreetBarPath);
        float wU = w / ppu, hU = h / ppu, halfW = CamHalf * 16f / 9f;
        var bg = new GameObject("Kadun baari");
        var sr = bg.AddComponent<SpriteRenderer>(); sr.sprite = spr; sr.sortingOrder = -10000;
        // jatkettu kangas (seinä ylös, lattia alas) keskitetty kameraan; baarin kuva sen yläosassa
        float extDy = (StreetBarExtCanvas.y * 0.5f - (StreetBarExtCanvas.x + h * 0.5f)) / ppu;
        bg.transform.position = new Vector3(StreetBarX0 + wU * 0.5f, CamY + extDy, 0f);
        Undo.RegisterCreatedObjectUndo(bg, "Kadun baari");
        var eti = AssetImporter.GetAtPath(StreetBarExtPath) as TextureImporter;
        if (eti != null)
        {
            eti.textureType = TextureImporterType.Sprite; eti.spriteImportMode = SpriteImportMode.Single;
            eti.spritePixelsPerUnit = ppu; eti.filterMode = FilterMode.Bilinear;
            eti.textureCompression = TextureImporterCompression.Uncompressed; eti.maxTextureSize = 4096; eti.mipmapEnabled = false;
            eti.SaveAndReimport();
            var eg = new GameObject("Reuna"); eg.transform.SetParent(bg.transform, false);
            eg.transform.localPosition = new Vector3(0f, -extDy, 0f);
            var esr = eg.AddComponent<SpriteRenderer>(); esr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(StreetBarExtPath); esr.sortingOrder = -10001;
        }
        float top = CamY + extDy + hU * 0.5f;
        float X(float px) => StreetBarX0 + px / ppu;
        float Y(float row) => top - row / ppu;

        var area = ReuseArea("Alue: Kadun baari");
        area.areaName = "Kadun baari";
        area.useSidewalk = false;
        area.maxDepthY = StreetBarDepthPx.Max(v => Y(v.y));
        area.minDepthY = Y(h + 260f);   // jatketulle lattialle asti
        area.camSize = 0f; area.camRiseY = 0f; area.camOffsetY = 0f;   // ei zoomia: hahmot samankokoisia kuin muualla
        area.camMinX = StreetBarX0 + halfW;
        area.camMaxX = Mathf.Max(area.camMinX, StreetBarX0 + wU - halfW);
        area.walkMinX = X(40f); area.walkMaxX = X(w - 60f);
        area.depthLimits = StreetBarDepthPx.Select(v => new Vector2(X(v.x), Y(v.y))).ToArray();

        // Sohvi tiskin taakse ja kauppa tiskin eteen (kopiot S-Clubin Sohvista ja baaritiskistä); oma baarimikko myöhemmin
        var clubSohvi = GameObject.Find("Sohvi");
        var clubShop = GameObject.Find("Baaritiski");
        if (clubSohvi != null)
        {
            var s2 = Object.Instantiate(clubSohvi);
            s2.name = "Kadun baarin Sohvi";
            s2.transform.position = new Vector3(X(StreetBarSohviPx), Y(StreetBarCounterTopRow), 0f);
            s2.transform.localScale = clubSohvi.transform.localScale * 0.85f;
            var s2sr = s2.GetComponent<SpriteRenderer>(); if (s2sr != null) s2sr.sortingOrder = -9500;   // taustan edessä, hahmojen takana
            Undo.RegisterCreatedObjectUndo(s2, "Sohvi");
        }
        if (clubShop != null)
        {
            var sh2 = Object.Instantiate(clubShop);
            sh2.name = "Kadun baarin tiski";
            sh2.transform.position = new Vector3(X(StreetBarCounterPx.x), area.MaxDepthAtX(X(StreetBarCounterPx.x), area.maxDepthY), 0f);
            var counter = sh2.GetComponent<ShopCounter>();
            if (counter != null) { counter.here = area; counter.halfWidth = StreetBarCounterPx.y / ppu; counter.title = "BAARI"; }
            Undo.RegisterCreatedObjectUndo(sh2, "Tiski");
        }

        // ovet: BAR-ovi jokaisessa kadun kuvasarjassa, ja paluuovi sisäkuvan vasemmassa ovessa
        var doors = new GameObject("Kadun baarin ovet");
        Undo.RegisterCreatedObjectUndo(doors, "Ovet");
        var ssr = street.GetComponent<SpriteRenderer>();
        float left = street.transform.position.x - ssr.size.x * 0.5f;
        float setU = StreetSetPx / BackgroundPPU;
        int count = Mathf.RoundToInt(ssr.size.x / setU);
        Vector2 inside = new Vector2(X(StreetBarExitPx + 160f), Mathf.Lerp(Y(612f), area.minDepthY, 0.3f));
        var animGo = GameObject.Find("Baarin ovi");
        var barDoorAnim = animGo != null ? animGo.GetComponent<AnimatedDoor>() : null;
        for (int i = 0; i < count; i++)
        {
            var d = new GameObject("BAR-ovi " + (i + 1)).AddComponent<Door>();
            d.transform.SetParent(doors.transform, false);
            d.transform.position = new Vector3(left + StreetBarDoorPx / BackgroundPPU + i * setU, streetArea.maxDepthY - 0.25f, 0f);
            d.prompt = "Mene baariin";
            d.here = streetArea; d.target = area; d.spawnPoint = inside;
            d.halfWidth = 1.4f; d.maxDistanceFromWall = 0.9f;
            if (i == 0 && barDoorAnim != null) { d.anim = barDoorAnim; d.animWait = 3.2f; }   // ovi aukeaa videon äänen tahdissa (kohta 66)
        }
        var exit = new GameObject("Baarista kadulle").AddComponent<Door>();
        exit.transform.SetParent(doors.transform, false);
        exit.transform.position = new Vector3(X(StreetBarExitPx), Y(612f), 0f);
        exit.prompt = "Ulos kadulle";
        exit.here = area; exit.target = streetArea;
        exit.returnToLastDoor = true;
        exit.halfWidth = 1.6f; exit.maxDistanceFromWall = 1.2f;
        exit.spawnPoint = new Vector2(left + StreetBarDoorPx / BackgroundPPU, streetArea.maxDepthY - 0.25f);
        EditorSceneManager.MarkSceneDirty(bg.scene);
        // pokerihuoneen ovi ja portsari (67) sekä kalusteet ja tappelijat (68) baarin mittojen mukaan
        if (!batch)
        {
            batch = true;
            try { CreateBarRoom(); AddStreetBarFurniture(); }
            finally { batch = false; }
        }
        Info($"Kadun baari luotu ({wU:0} yksikköä leveä): BAR-ovesta (E) sisään{(barDoorAnim != null ? ", ovi aukeaa" : " (avautuva ovi puuttuu: kohta 66)")}, vasemmasta ovesta ulos.\nTiski vasemmalla, sohvat ja tikkataulu keskellä, oikean nurkan teräsovesta pokerihuoneeseen (kohta 67).\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- S-Clubin prätkäjätkät ----------------
    static readonly Vector2[] ClubBikers = { new Vector2(18f, 0.45f), new Vector2(34f, 0.7f) };   // x S-Clubin vasemmasta reunasta (yks), syvyys 0 = takana … 1 = edessä
    static readonly Vector2[] ClubRockers = { new Vector2(21.5f, 0.55f), new Vector2(38f, 0.5f) };   // rokkimimmit (samat koordinaatit)
    static readonly Vector2 ClubDrunk = new Vector2(30f, 0.5f);   // puliukko

    /// Koviksen haymaker (iso heilautus): kolmas isku, pitkä veto, kaataa.
    static void SetKovisHaymaker(Enemy e, Sprite[] hay)
    {
        if (hay == null || hay.Length < 13) return;
        e.punch3Sprites = hay;
        e.punch3ImpactFrame = 11;        // 0–9 veto, 10 välikuva, 11 osuma, loput palautus
        e.punch3Damage = 22;
        e.punch3Knockdown = true;
        e.punch3LaunchUp = 9f; e.punch3LaunchX = 10f;   // pelaaja lentää korkealle ja kauas
        e.punch3Heavy = true;            // iso pysäytys, tärähdys ja kipinä, ei voi torjua
        // omat äänet: Assets/Audio/big thug/haymaker_lataus*.wav (veto) ja haymaker_isku*.wav (heilautus)
        e.punch3WindupSounds = LoadClips("Assets/Audio/big thug", "haymaker_lataus");
        e.punch3SwingSounds = LoadClips("Assets/Audio/big thug", "haymaker_isku");
        e.punch3Chance = 0.3f;
        e.punch3WindupTime = 0.9f;       // selvä varoitus: pelaaja ehtii väistää
        e.punch3RecoverTime = 0.6f;
        e.punch3Reach = 2.6f;            // syöksyy eteen
    }

    [MenuItem("Beat em up/75. Kolmas puliukko (apuri) kannen rosvojen viimeiseen aaltoon")]
    static void AddLastWaveDrunk()
    {
        var all = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var tmpl = all.FirstOrDefault(en => en != null && en.gameObject.name == "Puliukko (kansi, apuri)");
        var raidGo = GameObject.Find("Laivan rosvot");
        var raid = raidGo != null ? raidGo.GetComponent<BouncerSquad>() : null;
        if (tmpl == null || raid == null) { Info("Tee ensin kohdat 56 (laivan tappelu) ja 61 (puliukot)."); return; }
        foreach (var old in all.Where(en => en != null && en.gameObject.name == "Puliukko (kansi, apuri 2)").ToList())
            Undo.DestroyObjectImmediate(old.gameObject);
        // apurin kopio puliukkojen alle (ei rosvojen alle: muuten se laskettaisiin rosvoksi)
        var go = Object.Instantiate(tmpl.gameObject, tmpl.transform.parent);
        go.name = "Puliukko (kansi, apuri 2)";
        Undo.RegisterCreatedObjectUndo(go, go.name);
        var e = go.GetComponent<Enemy>();
        e.wakeDistance = -1f;
        e.joinsFightWhenSquadComes = false;   // tulee vasta viimeisessä aallossa
        e.ally = true; e.fightsEveryone = false;
        var deck = raid.area;
        if (deck != null) go.transform.position = new Vector3(tmpl.transform.position.x, Mathf.Lerp(deck.maxDepthY, deck.minDepthY, 0.7f), 0f);
        go.SetActive(false);
        raid.lastWaveHelpers = new[] { e };
        EditorUtility.SetDirty(raid);
        EditorSceneManager.MarkSceneDirty(raid.gameObject.scene);
        Info("Kolmas puliukko (heron apuri) lisätty: kävelee ruudun oikeasta reunasta sisään, kun kannen rosvojen viimeinen aalto alkaa, ja lyö vain rosvoja.\n\nTallenna scene (Ctrl+S).");
    }

    // salin pöytä ylösalaisin: kansi (kuvan alareuna käännettynä) käsien korkeudelle pään yläpuolelle (ennen 1,95: kansi rinnan kohdalla)
    const float SalonTableCarryLower = 0.4f;

    [MenuItem("Beat em up/76. Laivan salin pöydät: kanto pään yläpuolella")]
    static void SalonTablesCarryHigher()
    {
        int n = 0;
        foreach (var c in Object.FindObjectsByType<Crate>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c == null || !c.carryUpsideDown || !c.gameObject.name.StartsWith("Salin pöytä")) continue;
            Undo.RecordObject(c, "Salin pöydät");
            c.carryLower = SalonTableCarryLower;
            EditorUtility.SetDirty(c);
            n++;
        }
        if (n > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Info(n > 0 ? $"{n} salin pöytää: kannetaan nyt pään yläpuolella.\n\nTallenna scene (Ctrl+S)." : "Salin pöytiä ei löytynyt (tee ensin kohta 62).");
    }

    [MenuItem("Beat em up/72. Koviksen uusi kävely ja haymaker kaikille Koviksille")]
    static void KovisNewWalk()
    {
        string path = FindTexture("vihu_kavely");
        if (path == null) { Info("vihu_kavely.png puuttuu."); return; }
        SetupAndSlice(path);
        string hp = FindTexture("vihu_haymaker");
        if (hp != null) SetupAndSlice(hp);
        var report = new List<string>();
        var walk = EnemySheet("vihu_kavely", report);
        var hay = EnemySheet("vihu_haymaker", report);
        int n = 0;
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            // kaikki Koviksen kopiot (katu, sali, baari, pokerihuone): tunnistetaan vanhoista kävelykuvista
            if (e.walkSprites == null || e.walkSprites.Length == 0 || e.walkSprites[0] == null || e.walkSprites[0].texture == null) continue;
            if (e.walkSprites[0].texture.name != "vihu_kavely") continue;
            Undo.RecordObject(e, "Koviksen kävely");
            e.walkSprites = walk;
            e.walkFrameTime = 0.085f;   // 12 kuvaa, askelsykli ~1 s (sama tahti kuin ennen, nyt sulavampi)
            SetKovisHaymaker(e, hay);
            EditorUtility.SetDirty(e); n++;
        }
        if (n > 0) EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Info($"Koviksen kävely ja haymaker päivitetty {n} Kovikselle.\n\n" + string.Join("\n", report) + "\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/69. S-Clubiin prätkäjätkät, rokkimimmit ja puliukko (skeittarit pois sisältä)")]
    static void AddClubBikers()
    {
        var club = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "S-Club");
        if (club == null) { Info("Tee ensin kohta 10 (S-Club)."); return; }
        var old = GameObject.Find("S-Clubin prätkäjätkät");
        if (old != null) Undo.DestroyObjectImmediate(old);
        // lauta ei sovi sisätiloihin: skeittarit pois klubin alueelta
        float halfW = CamHalf * 16f / 9f, x0 = club.camMinX - halfW - 1f, x1 = club.camMaxX + halfW + 1f;
        int removed = 0;
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (e == null || !e.gameObject.name.StartsWith("Skettari")) continue;
            float x = e.transform.position.x;
            if (x >= x0 && x <= x1) { Undo.DestroyObjectImmediate(e.gameObject); removed++; }
        }
        var root = new GameObject("S-Clubin prätkäjätkät");
        Undo.RegisterCreatedObjectUndo(root, "S-Clubin prätkäjätkät");
        int made = 0;
        foreach (var v in ClubBikers)
        {
            var b = MakeBiker(root.transform);
            if (b == null) break;
            b.gameObject.name = "Prätkäjätkä (S-Club) " + (++made);
            b.transform.position = new Vector3(ClubX0 + v.x, Mathf.Lerp(club.maxDepthY - 0.3f, club.minDepthY + 0.4f, v.y), 0f);
            b.wakeDistance = 8f;
        }
        // puliukko (kaikkia vastaan) hoipertelee klubissa
        var drunkT = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(en => en != null && en.gameObject.name == "Puliukko");
        int drunks = 0;
        if (drunkT != null)
        {
            var dgo = Object.Instantiate(drunkT.gameObject, root.transform);
            dgo.name = "Puliukko (S-Club)"; drunks++;
            dgo.transform.position = new Vector3(ClubX0 + ClubDrunk.x, Mathf.Lerp(club.maxDepthY - 0.3f, club.minDepthY + 0.4f, ClubDrunk.y), 0f);
            var de = dgo.GetComponent<Enemy>(); de.wakeDistance = 7f; de.joinsFightWhenSquadComes = false;
            dgo.SetActive(true);
        }
        int rockers = 0;
        foreach (var v in ClubRockers)
        {
            var r = MakeRocker(root.transform);
            if (r == null) break;
            r.gameObject.name = "Rokkimimmi (S-Club) " + (++rockers);
            r.transform.position = new Vector3(ClubX0 + v.x, Mathf.Lerp(club.maxDepthY - 0.3f, club.minDepthY + 0.4f, v.y), 0f);
            // ei käy päälle itsestään (ApplyRocker2): tepastelee neutraalisti, kunnes lähellä syttyy tappelu
            r.wanderMinX = r.transform.position.x - 1.5f; r.wanderMaxX = r.transform.position.x + 1.5f;
            r.gameObject.SetActive(true);
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info($"S-Clubiin {made} prätkäjätkää, {rockers} rokkimimmiä ja {drunks} puliukko" + (removed > 0 ? $", {removed} skeittaria poistettu klubista" : "") + ".\nPrätkäjätkät tappelevat kaikkia vastaan.\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Kadun baarin pöydät, tuolit ja tappelijat ----------------
    // baarin kuvan pikseleinä: x ja syvyys 0 = takaraja … 1 = kuvan lattian alareuna
    static readonly Vector2[] StreetBarTables = { new Vector2(2200f, 1.8f), new Vector2(3050f, 1.8f),
                                                  // alaosa (lattia eturivissä, myös tiskin edessä)
                                                  new Vector2(760f, 1.22f), new Vector2(1420f, 1.25f), new Vector2(1300f, 2.15f) };
    // snookerpöydät (etujalkojen keskikohta) takarivissä sohvien edessä: niiden taakse ei mahdu muuta, pullopöydät edessä
    static readonly Vector2[] StreetBarSnooker = { new Vector2(2050f, 0.75f), new Vector2(2950f, 0.75f) };
    // kyltit baarin kuvan pikseleinä (keskikohta x, rivi; leveys px): Samperi's Snooker sohvien yläpuolelle, Poker night pokerihuoneen teräsoven yläpuolelle
    static readonly Vector3 StreetBarSnookerSign = new Vector3(1820f, 60f, 330f), StreetBarPokerSign = new Vector3(3300f, 110f, 150f);
    // kadulla (kadun kuvasarjan pikseleinä): Samperi's Snooker BAR-oven yläpuolelle parvekkeiden väliin, Poker night oven oikeaan ikkunaan
    // (kamera näyttää kadusta vasta n. rivistä 190 alaspäin: kyltti oven päälle BAR-valon kohdalle, peittää lampun ja BAR-kyltin)
    static readonly Vector3 StreetSnookerSign = new Vector3(2263f, 283f, 280f), StreetPokerSign = new Vector3(2543f, 400f, 92f);
    const float StreetBottomMargin = 0.4f;   // kävelyalueen alaraja kameran alareunan yläpuolella (oli 1,2: hahmo ei päässyt ruudun alaosaan)
    static readonly (string who, Vector2 at)[] StreetBarFighters = {
        ("Kovis", new Vector2(2080f, 0.15f)), ("Kovis", new Vector2(2500f, 1.1f)), ("Prätkäjätkä", new Vector2(2600f, 0.2f)),
        ("Punkkari", new Vector2(1650f, 0.8f)), ("Punkkari", new Vector2(3350f, 0.6f)),
        ("Lippis", new Vector2(2450f, 0.3f)), ("Lippis", new Vector2(1400f, 0.45f)), ("Kovis", new Vector2(3350f, 1.05f)), ("Punkkari", new Vector2(3400f, 1.3f)),
        // baarin perälle eteen: prätkäjätkä ja puliukko (molemmat kaikkia vastaan)
        ("Prätkäjätkä", new Vector2(3450f, 1.95f)), ("Puliukko", new Vector2(3420f, 1.55f)),
        ("Rokkimimmi", new Vector2(1250f, 0.02f)),
        // kaksi rokkimimmiä lisää tappelijoiksi (heräävät kuten muut)
        ("Rokkimimmi tappelija", new Vector2(1000f, 1.65f)), ("Rokkimimmi tappelija", new Vector2(2600f, 1.45f)),
        ("Prätkäjätkä", new Vector2(720f, 0.12f)) };   // tiskin lähellä: suutelee vakioasiakasta, kun hero tulee sisään   // tiskillä (neutraali), liittyy kun tappelu alkaa

    // baarin perällä snookerpöytien takana (sohvien edessä) seisoskeleva porukka: ei tepastele, liittyy tappeluun
    static readonly (string who, Vector2 at, int facing)[] StreetBarBackCrowd = {
        ("Lippis", new Vector2(1880f, 0.05f), 1), ("Punkkari", new Vector2(2230f, 0.08f), -1), ("Rokkimimmi tappelija", new Vector2(2380f, 0.04f), -1),
        ("Kovis", new Vector2(2780f, 0.06f), 1), ("Lippis", new Vector2(3120f, 0.05f), -1), ("Punkkari", new Vector2(2650f, 0.02f), 1) };

    /// Pullopöydät (6–8 pulloa ja lasia, ei annoksia) ja kaksi pientä tuolia kunkin päihin. Palauttaa pullot, lasit ja tuolit.
    static (int bottles, int glasses, int chairs) AddBottleTables(Transform root, IEnumerable<Vector3> positions, int seed, bool beerOnly = false, System.Func<int, float> plateX = null)
    {
        string tp = FindTexture("poyta");
        if (tp == null) return (0, 0, 0);
        SetupAndSlice(tp);
        Sprite[] Sheet(string n)
        {
            string bp = FindTexture(n); if (bp == null) return null;
            SetupAndSlice(bp);
            return LoadSprites(n).OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
        }
        var table = Sheet("poyta");
        var kinds = new List<(Sprite[] sp, string stain, float sc)>();
        foreach (var (n, st, sc) in new[] { ("pullo_olut", "olut", 1f), ("pullo_sininen", "sininen", 1.12f), ("pullo_likoori", "likoori", 1.25f), ("pullo_vodka", "vodka", 1.4f) })
        { var sp = Sheet(n); if (sp != null && sp.Length >= 7) kinds.Add((sp, st, sc)); }
        var glasses = new List<(Sprite[] sp, string stain, bool tall)>();
        foreach (var (n, st, tall) in new[] { ("pullo_lasi_tumbler", "-", false), ("pullo_lasi_viski", "likoori", false), ("pullo_lasi_olut", "olut", true), ("pullo_lasi_tuoppi", "-", true) })
        { var sp = Sheet(n); if (sp != null && sp.Length >= 5) glasses.Add((sp, st, tall)); }
        if (beerOnly) { kinds = kinds.Where(k => k.stain == "olut").ToList(); glasses.Clear(); }   // vain muutama olutpullo
        var glassSnd = LoadClips("Assets/Audio/sfx", "glass");
        var chairSprite = ImportProp("Assets/Sprites/Rekvisiitta/tuoli.png");
        var wood = AssetDatabase.FindAssets("t:AudioClip puu", new[] { "Assets/Audio" }).Select(AssetDatabase.GUIDToAssetPath)
            .Where(q => Path.GetFileNameWithoutExtension(q).ToLowerInvariant().StartsWith("puu")).Select(AssetDatabase.LoadAssetAtPath<AudioClip>).Where(clip => clip != null).ToArray();

        var rnd = new System.Random(seed);
        float tableTop = 1.28f * TableScale - 0.04f;
        int bottles = 0, glassN = 0, chairs = 0, tableIdx = 0;
        foreach (var pos in positions)
        {
            tableIdx++;
            var go = new GameObject("Pöytä");
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            var vis = new GameObject("Visual").AddComponent<SpriteRenderer>(); vis.transform.SetParent(go.transform, false);
            var sh = new GameObject("Shadow").AddComponent<SpriteRenderer>(); sh.transform.SetParent(go.transform, false);
            sh.color = new Color(0f, 0f, 0f, 0.35f);
            var c = go.AddComponent<Crate>();
            c.body = vis; c.shadow = sh;
            c.sprites = new[] { table[0], table[1], table[2] };
            c.breakSprites = new[] { table[3], table[4], table[5], table[6] };
            c.breakFrameTime = 0.1f; c.hitsToBreak = 3; c.breakable = true; c.footOffset = 0.04f;
            c.visualScale = TableScale; c.shadowWidth = 2.6f; c.carryLower = 0.86f * TableScale; c.plowThrough = true;
            c.hitRadiusX = 1.4f * TableScale; c.debrisTime = 6f;
            c.moneyChance = 0.3f; c.energyChance = 0.15f; c.throwDamage = 22;
            c.breakSounds = wood;
            vis.sprite = table[0];
            // pullopöytä (ei annosta): täynnä pulloja ja laseja; annospöytä (plateX antaa lautasen paikan): lasit, olut ja viinapullot lautasen vapaalla puolella.
            // Esineet pakataan riviin oikeilla leveyksillä (kuvan tiukka reuna) ja jaetaan tasavälein, joten ne eivät mene päällekkäin.
            float plateAt = plateX != null ? plateX(tableIdx) : float.NaN;
            bool dish = !float.IsNaN(plateAt);
            float xMin = -1.12f, xMax = 1.12f;
            if (dish) { if (plateAt > 0f) xMax = plateAt - 0.7f; else xMin = plateAt + 0.7f; }
            var cand = new List<(Sprite[] sp, string stain, float scl, bool glass, float pivot, float w)>();
            if (beerOnly)
            {
                // vanha asettelu: reunoille ja keskelle, annokset jäävät väleihin
                var slots0 = new List<float>(new[] { -1.12f, -0.1f, 0.1f, 1.12f });
                int items0 = 2 + rnd.Next(2);
                for (int i = 0; i < items0 && slots0.Count > 0 && kinds.Count > 0; i++)
                {
                    int si = rnd.Next(slots0.Count); float sx0 = slots0[si]; slots0.RemoveAt(si);
                    var k0 = kinds[rnd.Next(kinds.Count)];
                    var bGo0 = new GameObject("Pullo"); bGo0.transform.SetParent(root, false); bGo0.transform.position = go.transform.position;
                    var b0 = bGo0.AddComponent<Bottle>();
                    b0.sprites = k0.sp; b0.stainKind = k0.stain; b0.scale = 1.05f * k0.sc * (0.94f + 0.12f * (float)rnd.NextDouble()); bottles++;
                    b0.breakSounds = glassSnd; b0.table = c;
                    b0.tableX = (sx0 + (float)(rnd.NextDouble() - 0.5) * 0.08f) * TableScale; b0.tableTop = tableTop;
                }
            }
            else
            {
                float range = xMax - xMin, sumW = 0f;
                for (int guard = 0; guard < 40; guard++)
                {
                    bool wantGlass = glasses.Count > 0 && (kinds.Count == 0 || rnd.Next(100) < (dish ? 45 : 25));
                    Sprite[] spr; string stainK; float scl = 1f, pivot = 0.2f; bool isGlass = false;
                    if (wantGlass) { var gl = glasses[rnd.Next(glasses.Count)]; spr = gl.sp; stainK = gl.stain; pivot = gl.tall ? 0.2f : 0.15f; isGlass = true; }
                    else if (kinds.Count > 0) { var k = kinds[rnd.Next(kinds.Count)]; spr = k.sp; stainK = k.stain; scl = 1.05f * k.sc * (0.94f + 0.12f * (float)rnd.NextDouble()); }
                    else break;
                    float w = TightSpriteWidth(spr != null && spr.Length > 0 ? spr[0] : null) * (isGlass ? 1f : scl) / TableScale + 0.02f;
                    if (sumW + w > range) { if (cand.Count >= 3 || guard > 25) break; else continue; }
                    cand.Add((spr, stainK, scl, isGlass, pivot, w)); sumW += w;
                }
                float gap = (range - sumW) / (cand.Count + 1);
                float cx = xMin + gap;
                foreach (var it in cand)
                {
                    float sx = cx + it.w * 0.5f + (float)(rnd.NextDouble() - 0.5) * gap * 0.3f;
                    cx += it.w + gap;
                    var bGo = new GameObject(it.glass ? "Lasi" : "Pullo");
                    bGo.transform.SetParent(root, false);
                    bGo.transform.position = go.transform.position;
                    var b = bGo.AddComponent<Bottle>();
                    b.sprites = it.sp; b.stainKind = it.stain;
                    if (it.glass) { b.pivotY = it.pivot; glassN++; } else { b.scale = it.scl; bottles++; }
                    b.breakSounds = glassSnd; b.table = c;
                    b.tableX = sx * TableScale; b.tableTop = tableTop;
                }
            }
            // tuolit pöydän päihin, selkänoja ulospäin
            if (chairSprite != null)
                foreach (float side in new[] { -1f, 1f })
                {
                    var cgo = new GameObject("Tuoli");
                    cgo.transform.SetParent(root, false);
                    cgo.transform.position = go.transform.position + new Vector3(side * 2.1f * TableScale, -0.05f, 0f);
                    var cb = new GameObject("Visual").AddComponent<SpriteRenderer>(); cb.transform.SetParent(cgo.transform, false);
                    cb.sprite = chairSprite; cb.flipX = side > 0f; cb.transform.localScale = new Vector3(ChairScaleSmall, ChairScaleSmall, 1f);
                    var csh = new GameObject("Shadow").AddComponent<SpriteRenderer>(); csh.transform.SetParent(cgo.transform, false);
                    csh.color = new Color(0f, 0f, 0f, 0.3f);
                    var ch = cgo.AddComponent<Chair>(); ch.body = cb; ch.shadow = csh; ch.breakSounds = wood;
                    chairs++;
                }
        }

        return (bottles, glassN, chairs);
    }

    [MenuItem("Beat em up/68. Kadun baari: pöydät, snookerpöydät, kyltit ja tappelijat")]
    static void AddStreetBarFurniture()
    {
        var area = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Kadun baari");
        var barBg = GameObject.Find("Kadun baari");
        string tp = FindTexture("poyta");
        if (area == null || barBg == null || tp == null) { Info("Tee ensin kohta 65 (kadun baari). Tarvitaan myös poyta.png."); return; }
        var old = GameObject.Find("Kadun baarin kalusteet");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var bsr = barBg.GetComponent<SpriteRenderer>();
        float ppu = bsr.sprite.pixelsPerUnit, left = bsr.bounds.min.x, top = bsr.bounds.max.y;
        float X(float px) => left + px / ppu;
        float Depth(float px, float k) { float x = X(px); return Mathf.Lerp(area.MaxDepthAtX(x, area.maxDepthY) - 0.3f, top - 860f / ppu, k); }
        var root = new GameObject("Kadun baarin kalusteet");
        Undo.RegisterCreatedObjectUndo(root, "Kadun baarin kalusteet");
        var (bottles, glassN, chairs) = AddBottleTables(root.transform, StreetBarTables.Select(v => new Vector3(X(v.x), Depth(v.x, v.y), 0f)), 23);
        // snookerpöydät palloineen ja keppeineen (kuten pokerihuoneessa)
        int snooker = 0;
        foreach (var v in StreetBarSnooker) if (AddSnookerTable(root.transform, new Vector2(X(v.x), Depth(v.x, v.y)), PokerPoolWidth) != null) snooker++;
        // kyltit: sisällä seinillä, kadulla oven yläpuolella ja ikkunassa (jokaisessa kadun kuvasarjassa, kuten BAR-ovi)
        var signSnooker = ImportPropCentered("Assets/Sprites/Rekvisiitta/kyltti_samperis_snooker.png");
        var signPoker = ImportPropCentered("Assets/Sprites/Rekvisiitta/kyltti_poker_night.png");
        void Sign(Transform parent, Sprite sp, Vector2 at, float width, string signName, int order = -9995)
        {
            if (sp == null) return;
            var sgo = new GameObject(signName); sgo.transform.SetParent(parent, false);
            sgo.transform.position = new Vector3(at.x, at.y, 0f);
            float sc = width / sp.bounds.size.x; sgo.transform.localScale = new Vector3(sc, sc, 1f);
            var ssr0 = sgo.AddComponent<SpriteRenderer>(); ssr0.sprite = sp; ssr0.sortingOrder = order;   // taustan päällä, hahmojen takana
        }
        float Row(float row) => top - row / ppu;
        Sign(root.transform, signSnooker, new Vector2(X(StreetBarSnookerSign.x), Row(StreetBarSnookerSign.y)), StreetBarSnookerSign.z / ppu, "Kyltti: Samperi's Snooker");
        Sign(root.transform, signPoker, new Vector2(X(StreetBarPokerSign.x), Row(StreetBarPokerSign.y)), StreetBarPokerSign.z / ppu, "Kyltti: Poker night");
        var oldOut = GameObject.Find("Kadun baarin kyltit");
        if (oldOut != null) Undo.DestroyObjectImmediate(oldOut);
        var street = GameObject.Find("Tausta");
        int outSigns = 0;
        if (street != null)
        {
            var outRoot = new GameObject("Kadun baarin kyltit");
            Undo.RegisterCreatedObjectUndo(outRoot, "Kadun baarin kyltit");
            var ssr = street.GetComponent<SpriteRenderer>();
            float sLeft = ssr.bounds.min.x, sTop = ssr.bounds.max.y;
            int sets = Mathf.RoundToInt(ssr.size.x / (StreetSetPx / BackgroundPPU));
            for (int i = 0; i < sets; i++)
            {
                float ox = sLeft + i * StreetSetPx / BackgroundPPU;
                Sign(outRoot.transform, signSnooker, new Vector2(ox + StreetSnookerSign.x / BackgroundPPU, sTop - StreetSnookerSign.y / BackgroundPPU), StreetSnookerSign.z / BackgroundPPU, "Samperi's Snooker " + (i + 1), -9985);   // avautuvan oven (BAR-kyltti) päällä
                Sign(outRoot.transform, signPoker, new Vector2(ox + StreetPokerSign.x / BackgroundPPU, sTop - StreetPokerSign.y / BackgroundPPU), StreetPokerSign.z / BackgroundPPU, "Poker night (ikkuna) " + (i + 1));
                outSigns++;
            }
        }
        // kadulla hahmo pääsee lähemmäs ruudun alareunaa
        var pcs = Object.FindFirstObjectByType<PlayerController>();
        var streetA = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Katu");
        if (Camera.main != null)
        {
            float bottomY = Camera.main.transform.position.y - Camera.main.orthographicSize + StreetBottomMargin;
            if (pcs != null) { Undo.RecordObject(pcs, "Kadun alaraja"); pcs.minDepthY = bottomY; EditorUtility.SetDirty(pcs); }
            if (streetA != null) { Undo.RecordObject(streetA, "Kadun alaraja"); streetA.minDepthY = bottomY; EditorUtility.SetDirty(streetA); }
        }

        // tappelijat: Kovikset ja Punkkarit samaa porukkaa (eivät lyö toisiaan, käyvät prätkäjätkän kimppuun), prätkäjätkä kaikkia vastaan
        var all = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var members = new List<Enemy>();
        var names = new List<string>();
        foreach (var (who, at) in StreetBarFighters)
        {
            Enemy e;
            if (who == "Prätkäjätkä") e = MakeBiker(root.transform);
            else if (who == "Rokkimimmi tappelija") { e = MakeRocker(root.transform); if (e != null) e.huntsBrawlers = true; }
            else if (who == "Rokkimimmi")
            {
                // porukkaa: ei lyö Koviksia ja Punkkareita; odottaa tiskillä ja tulee mukaan vasta tappelun alkaessa
                e = MakeRocker(root.transform);
                if (e == null) continue;
                e.huntsBrawlers = true;
                e.transform.position = new Vector3(X(at.x), Depth(at.x, at.y), 0f);
                e.wakeDistance = -1f; e.joinsFightWhenSquadComes = false;
                e.gameObject.SetActive(true);
                members.Add(e); names.Add(who);
                continue;
            }
            else
            {
                var tmpl = all.FirstOrDefault(en => en != null && en.gameObject.name == who);
                if (tmpl == null) continue;
                var ego = Object.Instantiate(tmpl.gameObject, root.transform);
                ego.name = who + " (baari)";
                e = ego.GetComponent<Enemy>();
                e.huntsBrawlers = true;
            }
            if (e == null) continue;
            e.transform.position = new Vector3(X(at.x), Depth(at.x, at.y), 0f);
            e.wakeDistance = 6f; e.joinsFightWhenSquadComes = false;
            // ennen tappelua: tepastelevat baarin perän suuntaan ja katsovat sinne (eivät tuijota ovelle tulevaa)
            e.wanderMinX = X(at.x); e.wanderMaxX = X(Mathf.Min(at.x + 320f, 3450f)); e.idleFacing = 1;
            e.gameObject.SetActive(true);
            members.Add(e); names.Add(who);
        }
        // oven lähellä vakioasiakkaan kanssa suuteleva prätkäjätkä: pysyy paikallaan eikä herää läheisyydestä (herää tappelun alkaessa)
        var kisser = members.LastOrDefault(m => m != null && m.displayName == "Prätkäjätkä");
        if (kisser != null) { kisser.wanderMinX = kisser.wanderMaxX = 0f; kisser.wakeDistance = -1f; kisser.idleFacing = -1; }
        // perän porukka snookerpöytien takana
        foreach (var (who, at, facing) in StreetBarBackCrowd)
        {
            Enemy e;
            if (who == "Rokkimimmi tappelija") { e = MakeRocker(root.transform); if (e != null) e.huntsBrawlers = true; }
            else
            {
                var tmpl = all.FirstOrDefault(en => en != null && en.gameObject.name == who);
                if (tmpl == null) continue;
                var ego = Object.Instantiate(tmpl.gameObject, root.transform);
                ego.name = who + " (baarin perä)";
                e = ego.GetComponent<Enemy>();
                e.huntsBrawlers = true;
            }
            if (e == null) continue;
            e.transform.position = new Vector3(X(at.x), Depth(at.x, at.y), 0f);
            e.wakeDistance = 6f; e.joinsFightWhenSquadComes = false;
            e.wanderMinX = e.wanderMaxX = 0f; e.idleFacing = facing;   // seisoo paikallaan juttelemassa
            e.gameObject.SetActive(true);
            members.Add(e); names.Add(who + " (perä)");
        }
        var brawl = root.AddComponent<BarBrawl>();
        brawl.members = members.ToArray();
        // portsari vartioi teräsovella (kohta 67) ja tulee mukaan tappelun alkaessa
        var guardGo = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(en => en != null && en.gameObject.name == "Portsari (baarin vahti)");
        var pf = Object.FindFirstObjectByType<PokerFight>();
        if (guardGo != null) { brawl.guard = guardGo; if (pf != null) { brawl.guardFightIdle = pf.guardFightIdle; brawl.guardFightIdleFrameTime = pf.guardFightIdleFrameTime; } }
        // portsari tulee vasemmalta ulko-ovelta, kun tappelu alkaa
        var bouncerT = all.FirstOrDefault(en => en != null && en.gameObject.name == "Portsari");
        if (bouncerT != null)
        {
            var lgo = new GameObject("Baarin portsari (ovelta)");
            lgo.transform.SetParent(root.transform, false);
            var lc = lgo.AddComponent<Latecomers>();
            var bgo = Object.Instantiate(bouncerT.gameObject, lgo.transform);
            bgo.name = "Portsari (baari, ovelta)";
            var be = bgo.GetComponent<Enemy>(); be.wakeDistance = 100f; be.joinsFightWhenSquadComes = false;
            bgo.SetActive(false);
            lc.enemies = new[] { be };
            lc.entry = new Vector2(X(StreetBarExitPx + 140f), Depth(StreetBarExitPx + 140f, 0.35f));
            lc.firstDelay = 1.5f;
            brawl.latecomers = lc;
            names.Add("portsari vasemmalta");
        }
        // vakioasiakas (ei tappele): tiskillä, tupakalla ja suutelemassa prätkäjätkää; poistuu tappelun alkaessa
        Sprite[] NS(string n) { string p = FindTexture(n); if (p == null) return new Sprite[0]; SetupAndSlice(p); return LoadSprites(n).OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray(); }
        var nIdle = NS("baarinainen_tiski");
        if (nIdle.Length > 0)
        {
            var ngo = new GameObject("Baarin vakioasiakas");
            ngo.transform.SetParent(root.transform, false);
            var nsr = ngo.AddComponent<SpriteRenderer>(); nsr.sprite = nIdle[0];
            var br = ngo.AddComponent<BarRegular>();
            br.counterIdle = nIdle; br.turn = NS("baarinainen_kaanto"); br.smoking = NS("baarinainen_tupakka");
            br.walk = NS("baarinainen_kavely"); br.kiss = NS("baarinainen_suudelma");
            br.counterSpot = new Vector2(X(1500f), Depth(1500f, 0.02f));
            br.exitSpot = new Vector2(X(StreetBarExitPx), Depth(StreetBarExitPx, 0.05f));
            br.brawl = brawl;
            br.partner = members.LastOrDefault(m => m != null && m.displayName == "Prätkäjätkä");   // tiskin luona oleva
            var kgo = new GameObject("Suudelma"); kgo.transform.SetParent(root.transform, false);
            br.kissRenderer = kgo.AddComponent<SpriteRenderer>();
            ngo.transform.position = br.counterSpot;
            names.Add("vakioasiakas (ei tappele)");
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info($"Kadun baariin {StreetBarTables.Length} pöytää ({bottles} pulloa, {glassN} lasia), {chairs} tuolia ja {snooker} snookerpöytää.\nKyltit: sisällä Samperi's Snooker ja Poker night, kadulla {outSigns} oven yläpuolelle ja ikkunaan.\nTappelijat: {string.Join(", ", names)}." +
             (guardGo != null ? "\nPortsari tulee ovelta mukaan, kun tappelu alkaa." : "\nPortsari puuttuu: aja kohta 67.") + "\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Salin tappelu ----------------
    static readonly int[] SalonPirateWaves = { 5, 5 }, SalonSailorWaves = { 3, 3 };   // rosvoaalloissa 4 rosvoa + Kovis
    const float SalonFightTriggerPx = 900f;   // salin kuvan x: tästä eteenpäin tappelu alkaa

    [MenuItem("Beat em up/64. Salin tappelu: 8 rosvoa ja 2 Kovista (5 + 5), 6 seiloria (3 + 3)")]
    static void AddSalonFight()
    {
        var sal = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Laivan sali");
        var salBg = GameObject.Find("Laivan sali");
        if (sal == null || salBg == null) { Info("Tee ensin kohta 60 (laivan sisätilat)."); return; }
        var all = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var pirateT = all.FirstOrDefault(e => e.gameObject.name == "Rosvo 1");
        var sailorT = all.FirstOrDefault(e => e.gameObject.name == "Seilori");
        if (pirateT == null || sailorT == null) { Info("Tee ensin kohta 56 (laivan tappelu: rosvot ja seilorit)."); return; }
        foreach (var n in new[] { "Salin rosvot", "Salin seilorit" })
        {
            var o = GameObject.Find(n);
            if (o != null) Undo.DestroyObjectImmediate(o);
        }
        var bsr = salBg.GetComponent<SpriteRenderer>();
        float ppu = bsr.sprite.pixelsPerUnit, left = bsr.bounds.min.x, top = bsr.bounds.max.y;
        float X(float px) => left + px / ppu;
        float Y(float row) => top - row / ppu;
        float Depth(float x, float k) => Mathf.Lerp(sal.MaxDepthAtX(x, sal.maxDepthY) - 0.2f, sal.minDepthY + 0.3f, k);

        BouncerSquad Squad(string name, Enemy template, int[] waves, float[] depths, bool fromLeft)
        {
            var root = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(root, name);
            var sq = root.AddComponent<BouncerSquad>();
            sq.area = sal; sq.bothSides = false; sq.fromLeftEdge = fromLeft;
            sq.triggerX = X(SalonFightTriggerPx);
            sq.waves = waves; sq.waveNextAt = fromLeft ? 2 : 1;   // seuraava aalto, kun edellisestä on pystyssä enää 2 (seiloreilla 1)
            sq.firstDelay = fromLeft ? 0.6f : 1.4f; sq.spawnInterval = 0.6f;
            sq.bouncers = new Enemy[depths.Length];
            for (int i = 0; i < depths.Length; i++)
            {
                var c = Object.Instantiate(template.gameObject, root.transform);
                c.name = template.gameObject.name.Split(' ')[0] + " (sali) " + (i + 1);
                float x;
                if (fromLeft) x = X(60f);   // siirtyy ruudun vasemman reunan taakse ilmestyessään (käytävän ovelta)
                else
                {
                    // salin perältä, viiston oikean seinän sisäpuolelta (ruudun ulkopuolella, kun tappelu alkaa)
                    float y0 = Mathf.Lerp(Y(SalonRightBottomPx - 60f), sal.minDepthY + 0.3f, depths[i]);
                    float wallPx = Mathf.Lerp(SalonCornerPx, 2584f, Mathf.InverseLerp(Y(SalonWallPx + 12f), Y(SalonRightBottomPx), y0));
                    x = Mathf.Min(X(2540f), X(wallPx) - 0.8f);
                    c.transform.position = new Vector3(x, y0, 0f);
                }
                if (fromLeft) c.transform.position = new Vector3(x, Depth(x, depths[i]), 0f);
                var e = c.GetComponent<Enemy>(); e.wakeDistance = 7f;
                c.SetActive(false);
                sq.bouncers[i] = e;
            }
            return sq;
        }
        var raid = Squad("Salin rosvot", pirateT, SalonPirateWaves, new[] { 0.3f, 0.7f, 0.5f, 0.15f, 0.85f, 0.45f, 0.65f, 0.25f }, true);
        // pari Kovista rosvojen puolelle: kummankin aallon viimeiseksi
        var kovisT = all.FirstOrDefault(e => e != null && e.gameObject.name == "Kovis");   // all-listassa voi olla juuri poistettuja (vanhat salin rosvot)
        int kovis = 0;
        if (kovisT != null)
        {
            var list = raid.bouncers.ToList();
            for (int k = 0; k < 2; k++)
            {
                var kv = Object.Instantiate(kovisT.gameObject, raid.transform);
                kv.name = "Kovis (sali) " + (k + 1);
                kv.transform.position = new Vector3(X(60f), Depth(X(60f), k == 0 ? 0.55f : 0.35f), 0f);
                var ke = kv.GetComponent<Enemy>(); ke.wakeDistance = 7f;
                kv.SetActive(false);
                list.Insert(k == 0 ? 4 : 9, ke);
                kovis++;
            }
            raid.bouncers = list.ToArray();
        }
        else raid.waves = new[] { 4, 4 };
        Squad("Salin seilorit", sailorT, SalonSailorWaves, new[] { 0.2f, 0.55f, 0.9f, 0.4f, 0.75f, 0.1f }, false);
        EditorSceneManager.MarkSceneDirty(salBg.scene);
        Info($"Salin tappelu: kun hero kävelee salin keskelle, 8 rosvoa (+ {kovis} Kovista) tulee käytävän ovelta kahtena aaltona ja 6 seiloria salin perältä kahtena kolmen aaltona.\nNaiset ja mies väistävät tappelua.\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Heron iso koukku ----------------
    [MenuItem("Beat em up/59. Heron iso koukku (alas, eteen + lyönti)")]
    static void SetupBigHook()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        string p = FindTexture("koukku_iso");
        if (pc == null || p == null) { Info("Tarvitaan pelaaja ja koukku_iso.png."); return; }
        SetupAndSlice(p);
        Undo.RecordObject(pc, "Iso koukku");
        pc.bigHookSprites = LoadSprites("koukku_iso").OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
        EditorUtility.SetDirty(pc);
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        Info($"Iso koukku: {pc.bigHookSprites.Length} kuvaa.\nAlas, eteen + lyönti. Kovis, samoalainen ja portsari kaatuvat, muut lentävät korkealle.\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Pelastusrenkaat ----------------
    // kannen kuvan x (px), syvyys 0 = kaide … 1 = edessä
    static readonly Vector2[] RingStandSpots = { new Vector2(650f, 0.15f), new Vector2(1450f, 0.15f) };   // keulan puolella, ei aurinkotuolien eteen
    // Uccopulcon satamakuvan x (px), kaiteen vieressä (ei laivan aukon kohdalla)
    static readonly float[] UccoRingStandPx = { 900f, 2250f };

    [MenuItem("Beat em up/58. Pelastusrenkaat: telineet laivalle ja satamaan, lyönti, heitto, skeittari renkaassa")]
    static void AddRingStands()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        var deck = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Laivan kansi");
        var deckBg = GameObject.Find("Laivan kansi");
        var ucco = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Uccopulco");
        var harbour = GameObject.Find("Uccopulco satama");
        if (pc == null) { Info("Scenessä ei ole pelaajaa."); return; }
        Sprite[] Sh(string n)
        {
            string p = FindTexture(n); if (p == null) return new Sprite[0];
            SetupAndSlice(p);
            return LoadSprites(n).OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
        }
        Undo.RecordObject(pc, "Renkaan kuvat");
        pc.ringTakeSprites = Sh("rengas_otto");
        pc.ringThrowSprites = Sh("rengas_heitto");
        pc.ringPickSprites = Sh("rengas_nosto");
        pc.ringSmashSprites = Sh("rengas_lyonti");
        pc.ringWalkSprites = Sh("rengas_kavely");
        pc.ringIdleSprites = Sh("rengas_idle");
        EditorUtility.SetDirty(pc);
        // skeittari renkaassa (muille vihuille rengas-iskun kaato)
        var ringed = Sh("rengas_skettari");
        var ringedSailor = Sh("rengas_seilori");
        int nsk = 0, nse = 0;
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            bool sailor = e.idleSprites != null && e.idleSprites.Any(x => x != null && x.name.StartsWith("seilori"));
            if (sailor && ringedSailor.Length >= 8) { Undo.RecordObject(e, "Renkaan kuvat"); e.ringedSprites = ringedSailor; EditorUtility.SetDirty(e); nse++; continue; }
            bool skater = e.looseBoardSprite != null || (e.footIdleSprites != null && e.footIdleSprites.Any(x => x != null && x.name.StartsWith("skettari")));
            if (!skater || ringed.Length < 8) continue;
            Undo.RecordObject(e, "Renkaan kuvat");
            e.ringedSprites = ringed; EditorUtility.SetDirty(e); nsk++;
        }
        var ring = Sh("rengas_kuvat");
        var breakSprites = Sh("rengas_teline_hajoaa");
        var full = ImportProp("Assets/Sprites/Rekvisiitta/rengas_teline.png");
        var empty = ImportProp("Assets/Sprites/Rekvisiitta/rengas_teline_tyhja.png");
        var wood = AssetDatabase.FindAssets("t:AudioClip puu", new[] { "Assets/Audio" }).Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => Path.GetFileNameWithoutExtension(p).ToLowerInvariant().StartsWith("puu")).Select(AssetDatabase.LoadAssetAtPath<AudioClip>).Where(c => c != null).ToArray();
        var old = GameObject.Find("Pelastusrenkaat");
        if (old != null) Undo.DestroyObjectImmediate(old);
        if (full == null || empty == null || ring.Length < 9) { Info("Renkaan kuvia puuttuu (rengas_teline, rengas_teline_tyhja, rengas_kuvat)."); return; }
        var root = new GameObject("Pelastusrenkaat");
        Undo.RegisterCreatedObjectUndo(root, "Pelastusrenkaat");
        int n = 0;
        RingStand Make(Vector3 pos, float lift)
        {
            var g = new GameObject("Rengasteline " + (++n));
            g.transform.SetParent(root.transform);
            g.transform.position = pos;
            var vis = new GameObject("Visual").AddComponent<SpriteRenderer>();
            vis.transform.SetParent(g.transform, false);
            vis.transform.localPosition = new Vector3(0f, 0.11f + lift, 0f);   // telineen jalat samalle tasolle kuin heron jalat
            vis.sprite = full;
            var st = g.AddComponent<RingStand>();
            st.body = vis; st.withRing = full; st.empty = empty; st.ringSprites = ring;
            st.breakSprites = breakSprites; st.breakSounds = wood;
            return st;
        }
        int nDeck = 0, nUcco = 0;
        if (deck != null && deckBg != null)
        {
            var bsr = deckBg.GetComponent<SpriteRenderer>();
            float ppu = bsr.sprite.pixelsPerUnit, left = bsr.bounds.min.x;
            var made = new List<SpriteRenderer>();
            foreach (var v in RingStandSpots)
            {
                var st = Make(new Vector3(left + v.x / ppu, Mathf.Lerp(deck.maxDepthY - 0.3f, deck.minDepthY + 0.4f, v.y), 0f), 0f);
                made.Add(st.body); nDeck++;
            }
            var night = deckBg.GetComponent<ShipNight>();
            if (night != null) night.tinted = (night.tinted ?? new SpriteRenderer[0]).Where(r => r != null).Concat(made).ToArray();
        }
        if (ucco != null && harbour != null)
        {
            var hsr = harbour.GetComponent<SpriteRenderer>();
            float ppu = hsr.sprite.pixelsPerUnit, left = hsr.bounds.min.x;
            foreach (float px in UccoRingStandPx)
            {
                // jalkakäytävällä kaiteen vieressä (jalkakäytävän korotus kuvaan)
                Make(new Vector3(left + px / ppu, ucco.maxDepthY - 0.1f, 0f), ucco.useSidewalk ? ucco.sidewalkHeight : 0f);
                nUcco++;
            }
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info($"Pelastusrenkaat: {nDeck} telinettä laivan kannelle, {nUcco} Uccopulcon satamaan.\nRenkaan kuvat: {nsk} skeittaria, {nse} seiloria.\n\n" +
             "Kiinniotto telineen vieressä: ota rengas. Lyönti: rengas vihun päähän. Potku/kiinniotto: heitto.\nLattialta rengas nostetaan kiinniottonapilla. Teline hajoaa iskuista.\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Laivan tappelu: seilorit ja rosvot ----------------
    const int SailorCount = 7;                         // 4 heti, 3 lisää myöhemmin
    static readonly int[] SailorWaves = { 4, 3 };
    // rosvot (8) hyökkäävät keulan puolelta aaltoina, kun hero tulee hyttiovelle; tässä vain syvyys (0 = kaide … 1 = edessä)
    static readonly float[] ShipPirateDepths = { 0.3f, 0.7f, 0.5f, 0.2f, 0.85f, 0.45f, 0.65f, 0.25f, 0.4f, 0.75f, 0.15f, 0.6f };
    static readonly int[] ShipPirateWaves = { 4, 5, 5 };   // 2. ja 3. aallossa mukana Kovis
    const int ShipKovisCount = 2;
    const float ShipFightTriggerPx = 3550f;   // kannen kuvan x: tästä eteenpäin (hyttiovelle) tappelu alkaa

    // baarin ovi: videosta kohdistettu avautumissarja katu_sarja.png:n päälle (katu_bar_ovi.png, 59 kuvaa 360 × 600, 15 fps).
    // Solun alareunan keskikohta katu_sarja.png:n pikseleissä (x vasemmalta, y ylhäältä). Ääni baarin_ovi.wav on kuvien tahdissa.
    const float BarDoorCenterPx = 2260f, BarDoorBottomPx = 900f, BarDoorFps = 15f;
    const string BarDoorSoundPath = "Assets/Audio/sfx/baarin_ovi.wav";

    [MenuItem("Beat em up/66. Baarin avautuva ovi (kadulla)")]
    static void AddBarDoor()
    {
        var street = GameObject.Find("Tausta");
        string p = FindTexture("katu_bar_ovi");
        if (street == null || p == null) { Info("Tarvitaan katutausta (kohta 4) ja kuva katu_bar_ovi.png"); return; }
        var old = GameObject.Find("Baarin ovi");
        if (old != null) Undo.DestroyObjectImmediate(old);
        SetupAndSlice(p);
        var frames = LoadSprites("katu_bar_ovi").OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
        if (frames.Length < 2) { Info("katu_bar_ovi.png: kuvia ei saatu leikattua (solu 360 × 600)."); return; }
        var bsr = street.GetComponent<SpriteRenderer>();
        float left = bsr.bounds.min.x, top = bsr.bounds.max.y;
        var go = new GameObject("Baarin ovi");
        // kuvat on tuotu 100 px/yks, katu 52 px/yks: skaalataan kadun kokoon; tukipiste alareunan keskellä
        float k100 = frames[0].pixelsPerUnit / BackgroundPPU;
        go.transform.position = new Vector3(left + BarDoorCenterPx / BackgroundPPU, top - BarDoorBottomPx / BackgroundPPU, 0f);
        go.transform.localScale = new Vector3(k100, k100, 1f);
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = frames[0]; sr.sortingOrder = -9990;   // taustan päällä, hahmojen alla
        var d = go.AddComponent<AnimatedDoor>(); d.body = sr; d.frames = frames; d.frameTime = 1f / BarDoorFps; d.linear = true;
        d.openSound = AssetDatabase.LoadAssetAtPath<AudioClip>(BarDoorSoundPath);
        Undo.RegisterCreatedObjectUndo(go, "Baarin ovi");
        Info($"Baarin ovi: {frames.Length} kuvaa ({(frames.Length - 1) / BarDoorFps:0.0} s), ääni {(d.openSound != null ? "baarin_ovi.wav" : "PUUTTUU")}.\n" +
             "Testaus: Play-tilassa valitse 'Baarin ovi' → Inspectorin AnimatedDoor ⋮ → Avaa.\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Baarin sisätila (pokerihuone) ----------------
    // baari_pokeri.png (1536 × 1024) koko ruudun levyiseksi, kamera paikallaan. Pokerinpelaajien idle videosta:
    // baari_poker_idle.png (33 kuvaa) ja baari_poker_idle2.png (36 kuvaa), 896 × 504, 12 fps, saumat ristihäivytetty,
    // yhteinen maski; solun vasen yläkulma kuvassa (392, 232).
    const string BarRoomPath = "Assets/Sprites/Taustat/baari_pokeri.png";
    const float PokerRoomPPU = 139.5f; // pokerihuoneen mittakaava: huone ja videon hahmot 10 % isompia kuin 153.4:llä (ennen 118 = 1.3 × hero); kuva 3299 px = 23.6 yks
    // alkuperäinen huone (1536 × 1024) laajennetun kuvan sisällä (ChatGPT-laajennus, alkuperäinen liitetty takaisin): vasen yläkulma
    static readonly Vector2 PokerRoomOffset = new Vector2(848f, 311f);   // uusi laajennus sivuseinineen (alkuperäinen huone liitetty takaisin)
    // pullopöydät huoneen koordinaateissa (x, rivi): vasen ja oikea laajennus sekä lattia edessä
    // (tuolit n. 320 px pöydän keskeltä: pöydät sijoitettu niin, että tuolit mahtuvat kuvaan eivätkä mene päällekkäin)
    // (tappelu alkaa takana pokeripöydän luona: pöydät lähellä sitä, sivuilla ja vähän edessä)
    static readonly Vector2[] PokerTablePx = { new Vector2(-220f, 800f), new Vector2(1760f, 820f), new Vector2(240f, 1080f), new Vector2(1360f, 1090f) };
    // sivuseinien jalat (huoneen koordinaatit): vasen seinä kulmasta kuvan reunaan, samoin oikea
    static readonly Vector2 PokerPoolPx = new Vector2(768f, 1560f);   // biljardipöydän etujalkojen keskikohta (huoneen koordinaatit)
    static readonly Vector2[] PokerKovisPx = { new Vector2(250f, 1250f), new Vector2(1350f, 1300f) };   // kaksi Kovista etuosassa (huoneen koordinaatit)
    const float PokerPoolWidth = 6.0f;                                  // yksikköä: selvästi pullopöytiä isompi (snooker)
    static readonly Vector2 PokerLeftWallEdge = new Vector2(-850f, 730f), PokerLeftWallCorner = new Vector2(-420f, 545f);
    static readonly Vector2 PokerRightWallCorner = new Vector2(2020f, 545f), PokerRightWallEdge = new Vector2(2451f, 780f);
    // jatkettu tausta (huone keskellä, lattiaa alas, reunat tummuvat): kankaan koko ja huoneen vasen yläkulma kankaalla
    const float BarRoomX0 = 50000f;
    const float BarRoomWallRow = 545f;       // seinän alareuna (ovi vasemmalla)
    const float BarRoomTableRow = 740f;      // pöydän, tuolien ja maton etureuna: tätä taemmas ei kävellä keskellä
    const float BarRoomCabinetRow = 640f;    // oikean reunan lipaston juuri
    const float BarRoomExitPx = 205f;        // teräsovi vasemmalla (ulos kadulle)
    const string PokerAmbiencePath = "Assets/Audio/sfx/baari_pokeri_ambienssi.wav";   // toisen idle-videon ääni, saumaton silmukka
    const float PokerCellX = 392f, PokerCellY = 232f, PokerCellW = 896f, PokerCellH = 504f, PokerFps = 12f;
    // loppuanimaatio baari_poker_loppu.png: 61 kuvaa 854 × 506 (pienennetty kuvan alueesta x, y, leveys, korkeus), 12 fps, ääni videosta
    static readonly Vector4 PokerFinaleRect = new Vector4(52f, 0f, 1421f, 842f);
    const string PokerFinaleSoundPath = "Assets/Audio/sfx/baari_pokeri_loppu.wav";
    // tappelu: tyhjä huone (sama rajaus), tuolit ja vastustajien jalat kuvan pikseleinä (x, rivi)
    const string PokerFightBgPath = "Assets/Sprites/Taustat/baari_pokeri_tappelu.png";
    static readonly Vector2[] PokerChairPx = { new Vector2(200f, 700f), new Vector2(470f, 590f), new Vector2(1100f, 590f), new Vector2(1380f, 730f) };
    static readonly Vector2 PokerBikerPx = new Vector2(980f, 600f);
    static readonly Vector2 PokerLippisPx = new Vector2(353f, 720f), PokerPuliukkoPx = new Vector2(613f, 613f), PokerPunkkariPx = new Vector2(1227f, 707f);

    /// Prätkäjätkä (motoristi_*.png): tappeluasento, kävely, jab, pitkä suora ja korkea potku. Ilman omia osuma- ja kaatumiskuvia
    /// käytetään varaliikkeitä (välähdys, kuvan kääntö). Palauttaa null, jos idle-kuvat puuttuvat.
    /// Prätkäjätkän rynnäkkölyönti hyppylyöntikuvista (15 kuvaa).
    static void SetBikerCharge(Enemy e, Sprite[] leap)
    {
        if (leap == null || leap.Length < 15) return;
        // rynnäkkölyönti: ei pitkää kyykkyä eikä korkeaa loikkaa (näytti koomiselta), vaan kaksi juoksuaskelta,
        // matala syöksy nyrkki edellä ja tasapainoinen alastulo (kuvat 3, 5–8 syöksy, 11–14 alastulo)
        e.altAttackSprites = new[] { leap[3], leap[5], leap[6], leap[7], leap[8], leap[11], leap[12], leap[13], leap[14] };
        e.altImpactFrame = 4; e.altDamage = 20; e.altKnockdown = true; e.altReach = 2.3f;
        e.altChance = 0.3f; e.altNearChance = 0.1f;
        e.chargeRange = 7.5f; e.chargeMinRange = 3.0f;
        e.altLungeSpeed = 14f; e.altLungeTime = 0.38f; e.altJumpHeight = 0.3f;
        e.altExtraWindup = 0.12f; e.altTimeScale = 0.85f; e.altKnockSpeed = 9f; e.altKnockUp = 6f;
    }

    [MenuItem("Beat em up/78. Prätkäjätkän uusi rynnäkkölyönti kaikille prätkäjätkille")]
    static void UpdateBikerCharge()
    {
        var report = new List<string>();
        string path = FindTexture("motoristi_hyppylyonti");
        if (path != null) SetupAndSlice(path);
        var leap = EnemySheet("motoristi_hyppylyonti", report);
        int n = 0;
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (e == null || e.displayName != "Prätkäjätkä") continue;
            Undo.RecordObject(e, "Rynnäkkölyönti");
            SetBikerCharge(e, leap);
            EditorUtility.SetDirty(e);
            n++;
        }
        if (n > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Info($"Prätkäjätkän rynnäkkölyönti päivitetty {n} prätkäjätkälle: kaksi juoksuaskelta, matala syöksy nyrkki edellä, ei pitkää kyykkyä eikä korkeaa loikkaa.\n\nTallenna scene (Ctrl+S).");
    }

    /// Rusty (rusty_idle.png 16 kuvaa, rusty_kavely.png 12 kuvaa): kevyt maalaiskundi. Ilman omia isku- ja kaatumiskuvia
    /// käytetään varaliikkeitä. Kevyt: heron uppercut ja koukut lennättävät korkealle.
    static Enemy MakeRusty(Transform parent)
    {
        var report = new List<string>();
        foreach (var n in new[] { "rusty_idle", "rusty_kavely", "rusty_ruoska", "rusty_nykaisy", "rusty_kaatuminen", "rusty_ylosnousu", "rusty_heitetty", "rusty_lyonti", "rusty_syoksy", "rusty_potku", "rusty_potku2" })
        {
            string path = FindTexture(n);
            if (path != null) SetupAndSlice(path);
        }
        var idle = EnemySheet("rusty_idle", report);
        if (idle.Length == 0) return null;
        var go = new GameObject("Rusty");
        go.transform.SetParent(parent, false);
        var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
        var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
        var e = go.AddComponent<Enemy>();
        e.body = visual; e.shadow = shadow; visual.sprite = idle[0];
        e.displayName = "Rusty";
        e.idleSprites = idle; e.idleFrameTime = 0.07f;
        var walk = EnemySheet("rusty_kavely", report);
        e.walkSprites = walk.Length > 0 ? walk : idle; e.walkFrameTime = 0.075f;
        // erikoisliike: ruoskaisku kaukaa (12 kuvaa, solu 1664 leveä): 0 asento, 1–3 veto, 4 kaari pään yli, 5 silmukka, 6 ruoska ojennettuna (isku), 7–11 palautus
        var whip = EnemySheet("rusty_ruoska", report);
        if (whip.Length >= 12)
        {
            e.altAttackSprites = whip; e.altImpactFrame = 6; e.altDamage = 14; e.altKnockdown = true; e.altReach = 7f;
            e.altChance = 0.4f; e.altNearChance = 0.15f; e.chargeRange = 7f; e.chargeMinRange = 2.5f;
            e.altLungeSpeed = 0f; e.altExtraWindup = 0.34f; e.altKnockSpeed = 6f; e.altKnockUp = 6f;   // ruoskan veto 0,5 s (jabin lyhyt veto ei saa nopeuttaa sitä)
            // hero voi napata ruoskasta kiinni (kurotus ajoissa): nykäisy 6 kuvaa, solu 2048 leveä; sen jälkeen ei ruoskaa
            var yank = EnemySheet("rusty_nykaisy", report);
            if (yank.Length >= 6) { e.whipAttack = true; e.yankSprites = yank; }
        }
        // iskut (solu 640 leveä, 8–10 kuvaa, osumakuva 3): jab = suora, syöksyisku kaataa, potku kaataa ja lennättää, korkea potku ruoskan jälkeen
        var jab = EnemySheet("rusty_lyonti", report);
        if (jab.Length >= 8) { e.punchSprites = jab; e.punchImpactFrame = 3; e.secondImpactFrame = -1; e.windupTime = 0.16f; e.punchRecoverTime = 0.4f; e.punchDamage = 8; }
        var lunge = EnemySheet("rusty_syoksy", report);
        if (lunge.Length >= 8) { e.punch2Sprites = lunge; e.punch2ImpactFrame = 3; e.punch2Damage = 12; e.punch2Knockdown = true; e.punch2Chance = 0.3f; e.punch2LaunchUp = 0f; }
        var kick = EnemySheet("rusty_potku", report);
        if (kick.Length >= 8)
        {
            e.punch3Sprites = kick; e.punch3ImpactFrame = 3; e.punch3WindupTime = 0.25f; e.punch3RecoverTime = 0.5f;
            e.punch3Damage = 14; e.punch3Knockdown = true; e.punch3LaunchX = 5f; e.punch3LaunchUp = 4f; e.punch3Chance = 0.2f; e.punch3Reach = 2.4f;
        }
        var kick2 = EnemySheet("rusty_potku2", report);
        if (kick2.Length >= 8) { e.afterWhipSprites = kick2; e.afterWhipImpact = 3; e.afterWhipDamage = 16; e.afterWhipReach = 2.5f; e.afterWhipChance = 0.3f; }
        if (jab.Length >= 8 && lunge.Length >= 8 && kick.Length >= 8) { e.combos = new[] { "JSK" }; e.comboChance = 0.3f; e.comboArmor = true; e.comboWindupScale = 0.75f; e.comboGap = 0.1f; }
        // 5 kuvaa: 0 osuma, 1 horjuu taakse, 2 lento selällään hattu ilmassa, 3 voltti ylösalaisin, 4 makaa selällään
        var fall = EnemySheet("rusty_kaatuminen", report);
        if (fall.Length >= 5)
        {
            e.hurtSprites = new[] { fall[0] };
            e.knockdownSprites = new[] { fall[0], fall[1], fall[2], fall[3], fall[4] };   // ilmassa 0–3, viimeinen jää makuuksi
            e.landSprites = new[] { fall[4] }; e.landFrameTime = 0.12f;
        }
        // 7 kuvaa: 0 makaa, 1 nousee istumaan, 2 konttaa, 3 polvillaan hattu maassa, 4 hattu kädessä, 5 laittaa hatun, 6 tappeluasento
        var rise = EnemySheet("rusty_ylosnousu", report);
        if (rise.Length >= 7) { e.getUpSprites = rise; e.getUpTime = 1.3f; }
        // heitto (9 kuvaa): 0 asento, 1 napattu kumaraan, 2 kippaa (otteessa 0–2), 3–5 lento (kippaa, pää alas, selälleen), 6–8 makaa
        var thrown = EnemySheet("rusty_heitetty", report);
        if (thrown.Length >= 9) { e.flipThrownSprites = thrown; e.flipFlightFrames = 3; e.flipFlightFrameTime = 0.15f; e.flipLandFrameTime = 0.13f; }
        e.lightweight = true;
        e.fightsEveryone = true;
        e.maxHealth = 90; e.attackRange = 1.8f; e.attackCooldown = 1.0f;
        e.blockChance = 0f;
        e.moveSpeedX = 3.0f; e.moveSpeedY = 1.5f;
        // Rustyn äänet (Assets/Audio/rusty): gasp = osuma, rusty_hit = Rustyn isku, fall = kaatuminen, launch = lento korkealle, defeated = ei nouse, whip = ruoska
        const float db3 = 0.708f;   // Rustyn äänet 3 dB hiljaisemmiksi (heron äänien rinnalla)
        e.hurtSounds = LoadClips("Assets/Audio/rusty", "gasp"); e.hurtVolume = 0.9f;   // osuma/kaatumisäänet on tasattu heron tasolle (-23 dB) tiedostoissa
        if (e.hurtSounds.Length == 0) e.hurtSounds = LoadClips("Assets/Audio/big thug", "gasp");
        e.attackSounds = LoadClips("Assets/Audio/rusty", "rusty_hit"); e.attackVolume = 0.9f; e.attackSoundChance = 0.8f;
        e.altSwingSounds = LoadClips("Assets/Audio/rusty", "whip"); e.altSwingVolume = db3;
        e.fallVoice = LoadClips("Assets/Audio/rusty", "fall").FirstOrDefault(); e.fallVoiceVolume = 1f;
        e.launchVoice = LoadClips("Assets/Audio/rusty", "launch").FirstOrDefault(); e.launchVoiceVolume = 1f;
        // speak: joskus ruudulle tullessa ja satunnaisesti, kun hero ei ole aivan vieressä
        e.tauntSounds = LoadClips("Assets/Audio/rusty", "speak"); e.tauntVolume = db3; e.tauntNoRepeat = true;
        e.tauntPause = new Vector2(10f, 20f); e.tauntMinDistance = 5f; e.tauntOnWakeChance = 0.6f;
        e.deathVoice = LoadClips("Assets/Audio/rusty", "defeated").FirstOrDefault(); e.deathVoiceVolume = 1f;
        e.wakeDistance = 100f; e.retreatChance = 0f;
        Undo.RegisterCreatedObjectUndo(go, "Rusty");
        ApplyPieArt(e, report);
        Debug.Log("Rusty: " + string.Join(", ", report));
        return e;
    }

    /// Romuukko (romuttamon pomo, romuukko_*.png videoista, solu 768 x 448): iso, hidas ja kestävä. Jab, syöksyisku ja korkea potku kombona
    /// (kombo menee loppuun), kierrepotku erikoisliikkeenä, torjunta. Kaatumiseen ja ylösnousuun käytetään heitetty-sarjaa (ei omia kuvia).
    static Enemy MakeRomuukko(Transform parent)
    {
        var report = new List<string>();
        foreach (var n in new[] { "romuukko_idle", "romuukko_kavely", "romuukko_juoksu", "romuukko_lyonti", "romuukko_syoksy", "romuukko_potku", "romuukko_potku2", "romuukko_torjunta", "romuukko_heitetty", "romuukko_kaatuminen", "romuukko_ylosnousu", "romuukko_puristus", "romuukko_rengas_idle", "romuukko_rengas_kavely", "romuukko_rengas_juoksu", "romuukko_rengas_heitto", "romuukko_poyta", "romuukko_kanto" })
        {
            string path = FindTexture(n);
            if (path != null) SetupAndSlice(path);
        }
        var idle = EnemySheet("romuukko_idle", report);
        if (idle.Length == 0) return null;
        var go = new GameObject("Romuukko");
        if (parent != null) go.transform.SetParent(parent, false);
        var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
        var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
        var e = go.AddComponent<Enemy>();
        e.body = visual; e.shadow = shadow; visual.sprite = idle[0];
        e.displayName = "Romuukko";
        e.visualScale = 1f; e.attackArtScale = 1f; e.knockArtScale = 1f; e.walkArtScale = 1f;
        e.idleSprites = idle; e.idleFrameTime = 0.1f;   // 12 kuvaa = hengitys, n. 1,2 s
        var walk = EnemySheet("romuukko_kavely", report);
        e.walkSprites = walk.Length > 0 ? walk : idle; e.walkFrameTime = 0.085f;   // 12 kuvaa, askelsykli n. 1 s
        var run = EnemySheet("romuukko_juoksu", report);
        if (run.Length >= 8) { e.runSprites = run; e.runFrameTime = 0.05f; e.runSpeedMultiplier = 1.9f; }
        // iskut: jab (9 kuvaa, osuma 3), syöksyisku (11 kuvaa, osuma 3, kaataa), korkea potku (8 kuvaa, osuma 3, kaataa)
        var jab = EnemySheet("romuukko_lyonti", report);
        if (jab.Length >= 8) { e.punchSprites = jab; e.punchImpactFrame = 3; e.secondImpactFrame = -1; e.windupTime = 0.22f; e.punchRecoverTime = 0.45f; e.punchDamage = 10; }
        var lunge = EnemySheet("romuukko_syoksy", report);
        if (lunge.Length >= 8) { e.punch2Sprites = lunge; e.punch2ImpactFrame = 3; e.punch2Damage = 16; e.punch2Knockdown = true; e.punch2Chance = 0.3f; e.punch2LaunchUp = 0f; }
        var kick = EnemySheet("romuukko_potku", report);
        if (kick.Length >= 8)
        {
            e.punch3Sprites = kick; e.punch3ImpactFrame = 3; e.punch3WindupTime = 0.3f; e.punch3RecoverTime = 0.55f;
            e.punch3Damage = 18; e.punch3Knockdown = true; e.punch3LaunchX = 5f; e.punch3LaunchUp = 3f; e.punch3Chance = 0.25f; e.punch3Reach = 2.5f;
        }
        // työntöpotku (8 kuvaa, osuma 2): kombon viimeinen isku (videon pyörähdyspotku on jätetty pois), kaataa
        var round = EnemySheet("romuukko_potku2", report);
        if (round.Length >= 8)
        {
            e.altAttackSprites = round; e.altImpactFrame = 2; e.altDamage = 18; e.altKnockdown = true; e.altReach = 2.6f;
            e.altChance = 0.15f; e.altLungeSpeed = 0f; e.altExtraWindup = 0.1f; e.altKnockSpeed = 7f; e.altKnockUp = 4f;
        }
        // videon koko sarja kombona: jab, jab, syöksyisku, korkea potku, työntöpotku (J J S K P); lyhyempi JSK välillä
        if (jab.Length >= 8 && lunge.Length >= 8 && kick.Length >= 8)
        {
            e.combos = round.Length >= 8 ? new[] { "JJSKP", "JJSKP", "JSK" } : new[] { "JJSK", "JSK" };
            e.comboChance = 0.45f; e.comboArmor = true; e.comboWindupScale = 0.55f; e.comboGap = 0.05f;
        }
        // torjunta (5 kuvaa: käsi ylös kasvojen eteen)
        var guard = EnemySheet("romuukko_torjunta", report);
        if (guard.Length >= 4)
        {
            e.blockSprites = new[] { guard[0], guard[1], guard[2], guard[3], guard[3], guard[2], guard[1] };
            e.blockTime = 0.5f; e.blockChance = 0.25f; e.maxBlocksInRow = 2;
        }
        // heitetty-sarja (9 kuvaa): 0–2 ote (asento), 3–5 lento (vinossa, voltti, selällään), 6–8 makaa
        var thrown = EnemySheet("romuukko_heitetty", report);
        if (thrown.Length >= 9) { e.flipThrownSprites = thrown; e.flipFlightFrames = 3; e.flipFlightFrameTime = 0.16f; e.flipLandFrameTime = 0.13f; }
        // osuma ja kaatuminen (9 kuvaa): 0 asento, 1 osuma (pää taakse), 2–4 ilmassa, 5–6 selällään jalat ylhäällä, 7–8 makaa
        var fall = EnemySheet("romuukko_kaatuminen", report);
        if (fall.Length >= 9)
        {
            e.hurtSprites = new[] { fall[1] };
            e.knockdownSprites = new[] { fall[1], fall[2], fall[3], fall[4], fall[8] };   // ilmassa 1–4, viimeinen jää makuuksi
            e.landSprites = new[] { fall[5], fall[6], fall[7], fall[8] }; e.landFrameTime = 0.12f;
        }
        else if (thrown.Length >= 9)
        {
            e.hurtSprites = new[] { thrown[0] };
            e.knockdownSprites = new[] { thrown[3], thrown[4], thrown[5], thrown[6], thrown[7] };
            e.landSprites = new[] { thrown[7] }; e.landFrameTime = 0.12f;
        }
        // ylösnousu (10 kuvaa): 0–1 makaa, 2 istuu, 3 polvillaan, 4–5 kyyryssä, 6–7 nousee, 8–9 tappeluasento
        var rise = EnemySheet("romuukko_ylosnousu", report);
        if (rise.Length >= 10) { e.getUpSprites = rise; e.getUpTime = 1.6f; }
        // karhunpuristus (8 kuvaa): 0–1 kurotus, 2 ote, 3–5 puristus, 6 päästää irti (hero lentää), 7 tappeluasento (ei käytössä)
        var hug = EnemySheet("romuukko_puristus", report);
        if (hug.Length >= 7)
        {
            e.clinchSprites = hug; e.clinchChance = 0.3f; e.clinchHits = 4; e.clinchDamage = 6; e.clinchFinalDamage = 14;
            e.clinchReach = 1.3f; e.clinchReachTime = 0.4f; e.clinchHitTime = 0.5f; e.clinchLaunchX = 8f; e.clinchLaunchUp = 5f;
        }
        // rengas: kanto (idle 8, kävely 12, juoksu 14) ja heitto (9 kuvaa, rengas lähtee kuvassa 7); lentävä rengas on autonrengas_lento.png
        var tireIdle = EnemySheet("romuukko_rengas_idle", report);
        var tireWalk = EnemySheet("romuukko_rengas_kavely", report);
        var tireRun = EnemySheet("romuukko_rengas_juoksu", report);
        var tireThrow = EnemySheet("romuukko_rengas_heitto", report);
        var tireSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Rekvisiitta/autonrengas_lento.png");
        if (tireIdle.Length >= 4 && tireThrow.Length >= 9 && tireSprite != null)
        {
            e.tireIdleSprites = tireIdle; e.tireWalkSprites = tireWalk; e.tireRunSprites = tireRun; e.tireThrowSprites = tireThrow;
            e.tireAirSprite = tireSprite; e.startsWithTire = true;
            e.tireFloorSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Rekvisiitta/autonrengas_maassa.png");
            e.tireRollingSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Rekvisiitta/autonrengas_pyorii.png");
            e.tireThrowTimes = new[] { 0.12f, 0.1f, 0.1f, 0.1f, 0.08f, 0.08f, 0.05f, 0.2f, 0.2f }; e.tireReleaseFrame = 7;
            e.tireThrowChance = 0.6f; e.tireThrowMinRange = 3f; e.tireThrowMaxRange = 10f; e.tireDamage = 18; e.tireRearmTime = 15f;
        }
        // pöydän, heron ja kevyen vihun nosto, kanto ja heitto (poyta 6 kuvaa: 0 kurkotus, 1 nosto, 2 pään yli, 3 veto taakse, 4 heitto, 5 jatko; kanto 15 kuvaa, silmukka)
        var tbl = EnemySheet("romuukko_poyta", report);
        var carryObj = EnemySheet("romuukko_kanto", report);
        if (tbl.Length >= 6 && carryObj.Length >= 8)
        {
            e.grabSprites = new[] { tbl[0], tbl[1], tbl[2], tbl[2], tbl[3], tbl[4], tbl[4], tbl[5] };
            e.throwsTables = true; e.tableRange = 7f; e.barrelThrowSpeed = 24.2f; e.barrelThrowUp = 1.5f;
            e.throwSpeed = 24.2f; e.throwUp = 4f;
            e.carryWalkSprites = carryObj; e.carryWalkFrameTime = 0.1f; e.carryThrowRange = 6f;
            e.grabsHero = true; e.grabChance = 0.25f; e.throwForward = true;
            e.grabsAnyone = true; e.grabsLightOnly = true;   // muita vastaan (jos tilanne sallii): nappaa myös kevyitä vihuja, ei isoja
        }
        e.bigBody = true;   // hero heittää kuperkeikalla kuten Horhen
        e.fightsEveryone = false;
        e.maxHealth = 420; e.attackRange = 2.2f; e.attackCooldown = 1.0f;
        e.moveSpeedX = 2.2f; e.moveSpeedY = 1.4f;
        e.flankChance = 0.1f; e.retreatChance = 0f; e.wakeDistance = 8f;
        e.hurtSounds = LoadClips("Assets/Audio/big thug", "gasp"); e.hurtVolume = 0.99f;
        Undo.RegisterCreatedObjectUndo(go, "Romuukko");
        Debug.Log("Romuukko: " + string.Join(", ", report));
        return e;
    }

    [MenuItem("Beat em up/119. Romuttamon pomo Romuukko (kopin eteen)")]
    static void CreateJunkBoss()
    {
        if (PlayModeBlocked()) return;
        var yard = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Romuttamo");
        if (yard == null) { Info("Tee ensin Romuttamo (118)."); return; }
        foreach (var old in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(en => en != null && en.displayName == "Romuukko").ToArray())
            Undo.DestroyObjectImmediate(old.gameObject);
        var boss = MakeRomuukko(null);
        if (boss == null) { Info("romuukko_idle.png puuttuu (Sprites/Viholliset)."); return; }
        const float left = 92000f, ppu = 941f / 12f, hutPx = 2300f;   // kopin ovi panoraamassa n. 2240 px
        boss.transform.position = new Vector3(left + hutPx / ppu, Mathf.Lerp(yard.minDepthY, yard.maxDepthY, 0.45f), 0f);
        boss.idleFacing = -1;
        EditorSceneManager.MarkSceneDirty(boss.gameObject.scene);
        Info("Romuukko lisätty romuttamon kopin eteen (herää, kun hero tulee 8 yksikön päähän). 420 elämää, kombo (jab, syöksy, korkea potku) menee loppuun, kierrepotku, torjunta, heitettävissä kuperkeikalla.\n\nKaatumiseen ja ylösnousuun on käytetty heitetty-sarjan kuvia; omat osuma-, kaatumis- ja ylösnousukuvat voi lisätä myöhemmin.\n\nTallenna scene (Ctrl+S).");
    }

    static Enemy[] YardGuards() =>
        Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(en => en != null && (en.gameObject.name == "Romuttamon Rusty" || en.gameObject.name == "Romuttamon rekkamies")).ToArray();

    [MenuItem("Beat em up/121. Romuttamon piha: renkaat, risupallot, vartijoiksi Rusty ja rekkamies (konttorin ovi lukossa kunnes voitettu)")]
    static void AddJunkyardGuards()
    {
        if (PlayModeBlocked()) return;
        var yard = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Romuttamo");
        if (yard == null) { Info("Tee ensin Romuttamo (118)."); return; }
        var trucker = GameObject.Find("Rekkamies");
        if (trucker == null || trucker.GetComponent<Enemy>() == null) { Info("Tee ensin Rekkamies (115): hänestä kopioidaan romuttamon vartija."); return; }
        foreach (var n in new[] { "Romuttamon renkaat", "Romuttamon Rusty", "Romuttamon rekkamies", "Romuttamon vartijat" })
        { var old = GameObject.Find(n); if (old != null) Undo.DestroyObjectImmediate(old); }
        var root = new GameObject("Romuttamon vartijat");
        Undo.RegisterCreatedObjectUndo(root, "Romuttamon vartijat");
        const float left = 92000f, ppu = 941f / 12f;
        float PX(float px) => left + px / ppu;
        float DY(float k) => Mathf.Lerp(yard.maxDepthY, yard.minDepthY, k);

        // vartijat: Rusty ja rekkamies, molemmat taistelevat vain heroa vastaan; ovi aukeaa, kun molemmat on voitettu
        var rusty = MakeRusty(root.transform);
        var truck = Object.Instantiate(trucker, root.transform).GetComponent<Enemy>();
        void Place(Enemy e, string label, float px, float k, int facing)
        {
            if (e == null) return;
            e.gameObject.name = label;
            e.transform.position = new Vector3(PX(px), DY(k), 0f);
            e.fightsEveryone = false; e.huntsBrawlers = false; e.joinsFightWhenSquadComes = false;
            e.wakeDistance = 10f; e.idleFacing = facing;
            e.wanderMinX = e.wanderMaxX = 0f;
            e.gameObject.SetActive(true);
        }
        var voice = truck != null ? truck.GetComponent<RekkamiesVoice>() : null;
        if (truck != null && voice == null) voice = truck.gameObject.AddComponent<RekkamiesVoice>();   // alkuperäisellä ei ole puhekomponenttia (tulee vasta valikosta 116)
        if (voice != null)
        {
            // puheet toimivat vain, kun hero on samalla alueella kuin ääni: kopio puhuu romuttamolla (ei baarin tanssi- ja Shanice-repliikkejä)
            voice.enemy = truck; voice.area = null; voice.brawl = null; voice.elaine = null; voice.shanice = null;   // area tyhjä: puhuu missä alueella tahansa
            voice.speakInterval = new Vector2(8f, 16f); voice.hearRange = 1000f;
            voice.speak = LoadClips("Assets/Audio/rekkamies", "puhe");
            voice.speakShanice = new AudioClip[0]; voice.danceTalk = new AudioClip[0];
        }
        Place(rusty, "Romuttamon Rusty", 1350f, 0.5f, -1);
        Place(truck, "Romuttamon rekkamies", 3150f, 0.4f, -1);

        // renkaat heitettäviksi ympäri pihaa
        var tires = new GameObject("Romuttamon renkaat");
        Undo.RegisterCreatedObjectUndo(tires, "Romuttamon renkaat");
        var ft = tires.AddComponent<FixedTires>();
        ft.area = yard;
        ft.floorSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Rekvisiitta/autonrengas_maassa.png");
        ft.rollingSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Rekvisiitta/autonrengas_pyorii.png");
        ft.airSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Rekvisiitta/autonrengas_lento.png");
        ft.spots = new[] { new Vector2(PX(900f), DY(0.3f)), new Vector2(PX(1800f), DY(0.6f)), new Vector2(PX(2700f), DY(0.25f)),
                           new Vector2(PX(3600f), DY(0.55f)), new Vector2(PX(4400f), DY(0.35f)) };

        // risupallot pyörivät pihan yli
        var weeds = yard.GetComponent<DesertTumbleweeds>();
        if (weeds == null) weeds = Undo.AddComponent<DesertTumbleweeds>(yard.gameObject);
        Undo.RecordObject(weeds, "Romuttamon risupallot");
        weeds.area = yard;
        weeds.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Rekvisiitta/aavikko_risupallo.png");
        weeds.rollSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx/tumbleweed.wav");
        weeds.count = 3; weeds.minSpeed = 1.5f; weeds.maxSpeed = 4f;
        weeds.breakSprites = LoadTumbleweedBreakSprites();
        EditorUtility.SetDirty(weeds);

        // konttorin ovi lukkoon
        var door = GameObject.Find("Romuttamolta konttoriin");
        if (door != null && door.GetComponent<Door>() != null)
        {
            var d = door.GetComponent<Door>();
            Undo.RecordObject(d, "Konttorin ovi");
            d.guards = YardGuards(); d.guardsLockedText = "Ovi on lukossa. Voita ensin pihan vartijat: Rusty ja rekkamies.";
            EditorUtility.SetDirty(d);
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info("Romuttamon piha: Rusty (x 1350) ja rekkamies (x 3150) vartijoina, 5 poimittavaa rengasta ja 3 risupalloa." +
             (door != null ? "\nKonttorin ovi on lukossa, kunnes molemmat vartijat on voitettu." : "\nKonttorin ovea ei vielä ole: aja 120 (se liittää vartijat oveen).") + "\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/120. Romuukon konttori: pomotaistelu, ovi romuttamon kopista, renkaat päädyissä")]
    static void CreateJunkOffice()
    {
        if (PlayModeBlocked()) return;
        const string path = "Assets/Sprites/Taustat/romuukko_konttori.png";   // 2073 x 753: kaksi kuvaa yhdistettynä, ikkunan lasi läpinäkyvä
        const string skyPath = "Assets/Sprites/Taustat/aavikko_parallax_ylempi_loop.png";
        var yard = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Romuttamo");
        if (yard == null || AssetImporter.GetAtPath(path) == null) { Info("Tee ensin Romuttamo (118). Tarvitaan romuukko_konttori.png (Sprites/Taustat)."); return; }
        foreach (var n in new[] { "Romuukon konttori", "Alue: Romuukon konttori", "Romuttamolta konttoriin", "Konttorista romuttamolle", "Konttorin renkaat", "Konttorin pöydät" })
        { var old = GameObject.Find(n); if (old != null) Undo.DestroyObjectImmediate(old); }
        foreach (var old in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(en => en != null && en.displayName == "Romuukko").ToArray())
            Undo.DestroyObjectImmediate(old.gameObject);

        const float X0 = 93000f;
        const float floorPx = 495f;      // seinän ja lattian raja kuvassa (rivi ylhäältä)
        const float doorPx = 1666f;      // ovi kuvan oikeassa puoliskossa
        const float windowCenterRow = 246f;
        float ppu = 753f / (2f * CamHalf);
        var sprite = ImportShipSprite(path, ppu, false, SpriteAlignment.Center);
        if (sprite == null) { Info("Kuvan tuonti epäonnistui: " + path); return; }
        float wU = sprite.rect.width / ppu, hU = sprite.rect.height / ppu;
        float PX(float px) => X0 + px / ppu;
        float top = CamY + hU * 0.5f;

        var root = new GameObject("Romuukon konttori");
        Undo.RegisterCreatedObjectUndo(root, "Romuukon konttori");
        var bg = new GameObject("Konttorin tausta").AddComponent<SpriteRenderer>();
        bg.transform.SetParent(root.transform, false);
        bg.sprite = sprite; bg.sortingOrder = -10000;
        bg.transform.position = new Vector3(X0 + wU * 0.5f, CamY, 0f);

        float halfW = CamHalf * 16f / 9f;
        var area = ReuseArea("Alue: Romuukon konttori");
        Undo.RecordObject(area, "Romuukon konttori");
        area.areaName = "Romuukon konttori"; area.useSidewalk = false;
        area.music = yard.music; area.ambience = yard.ambience; area.ambienceVolume = yard.ambienceVolume;
        area.maxDepthY = top - floorPx / ppu;
        area.minDepthY = CamY - CamHalf + 1.2f;
        area.camMinX = X0 + halfW; area.camMaxX = X0 + Mathf.Max(wU - halfW, halfW);
        area.walkMinX = X0 + 0.8f; area.walkMaxX = X0 + wU - 0.8f;
        area.depthLimits = new Vector2[0];
        area.backScale = 1f;

        // ikkunasta näkyy aavikkokadun taivas (ylempi parallax: pilvet ja mesat), kuvan takana
        var sky = ImportShipSprite(skyPath, 100f, true, SpriteAlignment.Center);
        if (sky != null)
        {
            var s = new GameObject("Konttorin ikkunan taivas").AddComponent<SpriteRenderer>();
            s.transform.SetParent(root.transform, false);
            float skyK = CamHalf * 2f * 1.15f / sky.bounds.size.y;
            float windowY = top - windowCenterRow / ppu;
            // ikkunan keskelle osuu taivaan kuvan rivi 430 (pilvien alareuna, mesojen yläpuoli ikkunan alareunassa)
            float rowToWorld = sky.bounds.size.y / 941f * skyK;
            s.transform.position = new Vector3(X0 + wU * 0.5f, windowY - (470.5f - 430f) * rowToWorld, 0f);
            s.sprite = sky; s.sortingOrder = -10200;
            s.transform.localScale = new Vector3(skyK, skyK, 1f);
            s.drawMode = SpriteDrawMode.Tiled; s.tileMode = SpriteTileMode.Continuous;
            s.size = new Vector2((2f * halfW + (area.camMaxX - area.camMinX) * 0.04f + 4f) / skyK, sky.bounds.size.y);
            var par = s.gameObject.AddComponent<ParallaxLayer>();
            par.factor = 0.04f; par.startCamX = area.camMinX; par.minX = X0; par.maxX = X0 + wU;
        }

        // ovet: romuttamon kopin ovi (panoraamin x 2243) <-> konttorin ovi
        const float yardLeft = 92000f, yardPpu = 941f / 12f, hutPx = 2243f;
        float hutX = yardLeft + hutPx / yardPpu;
        var enter = new GameObject("Romuttamolta konttoriin").AddComponent<Door>();
        Undo.RegisterCreatedObjectUndo(enter.gameObject, "Konttorin ovi");
        enter.here = yard; enter.target = area;
        enter.spawnPoint = new Vector2(PX(doorPx) - 1.2f, Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.3f));
        enter.prompt = "Mene konttoriin"; enter.halfWidth = 1.2f; enter.maxDistanceFromWall = 1.4f;
        enter.transform.position = new Vector3(hutX, yard.MaxDepthAtX(hutX, yard.maxDepthY), 0f);
        enter.guards = YardGuards(); enter.guardsLockedText = "Ovi on lukossa. Voita ensin pihan vartijat: Rusty ja rekkamies.";
        var back = new GameObject("Konttorista romuttamolle").AddComponent<Door>();
        back.transform.SetParent(root.transform, false);
        back.here = area; back.target = yard;
        back.spawnPoint = new Vector2(hutX - 0.5f, Mathf.Lerp(yard.minDepthY, yard.maxDepthY, 0.3f));
        back.prompt = "Mene ulos romuttamolle"; back.halfWidth = 1.6f; back.maxDistanceFromWall = 1.4f;
        back.transform.position = new Vector3(PX(doorPx), area.maxDepthY, 0f);

        // renkaat heitettäviksi molempiin päätyihin
        var tires = new GameObject("Konttorin renkaat");
        Undo.RegisterCreatedObjectUndo(tires, "Konttorin renkaat");
        var ft = tires.AddComponent<FixedTires>();
        ft.area = area;
        ft.floorSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Rekvisiitta/autonrengas_maassa.png");
        ft.rollingSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Rekvisiitta/autonrengas_pyorii.png");
        ft.airSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Rekvisiitta/autonrengas_lento.png");
        float DY(float k) => Mathf.Lerp(area.maxDepthY, area.minDepthY, k);
        ft.spots = new[] { new Vector2(PX(170f), DY(0.45f)), new Vector2(PX(430f), DY(0.2f)), new Vector2(PX(1930f), DY(0.4f)), new Vector2(PX(2020f), DY(0.15f)) };

        // pöytiä heitettäväksi (olutpulloja pöydillä); Romuukko heittää niitä välillä
        var tableRoot = new GameObject("Konttorin pöydät");
        Undo.RegisterCreatedObjectUndo(tableRoot, "Konttorin pöydät");
        AddBottleTables(tableRoot.transform, new[] { new Vector3(PX(900f), DY(0.5f), 0f), new Vector3(PX(1250f), DY(0.25f), 0f), new Vector3(PX(1500f), DY(0.62f), 0f) }, 120, true);

        // pomo: konttorin vasemmassa laidassa, katsoo ovelle päin
        var boss = MakeRomuukko(null);
        if (boss == null) { Info("Konttori luotu, mutta romuukko_idle.png puuttuu."); return; }
        boss.transform.position = new Vector3(PX(780f), DY(0.45f), 0f);
        boss.idleFacing = 1; boss.wakeDistance = 13f;
        EditorUtility.SetDirty(area);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info($"Romuukon konttori luotu (x = {X0:0}). Ovi romuttamon kopista (panoraamin x 2243) vie konttoriin ja konttorin ovi takaisin. Ikkunasta näkyy aavikkokadun taivas.\n" +
             "Pomo Romuukko odottaa konttorin vasemmassa laidassa (herää 13 yksikön päästä); päädyissä on 2 + 2 poimittavaa rengasta ja keskellä 3 pöytää (hän heittää niitä välillä, samoin heron ja kevyitä vihuja).\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/123. Juna-asema: sisätila ja ovi Aavikkokadun aseman pääovesta (vasemmalta sisään)")]
    static void CreateTrainStation()
    {
        if (PlayModeBlocked()) return;
        const string path = "Assets/Sprites/Taustat/asema_sisa.png";   // 3963 x 941: päätykuva, keskikuva ja päätykuva peilattuna (ikkunat läpinäkyviä)
        const string skyPath = "Assets/Sprites/Taustat/aavikko_parallax_ylempi_loop.png";
        var street = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Aavikkokatu");
        if (street == null || AssetImporter.GetAtPath(path) == null) { Info("Tee ensin Aavikkokatu (95). Tarvitaan asema_sisa.png (Sprites/Taustat)."); return; }
        foreach (var n in new[] { "Aseman sisätila", "Alue: Juna-asema", "Aavikkokadulta asemalle", "Asemalta kadulle" })
        { var old = GameObject.Find(n); if (old != null) Undo.DestroyObjectImmediate(old); }

        const float X0 = 94000f;
        const float floorPx = 605f;         // seinän ja lattian raja kuvassa (rivi ylhäältä)
        const float windowCenterRow = 306f; // ikkunoiden keskikohta
        float ppu = 941f / (2f * CamHalf);
        var sprite = ImportShipSprite(path, ppu, false, SpriteAlignment.Center);
        if (sprite == null) { Info("Kuvan tuonti epäonnistui: " + path); return; }
        float wU = sprite.rect.width / ppu, hU = sprite.rect.height / ppu;
        float PX(float px) => X0 + px / ppu;
        float top = CamY + hU * 0.5f;

        var root = new GameObject("Aseman sisätila");
        Undo.RegisterCreatedObjectUndo(root, "Aseman sisätila");
        var bg = new GameObject("Aseman tausta").AddComponent<SpriteRenderer>();
        bg.transform.SetParent(root.transform, false);
        bg.sprite = sprite; bg.sortingOrder = -10000;
        bg.transform.position = new Vector3(X0 + wU * 0.5f, CamY, 0f);

        float halfW = CamHalf * 16f / 9f;
        var area = ReuseArea("Alue: Juna-asema");
        Undo.RecordObject(area, "Juna-asema");
        area.areaName = "Juna-asema"; area.useSidewalk = false;
        area.music = street.music; area.ambience = street.ambience; area.ambienceVolume = street.ambienceVolume;
        float DepthAt(float py) => top - py / ppu;
        area.maxDepthY = DepthAt(floorPx);
        area.minDepthY = CamY - CamHalf + 1.2f;
        area.camMinX = X0 + halfW; area.camMaxX = X0 + Mathf.Max(wU - halfW, halfW);
        area.walkMinX = X0 + 0.5f; area.walkMaxX = X0 + wU - 0.5f;
        area.backScale = 1f;
        // päädyissä ovet ovat sivuseinällä: takaraja matalammalla, jotta oven eteen pääsee
        area.depthLimitsExceedMax = false;
        area.depthLimits = new[] {
            new Vector2(X0, DepthAt(670f)), new Vector2(PX(220f), DepthAt(670f)), new Vector2(PX(330f), DepthAt(floorPx)),
            new Vector2(X0 + wU - 330f / ppu, DepthAt(floorPx)), new Vector2(X0 + wU - 220f / ppu, DepthAt(670f)), new Vector2(X0 + wU, DepthAt(670f)) };

        // ikkunoista näkyy aavikkokadun taivas (ylempi parallax: pilvet ja mesat), kuvan takana
        var sky = ImportShipSprite(skyPath, 100f, true, SpriteAlignment.Center);
        if (sky != null)
        {
            var s = new GameObject("Aseman ikkunoiden taivas").AddComponent<SpriteRenderer>();
            s.transform.SetParent(root.transform, false);
            float skyK = CamHalf * 2f * 1.15f / sky.bounds.size.y;
            float windowY = top - windowCenterRow / ppu;
            float rowToWorld = sky.bounds.size.y / 941f * skyK;
            s.transform.position = new Vector3(X0 + wU * 0.5f, windowY - (470.5f - 430f) * rowToWorld, 0f);
            s.sprite = sky; s.sortingOrder = -10200;
            s.transform.localScale = new Vector3(skyK, skyK, 1f);
            s.drawMode = SpriteDrawMode.Tiled; s.tileMode = SpriteTileMode.Continuous;
            s.size = new Vector2((2f * halfW + (area.camMaxX - area.camMinX) * 0.04f + 4f) / skyK, sky.bounds.size.y);
            var par = s.gameObject.AddComponent<ParallaxLayer>();
            par.factor = 0.04f; par.startCamX = area.camMinX; par.minX = X0; par.maxX = X0 + wU;
        }

        // vasen ovi (päätykuvan ovi) <-> aseman pääovi Aavikkokadulla (jatkokuva 12, oven keskikohta kuvassa 750 px)
        const float streetPpu = 887f / 12f;
        float stationDoorX = 86000f + (15268.71f + 750f * 1.027588f) / streetPpu;
        var enter = new GameObject("Aavikkokadulta asemalle").AddComponent<Door>();
        Undo.RegisterCreatedObjectUndo(enter.gameObject, "Aseman ovi");
        enter.here = street; enter.target = area;
        enter.spawnPoint = new Vector2(PX(420f), Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.3f));
        enter.prompt = "Mene asemalle"; enter.halfWidth = 1.8f; enter.maxDistanceFromWall = 1.1f;
        enter.transform.position = new Vector3(stationDoorX, street.maxDepthY, 0f);
        var back = new GameObject("Asemalta kadulle").AddComponent<Door>();
        back.transform.SetParent(root.transform, false);
        back.here = area; back.target = street; back.returnToLastDoor = false;
        back.spawnPoint = new Vector2(stationDoorX, street.maxDepthY - 0.3f);
        back.prompt = "Mene ulos kadulle"; back.halfWidth = 1.4f; back.maxDistanceFromWall = 2.4f;
        back.transform.position = new Vector3(PX(115f), area.MaxDepthAtX(PX(115f), area.maxDepthY), 0f);
        EditorUtility.SetDirty(area); EditorUtility.SetDirty(enter);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info($"Juna-asema luotu (x = {X0:0}, leveys {wU:0} yksikköä). Sisään pääsee Aavikkokadun aseman pääovesta (E), ulos vasemmasta ovesta. Ikkunoista näkyy taivasparallaksi.\n" +
             "Oikean pään ovi (pihalle ja junaan) puuttuu vielä: sen paikka on x ≈ " + PX(wU * ppu - 115f).ToString("0") + ".\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/124. Junapiha: laituri ja juna (ikkunoista näkyy aavikon taivas ja hiekka parallaxina), ovi aseman oikeasta päästä")]
    static void CreateTrainYard()
    {
        if (PlayModeBlocked()) return;
        const string path = "Assets/Sprites/Taustat/asema_piha.png";   // 5480 x 941: neljä kuvaa (aseman ovi, kaksi vaunuosaa, veturi), junan ikkunat läpinäkyviä
        const string skyPath = "Assets/Sprites/Taustat/aavikko_parallax_ylempi_loop.png";
        const string sandPath = "Assets/Sprites/Taustat/aavikko_parallax_alempi_loop.png";
        var station = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Juna-asema");
        var street = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Aavikkokatu");
        if (street == null || AssetImporter.GetAtPath(path) == null) { Info("Tee ensin Aavikkokatu (95). Tarvitaan asema_piha.png (Sprites/Taustat)."); return; }
        foreach (var n in new[] { "Junapihan sisalto", "Alue: Junapiha", "Asemalta laiturille", "Laiturilta asemalle" })
        { var old = GameObject.Find(n); if (old != null) Undo.DestroyObjectImmediate(old); }

        const float X0 = 95000f;
        const float floorPx = 700f;          // laiturin reuna / seinä (rivi ylhäältä)
        const float horizonRow = 515f;       // ikkunoiden alaosa: hiekan horisontti
        float ppu = 941f / (2f * CamHalf);
        var sprite = ImportShipSprite(path, ppu, false, SpriteAlignment.Center);
        if (sprite == null) { Info("Kuvan tuonti epäonnistui: " + path); return; }
        float wU = sprite.rect.width / ppu, hU = sprite.rect.height / ppu;
        float PX(float px) => X0 + px / ppu;
        float top = CamY + hU * 0.5f;

        var root = new GameObject("Junapihan sisalto");
        Undo.RegisterCreatedObjectUndo(root, "Junapiha");
        var bg = new GameObject("Junapihan tausta").AddComponent<SpriteRenderer>();
        bg.transform.SetParent(root.transform, false);
        bg.sprite = sprite; bg.sortingOrder = -10000;
        bg.transform.position = new Vector3(X0 + wU * 0.5f, CamY, 0f);

        float halfW = CamHalf * 16f / 9f;
        var area = ReuseArea("Alue: Junapiha");
        Undo.RecordObject(area, "Junapiha");
        area.areaName = "Junapiha"; area.useSidewalk = false;
        area.music = street.music; area.ambience = street.ambience; area.ambienceVolume = street.ambienceVolume;
        area.maxDepthY = top - floorPx / ppu;
        area.minDepthY = CamY - CamHalf + 1.2f;
        area.camMinX = X0 + halfW; area.camMaxX = X0 + Mathf.Max(wU - halfW, halfW);
        area.walkMinX = X0 + 0.5f; area.walkMaxX = X0 + wU - 0.5f;
        area.depthLimits = new Vector2[0];
        area.backScale = 1f;

        // junan ikkunoista ja katoksen yli näkyy aavikko: taivas ja mesat (hidas) sekä hiekka (nopeampi), kuvan takana
        float horizonY = top - horizonRow / ppu;
        var layers = new[] { (skyPath, "Junapihan taivas ja mesat", -10200, 0.04f, 1.15f, 3.47f), (sandPath, "Junapihan kaukainen hiekka", -10100, 0.08f, 1f, -0.133f) };
        foreach (var (lp, lname, order, factor, mul, dy) in layers)
        {
            var spr = ImportShipSprite(lp, 100f, true, SpriteAlignment.Center);
            if (spr == null) continue;
            var s = new GameObject(lname).AddComponent<SpriteRenderer>();
            s.transform.SetParent(root.transform, false);
            float k = CamHalf * 2f / spr.bounds.size.y * mul;
            s.transform.position = new Vector3(X0 + wU * 0.5f, horizonY + dy * (CamHalf / 6f), 0f);
            s.sprite = spr; s.sortingOrder = order;
            s.transform.localScale = new Vector3(k, k, 1f);
            s.drawMode = SpriteDrawMode.Tiled; s.tileMode = SpriteTileMode.Continuous;
            s.size = new Vector2((2f * halfW + (area.camMaxX - area.camMinX) * factor + 4f) / k, spr.bounds.size.y);
            var par = s.gameObject.AddComponent<ParallaxLayer>();
            par.factor = factor; par.startCamX = area.camMinX; par.minX = X0; par.maxX = X0 + wU;
        }

        // vaunun sisäpuoli: loopattava parallax junan ikkunoiden takana (istuimet, ikkunat, valot); sen omista ikkunoista näkyy aavikko.
        // Kuvasta on leikattu vain junan ikkunoiden korkuinen kaista (rivit 250–523), joten sisäpuolta ei näy katoksen ja junan katon välissä.
        const string bandPath = "Assets/Sprites/Taustat/junapiha_vaunun_sisa.png";
        const float interiorScale = 0.55f;   // sisäkuvan koko suhteessa laiturin kuvaan
        if (AssetImporter.GetAtPath(bandPath) != null)
        {
            var band = ImportShipSprite(bandPath, ppu / interiorScale, true, SpriteAlignment.Center);
            if (band != null)
            {
                var carInside = new GameObject("Junan vaunun sisäpuoli").AddComponent<SpriteRenderer>();
                carInside.transform.SetParent(root.transform, false);
                carInside.sprite = band; carInside.sortingOrder = -10050;
                carInside.transform.position = new Vector3(X0 + wU * 0.5f, top - 470.2f / ppu, 0f);
                carInside.drawMode = SpriteDrawMode.Tiled; carInside.tileMode = SpriteTileMode.Continuous;
                const float carFactor = 0.92f;   // liikkuu vain hieman hitaammin kuin laituri (1 = täsmälleen mukana)
                carInside.size = new Vector2(2f * halfW + (area.camMaxX - area.camMinX) * carFactor + 4f, band.bounds.size.y);
                var cpar = carInside.gameObject.AddComponent<ParallaxLayer>();
                cpar.factor = carFactor; cpar.startCamX = area.camMinX; cpar.minX = X0; cpar.maxX = X0 + wU;
            }
        }

        // ovi: aseman oikean pään ovi <-> laiturin oikea ovi (kuvan vasemmassa reunassa, keskikohta 115 px)
        string doorInfo = "Asemaa (123) ei ole vielä: aja se ja sitten tämä uudelleen, niin ovi yhdistää asemaan.";
        if (station != null)
        {
            float stationDoorX = station.walkMaxX + 0.5f - 115f / ppu;   // peilatun päätykuvan ovi
            var enter = new GameObject("Asemalta laiturille").AddComponent<Door>();
            Undo.RegisterCreatedObjectUndo(enter.gameObject, "Aseman oikea ovi");
            enter.here = station; enter.target = area;
            enter.spawnPoint = new Vector2(PX(330f), Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.3f));
            enter.prompt = "Mene laiturille"; enter.halfWidth = 1.4f; enter.maxDistanceFromWall = 2.4f;
            enter.transform.position = new Vector3(stationDoorX, station.MaxDepthAtX(stationDoorX, station.maxDepthY), 0f);
            var back = new GameObject("Laiturilta asemalle").AddComponent<Door>();
            back.transform.SetParent(root.transform, false);
            back.here = area; back.target = station; back.returnToLastDoor = false;
            back.spawnPoint = new Vector2(stationDoorX - 1.6f, Mathf.Lerp(station.maxDepthY, station.minDepthY, 0.3f));
            back.prompt = "Mene asemalle"; back.halfWidth = 1.4f; back.maxDistanceFromWall = 1.4f;
            back.transform.position = new Vector3(PX(115f), area.maxDepthY, 0f);
            EditorUtility.SetDirty(enter);
            doorInfo = "Aseman oikean pään ovi vie laiturille ja laiturin vasen ovi takaisin asemalle.";
        }
        EditorUtility.SetDirty(area);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info($"Junapiha luotu (x = {X0:0}, leveys {wU:0} yksikköä). {doorInfo}\n" +
             $"Junan ovet junaan nousua varten (vaunujen oven kohdat kuvassa): 1. vaunu x ≈ {PX(1190f):0.0}, 2. vaunu x ≈ {PX(1258f + 1075f):0.0}, 3. vaunu x ≈ {PX(1258f + 1274f + 1075f):0.0}.\n" +
             "Ikkunoista näkyy aavikon taivas ja hiekka parallaxina.\n\nTallenna scene (Ctrl+S).");
    }

    /// Rekkamiehen puhe: lisää komponentin (jos puuttuu) ja puheäänet. Puhuu aina, kun hero on samalla alueella (baari, diner, romuttamo), ei muilla alueilla.
    static bool EnsureTruckerVoice(Enemy e)
    {
        if (e == null) return false;
        var v = e.GetComponent<RekkamiesVoice>();
        if (v == null) v = Undo.AddComponent<RekkamiesVoice>(e.gameObject);
        Undo.RecordObject(v, "Rekkamiehen puhe");
        v.enemy = e; v.area = null; v.hearRange = 1000f;
        var brawl = Object.FindFirstObjectByType<BarBrawl>();
        if (brawl != null) v.brawl = brawl;
        v.speak = LoadClips("Assets/Audio/rekkamies", "puhe");
        v.speakShanice = LoadClips("Assets/Audio/rekkamies", "shanice");
        v.danceTalk = LoadClips("Assets/Audio/rekkamies", "tanssilattialla");
        v.speakInterval = new Vector2(10f, 20f);
        EditorUtility.SetDirty(v);
        return v.speak != null && v.speak.Length > 0;
    }

    [MenuItem("Beat em up/126. Baarin rekkamies rauhalliseksi (tanssii, herää vasta tappelun alkaessa) ja mukaan baaritappeluun")]
    static void CalmBarTrucker()
    {
        if (PlayModeBlocked()) return;
        Enemy trucker = null;
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (e != null && e.displayName == "Rekkamies" && e.gameObject.name == "Rekkamies" && e.transform.position.x >= 91000f && e.transform.position.x < 92000f) { trucker = e; break; }
        if (trucker == null) { Info("Baarin rekkamiestä ei löytynyt (nimi 'Rekkamies', x 91000–92000). Tee ensin Rekkamies (115)."); return; }
        Undo.RecordObject(trucker, "Baarin rekkamies");
        Undo.RecordObject(trucker.transform, "Baarin rekkamies");
        trucker.transform.position = new Vector3(91000f + BarWx(3450f), -2.2f, 0f);   // tanssilattialla Elainen kanssa
        trucker.wakeDistance = -1f; trucker.joinsFightWhenSquadComes = false;
        trucker.wanderMinX = trucker.wanderMaxX = 0f; trucker.idleFacing = -1;
        EditorUtility.SetDirty(trucker);
        string brawlInfo = "baaritappelua ei löytynyt";
        var brawl = Object.FindFirstObjectByType<BarBrawl>();
        if (brawl != null)
        {
            Undo.RecordObject(brawl, "Baarin rekkamies tappeluun");
            var list = new List<Enemy>(brawl.members ?? new Enemy[0]);
            list.RemoveAll(m => m == null);
            if (!list.Contains(trucker)) list.Add(trucker);
            brawl.members = list.ToArray();
            EditorUtility.SetDirty(brawl);
            brawlInfo = "mukana baaritappelussa";
        }
        EditorSceneManager.MarkSceneDirty(trucker.gameObject.scene);
        Info("Baarin rekkamies on rauhallinen (ei herää hero lähellä), " + brawlInfo + ". Tappelu alkaa vasta, kun joku saa osuman tai 2 minuuttia on kulunut.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/127. Aavikkobaari: lisää väkeä alalattialle (3 motoristia, 2 punkkaria) tappeluun mukaan")]
    static void AddBarLowerCrowd()
    {
        if (PlayModeBlocked()) return;
        string msg = AddBarLowerCrowdCore();
        Info(msg ?? "Tee ensin Aavikkobaari (110) ja sen väki (116).");
    }

    /// Baarin alalattian lisäväki (3 motoristia, 2 punkkaria): myös valikko 116 kutsuu tätä, ettei väki katoa, kun baarin väki tehdään uusiksi.
    static string AddBarLowerCrowdCore()
    {
        var bar = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Aavikkobaari");
        var brawl = Object.FindFirstObjectByType<BarBrawl>();
        if (bar == null || brawl == null) return null;
        var root = GameObject.Find("Aavikkobaarin tappelu");
        if (root == null) { root = new GameObject("Aavikkobaarin tappelu"); Undo.RegisterCreatedObjectUndo(root, "Aavikkobaarin väki"); }
        foreach (var o in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(en => en != null && en.gameObject.name.StartsWith("Baarin lisäväki ")).ToArray())
            Undo.DestroyObjectImmediate(o.gameObject);   // aja uudestaan: vanhat lisähahmot pois
        var members = new List<Enemy>((brawl.members ?? new Enemy[0]).Where(m => m != null && !m.gameObject.name.StartsWith("Baarin lisäväki ")));
        const float left = 91000f;
        float X(float px) => left + BarWx(px);
        int n = 0;
        void Add(Enemy e, string label, float px, float y, int facing, float wander)
        {
            if (e == null) return;
            e.gameObject.name = "Baarin lisäväki " + label;
            e.transform.position = new Vector3(X(px), Mathf.Max(y, bar.minDepthY + 0.2f), 0f);
            e.wakeDistance = -1f; e.joinsFightWhenSquadComes = false; e.huntsBrawlers = true;
            e.wanderMinX = X(px - wander); e.wanderMaxX = X(px + wander); e.idleFacing = facing;
            e.gameObject.SetActive(true);
            members.Add(e); n++;
        }
        // alalattia: motoristit ja punkkarit eri kohtiin (vanha pohjoispuoli täynnä tiskillä ja lavalla)
        Add(MakeBiker(root.transform), "motoristi 1", 600f, -5.0f, 1, 140f);
        Add(MakeBiker(root.transform), "motoristi 2", 2000f, -6.0f, -1, 160f);
        Add(MakeBiker(root.transform), "motoristi 3", 3600f, -5.4f, -1, 140f);
        var punkTemplate = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(en => en != null && en.displayName == "Punkkari" && !en.gameObject.name.StartsWith("Baarin lisäväki "));
        string punkInfo = "";
        if (punkTemplate != null)
        {
            Add(Object.Instantiate(punkTemplate.gameObject, root.transform).GetComponent<Enemy>(), "punkkari 1", 1300f, -5.8f, 1, 150f);
            Add(Object.Instantiate(punkTemplate.gameObject, root.transform).GetComponent<Enemy>(), "punkkari 2", 2750f, -5.1f, -1, 150f);
        }
        else punkInfo = " (punkkaria ei löytynyt scenestä: aja vihujen kohdat 1–2)";
        Undo.RecordObject(brawl, "Baarin lisäväki");
        brawl.members = members.ToArray();
        EditorUtility.SetDirty(brawl);
        EditorSceneManager.MarkSceneDirty(brawl.gameObject.scene);
        return "Baarin alalattialle lisätty " + n + " hahmoa (3 motoristia ja punkkareita)" + punkInfo + ". He ovat rauhallisia ja heräävät tappelun mukana. Motoristit saavat piirakkakuvat, heiton ja juoksun automaattisesti.\n\nTallenna scene (Ctrl+S).";
    }

    [MenuItem("Beat em up/125. Rekkamiehen puhe kaikille rekkamiehille (baari, diner, romuttamo)")]
    static void ApplyTruckerVoices()
    {
        if (PlayModeBlocked()) return;
        int n = 0; bool clips = true;
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (e == null || e.displayName != "Rekkamies") continue;
            clips &= EnsureTruckerVoice(e); n++;
        }
        if (n > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Info(n + " rekkamiehelle lisätty puhe" + (clips ? "" : " (puheääniä ei löytynyt: Assets/Audio/rekkamies/puhe*.mp3)") + ". He puhuvat 10–20 sekunnin välein, kun hero on samalla alueella.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/98. Roccon ja Rubyn ruoskasta kiinniotto (kuvat pelaajalle)")]
    static void AssignWhipCatch()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { Info("Scenessä ei ole pelaajaa."); return; }
        string path = FindTexture("ruoskan_kiinniotto");
        if (path != null) SetupAndSlice(path);
        var sp = LoadSprites("ruoskan_kiinniotto")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        if (sp.Length < 6) { Info($"ruoskan_kiinniotto.png: {sp.Length}/6 kuvaa."); return; }
        Undo.RecordObject(pc, "Ruoskan kiinniotto");
        pc.whipCatchSprites = sp;
        string rpath = FindTexture("sankaritar_ruoskan_kiinniotto");
        if (rpath != null)
        {
            SetupAndSlice(rpath);
            var rsp = LoadSprites("sankaritar_ruoskan_kiinniotto")
                .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
            if (rsp.Length >= 6) pc.rubyWhipCatchSprites = rsp;
        }
        EditorUtility.SetDirty(pc);
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        Info("Ruoskan kiinniotto: Rocco ja Ruby nappaavat Rustyn ruoskasta (kurotus eli vastaheitto-nappi juuri ennen kuin ruoska iskee) ja nykäisee kumoon. Tallenna scene (Ctrl+S).");
    }

    /// Peltihalli: motoristit ja rokkimimmit tappelevat keskenään, ja baaritiskin puliukko mätkii kaikkia heron tullessa.
    static string SetupHallBrawl()
    {
        int bikers = 0, rockers = 0;
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (e == null) continue;
            string n = e.gameObject.name;
            if (n.StartsWith("Peltihallin motoristi")) { Undo.RecordObject(e, "Peltihallin tappelu"); e.brawlsWithOthers = true; e.brawlGroup = "motoristi"; EditorUtility.SetDirty(e); bikers++; }
            else if (n.StartsWith("Peltihallin rokkimimmi")) { Undo.RecordObject(e, "Peltihallin tappelu"); e.brawlsWithOthers = true; e.brawlGroup = "rokkimimmi"; EditorUtility.SetDirty(e); rockers++; }
        }
        string drunkInfo = "puliukkoa ei lisätty (tee ensin kohta 61)";
        var hall = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Peltihalli");
        var shop = GameObject.Find("Peltihallin baaritiski");
        var template = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(en => en != null && en.gameObject.name == "Puliukko");
        if (hall != null && shop != null && template != null)
        {
            var old = GameObject.Find("Peltihallin puliukko");
            if (old != null) Undo.DestroyObjectImmediate(old);
            var clone = Object.Instantiate(template.gameObject, shop.transform.parent);
            clone.name = "Peltihallin puliukko";
            Undo.RegisterCreatedObjectUndo(clone, "Peltihallin puliukko");
            float x = shop.transform.position.x;
            clone.transform.position = new Vector3(x + 1.4f, hall.maxDepthY - 0.5f, 0f);
            var de = clone.GetComponent<Enemy>();
            de.joinsFightWhenSquadComes = false;
            de.wakeDistance = 22f;                       // herää, kun hero tulee paikalle
            de.wanderMinX = x - 2.2f; de.wanderMaxX = x + 2.2f;   // tepastelee tiskin luona
            de.brawlsWithOthers = true; de.brawlGroup = "";       // mätkii kaikkia
            clone.SetActive(true);
            drunkInfo = "puliukko lisätty baaritiskille";
        }
        else if (hall == null || shop == null) drunkInfo = "puliukkoa ei lisätty (peltihalli tai baaritiski puuttuu)";
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return $"{bikers} motoristia ja {rockers} rokkimimmiä tappelevat keskenään; {drunkInfo}.";
    }

    [MenuItem("Beat em up/101. Peltihalli: tappelu (motoristit, rokkimimmit ja tiskin puliukko)")]
    static void AddHallBrawl()
    {
        if (PlayModeBlocked()) return;
        Info(SetupHallBrawl() + "\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/99. Aavikkokatu: Rustyt (6 kpl)")]
    static void AddDesertStreetRustys()
    {
        if (PlayModeBlocked()) return;
        var area = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Aavikkokatu");
        if (area == null) { Info("Aavikkokatua ei löydy: tee ensin kohta 95 (Aavikkokatu)."); return; }
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null && (pc.whipCatchSprites == null || pc.whipCatchSprites.Length < 6 || pc.rubyWhipCatchSprites == null || pc.rubyWhipCatchSprites.Length < 6)) AssignWhipCatch();
        var oldRoot = GameObject.Find("Aavikkokadun Rustyt");
        if (oldRoot != null) Undo.DestroyObjectImmediate(oldRoot);
        var root = new GameObject("Aavikkokadun Rustyt");
        Undo.RegisterCreatedObjectUndo(root, "Aavikkokadun Rustyt");
        // Rustyt odottavat pitkin katua ja heräävät, kun hero lähestyy (aaltoina)
        float[] along = { 0.2f, 0.32f, 0.44f, 0.56f, 0.68f, 0.82f };
        float[] depth = { 0.35f, 0.7f, 0.5f, 0.25f, 0.75f, 0.5f };
        int made = 0;
        for (int i = 0; i < along.Length; i++)
        {
            var rusty = MakeRusty(root.transform);
            if (rusty == null) { Info("Tarvitaan Sprites/Viholliset/rusty_idle.png ja rusty_kavely.png."); return; }
            rusty.name = "Aavikkokadun Rusty " + (i + 1);
            rusty.transform.position = new Vector3(Mathf.Lerp(area.walkMinX, area.walkMaxX, along[i]),
                Mathf.Lerp(area.minDepthY, area.maxDepthY, depth[i]), 0f);
            rusty.wakeDistance = 13f;
            rusty.gameObject.SetActive(true);
            made++;
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info($"Aavikkokadulle lisätty {made} Rustya, jotka heräävät kun hero lähestyy. Tallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/97. Lisää Rusty pelaajan viereen")]
    static void AddRusty()
    {
        if (PlayModeBlocked()) return;
        var pc = Object.FindFirstObjectByType<PlayerController>();
        var rusty = MakeRusty(null);
        if (rusty == null) { Info("Tarvitaan Sprites/Viholliset/rusty_idle.png ja rusty_kavely.png."); return; }
        Vector3 at = pc != null ? pc.transform.position + new Vector3(4f, 0f, 0f) : Vector3.zero;
        rusty.transform.position = at;
        rusty.gameObject.SetActive(true);
        if (pc != null && (pc.whipCatchSprites == null || pc.whipCatchSprites.Length < 6 || pc.rubyWhipCatchSprites == null || pc.rubyWhipCatchSprites.Length < 6)) AssignWhipCatch();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Info("Rusty lisätty pelaajan viereen: idle, kävely ja kevyt (uppercut lennättää korkealle). Tallenna scene (Ctrl+S).");
    }

    /// Prätkäjätkän äänet (Assets/Audio/motoristi): gasp = osuma, hit = lyönti, fall = kaatuminen, lataus = hyppylyönnin veto, speak = puhe.
    static void ApplyBikerAudio(Enemy e)
    {
        const float db3 = 0.708f;   // motoristin äänet 3 dB hiljaisemmiksi
        e.hurtSounds = LoadClips("Assets/Audio/motoristi", "gasp"); e.hurtVolume = 0.9f;   // osuma/kaatumisäänet on tasattu heron tasolle (-23 dB) tiedostoissa
        if (e.hurtSounds.Length == 0) { e.hurtSounds = LoadClips("Assets/Audio/big thug", "gasp"); e.hurtVolume = 0.8f; }
        e.attackSounds = LoadClips("Assets/Audio/motoristi", "hit"); e.attackVolume = 0.9f; e.attackSoundChance = 0.8f;
        e.fallVoice = LoadClips("Assets/Audio/motoristi", "fall").FirstOrDefault(); e.fallVoiceVolume = 1f;
        e.altWindupSounds = LoadClips("Assets/Audio/motoristi", "lataus"); e.altWindupVolume = 1f;   // tasattu heron tasolle tiedostossa
        e.tauntSounds = LoadClips("Assets/Audio/motoristi", "speak"); e.tauntVolume = db3; e.tauntNoRepeat = true;
        e.tauntPause = new Vector2(10f, 20f); e.tauntMinDistance = 5f; e.tauntOnWakeChance = 0.5f;
    }

    /// Prätkäjätkä nostaa ja heittää pöytiä sekä heroa (motoristi_poyta.png 6 kuvaa: 0 kurkotus, 1 nosto, 2 pään yli, 3 veto taakse, 4 heitto, 5 jatko)
    /// ja kantaa nostettua pään yllä (motoristi_kanto.png 15 kuvaa, silmukka). Samat asetukset kuin Rekkamiehellä, mutta myös hero napataan.
    static bool ApplyBikerThrow(Enemy e, List<string> report)
    {
        foreach (var n in new[] { "motoristi_poyta", "motoristi_kanto" })
        {
            string path = FindTexture(n);
            if (path != null) SetupAndSlice(path);
        }
        var tbl = EnemySheet("motoristi_poyta", report);
        var carry = EnemySheet("motoristi_kanto", report);
        if (tbl.Length < 6 || carry.Length < 8) return false;
        e.grabSprites = new[] { tbl[0], tbl[1], tbl[2], tbl[2], tbl[3], tbl[4], tbl[4], tbl[5] };   // 0 kurkotus, 1–4 nosto, 5–6 heitto, 7 jatko
        e.throwsTables = true; e.tableRange = 7f; e.barrelThrowSpeed = 24.2f; e.barrelThrowUp = 1.5f;
        e.throwSpeed = 24.2f; e.throwUp = 4f;
        e.carryWalkSprites = carry; e.carryWalkFrameTime = 0.067f; e.carryThrowRange = 6f;
        e.grabsHero = true; e.grabChance = 0.25f; e.throwForward = true;
        e.grabsAnyone = true; e.grabsLightOnly = true;   // tappelussa muita vastaan (esim. aavikkobaari): nappaa ja heittää myös muita vihuja, mutta ei isoja (bigBody) eikä raskaita
        return true;
    }

    [MenuItem("Beat em up/122. Prätkäjätkät heittävät pöytiä ja heroa (kaikille sceneen)")]
    static void UpdateBikerThrow()
    {
        if (PlayModeBlocked()) return;
        int n = 0; var report = new List<string>();
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (e == null || e.displayName != "Prätkäjätkä") continue;
            Undo.RecordObject(e, "Prätkäjätkän heitto");
            if (!ApplyBikerThrow(e, report)) { Info("motoristi_poyta (6 kuvaa) tai motoristi_kanto (15 kuvaa) puuttuu.\n\n" + string.Join("\n", report)); return; }
            EditorUtility.SetDirty(e);
            n++;
        }
        if (n > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Info($"{n} prätkäjätkää heittää nyt pöytiä (hakee lähimmän ehjän pöydän, kantaa sen pään yllä ja heittää) ja napaten heron (25 % todennäköisyys, heitto eteen). Tappelussa muita vastaan (esim. aavikkobaari) he heittävät myös kevyitä vihuja.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/100. Prätkäjätkien äänet ja nopeampi juoksu (kaikille sceneen)")]
    static void UpdateBikerAudio()
    {
        int n = 0;
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (e == null || e.displayName != "Prätkäjätkä") continue;
            Undo.RecordObject(e, "Prätkäjätkän äänet");
            ApplyBikerAudio(e);
            if (e.runSprites != null && e.runSprites.Length >= 8) { e.runFrameTime = 0.085f; e.runSpeedMultiplier = 2.5f; }
            EditorUtility.SetDirty(e);
            n++;
        }
        if (n > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Info($"Äänet, hitaampi jalkojen liike ja hieman nopeampi vauhti (2,5-kertainen) päivitetty {n} prätkäjätkälle.\n\nTallenna scene (Ctrl+S).");
    }

    static Enemy MakeBiker(Transform parent)
    {
        var report = new List<string>();
        foreach (var n in new[] { "motoristi_idle", "motoristi_kavely", "motoristi_lyonti", "motoristi_suora", "motoristi_potku", "motoristi_hyppylyonti", "motoristi_juoksu", "motoristi_kaatuminen", "motoristi_ylosnousu", "motoristi_heitetty" })
        {
            string path = FindTexture(n);
            if (path != null) SetupAndSlice(path);
        }
        var idle = EnemySheet("motoristi_idle", report);
        if (idle.Length == 0) return null;
        var go = new GameObject("Prätkäjätkä");
        go.transform.SetParent(parent, false);
        var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
        var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
        var e = go.AddComponent<Enemy>();
        e.body = visual; e.shadow = shadow; visual.sprite = idle[0];
        e.displayName = "Prätkäjätkä";
        e.idleSprites = idle; e.idleFrameTime = 0.12f;
        var walk = EnemySheet("motoristi_kavely", report);
        e.walkSprites = walk.Length > 0 ? walk : idle; e.walkFrameTime = 0.09f;
        var jab = EnemySheet("motoristi_lyonti", report);   // 10 kuvaa: veto, käsi ojennettuna (5), palautus
        if (jab.Length >= 8) { e.punchSprites = jab; e.punchImpactFrame = 5; e.secondImpactFrame = -1; e.windupTime = 0.18f; e.punchRecoverTime = 0.45f; e.punchDamage = 10; }
        var cross = EnemySheet("motoristi_suora", report);   // 10 kuvaa: pitkä suora (ojennettuna 2–6), kaataa
        if (cross.Length >= 8) { e.punch2Sprites = cross; e.punch2ImpactFrame = 2; e.punch2Damage = 16; e.punch2Knockdown = true; e.punch2Chance = 0.3f; e.punch2LaunchUp = 0f; }
        var kick = EnemySheet("motoristi_potku", report);    // 11 kuvaa (solu 640 leveä): polvi ylös, potku ojennettuna (3–7)
        if (kick.Length >= 8)
        {
            e.punch3Sprites = kick; e.punch3ImpactFrame = 3; e.punch3WindupTime = 0.3f; e.punch3RecoverTime = 0.5f;
            e.punch3Damage = 18; e.punch3Knockdown = true; e.punch3LaunchX = 5f; e.punch3LaunchUp = 4f; e.punch3Chance = 0.2f; e.punch3Reach = 2.4f;
        }
        // erikoisliike: hyppylyönti kaukaa (kyykky ja ponnistus, lento nyrkki edellä, alastulo); joskus myös vierestä
        var leap = EnemySheet("motoristi_hyppylyonti", report);   // 15 kuvaa: 0–7 kyykky ja ponnistus, 8 lento (isku), 9–14 alastulo
        SetBikerCharge(e, leap);
        var run = EnemySheet("motoristi_juoksu", report);   // 10 kuvaa, kaukana juostaan
        if (run.Length >= 8) { e.runSprites = run; e.runFrameTime = 0.085f; e.runSpeedMultiplier = 2.5f; }
        ApplyBikerThrow(e, report);   // pöytien ja heron nosto, kanto ja heitto
        ApplyPieArt(e, report);       // piirakan osuma- ja heittokuvat (ei tarvitse valikkoa 105)
        var fall = EnemySheet("motoristi_kaatuminen", report);
        if (fall.Length >= 8)
        {
            // 0 osuma, 1 pää taakse, 2–3 horjuu, 4–5 kaatuu eteen, 6–7 makaa mahallaan
            e.hurtSprites = new[] { fall[1] };
            e.knockdownSprites = new[] { fall[1], fall[2], fall[3], fall[4], fall[5], fall[7] };
            e.landSprites = new[] { fall[6], fall[7] }; e.landFrameTime = 0.12f;
        }
        var up = EnemySheet("motoristi_ylosnousu", report);
        if (up.Length >= 3) { e.getUpSprites = up; e.getUpTime = 1.2f; }   // 8 kuvaa: mahallaan → kontallaan → polvelta → tappeluasento
        e.bigBody = true;   // hero heittää kuperkeikalla (kuten Kovista)
        var thrown = EnemySheet("motoristi_heitetty", report);   // 0 asento, 1 napattu, 2 kippaa, 3 ylösalaisin, 4–5 selällään, 6 pyörähtää, 7 mahallaan
        if (thrown.Length >= 8) { e.flipThrownSprites = thrown; e.flipFlightFrames = 1; e.flipFlightFrameTime = 0.12f; e.flipLandFrameTime = 0.13f; }
        e.fightsEveryone = true;   // käy kaikkien kimppuun (puliukon tapaan)
        e.maxHealth = 160; e.attackRange = 1.9f; e.attackCooldown = 0.9f;
        e.blockChance = 0f;   // prätkäjätkä ei torju: ottaa iskut vastaan ja lyö takaisin
        e.moveSpeedX = 2.8f; e.moveSpeedY = 1.4f;
        ApplyBikerAudio(e);
        e.wakeDistance = 100f; e.retreatChance = 0f;
        Undo.RegisterCreatedObjectUndo(go, "Prätkäjätkä");
        Debug.Log("Prätkäjätkä: " + string.Join(", ", report));
        return e;
    }

    /// Uusi rokkimimmi (rokkari2_*.png, 768 × 512): taisteluidle, lyöntisarja (etukäsi + takakäsi), sivupotku,
    /// hyppypotku (syöksy kauempaa, kaataa), pyörähdyspotku (kaataa), kävely ilman suojausta ja rento idle.
    /// Osuma, kaatuminen, ylösnousu (selältä ensin vatsalleen), torjunta ja heron niskalenkki omista kuvistaan.
    static bool ApplyRocker2(Enemy e, List<string> report)
    {
        foreach (var n in new[] { "rokkari2_taisteluidle", "rokkari2_lyonnit", "rokkari2_sivupotku", "rokkari2_hyppypotku", "rokkari2_pyorahdys", "rokkari2_kavely", "rokkari2_idle", "rokkari2_taistelukavely", "rokkari2_kaatuminen", "rokkari2_ylosnousu", "rokkari2_torjunta", "rokkari2_niskalenkki", "rokkari2_niskalenkki_lento", "rokkari2_kaanto" })
        {
            string path = FindTexture(n);
            if (path != null) SetupAndSlice(path);
        }
        var idle = EnemySheet("rokkari2_taisteluidle", report);
        if (idle.Length == 0) return false;
        e.idleSprites = idle; e.idleFrameTime = 0.16f;
        var calm = EnemySheet("rokkari2_idle", report);         // rento seisonta kädet alhaalla ennen tappelua
        e.calmIdleSprites = calm.Length > 0 ? calm : idle;
        if (e.body != null) e.body.sprite = e.calmIdleSprites[0];
        var punch = EnemySheet("rokkari2_lyonnit", report);     // 14 kuvaa: etukäsi ojennettuna 5, takakäsi 9
        if (punch.Length >= 12)
        {
            e.punchSprites = punch; e.punchImpactFrame = 5; e.secondImpactFrame = 9; e.secondImpactPunchOnly = true;
            e.windupTime = 0.22f; e.punchRecoverTime = 0.55f; e.punchDamage = 7;
        }
        var side = EnemySheet("rokkari2_sivupotku", report);    // 14 kuvaa: ojennettuna 6
        if (side.Length >= 10) { e.punch2Sprites = side; e.punch2ImpactFrame = 6; e.punch2Damage = 12; e.punch2Knockdown = false; e.punch2Chance = 0.3f; e.punch2LaunchUp = 0f; }
        var jump = EnemySheet("rokkari2_hyppypotku", report);   // 17 kuvaa: hyppy, polvi ylös, potku ojennettuna 9
        if (jump.Length >= 12)
        {
            e.altAttackSprites = jump; e.altImpactFrame = 9; e.altDamage = 14; e.altKnockdown = true; e.altKnockSpeed = 6f; e.altKnockUp = 5f;
            e.altChance = 0.25f; e.altExtraWindup = 0.3f; e.altReach = 2.4f; e.altLungeSpeed = 5f; e.altLungeTime = 0.25f;
            e.chargeRange = 3.8f; e.chargeMinRange = 2.4f; e.altNearChance = 0.1f; e.altJumpHeight = 0f; e.altTimeScale = 1f;
        }
        var spin = EnemySheet("rokkari2_pyorahdys", report);    // 16 kuvaa: kääntyy selkä edellä, korkea potku ojennettuna 8
        if (spin.Length >= 12)
        {
            e.punch3Sprites = spin; e.punch3ImpactFrame = 8; e.punch3WindupTime = 0.45f; e.punch3RecoverTime = 0.55f;
            e.punch3Damage = 16; e.punch3Knockdown = true; e.punch3LaunchX = 6f; e.punch3LaunchUp = 5f; e.punch3Chance = 0.15f; e.punch3Reach = 2.5f;
            e.punch3Heavy = false;
        }
        // kombo: lyöntisarja, sivupotku, pyörähdyspotku (menee loppuun)
        e.combos = new[] { "JSK", "JK" }; e.comboChance = 0.4f; e.comboArmor = true; e.comboWindupScale = 0.7f; e.comboGap = 0.08f;
        // neutraali kävely ilman suojausta ennen tappelua (14 kuvaa); tappelussa suojattu kävely, jos tehty (rokkari2_taistelukavely)
        var walk2 = EnemySheet("rokkari2_kavely", report);
        var fightWalk = EnemySheet("rokkari2_taistelukavely", report);
        e.calmWalkSprites = walk2; e.calmWalkFrameTime = 0.07f;
        e.walkSprites = fightWalk.Length > 0 ? fightWalk : walk2.Length > 0 ? walk2 : idle;
        e.walkFrameTime = fightWalk.Length > 0 ? 0.075f : walk2.Length > 0 ? 0.065f : 0.16f;   // taistelukävely: 12 kuvaa, kaksi askelta (jalat vuorotellen) ~0,9 s
        // kaatuminen: 0 asento, 1 pää taakse (osuma), 2 kaatuu taakse, 3 kiertyy ilmassa, 4 vatsallaan ilmassa, 5–6 alastulo, 7–8 makaa
        var fall2 = EnemySheet("rokkari2_kaatuminen", report);
        if (fall2.Length >= 9)
        {
            e.hurtSprites = new[] { fall2[1] };
            e.knockdownSprites = new[] { fall2[1], fall2[2], fall2[3], fall2[4], fall2[8] };   // ilmassa 1–4, viimeinen makuu
            e.landSprites = new[] { fall2[5], fall2[6], fall2[7] }; e.landFrameTime = 0.1f;
        }
        // ylösnousu vatsaltaan: punnerrus, kontallaan, polvelta tappeluasentoon; kuvissa katse vasemmalle -> kääntyy lopussa
        var up2 = EnemySheet("rokkari2_ylosnousu", report);
        if (up2.Length >= 8) { e.getUpSprites = up2; e.getUpTime = 1.0f; e.getUpFacesBack = true; }
        // heiton jälkeen selällään: ensin kääntyy vatsalleen (4 kuvaa), sitten tavallinen ylösnousu
        var roll2 = EnemySheet("rokkari2_kaanto", report);
        if (roll2.Length >= 4) { e.getUpRollSprites = roll2; e.getUpRollTime = 0.45f; }
        // torjunta kuten lippiksellä (kun kuvat on tehty): kädet ylös, suoja, paluu; enintään 2 peräkkäin
        var block2 = EnemySheet("rokkari2_torjunta", report);
        if (block2.Length > 0) { e.blockSprites = block2; e.blockTime = 0.5f; e.blockChance = 0.3f; e.maxBlocksInRow = 2; }
        else e.blockChance = 0f;   // ilman kuvia ei torju (idle-kuva näyttäisi oudolta)
        // heron niskalenkki: ote ja heitto (8) sekä lento ja alastulo (6); ilman uusia kuvia vanhat
        var hl2 = EnemySheet("rokkari2_niskalenkki", report);
        var hlf2 = EnemySheet("rokkari2_niskalenkki_lento", report);
        if (hl2.Length >= 8 && hlf2.Length >= 6) { e.headlockThrownSprites = hl2; e.headlockFlightSprites = hlf2; }
        // ei käy päälle itsestään: herää, kun lähellä syttyy tappelu tai kun häntä lyödään
        e.wakeDistance = -1f; e.wakeOnFightRadius = 5f;
        return true;
    }

    [MenuItem("Beat em up/73. Uusi rokkimimmi kaikille rokkimimmeille")]
    static void Rocker2All()
    {
        var report = new List<string>();
        int n = 0;
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (e == null || e.displayName != "Rokkimimmi") continue;
            Undo.RecordObject(e, "Uusi rokkimimmi");
            if (e.body != null) Undo.RecordObject(e.body, "Uusi rokkimimmi");
            if (!ApplyRocker2(e, report)) { Info("rokkari2_*.png puuttuu."); return; }
            // S-Clubin rokkimimmit tepastelevat neutraalisti paikkansa lähellä (kadun baarin rokkimimmit odottavat tiskillä)
            if (e.gameObject.name.Contains("S-Club") && e.wanderMaxX <= e.wanderMinX)
            { e.wanderMinX = e.transform.position.x - 1.5f; e.wanderMaxX = e.transform.position.x + 1.5f; }
            EditorUtility.SetDirty(e); n++;
        }
        if (n > 0) EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Info($"Uusi rokkimimmi: {n} kpl päivitetty.\n\n" + string.Join("\n", report.Distinct()) + "\n\nTallenna scene (Ctrl+S).");
    }

    /// Rokkimimmi (rokkari_*.png): neutraali idle ja kävely, etukäden ja takakäden lyönnit, etupotku ja korkea potku,
    /// kombona koko sarja (etukäsi, takakäsi, etukäsi, etupotku, korkea potku). Taisteluidle, -kävely ja ylösnousu tulossa.
    static Enemy MakeRocker(Transform parent)
    {
        var report = new List<string>();
        foreach (var n in new[] { "rokkari_idle", "rokkari_kavely", "rokkari_lyonti", "rokkari_suora", "rokkari_potku", "rokkari_korkea", "rokkari_kaatuminen",
                                  "rokkari_taisteluidle", "rokkari_taistelukavely", "rokkari_ylosnousu", "rokkari_niskalenkki", "rokkari_niskalenkki_lento" })
        {
            string path = FindTexture(n);
            if (path != null) SetupAndSlice(path);
        }
        var idle = EnemySheet("rokkari_idle", report);
        if (idle.Length == 0) return null;
        var go = new GameObject("Rokkimimmi");
        go.transform.SetParent(parent, false);
        var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
        var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
        var e = go.AddComponent<Enemy>();
        e.body = visual; e.shadow = shadow; visual.sprite = idle[0];
        e.displayName = "Rokkimimmi";
        var fightIdle = EnemySheet("rokkari_taisteluidle", report);
        e.idleSprites = fightIdle.Length > 0 ? fightIdle : idle; e.idleFrameTime = 0.14f;
        e.calmIdleSprites = idle;   // neutraali asento ennen tappelua
        var walk = EnemySheet("rokkari_taistelukavely", report);
        if (walk.Length == 0) walk = EnemySheet("rokkari_kavely", report);
        e.walkSprites = walk.Length > 0 ? walk : idle; e.walkFrameTime = 0.08f;
        var jab = EnemySheet("rokkari_lyonti", report);      // 10 kuvaa: etukäsi, ojennettuna 4
        if (jab.Length >= 8) { e.punchSprites = jab; e.punchImpactFrame = 4; e.secondImpactFrame = -1; e.windupTime = 0.12f; e.punchRecoverTime = 0.35f; e.punchDamage = 8; }
        var cross = EnemySheet("rokkari_suora", report);     // 11 kuvaa: takakäsi, ojennettuna 5
        if (cross.Length >= 8) { e.punch2Sprites = cross; e.punch2ImpactFrame = 5; e.punch2Damage = 12; e.punch2Knockdown = false; e.punch2Chance = 0.3f; e.punch2LaunchUp = 0f; }
        var kick = EnemySheet("rokkari_potku", report);      // 10 kuvaa: etupotku, ojennettuna 4
        if (kick.Length >= 8) { e.altAttackSprites = kick; e.altImpactFrame = 4; e.altDamage = 12; e.altKnockdown = false; e.altChance = 0.25f; e.altReach = 2.3f; e.altLungeSpeed = 0f; }
        var high = EnemySheet("rokkari_korkea", report);     // 13 kuvaa: korkea potku, ojennettuna 3 (kombon viimeinen, kaataa)
        if (high.Length >= 8)
        {
            e.punch3Sprites = high; e.punch3ImpactFrame = 3; e.punch3WindupTime = 0.25f; e.punch3RecoverTime = 0.5f;
            e.punch3Damage = 16; e.punch3Knockdown = true; e.punch3LaunchX = 5f; e.punch3LaunchUp = 5f; e.punch3Chance = 0.1f; e.punch3Reach = 2.4f;
        }
        // kombo: koko sarja (etukäsi, takakäsi, etukäsi, etupotku, korkea potku); menee loppuun
        e.combos = new[] { "JSJPK" }; e.comboChance = 0.5f; e.comboArmor = true; e.comboWindupScale = 0.7f; e.comboGap = 0.08f;
        var fall = EnemySheet("rokkari_kaatuminen", report); // 0 asento, 1 pää taakse, 2–4 horjuu, 5 kaatuu, 6–7 selällään
        if (fall.Length >= 8)
        {
            e.hurtSprites = new[] { fall[1] };
            e.knockdownSprites = new[] { fall[1], fall[2], fall[3], fall[5], fall[7] };
            e.landSprites = new[] { fall[6], fall[7] }; e.landFrameTime = 0.12f;
        }
        var up = EnemySheet("rokkari_ylosnousu", report);
        if (up.Length >= 3) { e.getUpSprites = up; e.getUpTime = 1.0f; }   // 8 kuvaa: selällään, kieräys, kontallaan, tappeluasento
        // heron niskalenkki: ote, veto, lento vaakatasossa, pyörähdys ja alastulo selälleen
        e.bigBody = false;
        e.headlockThrownSprites = EnemySheet("rokkari_niskalenkki", report);
        e.headlockFlightSprites = EnemySheet("rokkari_niskalenkki_lento", report);
        e.maxHealth = 90; e.attackRange = 1.8f; e.attackCooldown = 0.8f; e.blockChance = 0f;
        e.moveSpeedX = 3.2f; e.moveSpeedY = 1.6f;   // nopea ja ketterä
        e.wakeDistance = 8f; e.retreatChance = 0.1f;
        ApplyRocker2(e, report);   // uusi ulkoasu (farkkuliivi, cowboy-saappaat), jos kuvat on tehty
        Undo.RegisterCreatedObjectUndo(go, "Rokkimimmi");
        ApplyPieArt(e, report);
        Debug.Log("Rokkimimmi: " + string.Join(", ", report));
        return e;
    }

    /// Snookerpöytä (biljardipoyta.png): kiinteä este (hypyllä yli), 8 punaista + värit + valkoinen (poimittavia ja heitettäviä
    /// kuten pullot) ja keppi verkalla (ase, kestää 10 lyöntiä). pos = etujalkojen keskikohta, width = leveys yksiköinä.
    static GameObject AddSnookerTable(Transform parent, Vector2 pos, float width)
    {
        var poolSprite = ImportProp("Assets/Sprites/Rekvisiitta/biljardipoyta.png");
        if (poolSprite == null) return null;
        var pgo = new GameObject("Biljardipöytä");
        pgo.transform.SetParent(parent, false);
        pgo.transform.position = new Vector3(pos.x, pos.y, 0f);
        var psr0 = new GameObject("Visual").AddComponent<SpriteRenderer>(); psr0.transform.SetParent(pgo.transform, false);
        psr0.sprite = poolSprite;
        float pk = width / poolSprite.bounds.size.x;
        psr0.transform.localScale = new Vector3(pk, pk, 1f);
        var psh = new GameObject("Shadow").AddComponent<SpriteRenderer>(); psh.transform.SetParent(pgo.transform, false);
        psh.sprite = PlayerController.CreateShadowSprite(); psh.color = new Color(0f, 0f, 0f, 0.35f);
        psh.transform.localPosition = new Vector3(0f, 0.37f, 0f); psh.transform.localScale = new Vector3(width * 1.05f, 0.9f, 1f);
        psh.sortingOrder = -9000;
        var ob = pgo.AddComponent<Obstacle>(); ob.body = psr0; ob.halfWidth = width * 0.48f; ob.depth = 0.75f;
        // hajoaa vähitellen potkuista ja lyönneistä (6 vaihetta, sama koko kuin ehjä kuva); pallot ja keppi valahtavat lattialle
        var dmg = new List<Sprite> { poolSprite };
        for (int k = 1; k <= 5; k++) { var ds = ImportProp($"Assets/Sprites/Rekvisiitta/biljardipoyta_vaurio{k}.png"); if (ds != null) dmg.Add(ds); }
        if (dmg.Count == 6)
        {
            ob.damageSprites = dmg.ToArray(); ob.maxHealth = 160;
            ob.hitSounds = AssetDatabase.FindAssets("t:AudioClip puu", new[] { "Assets/Audio" }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(q => Path.GetFileNameWithoutExtension(q).ToLowerInvariant().StartsWith("puu")).Select(AssetDatabase.LoadAssetAtPath<AudioClip>).Where(clip => clip != null).ToArray();
        }
        // pallot (snooker: 15 punaista kolmiossa + valkoinen) ja keppi pöydän verkalla
        var tops = new List<SpriteRenderer>();
        System.Func<float, float, Vector3> cloth = (ix, iy) => new Vector3((ix - poolSprite.rect.width * 0.5f) / poolSprite.pixelsPerUnit * pk, (poolSprite.rect.height - iy) / poolSprite.pixelsPerUnit * pk, 0f);
        SpriteRenderer Put(Sprite sp, Vector3 lp, float size, float rot = 0f)
        {
            var r0 = new GameObject(sp.name).AddComponent<SpriteRenderer>(); r0.transform.SetParent(pgo.transform, false);
            r0.sprite = sp; r0.transform.localPosition = lp; r0.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
            float sc = size / sp.bounds.size.x; r0.transform.localScale = new Vector3(sc, sc, 1f);
            tops.Add(r0); return r0;
        }
        // snooker: 8 punaista kolmiossa (rivit 1-2-3-2), värit omilla paikoillaan, valkoinen aloitusalueella.
        // Pallot ovat poimittavia (L2, kuten pullo) ja heitettäviä: lentävät kauas, eivät hajoa.
        Sprite Ball(string ballName) => ImportPropCentered("Assets/Sprites/Rekvisiitta/snooker_" + ballName + ".png");
        const float BallSize = 0.165f;   // kuva ääriviivoineen (pallo 96/116): pallo n. 15 % pienempi kuin ennen
        float midLocalY = cloth(800f, 245f).y;
        void AddBall(Sprite bs, Vector3 local)
        {
            // maan kohta (syvyys) pöydän sisällä, korkeus = pallon alareuna verkalla
            float depthOff = 0.37f + (local.y - midLocalY);
            var bgo = new GameObject("Snookerpallo " + bs.name);
            bgo.transform.SetParent(pgo.transform, false);
            var b = bgo.AddComponent<Bottle>();
            b.sprites = new[] { bs };
            b.ball = true; b.pivotY = 0f; b.scale = BallSize / bs.bounds.size.x;
            b.ballTableScale = 1.2f; b.ballFlightScale = 1.75f;   // kädessä ja lennossa isompi (näkyy), mutta pienempi kuin ennen (2)
            b.throwDamage = 14; b.throwSpeed = 22f;
            b.restOnRenderer = psr0; b.pickRangeX = 1.0f; b.pickRangeY = 1.1f;
            bgo.transform.position = new Vector3(pgo.transform.position.x + local.x, pgo.transform.position.y + depthOff, 0f);
            b.restHeight = pgo.transform.position.y + local.y - BallSize * 0.5f - bgo.transform.position.y;
        }
        var red = Ball("punainen");
        Vector3 apex = cloth(1180f, 245f);
        if (red != null)
        {
            int[] rowCount = { 1, 2, 3, 2 };
            for (int row = 0; row < rowCount.Length; row++)
                for (int j = 0; j < rowCount[row]; j++)
                {
                    float ly = (j - (rowCount[row] - 1) * 0.5f) * BallSize * 0.62f;   // pallot piirretään pöydällä 1,25 × (Bottle.ballTableScale)
                    AddBall(red, apex + new Vector3(row * BallSize * 1.09f, ly, 0f));
                }
        }
        foreach (var (bn, ix, dy) in new[] { ("pinkki", 1150f, 0f), ("musta", 1500f, 0f), ("sininen", 880f, 0f),
                                            ("ruskea", 470f, 0f), ("keltainen", 470f, -0.18f), ("vihrea", 470f, 0.18f), ("valkoinen", 360f, 0.08f) })
        {
            var bsp = Ball(bn);
            if (bsp != null) AddBall(bsp, new Vector3(cloth(ix, 245f).x, midLocalY + dy * 0.5f, 0f));
        }
        var cue = ImportPropCentered("Assets/Sprites/Rekvisiitta/biljardikeppi.png");
        if (cue != null)
        {
            var cueR = Put(cue, cloth(700f, 300f), width * 0.42f, -4f);
            var cueC = cueR.gameObject.AddComponent<Cue>(); cueC.body = cueR; cueC.durability = 10;
            cueC.breakSounds = AssetDatabase.FindAssets("t:AudioClip puu", new[] { "Assets/Audio" }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(q => Path.GetFileNameWithoutExtension(q).ToLowerInvariant().StartsWith("puu")).Select(AssetDatabase.LoadAssetAtPath<AudioClip>).Where(clip => clip != null).ToArray();
        }
        ob.onTop = tops.ToArray();
        return pgo;
    }

    [MenuItem("Beat em up/67. Pokerihuone (baarin takahuone)")]
    static void CreateBarRoom()
    {
        var bar = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Kadun baari");
        var barBg = GameObject.Find("Kadun baari");
        var ti = AssetImporter.GetAtPath(BarRoomPath) as TextureImporter;
        if (bar == null || barBg == null || ti == null) { Info("Tarvitaan kadun baari (kohta 65) ja kuva " + BarRoomPath); return; }
        foreach (var n in new[] { "Baarin sisä", "Baarin ovet", "Baarin pokeri", "Baarin pokeri loppu", "Pokerihuoneen tappelu", "Pokerihuoneen pöydät" })
        {
            var o = GameObject.Find(n);
            if (o != null) Undo.DestroyObjectImmediate(o);
        }
        ti.GetSourceTextureWidthAndHeight(out int srcW, out int srcH);
        float ppu = PokerRoomPPU;   // laajennettu kuva täyttää ruudun leveyden; videon hahmot n. 1.3 × hero
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = ppu;
        ti.filterMode = FilterMode.Bilinear;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.maxTextureSize = 4096;
        ti.mipmapEnabled = false;
        var st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        st.spriteMeshType = SpriteMeshType.FullRect;
        st.spriteAlignment = (int)SpriteAlignment.Center;
        ti.SetTextureSettings(st);
        ti.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BarRoomPath);
        float wU = sprite.rect.width / ppu, hU = sprite.rect.height / ppu;

        var bg = new GameObject("Baarin sisä");
        var sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -10000;
        // kuvan yläreuna ruudun yläreunassa (kamera takaseinän luona); lattiaa alas, kamera seuraa
        float top = CamY + CamHalf;
        bg.transform.position = new Vector3(BarRoomX0 + wU * 0.5f, top - hU * 0.5f, 0f);
        Undo.RegisterCreatedObjectUndo(bg, "Baarin sisä");
        // huoneen koordinaatit (alkuperäisen 1536 × 1024 -kuvan pikselit) laajennetun kuvan sisällä
        System.Func<float, float, Vector2> pt = (px, row) => new Vector2(BarRoomX0 + (px + PokerRoomOffset.x) / ppu, top - (row + PokerRoomOffset.y) / ppu);
        SpriteRenderer extSr = null; Sprite fightExt = null;   // laajennettu kuva korvaa erilliset reunakuvat

        var area = ReuseArea("Alue: Baari");   // sama alue säilyy uudelleenrakennuksessa
        area.areaName = "Baari";
        area.useSidewalk = false;
        area.backScale = 0.85f;   // syvä huone: hahmot takaseinällä 85 % (ovi ei jää liian pieneksi)
        area.maxDepthY = top - (BarRoomWallRow + PokerRoomOffset.y) / ppu;
        float bottom = top - hU;
        area.minDepthY = bottom + 0.7f;
        area.walkMinX = BarRoomX0 + 1.4f; area.walkMaxX = BarRoomX0 + wU - 1.4f;   // kuvan reunoissa sivuseinien nurkat: hahmo kokonaan kuvan sisällä
        // kamera: ei zoomia; takaseinän luona kuvan yläreuna ruudun yläreunassa, edessä kamera laskee kuvan alareunaan
        float frontCam = bottom + CamHalf;
        area.camSize = 0f; area.camOffsetY = frontCam - CamY; area.camRiseY = Mathf.Max(0.01f, CamY - frontCam);
        float halfW = CamHalf * 16f / 9f;
        area.camMinX = BarRoomX0 + Mathf.Min(halfW, wU * 0.5f); area.camMaxX = BarRoomX0 + Mathf.Max(wU - halfW, wU * 0.5f);
        // pokeripöytä keskellä ja lipasto oikealla ovat lähempänä kuin seinä
        area.depthLimits = new[] { pt(PokerLeftWallEdge.x, PokerLeftWallEdge.y), pt(PokerLeftWallCorner.x, PokerLeftWallCorner.y), pt(330f, BarRoomWallRow), pt(400f, BarRoomTableRow), pt(1290f, BarRoomTableRow),
                                   pt(1350f, BarRoomCabinetRow), pt(1560f, BarRoomCabinetRow), pt(1600f, BarRoomWallRow),
                                   pt(PokerRightWallCorner.x, PokerRightWallCorner.y), pt(PokerRightWallEdge.x, PokerRightWallEdge.y) };

        // tappelun tausta (tyhjä huone: pöytä nurin, kortit lattialla), sama rajaus kuin pokerihuoneen kuvassa
        Sprite fightSprite = null;
        var fti = AssetImporter.GetAtPath(PokerFightBgPath) as TextureImporter;
        if (fti != null)
        {
            fti.textureType = TextureImporterType.Sprite; fti.spriteImportMode = SpriteImportMode.Single;
            fti.spritePixelsPerUnit = ppu; fti.filterMode = FilterMode.Bilinear;
            fti.textureCompression = TextureImporterCompression.Uncompressed; fti.maxTextureSize = 4096; fti.mipmapEnabled = false;
            fti.SetTextureSettings(st); fti.spritePixelsPerUnit = ppu; fti.SaveAndReimport();
            fightSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PokerFightBgPath);
        }
        SpriteLoop pokerLoop = null;

        // pokerinpelaajat (idle-silmukka taustan päällä)
        string pp = FindTexture("baari_poker_idle");
        if (pp != null)
        {
            SetupAndSlice(pp);
            var fr = LoadSprites("baari_poker_idle").OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
            if (fr.Length >= 2)
            {
                var pg = new GameObject("Baarin pokeri");
                float k100 = fr[0].pixelsPerUnit / ppu;
                Vector2 foot = pt(PokerCellX + PokerCellW * 0.5f, PokerCellY + PokerCellH);
                pg.transform.position = new Vector3(foot.x, foot.y, 0f);
                pg.transform.localScale = new Vector3(k100, k100, 1f);
                var psr = pg.AddComponent<SpriteRenderer>(); psr.sprite = fr[0]; psr.sortingOrder = -9990;
                var loop = pg.AddComponent<SpriteLoop>(); loop.frames = fr; loop.fps = PokerFps; pokerLoop = loop;
                // toinen idle-video (vaihtuu ristihäivytyksellä) ja sen napsahdusäänet silmukkana
                if (FindTexture("baari_poker_idle2") != null)
                {
                    SetupAndSlice(FindTexture("baari_poker_idle2"));
                    loop.altFrames = LoadSprites("baari_poker_idle2").OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
                }
                loop.ambience = AssetDatabase.LoadAssetAtPath<AudioClip>(PokerAmbiencePath);
                Undo.RegisterCreatedObjectUndo(pg, "Pokeri");
                // loppuanimaatio: pelaajat nousevat, pöytä kaatuu ja kortit lentävät (alkaa n. 10 s idlen jälkeen)
                string lp = FindTexture("baari_poker_loppu");
                if (lp != null)
                {
                    SetupAndSlice(lp);
                    var lf = LoadSprites("baari_poker_loppu").OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
                    if (lf.Length >= 2)
                    {
                        var fg = new GameObject("Baarin pokeri loppu");
                        float kf = lf[0].pixelsPerUnit / ppu * (PokerFinaleRect.z / 854f);   // solu tallennettu pienennettynä
                        Vector2 ffoot = pt(PokerFinaleRect.x + PokerFinaleRect.z * 0.5f, PokerFinaleRect.y + PokerFinaleRect.w);
                        fg.transform.position = new Vector3(ffoot.x, ffoot.y, 0f);
                        fg.transform.localScale = new Vector3(kf, kf, 1f);
                        var fsr = fg.AddComponent<SpriteRenderer>(); fsr.sprite = lf[0]; fsr.sortingOrder = -9988; fsr.enabled = false;
                        loop.finaleRenderer = fsr; loop.finale = lf; loop.finaleFps = 12f; loop.finaleAfter = 10f;
                        loop.finaleSound = AssetDatabase.LoadAssetAtPath<AudioClip>(PokerFinaleSoundPath);
                        Undo.RegisterCreatedObjectUndo(fg, "Pokerin loppu");
                    }
                }
            }
        }

        // pullopöydät tuoleineen laajennetuille sivuille ja eteen (alkuperäisen huoneen ulkopuolelle: animaatiot eivät peitä)
        var tables = new GameObject("Pokerihuoneen pöydät");
        Undo.RegisterCreatedObjectUndo(tables, "Pokerihuoneen pöydät");
        var tableInfo = AddBottleTables(tables.transform, PokerTablePx.Select(v => { var q = pt(v.x, v.y); return new Vector3(q.x, q.y, 0f); }), 31);
        // iso biljardipöytä (snooker) huoneen etuosaan: kiinteä este, hypyllä yli
        // kaksi Kovista huoneen etuosassa: tappelua sillä aikaa, kun jätkät pelaavat korttia
        var kovisT = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(en => en != null && en.gameObject.name == "Kovis");
        int pokerKovis = 0;
        if (kovisT != null)
            foreach (var kp in PokerKovisPx)
            {
                var kgo = Object.Instantiate(kovisT.gameObject, tables.transform);
                kgo.name = "Kovis (pokerihuone) " + (++pokerKovis);
                Vector2 kq = pt(kp.x, kp.y);
                kgo.transform.position = new Vector3(kq.x, kq.y, 0f);
                var ke = kgo.GetComponent<Enemy>(); ke.wakeDistance = 7f; ke.joinsFightWhenSquadComes = false;
                kgo.SetActive(true);
            }
        if (AddSnookerTable(tables.transform, pt(PokerPoolPx.x, PokerPoolPx.y), PokerPoolWidth) != null)
        {
            // Roccon keppikuvat (seisonta keppi kädessä)
            var pcCue = Object.FindFirstObjectByType<PlayerController>();
            string kip = FindTexture("keppi_idle");
            if (pcCue != null && kip != null)
            {
                SetupAndSlice(kip);
                Undo.RecordObject(pcCue, "Keppi");
                pcCue.cueIdleSprites = LoadSprites("keppi_idle").OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
                foreach (var kn in new[] { "keppi_lyonti_a", "keppi_lyonti_b", "keppi_kavely", "keppi_heitto" }) { string kp2 = FindTexture(kn); if (kp2 != null) SetupAndSlice(kp2); }
                pcCue.cueSwingASprites = LoadSprites("keppi_lyonti_a").OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
                pcCue.cueWalkSprites = LoadSprites("keppi_kavely").OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
                pcCue.cueThrowSprites = LoadSprites("keppi_heitto").OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
                pcCue.cueSwingBSprites = LoadSprites("keppi_lyonti_b").OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
                pcCue.cueReach = 3.6f;   // keppi osuu kauempaa
                EditorUtility.SetDirty(pcCue);
            }
        }

        // ovet: baarin oikean nurkan teräsovesta (E) pokerihuoneeseen, pokerihuoneen teräsovesta takaisin baariin
        var doors = new GameObject("Baarin ovet");
        Undo.RegisterCreatedObjectUndo(doors, "Ovet");
        var bsr = barBg.GetComponent<SpriteRenderer>();
        float bppu = bsr.sprite.pixelsPerUnit, bleft = bsr.bounds.min.x, btop = bsr.bounds.max.y;
        Vector2 barBack = new Vector2(bleft + StreetBarBackDoorPx.x / bppu, btop - StreetBarBackDoorPx.y / bppu);
        var d = new GameObject("Pokerihuoneen ovi").AddComponent<Door>();
        d.transform.SetParent(doors.transform, false);
        d.transform.position = new Vector3(barBack.x, barBack.y, 0f);
        d.prompt = "Mene pokerihuoneeseen";
        d.here = bar; d.target = area;
        d.spawnPoint = new Vector2(pt(BarRoomExitPx + 120f, 0f).x, area.maxDepthY - 1.0f);   // teräsoven eteen (huoneen koordinaatit laajennetussa kuvassa)
        d.halfWidth = 1.4f; d.maxDistanceFromWall = 1.4f;
        var exit = new GameObject("Pokerihuoneesta baariin").AddComponent<Door>();
        exit.transform.SetParent(doors.transform, false);
        exit.transform.position = new Vector3(pt(BarRoomExitPx, 0f).x, area.maxDepthY, 0f);
        exit.prompt = "Takaisin baariin";
        exit.here = area; exit.target = bar;
        exit.halfWidth = 1.8f; exit.maxDistanceFromWall = 1.2f;
        exit.spawnPoint = new Vector2(barBack.x - 0.8f, btop - 650f / bppu);   // takaoven eteen, vähän vasemmalle (viisto seinä)
        exit.blockedDuringFight = true;   // tappelusta ei karata

        // tappelu: loppuanimaation jälkeen musta häivytys, tyhjä huone, pelin tuolit ja vastustajat
        string fightInfo = "";
        if (pokerLoop != null && fightSprite != null)
        {
            var froot = new GameObject("Pokerihuoneen tappelu");
            Undo.RegisterCreatedObjectUndo(froot, "Pokeritappelu");
            var pf = froot.AddComponent<PokerFight>();
            pf.loop = pokerLoop; pf.background = sr; pf.fightBackground = fightSprite; pf.area = area;
            pf.extBackground = extSr; pf.fightExtBackground = fightExt;
            pf.fightDepthLimits = new[] { pt(PokerLeftWallEdge.x, PokerLeftWallEdge.y), pt(PokerLeftWallCorner.x, PokerLeftWallCorner.y), pt(1290f, BarRoomWallRow),
                                          pt(1350f, BarRoomCabinetRow), pt(1560f, BarRoomCabinetRow), pt(1600f, BarRoomWallRow),
                                          pt(PokerRightWallCorner.x, PokerRightWallCorner.y), pt(PokerRightWallEdge.x, PokerRightWallEdge.y) };
            // pelin tuolit (nosto, lyönti ja heitto kuten El Loipparissa)
            var chairSprite = ImportProp("Assets/Sprites/Rekvisiitta/tuoli.png");
            var wood = AssetDatabase.FindAssets("t:AudioClip puu", new[] { "Assets/Audio" }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(q => Path.GetFileNameWithoutExtension(q).ToLowerInvariant().StartsWith("puu")).Select(AssetDatabase.LoadAssetAtPath<AudioClip>).Where(clip => clip != null).ToArray();
            var chairs = new List<GameObject>();
            if (chairSprite != null)
                for (int i = 0; i < PokerChairPx.Length; i++)
                {
                    var cgo = new GameObject("Pokerituoli " + (i + 1));
                    cgo.transform.SetParent(froot.transform, false);
                    Vector2 cp = pt(PokerChairPx[i].x, PokerChairPx[i].y);
                    cgo.transform.position = new Vector3(cp.x, cp.y, 0f);
                    var b = new GameObject("Visual").AddComponent<SpriteRenderer>(); b.transform.SetParent(cgo.transform, false);
                    b.sprite = chairSprite; b.flipX = PokerChairPx[i].x > 768f;   // selkänoja ulospäin
                    b.transform.localScale = new Vector3(ChairScaleSmall, ChairScaleSmall, 1f);
                    var sh = new GameObject("Shadow").AddComponent<SpriteRenderer>(); sh.transform.SetParent(cgo.transform, false);
                    sh.color = new Color(0f, 0f, 0f, 0.3f);
                    var c = cgo.AddComponent<Chair>(); c.body = b; c.shadow = sh; c.breakSounds = wood;
                    chairs.Add(cgo);
                }
            pf.activate = chairs.ToArray();
            // vastustajat samoille paikoille kuin loppuanimaatiossa (prätkäjätkä myöhemmin)
            var all = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var fighters = new List<Enemy>();
            var names = new List<string>();
            foreach (var (tName, px) in new[] { ("Lippis", PokerLippisPx), ("Puliukko", PokerPuliukkoPx), ("Punkkari", PokerPunkkariPx) })
            {
                var tmpl = all.FirstOrDefault(en => en != null && en.gameObject.name == tName);
                if (tmpl == null) continue;
                var ego = Object.Instantiate(tmpl.gameObject, froot.transform);
                ego.name = tName + " (pokeri)";
                Vector2 ep = pt(px.x, px.y);
                ego.transform.position = new Vector3(ep.x, ep.y, 0f);
                var e = ego.GetComponent<Enemy>(); e.wakeDistance = 100f; e.joinsFightWhenSquadComes = false;
                if (!e.fightsEveryone) e.huntsBrawlers = true;   // Lippis ja Punkkari: prätkäjätkän ja puliukon kimppuun, eivät toistensa
                ego.SetActive(false);
                fighters.Add(e); names.Add(tName);
            }
            // prätkäjätkä: oma hahmo videoista (kävely, lyönti, suora, potku; kaatumiskuvat tulossa)
            var biker = MakeBiker(froot.transform);
            if (biker != null)
            {
                Vector2 bp = pt(PokerBikerPx.x, PokerBikerPx.y);
                biker.transform.position = new Vector3(bp.x, bp.y, 0f);
                biker.gameObject.SetActive(false);
                fighters.Add(biker); names.Add("Prätkäjätkä");
            }
            pf.fighters = fighters.ToArray();
            // portsari vartioi baarissa teräsoven vieressä (vartioasento), tulee tappeluun ovesta
            var bouncerT = all.FirstOrDefault(en => en != null && en.gameObject.name == "Portsari");
            string vp = FindTexture("portsari_vahti");
            if (bouncerT != null && vp != null)
            {
                SetupAndSlice(vp);
                var guardIdle = LoadSprites("portsari_vahti").OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
                var g = Object.Instantiate(bouncerT.gameObject, froot.transform);
                g.name = "Portsari (baarin vahti)";
                g.transform.position = new Vector3(barBack.x - 1.4f, btop - 640f / bppu, 0f);
                var ge = g.GetComponent<Enemy>();
                pf.guardFightIdle = ge.idleSprites; pf.guardFightIdleFrameTime = ge.idleFrameTime;
                if (guardIdle.Length > 0) { ge.idleSprites = guardIdle; ge.idleFrameTime = 0.16f; }
                ge.wakeDistance = -1f;              // ei herää ohi kävellessä, vasta tappelusta tai osumasta
                ge.joinsFightWhenSquadComes = false; ge.wanderMinX = ge.wanderMaxX = 0f;
                g.SetActive(true);
                pf.guard = null;   // baarin vartija jää baariin (liittyy baaritappeluun)
            }
            // kaksi portsaria tulee pokerihuoneen teräsovesta, kun tappelu alkaa
            if (bouncerT != null)
            {
                var lgo = new GameObject("Pokerihuoneen portsarit");
                lgo.transform.SetParent(froot.transform, false);
                var lc = lgo.AddComponent<Latecomers>();
                var list = new List<Enemy>();
                for (int k = 0; k < 2; k++)
                {
                    var bgo = Object.Instantiate(bouncerT.gameObject, lgo.transform);
                    bgo.name = "Portsari (pokerihuone) " + (k + 1);
                    var be = bgo.GetComponent<Enemy>(); be.wakeDistance = 100f; be.joinsFightWhenSquadComes = false;
                    bgo.SetActive(false); list.Add(be);
                }
                lc.enemies = list.ToArray();
                lc.entry = (Vector2)exit.transform.position + new Vector2(1.2f, -0.6f);   // teräsoven edestä
                lc.firstDelay = 2.0f; lc.interval = 1.2f;
                pf.latecomers = lc;
                names.Add("2 portsaria ovesta");
            }
            fightInfo = $"\nTappelu: loppuanimaation jälkeen musta häivytys, {chairs.Count} tuolia ja {string.Join(", ", names)}.";
        }
        else fightInfo = "\nTappelu puuttuu: tarvitaan " + PokerFightBgPath + " ja pokerin idle-kuvat.";

        EditorSceneManager.MarkSceneDirty(bg.scene);
        Info($"Pokerihuone luotu (pokeri{(pp == null ? " PUUTTUU: baari_poker_idle.png" : "")}, {pokerKovis} Kovista etuosassa).\nBaarin oikean nurkan teräsovesta E: pokerihuoneeseen. Takaisin baariin vasemman reunan teräsovesta." + fightInfo + "\n\nTallenna scene (Ctrl+S).");
    }

    // hyttiovi: Geminin videosta kohdistettu avautumissarja kannen kuvan päälle (laiva_ovi.png, 12 kuvaa 372 × 600)
    const float CabinDoorRoiX = 1240f, CabinDoorRoiBottom = 770f, CabinDoorCellW = 372f;   // viimeisen 1774 px ruudun koordinaatit
    static AnimatedDoor AddShipCabinDoor(GameObject deckBg, Transform parent, List<string> report)
    {
        string p = FindTexture("laiva_ovi");
        if (p == null || deckBg == null) { report.Add("Hyttiovi: laiva_ovi.png puuttuu (ei avautuvaa ovea)"); return null; }
        SetupAndSlice(p);
        var frames = LoadSprites("laiva_ovi").OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
        if (frames.Length < 2) return null;
        var bsr = deckBg.GetComponent<SpriteRenderer>();
        float ppu = bsr.sprite.pixelsPerUnit, W = bsr.sprite.rect.width;
        float left = bsr.bounds.min.x, top = bsr.bounds.max.y;
        var go = new GameObject("Hyttiovi");
        go.transform.SetParent(parent, false);
        // kuvat on tuotu 100 px/yks, kansi omalla tarkkuudellaan: skaalataan kannen kokoon; tukipiste alareunan keskellä
        float k100 = frames[0].pixelsPerUnit / ppu;
        go.transform.position = new Vector3(left + (W - 1774f + CabinDoorRoiX + CabinDoorCellW * 0.5f) / ppu, top - CabinDoorRoiBottom / ppu, 0f);
        go.transform.localScale = new Vector3(k100, k100, 1f);
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = frames[0]; sr.sortingOrder = -9990;   // taustan päällä, hahmojen alla
        var d = go.AddComponent<AnimatedDoor>(); d.body = sr; d.frames = frames; d.frameTime = 0.055f;
        var night = deckBg.GetComponent<ShipNight>();
        if (night != null) night.tinted = (night.tinted ?? new SpriteRenderer[0]).Where(r => r != null).Concat(new[] { sr }).ToArray();
        report.Add($"Hyttiovi: {frames.Length} kuvaa, aukeaa seilorien tullessa");
        return d;
    }

    [MenuItem("Beat em up/56. Laivan tappelu: seilorit (liittolaiset) ja rosvot")]
    static void AddShipFight()
    {
        var deck = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Laivan kansi");
        var deckBg = GameObject.Find("Laivan kansi");
        if (deck == null || deckBg == null) { Info("Tee ensin kohta 55 (laivan kansi)."); return; }
        foreach (var n in new[] { "seilori_idle", "seilori_kavely", "seilori_lyonti", "seilori_potku", "seilori_osuma", "seilori_kaatuminen", "seilori_juoksu", "seilori_koukku", "seilori_heitto", "seilori_ylosnousu" })
        {
            string path = FindTexture(n);
            if (path != null) SetupAndSlice(path);
        }
        foreach (var n in new[] { "Laivan seilorit", "Laivan rosvot" })
        {
            var o = GameObject.Find(n);
            if (o != null) Undo.DestroyObjectImmediate(o);
        }
        var report = new List<string>();
        Sprite[] idle = EnemySheet("seilori_idle", report);
        if (idle.Length == 0) { Info("seilori_idle.png puuttuu."); return; }
        var bsr = deckBg.GetComponent<SpriteRenderer>();
        float ppu = bsr.sprite.pixelsPerUnit, left = bsr.bounds.min.x;
        System.Func<float, float> X = px => left + px / ppu;

        // seilori: liittolainen, taistelee vain rosvoja vastaan
        var go = new GameObject("Seilori");
        var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
        var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
        var t = go.AddComponent<Enemy>();
        t.body = visual; t.shadow = shadow; visual.sprite = idle[0];
        t.displayName = "Seilori";
        t.ally = true;
        t.idleSprites = idle; t.idleFrameTime = 0.16f;
        t.walkSprites = EnemySheet("seilori_kavely", report); t.walkFrameTime = 0.07f;   // 21 kuvaa videosta
        t.punchSprites = EnemySheet("seilori_lyonti", report);   // kombo: jab (0–2) ja heti perään nopea pikkukoukku (3–8)
        t.punchImpactFrame = 2; t.secondImpactFrame = 5;
        t.windupTime = 0.15f; t.punchRecoverTime = 0.5f; t.punchDamage = 8;
        t.altAttackSprites = EnemySheet("seilori_potku", report); t.altImpactFrame = 5;  // potku ojennettuna
        t.altDamage = 14; t.altKnockdown = true; t.altChance = 0.3f; t.altReach = 2.4f;
        t.hurtSprites = EnemySheet("seilori_osuma", report);   // osuma: horjahdus taakse (kaatumisen alusta)
        t.flipThrownSprites = EnemySheet("seilori_heitto", report);       // heitettynä: 0 asento, 1 napattu, 2 kierähdys, 3 lento, 4–7 alastulo ja makuu
        t.flipFlightFrames = 1; t.flipFlightFrameTime = 0.12f; t.flipLandFrameTime = 0.11f;
        t.knockdownSprites = EnemySheet("seilori_kaatuminen", report);   // 9 kuvaa: horjuu taakse, kaatuu selälleen, makuu
        t.getUpSprites = EnemySheet("seilori_ylosnousu", report); t.getUpTime = 1.0f;   // kip-up: jalat ylös, ponnistus, kyykky, asento
        t.runSprites = EnemySheet("seilori_juoksu", report); t.runFrameTime = 0.066f;   // juoksu videosta (hyttiovelta tullessa)
        t.punch2Sprites = EnemySheet("seilori_koukku", report);           // yläkoukku: kyykky, isku ylös (kuva 6)
        t.punch2ImpactFrame = 5; t.punch2Damage = 18; t.punch2Knockdown = true; t.punch2Chance = 0.2f;
        t.punch2LaunchUp = 13f; t.punch2LaunchX = 3f;   // vahva yläkoukku lennättää korkealle
        t.attackRange = 1.9f; t.moveSpeedX = 3f; t.moveSpeedY = 1.6f; t.runSpeedMultiplier = 1.5f;
        t.maxHealth = 80; t.attackCooldown = 0.9f;
        t.hurtSounds = LoadClips("Assets/Audio/big thug", "gasp"); t.hurtVolume = 0.8f;
        t.wakeDistance = 100f; t.blockChance = 0.1f; t.retreatChance = 0f;
        var hyttiovi = new Vector2(X(ShipDoorXPx) - 1f, deck.maxDepthY - 0.3f);
        var root = new GameObject("Laivan seilorit");
        Undo.RegisterCreatedObjectUndo(root, "Laivan seilorit");
        var squad = root.AddComponent<BouncerSquad>();
        squad.area = deck; squad.bothSides = false; squad.firstDelay = 1.0f; squad.spawnInterval = 0.7f;
        squad.triggerX = X(ShipFightTriggerPx);   // hero hyttiovella: seilorit juoksevat ovesta
        squad.waves = SailorWaves; squad.waveNextAt = 1;   // lisää seiloreita, kun pystyssä on enää yksi
        squad.door = AddShipCabinDoor(deckBg, root.transform, report);   // ovi aukeaa, seilorit juoksevat ulos
        squad.bouncers = new Enemy[SailorCount];
        go.transform.SetParent(root.transform, false);
        for (int i = 0; i < SailorCount; i++)
        {
            var c = i == 0 ? go : Object.Instantiate(go, root.transform);
            if (i > 0) c.name = "Seilori_" + (i + 1);
            float y = Mathf.Lerp(deck.maxDepthY, deck.minDepthY, 0.15f + 0.2f * (i % 4) + 0.1f * (i / 4));
            c.transform.position = new Vector3(hyttiovi.x - 0.3f * i, y, 0f);
            squad.bouncers[i] = c.GetComponent<Enemy>();
            c.SetActive(false);            // tulevat hyttiovelta juosten, kun tappelu alkaa
        }
        report.Add($"Seilorit: {SailorCount} (4 hyttiovelta tappelun alkaessa, 3 lisää kun pystyssä on enää yksi)");

        // rosvot: Kovis-pohja (osaa heittää), merirosvon omat kuvat niiltä osin kuin ne ovat valmiina
        foreach (var n in new[] { "merirosvo_idle", "merirosvo_lyonti", "merirosvo_suora", "merirosvo_polvi", "merirosvo_kaatuminen", "merirosvo_ylosnousu", "merirosvo_heitto", "merirosvo_heitetty", "merirosvo_kierrepotku", "merirosvo_kavely", "merirosvo_kavely_puhe", "merirosvo_suojaus" })
        {
            string path = FindTexture(n);
            if (path != null) SetupAndSlice(path);
        }
        var pIdle = EnemySheet("merirosvo_idle", report);
        var pJab = EnemySheet("merirosvo_lyonti", report);
        var pCross = EnemySheet("merirosvo_suora", report);
        var pKnee = EnemySheet("merirosvo_polvi", report);
        var pFall = EnemySheet("merirosvo_kaatuminen", report);
        var pThrow = EnemySheet("merirosvo_heitto", report);     // 8 kuvaa: 0 tarttuu, 1–4 veto ja olalle, 5 heilautus, 6 irti, 7 palautus
        var pUp = EnemySheet("merirosvo_ylosnousu", report);
        var pThrown = EnemySheet("merirosvo_heitetty", report);
        var pSpin = EnemySheet("merirosvo_kierrepotku", report);  // 13 kuvaa: yksi pyörähdys, potku kuvassa 8
        var pWalk = EnemySheet("merirosvo_kavely", report);
        var pBlock = EnemySheet("merirosvo_suojaus", report);   // 6 kuvaa: 0 asento, 1–2 suoja ylös, 3 laskee, 4 suoja, 5 asento
        var pTalk = EnemySheet("merirosvo_kavely_puhe", report);  // sama vaihe kuin kävelyssä, suu liikkuu
        var pirateVoice = LoadClips("Assets/Audio/Merirosvo", "").Where(c => !c.name.Contains("gasp") && !c.name.Contains("hit")).ToArray();   // "tämä on ryöstö" -huudot (puhekävely niiden ajan)  // heron kuperkeikkaheitto: 0 asento, 1 napattu, 2 kierähdys, 3 lento, 4–7 alastulo ja makuu      // 6 kuvaa: makaa, punnerrus, konttaus, polvella, nousu; 4–5 peilattu   // 7 kuvaa: 0 tuskainen osuma, 1–4 lento ja pyörähdys mahalleen, 5–6 makaa
        var lippisT = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(e => e.gameObject.name == "Kovis");
        if (lippisT != null)
        {
            var pr = new GameObject("Laivan rosvot");
            Undo.RegisterCreatedObjectUndo(pr, "Laivan rosvot");
            int n2 = 0;
            var raid = pr.AddComponent<BouncerSquad>();
            raid.area = deck; raid.bothSides = false; raid.fromLeftEdge = true;
            raid.triggerX = X(ShipFightTriggerPx);
            raid.waves = ShipPirateWaves; raid.waveNextAt = 2;   // kun rosvoja on pystyssä enää 2, tulee 4 lisää
            raid.firstDelay = 0.4f; raid.spawnInterval = 0.6f;
            raid.bouncers = new Enemy[ShipPirateDepths.Length];
            foreach (var depth in ShipPirateDepths)
            {
                var r = Object.Instantiate(lippisT.gameObject, pr.transform);
                r.name = "Rosvo " + (++n2);
                r.transform.position = new Vector3(X(ShipFightTriggerPx) - 10f, Mathf.Lerp(deck.maxDepthY, deck.minDepthY, depth), 0f);
                raid.bouncers[n2 - 1] = r.GetComponent<Enemy>();
                r.SetActive(false);   // hyökkäävät keulan puolelta, kun tappelu alkaa
                var re = r.GetComponent<Enemy>(); re.displayName = "Rosvo"; re.wakeDistance = 7f;
                if (pIdle.Length > 0)
                {
                    re.visualScale = 1f; re.attackArtScale = 1f; re.walkArtScale = 1f;
                    re.idleSprites = pIdle; re.idleFrameTime = 0.13f;
                    re.walkSprites = pIdle; re.walkFrameTime = 0.1f;   // kävelykuvat tulossa: siihen asti tappeluasento
                    re.hurtSprites = null;                            // osumakuvat tulossa: tappeluasento + välähdys
                    re.body.sprite = pIdle[0];
                }
                if (pFall.Length >= 7)
                {
                    re.knockArtScale = 1f;
                    re.hurtSprites = new[] { pFall[0] };                      // osuma: tuskainen horjahdus
                    re.knockdownSprites = new[] { pFall[0], pFall[1], pFall[2], pFall[3], pFall[4], pFall[6] };
                    re.landSprites = new[] { pFall[5], pFall[6] }; re.landFrameTime = 0.12f;
                    re.getUpSprites = pUp.Length >= 6 ? pUp : null;          // ilman kuvia: käännetty tappeluasento
                    re.getUpFacesBack = pUp.Length >= 6; re.getUpTime = 1.0f;
                }
                if (pThrow.Length >= 8) { re.grabSprites = pThrow; re.throwForward = false; re.throwTurnsAround = false; }   // olan yli selän taakse; kuvat päättyvät alkuasentoon
                if (pThrown.Length >= 8)
                {
                    re.bigBody = true;   // hero heittää kuperkeikalla (kuten Kovista)
                    re.flipThrownSprites = pThrown; re.flipFlightFrames = 1; re.flipFlightFrameTime = 0.12f; re.flipLandFrameTime = 0.11f;
                }
                if (pWalk.Length >= 10) { re.walkSprites = pWalk; re.walkFrameTime = 0.11f; re.walkArtScale = 1f; }
                if (pTalk.Length >= 10) { re.walkTalkSprites = pTalk; re.talkDuration = 1.6f; }
                re.tauntSounds = pirateVoice; re.tauntVolume = 1f;   // ei Koviksen huutoja
                // kovempia kuin Kovis: kestävämpiä, lyövät tiheämmin ja torjuvat välillä
                re.maxHealth = 130; re.attackCooldown = 0.8f; re.blockChance = 0.25f;
                if (pBlock.Length >= 6) re.blockSprites = new[] { pBlock[1], pBlock[2], pBlock[2], pBlock[4], pBlock[3] };   // suoja nousee, pitää, laskee
                re.moveSpeedX = Mathf.Max(re.moveSpeedX, 3.2f);
                var pirateGasp = LoadClips("Assets/Audio/Merirosvo gasp", "merirosvogasp");   // osuma, kaatuminen, heitosta mätkähdys
                if (pirateGasp.Length > 0) { re.hurtSounds = pirateGasp; re.hurtVolume = 0.95f; }
                var pirateHit = LoadClips("Assets/Audio/Merirosvo isku", "merirosvohit");   // nyrkkeilijän puhallukset lyönneissä ja potkuissa
                if (pirateHit.Length > 0) { re.attackSounds = pirateHit; re.attackSoundChance = 1f; }
                // kombot: nopea jab–suora–jab ja jab–suora–polvi; kierrepotku välillä yksinään
                re.combos = new[] { "JSJ", "JSP" }; re.comboChance = 0.7f; re.comboArmor = true; re.debugCombos = true;   // vasen–oikea–vasen menee loppuun (debug: Console)
                re.holdGround = true; re.retreatChance = 0f;   // ei peräänny heron tullessa kimppuun
                re.comboWindupScale = 0.7f; re.comboGap = 0.1f;   // iskujen välissä käsi palaa näkyvästi
                re.punchActiveTime = 0.13f;   // isku näkyy ojennettuna hetken: kombon iskut erottuvat toisistaan
                if (pSpin.Length >= 13)
                {
                    // kierrepotku: alku hitaasti (kuvat kahdesti), kiihtyy loppua kohti; potku kuvassa 8
                    re.punch3Sprites = new[] { 0, 0, 1, 1, 2, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }.Select(i => pSpin[i]).ToArray();
                    re.punch3ImpactFrame = 11; re.punch3WindupTime = 0.6f; re.punch3RecoverTime = 0.55f;
                    re.punch3Damage = 24; re.punch3Knockdown = true; re.punch3LaunchX = 6f; re.punch3LaunchUp = 8f;
                    re.punch3Chance = 0.15f; re.punch3Reach = 2.5f;
                }
                if (pJab.Length >= 6) { re.punchSprites = pJab; re.punchImpactFrame = 3; re.secondImpactFrame = -1; re.windupTime = 0.12f; re.punchRecoverTime = 0.4f; re.punchDamage = 10; }
                if (pCross.Length >= 5)
                {
                    // pitkä suora askeleella: kaataa
                    re.punch2Sprites = pCross; re.punch2ImpactFrame = 2; re.punch2Damage = 16; re.punch2Knockdown = true; re.punch2Chance = 0.25f;
                    re.punch2LaunchUp = 0f;
                }
                if (pKnee.Length >= 6)
                {
                    re.altAttackSprites = pKnee; re.altImpactFrame = 3; re.altDamage = 14; re.altKnockdown = false;
                    re.altChance = 0.12f; re.altReach = 1.5f; re.altLungeSpeed = 0f; re.altUnblockable = false; re.altTimeScale = 1f;
                }
            }
            // pari Kovista rosvojen puolelle: 2. ja 3. aallon viimeisiksi
            var list = raid.bouncers.ToList();
            for (int k = 0; k < ShipKovisCount; k++)
            {
                var kv = Object.Instantiate(lippisT.gameObject, pr.transform);
                kv.name = "Kovis (rosvo) " + (k + 1);
                kv.transform.position = new Vector3(X(ShipFightTriggerPx) - 10f, Mathf.Lerp(deck.maxDepthY, deck.minDepthY, k == 0 ? 0.55f : 0.35f), 0f);
                var ke = kv.GetComponent<Enemy>(); ke.wakeDistance = 7f;
                kv.SetActive(false);
                list.Insert(Mathf.Min(list.Count, k == 0 ? 8 : 13), ke);
            }
            raid.bouncers = list.ToArray();
            report.Add(pIdle.Length > 0 ? $"Rosvot: {n2} + {ShipKovisCount} Kovista aaltoina 4 + 5 + 5 keulan puolelta (seuraava aalto, kun 2 jäljellä), kun hero tulee hyttiovelle (merirosvon kuvat; huuto: {pirateVoice.Length} ääntä kansiossa Assets/Audio/Merirosvo)" : $"Rosvot: {n2} (väliaikaisesti Kovis-hahmoina)");
        }
        else report.Add("Rosvot: Kovis-malli puuttuu");
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info("Laivan tappelu:\n" + string.Join("\n", report) + "\n\nSeilorit lyövät vain rosvoja, eivät heroa.\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Risteilyaluksen kansi ----------------
    const string ShipDeckPath = "Assets/Sprites/Taustat/laiva_kansi.png";          // 3 kuvaa koottuna, 4256 × 887, keula vasemmalla
    const string ShipSeaFarPath = "Assets/Sprites/Taustat/laiva_meri_kauko.png";   // taivas, horisontti (rivi 291), kaukaiset saaret
    const string ShipSeaNearPath = "Assets/Sprites/Taustat/laiva_meri_lahi.png";   // vaahtoava meri laivan vieressä
    const float ShipX0 = 15000f;
    const float ShipFloorPx = 540f;                  // kannen takareuna (kaiteen juuri)
    const float ShipBowPx = 400f;                    // keulan runko: ei kävellä tätä vasemmalle
    const float ShipWallTopXPx = 3870f, ShipWallBottomXPx = 4185f;   // viiston seinän juuri: takareunassa / alareunassa
    const float ShipDoorXPx = 4070f;                 // hyttiosaston ovi viistossa seinässä
    const float ShipHorizonPx = 330f;                // meren horisontti kannen kuvan rivillä
    const float UccoWarehouseDoorPx = 3860f;
    static readonly Vector3[] ShipBikiniSpots = { new Vector3(2938f, 492f, 0f), new Vector3(3246f, 492f, 0f), new Vector3(2058f, 487f, 1f) };   // z = 1: peilikuva   // aurinkotuolien istuinkohta kannen kuvassa
    const float ShipAuroraBarPx = 2290f;
    static readonly Vector2 ShipBarCounterPx = new Vector2(2428f, 200f);   // kannen baaritiski: keskikohta ja puolileveys (n. 2250–2605)
    static readonly Vector2 ShipSohviPx = new Vector2(2345f, 393f);   // Sohvi baaritiskin takana (tiskin yläreuna rivillä 393)
    const int DanceMirrorFrom = 48;          // tanssivideon kuva 180° kohdalla: sen peilikuva = alkuasento
    static readonly Vector3[] ShipBikini2Spots = { new Vector3(1830f, 487f, 0f), new Vector3(3473f, 482f, 0f) };   // kansituoli + neljäs aurinkotuoli

    [MenuItem("Beat em up/55. Risteilyaluksen kansi (varaston ovesta), rullaava meri")]
    static void CreateShipDeck()
    {
        var ucco = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Uccopulco");
        var harborGo = GameObject.Find("Uccopulco satama");
        var ti = AssetImporter.GetAtPath(ShipDeckPath) as TextureImporter;
        if (ucco == null || harborGo == null || ti == null) { Info("Tarvitaan Uccopulco satamineen (kohta 44) ja " + ShipDeckPath); return; }
        foreach (var n in new[] { "Laivan kansi", "Laivan ovet", "Laivan meri kaukana", "Laivan meri lähellä", "Laivan turistit" })
        {
            var o = GameObject.Find(n);
            if (o != null) Undo.DestroyObjectImmediate(o);
        }
        // sama mittakaava kuin Uccopulcon katu (tausta 80 %), alareuna ruudun alareunaan
        ti.GetSourceTextureWidthAndHeight(out int srcW, out int srcH);
        float ppu = srcH / (1024f / BackgroundPPU * UccoK);
        Sprite Import(string path, float p, bool tiled)
        {
            var t = AssetImporter.GetAtPath(path) as TextureImporter;
            if (t == null) return null;
            t.textureType = TextureImporterType.Sprite;
            t.spriteImportMode = SpriteImportMode.Single;
            t.spritePixelsPerUnit = p;
            t.filterMode = FilterMode.Bilinear;
            t.textureCompression = TextureImporterCompression.Uncompressed;
            t.maxTextureSize = 8192;
            t.mipmapEnabled = false;
            t.alphaIsTransparency = true;
            t.wrapMode = tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            var st = new TextureImporterSettings(); t.ReadTextureSettings(st);
            st.spriteMeshType = SpriteMeshType.FullRect; st.spriteAlignment = (int)SpriteAlignment.Center;
            t.SetTextureSettings(st);
            t.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        var deck = Import(ShipDeckPath, ppu, false);
        float wU = deck.rect.width / ppu, hU = deck.rect.height / ppu;
        float cy = CamY - CamHalf + hU * 0.5f;
        float top = cy + hU * 0.5f;
        var bg = new GameObject("Laivan kansi");
        var sr = bg.AddComponent<SpriteRenderer>(); sr.sprite = deck; sr.sortingOrder = -10000;
        bg.transform.position = new Vector3(ShipX0 + wU * 0.5f, cy, 0f);
        Undo.RegisterCreatedObjectUndo(bg, "Laivan kansi");
        System.Func<float, float> X = px => ShipX0 + px / ppu;
        System.Func<float, float> Y = row => top - row / ppu;

        float halfW = CamHalf * 16f / 9f;
        var area = ReuseArea("Alue: Laivan kansi");   // sama alue: tappelu (56), renkaat ja pöydät viittaavat siihen
        area.areaName = "Laivan kansi";
        area.music = ucco.music;
        area.useSidewalk = false;
        area.maxDepthY = Y(ShipFloorPx) - 0.1f;
        area.minDepthY = CamY - CamHalf + 1.2f;
        area.camMinX = ShipX0 + halfW;
        area.camMaxX = ShipX0 + wU - halfW;
        area.camRiseY = Mathf.Min(2f, top - (CamY + CamHalf));
        area.walkMinX = X(ShipBowPx);
        area.walkMaxX = X(ShipWallBottomXPx) - 0.5f;
        // viisto seinä: takaraja tulee eteenpäin seinän juurta pitkin
        area.depthLimits = new[] { new Vector2(X(ShipWallTopXPx) - 0.4f, area.maxDepthY), new Vector2(X(ShipWallBottomXPx) - 0.4f, area.minDepthY) };

        // rullaava meri: laiva kulkee vasemmalle, meri virtaa oikealle. Kaukainen hitaasti, lähellä nopeasti.
        GameObject Layer(string name, string path, float scaleRows, float topRow, int order, float speed, float parallax, string nightPath = null)
        {
            // skaala: yksi kuvan pikseli = scaleRows kannen pikseliä
            float p = ppu / scaleRows;
            var spr = Import(path, p, true);
            if (spr == null) return null;
            var go = new GameObject(name);
            var s = go.AddComponent<SpriteRenderer>(); s.sprite = spr; s.sortingOrder = order;
            s.drawMode = SpriteDrawMode.Tiled;
            float w = spr.rect.width / p, h = spr.rect.height / p;
            s.size = new Vector2(Mathf.Ceil((2f * halfW) / w + 2f) * w, h);
            go.transform.position = new Vector3(ShipX0 + wU * 0.5f, Y(topRow) - h * 0.5f, 0f);
            var sl = go.AddComponent<ScrollingLayer>(); sl.autoSpeed = speed; sl.parallax = parallax; sl.area = area;
            sl.bobAmplitude = order < -10095 ? 0.22f : 0.3f; sl.bobPeriod = 7f;   // laiva keinuu: meri liikkuu hitaasti ylös ja alas
            if (nightPath != null && File.Exists(nightPath)) sl.nightSprite = Import(nightPath, p, true);   // kuunvalo
            Undo.RegisterCreatedObjectUndo(go, name);
            return go;
        }
        float farScale = 1.25f;                                  // horisontti (kuvan rivi 291) kannen riville ShipHorizonPx
        Layer("Laivan meri kaukana", ShipSeaFarPath, farScale, ShipHorizonPx - 291f * farScale, -10100, 0.35f, 0.04f, "Assets/Sprites/Taustat/laiva_meri_kauko_yo.png");
        Layer("Laivan meri lähellä", ShipSeaNearPath, 0.55f, ShipHorizonPx + 45f, -10090, 4f, 0.5f, "Assets/Sprites/Taustat/laiva_meri_lahi_yo.png");
        // yö (tanssin ajaksi): kansi ja turistit sävytetään, meri vaihtuu kuunvaloon
        var night = bg.AddComponent<ShipNight>();

        // ovet: sataman varastosta kannelle ja kannelta takaisin
        var doors = new GameObject("Laivan ovet");
        Undo.RegisterCreatedObjectUndo(doors, "Ovet");
        float hppu = harborGo.GetComponent<SpriteRenderer>().sprite.pixelsPerUnit;
        float hLeft = harborGo.GetComponent<SpriteRenderer>().bounds.min.x;
        var d = new GameObject("Varaston ovi laivaan").AddComponent<Door>();
        d.transform.SetParent(doors.transform, false);
        d.transform.position = new Vector3(hLeft + UccoWarehouseDoorPx / hppu, ucco.maxDepthY - 0.25f, 0f);
        d.prompt = "Nouse laivaan";
        d.here = ucco; d.target = area;
        d.spawnPoint = new Vector2(X(900f), Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.35f));
        d.halfWidth = 1.6f; d.maxDistanceFromWall = 0.9f;
        var back = new GameObject("Laivasta satamaan").AddComponent<Door>();
        back.transform.SetParent(doors.transform, false);
        back.transform.position = new Vector3(X(700f), area.maxDepthY, 0f);
        back.prompt = "Takaisin satamaan";
        back.here = area; back.target = ucco;
        back.returnToLastDoor = true;
        back.spawnPoint = d.transform.position;
        back.halfWidth = 1.4f; back.maxDistanceFromWall = 0.9f;
        // bikininainen drinkin kanssa kahdella aurinkotuolilla (silmukka: katsoo, sulkee silmät, siemaisee)
        // Auroran nousu, seisova idle ja kävely (ruudut 512 × 768)
        Sprite[][] aurora = null;
        {
            var sets = new[] { "turisti_aurora_nousu", "turisti_aurora_idle", "turisti_aurora_kavely" }.Select(n =>
            {
                string p = FindTexture(n);
                if (p == null) return null;
                SetupAndSlice(p);
                var t2 = (TextureImporter)AssetImporter.GetAtPath(p);
                t2.mipmapEnabled = true; t2.filterMode = FilterMode.Trilinear; t2.mipMapsPreserveCoverage = true; t2.SaveAndReimport();
                return LoadSprites(n).OrderBy(sp => int.TryParse(sp.name.Substring(sp.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
            }).ToArray();
            if (sets.All(x => x != null && x.Length > 0)) aurora = sets;
        }
        // heron ja Auroran suudelma (ruudut 512 × 512)
        Sprite[] KissSheet(string n)
        {
            string p = FindTexture(n);
            if (p == null) return null;
            SetupAndSlice(p);
            var t2 = (TextureImporter)AssetImporter.GetAtPath(p);
            t2.mipmapEnabled = true; t2.filterMode = FilterMode.Trilinear; t2.mipMapsPreserveCoverage = true; t2.SaveAndReimport();
            return LoadSprites(n).OrderBy(sp => int.TryParse(sp.name.Substring(sp.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
        }
        var kissA = KissSheet("turisti_kiss_a");
        var kissB = KissSheet("turisti_kiss_b");
        var danceA = KissSheet("turisti_tanssi_a");   // tanssi suudellen (0°→270°)
        var danceB = KissSheet("turisti_tanssi_b");   // tanssi halaten
        var tRoot = new GameObject("Laivan turistit");
        Undo.RegisterCreatedObjectUndo(tRoot, "Turistit");
        int ti2 = 0;
        foreach (var (tex, spots) in new[] { ("turisti_bikini", ShipBikiniSpots), ("turisti_bikini2", ShipBikini2Spots) })
        {
        string tp = FindTexture(tex);
        if (tp != null)
        {
            SetupAndSlice(tp);
            // pienennetään pelissä: mipmapit ja trilineaarinen suodatus (siisti pienennys kuten kuvankatselimessa)
            var tti = (TextureImporter)AssetImporter.GetAtPath(tp);
            tti.mipmapEnabled = true; tti.filterMode = FilterMode.Trilinear; tti.mipMapsPreserveCoverage = true; tti.alphaIsTransparency = true;
            tti.SaveAndReimport();
            var fr = LoadSprites(tex).OrderBy(sp => int.TryParse(sp.name.Substring(sp.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
            if (fr.Length >= 10)
            {
                int[] seq = { 0, 0, 1, 0, 0, 2, 2, 0, 0, 3, 4, 5, 5, 4, 3, 0, 6, 7, 8, 9, 0, 0 };
                var loop = seq.Select(i => fr[i]).ToArray();
                foreach (var spot in spots)
                {
                    var go = new GameObject("Bikininainen " + (++ti2));
                    go.transform.SetParent(tRoot.transform, false);
                    float sc = 100f / ppu * 0.5f;                           // kuvat kaksinkertaisella tarkkuudella: 2 pikseliä = 1 kannen pikseli
                    go.transform.localScale = new Vector3(sc, sc, 1f);
                    go.transform.position = new Vector3(X(spot.x), Y(spot.y) - 36f / ppu, 0f);   // istumapiste ruudun rivillä 440 (512 korkeassa ruudussa)
                    var r = go.AddComponent<SpriteRenderer>(); r.sprite = loop[0];
                    r.sortingOrder = Mathf.RoundToInt(-area.maxDepthY * 100f) + 50;
                    if (tex == "turisti_bikini2" && spot.x < 2000f && aurora != null)
                    {
                        // Aurora: nousee tuolilta, kun pelaaja tulee lähelle, ja kävelee baarille
                        var tu = go.AddComponent<Tourist>();
                        tu.sitLoop = loop; tu.sitStartFrame = ti2 * 7;
                        tu.standUp = aurora[0]; tu.idle = aurora[1]; tu.walk = aurora[2];
                        tu.standOffsetY = 16f * sc / 100f;      // varpaat 16 kuvan pikseliä istumaruudun alareunan alla
                        tu.walkToX = X(ShipAuroraBarPx);
                        tu.walkToY = area.maxDepthY - 0.45f;              // kannen lattialle tuolirivin eteen (ei ilmassa)
                        // suudelma ja tanssi: E Auroran vieressä
                        if (kissA != null && kissB != null && kissA.Length >= 31 && kissB.Length >= 52)
                        {
                            Tourist.Frame F(Sprite sp, float dur, bool dance = false, bool flip = false) => new Tourist.Frame { sprite = sp, time = dur, dance = dance, flip = flip };
                            var intro = new List<Tourist.Frame> { F(kissA[0], 0.4f), F(kissA[1], 0.35f), F(kissA[2], 0.35f) };   // vastakkain, askel, kädet auki
                            for (int q = 3; q <= 5; q++) intro.Add(F(kissA[q], 0.25f));                                         // halaus, poskelle, suudelma
                            intro.Add(F(kissA[11], 0.3f));
                            var outro = new List<Tourist.Frame> { F(kissA[11], 0.25f) };
                            for (int q = 5; q >= 3; q--) outro.Add(F(kissA[q], 0.25f));
                            outro.Add(F(kissA[2], 0.45f));                                                                       // kädet auki, irti
                            // suudelmajakso: varpaille, jalka ylös, video 1 ja 2 edestakaisin, jalka alas
                            var kiss = new List<Tourist.Frame>();
                            for (int q = 6; q <= 8; q++) kiss.Add(F(kissA[q], 0.15f));
                            for (int q = 0; q < 52; q++) kiss.Add(F(kissB[q], 0.12f));
                            for (int q = 50; q >= 0; q--) kiss.Add(F(kissB[q], 0.12f));
                            for (int q = 13; q <= 30; q++) kiss.Add(F(kissA[q], 0.12f));
                            for (int q = 29; q >= 12; q--) kiss.Add(F(kissA[q], 0.12f));
                            kiss.Add(F(kissA[10], 0.15f)); kiss.Add(F(kissA[11], 0.2f));
                            tu.kissIntro = intro.ToArray(); tu.kissOutro = outro.ToArray(); tu.danceKiss = kiss.ToArray();
                            if (danceA != null && danceA.Length >= 80)
                            {
                                // täysi kierros: video 0°→270°, sitten peilikuvat 270°→360° (peilikuva näyttää parin 180° käännettynä)
                                var spin = new List<Tourist.Frame>();
                                for (int q = 0; q < 80; q++) spin.Add(F(danceA[q], 0.1f, true));
                                for (int q = 79; q >= DanceMirrorFrom; q--) spin.Add(F(danceA[q], 0.1f, true, true));
                                tu.danceSpin = spin.ToArray();
                            }
                            if (danceB != null && danceB.Length >= 81)
                            {
                                // halaten keinuen: suudelma irtoaa, kierähdys ja takaisin
                                var hug = new List<Tourist.Frame>();
                                for (int q = 0; q < 81; q++) hug.Add(F(danceB[q], 0.1f, true));
                                for (int q = 79; q >= 0; q--) hug.Add(F(danceB[q], 0.1f, true));
                                tu.danceHug = hug.ToArray();
                            }
                            tu.danceMusic = AssetDatabase.FindAssets("t:AudioClip tanssi", new[] { "Assets/Audio" }).Select(AssetDatabase.GUIDToAssetPath)
                                .Select(AssetDatabase.LoadAssetAtPath<AudioClip>).FirstOrDefault(clip => clip != null);
                            tu.danceDuration = tu.danceMusic != null ? Mathf.Max(20f, tu.danceMusic.length - tu.musicFadeOut - 1f) : 148f;   // tanssi kestää kappaleen verran
                        }
                        continue;
                    }
                    var dn = go.AddComponent<Dancer>(); dn.sprites = loop; dn.keepOrder = true; dn.flipX = spot.z > 0.5f; dn.frameTime = 0.22f; dn.startFrame = ti2 * 7;
                }
            }
        }
        }
        // Sohvi laivan baaritiskin taakse (kopio S-Clubin Sohvista: kuvan alareuna = tiskin yläreuna)
        var clubSohvi = GameObject.Find("Sohvi");
        if (clubSohvi != null)
        {
            var s2 = Object.Instantiate(clubSohvi, tRoot.transform);
            s2.name = "Laivan Sohvi";
            s2.transform.position = new Vector3(X(ShipSohviPx.x), Y(ShipSohviPx.y), 0f);
            s2.transform.localScale = clubSohvi.transform.localScale * 0.9f;
            var ssr = s2.GetComponent<SpriteRenderer>(); if (ssr != null) ssr.sortingOrder = -9500;   // taustan edessä, hahmojen takana
        }
        // kauppa laivan baaritiskin eteen (kopio S-Clubin tiskistä)
        var clubShop = GameObject.Find("Baaritiski");
        if (clubShop != null)
        {
            var sh = Object.Instantiate(clubShop, tRoot.transform);
            sh.name = "Laivan tiski";
            sh.transform.position = new Vector3(X(ShipBarCounterPx.x), area.maxDepthY, 0f);
            var counter = sh.GetComponent<ShopCounter>();
            if (counter != null) { counter.here = area; counter.halfWidth = ShipBarCounterPx.y / ppu; counter.title = "LAIVAN BAARI"; }
        }
        night.tinted = new[] { sr }.Concat(tRoot.GetComponentsInChildren<SpriteRenderer>()).ToArray();
        EditorSceneManager.MarkSceneDirty(bg.scene);
        Info($"Laivan kansi luotu ({wU:0} yksikköä). Sataman varaston ovesta (E) noustaan kannelle; meri rullaa.\nHyttiosaston ovi tulee myöhemmin.\n\nTallenna scene (Ctrl+S).");
    }

    const string LoipPath = "Assets/Sprites/Taustat/loippari_sisa.png";   // 3 kuvaa yhdistettynä: tiski, lava, terassi (4429 × 887)
    const float LoipX0 = 12000f;
    const float LoipFloorPx = 512f;          // seinän alareuna / lattian takaraja kuvassa
    const float LoipStreetDoorPx = 4551f;    // El Loipparin ovet Uccopulcon katukuvassa
    const float LoipExitPx = 130f;           // sisätilan vasemman reunan heiluriovet (ulos kadulle)

    [MenuItem("Beat em up/45. El Loippari (Uccopulcon baari)")]
    static void CreateLoippari()
    {
        var street = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Uccopulco");
        var streetBg = GameObject.Find("Uccopulco");
        var ti = AssetImporter.GetAtPath(LoipPath) as TextureImporter;
        if (street == null || streetBg == null || ti == null)
        {
            Info("Tarvitaan Uccopulco (kohta 44) ja kuva " + LoipPath);
            return;
        }
        foreach (var n in new[] { "El Loippari", "Alue: El Loippari", "El Loipparin ovet" })
        {
            var o = GameObject.Find(n);
            if (o != null) Undo.DestroyObjectImmediate(o);
        }
        ti.GetSourceTextureWidthAndHeight(out int srcW, out int srcH);
        float ppu = srcH / (2f * CamHalf);   // eri mittakaava kuin Uccopulcon katu: koko kuva ruudun korkuiseksi (kyltit näkyvät)
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = ppu;
        ti.filterMode = FilterMode.Bilinear;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.maxTextureSize = 8192;
        ti.mipmapEnabled = false;
        var st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        st.spriteMeshType = SpriteMeshType.FullRect;
        st.spriteAlignment = (int)SpriteAlignment.Center;
        ti.SetTextureSettings(st);
        ti.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(LoipPath);
        float wU = sprite.rect.width / ppu, hU = sprite.rect.height / ppu;

        var bg = new GameObject("El Loippari");
        var sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -10000;
        float cy = CamY;
        bg.transform.position = new Vector3(LoipX0 + wU * 0.5f, cy, 0f);
        Undo.RegisterCreatedObjectUndo(bg, "El Loippari");
        float top = cy + hU * 0.5f;

        float halfW = CamHalf * 16f / 9f;
        var area = new GameObject("Alue: El Loippari").AddComponent<Area>();
        area.areaName = "El Loippari";
        area.music = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Musiikki/Uccopulco.mp3");   // toistaiseksi sama kuin kadulla
        area.useSidewalk = false;
        area.maxDepthY = top - LoipFloorPx / ppu;
        area.minDepthY = CamY - CamHalf + 1.2f;
        area.camMinX = LoipX0 + halfW;
        area.camMaxX = LoipX0 + wU - halfW;
        // terassi (viimeinen kuva): kaiteen ja pylväiden juuret ovat lähempänä kuin seinä, ei kävellä kaiteen yli
        float terraceX0 = srcW - 1774f;   // kolmannen kuvan alku yhdistetyssä kuvassa
        System.Func<float, float, Vector2> pt = (lx, row) => new Vector2(LoipX0 + (terraceX0 + lx) / ppu, top - row / ppu);
        area.depthLimits = new[] { pt(440f, LoipFloorPx), pt(620f, 548f), pt(1774f, 572f) };
        Undo.RegisterCreatedObjectUndo(area.gameObject, "Alue");

        var doors = new GameObject("El Loipparin ovet");
        Undo.RegisterCreatedObjectUndo(doors, "Ovet");
        float streetLeft = streetBg.GetComponent<SpriteRenderer>().bounds.min.x;
        float streetPpu = streetBg.GetComponent<SpriteRenderer>().sprite.pixelsPerUnit;
        float doorX = streetLeft + LoipStreetDoorPx / streetPpu;
        var d = new GameObject("El Loipparin ovi").AddComponent<Door>();
        d.transform.SetParent(doors.transform, false);
        d.transform.position = new Vector3(doorX, street.maxDepthY - 0.25f, 0f);
        d.prompt = "Mene El Loippariin";
        d.here = street; d.target = area;
        d.spawnPoint = new Vector2(LoipX0 + LoipExitPx / ppu + 2.5f, Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.3f));
        d.halfWidth = 1.6f; d.maxDistanceFromWall = 0.9f;
        var exit = new GameObject("El Loipparin uloskäynti").AddComponent<Door>();
        exit.transform.SetParent(doors.transform, false);
        exit.transform.position = new Vector3(LoipX0 + LoipExitPx / ppu, area.maxDepthY, 0f);
        exit.prompt = "Ulos kadulle";
        exit.here = area; exit.target = street;
        exit.returnToLastDoor = true;
        exit.halfWidth = 2.2f; exit.maxDistanceFromWall = 100f;
        exit.spawnPoint = new Vector2(doorX, street.maxDepthY - 0.25f);

        EditorSceneManager.MarkSceneDirty(bg.scene);
        Info($"El Loippari luotu ({wU:0} yksikköä: baaritiski, lava, terassi).\nOvi Uccopulcon kadulla x = {doorX:0.0}. Sisältä ulos vasemman reunan heiluriovista.\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- El Loipparin yläkerta: välipomo Horhe ----------------
    const string LoipUpPath = "Assets/Sprites/Taustat/loippari_ylakerta.png";   // toimisto: kassakaappi, pöytä, pokeripöytä, parvekkeen ovet (2171 × 724)
    const float LoipUpX0 = 60000f;
    const float LoipUpFloorPx = 412f;        // seinän alareuna / lattian takaraja kuvassa
    const float LoipUpStairPx = 150f;        // portaiden yläpää (kaide vasemmassa reunassa)
    const float LoipStairPx = 1740f;         // portaiden alapää El Loipparin kuvassa (lavan vasemmalla puolella)

    [MenuItem("Beat em up/77. El Loipparin yläkerta ja välipomo Horhe")]
    static void CreateLoipparUpstairs()
    {
        var loip = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "El Loippari");
        var loipBg = GameObject.Find("El Loippari");
        var ti = AssetImporter.GetAtPath(LoipUpPath) as TextureImporter;
        if (loip == null || loipBg == null || ti == null) { Info("Tarvitaan El Loippari (kohta 45) ja kuva " + LoipUpPath); return; }
        foreach (var n in new[] { "horhe_idle", "horhe_ote", "horhe_lapsy", "horhe_vasara", "horhe_torjunta", "horhe_harkko", "horhe_kavely", "horhe_juoksu", "horhe_potkut", "horhe_koukku", "horhe_takakoukku", "horhe_osuma", "horhe_kaatuminen", "horhe_ylosnousu", "horhe_heitetty" })
        {
            string path = FindTexture(n);
            if (path != null) SetupAndSlice(path);
        }
        var report = new List<string>();
        var idle = EnemySheet("horhe_idle", report);
        var ote = EnemySheet("horhe_ote", report).Take(7).ToArray();
        if (idle.Length == 0 || ote.Length < 7) { Info("horhe_idle.png (6 kuvaa) ja horhe_ote.png (7 kuvaa) tarvitaan.\n\n" + string.Join("\n", report)); return; }
        foreach (var n in new[] { "Loipparin yläkerta", "Alue: Loipparin yläkerta", "Loipparin portaat", "Horhe" })
        {
            var o = GameObject.Find(n);
            if (o != null) Undo.DestroyObjectImmediate(o);
        }

        // tausta: koko kuva ruudun korkuiseksi kuten El Loipparissa
        ti.GetSourceTextureWidthAndHeight(out int srcW, out int srcH);
        float ppu = srcH / (2f * CamHalf);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = ppu;
        ti.filterMode = FilterMode.Bilinear;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.maxTextureSize = 8192;
        ti.mipmapEnabled = false;
        var st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        st.spriteMeshType = SpriteMeshType.FullRect;
        st.spriteAlignment = (int)SpriteAlignment.Center;
        ti.SetTextureSettings(st);
        ti.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(LoipUpPath);
        float wU = sprite.rect.width / ppu, hU = sprite.rect.height / ppu;
        var bg = new GameObject("Loipparin yläkerta");
        var sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = sprite; sr.sortingOrder = -10000;
        bg.transform.position = new Vector3(LoipUpX0 + wU * 0.5f, CamY, 0f);
        Undo.RegisterCreatedObjectUndo(bg, "Loipparin yläkerta");
        float top = CamY + hU * 0.5f;
        float halfW = CamHalf * 16f / 9f;
        var area = new GameObject("Alue: Loipparin yläkerta").AddComponent<Area>();
        area.areaName = "Loipparin yläkerta";
        area.music = loip.music;
        area.useSidewalk = false;
        area.maxDepthY = top - LoipUpFloorPx / ppu;
        area.minDepthY = CamY - CamHalf + 1.2f;
        area.camMinX = LoipUpX0 + halfW;
        area.camMaxX = LoipUpX0 + Mathf.Max(wU - halfW, halfW);
        Undo.RegisterCreatedObjectUndo(area.gameObject, "Alue");

        // portaat: alhaalta lavan vierestä ylös, ylhäältä kaiteen kohdalta alas
        var stairs = new GameObject("Loipparin portaat");
        Undo.RegisterCreatedObjectUndo(stairs, "Portaat");
        var lsr = loipBg.GetComponent<SpriteRenderer>();
        float loipLeft = lsr.bounds.min.x, loipPpu = lsr.sprite.pixelsPerUnit;
        float stairX = loipLeft + LoipStairPx / loipPpu;
        float upX = LoipUpX0 + LoipUpStairPx / ppu;
        var up = new GameObject("Portaat ylös").AddComponent<Door>();
        up.transform.SetParent(stairs.transform, false);
        up.transform.position = new Vector3(stairX, loip.maxDepthY, 0f);
        up.prompt = "Yläkertaan";
        up.here = loip; up.target = area;
        up.spawnPoint = new Vector2(upX + 1.8f, Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.3f));
        up.halfWidth = 1.2f; up.maxDistanceFromWall = 1.2f;
        var down = new GameObject("Portaat alas").AddComponent<Door>();
        down.transform.SetParent(stairs.transform, false);
        down.transform.position = new Vector3(upX, area.maxDepthY, 0f);
        down.prompt = "Alakertaan";
        down.here = area; down.target = loip;
        down.spawnPoint = new Vector2(stairX + 0.6f, Mathf.Lerp(loip.maxDepthY, loip.minDepthY, 0.25f));
        down.halfWidth = 1.6f; down.maxDistanceFromWall = 1.2f;

        // Horhe: valkoinen puku, kultaketjut; tarttuu rinnuksista ja hakkaa takakädellä
        var go = new GameObject("Horhe");
        var visual = new GameObject("Visual").AddComponent<SpriteRenderer>();
        visual.transform.SetParent(go.transform, false);
        var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>();
        shadow.transform.SetParent(go.transform, false);
        var t = go.AddComponent<Enemy>();
        t.body = visual; t.shadow = shadow; visual.sprite = idle[0];
        t.displayName = "Horhe";
        t.visualScale = 1f; t.attackArtScale = 1f; t.knockArtScale = 1f; t.walkArtScale = 1f;
        t.idleSprites = idle; t.idleFrameTime = 0.14f;
        // kävely videosta (14 kuvaa, n. 0,9 s askelsykli), osuma, kaatuminen (osuma + 5: horjuu, ilmassa, makaa) ja ylösnousu (6)
        var hWalk = EnemySheet("horhe_kavely", report);
        if (hWalk.Length > 0) { t.walkSprites = hWalk; t.walkFrameTime = 0.066f; }
        var hRun = EnemySheet("horhe_juoksu", report);
        if (hRun.Length > 0) t.runSprites = hRun;   // juoksu videosta (14 kuvaa = kaksi askelta)
        var hKick = EnemySheet("horhe_potkut", report);
        if (hKick.Length >= 22) t.kickComboSprites = hKick.Take(22).ToArray();   // pyörähdyspotku + takapotku
        var hHook = EnemySheet("horhe_koukku", report);
        var hRear = EnemySheet("horhe_takakoukku", report);
        SetHorheHooks(t, hHook, hRear);
        var hHurt = EnemySheet("horhe_osuma", report);
        var hFall = EnemySheet("horhe_kaatuminen", report);
        var hUp = EnemySheet("horhe_ylosnousu", report);
        if (hHurt.Length > 0) t.hurtSprites = hHurt;
        if (hFall.Length >= 5) t.knockdownSprites = (hHurt.Length > 0 ? new[] { hHurt[0] } : new Sprite[0]).Concat(hFall.Take(5)).ToArray();
        if (hUp.Length >= 6) { t.getUpSprites = hUp.Take(6).ToArray(); t.getUpTime = 1.0f; }
        t.punchSprites = new[] { ote[2], ote[0], ote[2] };            // suora etukädellä
        t.punchImpactFrame = 1;
        t.punch2Sprites = new[] { ote[4], ote[5], ote[6], ote[4] };  // takasuora
        t.punch2ImpactFrame = 2; t.punch2Damage = 14; t.punch2Chance = 0.4f;
        t.windupTime = 0.22f; t.punchRecoverTime = 0.45f;
        t.punchDamage = 11;
        t.attackRange = 2.1f;
        t.clinchSprites = ote;                                        // erikoisliike: ote ja turpaanveto
        t.clinchChance = 0.4f; t.clinchHits = 3; t.clinchDamage = 7; t.clinchFinalDamage = 14; t.clinchReach = 2.4f;
        t.punch3SwingSounds = LoadClips("Assets/Audio/big thug", "haymaker_isku");   // iskun murahdus otteessa
        // läpsykombo: sekunnin lataus (takakäsi taakse, tärisee), 10 läpsyä vuorokäsin eteenpäin astuen, viimeinen kaataa
        var slaps = EnemySheet("horhe_lapsy", report);
        if (slaps.Length >= 8) { t.slapSprites = slaps.Take(8).ToArray(); t.slapChance = 0.3f; t.slapCount = 10; t.slapDamage = 3; t.slapFinalDamage = 10; }
        t.punch3WindupSounds = LoadClips("Assets/Audio/big thug", "haymaker_lataus");   // latauksen murahdus (läpsykombo ja vasara)
        // vasara: molemmat kädet ristissä pään yli ja alas (6 kuvaa: 0 asento, 1–2 nosto, 3 alas, 4 isku, 5 jälkiliike), kaataa
        var hammer = EnemySheet("horhe_vasara", report);
        if (hammer.Length >= 6)
        {
            t.punch3Sprites = hammer.Take(6).ToArray(); t.punch3ImpactFrame = 4; t.punch3Damage = 18; t.punch3Knockdown = true;
            t.punch3Chance = 0.3f; t.punch3WindupTime = 0.6f; t.punch3RecoverTime = 0.6f; t.punch3Reach = 2.3f;
            t.punch3LaunchUp = 0f; t.punch3Heavy = false;   // torjuttavissa
        }
        // kultaharkon heitto matkan päästä: taskusta, veto taakse, pään korkeudelta suoraan eteen (7 kuvaa)
        var barThrow = EnemySheet("horhe_harkko", report);
        var barSprite = ImportPropCentered("Assets/Sprites/Rekvisiitta/kultaharkko.png");
        if (barThrow.Length >= 7 && barSprite != null) { t.barThrowSprites = barThrow.Take(7).ToArray(); t.barSprite = barSprite; t.barThrowChance = 0.5f; t.barDamage = 16; }
        // suojaus: kädet ylös kasvojen eteen (4 kuvaa) ja takaisin
        var guard = EnemySheet("horhe_torjunta", report);
        if (guard.Length >= 4)
        {
            t.blockSprites = new[] { guard[0], guard[1], guard[2], guard[3], guard[3], guard[2], guard[1] };
            t.blockTime = 0.5f; t.blockChance = 0.3f; t.maxBlocksInRow = 2;
        }
        t.moveSpeedX = 2.4f; t.moveSpeedY = 1.5f;
        t.maxHealth = 260;
        t.attackCooldown = 1.1f;
        t.hurtSounds = LoadClips("Assets/Audio/big thug", "gasp");
        t.hurtVolume = 0.99f;
        t.bigBody = true;                  // hero heittää kuperkeikalla kuten Koviksen
        SetBossThrown(t, EnemySheet("horhe_heitetty", report));
        t.wakeDistance = 7f;
        t.runSpeedMultiplier = 1.15f; t.flankChance = 0.1f; t.retreatChance = 0f; t.blockChance = 0f;
        float hx = LoipUpX0 + 1250f / ppu;   // pokeripöydän ja kirjoituspöydän välissä
        go.transform.position = new Vector3(hx, Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.35f), 0f);
        Undo.RegisterCreatedObjectUndo(go, "Horhe");

        EditorSceneManager.MarkSceneDirty(bg.scene);
        Info($"El Loipparin yläkerta luotu ({wU:0} yksikköä). Portaat lavan vasemmalta puolelta (x = {stairX:0.0}).\n" +
             "Välipomo Horhe: suora, takasuora, ote ja turpaanveto (3 iskua) sekä läpsykombo (10 läpsyä, vain suojaus auttaa).\n" +
             string.Join("\n", report) + "\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Aloituskohta ----------------
    [MenuItem("Beat em up/36. Aloita peli takakujan parkkipaikalta")]
    static void StartAtAlley()
    {
        var alley = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Takakuja");
        var bike = GameObject.Find("Takakujan prätkä");
        if (alley == null || bike == null) { Info("Tee ensin kohta 34 (takakuja ja prätkä)."); return; }
        var old = GameObject.Find("Aloituskohta");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var go = new GameObject("Aloituskohta");
        var gs = go.AddComponent<GameStart>();
        gs.area = alley;
        gs.position = new Vector2(bike.transform.position.x - 2.5f, bike.transform.position.y);   // pyörän vieressä
        Undo.RegisterCreatedObjectUndo(go, "Aloituskohta");
        EditorSceneManager.MarkSceneDirty(go.scene);
        Info("Peli alkaa nyt takakujan parkkipaikalta prätkän vierestä.\nTakaisin kadun alkuun: valikko 37 (tai poista objekti \"Aloituskohta\").\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Samoalainen (Uccopulco) ----------------
    static readonly Vector2[] SamoaUcco = { new Vector2(30f, 0.4f), new Vector2(62f, 0.15f), new Vector2(95f, 0.6f), new Vector2(158f, 0.5f), new Vector2(194f, 0.35f) };   // x kadun alusta (yli 121: satama), syvyys 0 = seinä … 1 = edessä, syvyys 0 = seinä … 1 = edessä

    [MenuItem("Beat em up/47. Samoalaiset Uccopulcoon")]
    static void AddSamoans()
    {
        var ucco = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Uccopulco");
        if (ucco == null) { Info("Tee ensin kohta 44 (Uccopulco)."); return; }
        foreach (var n in new[] { "samoa_idle", "samoa_osuma", "samoa_kaatuminen", "samoa_ylosnousu", "samoa_kavely", "samoa_lyonnit", "samoa_taklaus", "samoa_heitto" })
        {
            string path = FindTexture(n);
            if (path != null) SetupAndSlice(path);
        }
        var report = new List<string>();
        Sprite[] idle = EnemySheet("samoa_idle", report);
        if (idle.Length == 0) { Info("samoa_idle.png puuttuu."); return; }
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (e != null && e.gameObject.name.StartsWith("Samoalainen")) Undo.DestroyObjectImmediate(e.gameObject);

        var go = new GameObject("Samoalainen");
        var visual = new GameObject("Visual").AddComponent<SpriteRenderer>();
        visual.transform.SetParent(go.transform, false);
        var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>();
        shadow.transform.SetParent(go.transform, false);
        var t = go.AddComponent<Enemy>();
        t.body = visual; t.shadow = shadow; visual.sprite = idle[0];
        t.displayName = "Samoalainen";
        t.idleSprites = idle;
        t.idleFrameTime = 0.18f;
        var walk = EnemySheet("samoa_kavely", report);       // 16 kuvaa videosta, 1.8 s askelsykli
        t.walkSprites = walk.Length > 0 ? walk : idle;
        t.walkFrameTime = 0.085f;            // askeleet tahdissa nopeampaan kävelyyn (ennen 0.11)
        // lyöntikombo: kämmenisku (kuva 5) ja heti perään toinen (kuva 7)
        t.punchSprites = EnemySheet("samoa_lyonnit", report);
        t.punchImpactFrame = 4;
        t.secondImpactFrame = 6;
        t.windupTime = 0.16f;              // nopeat kämmeniskut (ennen 0.3 / 0.55)
        t.punchActiveTime = 0.08f;
        t.punchRecoverTime = 0.3f;
        t.attackCooldown = 1.0f;
        t.attackRange = 2.1f;
        // erikoisliike: taklaus kaukaa, ei voi torjua, lennättää reilusti taaksepäin
        t.altAttackSprites = EnemySheet("samoa_taklaus", report);   // 11 kuvaa: asento, kyyky, syöksy, sukellus, nousu
        t.altImpactFrame = 4;
        t.altDamage = 22;
        t.altReach = 1.8f;
        t.altChance = 0.35f;
        t.altExtraWindup = 0.2f;
        t.altTimeScale = 1.2f;
        t.altLungeSpeed = 13f;
        t.altLungeTime = 0.5f;
        t.altKnockdown = true;
        t.altUnblockable = true;
        t.altKnockSpeed = 13f;
        t.altKnockUp = 7f;
        t.chargeRange = 8f;
        t.chargeMinRange = 2.8f;
        t.stompSounds = LoadClips("Assets/Audio/sfx", "tomina");
        t.stompVolume = 0.8f;
        // heron heitto: kuperkeikka omilla kuvilla kuten portsarilla (0–2 ote, 3–8 lento, 9–11 maassa)
        t.flipThrownSprites = EnemySheet("samoa_heitto", report);
        t.flipFlightFrames = 6; t.flipFlightFrameTime = 0.07f; t.flipLandFrameTime = 0.1f;
        t.hurtSprites = EnemySheet("samoa_osuma", report);
        t.knockdownSprites = EnemySheet("samoa_kaatuminen", report);   // 7 kuvaa: horjuu, kaatuu, kierähtää, tähdet, makaa
        t.getUpSprites = EnemySheet("samoa_ylosnousu", report);        // 10 kuvaa
        t.getUpTime = 1.0f;
        t.bigBody = true;                     // iso: heitetään kuperkeikalla, kaataa muita lentäessään
        t.maxHealth = 150;
        t.moveSpeedX = 2.5f; t.moveSpeedY = 1.5f;   // ennen 1.9 / 1.2
        t.punchDamage = 12;
        t.runSpeedMultiplier = 1.1f; t.flankChance = 0.05f; t.retreatChance = 0f; t.blockChance = 0f;
        t.hurtSounds = LoadClips("Assets/Audio/big thug", "gasp");   // Koviksen äänet toistaiseksi
        t.hurtVolume = 0.99f;
        Undo.RegisterCreatedObjectUndo(go, "Samoalainen");

        for (int i = 0; i < SamoaUcco.Length; i++)
        {
            var v = SamoaUcco[i];
            var c = i == 0 ? go : Object.Instantiate(go);
            if (i > 0) { c.name = "Samoalainen_" + (i + 1); Undo.RegisterCreatedObjectUndo(c, "Samoalainen"); }
            float y = Mathf.Lerp(ucco.maxDepthY, ucco.minDepthY, v.y);
            c.transform.position = new Vector3(UccoX0 + v.x * UccoK, y, 0f);
        }
        EditorSceneManager.MarkSceneDirty(go.scene);
        Info($"Samoalaisia Uccopulcossa: {SamoaUcco.Length}\n\n" + string.Join("\n", report) +
             "\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/80. El Loipparin alkuun snookerpöytä")]
    static void AddLoipparSnooker()
    {
        var area = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "El Loippari");
        var bg = GameObject.Find("El Loippari");
        if (area == null || bg == null) { Info("Tee ensin kohta 45 (El Loippari)."); return; }
        var old = GameObject.Find("El Loipparin snooker");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var sr = bg.GetComponent<SpriteRenderer>();
        float ppu = sr.sprite.pixelsPerUnit;
        // ovelta sisään tullessa ensimmäisenä, keskisyvyydellä
        float x = LoipX0 + LoipExitPx / ppu + 7.5f;
        float y = Mathf.Lerp(area.MaxDepthAtX(x, area.maxDepthY) - 0.3f, area.minDepthY + 0.4f, 0.5f);
        // tieltä pois pöydät (pulloineen ja annoksineen) ja tuolit, jotka osuisivat snookerpöydän päälle
        float half = PokerPoolWidth * 0.5f + 1.3f;
        int removed = 0;
        foreach (var c in Object.FindObjectsByType<Crate>(FindObjectsSortMode.None).ToList())
        {
            if (c == null || c.gameObject.name != "Pöytä") continue;
            Vector3 q = c.transform.position;
            if (Mathf.Abs(q.x - x) > half || Mathf.Abs(q.y - y) > 1.3f) continue;
            foreach (var b in Object.FindObjectsByType<Bottle>(FindObjectsSortMode.None)) if (b != null && b.table == c) Undo.DestroyObjectImmediate(b.gameObject);
            foreach (var f in Object.FindObjectsByType<FishPlate>(FindObjectsSortMode.None)) if (f != null && f.table == c) Undo.DestroyObjectImmediate(f.gameObject);
            Undo.DestroyObjectImmediate(c.gameObject); removed++;
        }
        foreach (var ch in Object.FindObjectsByType<Chair>(FindObjectsSortMode.None).ToList())
        {
            if (ch == null) continue;
            Vector3 q = ch.transform.position;
            if (Mathf.Abs(q.x - x) <= half && Mathf.Abs(q.y - y) <= 1.3f) { Undo.DestroyObjectImmediate(ch.gameObject); removed++; }
        }
        var root = new GameObject("El Loipparin snooker");
        Undo.RegisterCreatedObjectUndo(root, "El Loipparin snooker");
        var t = AddSnookerTable(root.transform, new Vector2(x, y), PokerPoolWidth);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info(t != null ? $"El Loipparin alkuun snookerpöytä palloineen ja keppeineen (x = {x:0.0}). Tieltä poistettu {removed} pöytää/tuolia.\n\nTallenna scene (Ctrl+S)." : "biljardipoyta.png puuttuu.");
    }

    // ---------------- El Loipparin pöydät ----------------
    // x sisätilan alusta (yks), syvyys 0 = seinä … 1 = edessä
    // baari täyteen: takarivi seinän vieressä, eturivi edessä lomittain, keskelle muutama (tappelutilaa jää)
    static List<Vector2> LoipTableSpots()
    {
        // x-paikat vanhassa mittakaavassa (kerrotaan LoipK:lla sijoittaessa); väli pidetään maailmassa ennallaan
        var l = new List<Vector2>();
        float k = LoipK, end = 95f;
        for (float x = 7f; x <= end; x += 5.5f / k) l.Add(new Vector2(x, 0.2f));
        for (float x = 9.5f; x <= end; x += 5.5f / k) l.Add(new Vector2(x, 0.92f));
        for (float x = 13f; x <= end; x += 11f / k) l.Add(new Vector2(x, 0.56f));   // keskirivi harvemmin: tappelutilaa jää
        return l;
    }
    // pyöreät telkkaripöydät seinän vieressä takarivin pöytien välissä (ei tiskin eteen eikä lavan eteen)
    static readonly float[] LoipTvX = { 25f, 32f, 64.5f, 71.5f, 78.5f, 92f };

    [MenuItem("Beat em up/48. El Loipparin pöydät (kala-annokset, pullot, lasit)")]
    static void AddLoipparTables()
    {
        var area = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "El Loippari");
        string tp = FindTexture("poyta");
        if (area == null || tp == null) { Info("Tarvitaan El Loippari (kohta 45) ja poyta.png."); return; }
        // kala-annoksen kuvat (Resources/Kala): yksittäisiä spritejä, annos alareunasta, muut keskeltä
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Kala" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100; ti.alphaIsTransparency = true; ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            var st = new TextureImporterSettings(); ti.ReadTextureSettings(st);
            st.spriteAlignment = (int)(Path.GetFileNameWithoutExtension(path) == "annos" ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
            ti.SetTextureSettings(st);
            ti.SaveAndReimport();
        }
        SetupAndSlice(tp);
        var table = LoadSprites("poyta").OrderBy(sp => int.TryParse(sp.name.Substring(sp.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
        Sprite[] Sheet7(string n)
        {
            string bp = FindTexture(n); if (bp == null) return null;
            SetupAndSlice(bp);
            return LoadSprites(n).OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
        }
        var kinds = new List<(Sprite[] sp, string stain, float sc)>();
        foreach (var (n, st, sc) in new[] { ("pullo_olut", "olut", 1f), ("pullo_sininen", "sininen", 1.12f), ("pullo_likoori", "likoori", 1.25f), ("pullo_vodka", "vodka", 1.4f) })
        { var sp = Sheet7(n); if (sp != null && sp.Length >= 7) kinds.Add((sp, st, sc)); }
        var glasses = new List<(Sprite[] sp, string stain, bool tall)>();
        foreach (var (n, st, tall) in new[] { ("pullo_lasi_tumbler", "-", false), ("pullo_lasi_viski", "likoori", false), ("pullo_lasi_olut", "olut", true), ("pullo_lasi_tuoppi", "-", true) })
        { var sp = Sheet7(n); if (sp != null && sp.Length >= 5) glasses.Add((sp, st, tall)); }
        var glass = LoadClips("Assets/Audio/sfx", "glass");
        var plateSnd = LoadClips("Assets/Audio/sfx", "posliini");   // lautasen hajoaminen
        Sprite[] round = new Sprite[0], tvSprites = new Sprite[0];
        string rp = FindTexture("poyta_tv"), tvp = FindTexture("telkkari");
        if (rp != null) { SetupAndSlice(rp); round = LoadSprites("poyta_tv").OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray(); }
        if (tvp != null) { SetupAndSlice(tvp); tvSprites = LoadSprites("telkkari").OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray(); }

        var old = GameObject.Find("El Loipparin pöydät");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = new GameObject("El Loipparin pöydät");
        Undo.RegisterCreatedObjectUndo(root, "El Loipparin pöydät");
        var rnd = new System.Random(11);
        int plates = 0, bottles = 0, glassesN = 0, tvs = 0;
        float top = 1.28f * TableScale - 0.04f;
        if (round.Length >= 14 && tvSprites.Length >= 11)
            foreach (float tx in LoipTvX)
            {
                float ty = Mathf.Lerp(area.maxDepthY - 0.3f, area.minDepthY + 0.4f, 0.04f);
                var go = new GameObject("Telkkaripöytä");
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(LoipX0 + tx * LoipK, ty, 0f);
                var vis = new GameObject("Visual").AddComponent<SpriteRenderer>(); vis.transform.SetParent(go.transform, false);
                var sh = new GameObject("Shadow").AddComponent<SpriteRenderer>(); sh.transform.SetParent(go.transform, false);
                sh.color = new Color(0f, 0f, 0f, 0.35f);
                var c = go.AddComponent<Crate>();
                c.body = vis; c.shadow = sh;
                c.sprites = new[] { round[0], round[1], round[2] };
                c.breakSprites = round.Skip(3).ToArray();
                c.breakFrameTime = 0.07f; c.hitsToBreak = 3; c.breakable = true; c.footOffset = 0.04f;
                c.visualScale = TvTableScale; c.shadowWidth = 1.3f; c.hitRadiusX = 0.8f * TvTableScale; c.debrisTime = 6f;
                c.carryLower = 0.84f * TvTableScale; c.plowThrough = true;
                c.moneyChance = 0.2f; c.energyChance = 0.1f; c.throwDamage = 20;
                vis.sprite = round[0];
                var tvGo = new GameObject("Telkkari");
                tvGo.transform.SetParent(root.transform, false);
                tvGo.transform.position = go.transform.position;
                var tv = tvGo.AddComponent<TvSet>();
                tv.sprites = tvSprites; tv.table = c; tv.tableTop = 1.16f * TvTableScale - 0.04f; tv.breakSounds = glass;
                tvs++;
            }
        var spots = LoipTableSpots();
        // lavan eteen ei takariviin (lava näkyy), tiskin eteen ei takariviin (kauppa)
        spots.RemoveAll(v => v.y < 0.3f && ((v.x > 38f && v.x < 62f) || (v.x > 12f && v.x < 22f)));
        spots.RemoveAll(v => v.y < 0.3f && LoipTvX.Any(tx => Mathf.Abs(tx - v.x) * LoipK < 2.4f));
        int tableNo = 0;
        foreach (var v in spots)
        {
            tableNo++;
            bool bottlesOnly = tableNo % 4 == 0;          // joka neljäs pöytä pelkkiä pulloja ja laseja
            float y = Mathf.Lerp(area.maxDepthY - 0.3f, area.minDepthY + 0.4f, v.y);
            var go = new GameObject("Pöytä");
            go.transform.SetParent(root.transform, false);
            go.transform.position = new Vector3(LoipX0 + v.x * LoipK, y, 0f);
            var vis = new GameObject("Visual").AddComponent<SpriteRenderer>(); vis.transform.SetParent(go.transform, false);
            var sh = new GameObject("Shadow").AddComponent<SpriteRenderer>(); sh.transform.SetParent(go.transform, false);
            sh.color = new Color(0f, 0f, 0f, 0.35f);
            var c = go.AddComponent<Crate>();
            c.body = vis; c.shadow = sh;
            c.sprites = new[] { table[0], table[1], table[2] };
            c.breakSprites = new[] { table[3], table[4], table[5], table[6] };
            c.breakFrameTime = 0.1f; c.hitsToBreak = 3; c.breakable = true; c.footOffset = 0.04f;
            c.visualScale = TableScale; c.shadowWidth = 2.6f; c.carryLower = 0.86f * TableScale; c.plowThrough = true;
            c.hitRadiusX = 1.4f * TableScale; c.debrisTime = 6f;
            c.moneyChance = 0.3f; c.energyChance = 0.15f; c.throwDamage = 22;
            vis.sprite = table[0];
            // pullopöytä: 7–9 pulloa ja 2 lasia; muuten 1–2 kala-annosta ja 3–5 pulloa tai lasia väliin
            int np = bottlesOnly ? 0 : (rnd.Next(3) == 0 ? 1 : 2);
            float[] plateX = np == 0 ? new float[0] : np == 1 ? new[] { (float)(rnd.NextDouble() - 0.5) * 0.4f } : new[] { -0.62f, 0.62f };
            foreach (float px in plateX)
            {
                var fGo = new GameObject("Kala-annos");
                fGo.transform.SetParent(root.transform, false);
                fGo.transform.position = go.transform.position;
                var f = fGo.AddComponent<FishPlate>();
                f.table = c; f.tableX = px * TableScale; f.tableTop = top + 0.04f; f.breakSounds = plateSnd.Length > 0 ? plateSnd : glass;
                plates++;
            }
            var slots = new List<float>();
            if (np == 0) for (int k = 0; k < 11; k++) slots.Add(Mathf.Lerp(-1.1f, 1.1f, (k + 0.5f) / 11f));
            else if (np == 1) slots.AddRange(new[] { -1.05f, -0.85f, -0.65f, 0.65f, 0.85f, 1.05f });
            else slots.AddRange(new[] { -1.12f, -0.1f, 0.1f, 1.12f });
            int items = np == 0 ? 9 + rnd.Next(3) : (np == 1 ? 4 + rnd.Next(2) : 3 + rnd.Next(2));
            for (int i = 0; i < items && slots.Count > 0; i++)
            {
                int si = rnd.Next(slots.Count); float sx = slots[si]; slots.RemoveAt(si);
                var bGo = new GameObject("Pullo");
                bGo.transform.SetParent(root.transform, false);
                bGo.transform.position = go.transform.position;
                var b = bGo.AddComponent<Bottle>();
                bool isGlass = glasses.Count > 0 && (kinds.Count == 0 || rnd.Next(3) == 0);   // useammin pulloja
                if (isGlass)
                {
                    var gl = glasses[rnd.Next(glasses.Count)];
                    b.sprites = gl.sp; b.stainKind = gl.stain; b.pivotY = gl.tall ? 0.2f : 0.15f; bGo.name = "Lasi"; glassesN++;
                }
                else if (kinds.Count > 0)
                {
                    var k = kinds[rnd.Next(kinds.Count)];
                    b.sprites = k.sp; b.stainKind = k.stain;
                    b.scale = 1.05f * k.sc * (0.94f + 0.12f * (float)rnd.NextDouble());
                    bottles++;
                }
                else { Object.DestroyImmediate(bGo); continue; }
                b.breakSounds = glass;
                b.table = c;
                b.tableX = (sx + (float)(rnd.NextDouble() - 0.5) * 0.08f) * TableScale;
                b.tableTop = top;
            }
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info($"El Loippariin {spots.Count} pöytää ja {tvs} telkkaripöytää: {plates} kala-annosta, {bottles} pulloa, {glassesN} lasia.\n" +
             "Lyönti pöytään: annos valuu lattialle. Potku, heitetty pöytä tai päälle lentävä vihu: annos räjähtää ja kala lentää.\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Uccopulcon ja El Loipparin rekvisiitta ----------------
    static readonly float[] UccoCrateX = { 14f, 33f, 48f, 70f, 88f, 112f, 132f, 152f, 176f }, UccoBarrelX = { 20f, 41f, 58f, 79f, 95f, 116f, 140f, 166f, 186f };
    static readonly float[] LoipCrateX = { 7f, 46f, 79f, 96f }, LoipBarrelX = { 9.5f, 49f, 82f };
    const float LoipCounterPx = 865f, LoipCounterHalfPx = 225f, LoipCounterTopRow = 370f;

    [MenuItem("Beat em up/50. Uccopulco: laatikot, tynnyrit ja Sohvi El Loipparin tiskille")]
    static void AddUccoProps()
    {
        var ucco = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Uccopulco");
        var loip = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "El Loippari");
        if (ucco == null || loip == null) { Info("Tee ensin kohdat 44 ja 45 (Uccopulco ja El Loippari)."); return; }
        var crates = Object.FindObjectsByType<Crate>(FindObjectsSortMode.None);
        var crateT = crates.FirstOrDefault(c => c.breakable && c.stackedOn == null && c.gameObject.name.StartsWith("Laatikko"));
        var barrelT = crates.FirstOrDefault(c => !c.breakable && c.rollSprites != null && c.rollSprites.Length > 0);
        foreach (var n in new[] { "Uccopulcon rekvisiitta", "El Loipparin Sohvi", "El Loipparin tiski" })
        {
            var o = GameObject.Find(n);
            if (o != null) Undo.DestroyObjectImmediate(o);
        }
        var root = new GameObject("Uccopulcon rekvisiitta");
        Undo.RegisterCreatedObjectUndo(root, "Uccopulcon rekvisiitta");
        int nc = 0, nb = 0;
        Crate Place(Crate t, float x, float y, string name)
        {
            if (t == null) return null;
            var go = Object.Instantiate(t.gameObject, root.transform);
            go.name = name;
            go.transform.position = new Vector3(x, y, 0f);
            var c = go.GetComponent<Crate>(); c.stackedOn = null;
            return c;
        }
        // joka toisen laatikon päälle toinen (hieman edessä, jotta piirtyy päälle)
        void Stack(Crate b, string name)
        {
            if (b == null) return;
            var top = Place(crateT, b.transform.position.x + 0.08f, b.transform.position.y - 0.02f, name);
            if (top != null) top.stackedOn = b;
        }
        // kadulla jalkakäytävällä seinän vieressä
        float sideY = Mathf.Lerp(ucco.maxDepthY, ucco.curbDepthY, 0.35f);
        for (int i = 0; i < UccoCrateX.Length; i++)
        {
            var b = Place(crateT, UccoX0 + UccoCrateX[i] * UccoK, sideY - 0.05f * (nc % 2), "Laatikko U" + (++nc));
            if (i % 2 == 0) Stack(b, "Laatikko U" + (++nc) + " (päällä)");
        }
        foreach (float x in UccoBarrelX) Place(barrelT, UccoX0 + x * UccoK, sideY + 0.05f, "Tynnyri U" + (++nb));
        // El Loipparissa seinien vieressä
        float wallY = loip.maxDepthY - 0.15f;
        for (int i = 0; i < LoipCrateX.Length; i++)
        {
            var b = Place(crateT, LoipX0 + LoipCrateX[i] * LoipK, wallY, "Laatikko L" + (++nc));
            if (i % 3 == 1) Stack(b, "Laatikko L" + (++nc) + " (päällä)");
        }
        foreach (float x in LoipBarrelX) Place(barrelT, LoipX0 + x * LoipK, wallY - 0.05f, "Tynnyri L" + (++nb));

        // Sohvi El Loipparin tiskin taakse ja kauppa tiskin eteen (kopiot S-Clubin Sohvista ja baaritiskistä)
        string sohviInfo = "Sohvi: tee ensin kohta 14 (S-Clubin baaritiski ja Sohvi)";
        var sohvi = GameObject.Find("Sohvi");
        var shop = GameObject.Find("Baaritiski");
        var loipBg = GameObject.Find("El Loippari");
        if (sohvi != null && shop != null && loipBg != null)
        {
            var bgSr = loipBg.GetComponent<SpriteRenderer>();
            float ppu = bgSr.sprite.pixelsPerUnit;
            float top = loipBg.transform.position.y + bgSr.bounds.size.y * 0.5f;
            float cx = bgSr.bounds.min.x + LoipCounterPx / ppu;
            var s2 = Object.Instantiate(sohvi);
            s2.name = "El Loipparin Sohvi";
            s2.transform.position = new Vector3(cx, top - LoipCounterTopRow / ppu, 0f);
            s2.transform.localScale = sohvi.transform.localScale * 0.85f;   // vähän pienempi kuin S-Clubissa
            Undo.RegisterCreatedObjectUndo(s2, "Sohvi");
            var sh2 = Object.Instantiate(shop);
            sh2.name = "El Loipparin tiski";
            sh2.transform.position = new Vector3(cx, loip.maxDepthY, 0f);
            var counter = sh2.GetComponent<ShopCounter>();
            if (counter != null)
            {
                counter.here = loip;
                counter.halfWidth = LoipCounterHalfPx / ppu;
                counter.title = "EL LOIPPARI";
            }
            Undo.RegisterCreatedObjectUndo(sh2, "Tiski");
            sohviInfo = "Sohvi El Loipparin tiskin takana, kauppa tiskin edessä";
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info($"Uccopulco ja El Loippari: {nc} laatikkoa, {nb} tynnyriä" + (crateT == null || barrelT == null ? " (mallilaatikko tai -tynnyri puuttui: tee kohdat 23 ja 32)" : "") +
             $".\n{sohviInfo}.\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- El Loipparin tappelijat ----------------
    static readonly Vector2[] LoipLippis = { new Vector2(18f, 0.5f), new Vector2(38f, 0.7f), new Vector2(60f, 0.4f), new Vector2(88f, 0.6f) };
    static readonly Vector2[] UccoLippis = { new Vector2(16f, 0.5f), new Vector2(45f, 0.3f), new Vector2(78f, 0.65f), new Vector2(108f, 0.4f), new Vector2(136f, 0.6f), new Vector2(172f, 0.3f) };
    static readonly Vector2[] UccoSkettari = { new Vector2(23f, 0.7f), new Vector2(54f, 0.55f), new Vector2(86f, 0.3f), new Vector2(116f, 0.6f), new Vector2(146f, 0.45f), new Vector2(184f, 0.65f) };
    static readonly Vector2[] LoipSamoa = { new Vector2(27f, 0.45f), new Vector2(50f, 0.6f), new Vector2(77f, 0.5f) };

    [MenuItem("Beat em up/51. El Loippariin Lippikset, samoalaiset ja portsarit")]
    static void AddLoipparFighters()
    {
        var loip = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "El Loippari");
        var loipBg = GameObject.Find("El Loippari");
        if (loip == null || loipBg == null) { Info("Tee ensin kohta 45 (El Loippari)."); return; }
        var all = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var e in all)
            if (e != null && e.gameObject.name.StartsWith("Loippari ")) Undo.DestroyObjectImmediate(e.gameObject);
        var oldSquad = GameObject.Find("El Loipparin portsarit");
        if (oldSquad != null) Undo.DestroyObjectImmediate(oldSquad);
        all = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var lippisT = all.FirstOrDefault(e => e.gameObject.name == "Lippis");
        var samoaT = all.FirstOrDefault(e => e.gameObject.name == "Samoalainen");
        var oldRoot = GameObject.Find("El Loipparin tappelijat");
        if (oldRoot != null) Undo.DestroyObjectImmediate(oldRoot);
        var root = new GameObject("El Loipparin tappelijat");
        Undo.RegisterCreatedObjectUndo(root, "El Loipparin tappelijat");
        var report = new List<string>();
        int Place(Enemy t, Vector2[] spots, string name)
        {
            if (t == null) { report.Add(name + ": malli puuttuu"); return 0; }
            int n = 0;
            foreach (var v in spots)
            {
                var go = Object.Instantiate(t.gameObject, root.transform);
                go.name = "Loippari " + name + " " + (++n);
                go.SetActive(true);
                go.transform.position = new Vector3(LoipX0 + v.x * LoipK, Mathf.Lerp(loip.maxDepthY, loip.minDepthY, v.y), 0f);
            }
            report.Add($"{name}: {n}");
            return n;
        }
        Place(lippisT, LoipLippis, "Lippis");
        Place(samoaT, LoipSamoa, "Samoalainen");

        // Uccopulcon kadulle myös Lippikset ja Skettarit (samoalaisten väleihin)
        var ucco = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Uccopulco");
        var skettT = all.FirstOrDefault(e => e.gameObject.name == "Skettari");
        foreach (var e in all)
            if (e != null && e.gameObject.name.StartsWith("Ucco ")) Undo.DestroyObjectImmediate(e.gameObject);
        var oldU = GameObject.Find("Uccopulcon tappelijat");
        if (oldU != null) Undo.DestroyObjectImmediate(oldU);
        if (ucco != null)
        {
            var uRoot = new GameObject("Uccopulcon tappelijat");
            Undo.RegisterCreatedObjectUndo(uRoot, "Uccopulcon tappelijat");
            void PlaceU(Enemy t, Vector2[] spots, string name)
            {
                if (t == null) { report.Add("Uccopulco " + name + ": malli puuttuu"); return; }
                int n = 0;
                foreach (var v in spots)
                {
                    var go = Object.Instantiate(t.gameObject, uRoot.transform);
                    go.name = "Ucco " + name + " " + (++n);
                    go.SetActive(true);
                    go.transform.position = new Vector3(UccoX0 + v.x * UccoK, Mathf.Lerp(ucco.maxDepthY, ucco.minDepthY, v.y), 0f);
                }
                report.Add($"Uccopulco {name}: {n}");
            }
            PlaceU(lippisT, UccoLippis, "Lippis");
            PlaceU(skettT, UccoSkettari, "Skettari");
        }

        // portsarit: kopio S-Clubin ryhmästä, tulevat Loipparin heiluriovista ja oikeasta reunasta ensimmäisestä iskusta
        var squad = Object.FindObjectsByType<BouncerSquad>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(q => q.gameObject.name == "Portsarit");
        if (squad != null)
        {
            var sGo = Object.Instantiate(squad.gameObject);
            sGo.name = "El Loipparin portsarit";
            Undo.RegisterCreatedObjectUndo(sGo, "Portsarit");
            var sq = sGo.GetComponent<BouncerSquad>();
            sq.area = loip;
            float ppu = loipBg.GetComponent<SpriteRenderer>().sprite.pixelsPerUnit;
            float doorX = LoipX0 + LoipExitPx / ppu + 0.8f;
            for (int i = 0; i < sq.bouncers.Length; i++)
            {
                var b = sq.bouncers[i];
                if (b == null) continue;
                b.gameObject.name = "Loippari Portsari " + (i + 1);
                float y = Mathf.Lerp(loip.maxDepthY, loip.minDepthY, 0.3f + 0.2f * (i % 3));
                b.transform.position = new Vector3(doorX - 0.4f * i, y, 0f);
                b.gameObject.SetActive(false);
            }
            report.Add($"Portsarit: {sq.bouncers.Length} (tulevat ensimmäisestä iskusta)");
        }
        else report.Add("Portsarit: tee ensin kohta 43");
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info("El Loippari:\n" + string.Join("\n", report) + "\n\nPortsarit käyvät lähimmän kimppuun: hero, Lippikset ja samoalaiset.\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- El Loipparin lava ----------------
    const float LoipStagePx = 2334f, LoipStageHalfPx = 505f, LoipStageFeetRow = 445f;   // lava yhdistetyssä sisäkuvassa (keskikohta, puolileveys, jalkojen rivi)

    [MenuItem("Beat em up/52. El Loipparin lava: mariachi-bändi ja tanssijat")]
    static void AddLoipparStage()
    {
        var loipBg = GameObject.Find("El Loippari");
        var band = GameObject.Find("Bändi");
        var bellySprites = LoadSprites("tanssija")
            .OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        if (loipBg == null || band == null || bellySprites.Length == 0) { Info("Tarvitaan El Loippari (45), bändi (40) ja tanssija.png."); return; }
        var old = GameObject.Find("El Loipparin lava");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = new GameObject("El Loipparin lava");
        Undo.RegisterCreatedObjectUndo(root, "El Loipparin lava");
        var bgSr = loipBg.GetComponent<SpriteRenderer>();
        float ppu = bgSr.sprite.pixelsPerUnit;
        float left = bgSr.bounds.min.x, top = bgSr.bounds.max.y;
        float feetY = top - LoipStageFeetRow / ppu;
        var b = Object.Instantiate(band, root.transform);
        b.name = "Loipparin bändi";
        b.transform.position = new Vector3(left + LoipStagePx / ppu, feetY, 0f);
        var bsr = b.GetComponent<SpriteRenderer>(); if (bsr != null) bsr.color = new Color(1f, 0.95f, 0.88f);   // lämmin valo
        // napatanssijat (tanssija.png) lavan molemmin puolin bändiä
        for (int i = 0; i < 2; i++)
        {
            var dGo = new GameObject("Loipparin tanssija " + (i + 1));
            dGo.transform.SetParent(root.transform, false);
            float side = i == 0 ? -1f : 1f;
            dGo.transform.position = new Vector3(left + (LoipStagePx + side * (LoipStageHalfPx - 110f)) / ppu, feetY, 0f);
            dGo.transform.localScale = new Vector3(DancerScale, DancerScale, 1f);
            var dsr = dGo.AddComponent<SpriteRenderer>();
            dsr.sprite = bellySprites[0];
            dsr.sortingOrder = -9000;
            dsr.color = new Color(1f, 0.95f, 0.9f);   // lämmin valo
            var dn = dGo.AddComponent<Dancer>();
            dn.sprites = bellySprites;
            dn.frameTime = 0.124f;
            dn.startFrame = i * bellySprites.Length / 2;   // eri tahdissa
            dn.flipX = side > 0f;                          // katsovat bändiä kohti
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info("El Loipparin lavalla mariachi-bändi ja tanssijat molemmin puolin.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/54. Heron pudotuspotku (juoksusta lyönti + hyppy)")]
    static void SetupDropKick()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        string p = FindTexture("pudotuspotku");
        if (pc == null || p == null) { Info("Tarvitaan pelaaja ja pudotuspotku.png."); return; }
        SetupAndSlice(p);
        Undo.RecordObject(pc, "Pudotuspotku");
        pc.runStaminaPerSecond = 7f;           // juoksu kuluttaa puolet vähemmän (ennen 14)
        pc.dropKickSprites = LoadSprites("pudotuspotku").OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
        EditorUtility.SetDirty(pc);
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        Info($"Pudotuspotku: {pc.dropKickSprites.Length} kuvaa. Juoksusta lyönti + hyppy yhtä aikaa: loikka jalat edellä, selälleen ja kip-up.\n\nTallenna scene (Ctrl+S).");
    }

    // ---------------- Tuolit ----------------
    static readonly Vector2[] LoipChairs = { new Vector2(15f, 0.62f), new Vector2(29.5f, 0.35f), new Vector2(36f, 0.7f), new Vector2(57f, 0.68f),
                                             new Vector2(67f, 0.4f), new Vector2(75f, 0.65f), new Vector2(86f, 0.45f), new Vector2(97f, 0.7f) };

    [MenuItem("Beat em up/53. Tuolit El Loippariin (heron tuolikuvat)")]
    static void AddChairs()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        var area = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "El Loippari");
        if (pc == null || area == null) { Info("Tarvitaan pelaaja ja El Loippari (kohta 45)."); return; }
        Sprite[] Sh(string n)
        {
            string p = FindTexture(n); if (p == null) return new Sprite[0];
            SetupAndSlice(p);
            return LoadSprites(n).OrderBy(x => int.TryParse(x.name.Substring(x.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
        }
        Undo.RecordObject(pc, "Tuolikuvat");
        pc.chairPickSprites = Sh("tuoli_nosto");
        pc.chairHoldSprites = Sh("tuoli_pito");
        pc.chairWalkSprites = Sh("tuoli_kavely");
        pc.chairSmashSprites = Sh("tuoli_lyonti");
        pc.chairThrowSprites = Sh("tuoli_heitto");
        pc.chairSwingSprites = Sh("tuolilyonti"); pc.chairSwingBareSprites = Sh("tuolilyonti_tyhja");
        EditorUtility.SetDirty(pc);
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Tuoli" }))
        {
            var ti = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100; ti.alphaIsTransparency = true; ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }
        var chairSprite = ImportProp("Assets/Sprites/Rekvisiitta/tuoli.png");
        var wood = AssetDatabase.FindAssets("t:AudioClip puu", new[] { "Assets/Audio" }).Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => Path.GetFileNameWithoutExtension(p).ToLowerInvariant().StartsWith("puu")).Select(AssetDatabase.LoadAssetAtPath<AudioClip>).Where(c => c != null).ToArray();
        var old = GameObject.Find("Tuolit");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = new GameObject("Tuolit");
        Undo.RegisterCreatedObjectUndo(root, "Tuolit");
        int n = 0;
        if (chairSprite != null)
            foreach (var v in LoipChairs)
            {
                var go = new GameObject("Tuoli " + (++n));
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(LoipX0 + v.x * LoipK, Mathf.Lerp(area.maxDepthY, area.minDepthY, v.y), 0f);
                var b = new GameObject("Visual").AddComponent<SpriteRenderer>(); b.transform.SetParent(go.transform, false);
                b.sprite = chairSprite;
                b.flipX = n % 2 == 0;
                var sh = new GameObject("Shadow").AddComponent<SpriteRenderer>(); sh.transform.SetParent(go.transform, false);
                sh.color = new Color(0f, 0f, 0f, 0.3f);
                var c = go.AddComponent<Chair>();
                c.body = b; c.shadow = sh; c.breakSounds = wood;
            }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info($"El Loippariin {n} tuolia. Heron tuolikuvat: nosto {pc.chairPickSprites.Length}, pito {pc.chairHoldSprites.Length}, kävely {pc.chairWalkSprites.Length}, lyönti {pc.chairSmashSprites.Length}, heitto {pc.chairThrowSprites.Length}.\n" +
             "Kiinniotto: tuoli käteen. Lyönti: lyö (osuessa tuoli hajoaa). Potku: heitto.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/81. Horhen konttoriin snookerpöydät, pöydät, tuolit ja pullot")]
    static void AddLoipparUpstairsProps()
    {
        var area = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Loipparin yläkerta");
        var chairT = Object.FindObjectsByType<Chair>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c != null && c.transform.parent != null && c.transform.parent.name == "Tuolit");
        var loipTables = GameObject.Find("El Loipparin pöydät");
        if (area == null || loipTables == null) { Info("Tarvitaan Loipparin yläkerta (kohta 77) ja El Loipparin pöydät (kohta 48)."); return; }
        // mallipöytä: El Loipparin pullopöytä (ei kala-annoksia), pullot kopioidaan sen mukana
        var bottles = Object.FindObjectsByType<Bottle>(FindObjectsSortMode.None);
        var plates = Object.FindObjectsByType<FishPlate>(FindObjectsSortMode.None);
        var tableT = loipTables.GetComponentsInChildren<Crate>(true)
            .Where(c => c.gameObject.name == "Pöytä" && !plates.Any(f => f != null && f.table == c))
            .OrderByDescending(c => bottles.Count(b => b != null && b.table == c)).FirstOrDefault();
        if (tableT == null) { Info("El Loipparin pullopöytää ei löytynyt (kohta 48)."); return; }
        var old = GameObject.Find("Horhen konttorin kalusteet");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = new GameObject("Horhen konttorin kalusteet");
        Undo.RegisterCreatedObjectUndo(root, "Horhen konttorin kalusteet");
        float halfW = CamHalf * 16f / 9f;
        float x0 = area.camMinX - halfW, x1 = area.camMaxX + halfW;
        Vector3 At(float fx, float depth) { float x = Mathf.Lerp(x0, x1, fx); return new Vector3(x, Mathf.Lerp(area.maxDepthY - 0.3f, area.minDepthY + 0.4f, depth), 0f); }
        // kaksi snookerpöytää: toinen taakse vasemmalle, toinen eteen oikealle (portaiden eteen ja Horhen ympärille jää tilaa)
        int snookers = 0;
        if (AddSnookerTable(root.transform, At(0.3f, 0.2f), PokerPoolWidth) != null) snookers++;
        if (AddSnookerTable(root.transform, At(0.74f, 0.82f), PokerPoolWidth) != null) snookers++;
        // pari tavallista pöytää pulloineen
        int nb = 0;
        foreach (var pos in new[] { At(0.52f, 0.88f), At(0.9f, 0.18f) })
        {
            var go = Object.Instantiate(tableT.gameObject, root.transform);
            go.name = "Pöytä"; go.transform.position = pos; go.SetActive(true);
            var c = go.GetComponent<Crate>();
            foreach (var b in bottles)
            {
                if (b == null || b.table != tableT) continue;
                var bGo = Object.Instantiate(b.gameObject, root.transform);
                bGo.name = b.gameObject.name; bGo.transform.position = pos;
                bGo.GetComponent<Bottle>().table = c;
                nb++;
            }
        }
        // tuolit pöytien ja snookerpöytien viereen
        int nc = 0;
        if (chairT != null)
            foreach (var (pos, flip) in new[] { (At(0.45f, 0.9f), false), (At(0.59f, 0.86f), true), (At(0.84f, 0.2f), false), (At(0.17f, 0.45f), true) })
            {
                var go = Object.Instantiate(chairT.gameObject, root.transform);
                go.name = "Tuoli " + (++nc); go.transform.position = pos;
                var ch = go.GetComponent<Chair>();
                if (ch.body != null) ch.body.flipX = flip;
                go.SetActive(true);
            }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info($"Horhen konttoriin {snookers} snookerpöytää, 2 pöytää ({nb} pulloa/lasia) ja {nc} tuolia." + (chairT == null ? "\nTuolit puuttuvat: tee ensin kohta 53." : "") + "\n\nTallenna scene (Ctrl+S).");
    }

    /// Pomon heitettynä olemisen kuvat (8 kuvaa, 640 × 448): 0–1 ote, 2 nosto, 3–4 lento (ylösalaisin, kippura),
    /// 5 alastulo, 6–7 makaa. Hero heittää isot vastukset kuperkeikalla; lento näyttää kuvat 3 ja 4 vuorotellen.
    static bool SetBossThrown(Enemy e, Sprite[] sp)
    {
        if (e == null || sp == null || sp.Length < 8) return false;
        e.bigBody = true;
        e.flipThrownSprites = sp.Take(8).ToArray();
        e.flipFlightFrames = 2; e.flipFlightFrameTime = 0.2f; e.flipLandFrameTime = 0.11f;
        return true;
    }

    [MenuItem("Beat em up/86. Horhen ja kapteenin heitettynä olemisen kuvat")]
    static void ApplyBossThrown()
    {
        foreach (var n in new[] { "horhe_heitetty", "kapteeni_heitetty" })
        {
            string path = FindTexture(n);
            if (path != null) SetupAndSlice(path);
        }
        var report = new List<string>();
        var horhe = EnemySheet("horhe_heitetty", report);
        var capt = EnemySheet("kapteeni_heitetty", report);
        int nh = 0, nc = 0;
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (e == null) continue;
            if (e.displayName == "Horhe") { Undo.RecordObject(e, "Horhen heitto"); if (SetBossThrown(e, horhe)) { nh++; EditorUtility.SetDirty(e); } }
            else if (e.displayName == "Kapteeni") { Undo.RecordObject(e, "Kapteenin heitto"); if (SetBossThrown(e, capt)) { nc++; EditorUtility.SetDirty(e); } }
        }
        if (nh + nc > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Info($"Heitettynä olemisen kuvat: Horhe {nh}, kapteeni {nc}.\n" + string.Join("\n", report) + "\n\nTallenna scene (Ctrl+S).");
    }

    /// Etukäden alakoukku tavalliseksi lyönniksi (suoran tilalle), takakäden alakoukku lennättää kattoon (takasuoran tilalle).
    /// Kummassakin 5 kuvaa: lataus 0–2, isku 3→4. Ilman takakoukkua etukäden koukku lennättää.
    static void SetHorheHooks(Enemy e, Sprite[] front, Sprite[] rear)
    {
        if (rear.Length >= 5)
        {
            e.punch2Sprites = UppercutFrames(rear); e.punch2ImpactFrame = 4;
            if (front.Length >= 5) { e.punchSprites = UppercutFrames(front); e.punchImpactFrame = 4; }
        }
        else if (front.Length >= 5) { e.punch2Sprites = UppercutFrames(front); e.punch2ImpactFrame = 4; }
    }

    /// Alakoukku (5 kuvaa: suoja, kyykky, nousu, nyrkki leualla, nyrkki ylhäällä): lataus 0–2, isku 3→4, palautus 4.
    /// Ei toistoja: Enemy.Awake järjestää kuvat numeron mukaan.
    static Sprite[] UppercutFrames(Sprite[] h) => h.Take(5).ToArray();

    [MenuItem("Beat em up/82. Horhen juoksu, potkukombo ja alakoukut")]
    static void AddHorheRun()
    {
        string path = FindTexture("horhe_juoksu"), kp = FindTexture("horhe_potkut"), hp = FindTexture("horhe_koukku"), rp = FindTexture("horhe_takakoukku");
        if (path == null) { Info("horhe_juoksu.png puuttuu."); return; }
        SetupAndSlice(path);
        if (kp != null) SetupAndSlice(kp);
        if (hp != null) SetupAndSlice(hp);
        if (rp != null) SetupAndSlice(rp);
        var report = new List<string>();
        var run = EnemySheet("horhe_juoksu", report);
        var kicks = kp != null ? EnemySheet("horhe_potkut", report) : new Sprite[0];
        var hook = hp != null ? EnemySheet("horhe_koukku", report) : new Sprite[0];
        var rear = rp != null ? EnemySheet("horhe_takakoukku", report) : new Sprite[0];
        int n = 0;
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (e == null || e.displayName != "Horhe") continue;
            Undo.RecordObject(e, "Horhen juoksu");
            e.runSprites = run;
            if (kicks.Length >= 22) e.kickComboSprites = kicks.Take(22).ToArray();
            SetHorheHooks(e, hook, rear);
            EditorUtility.SetDirty(e); n++;
        }
        if (n > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Info($"Horhen juoksu ({run.Length} kuvaa) potkukombo ({kicks.Length} kuvaa) alakoukku ({hook.Length} kuvaa) ja takakäden alakoukku ({rear.Length} kuvaa) {n} Horhelle.\n" + string.Join("\n", report) + "\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/79. Tuolit S-Clubiin (potkittavat ja lyötävät)")]
    static void AddClubChairs()
    {
        var club = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "S-Club");
        var tmpl = Object.FindObjectsByType<Chair>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c != null && c.transform.parent != null && c.transform.parent.name == "Tuolit");
        if (club == null || tmpl == null) { Info("Tarvitaan S-Club (kohta 10) ja El Loipparin tuolit (kohta 53)."); return; }
        var old = GameObject.Find("S-Clubin tuolit");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = new GameObject("S-Clubin tuolit");
        Undo.RegisterCreatedObjectUndo(root, "S-Clubin tuolit");
        float halfW = CamHalf * 16f / 9f;
        float x0 = club.camMinX - halfW + 2.5f, x1 = club.camMaxX + halfW - 2.5f;
        const int n = 8;
        for (int i = 0; i < n; i++)
        {
            var go = Object.Instantiate(tmpl.gameObject, root.transform);
            go.name = "Tuoli " + (i + 1);
            float x = Mathf.Lerp(x0, x1, (i + 0.5f) / n) + ((i * 37) % 7 - 3) * 0.15f;
            float k = i % 2 == 0 ? 0.3f : 0.7f;   // vuorotellen taka- ja etuosaan
            go.transform.position = new Vector3(x, Mathf.Lerp(club.MaxDepthAtX(x, club.maxDepthY) - 0.2f, club.minDepthY + 0.3f, k), 0f);
            var c = go.GetComponent<Chair>();
            if (c.body != null) c.body.flipX = i % 2 == 0;
            go.SetActive(true);
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info($"S-Clubiin {n} tuolia (kopio El Loipparin tuolista): kiinniotolla käteen, lyönti hajottaa, potku lennättää.\n\nTallenna scene (Ctrl+S).");
    }

    /// Hajoaville puuesineille (pöydät, laatikot) hajoamisäänet Audio/sfx/puu*.
    [MenuItem("Beat em up/49. Puun hajoamisäänet pöydille ja laatikoille")]
    static void ApplyWoodBreakSounds()
    {
        var clips = AssetDatabase.FindAssets("t:AudioClip puu", new[] { "Assets/Audio" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => Path.GetFileNameWithoutExtension(p).ToLowerInvariant().StartsWith("puu"))
            .OrderBy(p => p).Select(AssetDatabase.LoadAssetAtPath<AudioClip>).Where(c => c != null).ToArray();
        int n = 0;
        foreach (var c in Object.FindObjectsByType<Crate>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!c.breakable) continue;
            Undo.RecordObject(c, "Hajoamisäänet");
            c.breakSounds = clips;
            EditorUtility.SetDirty(c);
            n++;
        }
        if (n > 0) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Info($"Hajoamisäänet ({clips.Length} kpl: puu*) {n} pöydälle ja laatikolle." + (clips.Length == 0 ? "\nLaita äänet kansioon Assets/Audio/sfx nimillä puu1, puu2." : "") + "\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/46. Aloita peli Uccopulcosta")]
    static void StartAtUccopulco()
    {
        var ucco = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Uccopulco");
        if (ucco == null) { Info("Tee ensin kohta 44 (Uccopulco)."); return; }
        var old = GameObject.Find("Aloituskohta");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var go = new GameObject("Aloituskohta");
        var gs = go.AddComponent<GameStart>();
        gs.area = ucco;
        gs.position = new Vector2(UccoX0 + 4f, Mathf.Lerp(ucco.curbDepthY, ucco.minDepthY, 0.3f));   // kadun alussa ajotiellä
        Undo.RegisterCreatedObjectUndo(go, "Aloituskohta");
        EditorSceneManager.MarkSceneDirty(go.scene);
        Info("Peli alkaa nyt Uccopulcon kadun alusta.\nTakaisin kadun alkuun: valikko 37.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/37. Aloita peli taas kadun alusta")]
    static void StartAtStreet()
    {
        var old = GameObject.Find("Aloituskohta");
        if (old != null) { Undo.DestroyObjectImmediate(old); EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); }
        Info("Peli alkaa taas kadun alusta.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/88. Aloita peli laivan salista (tanssisalista)")]
    static void StartAtShipSalon()
    {
        var sal = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Laivan sali");
        if (sal == null) { Info("Tee ensin laivan sisätilat (kohta 60)."); return; }
        var old = GameObject.Find("Aloituskohta");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var go = new GameObject("Aloituskohta");
        var gs = go.AddComponent<GameStart>();
        gs.area = sal;
        // sama kohta kuin käytävän ovesta saliin tultaessa
        var door = GameObject.Find("Käytävästä saliin");
        var d = door != null ? door.GetComponent<Door>() : null;
        gs.position = d != null ? d.spawnPoint
            : new Vector2((sal.camMinX + sal.camMaxX) * 0.5f, Mathf.Lerp(sal.maxDepthY, sal.minDepthY, 0.4f));
        Undo.RegisterCreatedObjectUndo(go, "Aloituskohta");
        EditorSceneManager.MarkSceneDirty(go.scene);
        Info("Peli alkaa nyt laivan salista (käytävän oven kohdalta).\nTakaisin kadun alkuun: valikko 37.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/90. Aloita peli laivan katolta")]
    static void StartAtShipRoof()
    {
        if (PlayModeBlocked()) return;
        var roof = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == RoofAreaName);
        if (roof == null) { Info("Tee ensin laivan katto (kohta 87)."); return; }
        var old = GameObject.Find("Aloituskohta");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var go = new GameObject("Aloituskohta");
        var gs = go.AddComponent<GameStart>();
        gs.area = roof;
        // Sama kohta kuin komentosillan tikkaista katolle tultaessa.
        var door = GameObject.Find("Ovi katolle")?.GetComponent<Door>();
        gs.position = door != null && door.target == roof ? door.spawnPoint
            : new Vector2(roof.walkMinX + 3f, Mathf.Lerp(roof.maxDepthY, roof.minDepthY, 0.45f));
        Undo.RegisterCreatedObjectUndo(go, "Aloituskohta");
        EditorSceneManager.MarkSceneDirty(go.scene);
        Info("Peli alkaa nyt laivan katolta.\nTakaisin kadun alkuun: valikko 37.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/92. Aloita peli Aavikolta")]
    static void StartAtDesert()
    {
        if (PlayModeBlocked()) return;
        var desert = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Aavikko");
        if (desert == null) { Info("Tee ensin Aavikko (90)."); return; }
        var old = GameObject.Find("Aloituskohta");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var go = new GameObject("Aloituskohta");
        var start = go.AddComponent<GameStart>();
        start.area = desert;
        var door = GameObject.Find("Kopterista Aavikkoon")?.GetComponent<Door>();
        start.position = door != null && door.target == desert ? door.spawnPoint
            : new Vector2(desert.walkMinX + 12f, Mathf.Lerp(desert.maxDepthY, desert.minDepthY, 0.45f));
        Undo.RegisterCreatedObjectUndo(go, "Aloituskohta");
        EditorSceneManager.MarkSceneDirty(go.scene);
        Info("Peli alkaa nyt Aavikolta Chinookin rampin edestä. Takaisin kadun alkuun: valikko 37. Tallenna scene (Ctrl+S).");
    }

    static void ImportBreakfastSprites()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Aamiainen" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100; ti.alphaIsTransparency = true; ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            var st = new TextureImporterSettings(); ti.ReadTextureSettings(st);
            st.spriteAlignment = (int)(Path.GetFileNameWithoutExtension(path) == "annos" ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
            ti.SetTextureSettings(st);
            ti.SaveAndReimport();
        }
    }

    /// Yksi dinerin pöytäryhmä: pöytä, olutpullot, tuolit, aamiaisannos ja kermapiirakka.
    static Crate BuildDinerTableSet(Transform parent, Vector3 pos, int seed, int pies = 1)
    {
        AddBottleTables(parent, new[] { pos }, seed, true);
        var table = parent.GetComponentsInChildren<Crate>(true).LastOrDefault(c => c.gameObject.name == "Pöytä");
        if (table == null) return null;
        var plateSnd = LoadClips("Assets/Audio/sfx", "posliini");
        float top = 1.28f * TableScale - 0.04f;
        if (pies == 0)
        {
            // tavallinen ruokapöytä: kaksi aamiaisannosta ja oluet, ei piirakoita
            foreach (float x in new[] { -0.62f, 0.62f })
            {
                var dGo = new GameObject("Aamiaisannos");
                dGo.transform.SetParent(parent, false);
                dGo.transform.position = table.transform.position;
                var df = dGo.AddComponent<FishPlate>();
                df.spriteSet = "Aamiainen"; df.countMul = 0.35f;
                df.table = table; df.tableX = x * TableScale; df.tableTop = top + 0.04f;
                df.breakSounds = plateSnd;
            }
            return table;
        }
        if (pies > 1)
        {
            // piirakkapöytä: vain piirakoita (3 kpl vierekkäin)
            float[] px = pies == 2 ? new[] { -0.8f, 0.8f } : new[] { -1.25f, 0f, 1.25f };
            foreach (float x in px) MakePie(parent, table, x * TableScale / 1.3f, top);
            return table;
        }
        MakePie(parent, table, 0.62f * TableScale, top);
        var pGo = new GameObject("Aamiaisannos");
        pGo.transform.SetParent(parent, false);
        pGo.transform.position = table.transform.position;
        var fp = pGo.AddComponent<FishPlate>();
        fp.spriteSet = "Aamiainen"; fp.countMul = 0.35f;
        fp.table = table; fp.tableX = -0.62f * TableScale; fp.tableTop = top + 0.04f;
        fp.breakSounds = plateSnd;
        return table;
    }

    [MenuItem("Beat em up/109. Diner: pöydät uusiutuvat (annoksineen ja piirakkoineen)")]
    static void AddDinerTableRespawn()
    {
        var diner = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Diner");
        if (diner == null) { Info("Tee ensin Diner (103) ja kalusteet (104)."); return; }
        var old = GameObject.Find("Pöytien uusiutuminen");
        if (old != null) Undo.DestroyObjectImmediate(old);
        ImportBreakfastSprites();
        const float left = 90000f; float ppu = DinerPpu;
        var holder = new GameObject("Pöytien uusiutuminen");
        Undo.RegisterCreatedObjectUndo(holder, "Pöytien uusiutuminen");
        var group = new GameObject("Pöytäryhmän malli");
        group.transform.SetParent(holder.transform, false);
        Vector3 origin = new Vector3(0f, -3f, 0f);
        var table = BuildDinerTableSet(group.transform, origin, 77);
        if (table == null) { Info("Pöydän tekeminen epäonnistui: tarvitaan poyta.png."); return; }
        group.SetActive(false);
        var rs = holder.AddComponent<DinerTableRespawn>();
        rs.area = diner; rs.template = group; rs.templateOrigin = origin;
        // lisäpöytä snookerpöydän taakse (oikea pääty): vain piirakoita
        Vector3 snookerTable = DinerClamp(diner, new Vector3(left + 3500f / ppu, -0.7f, 0f), 1.6f);   // kauas snookerin taakse, lähelle takaseinää
        var snookerSet = new GameObject("Snookerin piirakkapöytä");
        snookerSet.transform.SetParent(holder.transform, false);
        BuildDinerTableSet(snookerSet.transform, snookerTable, 88, 3);
        // lisäpöydät vasempaan reunaan: annoksia ja oluita, ei piirakoita
        Vector3 left1 = DinerClamp(diner, new Vector3(left + 170f / ppu, -2.5f, 0f), 1.6f), left2 = DinerClamp(diner, new Vector3(left + 830f / ppu, -3.8f, 0f), 1.6f);
        var leftSet = new GameObject("Vasemman reunan pöydät");
        leftSet.transform.SetParent(holder.transform, false);
        BuildDinerTableSet(leftSet.transform, left1, 91, 0);
        BuildDinerTableSet(leftSet.transform, left2, 92, 0);
        // oma malli ilman piirakkaa (uusiutuvat pöydät)
        var plainGroup = new GameObject("Pöytäryhmän malli (annokset)");
        plainGroup.transform.SetParent(holder.transform, false);
        BuildDinerTableSet(plainGroup.transform, origin, 78, 0);
        plainGroup.SetActive(false);
        var spots7 = DinerTableSpots(diner, left, ppu);   // 4 ylärivin + 5 alalattian pöytää (sama lista kuin valikossa 104)
        rs.spots = spots7.Concat(new[] { snookerTable, left1, left2 }).ToArray();
        rs.spotTemplates = new GameObject[] { null, null, null, null, plainGroup, plainGroup, plainGroup, plainGroup, plainGroup, null, plainGroup, plainGroup };
        rs.maxTables = 16;
        rs.delay = 1000000f;   // ei automaattista uusiutumista tavallisessa tappelussa: pöydät palautuvat vain piirakkasodan alkaessa (RespawnMissingNow)
        EditorSceneManager.MarkSceneDirty(holder.scene);
        Info("Dinerin pöydät uusiutuvat: kun pöytä on rikki tai heitetty kauas, samaan paikkaan tulee 6 sekunnin päästä uusi pöytä annoksineen, piirakkoineen, oluineen ja tuoleineen (enintään 10 pöytää kerrallaan).\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/108. Diner: vihut (Rusty, rokkimimmi, motoristi, puliukko) ja piirakkasota")]
    static void AddDinerFoes()
    {
        var diner = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Diner");
        if (diner == null) { Info("Tee ensin Diner (103) ja kalusteet (104)."); return; }
        foreach (var name in new[] { "Dinerin vihut", "Piirakkasota" })
        { var o = GameObject.Find(name); if (o != null) Undo.DestroyObjectImmediate(o); }
        var puliTemplate = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(en => en != null && en.gameObject.name == "Puliukko");
        var root = new GameObject("Dinerin vihut");
        Undo.RegisterCreatedObjectUndo(root, "Dinerin vihut");
        const float left = 90000f; float ppu = DinerPpu;
        // paikat kuvapikseleinä (x) ja syvyys (y): pöytien väleissä lattialla
        var spots = new[] { new Vector2(850f, -2.6f), new Vector2(1550f, -4.9f), new Vector2(2350f, -3.0f), new Vector2(3350f, -3.4f), new Vector2(3000f, -5.4f), new Vector2(420f, -5.0f), new Vector2(1250f, -2.4f),
                            new Vector2(900f, -6.3f), new Vector2(2150f, -6.6f), new Vector2(3050f, -6.2f) };
        var foes = new List<Enemy> { MakeRusty(root.transform), MakeRocker(root.transform), MakeBiker(root.transform), null, MakeRocker(root.transform), MakeBiker(root.transform), null,
                                     MakeRusty(root.transform), MakeRusty(root.transform), null };
        var truckerTemplate = GameObject.Find("Rekkamies");
        if (truckerTemplate != null && truckerTemplate.GetComponent<Enemy>() != null)
        {
            var truck = Object.Instantiate(truckerTemplate, root.transform);
            truck.name = "Dinerin rekkamies";
            foes[9] = truck.GetComponent<Enemy>();
            EnsureTruckerVoice(foes[9]);
            foes[9].fightsEveryone = false; foes[9].huntsBrawlers = false;   // tähän piirakkasotaan hän osallistuu kuvien valmistuttua (otto, heitto, osuma)
        }
        if (puliTemplate != null)
        {
            var clone = Object.Instantiate(puliTemplate.gameObject, root.transform);
            clone.name = "Dinerin puliukko";
            foes[3] = clone.GetComponent<Enemy>();
            var clone2 = Object.Instantiate(puliTemplate.gameObject, root.transform);
            clone2.name = "Dinerin puliukko 2";
            foes[6] = clone2.GetComponent<Enemy>();
        }
        int n = 0;
        for (int i = 0; i < foes.Count; i++)
        {
            var e = foes[i];
            if (e == null) continue;
            e.gameObject.name = "Dinerin " + e.gameObject.name.Replace("Dinerin ", "");
            e.transform.position = DinerClamp(diner, new Vector3(left + spots[i].x / ppu, spots[i].y, 0f), 0.8f);
            e.wakeDistance = 12f;
            e.joinsFightWhenSquadComes = false;
            // rokkimimmi tappelee tavallisesti; piirakoita heitetään sodassa ja satunnaisesti tappelussa (pieFightRate)
            // motoristit ja puliukot tappelevat välillä keskenään (kaksi motoristia eri ryhmissä: he lyövät toisiaankin; puliukot mätkivät kaikkia)
            if (i == 2 || i == 5) { e.brawlsWithOthers = true; e.brawlGroup = i == 2 ? "motoristi" : "motoristi2"; }
            if (i == 3 || i == 6) { e.brawlsWithOthers = true; e.brawlGroup = ""; }
            e.gameObject.SetActive(true);
            n++;
        }
        var war = new GameObject("Piirakkasota").AddComponent<PieWar>();
        Undo.RegisterCreatedObjectUndo(war.gameObject, "Piirakkasota");
        war.area = diner; war.maxHits = 10;
        war.linsey = Object.FindFirstObjectByType<DinerLinsey>();
        war.counter = Object.FindFirstObjectByType<PieCounter>();
        if (war.counter != null)
        {
            // tiskille enemmän ja tiheämmin piirakoita (12 paikkaa, uusi 3 s välein)
            Undo.RecordObject(war.counter, "Piirakkatiski");
            war.counter.xs = new[] { 1480f, 1640f, 1800f, 1960f, 2120f, 2280f, 2440f, 2600f, 2760f, 3000f, 3140f, 3280f }.Select(px => left + px / ppu).ToArray();
            war.counter.interval = 3f; war.counter.initial = 6;
            EditorUtility.SetDirty(war.counter);
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Info("Dineriin " + n + " vihua (3 Rustya, 2 rokkimimmiä, 2 motoristia, 2 puliukkoa, rekkamies) ja piirakkasota.\n" + (war.linsey == null ? "Linsey puuttuu: aja 106. " : "") + (war.counter == null ? "Tiskin piirakat puuttuvat: aja 104. " : "")
             + "\nPiirakkasota: mene Linseyn luo ja paina E. Aja sen jälkeen 105, niin vihut saavat piirakkakuvat.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/107. Aloita peli Aavikkokadulta dinerin läheltä")]
    static void StartAtDiner()
    {
        if (PlayModeBlocked()) return;
        var street = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Aavikkokatu");
        if (street == null) { Info("Tee ensin Aavikkokatu (95)."); return; }
        var old = GameObject.Find("Aloituskohta");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var go = new GameObject("Aloituskohta");
        var start = go.AddComponent<GameStart>();
        start.area = street;
        // dinerin ovi (103) on aavikkokadun kuvasarjassa; muuten lasketaan sen paikka kuvasta
        var door = GameObject.Find("Aavikkokadulta dineriin");
        float doorX = door != null ? door.transform.position.x : 86000f + 5896.992f / (887f / 12f) + 897f * 1.157184f / (887f / 12f);
        start.position = new Vector2(doorX - 5f, Mathf.Lerp(street.minDepthY, street.maxDepthY, 0.45f));
        Undo.RegisterCreatedObjectUndo(go, "Aloituskohta");
        EditorSceneManager.MarkSceneDirty(go.scene);
        Info("Peli alkaa nyt Aavikkokadulta dinerin oven vierestä (5 yksikköä vasemmalla). Takaisin kadun alkuun: valikko 37. Tallenna scene (Ctrl+S).");
    }

    static AudioClip[] LoadClips(string folder, string filter)
    {
        if (!AssetDatabase.IsValidFolder(folder)) return new AudioClip[0];
        return AssetDatabase.FindAssets("t:AudioClip " + filter, new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(p => p)
            .Select(AssetDatabase.LoadAssetAtPath<AudioClip>)
            .Where(c => c != null)
            .ToArray();
    }

    static GameObject MakeBlock(string name, Sprite sprite, Vector3 pos, Vector3 scale, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        return go;
    }

    static string FindTexture(string fileName)
    {
        return AssetDatabase.FindAssets("t:Texture2D " + fileName)
            .Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == fileName);
    }

    static Sprite[] LoadSprites(string fileName)
    {
        string path = FindTexture(fileName);
        if (path == null) return new Sprite[0];
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
    }

    /// Luo pienen valkoisen kuvan Assets/Sprites/valkoinen.png (1 × 1 yksikkö), jota käytetään kadun paloina.
    static Sprite GetWhiteBlockSprite()
    {
        const string path = "Assets/Sprites/valkoinen.png";
        if (!File.Exists(path))
        {
            Directory.CreateDirectory("Assets/Sprites");
            var tex = new Texture2D(16, 16);
            tex.SetPixels(Enumerable.Repeat(Color.white, 256).ToArray());
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 16;
            ti.filterMode = FilterMode.Point;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
