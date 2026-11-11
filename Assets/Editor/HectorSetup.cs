using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Vankilan vangit: Hector (valikko 366). Kuvat Assets/Sprites/Viholliset/hector_*.png (Tools/PrisonAssets/make_hector_sheets.py ja make_hector_fight.py).
/// Iskut: J jabi, S toinen jabi, K suora; alt-iskut (numero kombossa): 0 korkea sivupotku, 1 etupotku, 2 hyppypotku (syöksy), 3 liukutaklaus (kaataa).
/// Kaikki liikkeet mukana.</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/366. Vankila: vangit (Hector: jabit, suora, potkut, hyppypotku, liukutaklaus)", false, 366)]
    static void AddPrisonInmates()
    {
        if (PlayModeBlocked()) return;
        var s = new Dictionary<string, Sprite[]>();
        foreach (var n in new[] { "idle", "kavely", "juoksu", "isku1", "isku2", "isku3", "potku1", "potku2", "hyppypotku", "liukutaklaus", "osuma", "suojaus", "kaatuminen", "nousu" })
        {
            var sp = LoadWalkerSprites("hector_" + n);
            if (sp == null || sp.Length == 0) { Info("Puuttuu tai ei leikkaudu: hector_" + n + ".png (Assets/Sprites/Viholliset). Aja Tools/PrisonAssets/make_hector_*.py."); return; }
            s[n] = sp;
        }
        if (!File.Exists(PrisonScenePath)) { Info("Aja ensin valikot 353–363."); return; }
        var scene = EditorSceneManager.GetSceneByPath(PrisonScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(PrisonScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var areas = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == scene).ToArray();
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Vankilan vangit").ToArray()) Undo.DestroyObjectImmediate(old);
            var root = new GameObject("Vankilan vangit");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Vankilan vangit");
            int count = 0;
            const float F = 0.045f;
            var gasp = LoadClips("Assets/Audio/big thug", "gasp");

            Enemy MakeHector(Transform parent, Vector3 pos)
            {
                var go = new GameObject("Hector " + (++count));
                go.transform.SetParent(parent, false);
                var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
                var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
                var e = go.AddComponent<Enemy>();
                e.body = visual; e.shadow = shadow; visual.sprite = s["idle"][0];
                e.displayName = "Hector";
                e.visualScale = 1f; e.attackArtScale = 1f; e.knockArtScale = 1f; e.walkArtScale = 1f;
                e.idleSprites = s["idle"]; e.idleFrameTime = 1f / 12f;
                e.walkSprites = s["kavely"]; e.walkFrameTime = 1f / 12f;
                e.runSprites = s["juoksu"]; e.runFrameTime = 1f / 24f; e.runSpeedMultiplier = 1.8f; e.runDistance = 6f;
                e.fightsEveryone = true; e.brawlGroup = "Vangit"; e.huntsBrawlers = false;   // vangit käyvät kaikkien kimppuun (hero, vartijat), eivät toisiaan
                e.maxHealth = 110; e.moveSpeedX = 3.1f; e.moveSpeedY = 2.4f; e.depthTolerance = 0.4f; e.attackCooldown = 0.7f; e.wakeDistance = 12f;
                e.comboChance = 0.55f; e.comboWindupScale = 0.6f; e.comboRecoverFraction = 0.6f; e.attackRange = 2.3f;
                // J = jabi (osuma kuvassa 4), S = toinen jabi, K = suora
                e.punchSprites = s["isku1"]; e.punchImpactFrame = 3; e.windupTime = 3 * F; e.punchRecoverTime = (s["isku1"].Length - 3) * F; e.punchActiveTime = 0.12f; e.punchDamage = 8;
                e.punch2Sprites = s["isku2"]; e.punch2ImpactFrame = 3; e.punch2Damage = 9; e.punch2Knockdown = false; e.punch2Chance = 0.35f;
                e.punch3Sprites = s["isku3"]; e.punch3ImpactFrame = 3; e.punch3Damage = 12; e.punch3Knockdown = false; e.punch3Chance = 0.25f;
                e.punch3WindupTime = 3 * F; e.punch3RecoverTime = (s["isku3"].Length - 3) * F; e.punch3Reach = 2.6f;
                // alt-iskut: 0 korkea sivupotku, 1 etupotku, 2 hyppypotku (syöksy), 3 liukutaklaus (kaataa, pitkä liuku)
                e.altAttackSprites = s["potku1"]; e.altImpactFrame = 5; e.altAnimationFrameTime = F; e.altDamage = 13; e.altReach = 3.2f;
                e.altChance = 0.35f;
                e.altVariants = new[]
                {
                    new Enemy.AltVariant { name = "Korkea sivupotku", sprites = s["potku1"], impactFrame = 5, frameTime = F, damage = 13, reach = 3.2f, knockdown = false, weight = 1f, lungeSpeed = 0f },
                    new Enemy.AltVariant { name = "Etupotku", sprites = s["potku2"], impactFrame = 2, frameTime = F, damage = 12, reach = 3.0f, knockdown = false, weight = 1f, lungeSpeed = 0f },
                    new Enemy.AltVariant { name = "Hyppypotku", sprites = s["hyppypotku"], impactFrame = 7, frameTime = F, damage = 18, reach = 3.2f, knockdown = true, knockSpeed = 7f, knockUp = 5f, weight = 0.7f, lungeSpeed = 9f, lungeTime = 0.45f, jumpHeight = 0f },
                    new Enemy.AltVariant { name = "Liukutaklaus", sprites = s["liukutaklaus"], impactFrame = 5, frameTime = F, damage = 16, reach = 2.4f, knockdown = true, knockSpeed = 8f, knockUp = 5f, weight = 0.5f, lungeSpeed = 12f, lungeTime = 0.55f, jumpHeight = 0f },
                };
                // numero = alt-variantti: 0 sivupotku, 1 etupotku, 2 hyppypotku, 3 liukutaklaus
                e.combos = new[] { "JS", "JK", "JSK", "J", "JS0", "JK1", "S2" };
                e.hurtSounds = gasp; e.hurtVolume = 0.95f;
                // suojaus: kuvat 1–5 jakautuvat suojausajalle; osuma: kuvat 1–3 (pää taakse); kaatuminen: 4 kuvaa (ilmassa 3, viimeinen makuu); nousu: 8 kuvaa
                e.blockSprites = s["suojaus"].Skip(1).ToArray(); e.blockChance = 0.25f;
                e.hurtSprites = s["osuma"].Skip(1).ToArray(); e.hurtTime = 0.32f;
                e.knockdownSprites = s["kaatuminen"]; e.airFrameTime = 0.12f; e.getUpSprites = s["nousu"]; e.getUpTime = 0.95f;
                go.transform.position = pos;
                EditorUtility.SetDirty(e);
                return e;
            }

            // (alue, osuudet kävelyalueen leveydestä, syvyys): vangit D- ja E-osastolla (2 + 2), ruokalassa (3), työpajassa (1) ja kuntopihalla (2)
            var plan = new (string area, float[] fr, float[] depth)[]
            {
                ("Vankilan D-osasto", new[] { 0.40f, 0.72f }, new[] { 0.55f, 0.4f }),
                ("Vankilan E-osasto", new[] { 0.42f, 0.74f }, new[] { 0.4f, 0.6f }),
                ("Vankilan ruokala", new[] { 0.30f, 0.52f, 0.78f }, new[] { 0.5f, 0.25f, 0.65f }),
                ("Vankilan työpaja", new[] { 0.55f }, new[] { 0.45f }),
                ("Vankilan kuntopiha", new[] { 0.40f, 0.70f }, new[] { 0.4f, 0.7f }),
            };
            var report = new List<string>();
            foreach (var (an, fr, depth) in plan)
            {
                var a = areas.FirstOrDefault(x => x.areaName == an);
                if (a == null) { report.Add(an + ": ei löytynyt"); continue; }
                var grp = new GameObject("Vangit: " + an); grp.transform.SetParent(root.transform, false);
                for (int i = 0; i < fr.Length; i++)
                {
                    float x = Mathf.Lerp(a.walkMinX, a.walkMaxX, fr[i]);
                    float yMax = a.MaxDepthAtX(x, a.maxDepthY), yMin = a.MinDepthAtX(x, a.minDepthY);
                    float y = Mathf.Clamp(Mathf.Lerp(yMax, yMin, depth[i]), yMin + 0.05f, yMax - 0.05f);
                    MakeHector(grp.transform, FreeSpot(a, scene, new Vector3(x, y, 0f)));   // ei päällekkäin laatikoiden, pöytien, vartijoiden eikä toisten Hectorien kanssa
                }
                report.Add(an + " " + fr.Length);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info(count + " vankia (Hector, 110 elämää) lisätty: " + string.Join(", ", report) + ".\nIskut: jabi, toinen jabi, suora; potkut: korkea sivupotku, etupotku, hyppypotku (syöksy), liukutaklaus (kaataa). Kombot JS, JK, JSK, J, JS0 (jabit + sivupotku), JK1 (jabi + suora + etupotku), S2 (jabi + hyppypotku).\nSuojaus (25 %), osumareaktio, kaatuminen ja nousu mukana.\nPuuttuu: omat äänet (nyt big thug gasp). Ajamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
