using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Khan (valikko 416): Muurin tornin katon pomo. Tuo kuvat (Assets/Sprites/Khan, Tools/KhanAssets/make_khan.py + make_khan_fx.py, pivot jalat, 108 px/yksikkö),
/// rakentaa Enemyn + KhanBrainin (iskut, syöksy, lohikäärmeen henkäys) ja sijoittaa Khanin alueelle "Muurin torni (katto)" (Muuri.unity). Aja valikko 343 ensin. Ajo uudelleen turvallinen.
/// Puuttuu vielä: kuolema.</summary>
public static partial class BeatEmUpSetup
{
    const string KH = "Assets/Sprites/Khan/";
    const float KhanPpu = 108f;

    [System.Serializable] class KhSet { public int n, w, h, x0, y1; public float px, py, mouth_vx, mouth_vy, scale; }
    [System.Serializable] class KhPivots
    {
        public KhSet khan_idle, khan_kavely, khan_juoksu, khan_isku, khan_potku, khan_matala, khan_kierre, khan_syoksy, khan_erikois, khan_paa, khan_osuma, khan_kaatuminen, khan_maahan, khan_nousu, khan_puuska, khan_suojaus, khan_isku2, khan_isku3, khan_alakoukku, khan_suora, khan_hyppy;
        public KhSet Get(string n) { return (KhSet)typeof(KhPivots).GetField(n).GetValue(this); }
    }
    static KhPivots khPivots;

    static Sprite[] KhFrames(string set, int a, int b)
    {
        var l = new List<Sprite>(); for (int i = a; i <= b; i++) l.Add(KhSprite(set, i)); return l.ToArray();
    }
    static Sprite KhSprite(string set, int i)
    {
        string path = KH + set + "_" + i.ToString("00") + ".png";
        var info = khPivots.Get(set);
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); ti = AssetImporter.GetAtPath(path) as TextureImporter; }
        if (ti == null) return null;
        var want = new Vector2(info.px, info.py);
        if (ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Single || !Mathf.Approximately(ti.spritePixelsPerUnit, KhanPpu) || (ti.spritePivot - want).sqrMagnitude > 1e-8f || ti.mipmapEnabled)
        {
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.spritePixelsPerUnit = KhanPpu; ti.mipmapEnabled = false;
            ti.filterMode = FilterMode.Bilinear; ti.wrapMode = TextureWrapMode.Clamp; ti.alphaIsTransparency = true; ti.maxTextureSize = 2048; ti.textureCompression = TextureImporterCompression.Compressed;
            var ts = new TextureImporterSettings(); ti.ReadTextureSettings(ts); ts.spriteAlignment = (int)SpriteAlignment.Custom; ts.spriteMeshType = SpriteMeshType.FullRect; ti.SetTextureSettings(ts);
            ti.spritePivot = want; ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static Sprite KhSingle(string file)
    {
        string path = KH + file;
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); ti = AssetImporter.GetAtPath(path) as TextureImporter; }
        if (ti == null) return null;
        ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.spritePixelsPerUnit = KhanPpu; ti.mipmapEnabled = false; ti.alphaIsTransparency = true; ti.filterMode = FilterMode.Bilinear;
        var ts = new TextureImporterSettings(); ti.ReadTextureSettings(ts); ts.spriteAlignment = (int)SpriteAlignment.Center; ts.spriteMeshType = SpriteMeshType.FullRect; ti.SetTextureSettings(ts); ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static AudioClip KhClip(string n) { return AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Sfx/Khan/" + n + ".wav"); }

    [MenuItem("Beat em up/416. Muuri: Khan (tornin katto, iskut, syöksy, lohikäärmeen henkäys)", false, 416)]
    static void AddKhan()
    {
        if (PlayModeBlocked()) return;
        string pj = KH + "pivots.json";
        if (!File.Exists(pj)) { Info("Puuttuu " + pj + ": aja Tools/KhanAssets/make_khan.py."); return; }
        AssetDatabase.Refresh();
        khPivots = JsonUtility.FromJson<KhPivots>(File.ReadAllText(pj));
        const float F = 2f / 24f;   // 12 kuvaa/s (joka toinen videon kuva)
        var idle = KhFrames("khan_idle", 0, khPivots.khan_idle.n - 1); var walk = KhFrames("khan_kavely", 0, khPivots.khan_kavely.n - 1); var run = KhFrames("khan_juoksu", 0, khPivots.khan_juoksu.n - 1);
        var isku = KhFrames("khan_isku", 0, khPivots.khan_isku.n - 1); var potku = KhFrames("khan_potku", 0, khPivots.khan_potku.n - 1); var matala = KhFrames("khan_matala", 0, 10);   // kuvat 11-14 ovat kierrepotkun alkua (outo pyörähdys): pois
        var kierre = KhFrames("khan_kierre", 0, khPivots.khan_kierre.n - 1); var syoksy = KhFrames("khan_syoksy", 0, 28);
        var erik = KhFrames("khan_erikois", 0, 100);   // kuvat 101-119: vain tuhkaa ja savua (Khan seisoo jo)
        var hurt = KhFrames("khan_osuma", 0, khPivots.khan_osuma.n - 1); var fall = KhFrames("khan_kaatuminen", 0, khPivots.khan_kaatuminen.n - 1);
        var land = KhFrames("khan_maahan", 0, khPivots.khan_maahan.n - 1); var getup = KhFrames("khan_nousu", 0, khPivots.khan_nousu.n - 1);
        var gustS = KhFrames("khan_puuska", 0, 104);   // kuvat 105-108: Khan seisoo jo
        var guardAll = KhFrames("khan_suojaus", 0, khPivots.khan_suojaus.n - 1);
        var isku2 = KhFrames("khan_isku2", 0, khPivots.khan_isku2.n - 1); var isku3 = KhFrames("khan_isku3", 0, khPivots.khan_isku3.n - 1); var alak = KhFrames("khan_alakoukku", 0, khPivots.khan_alakoukku.n - 1);
        var suora = KhFrames("khan_suora", 0, khPivots.khan_suora.n - 1); var hyppy = KhFrames("khan_hyppy", 0, khPivots.khan_hyppy.n - 1);
        var head = KhSingle("khan_paa.png");
        var flames = Enumerable.Range(0, 12).Select(i => KhSingle("khan_liekki_" + i + ".png")).Where(s => s != null).ToArray();
        foreach (var arr in new[] { idle, walk, run, isku, potku, matala, kierre, syoksy, erik, hurt, fall, land, getup, gustS, guardAll, isku2, isku3, alak, suora, hyppy })
            if (arr == null || arr.Length == 0 || arr.Any(s => s == null)) { Info("Khanin kuvien tuonti epäonnistui (puuttuvia kuvia): aja Tools/KhanAssets/make_khan.py."); return; }
        if (head == null || flames.Length == 0) { Info("Lohikäärmeen pää tai liekkipalat puuttuvat: aja make_khan.py ja make_khan_fx.py."); return; }

        // suun paikka Khanin jaloista (maailmayksikköä): videon suupiste -> erikoisliikkeen kuvan pivotti
        var es = khPivots.khan_erikois; var hs = khPivots.khan_paa;
        float pivVx = es.x0 + es.px * es.w / hs.scale, groundVy = es.y1 - es.py * es.h / hs.scale;
        var mouth = new Vector2((hs.mouth_vx - pivVx) * hs.scale / KhanPpu, (groundVy - hs.mouth_vy) * hs.scale / KhanPpu);

        if (!File.Exists(WallScenePath)) { Info("Muuri-scene puuttuu: aja valikko 327/343."); return; }
        var scene = EditorSceneManager.GetSceneByPath(WallScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(WallScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var roof = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.gameObject.scene == scene && a.areaName == "Muurin torni (katto)");
            if (roof == null) { Info("Alue \"Muurin torni (katto)\" puuttuu: aja valikko 343."); return; }
            foreach (var o in scene.GetRootGameObjects().Where(g => g.name == "Khan (katto)").ToArray()) Undo.DestroyObjectImmediate(o);
            float x = Mathf.Lerp(roof.walkMinX, roof.walkMaxX, 0.68f), y = Mathf.Lerp(roof.maxDepthY, roof.minDepthY, 0.5f);
            var go = new GameObject("Khan (katto)"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene); Undo.RegisterCreatedObjectUndo(go, "Khan");
            go.transform.position = new Vector3(x, y, 0f);
            var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
            var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
            var e = go.AddComponent<Enemy>();
            e.body = visual; e.shadow = shadow; visual.sprite = idle[0];
            e.displayName = "Khan";
            e.visualScale = 1f; e.attackArtScale = 1f; e.knockArtScale = 1f; e.walkArtScale = 1f;
            e.idleSprites = idle; e.idleFrameTime = F; e.walkSprites = walk; e.walkFrameTime = F; e.runSprites = run; e.runFrameTime = F; e.runSpeedMultiplier = 2.1f; e.runDistance = 6f;
            e.maxHealth = 950; e.moveSpeedX = 2.6f; e.moveSpeedY = 2.0f; e.depthTolerance = 0.45f; e.attackCooldown = 0.6f; e.wakeDistance = 10f; e.attackRange = 2.4f;
            e.comboChance = 0.55f; e.comboWindupScale = 0.7f; e.comboRecoverFraction = 0.7f;
            // J = jabi (video H), S = toinen jabi (H), K = korkea potku (kaataa); vaihtoehtoiset: matala potku, kierrepotku, alakoukku, pitkä suora, jabi + suora (video B)
            e.punchSprites = isku2; e.punchImpactFrame = 4; e.windupTime = 4 * F; e.punchRecoverTime = (isku2.Length - 4) * F * 0.7f; e.punchActiveTime = 0.12f; e.punchDamage = 12;
            e.punch2Sprites = isku3; e.punch2ImpactFrame = 2; e.punch2Damage = 15; e.punch2Knockdown = false; e.punch2Chance = 0.4f;
            e.punch3Sprites = potku; e.punch3ImpactFrame = 4; e.punch3Damage = 22; e.punch3Knockdown = true; e.punch3IsPunch = false; e.punch3Chance = 0.3f; e.punch3WindupTime = 4 * F; e.punch3RecoverTime = (potku.Length - 4) * F; e.punch3Reach = 3.4f;
            e.altChance = 0.4f; e.altExtraWindup = 0.08f;
            e.altVariants = new[]
            {
                new Enemy.AltVariant { name = "Matala potku", sprites = matala, impactFrame = 3, frameTime = F, damage = 20, reach = 3.6f, knockdown = true, knockSpeed = 8f, knockUp = 5f, weight = 1f },
                new Enemy.AltVariant { name = "Kierrepotku", sprites = kierre, impactFrame = 3, frameTime = F, damage = 24, reach = 3.4f, knockdown = true, knockSpeed = 9f, knockUp = 5f, weight = 1f },
                new Enemy.AltVariant { name = "Alakoukku", sprites = alak, impactFrame = 4, frameTime = F, damage = 20, reach = 3.0f, knockdown = true, knockSpeed = 5f, knockUp = 9f, weight = 1f },
                new Enemy.AltVariant { name = "Pitkä suora", sprites = suora, impactFrame = 3, frameTime = F, damage = 18, reach = 3.6f, knockdown = true, knockSpeed = 7f, knockUp = 5f, weight = 0.8f },
                new Enemy.AltVariant { name = "Jabi ja suora", sprites = isku, impactFrame = 10, frameTime = F, damage = 16, reach = 2.8f, knockdown = false, weight = 0.8f },
            };
            e.combos = new[] { "JS", "JK", "JSK", "SK", "J" };
            e.altAttackSprites = matala; e.altImpactFrame = 3; e.altAnimationFrameTime = F; e.altDamage = 20; e.altReach = 3.6f;
            e.hurtSprites = hurt; e.hurtTime = 0.34f;
            e.knockdownSprites = fall; e.airFrameTime = 1f / 14f;     // kaatuminen ilmassa (kuvat 16-46), sitten pomppu + makuu (landSprites)
            e.landSprites = land; e.landFrameTime = F;
            e.getUpSprites = getup; e.getUpTime = 2.4f;                // nousu 3,8 s -> 2,4 s
            // suojaus: kääriytyy viittaansa (kääriytyminen, pito, avautuminen)
            var blk = new System.Collections.Generic.List<Sprite>();
            for (int i = 0; i <= 12; i++) blk.Add(guardAll[i]);
            for (int i = 0; i < 6; i++) blk.Add(guardAll[18]);
            for (int i = 44; i < guardAll.Length; i++) blk.Add(guardAll[i]);
            e.blockSprites = blk.ToArray(); e.blockTime = 0.95f; e.blockChance = 0.3f;
            var hc = KhClip("osuma"); e.hurtSounds = hc != null ? new[] { hc } : LoadClips("Assets/Audio/big thug", "gasp"); e.hurtVolume = 1f;
            e.ally = false; e.fightsEveryone = false;

            var brain = go.AddComponent<KhanBrain>();
            brain.chancePerSec = 0.5f; brain.globalCooldown = 2.0f; brain.rageBelow = 0.4f; brain.rageScale = 0.6f; brain.rageSpeed = 1.15f;
            brain.moves = new[]
            {
                new MongolBrain.Move { name = "Syöksy", sprites = syoksy, frameTime = F, hitFrom = 12, hitTo = 14, reach = 3.2f, minRange = 4.5f, maxRange = 12f, weight = 1f, cooldown = 8f, damage = 24, knockdown = true,
                    impactHold = 0f, moveFrom = 6, moveTo = 14, moveSpeed = 10f, sound = KhClip("syoksy"), soundVolume = 1f },
            };
            brain.moves = brain.moves.Concat(new[] { new MongolBrain.Move { name = "Hyppypotku", sprites = hyppy, frameTime = F * 0.75f, hitFrom = 17, hitTo = 36, reach = 3.8f, minRange = 3.5f, maxRange = 11f, weight = 1f, cooldown = 9f, damage = 26, knockdown = true,
                impactHold = 0f, moveFrom = 13, moveTo = 36, moveSpeed = 5.5f, sound = KhClip("hyppy"), soundVolume = 1f } }).ToArray();
            // erikoiskombo: jabi, takasuora, etupotku, alapotku, koukku, pitkä suora, kierrepotku, hyppypotku (viimeinen kaataa)
            float CF = F * 0.7f;
            MongolBrain.Move CM(string n, Sprite[] sp, int hit, int hitEnd, float reach, int dmg, bool kd, int mTo, float mSpd = 2.4f) => new MongolBrain.Move { name = n, sprites = sp, frameTime = CF, hitFrom = hit, hitTo = hitEnd, reach = reach, damage = dmg, knockdown = kd, impactHold = 0.03f, moveFrom = 0, moveTo = mTo, moveSpeed = mSpd };
            brain.combo = new[]
            {
                CM("Kombo: jabi", isku2, 4, 5, 3.0f, 10, false, 3),
                CM("Kombo: takasuora", isku.Skip(8).ToArray(), 2, 3, 3.0f, 12, false, 1),
                CM("Kombo: etupotku", potku, 4, 5, 3.4f, 14, false, 3),
                CM("Kombo: alapotku", matala, 3, 4, 3.6f, 14, false, 2),
                CM("Kombo: koukku", alak, 4, 5, 3.0f, 16, false, 3),
                CM("Kombo: pitkä suora", suora, 3, 4, 3.6f, 16, false, 2),
                CM("Kombo: kierrepotku", kierre, 3, 4, 3.4f, 20, false, 2),
                new MongolBrain.Move { name = "Kombo: hyppypotku", sprites = hyppy.Skip(12).ToArray(), frameTime = CF, hitFrom = 5, hitTo = 24, reach = 3.8f, damage = 30, knockdown = true, impactHold = 0f, moveFrom = 1, moveTo = 24, moveSpeed = 5f, sound = KhClip("hyppy"), soundVolume = 1f },
            };
            brain.special = new MongolBrain.Move { name = "Lohikäärmeen henkäys", sprites = erik, frameTime = F, hitFrom = 48, hitTo = -1, damage = 0, impactHold = 0f, sound = KhClip("erikois"), soundVolume = 1f };   // hitFrom = tulen alku: lataus (kuvat 0-47) kiihtyy
            brain.gust = new MongolBrain.Move { name = "Puuska", sprites = gustS, frameTime = 0.04f, hitFrom = 59, hitTo = -1, damage = 0, impactHold = 0f, sound = KhClip("puuska_lataus"), soundVolume = 1f };   // kiihdytetty (25 kuvaa/s); hitFrom = purkaus
            brain.gustBlast = 59;
            brain.fireFrom = 48; brain.fireTo = 84; brain.mouth = mouth; brain.flames = flames; brain.head = head;
            EditorUtility.SetDirty(e); EditorUtility.SetDirty(brain);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Khan rakennettu katolle (x " + x.ToString("0.0") + "): 950 elämää, iskut + syöksy + lohikäärmeen henkäys + puuska (suun paikka " + mouth.x.ToString("0.00") + ", " + mouth.y.ToString("0.00") + "). Osumareaktio, kaatuminen, nousu, suojaus ja hyppypotku mukana. Puuttuu: kuolema.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
