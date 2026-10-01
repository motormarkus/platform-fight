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
    const string JabName = "jab", CrossName = "takasuora", RoundhouseName = "kiertopotku";

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
        EditorUtility.DisplayDialog("Beat em up",
            report.Count == 0 ? "Sprites-kansiosta ei löytynyt sprite sheetejä." :
            "Leikattu (512 × 384, jalat alareunassa):\n\n" + string.Join("\n", report), "OK");
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
        int CellW = baseName0.StartsWith("tanssija") ? 256 : BeatEmUpSetup.CellW;
        // myyjä on piirretty tarkemmin (kaksinkertainen resoluutio)
        int ppu = baseName0.StartsWith("myyja") || baseName0.StartsWith("laatikko") ? 200 : 100;
        if (w % CellW != 0 || h % CellH != 0) { Object.DestroyImmediate(tex); return -1; }

        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Multiple;
        ti.spritePixelsPerUnit = ppu;
        ti.filterMode = FilterMode.Bilinear;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.maxTextureSize = 2048;
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
                if (IsEmpty(tex, x, y, CellW)) continue;
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

    static bool IsEmpty(Texture2D tex, int x, int y, int cellW)
    {
        Color[] px = tex.GetPixels(x, y, cellW, CellH);
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
    const int BackgroundRepeats = 2;   // kuinka monta kertaa kolmen kuvan sarja toistetaan (kadun pituus)
    // Mitattu kuvasta: jalkakäytävän pinta riveillä 592–615, kynnyksen etureuna 615–637.
    const float CurbTopRow = 615f, CurbBottomRow = 637f, WallBaseRow = 590f;

    [MenuItem("Beat em up/4. Aseta katutausta")]
    static void SetupBackground()
    {
        var ti = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
        if (ti == null)
        {
            EditorUtility.DisplayDialog("Beat em up", $"Taustakuvaa ei löytynyt:\n{BackgroundPath}", "OK");
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
        EditorUtility.DisplayDialog("Beat em up",
            $"Tausta lisätty ({BackgroundRepeats} × {wU:0.0} yksikköä). Testikatu piilotettu.\nTallenna scene (Ctrl+S).", "OK");
    }

    [MenuItem("Beat em up/5. Päivitä kävely ja juoksu")]
    static void UpdateWalk()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { EditorUtility.DisplayDialog("Beat em up", "Scenessä ei ole pelaajaa. Tee ensin kohta 2.", "OK"); return; }
        Sprite[] walk = LoadSprites("kavely")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0)
            .ToArray();
        if (walk.Length == 0) { EditorUtility.DisplayDialog("Beat em up", "kavely.png ei löytynyt tai sitä ei ole leikattu. Tee ensin kohta 1.", "OK"); return; }
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
        EditorUtility.DisplayDialog("Beat em up", $"Kävely: {walk.Length} kuvaa ({pc.walkFrameTime:0.000} s/kuva)\nJuoksu: {run.Length} kuvaa ({pc.runSpriteFrameTime:0.000} s/kuva) (Shift)\n\nTallenna scene (Ctrl+S).", "OK");
    }

    [MenuItem("Beat em up/6. Päivitä äänet")]
    static void UpdateSounds()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { EditorUtility.DisplayDialog("Beat em up", "Scenessä ei ole pelaajaa. Tee ensin kohta 2.", "OK"); return; }
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
        EditorUtility.DisplayDialog("Beat em up",
            $"Iskujen gruntit: {grunts.Length} kpl\nOsuman gaspit (gasphero): {pc.hurtSounds.Length} kpl\n(Assets/Audio/Pelaaja)\n\nTallenna scene (Ctrl+S).", "OK");
    }

    // Pelin taustamusiikki (tiedostonimi ilman päätettä kansiossa Assets/Audio/Musiikki)
    const string MusicName = "Blade Anthem";

    [MenuItem("Beat em up/7. Aseta taustamusiikki")]
    static void SetupMusic()
    {
        var paths = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/Musiikki" })
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToList();
        // valittu kappale, tai jos sitä ei löydy, ensimmäinen kansiosta
        string path = paths.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == MusicName) ?? paths.FirstOrDefault();
        if (path == null)
        {
            EditorUtility.DisplayDialog("Beat em up", "Kansiosta Assets/Audio/Musiikki ei löytynyt äänitiedostoa.", "OK");
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
        EditorUtility.SetDirty(mp);
        EditorSceneManager.MarkSceneDirty(mp.gameObject.scene);
        EditorUtility.DisplayDialog("Beat em up",
            $"Taustamusiikki: {Path.GetFileName(path)}\nSoitetaan silmukkana, voimakkuus 0.6 (säädä Musiikki-objektista).\n\nTallenna scene (Ctrl+S).", "OK");
    }

    [MenuItem("Beat em up/3. Päivitä lyöntikombo")]
    static void UpdateCombo()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { EditorUtility.DisplayDialog("Beat em up", "Scenessä ei ole pelaajaa. Tee ensin kohta 2.", "OK"); return; }

        var combo = new List<PlayerController.ComboHit>();
        var found = new List<string>();

        combo.Add(MakeHit("Jab", JabName, impactFrame: 2, frameTime: 0.045f, impactHold: 0.09f, lunge: 0.10f,
            fallbackFrame: 8, found, damage: 6, reach: 1.6f, knockdown: false));
        combo.Add(MakeHit("Takasuora", CrossName, impactFrame: 3, frameTime: 0.05f, impactHold: 0.11f, lunge: 0.18f,
            fallbackFrame: 8, found, damage: 8, reach: 1.7f, knockdown: false));
        combo.Add(MakeHit("Kiertopotku", RoundhouseName, impactFrame: 3, frameTime: 0.055f, impactHold: 0.14f, lunge: 0.30f,
            fallbackFrame: 11, found, damage: 14, reach: 1.9f, knockdown: true));

        Undo.RecordObject(pc, "Päivitä lyöntikombo");
        pc.punchCombo = combo.ToArray();
        EditorUtility.SetDirty(pc);
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);

        EditorUtility.DisplayDialog("Beat em up",
            "Kombo päivitetty: Jab → Takasuora → Kiertopotku.\n\n" + string.Join("\n", found) +
            "\n\nTallenna scene (Ctrl+S).", "OK");
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
            EditorUtility.DisplayDialog("Beat em up",
                $"Spritejä ei löytynyt (idle: {idle.Length}, hahmo_spritesheet: {action.Length}).\n\n" +
                "Tee ensin kohta 1 ja leikkaa kuvat Sprite Editorissa (512 × 384).", "OK");
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
        if (pc == null) { EditorUtility.DisplayDialog("Beat em up", "Scenessä ei ole pelaajaa. Tee ensin kohta 2.", "OK"); return; }

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
            EditorUtility.DisplayDialog("Beat em up", "vihu_idle.png ei löytynyt kansiosta " + EnemyFolder + " tai sitä ei ole leikattu. Tee ensin kohta 1.", "OK");
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
        EditorUtility.DisplayDialog("Beat em up", "Kovis valmis.\n\n" + string.Join("\n", report) + "\n\nTallenna scene (Ctrl+S).", "OK");
    }

    // Koviksien ryhmät kadun varrella: (x, syvyys y). Ruudun leveys on n. 29.5 yksikköä.
    static readonly Vector2[] KovisSpawns =
    {
        new Vector2(9f, -2.0f), new Vector2(12f, -3.3f),                               // 1. ruutu: kerrostalo
        new Vector2(27f, -1.6f), new Vector2(30f, -3.6f), new Vector2(33f, -2.5f),     // 2. ruutu: baari
        new Vector2(56f, -2.0f), new Vector2(59f, -3.4f), new Vector2(62f, -1.7f), new Vector2(65f, -2.9f), // 3. ruutu: S-Club
        new Vector2(84f, -2.2f), new Vector2(88f, -3.2f), new Vector2(92f, -1.8f),     // loppu
    };

    [MenuItem("Beat em up/9. Lisää Koviksia kadulle")]
    static void AddKovisGroups()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        var template = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).FirstOrDefault(x => x.gameObject.name == "Kovis");
        if (pc == null || template == null)
        {
            EditorUtility.DisplayDialog("Beat em up", "Tee ensin kohta 8 (Kovis), sitten tämä.", "OK");
            return;
        }
        // vanhat kopiot pois, alkuperäinen Kovis siirretään ensimmäiseen kohtaan
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            if (e.gameObject.name.StartsWith("Kovis_")) Undo.DestroyObjectImmediate(e.gameObject);

        float x0 = pc.transform.position.x;
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
        EditorUtility.DisplayDialog("Beat em up",
            $"Kadulla on nyt {KovisSpawns.Length} Kovista ryhmissä (2, 3, 4 ja 3).\n" +
            "Ne heräävät, kun tulet n. 9 yksikön päähän. Kaksi lyö kerrallaan, muut odottavat vuoroaan.\n\nTallenna scene (Ctrl+S).", "OK");
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
            EditorUtility.DisplayDialog("Beat em up", "Tarvitaan pelaaja, katutausta (kohta 4), kamera ja kuva " + ClubInteriorPath, "OK");
            return;
        }

        // kuvan tuonti
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = ClubPPU;
        ti.filterMode = FilterMode.Bilinear;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.maxTextureSize = 4096;
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
        EditorUtility.DisplayDialog("Beat em up",
            "S-Club sisätila luotu (stage vasemmalla, baaritiski oikealla).\n\n" + string.Join("\n", report) +
            $"\nSisällä syvyys {clubArea.minDepthY:0.00} … {clubArea.maxDepthY:0.00}\n\n" +
            "Mene oven eteen jalkakäytävälle ja paina E (ohjaimessa B).\nSisältä pääsee ulos vasemmasta reunasta samalla napilla.\n\nTallenna scene (Ctrl+S).", "OK");
    }

    // ---------------- Tanssijat ----------------
    // Lavan kohta S-Clubin sisäkuvassa (pikseleinä): tanko x = 770, jalat lavan pinnalla rivillä ~ 525
    const float StageFeetRow = 527f;
    static readonly float[] DancerPx = { 655f, 890f };
    const float DancerScale = 1.2f;   // tanssijat 20 % isompina

    [MenuItem("Beat em up/11. Lisää tanssijat lavalle")]
    static void AddDancers()
    {
        var club = GameObject.Find("S-Club sisä");
        var sprites = LoadSprites("tanssija")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        if (club == null || sprites.Length == 0)
        {
            EditorUtility.DisplayDialog("Beat em up", "Tarvitaan S-Clubin sisätila (kohta 10) ja leikattu tanssija.png (kohta 1).", "OK");
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
        EditorUtility.DisplayDialog("Beat em up", $"Kaksi tanssijaa lavalla ({sprites.Length} kuvaa, silmukka).\n\nTallenna scene (Ctrl+S).", "OK");
    }

    // ---------------- Punkkari ----------------
    // Kadulla (x pelaajan aloituskohdasta, syvyys y) ja S-Clubin sisällä (x sisätilan vasemmasta reunasta, syvyys 0 = keskellä)
    static readonly Vector2[] PunkStreet = { new Vector2(18f, -2.8f), new Vector2(40f, -2.2f), new Vector2(70f, -3.0f), new Vector2(100f, -2.4f) };
    static readonly Vector2[] PunkClub = { new Vector2(13f, 0.3f), new Vector2(19f, -0.6f), new Vector2(25f, 0.5f) };

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
        if (pc == null) { EditorUtility.DisplayDialog("Beat em up", "Scenessä ei ole pelaajaa.", "OK"); return; }
        var report = new List<string>();
        Sprite[] idle = EnemySheet("punk_idle", report);
        if (idle.Length == 0)
        {
            EditorUtility.DisplayDialog("Beat em up", "punk_idle.png puuttuu tai sitä ei ole leikattu (kohta 1).", "OK");
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
        t.idleFrameTime = 0.2f;
        t.walkFrameTime = 0.115f;          // videon oma tahti (12 kuvaa, 1.4 s sykli)
        t.moveSpeedX = 2.6f;               // Kovista nopeampi
        t.moveSpeedY = 1.6f;
        t.maxHealth = 45;                  // mutta kestää vähemmän
        t.punchDamage = 7;
        t.punchImpactFrame = 3;
        t.altImpactFrame = 4;              // pusku osuu kuvassa 5 (tähti)
        t.altDamage = 12;
        t.altChance = 0.35f;
        t.attackCooldown = 1.3f;
        t.hurtSounds = LoadClips("Assets/Audio/big thug", "gasp");
        t.hurtVolume = 0.99f;
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
        EditorUtility.DisplayDialog("Beat em up",
            $"Punkkareita: {PunkStreet.Length} kadulla" + (club != null ? $", {PunkClub.Length} S-Clubissa" : " (S-Club puuttuu, tee kohta 10)") +
            "\n\n" + string.Join("\n", report) + "\n\nTallenna scene (Ctrl+S).", "OK");
    }

    [MenuItem("Beat em up/13. Päivitä erikoisliike (pyörähdyspotku)")]
    static void UpdateSpecial()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { EditorUtility.DisplayDialog("Beat em up", "Scenessä ei ole pelaajaa.", "OK"); return; }
        Sprite[] sp = LoadSprites("erikoispotku")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        if (sp.Length == 0) { EditorUtility.DisplayDialog("Beat em up", "erikoispotku.png puuttuu tai sitä ei ole leikattu (kohta 1).", "OK"); return; }
        Undo.RecordObject(pc, "Erikoisliike");
        pc.specialSprites = sp;
        pc.specialFrameTime = 0.045f;
        // nopea 360: potku eteen kuvissa 7–9, heti perään taakse kuvissa 10–13 (18 kuvaa × 0.045 s)
        pc.specialHitFrom = 0.25f;
        pc.specialHitTo = 0.60f;
        EditorUtility.SetDirty(pc);
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        EditorUtility.DisplayDialog("Beat em up",
            $"Pyörähdyspotku: {sp.Length} kuvaa ({sp.Length * 0.045f:0.00} s).\nNäppäin L, ohjaimessa LB. Osuu molempiin suuntiin ja kaataa.\n\nTallenna scene (Ctrl+S).", "OK");
    }

    // Baaritiski S-Clubin sisäkuvassa (pikseleinä): n. 1710–2600, keskikohta 2155
    const float BarCounterPx = 2155f, BarCounterHalfPx = 410f;
    const float CounterTopRow = 402f;   // tiskin yläreuna sisäkuvassa

    [MenuItem("Beat em up/14. Luo baaritiski (kauppa) S-Clubiin")]
    static void CreateShop()
    {
        var club = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "S-Club");
        if (club == null) { EditorUtility.DisplayDialog("Beat em up", "Tee ensin kohta 10 (S-Clubin sisätila).", "OK"); return; }
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
        EditorUtility.DisplayDialog("Beat em up",
            sohviInfo + "\n" + $"Baaritiski luotu S-Clubin oikealle puolelle ({soundCount}/{shop.items.Length} tuotteella ääni).\nMene tiskin eteen ja paina E: juomat ja snackit maksavat markkoja,\njoita putoaa kaatuneista vihollisista.\n\nValikoimaa ja hintoja voi muuttaa Baaritiski-objektista.\n\nTallenna scene (Ctrl+S).", "OK");
    }

    [MenuItem("Beat em up/15. Aseta rahapudotusten kuvat")]
    static void SetupPickups()
    {
        var report = new List<string>();
        foreach (var n in new[] { "seteli", "setelitukku" })
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
        EditorUtility.DisplayDialog("Beat em up",
            "Rahapudotukset:\n" + string.Join("\n", report) + "\n\nSeteli 1 mk, setelitukku 50 mk (1/25).\n\nTallenna scene (Ctrl+S).", "OK");
    }

    // ---------------- Rekvisiitta: moottoripyörät ----------------
    const string BikePath = "Assets/Sprites/Rekvisiitta/moottoripyora.png";             // keula vasemmalle
    const string BikeRightPath = "Assets/Sprites/Rekvisiitta/moottoripyora_oikea.png";  // keula oikealle (pakoputket oikealla puolella)
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
        if (pc == null || street == null || sprite == null)
        {
            EditorUtility.DisplayDialog("Beat em up", "Tarvitaan pelaaja, katutausta (kohta 4) ja kuva " + BikePath, "OK");
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
                // oikealle osoittava pyörä omasta kuvastaan; jos sitä ei ole, peilataan varalta
                sr.sprite = spot.flip && spriteRight != null ? spriteRight : sprite;
                sr.flipX = spot.flip && spriteRight == null;
                sr.sortingOrder = Mathf.RoundToInt(-y * 100f);            // sama syvyysjärjestys kuin hahmoilla
            }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        EditorUtility.DisplayDialog("Beat em up",
            $"{count} moottoripyörää jalkakäytävällä baarien ja S-Clubien edessä.\nPelaaja kulkee niiden edestä ja takaa syvyyden mukaan.\n\nTallenna scene (Ctrl+S).", "OK");
    }

    [MenuItem("Beat em up/17. Päivitä sivupotku")]
    static void UpdateSideKick()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { EditorUtility.DisplayDialog("Beat em up", "Scenessä ei ole pelaajaa.", "OK"); return; }
        Sprite[] sp = LoadSprites("sivupotku")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        if (sp.Length == 0) { EditorUtility.DisplayDialog("Beat em up", "sivupotku.png puuttuu tai sitä ei ole leikattu (kohta 1).", "OK"); return; }
        Undo.RecordObject(pc, "Sivupotku");
        pc.sideKickSprites = sp;
        pc.sideKickFrameTime = 0.06f;
        pc.sideKickImpactFrame = 3;
        pc.sideKickImpactHold = 0.14f;
        pc.sideKickArtOffset = 0.43f;     // sheetin kuvat siirretty 43 px vasemmalle
        EditorUtility.SetDirty(pc);
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        EditorUtility.DisplayDialog("Beat em up",
            $"Sivupotku: {sp.Length} kuvaa.\nK-nappi vuorottelee tavallisen potkun ja sivupotkun välillä.\n\nTallenna scene (Ctrl+S).", "OK");
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
        if (pc == null) { EditorUtility.DisplayDialog("Beat em up", "Scenessä ei ole pelaajaa.", "OK"); return; }
        Sprite[] sp = LoadSprites("hero_heitetty")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        if (sp.Length < 8) { EditorUtility.DisplayDialog("Beat em up", $"hero_heitetty.png: {sp.Length}/8 kuvaa. Tee ensin kohta 1.", "OK"); return; }
        Undo.RecordObject(pc, "Heittokuvat");
        pc.thrownSprites = sp;
        pc.kipUpSprites = LoadSprites("hero_kipup")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        EditorUtility.SetDirty(pc);
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        EditorUtility.DisplayDialog("Beat em up", $"Heron heittokuvat: {sp.Length} kuvaa.\nKip-up-nousu: {pc.kipUpSprites.Length} kuvaa.\n\nTallenna scene (Ctrl+S).", "OK");
    }

    [MenuItem("Beat em up/19. Päivitä pusku ja suojaus")]
    static void UpdatePushAndBlock()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { EditorUtility.DisplayDialog("Beat em up", "Scenessä ei ole pelaajaa.", "OK"); return; }
        Sprite[] push = LoadSprites("pusku")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        Sprite[] block = LoadSprites("suojaus")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        if (push.Length < 6 || block.Length < 5)
        {
            EditorUtility.DisplayDialog("Beat em up", $"pusku.png: {push.Length}/6 kuvaa, suojaus.png: {block.Length}/5 kuvaa. Tee ensin kohta 1.", "OK");
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
        EditorUtility.DisplayDialog("Beat em up",
            $"Pusku: {push.Length} kuvaa (U, ohjaimessa RT, tai lyönti suojauksesta). Kaataa vihollisen.\n" +
            $"Suojaus: {block.Length} kuvaa (pidä I, ohjaimessa LT). Torjuu edestä tulevat lyönnit, ei heittoja.\n\nTallenna scene (Ctrl+S).", "OK");
    }

    [MenuItem("Beat em up/20. Päivitä vastaheitto")]
    static void UpdateCounterThrow()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { EditorUtility.DisplayDialog("Beat em up", "Scenessä ei ole pelaajaa.", "OK"); return; }
        Sprite[] sp = LoadSprites("heitto")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        if (sp.Length < 7) { EditorUtility.DisplayDialog("Beat em up", $"heitto.png: {sp.Length}/8 kuvaa. Tee ensin kohta 1.", "OK"); return; }
        Undo.RecordObject(pc, "Vastaheitto");
        pc.counterThrowSprites = sp;
        EditorUtility.SetDirty(pc);
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        EditorUtility.DisplayDialog("Beat em up",
            $"Vastaheitto: {sp.Length} kuvaa.\nPaina O (ohjaimessa oikean tatin painallus) juuri kun vihollinen lyö:\n" +
            "ukko nappaa kädestä, vetää olan yli ja heittää selän taakse.\nOhi mennyt kurotus jättää hetkeksi alttiiksi.\n\nTallenna scene (Ctrl+S).", "OK");
    }

    [MenuItem("Beat em up/21. Päivitä vastaheitot (Kovis kuperkeikka, Punkkari niskalenkki)")]
    static void UpdateMonkeyFlip()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { EditorUtility.DisplayDialog("Beat em up", "Scenessä ei ole pelaajaa.", "OK"); return; }
        Sprite[] sp = LoadSprites("kuperkeikka")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        if (sp.Length < 8) { EditorUtility.DisplayDialog("Beat em up", $"kuperkeikka.png: {sp.Length}/8 kuvaa. Tee ensin kohta 1.", "OK"); return; }
        Undo.RecordObject(pc, "Kuperkeikkaheitto");
        pc.monkeyFlipSprites = sp;
        EditorUtility.SetDirty(pc);
        // Koviksille iso-merkintä (kuperkeikka), punkkareille ei (niskalenkki)
        int big = 0, small = 0;
        var sheetReport = new List<string>();
        Sprite[] kovisFlip = EnemySheet("vihu_kuperkeikka", sheetReport);
        Sprite[] punkHeadlock = EnemySheet("punk_niskalenkki", sheetReport);
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            Undo.RecordObject(e, "Iso vastus");
            e.bigBody = e.gameObject.name.StartsWith("Kovis");
            if (e.bigBody) e.flipThrownSprites = kovisFlip;   // Koviksen oma lento ja alastulo
            else if (e.gameObject.name.StartsWith("Punkkari")) e.headlockThrownSprites = punkHeadlock;
            if (e.bigBody) big++; else small++;
            EditorUtility.SetDirty(e);
        }
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        EditorUtility.DisplayDialog("Beat em up",
            $"Kuperkeikkaheitto: {sp.Length} kuvaa (+ kip-up lopuksi).\n" +
            $"Isoja vastuksia (Kovis, kuperkeikka): {big}\nMuita (niskalenkki): {small}\n" +
            $"Vihollisten omat kuvat: {string.Join(", ", sheetReport)}\n\n" +
            "Sama nappi O: ohjelma valitsee heiton vastuksen koon mukaan.\nIson vastuksen voi merkitä myös käsin Enemy-komponentin Big Body -ruudulla.\n\nTallenna scene (Ctrl+S).", "OK");
    }

    [MenuItem("Beat em up/22. Päivitä hyppy ja kuperkeikan lento")]
    static void UpdateJumpAndFlip()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { EditorUtility.DisplayDialog("Beat em up", "Scenessä ei ole pelaajaa.", "OK"); return; }
        Undo.RecordObject(pc, "Hyppy ja heitto");
        pc.jumpVelocity = 12.5f;     // n. 2.6 yksikköä korkea hyppy (ennen 2.0)
        pc.monkeyFlipSpeed = 9f;     // kuperkeikka lennättää n. 5.5 yksikköä (ennen n. 3)
        pc.monkeyFlipUp = 7.5f;
        EditorUtility.SetDirty(pc);
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        float h = pc.jumpVelocity * pc.jumpVelocity / (2f * pc.gravity);
        EditorUtility.DisplayDialog("Beat em up",
            $"Hypyn korkeus n. {h:0.0} yksikköä (Jump Velocity {pc.jumpVelocity}).\n" +
            $"Kuperkeikka: nopeus {pc.monkeyFlipSpeed}, nousu {pc.monkeyFlipUp}.\n\nTallenna scene (Ctrl+S).", "OK");
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
            EditorUtility.DisplayDialog("Beat em up", "Tarvitaan pelaaja, katutausta (kohta 4) ja kuva " + CratePath, "OK");
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
        var blocked = new List<float>();
        var bikes = GameObject.Find("Moottoripyörät");
        if (bikes != null) foreach (Transform b in bikes.transform) blocked.Add(b.position.x);
        foreach (var d in Object.FindObjectsByType<Door>(FindObjectsSortMode.None)) blocked.Add(d.transform.position.x);

        var ssr = street.GetComponent<SpriteRenderer>();
        float left = street.transform.position.x - ssr.size.x * 0.5f;
        float right = street.transform.position.x + ssr.size.x * 0.5f;
        int count = 0;
        for (float x = left + 7f; x < right - 4f; x += CrateSpacing)
        {
            float cx = x;
            // siirretään vähän sivuun, jos kohdalla on pyörä tai ovi
            for (int tries = 0; tries < 4 && blocked.Any(b => Mathf.Abs(b - cx) < 1.8f); tries++) cx += 1.5f;
            if (blocked.Any(b => Mathf.Abs(b - cx) < 1.8f)) continue;

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
            c.body = vis;
            c.shadow = sh;
            vis.sprite = sprites.Length > 0 ? sprites[0] : null;
            vis.transform.localPosition = new Vector3(0f, pc.sidewalkHeight - c.footOffset, 0f);
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        EditorUtility.DisplayDialog("Beat em up",
            $"{count} puulaatikkoa kävelykadulla {CrateSpacing:0} yksikön välein ({sprites.Length} kuvaa, sirpaleet: {(burst != null ? "OK" : "puuttuu")}).\n" +
            $"Pelaajan nosto- ja heittokuvat (nosto_heitto.png): {(carry.Length > 0 ? carry.Length + " kuvaa" : "puuttuu, käytetään varakuvia")}\n" +
            $"Kantoasento (kanto.png): {(carryPose != null ? "OK" : "puuttuu")}, kantokävely (kanto_kavely.png): {(carryWalk.Length > 0 ? carryWalk.Length + " kuvaa" : "puuttuu")}\n\n" +
            "O laatikon vieressä nostaa, lyönti/potku heittää. Kolme iskua hajottaa.\n\nTallenna scene (Ctrl+S).", "OK");
    }

    [MenuItem("Beat em up/24. Päivitä potkukombo (matala, etupotku, korkea)")]
    static void UpdateKickCombo()
    {
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) { EditorUtility.DisplayDialog("Beat em up", "Scenessä ei ole pelaajaa.", "OK"); return; }
        string path = FindTexture("korkea_potku");
        if (path != null) SetupAndSlice(path);
        Sprite[] sp = LoadSprites("korkea_potku")
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        if (sp.Length < 4) { EditorUtility.DisplayDialog("Beat em up", "korkea_potku.png puuttuu.", "OK"); return; }
        Undo.RecordObject(pc, "Potkukombo");
        pc.hiKickSprites = sp;
        pc.hiKickFrameTime = 0.05f;
        pc.hiKickImpactFrame = 3;
        pc.hiKickImpactHold = 0.14f;
        pc.hiKickArtOffset = 0.64f;     // sheetin kuvat siirretty 64 px vasemmalle
        EditorUtility.SetDirty(pc);
        EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
        EditorUtility.DisplayDialog("Beat em up",
            $"Korkea potku: {sp.Length} kuvaa.\nHakkaa K: matala potku → etupotku → korkea potku (kaataa).\n\nTallenna scene (Ctrl+S).", "OK");
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
            float[] lunges = { 0.18f, 0.26f, 0.40f };
            for (int i = 0; i < pc.punchCombo.Length && i < lunges.Length; i++) pc.punchCombo[i].lunge = lunges[i];
            EditorUtility.SetDirty(pc);
        }
        EditorSceneManager.MarkSceneDirty(fx.gameObject.scene);
        EditorUtility.DisplayDialog("Beat em up",
            "Osumapysäytys: kevyt 0.08 s, raskas 0.16 s.\nTärähdys: kevyt 0.08, raskas 0.22.\nLyöntikombon liuku: 0.18 / 0.26 / 0.40.\nPotkujen liuku ja osumaläiskät ovat koodissa.\n\nTallenna scene (Ctrl+S).", "OK");
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
