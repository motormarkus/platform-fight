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
        if (w % CellW != 0 || h % CellH != 0) { Object.DestroyImmediate(tex); return -1; }

        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Multiple;
        ti.spritePixelsPerUnit = 100;
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

    [MenuItem("Beat em up/7. Aseta taustamusiikki")]
    static void SetupMusic()
    {
        string path = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/Musiikki" })
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).FirstOrDefault();
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
        Debug.Log("Pelaaja luotu. Paina Play. Ohjaus: WASD/nuolet, Space = hyppy, J = lyönti, K = potku (ilmassa = hyppypotku).");
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
        e.hurtSounds = LoadClips("Assets/Audio/big thug", "gasp");
        e.hurtVolume = 0.99f;
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
