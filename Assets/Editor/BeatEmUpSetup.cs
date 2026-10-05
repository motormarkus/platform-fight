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
            ApplyWoodBreakSounds(); // puu1/puu2 kaikille hajoaville pöydille ja laatikoille
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
    static int SetupAndSlice(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2);
        tex.LoadImage(bytes);
        int w = tex.width, h = tex.height;
        string baseName0 = Path.GetFileNameWithoutExtension(path);
        // tanssijan kuvat ovat kapeampia (256 × 384), muut 512 × 384
        int CellW = baseName0.StartsWith("turisti_tanssi") ? 384 : baseName0.StartsWith("tuoli_") ? 768 : baseName0.StartsWith("tanssija") ? 256 : baseName0.StartsWith("pratka") || baseName0.StartsWith("vihu_pratka") || baseName0.StartsWith("vihu_pyora") || baseName0.StartsWith("bandi") ? 768
                  : baseName0.StartsWith("poyta") ? 448 : baseName0.StartsWith("pullo_") ? 128 : baseName0.StartsWith("telkkari") ? 256 : BeatEmUpSetup.CellW;
        // saksipotkun ilmakuvat ja pomon nyrkki pään yllä tarvitsevat enemmän korkeutta (512 × 512)
        int CellH = baseName0.StartsWith("turisti_aurora") ? 768 : baseName0.StartsWith("tuoli_") || baseName0.StartsWith("turisti_") ? 512 : baseName0.StartsWith("saksipotku") || baseName0.StartsWith("pomo_lyonti") || baseName0.StartsWith("vihu_lento") ? 512
                  : baseName0.StartsWith("vihu_pyora_kaatuu") ? 640 : baseName0.StartsWith("pratka") || baseName0.StartsWith("vihu_pratka") || baseName0.StartsWith("vihu_pyora") || baseName0.StartsWith("bandi") ? 448
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
            // alaraja: jalat ja varjo pysyvät kuvassa (1,2 yksikköä kameran alareunan yläpuolella)
            var mainCam = Camera.main;
            if (mainCam != null)
                pc.minDepthY = mainCam.transform.position.y - mainCam.orthographicSize + 1.2f;
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
        e.punchSprites = Sheet("vihu_lyonti");
        e.hurtSprites = Sheet("vihu_osuma");
        e.knockdownSprites = Sheet("vihu_kaatuminen");
        e.getUpSprites = Sheet("vihu_ylosnousu");
        e.grabSprites = Sheet("vihu_heitto");      // tarttuu ja heittää välillä, kun pääsee viereen
        e.hurtSounds = LoadClips("Assets/Audio/big thug", "gasp");
        e.hurtVolume = 0.99f;
        e.bigBody = true;                          // pelaaja heittää kuperkeikalla
        e.flipThrownSprites = Sheet("vihu_kuperkeikka");
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
        foreach (var n in new[] { "S-Club sisä", "Alue: Katu", "Alue: S-Club", "Ovet" })
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
        var streetArea = new GameObject("Alue: Katu").AddComponent<Area>();
        streetArea.areaName = "Katu";
        streetArea.minDepthY = pc.minDepthY;
        streetArea.maxDepthY = pc.maxDepthY;
        streetArea.useSidewalk = pc.useSidewalk;
        streetArea.sidewalkHeight = pc.sidewalkHeight;
        streetArea.curbDepthY = pc.curbDepthY;
        streetArea.camMinX = follow.minX;
        streetArea.camMaxX = follow.maxX;
        Undo.RegisterCreatedObjectUndo(streetArea.gameObject, "Alue");

        var clubArea = new GameObject("Alue: S-Club").AddComponent<Area>();
        clubArea.areaName = "S-Club";
        clubArea.music = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Musiikki/S-Club.mp3");   // klubin oma musiikki
        float top = camY + hU * 0.5f;
        clubArea.maxDepthY = top - ClubFloorRow / ClubPPU;
        clubArea.minDepthY = pc.minDepthY;
        clubArea.useSidewalk = false;
        clubArea.camMinX = ClubX0 + halfW;
        clubArea.camMaxX = ClubX0 + wU - halfW;
        Undo.RegisterCreatedObjectUndo(clubArea.gameObject, "Alue");

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
    static readonly float[] DancerPx = { 655f, 890f };
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

    [MenuItem("Beat em up/11. Lisää tanssijat lavalle")]
    static void AddDancers()
    {
        var club = GameObject.Find("S-Club sisä");
        var sprites = LoadSprites("tanssija")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        if (club == null || sprites.Length == 0)
        {
            Info( "Tarvitaan S-Clubin sisätila (kohta 10) ja leikattu tanssija.png (kohta 1).");
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
        for (int i = 0; i < DancerPx.Length; i++)
        {
            var go = new GameObject("Tanssija " + (i + 1));
            go.transform.SetParent(root.transform, false);
            go.transform.position = new Vector3(left + DancerPx[i] / ppu, top - StageFeetRow / ppu, 0f);
            go.transform.localScale = new Vector3(DancerScale, DancerScale, 1f);   // jalat pysyvät paikallaan
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprites[0];
            sr.sortingOrder = -9000;                       // taustan edessä, pelaajien takana
            sr.color = new Color(1f, 0.86f, 0.95f);        // lavan violetti valo
            var d = go.AddComponent<Dancer>();
            d.sprites = sprites;
            d.frameTime = 0.124f;
            d.startFrame = i * sprites.Length / 2;         // eri tahdissa
            d.flipX = i == 1;                              // peilikuva toisella puolella tankoa
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Info( $"Kaksi tanssijaa lavalla ({sprites.Length} kuvaa, silmukka).\n\nTallenna scene (Ctrl+S).");
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

    [MenuItem("Beat em up/26. Päivitä saksipotku (ylös, alas, K, K)")]
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
            "Saksipotku: ylös, alas, K, K (maasta) tai hyppy + K (ilmassa).\nKaksi potkua vuorojaloin, toinen kaataa. Hyppy + J = vanha hyppypotku.\n\nTallenna scene (Ctrl+S).");
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
        new Vector2(7f, 0.88f), new Vector2(11f, 0.88f), new Vector2(15f, 0.88f), new Vector2(19f, 0.88f), new Vector2(23f, 0.88f),
        new Vector2(27f, 0.88f), new Vector2(31f, 0.88f), new Vector2(35f, 0.88f) };
    // telkkarit pyöreillä pöydillä seinän vieressä, takarivin pöytien välissä
    static readonly float[] TvTablesX = { 7f, 11f, 15f, 19f, 23f, 27f, 31f, 35f };

    const float TableScale = 1.3f;   // suorakaidepöytä 30 % isompi, pullot 5 % (Bottle.scale)
    const float TvTableScale = 1.3f; // pyöreä telkkaripöytä 30 % isompi, telkkari myös (TvSet.scale)

    [MenuItem("Beat em up/41. S-Clubin baaripöydät ja pullot")]
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
        Info($"S-Clubiin {BarTables.Length} pöytää, {tvs} telkkaria ja {bottles} pulloa ({kinds.Count} pullomerkkiä), lasiääniä {glass.Length}.\n" +
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

        foreach (var n in new[] { "Katto", "Alue: Katto", "Palotikkaat", "Katon viholliset" })
        {
            var o = GameObject.Find(n);
            if (o != null) Undo.DestroyObjectImmediate(o);
        }

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

        // viholliset: kopiot kadun malleista
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

        EditorSceneManager.MarkSceneDirty(bg.scene);
        Info(
            $"Palotikkaat kadun lopussa (x = {ladderX:0.0}): mene tikkaiden eteen jalkakäytävälle ja paina E (ohjaimessa ympyrä).\n" +
            $"Katolla {RoofEnemies.Length} vihollista ja pomo. Takaisin alas vasemman reunan tikkailta.\n" +
            (report.Count > 0 ? string.Join("\n", report) + "\n" : "") +
            "\nKaton oikea puoli on kattokuvan 1 peilikuva, kunnes kuvat 2 ja 3 valmistuvat.\n\nTallenna scene (Ctrl+S).");
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
        }

        EditorSceneManager.MarkSceneDirty(root.scene);
        Info($"Valtatie luotu ({HighwayLength:0} yksikköä). Aja prätkällä kujan oikeaan reunaan: pimennys, otsikko ja valtatie.\n" +
             $"Maisema liikkuu {px.factor * 100:0} % tien vauhdista.\n\nTallenna scene (Ctrl+S).");
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
    // ---------------- Laivan tappelu: seilorit ja rosvot ----------------
    const int SailorCount = 4;
    static readonly Vector2[] ShipPirateSpots = { new Vector2(3150f, 0.3f), new Vector2(3350f, 0.7f), new Vector2(3550f, 0.4f), new Vector2(3700f, 0.8f), new Vector2(3800f, 0.2f) };   // kannen kuvan x, syvyys 0 = kaide … 1 = edessä

    [MenuItem("Beat em up/56. Laivan tappelu: seilorit (liittolaiset) ja rosvot")]
    static void AddShipFight()
    {
        var deck = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Laivan kansi");
        var deckBg = GameObject.Find("Laivan kansi");
        if (deck == null || deckBg == null) { Info("Tee ensin kohta 55 (laivan kansi)."); return; }
        foreach (var n in new[] { "seilori_idle", "seilori_kavely", "seilori_lyonti", "seilori_potku", "seilori_osuma", "seilori_kaatuminen", "seilori_juoksu", "seilori_koukku", "seilori_heitto" })
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
        squad.bouncers = new Enemy[SailorCount];
        go.transform.SetParent(root.transform, false);
        for (int i = 0; i < SailorCount; i++)
        {
            var c = i == 0 ? go : Object.Instantiate(go, root.transform);
            if (i > 0) c.name = "Seilori_" + (i + 1);
            float y = Mathf.Lerp(deck.maxDepthY, deck.minDepthY, 0.15f + 0.2f * i);
            c.transform.position = new Vector3(hyttiovi.x - 0.3f * i, y, 0f);
            squad.bouncers[i] = c.GetComponent<Enemy>();
            c.SetActive(false);            // tulevat hyttiovelta juosten, kun tappelu alkaa
        }
        report.Add($"Seilorit: {SailorCount} (tulevat hyttiovelta, kun tappelu alkaa)");

        // rosvot: toistaiseksi Kovikset (osaavat heittää), vaihdetaan rosvon kuviin, kun ne valmistuvat
        var lippisT = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(e => e.gameObject.name == "Kovis");
        if (lippisT != null)
        {
            var pr = new GameObject("Laivan rosvot");
            Undo.RegisterCreatedObjectUndo(pr, "Laivan rosvot");
            int n2 = 0;
            foreach (var v in ShipPirateSpots)
            {
                var r = Object.Instantiate(lippisT.gameObject, pr.transform);
                r.name = "Rosvo " + (++n2);
                r.SetActive(true);
                r.transform.position = new Vector3(X(v.x), Mathf.Lerp(deck.maxDepthY, deck.minDepthY, v.y), 0f);
                var re = r.GetComponent<Enemy>(); re.displayName = "Rosvo"; re.wakeDistance = 7f;
            }
            report.Add($"Rosvot: {n2} (väliaikaisesti Kovis-hahmoina)");
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
        foreach (var n in new[] { "Laivan kansi", "Alue: Laivan kansi", "Laivan ovet", "Laivan meri kaukana", "Laivan meri lähellä", "Laivan turistit" })
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
        var area = new GameObject("Alue: Laivan kansi").AddComponent<Area>();
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
        Undo.RegisterCreatedObjectUndo(area.gameObject, "Alue");

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
        var dancers = GameObject.Find("Tanssijat");
        if (loipBg == null || band == null || dancers == null) { Info("Tarvitaan El Loippari (45), bändi (40) ja tanssijat (11)."); return; }
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
        // tanssijat lavan molemmin puolin bändiä
        int i = 0;
        foreach (Transform d in dancers.transform)
        {
            if (i >= 2) break;
            var dGo = Object.Instantiate(d.gameObject, root.transform);
            dGo.name = "Loipparin tanssija " + (i + 1);
            float side = i == 0 ? -1f : 1f;
            dGo.transform.position = new Vector3(left + (LoipStagePx + side * (LoipStageHalfPx - 110f)) / ppu, feetY, 0f);
            dGo.transform.localScale = new Vector3(DancerScale, DancerScale, 1f);
            var dsr = dGo.GetComponent<SpriteRenderer>(); if (dsr != null) dsr.color = new Color(1f, 0.95f, 0.9f);
            var dn = dGo.GetComponent<Dancer>(); if (dn != null) dn.flipX = side > 0f;   // katsovat bändiä kohti
            i++;
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
