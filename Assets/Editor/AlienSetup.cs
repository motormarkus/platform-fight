using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Koe-olento (alien-pomo): kuvat Assets/Sprites/Viholliset/alien_*.png (Tools/AlienAssets), aivo AlienBrain, ääni Resources/Sfx/Alien/karjaisu.wav.
/// Valikko 332 lisää yhden alienin Muuri-scenen kuun pinnan vasempaan päähän ja panee kuurobotit hyökkäämään sen kimppuun (huntsBrawlers).
/// MakeAlien on valmis käytettäväksi myös laboratoriossa.</summary>
public static partial class BeatEmUpSetup
{
    static readonly string[] AlienSheetNames = { "idle", "kavely", "laukka", "raapiminen", "karjaisu", "pusku", "hantahuitaisu", "loikka", "osuma", "kaatuminen", "maahan", "nousu" };

    /// <summary>Lataa alienin kuvasarjat; puuttuvan nimi palautetaan missing-parametrissa.</summary>
    static Dictionary<string, Sprite[]> LoadAlienSheets(out string missing)
    {
        var d = new Dictionary<string, Sprite[]>(); missing = null;
        foreach (var n in AlienSheetNames)
        {
            var sp = LoadWalkerSprites("alien_" + n);
            if (sp == null || sp.Length == 0) { missing = "alien_" + n + ".png"; return null; }
            d[n] = sp;
        }
        return d;
    }

    static Sprite[] Seq(Sprite[] sheet, params int[] cells) => cells.Select(c => sheet[Mathf.Clamp(c, 0, sheet.Length - 1)]).ToArray();
    static Sprite[] SeqRange(Sprite[] sheet, int first, int last) => Enumerable.Range(first, last - first + 1).Select(c => sheet[Mathf.Clamp(c, 0, sheet.Length - 1)]).ToArray();

    /// <summary>Luo alienin (Enemy + AlienBrain) annettuun paikkaan. Aggressiivinen pomo: nopea, käy kaikkien (herojen ja vihujen) kimppuun.</summary>
    static Enemy MakeAlien(Transform parent, Vector3 pos, Dictionary<string, Sprite[]> s)
    {
        var go = new GameObject("Koe-olento");
        go.transform.SetParent(parent, false);
        var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
        var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
        var e = go.AddComponent<Enemy>();
        e.body = visual; e.shadow = shadow; visual.sprite = s["idle"][0];
        e.displayName = "Koe-olento"; e.isBoss = true; e.fightsEveryone = true; e.bigBody = true;
        e.visualScale = 1f; e.attackArtScale = 1f; e.knockArtScale = 1f; e.walkArtScale = 1f;
        e.idleSprites = s["idle"]; e.idleFrameTime = 0.1f;
        e.walkSprites = s["kavely"]; e.walkFrameTime = 0.05f;
        e.runSprites = s["laukka"]; e.runFrameTime = 0.04f; e.runSpeedMultiplier = 2.0f; e.runDistance = 4.5f;
        e.maxHealth = 650; e.moveSpeedX = 4.6f; e.moveSpeedY = 3.4f; e.attackRange = 3.0f; e.attackCooldown = 0.35f; e.wakeDistance = 24f; e.depthTolerance = 0.55f;
        // tavallinen isku = raapaisu (raapimisen alku)
        const float F = 0.045f;
        e.punchSprites = SeqRange(s["raapiminen"], 0, 9); e.punchImpactFrame = 4; e.windupTime = 4 * F; e.punchRecoverTime = 6 * F; e.punchDamage = 11;
        e.combos = new[] { "J" }; e.comboChance = 0.3f;
        e.hurtSprites = SeqRange(s["osuma"], 0, 3); e.hurtTime = 0.28f;
        e.knockdownSprites = s["kaatuminen"]; e.airFrameTime = 0.05f;
        e.landSprites = s["maahan"]; e.landFrameTime = 0.08f;
        e.getUpSprites = s["nousu"]; e.getUpTime = 0.9f;
        var brain = go.AddComponent<AlienBrain>();
        var roarClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Sfx/Alien/karjaisu.wav");
        // karjaisun kuvien kestot videon mukaan (24 fps), jotta karjaisu osuu äänen huippuun: video-ruudut 0,6,12,18,24,28,31,34,37,40,44,48,52,56,60,64,68,72,76,79,82,85,88,92,96
        int[] vf = { 0, 6, 12, 18, 24, 28, 31, 34, 37, 40, 44, 48, 52, 56, 60, 64, 68, 72, 76, 79, 82, 85, 88, 92, 96 };
        var roarTimes = new float[vf.Length];
        for (int k = 0; k < vf.Length; k++) roarTimes[k] = (k + 1 < vf.Length ? vf[k + 1] - vf[k] : 4) / 24f;
        brain.roar = new AlienBrain.Move
        {
            name = "karjaisu", sprites = s["karjaisu"], frameTimes = roarTimes, armor = true, allAround = true, reach = 5f, damage = 5,
            windows = new[] { 7, 8, 12, 13 }, shakeFrame = 7, sound = roarClip, soundFrame = 0, cooldown = 30f, maxRange = 99f,
        };
        brain.moves = new[]
        {
            // pusku paikalta: kyykky, syöksy (kuvat 4-8), kaataa
            new AlienBrain.Move { name = "pusku", sprites = Seq(s["pusku"], 0, 0, 1, 1, 2, 3, 3, 3, 4, 5, 5), frameTime = F, windows = new[] { 4, 8 }, damage = 18, knockFromWindow = 0, reach = 2.8f,
                minRange = 0.8f, maxRange = 4.2f, weight = 1.3f, cooldown = 2.2f, armor = true, moveFrom = 4, moveTo = 8, moveSpeed = 13f },
            // pusku juoksusta: pitkä syöksy kohti kohdetta, pysähtyy osumaan
            new AlienBrain.Move { name = "puskuJuoksu", sprites = Seq(s["pusku"], 0, 1, 2, 3, 3, 3, 3, 3, 3, 3, 3, 4, 5), frameTime = F, windows = new[] { 3, 10 }, damage = 24, knockFromWindow = 0, reach = 2.8f,
                minRange = 5f, maxRange = 13f, weight = 1.2f, cooldown = 4f, armor = true, moveFrom = 3, moveTo = 10, moveSpeed = 22f, stopOnHit = true },
            // loikka: ponnistus, lento kohteen päälle ja laskeutuminen tallaa maahan (kuvat 19-21)
            new AlienBrain.Move { name = "loikka", sprites = SeqRange(s["loikka"], 2, 27), frameTime = F, windows = new[] { 19, 21 }, damage = 30, knockFromWindow = 0, reach = 2.6f, allAround = true,
                minRange = 3.2f, maxRange = 9.5f, weight = 1.1f, cooldown = 5f, armor = true, moveFrom = 3, moveTo = 18, moveSpeed = 13f, homing = true, shakeFrame = 19 },
            // häntähuitaisu: osuu molemmin puolin, toinen osuma kaataa
            new AlienBrain.Move { name = "hantahuitaisu", sprites = s["hantahuitaisu"], frameTime = 0.04f, windows = new[] { 8, 10, 11, 15 }, windowDamage = new[] { 12, 20 }, knockFromWindow = 1, reach = 3.8f, allAround = true,
                minRange = 0f, maxRange = 3.8f, weight = 1.2f, cooldown = 3.5f, armor = true },
            // raapiminen: nousee takajaloille ja raapii neljästi, viimeinen kaataa
            new AlienBrain.Move { name = "raapiminen", sprites = s["raapiminen"], frameTime = F, windows = new[] { 4, 5, 8, 10, 13, 14, 17, 18 }, windowDamage = new[] { 9, 9, 9, 14 }, knockFromWindow = 3, reach = 3.2f,
                minRange = 0f, maxRange = 3.3f, weight = 1.5f, cooldown = 1.6f, armor = true, moveFrom = 2, moveTo = 18, moveSpeed = 2.2f },
        };
        // puraisu (valinnainen kuvasarja alien_puraisu.png): tarttuu hampailla, ravistaa ja heittää. Kuvaindeksit suhteessa pituuteen: tartunta 25 %, heitto 75 %; säädä kun sarja on valmis
        var biteSheet = LoadWalkerSprites("alien_puraisu");
        if (biteSheet != null && biteSheet.Length >= 6)
        {
            int nb = biteSheet.Length, gf = nb == 40 ? 5 : Mathf.RoundToInt(nb * 0.25f), rf = nb == 40 ? 33 : Mathf.RoundToInt(nb * 0.75f);   // 40 kuvaa: leuat sulkeutuvat kuvassa 5, ravistus 6-32, heitto kuvassa 33
            var list = brain.moves.ToList();
            list.Add(new AlienBrain.Move { name = "puraisu", sprites = biteSheet, frameTime = F, grabFrame = gf, releaseFrame = rf, reach = 2.6f, minRange = 0f, maxRange = 3.2f, weight = 1.4f, cooldown = 6f, armor = true,
                moveFrom = 2, moveTo = gf, moveSpeed = 7f, mouthDx = 2.4f, mouthLift = 1.2f, tickEvery = 0.3f, tickDamage = 4, throwSpeed = 12f, throwUp = 6f, throwDamage = 22 });
            brain.moves = list.ToArray();
        }
        go.transform.position = pos;
        EditorUtility.SetDirty(e); EditorUtility.SetDirty(brain);
        return e;
    }

    [MenuItem("Beat em up/332. Koe-olento (alien-pomo): laboratorioon (tai kuun pinnan päähän)", false, 332)]
    static void AddAlienToMoon()
    {
        if (PlayModeBlocked()) return;
        var sheets = LoadAlienSheets(out string missing);
        if (sheets == null) { Info("Puuttuu tai ei leikkaudu: " + missing + " (Assets/Sprites/Viholliset). Aja Tools/AlienAssets/make_alien_sheets*.py."); return; }
        if (!File.Exists(WallScenePath)) { Info("Aja ensin valikko 327 (Kiinan muuri)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(WallScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(WallScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var surf = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Muurin kuun pinta" && a.gameObject.scene == scene);
            if (surf == null) { Info("Kuun pinnan aluetta ei löytynyt: aja valikko 327."); return; }
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Koe-olento (kuu)").ToArray()) Undo.DestroyObjectImmediate(old);
            var root = new GameObject("Koe-olento (kuu)");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Koe-olento");
            float ppu = 941f / (2f * CamHalf);
            // paikka: laboratorion toinen ruutu, jos laboratorio on tehty (valikko 327); muuten kuun pinnan vasen pää (tukikohdan oven edessä)
            var labArea = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Muurin laboratorio" && a.gameObject.scene == scene);
            var spawnArea = labArea != null ? labArea : surf;
            float x = labArea != null ? WallLabX0 + 3200f / ppu : WallSurfX0 + (SurfBasePx + 450f) / ppu;
            float yMax = spawnArea.MaxDepthAtX(x, spawnArea.maxDepthY), yMin = spawnArea.MinDepthAtX(x, spawnArea.minDepthY);
            var pos = new Vector3(x, Mathf.Clamp(Mathf.Lerp(yMax, yMin, 0.5f), yMin + 0.05f, yMax - 0.05f), 0f);
            MakeAlien(root.transform, pos, sheets);
            int robots = 0;
            foreach (var rr in scene.GetRootGameObjects().Where(g => g.name == "Kuurobotit"))
                foreach (var en in rr.GetComponentsInChildren<Enemy>(true)) { en.huntsBrawlers = true; EditorUtility.SetDirty(en); robots++; }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Koe-olento (650 elämää) lisätty " + (labArea != null ? "laboratorion toiseen ruutuun" : "kuun pinnan vasempaan päähän") + ". Nopea ja aggressiivinen: käy kaikkien (herojen ja vihujen) kimppuun. Liikkeet: raapaisu, raapiminen, pusku (paikalta ja juoksusta), loikka (laskeutuminen tallaa), häntähuitaisu ja karjaisu (sisääntulo, 50 % ja 25 %). " +
                 robots + " kuurobottia käy sen kimppuun.\\nÄänet: vain karjaisu (Resources/Sfx/Alien/karjaisu.wav); kipu/isku/nousu-äänet tulevat, kun tiedostot pain*, hit*, fall*, nousu* lisätään samaan kansioon.\nAjamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
