using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Nokkamies (Foreman, valikko 395): työmaakonttorin yläkerran toimiston pomo (Vegas, alue "Työmaakonttorin toimisto"). Kuvat Assets/Sprites/Viholliset/nokkamies_*.png (Tools/NokkamiesAssets/make_nokkamies_sheets.py).
/// Vaikeus: Warden/Timppa-tasoa (1000 elämää, torjunta 50 %, max 5 peräkkäin, nopea). Liikkeet: suorat J/S/K (K = kovin suora, kaataa), kombot, etupotku P ja suora+korkea potku D,
/// MongolBrainin erikoisliikkeet: syöksy (juoksusta, liukuu nyrkki ojossa, kaataa), iskusyöksy (loikka lähietäisyydeltä), takapotku ja kolmas suora. Äänet väliaikaisesti Gestapon (Enemy.cs).
/// Aja ensin valikko 394 (yläkerta).
/// </summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/395. Las Vegas: Nokkamies (foreman) yläkertaan (suorat, potkut, syöksyt, torjunta)", false, 395)]
    static void AddNokkamies()
    {
        if (PlayModeBlocked()) return;
        var sheets = new Dictionary<string, Sprite[]>();
        foreach (var n in new[] { "idle", "kavely", "juoksu", "suora1", "suora2", "suora3", "suora4", "potku1", "potku2", "potku3", "syoksy", "syoksy2", "osuma", "kaatuminen", "maahan", "nousu", "suoja" })
        {
            var sp = LoadWalkerSprites("nokkamies_" + n);
            if (sp == null || sp.Length == 0) { Info("Puuttuu tai ei leikkaudu: nokkamies_" + n + ".png (Assets/Sprites/Viholliset). Aja Tools/NokkamiesAssets/make_nokkamies_sheets.py."); return; }
            sheets[n] = sp;
        }
        if (!File.Exists(VegasScenePath)) { Info("Aja ensin valikko 376 (Vegas)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var room = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a != null && a.gameObject.scene == scene && a.areaName == "Työmaakonttorin toimisto");
            if (room == null) { Info("Aluetta \"Työmaakonttorin toimisto\" ei löytynyt: aja valikko 394."); return; }
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Nokkamies").ToArray()) Undo.DestroyObjectImmediate(old);
            var go = new GameObject("Nokkamies");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, "Nokkamies");
            var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
            var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
            var e = go.AddComponent<Enemy>();
            const float F = 1f / 14f;   // iskukuvien kuva-aika (kuvat videosta joka toinen ruutu 24 fps -> 12 fps, hieman nopeammin)
            e.body = visual; e.shadow = shadow; visual.sprite = sheets["idle"][0];
            e.displayName = "Nokkamies"; e.isBoss = true;
            e.visualScale = 1f; e.attackArtScale = 1f; e.knockArtScale = 1f; e.walkArtScale = 1f;
            e.idleSprites = sheets["idle"]; e.idleFrameTime = 1f / 12f;
            e.walkSprites = sheets["kavely"]; e.walkFrameTime = 1f / 12f;
            e.runSprites = sheets["juoksu"]; e.runFrameTime = 1f / 12f; e.runSpeedMultiplier = 2.0f; e.runDistance = 5f;
            e.maxHealth = 1000; e.moveSpeedX = 3.3f; e.moveSpeedY = 2.4f; e.depthTolerance = 0.45f; e.attackRange = 2.3f; e.attackCooldown = 0.5f; e.wakeDistance = 18f;
            // suorat: J = suora1 (osuma kuvassa 3), S = suora2 (kuvassa 2), K = suora4 (kuvassa 2, kaataa)
            var j = sheets["suora1"]; var s2 = sheets["suora2"]; var hk = sheets["suora4"];
            e.punchSprites = j; e.punchImpactFrame = 3; e.windupTime = 3 * F; e.punchRecoverTime = (j.Length - 3) * F; e.punchDamage = 17; e.punchActiveTime = 0.17f;
            e.punch2Sprites = s2; e.punch2ImpactFrame = 2; e.punch2Damage = 19; e.punch2Knockdown = false; e.punch2Chance = 0.45f;
            e.punch3Sprites = hk; e.punch3ImpactFrame = 2; e.punch3Damage = 28; e.punch3Knockdown = true; e.punch3Chance = 0.3f; e.punch3WindupTime = 0.28f; e.punch3RecoverTime = 0.35f; e.punch3Reach = 3.0f;
            // potkut kombojen osina: P = etupotku (alt, ei kaada), D = suora + korkea potku (kaataa; potku kuvissa 6-8)
            e.altAttackSprites = sheets["potku1"]; e.altImpactFrame = 3; e.altDamage = 20; e.altChance = 0f; e.altExtraWindup = 0.05f; e.altReach = 2.9f; e.altAnimationFrameTime = F; e.altKnockdown = false;
            e.doubleKickSprites = sheets["potku3"]; e.doubleKickImpactFrame = 7; e.doubleKickDamage = 26; e.doubleKickFrameTime = F; e.doubleKickReach = 3.1f; e.doubleKickLaunchX = 12f; e.doubleKickLaunchUp = 6f; e.doubleKickChance = 0f;
            e.combos = new[] { "JS", "JSK", "JJ", "JSP", "JSPD", "JJSPK", "JSJSPD" }; e.comboChance = 0.75f; e.comboArmor = true; e.comboArmorMinLength = 6;
            // torjunta: kädet ylös ja kasvot peittoon (suojakuvat 0-3)
            var g = sheets["suoja"];
            e.blockSprites = new[] { g[0], g[1], g[2], g[3], g[2], g[1] };
            e.blockTime = 0.45f; e.blockChance = 0.5f; e.maxBlocksInRow = 5; e.guardWhenHurt = true;
            e.hurtSprites = sheets["osuma"]; e.hurtTime = 0.3f;
            e.knockdownSprites = sheets["kaatuminen"]; e.airFrameTime = 0.05f;
            e.landSprites = sheets["maahan"]; e.landFrameTime = 0.09f;
            e.getUpSprites = sheets["nousu"]; e.getUpTime = sheets["nousu"].Length / 14f;
            // erikoisliikkeet (MongolBrain)
            var brain = go.AddComponent<MongolBrain>();
            brain.globalCooldown = 1.1f;
            int syN = sheets["syoksy"].Length, sy2N = sheets["syoksy2"].Length;
            brain.moves = new[]
            {
                // syöksy (syöksylyöntivideo): kuvat 0-12 vauhdinotto, 13-20 syöksy, 21-27 nyrkki ojossa, 28- toipuminen; osumaikkuna 14-25, liuku 13-22
                new MongolBrain.Move { name = "syöksy", sprites = sheets["syoksy"], frameTime = 0.055f, hitFrom = 14, hitTo = Mathf.Min(25, syN - 1), reach = 2.9f, minRange = 4.5f, maxRange = 12f, weight = 1.4f, cooldown = 4.5f, damage = 28, knockdown = true, moveFrom = 13, moveTo = 22, moveSpeed = 10f },
                // iskusyöksy (hyökkäysvideon loppu): käsien levitys, loikka eteen ja suora (osuma kuvissa 5-9)
                new MongolBrain.Move { name = "iskusyöksy", sprites = sheets["syoksy2"], frameTime = 0.06f, hitFrom = 5, hitTo = Mathf.Min(9, sy2N - 1), reach = 2.8f, minRange = 3f, maxRange = 8f, weight = 1.1f, cooldown = 3.8f, damage = 24, knockdown = true, moveFrom = 4, moveTo = 10, moveSpeed = 9f },
                // takapotku (kuvat 78-106, osuma 4-5) ja kolmas suora lähietäisyydellä
                new MongolBrain.Move { name = "takapotku", sprites = sheets["potku2"], frameTime = 0.06f, hitFrom = 4, hitTo = 5, reach = 3.0f, minRange = 1.3f, maxRange = 3.3f, weight = 0.9f, cooldown = 3f, damage = 22, knockdown = true },
                new MongolBrain.Move { name = "suora3", sprites = sheets["suora3"], frameTime = 0.06f, hitFrom = 2, hitTo = 3, reach = 2.4f, minRange = 1.2f, maxRange = 2.6f, weight = 1f, cooldown = 2f, damage = 17, knockdown = false, impactHold = 0.15f },
            };
            float x = Mathf.Lerp(room.walkMinX, room.walkMaxX, 0.42f), y = Mathf.Lerp(room.maxDepthY, room.minDepthY, 0.5f);
            go.transform.position = new Vector3(x, y, 0f);
            EditorUtility.SetDirty(e); EditorUtility.SetDirty(brain);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Nokkamies lisätty työmaakonttorin yläkertaan (pomo, 1000 elämää): suorat J/S/K (K kaataa), kombot, etupotku, suora + korkea potku, syöksy juoksusta, iskusyöksy, takapotku, torjunta 50 % (max 5 peräkkäin). Äänet väliaikaisesti Gestapon.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    /// <summary>Nokkamies (valikko 395).</summary>
    public static void RunNokkamiesBatch()
    {
        batch = true;
        try { Step("395 Nokkamies", AddNokkamies); }
        finally { batch = false; }
    }
}
