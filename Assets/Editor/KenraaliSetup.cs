using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Kenraali (linnakkeen ensimmäinen pomo, valikko 352): kuvat Assets/Sprites/Viholliset/kenraali_*.png (Tools/GeneralAssets/make_general_sheets.py ja make_general_extra.py).
/// Vaikeus kuten Deaconilla (520 elämää, torjunta 50 %, max 5 peräkkäin, torjuu myös osuman jälkeen). Liikkeet: suorat (J, S), heijari (K, kaataa), kolmas suora ja syöksysuora (aivo:
/// loikkaa juoksusta liukuen nyrkki ojossa ja kaataa), torjunta. Äänet väliaikaisesti Gestapon (Enemy.cs). Nurkkapurkaus: aivon (MongolBrain) kaatava liike, eli syöksy.
/// Pomo sijoitetaan pomohuoneeseen "Muurin pomohuone 1" (valikko 351).
/// </summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/352. Linnake: Kenraali pomohuoneeseen (suorat, heijari, syöksysuora, torjunta)", false, 352)]
    static void AddKenraali()
    {
        if (PlayModeBlocked()) return;
        var sheets = new Dictionary<string, Sprite[]>();
        foreach (var n in new[] { "idle", "kavely", "juoksu", "suora1", "suora2", "suora3", "heijari", "syoksy", "potku1", "potku2", "potku3", "sivupotku", "suoja", "osuma", "kaatuminen", "maahan", "nousu" })
        {
            var sp = LoadWalkerSprites("kenraali_" + n);
            if (sp == null || sp.Length == 0) { Info("Puuttuu tai ei leikkaudu: kenraali_" + n + ".png (Assets/Sprites/Viholliset). Aja Tools/GeneralAssets/make_general_sheets.py ja make_general_extra.py."); return; }
            sheets[n] = sp;
        }
        string hallScene = AreaSceneName("Muurin juhlahalli");
        if (hallScene == null) { Info("Linnakkeen scenea ei löytynyt: aja valikko 340/330 ja 351 (pomohuone)."); return; }
        string scenePath = "Assets/Scenes/Areas/" + hallScene + ".unity";
        var scene = EditorSceneManager.GetSceneByPath(scenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var room = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Muurin pomohuone 1" && a.gameObject.scene == scene);
            if (room == null) { Info("Pomohuonetta ei löytynyt: aja ensin valikko 351."); return; }
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Kenraali").ToArray()) Undo.DestroyObjectImmediate(old);
            var go = new GameObject("Kenraali");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, "Kenraali");
            var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
            var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
            var e = go.AddComponent<Enemy>();
            const float F = 0.06f;   // iskukuvien kuva-aika (kuvat on otettu videosta 3 ruudun välein)
            e.body = visual; e.shadow = shadow; visual.sprite = sheets["idle"][0];
            e.displayName = "Kenraali"; e.isBoss = true;
            e.visualScale = 1f; e.attackArtScale = 1f; e.knockArtScale = 1f; e.walkArtScale = 1f;
            e.idleSprites = sheets["idle"]; e.idleFrameTime = 1f / 12f;
            e.walkSprites = sheets["kavely"]; e.walkFrameTime = 1f / 12f;
            e.runSprites = sheets["juoksu"]; e.runFrameTime = 1f / 12f; e.runSpeedMultiplier = 2.1f; e.runDistance = 5f;
            e.maxHealth = 520; e.moveSpeedX = 2.5f; e.moveSpeedY = 1.8f; e.depthTolerance = 0.45f; e.attackRange = 2.2f; e.attackCooldown = 0.7f; e.wakeDistance = 16f;
            // suorat: J = suora1 (osuma kuvassa 4), S = suora2 (kuvassa 2), K = heijari (kuvassa 3, kaataa)
            var j = sheets["suora1"]; var s2 = sheets["suora2"]; var hk = sheets["heijari"];
            e.punchSprites = j; e.punchImpactFrame = 4; e.windupTime = 4 * F; e.punchRecoverTime = (j.Length - 4) * F; e.punchDamage = 14; e.punchActiveTime = 0.17f;   // osumakuva pysyy ruudulla hetken (veto kiihtyy Enemyssä muutenkin)
            e.punch2Sprites = s2; e.punch2ImpactFrame = 2; e.punch2Damage = 16; e.punch2Knockdown = false; e.punch2Chance = 0.45f;
            e.punch3Sprites = hk; e.punch3ImpactFrame = 3; e.punch3Damage = 24; e.punch3Knockdown = true; e.punch3Chance = 0.3f; e.punch3WindupTime = 0.3f; e.punch3RecoverTime = 0.35f; e.punch3Reach = 3.0f;
            // potkut kombojen osina: P = etupotku (alt, ei kaada), D = pyöräytyspotku (kaataa)
            e.altAttackSprites = sheets["potku1"]; e.altImpactFrame = 3; e.altDamage = 17; e.altChance = 0f; e.altExtraWindup = 0.05f; e.altReach = 2.9f; e.altAnimationFrameTime = 0.06f; e.altKnockdown = false;
            e.doubleKickSprites = sheets["potku3"]; e.doubleKickImpactFrame = 8; e.doubleKickDamage = 23; e.doubleKickFrameTime = 0.06f; e.doubleKickReach = 3.0f; e.doubleKickLaunchX = 12f; e.doubleKickLaunchUp = 6f; e.doubleKickChance = 0f;
            // kombot (J suora 1, S suora 2, K heijari, P etupotku, D pyöräytyspotku); pisin kuusi iskua, loppu aina kaatava
            e.combos = new[] { "JS", "JSK", "JJ", "JSP", "JSPD", "JJSPK", "JSJSPD" }; e.comboChance = 0.75f; e.comboArmor = true; e.comboArmorMinLength = 6;   // vain kuuden iskun kombo (JSJSPD) on haarniskassa: Roccon counter ei katkaise sitä e.comboWindupScale = 0.6f; e.comboRecoverFraction = 0.6f;
            // torjunta kuten Deaconilla: kädet ylös ja kasvot peittoon
            var g = sheets["suoja"];
            e.blockSprites = new[] { g[0], g[1], g[2], g[3], g[3], g[2], g[1] };
            e.blockTime = 0.45f; e.blockChance = 0.5f; e.maxBlocksInRow = 5; e.guardWhenHurt = true;
            e.hurtSprites = sheets["osuma"]; e.hurtTime = 0.3f;
            e.knockdownSprites = sheets["kaatuminen"]; e.airFrameTime = 0.05f;
            e.landSprites = sheets["maahan"]; e.landFrameTime = 0.09f;
            e.getUpSprites = sheets["nousu"]; e.getUpTime = sheets["nousu"].Length / 14f;
            // erikoisliikkeet (MongolBrain): syöksysuora juoksusta (liukuu eteen nyrkki ojossa, pölypilvi; kaataa) ja kolmas suora lähietäisyydellä
            var brain = go.AddComponent<MongolBrain>();
            brain.globalCooldown = 1.2f;
            brain.moves = new[]
            {
                // syoksy: kuvat 0 suoja, 1 pölyn alku, 2–9 nyrkki ojossa liukuen; osumaikkuna 2–8, liuku kuvista 1–8
                new MongolBrain.Move { name = "syöksysuora", sprites = sheets["syoksy"], frameTime = 0.07f, hitFrom = 2, hitTo = 8, reach = 2.7f, minRange = 4.5f, maxRange = 12f, weight = 1.4f, cooldown = 4.5f, damage = 26, knockdown = true, moveFrom = 1, moveTo = 8, moveSpeed = 11f },
                // yksittäiset potkut (kuvat videosta 2 välein): korkea potku (osuma kuvassa 3, kaataa) ja sivupotku (2–4); etupotku ja pyöräytyspotku ovat kombojen osia (P, D)
                new MongolBrain.Move { name = "korkea potku", sprites = sheets["potku2"], frameTime = 0.06f, hitFrom = 3, hitTo = 4, reach = 3.0f, minRange = 1.3f, maxRange = 3.3f, weight = 0.9f, cooldown = 3f, damage = 20, knockdown = true },
                new MongolBrain.Move { name = "sivupotku", sprites = sheets["sivupotku"], frameTime = 0.06f, hitFrom = 2, hitTo = 4, reach = 2.8f, minRange = 1.2f, maxRange = 3.1f, weight = 0.8f, cooldown = 2.5f, damage = 15, knockdown = false },
                new MongolBrain.Move { name = "suora3", sprites = sheets["suora3"], frameTime = 0.06f, hitFrom = 2, hitTo = 4, reach = 2.4f, minRange = 1.2f, maxRange = 2.6f, weight = 1f, cooldown = 2f, damage = 15, knockdown = false, impactHold = 0.15f },
            };
            float x = Mathf.Lerp(room.walkMinX, room.walkMaxX, 0.65f), y = Mathf.Lerp(room.maxDepthY, room.minDepthY, 0.45f);
            go.transform.position = new Vector3(x, y, 0f);
            EditorUtility.SetDirty(e); EditorUtility.SetDirty(brain);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Kenraali lisätty pomohuoneeseen (pomo, 520 elämää kuten Deacon): suorat J/S, heijari (K, kaataa), kolmas suora, syöksysuora juoksusta (liukuu, kaataa), torjunta 50 % (max 5 peräkkäin). Nurkkapurkaus: syöksysuora. Äänet väliaikaisesti Gestapon.\nAjamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
