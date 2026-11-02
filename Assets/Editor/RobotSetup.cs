using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Kuurobotti (vihollinen): kuvat Assets/Sprites/Viholliset/robotti_*.png (Tools/RobotAssets/make_robot_sheets.py).
/// Valikko 329 lisää heitä Muuri-scenen kuun pinnalle. Käyttää MongolBrainia (liikkeet: toinen potku, hyppypotku, rakettisyöksy).
/// Osuma-, kaatumis-, nousu- ja suojakuvia ei ole vielä: niiden tilalla on väliaikaisesti idle-kuva.</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/329. Kuurobotit: kuun pinnalle", false, 329)]
    static void AddMoonRobots()
    {
        if (PlayModeBlocked()) return;
        var sheets = new Dictionary<string, Sprite[]>();
        foreach (var n in new[] { "idle", "kavely", "juoksu", "lyonti1", "lyonti2", "potku1", "potku2", "hyppypotku", "raketti" })
        {
            var sp = LoadWalkerSprites("robotti_" + n);
            if (sp == null || sp.Length == 0) { Info("Puuttuu tai ei leikkaudu: robotti_" + n + ".png (Assets/Sprites/Viholliset). Aja Tools/RobotAssets/make_robot_sheets.py."); return; }
            sheets[n] = sp;
        }
        // valinnaiset (tulossa): osuma, kaatuminen, maahan, nousu, suoja
        Sprite[] Opt(string n) { var sp = LoadWalkerSprites("robotti_" + n); return sp != null && sp.Length > 0 ? sp : null; }
        var hit = Opt("osuma"); var fall = Opt("kaatuminen"); var down = Opt("maahan"); var getUp = Opt("nousu"); var guard = Opt("suoja"); var damaged = Opt("idle_vaurio");
        if (!File.Exists(WallScenePath)) { Info("Aja ensin valikko 327 (Kiinan muuri)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(WallScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(WallScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var surf = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Muurin kuun pinta" && a.gameObject.scene == scene);
            if (surf == null) { Info("Kuun pinnan aluetta ei löytynyt: aja valikko 327."); return; }
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Kuurobotit").ToArray()) Undo.DestroyObjectImmediate(old);
            var root = new GameObject("Kuurobotit");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Kuurobotit");
            float ppu = 941f / (2f * CamHalf);
            const float F = 1f / 30f;
            int count = 0;

            Enemy MakeRobot(Transform parent, Vector3 pos)
            {
                var go = new GameObject("Kuurobotti " + (++count));
                go.transform.SetParent(parent, false);
                var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
                var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
                var e = go.AddComponent<Enemy>();
                e.body = visual; e.shadow = shadow; visual.sprite = sheets["idle"][0];
                e.displayName = "Kuurobotti";
                e.visualScale = 1f; e.attackArtScale = 1f; e.knockArtScale = 1f; e.walkArtScale = 1f;
                e.idleSprites = sheets["idle"]; e.idleFrameTime = 0.09f;
                e.walkSprites = sheets["kavely"]; e.walkFrameTime = 0.08f;
                e.runSprites = sheets["juoksu"]; e.runFrameTime = 0.07f; e.runSpeedMultiplier = 1.9f; e.runDistance = 6f;
                e.maxHealth = 150; e.moveSpeedX = 3.6f; e.moveSpeedY = 2.6f; e.attackRange = 2.3f; e.attackCooldown = 0.7f; e.wakeDistance = 14f;
                // lyönnit J, S ja ensimmäinen potku K
                var j = sheets["lyonti1"]; var s = sheets["lyonti2"]; var k = sheets["potku1"];
                e.punchSprites = j; e.punchImpactFrame = 3; e.windupTime = 3 * F * 2f; e.punchRecoverTime = (j.Length - 3) * F * 2f; e.punchDamage = 12;
                e.punch2Sprites = s; e.punch2ImpactFrame = 3; e.punch2Damage = 14; e.punch2Knockdown = false; e.punch2Chance = 0.4f;
                e.punch3Sprites = k; e.punch3ImpactFrame = 6; e.punch3Damage = 20; e.punch3Knockdown = true; e.punch3Chance = 0.3f; e.punch3WindupTime = 0.2f; e.punch3RecoverTime = 0.25f; e.punch3Reach = 3.0f;
                e.combos = new[] { "JS", "JSK", "J" }; e.comboChance = 0.6f; e.comboWindupScale = 0.6f; e.comboRecoverFraction = 0.6f;
                if (guard != null) { e.blockSprites = guard; e.blockChance = 0.3f; e.maxBlocksInRow = 2; e.blockTime = 0.4f; }
                // osuma/kaatuminen: kuvat tulossa; siihen asti idle-kuva
                e.hurtSprites = hit ?? new[] { sheets["idle"][0] }; e.hurtTime = 0.3f;
                e.knockdownSprites = fall ?? new[] { sheets["idle"][0] }; e.airFrameTime = 0.05f;
                e.landSprites = down ?? new[] { sheets["idle"][0] }; e.landFrameTime = 0.07f;
                e.getUpSprites = getUp ?? new[] { sheets["idle"][0] }; e.getUpTime = getUp != null ? getUp.Length / 15f : 0.5f;
                if (damaged != null) { e.damagedIdleSprites = damaged; e.damagedFraction = 0.3f; e.damagedIdleFrameTime = 0.1f; }   // alle 30 %: oma idle + savu + glitch-puheet
                var brain = go.AddComponent<MongolBrain>();
                brain.moves = new[]
                {
                    new MongolBrain.Move { name = "potku2", sprites = sheets["potku2"], frameTime = 0.05f, hitFrom = 4, hitTo = 8, reach = 3.2f, minRange = 1.8f, maxRange = 3.6f, weight = 1f, cooldown = 3f, damage = 20 },
                    new MongolBrain.Move { name = "hyppypotku", sprites = sheets["hyppypotku"], frameTime = 0.07f, hitFrom = 7, hitTo = 16, reach = 2.6f, minRange = 3.5f, maxRange = 9f, weight = 1f, cooldown = 5f, damage = 26, knockdown = true, moveFrom = 5, moveTo = 16, moveSpeed = 6f },
                    // rakettisyöksy: valmistelu (kuvat 0-4), sitten heti täysi nopeus lentokuvien ajan (5-15); isku kaataa
                    new MongolBrain.Move { name = "rakettisyöksy", sprites = sheets["raketti"], frameTime = 0.065f, hitFrom = 5, hitTo = 15, reach = 2.6f, minRange = 4f, maxRange = 14f, weight = 1.2f, cooldown = 6f, damage = 30, knockdown = true, moveFrom = 5, moveTo = 15, moveSpeed = 13f },
                };
                go.transform.position = pos;
                EditorUtility.SetDirty(e); EditorUtility.SetDirty(brain);
                return e;
            }
            Vector3 At(Area a, float x0, float px, float depth)
            {
                float x = x0 + px / ppu; float yMax = a.MaxDepthAtX(x, a.maxDepthY), yMin = a.MinDepthAtX(x, a.minDepthY);
                return new Vector3(x, Mathf.Clamp(Mathf.Lerp(yMax, yMin, depth), yMin + 0.05f, yMax - 0.05f), 0f);
            }
            // kuun pinta: ryhmiä oikealta (asemalta) vasemmalle
            foreach (var (px, d) in new[] { (7300f, 0.5f), (6300f, 0.3f), (5600f, 0.7f), (4500f, 0.4f), (3700f, 0.65f), (2700f, 0.35f), (1700f, 0.6f), (800f, 0.45f) })
                MakeRobot(root.transform, At(surf, WallSurfX0, px + SurfBasePx, d));   // SurfBasePx: tukikohdan ulkokuva pinnan vasemmassa päässä
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info(count + " kuurobottia lisätty kuun pinnalle (150 elämää). Liikkeet: lyönnit J, S + potku K kombona, toinen potku, hyppypotku ja rakettisyöksy (lähtee täyteen vauhtiin heti raketit käynnistyessä, kaataa). " +
                 (hit == null ? "Osuma/kaatuminen/nousu/suojakuvat puuttuvat vielä: väliaikaisesti idle-kuva. " : "") + "Äänet: Jakuzan.\nAjamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    [MenuItem("Beat em up/330. Kiinan muuri kerralla (327 muuri, 328 mongolisoturit, 329 kuurobotit)", false, 330)]
    static void BuildWholeWall()
    {
        if (PlayModeBlocked()) return;
        var steps = new (string name, System.Action run)[] { ("327 Kiinan muuri", AddGreatWall), ("328 Mongolisoturit", AddMongols), ("329 Kuurobotit", AddMoonRobots) };
        var summary = new List<string>();
        batch = true; batchLog.Clear();
        try
        {
            foreach (var (name, run) in steps)
            {
                int before = batchLog.Count;
                try { run(); }
                catch (System.Exception ex) { batchLog.Add("VIRHE: " + ex.Message); Debug.LogException(ex); }
                var lines = batchLog.Skip(before).ToList();
                string first = lines.Count > 0 ? lines[0].Split('\n')[0] : "(ei ilmoitusta)";
                summary.Add("- " + name + ": " + (first.Length > 150 ? first.Substring(0, 150) + "..." : first));
            }
        }
        finally { batch = false; }
        Info("Kiinan muuri valmis (kaikki vaiheet ajettu jarjestyksessa). Yhteenveto:\n" + string.Join("\n", summary) + "\n\nTarkemmat ilmoitukset ovat Consolessa (Beat em up: ...).");
    }
}
