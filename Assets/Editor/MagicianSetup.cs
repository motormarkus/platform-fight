using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Taikuri (valikko 409): tuo kuvat spriteiksi (pivot jalat, 108 px/yksikkö), rakentaa Enemyn + MagicianBrainin + MagicianHatin ja sijoittaa taikurin kasinon showbaarin lavalle,
/// baariin ja hääkappeliin (siirrä tarvittaessa). Kuvat: Assets/Sprites/Taikuri/ (Tools/MagicianAssets/*.py, pivots.json: Tools/MagicianAssets/make_pivots.py).
/// Aja valikot 376, 378, 379, 406 ensin.</summary>
public static partial class BeatEmUpSetup
{
    const string TK = "Assets/Sprites/Taikuri/";
    const float MagicianPpu = 108f;

    [System.Serializable] class MgSet { public int n, w, h; public float px, py; }
    [System.Serializable] class MgPivots
    {
        public MgSet taikuri_isku, taikuri_kyyhky, taikuri_silinteri, taikuri_savu_heitto, taikuri_kaatuminen, taikuri_vaisto, taikuri_osuma, taikuri_kavely, taikuri_juoksu, taikuri_harka, taikuri_harka_juoksu, savu_rajahdys;
        public MgSet Get(string n) { return (MgSet)typeof(MgPivots).GetField(n).GetValue(this); }
    }

    static MgPivots mgPivots;
    static Sprite[] MgFrames(string set, int a, int b)
    {
        var l = new List<Sprite>(); for (int i = a; i <= b; i++) l.Add(MgSprite(set, i)); return l.ToArray();
    }
    static Sprite[] MgFramesList(string set, IEnumerable<int> idx) => idx.Select(i => MgSprite(set, i)).ToArray();
    static Sprite MgSprite(string set, int i)
    {
        string path = TK + set + "_" + i.ToString("00") + ".png";
        var info = mgPivots.Get(set);
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); ti = AssetImporter.GetAtPath(path) as TextureImporter; }
        if (ti == null) return null;
        var want = new Vector2(info.px, info.py);
        if (ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Single || !Mathf.Approximately(ti.spritePixelsPerUnit, MagicianPpu) || (ti.spritePivot - want).sqrMagnitude > 1e-8f || ti.mipmapEnabled)
        {
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.spritePixelsPerUnit = MagicianPpu; ti.mipmapEnabled = false;
            ti.filterMode = FilterMode.Bilinear; ti.wrapMode = TextureWrapMode.Clamp; ti.alphaIsTransparency = true; ti.maxTextureSize = 2048; ti.textureCompression = TextureImporterCompression.Compressed;
            var ts = new TextureImporterSettings(); ti.ReadTextureSettings(ts); ts.spriteAlignment = (int)SpriteAlignment.Custom; ts.spriteMeshType = SpriteMeshType.FullRect; ti.SetTextureSettings(ts);
            ti.spritePivot = want; ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static Sprite MgSingle(string file, Vector2 pivot, float ppu)
    {
        string path = TK + file;
        var ti = AssetImporter.GetAtPath(path) as TextureImporter; if (ti == null) return null;
        ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.spritePixelsPerUnit = ppu; ti.mipmapEnabled = false; ti.alphaIsTransparency = true;
        var ts = new TextureImporterSettings(); ti.ReadTextureSettings(ts); ts.spriteAlignment = (int)SpriteAlignment.Custom; ti.SetTextureSettings(ts); ti.spritePivot = pivot; ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static Sprite[] Hold(Sprite[] a, Sprite last, int extra) { var l = new List<Sprite>(a); for (int i = 0; i < extra; i++) l.Add(last); return l.ToArray(); }

    [MenuItem("Beat em up/409. Las Vegas: taikuri (showbaarin lava, baari, hääkappeli)", false, 409)]
    static void AddVegasMagician()
    {
        if (PlayModeBlocked()) return;
        string pj = TK + "pivots.json";
        if (!File.Exists(pj)) { Info("Puuttuu " + pj + ": aja Tools/MagicianAssets/make_pivots.py."); return; }
        mgPivots = JsonUtility.FromJson<MgPivots>(File.ReadAllText(pj));
        if (!File.Exists(VegasScenePath)) { Info("Aja ensin valikko 376 (Vegas)."); return; }
        Sprite[] guard, calm, walk, run, jab, wand, cross, kick, hurt, down, getup, dodge, iceM, cardsM, mirrorM, smokeM, expl, poof, mApproach, mStrike, kickBrain, bullM; int bullFrom;
        Sprite hatSp;
        const string I = "taikuri_isku";
        guard = MgFrames(I, 12, 26);
        calm = MgFrames("taikuri_kyyhky", 0, 119).Concat(MgFrames("taikuri_silinteri", 0, 119)).ToArray();
        walk = MgFrames("taikuri_kavely", 0, 28); run = MgFrames("taikuri_juoksu", 0, 28);
        jab = MgFrames(I, 40, 49); wand = MgFrames(I, 50, 59); cross = MgFrames(I, 24, 31); kick = MgFrames(I, 60, 79);
        hurt = MgFrames("taikuri_osuma", 0, 5);
        down = MgFrames("taikuri_kaatuminen", 11, 35).Concat(new[] { MgSprite("taikuri_kaatuminen", 40) }).ToArray();
        getup = MgFrames("taikuri_kaatuminen", 42, 74);
        dodge = MgFrames("taikuri_vaisto", 0, 5);
        iceM = MgFrames(I, 30, 38);
        var cardsRaw = MgFrames(I, 98, 105); cardsM = Hold(cardsRaw, cardsRaw[cardsRaw.Length - 1], 4);
        mirrorM = MgFrames("taikuri_silinteri", 0, 30);
        var smokeRaw = MgFrames("taikuri_savu_heitto", 0, 28); smokeM = Hold(smokeRaw, smokeRaw[smokeRaw.Length - 1], 26);
        expl = MgFrames("savu_rajahdys", 0, mgPivots.savu_rajahdys.n - 1);
        poof = Enumerable.Range(0, 8).Select(i => MgPoof(i)).Where(s => s != null).ToArray();
        // härkä: muodonmuutos (kuvat 0-45 kahdennettuina) + juoksusilmukka x 5; liike alkaa kuvasta 72
        var harkaA = MgFrames("taikuri_harka", 0, 45).SelectMany(sp => new[] { sp, sp }).ToArray(); var harkaB = MgFrames("taikuri_harka_juoksu", 0, 12);
        bullM = harkaA.Concat(Enumerable.Repeat(harkaB, 5).SelectMany(x => x)).ToArray(); bullFrom = 2 * 36;   // härkä lähtee liikkeelle heti kun se alkaa erottua savusta (videon kuva ~72), ei vasta kun muodonmuutos on kokonaan ohi
        mApproach = run; mStrike = jab; kickBrain = kick;
        hatSp = MgSingle("taikuri_hattu.png", new Vector2(0.5f, 0.5f), MagicianPpu);
        foreach (var arr in new[] { guard, calm, walk, run, jab, wand, cross, kick, hurt, down, getup, dodge, iceM, cardsM, mirrorM, smokeM, expl, bullM })
            if (arr == null || arr.Length == 0 || arr.Any(s => s == null)) { Info("Taikurin kuvien tuonti epäonnistui (puuttuvia kuvia). Aja Tools/MagicianAssets-skriptit ja make_pivots.py."); return; }
        if (hatSp == null) { Info("taikuri_hattu.png puuttuu."); return; }

        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var areas = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == scene).ToArray();
            foreach (var o in scene.GetRootGameObjects().Where(g => g.name == "Taikurit").ToArray()) Undo.DestroyObjectImmediate(o);
            var root = new GameObject("Taikurit"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene); Undo.RegisterCreatedObjectUndo(root, "Taikurit");
            var placed = new List<string>();
            foreach (var (areaName, frac) in new[] { ("Vegasin showbaari", 0.5f), ("Vegasin baari", 0.5f), ("Vegasin hääkappeli", 0.5f) })
            {
                var area = areas.FirstOrDefault(a => a.areaName == areaName);
                if (area == null) continue;
                float x = Mathf.Lerp(area.walkMinX, area.walkMaxX, frac), y = Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.45f);
                if (areaName == "Vegasin hääkappeli") { x = VegasChapelX0 + (5120f / (1844f / (2f * CamHalf))) * 1.66f; y = Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.62f); }   // lähellä Melviniä (alttari), alempana
                if (areaName == "Vegasin showbaari") x = VegasShowbarX0 + (5120f / (1844f / (2f * CamHalf))) * 1.5f;   // lavan keskelle (kuva 2)
                BuildMagician(root.transform, areaName, new Vector3(x, y, 0f), guard, calm, walk, run, jab, wand, cross, kick, hurt, down, getup, dodge, iceM, cardsM, mirrorM, smokeM, expl, poof, mApproach, mStrike, hatSp, bullM, bullFrom);
                placed.Add(areaName);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Taikuri rakennettu: " + (placed.Count > 0 ? string.Join(", ", placed) : "ei yhtään aluetta löytynyt (aja 376, 378, 379, 406)") + ". Paikat ovat alueen keskellä (showbaarissa lavalla): siirrä tarvittaessa.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    static Sprite MgPoof(int i)
    {
        string path = TK + "taikuri_poofpieni_" + i.ToString("00") + ".png";
        var ti = AssetImporter.GetAtPath(path) as TextureImporter; if (ti == null) return null;
        if (ti.textureType != TextureImporterType.Sprite || !Mathf.Approximately(ti.spritePixelsPerUnit, 44f))
        {
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.spritePixelsPerUnit = 44f; ti.mipmapEnabled = false; ti.alphaIsTransparency = true;
            var ts = new TextureImporterSettings(); ti.ReadTextureSettings(ts); ts.spriteAlignment = (int)SpriteAlignment.BottomCenter; ti.SetTextureSettings(ts); ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void BuildMagician(Transform parent, string areaName, Vector3 pos, Sprite[] guard, Sprite[] calm, Sprite[] walk, Sprite[] run, Sprite[] jab, Sprite[] wand, Sprite[] cross, Sprite[] kick,
        Sprite[] hurt, Sprite[] down, Sprite[] getup, Sprite[] dodge, Sprite[] iceM, Sprite[] cardsM, Sprite[] mirrorM, Sprite[] smokeM, Sprite[] expl, Sprite[] poof, Sprite[] mApproach, Sprite[] mStrike, Sprite hatSp, Sprite[] bullM, int bullFrom)
    {
        const float F = 1f / 24f;
        var go = new GameObject("Taikuri (" + areaName + ")"); go.transform.SetParent(parent, false); go.transform.position = pos;
        var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
        var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
        var e = go.AddComponent<Enemy>();
        e.body = visual; e.shadow = shadow; visual.sprite = guard[0];
        e.displayName = "Taikuri";
        e.visualScale = 1f; e.attackArtScale = 1f; e.knockArtScale = 1f; e.walkArtScale = 1f;
        e.idleSprites = guard; e.idleFrameTime = 0.1f; e.calmIdleSprites = calm;   // tempputemput (kyyhky ja silinteri) kunnes tappelu alkaa
        e.walkSprites = walk; e.walkFrameTime = F; e.runSprites = run; e.runFrameTime = F; e.runSpeedMultiplier = 2.0f; e.runDistance = 5.5f;
        e.maxHealth = 700; e.moveSpeedX = 3.0f; e.moveSpeedY = 2.3f; e.depthTolerance = 0.45f; e.attackCooldown = 0.55f; e.wakeDistance = 8f;
        e.comboChance = 0.55f; e.comboWindupScale = 0.6f; e.comboRecoverFraction = 0.6f; e.attackRange = 2.3f;
        // J = jabi, S = kepin työntö, K = suora (kaataa)
        e.punchSprites = jab; e.punchImpactFrame = 4; e.windupTime = 4 * F; e.punchRecoverTime = (jab.Length - 4) * F; e.punchActiveTime = 0.12f; e.punchDamage = 10;
        e.punch2Sprites = wand; e.punch2ImpactFrame = 4; e.punch2Damage = 13; e.punch2Knockdown = false; e.punch2Chance = 0.4f;
        e.punch3Sprites = cross; e.punch3ImpactFrame = 4; e.punch3Damage = 20; e.punch3Knockdown = true; e.punch3IsPunch = true; e.punch3Chance = 0.3f; e.punch3WindupTime = 4 * F; e.punch3RecoverTime = (cross.Length - 4) * F; e.punch3Reach = 2.8f;
        e.altChance = 0.3f; e.altExtraWindup = 0.08f;
        e.altVariants = new[] { new Enemy.AltVariant { name = "Korkea potku", sprites = kick, impactFrame = 8, frameTime = F, damage = 20, reach = 3.4f, knockdown = true, knockSpeed = 8f, knockUp = 5f, weight = 1f } };
        e.combos = new[] { "JS", "JK", "JSK", "J", "SK" };
        e.altAttackSprites = kick; e.altImpactFrame = 8; e.altAnimationFrameTime = F; e.altDamage = 20; e.altReach = 3.4f;
        e.hurtSprites = hurt; e.hurtTime = 0.34f;
        e.knockdownSprites = down; e.airFrameTime = 1f / 30f;
        e.getUpSprites = getup; e.getUpTime = getup.Length / 30f;
        e.blockChance = 0f;
        e.hurtSounds = LoadClips("Assets/Audio/big thug", "gasp"); e.hurtVolume = 0.95f;
        string bvoice = AssetDatabase.GetSubFolders("Assets/Audio").FirstOrDefault(f => Path.GetFileName(f).ToLowerInvariant().StartsWith("portsari"));
        e.tauntSounds = bvoice != null ? LoadClips(bvoice, "poke") : new AudioClip[0];
        e.ally = false; e.fightsEveryone = false;

        var brain = go.AddComponent<MagicianBrain>();
        brain.chancePerSec = 0.45f; brain.globalCooldown = 2.2f; brain.moves = new MongolBrain.Move[0];
        MongolBrain.Move M(string name, Sprite[] sp, float ft, int hitFrom) => new MongolBrain.Move { name = name, sprites = sp, frameTime = ft, hitFrom = hitFrom, hitTo = -1, damage = 0, impactHold = 0.05f };
        brain.spells = new[]
        {
            new MagicianBrain.Spell { name = "Savupommi", kind = MagicianBrain.Kind.Smoke, move = M("Savupommi", smokeM, F, 28), eventFrame = 28, minRange = 0f, maxRange = 4f, weight = 0.5f, cooldown = 16f },
            new MagicianBrain.Spell { name = "Jäätävä lattia", kind = MagicianBrain.Kind.Ice, move = M("Jää", iceM, F * 1.4f, 4), eventFrame = 4, minRange = 2.5f, maxRange = 10f, weight = 0.9f, cooldown = 9f },
            new MagicianBrain.Spell { name = "Korttisuihku", kind = MagicianBrain.Kind.Cards, move = M("Kortit", cardsM, F * 1.4f, 7), eventFrame = 7, minRange = 2f, maxRange = 12f, weight = 0.9f, cooldown = 8f },
            new MagicianBrain.Spell { name = "Peilikuvat", kind = MagicianBrain.Kind.Mirrors, move = M("Peilikuvat", mirrorM, 0.05f, 13), eventFrame = 13, minRange = 2f, maxRange = 10f, weight = 0.7f, cooldown = 12f },
        };
        var bullMove = new MongolBrain.Move { name = "Härkä", sprites = bullM, frameTime = 0.027f, hitFrom = 0, hitTo = -1, damage = 0, impactHold = 0f, moveFrom = bullFrom, moveTo = 99999, moveSpeed = 14f };
        var sp2 = new System.Collections.Generic.List<MagicianBrain.Spell>(brain.spells);
        sp2.Add(new MagicianBrain.Spell { name = "Härkä", kind = MagicianBrain.Kind.Bull, move = bullMove, eventFrame = 0, minRange = 4f, maxRange = 14f, weight = 0.6f, cooldown = 22f });
        brain.spells = sp2.ToArray();
        brain.dodgeMove = M("Väistö", dodge, 0.05f, 3); brain.dodgeEventFrame = 3; brain.dodgeChance = 0.3f;
        brain.teleportKick = new MongolBrain.Move { name = "Kaatava potku", sprites = kick, frameTime = F, hitFrom = 8, hitTo = 10, reach = 3.4f, damage = 20, knockdown = true };
        brain.explosionSprites = expl; brain.explosionFrameTime = 0.05f; brain.explosionScale = 1f; brain.explosionPivot = new Vector2(0.5f, 0.05f);
        brain.poofSmall = poof;
        brain.mirrorConfig = new MirrorImages.Config { approach = mApproach, strike = mStrike, poof = poof, hitFrame = 4, copies = 3, damage = 12, poofScale = 1f };
        brain.cardConfig = new CardBarrage.Config();
        var hat = go.AddComponent<MagicianHat>(); hat.hatSprite = hatSp;
        EditorUtility.SetDirty(e); EditorUtility.SetDirty(brain); EditorUtility.SetDirty(hat);
    }
}
