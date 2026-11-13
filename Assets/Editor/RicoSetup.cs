using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Vankilan vanki Rico (valikko 368). Kuvat Assets/Sprites/Viholliset/rico_*.png (Tools/PrisonAssets/make_rico_sheets.py).
/// Iskut: J jabi, S takasuora, K koukku (kaataa); alt-iskut (numero kombossa): 0 etupotku, 1 roundhouse (kaataa). Osuma, kaatuminen, nousu ja suojaus mukana.
/// Vangit ja vartijat tappelevat keskenään (fightsEveryone, brawlGroup "Vangit"); sijoitus FreeSpot-apurilla ettei mikään ole päällekkäin.</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/368. Vankila: Rico (jabi, takasuora, koukku, etupotku, roundhouse)", false, 368)]
    static void AddPrisonRico()
    {
        if (PlayModeBlocked()) return;
        var s = new Dictionary<string, Sprite[]>();
        foreach (var n in new[] { "idle", "kavely", "juoksu", "isku1", "isku2", "isku3", "potku1", "potku2", "osuma", "kaatuminen", "nousu", "suojaus" })
        {
            var sp = LoadWalkerSprites("rico_" + n);
            if (sp == null || sp.Length == 0) { Info("Puuttuu tai ei leikkaudu: rico_" + n + ".png (Assets/Sprites/Viholliset). Aja Tools/PrisonAssets/make_rico_sheets.py."); return; }
            s[n] = sp;
        }
        if (!File.Exists(PrisonScenePath)) { Info("Aja ensin valikot 353–363."); return; }
        var scene = EditorSceneManager.GetSceneByPath(PrisonScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(PrisonScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var areas = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == scene).ToArray();
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Vankilan Ricot").ToArray()) Undo.DestroyObjectImmediate(old);
            var root = new GameObject("Vankilan Ricot");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Vankilan Ricot");
            int count = 0;
            const float F = 0.045f;
            var gasp = LoadClips("Assets/Audio/big thug", "gasp");

            Enemy MakeRico(Transform parent, Vector3 pos)
            {
                var go = new GameObject("Rico " + (++count));
                go.transform.SetParent(parent, false);
                var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
                var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
                var e = go.AddComponent<Enemy>();
                e.body = visual; e.shadow = shadow; visual.sprite = s["idle"][0];
                e.displayName = "Rico"; e.bigBody = true;   // iso vihu: Rocco heittää kuperkeikalla
                e.visualScale = RicoScale; e.attackArtScale = 1f; e.knockArtScale = RicoKnockScale; e.walkArtScale = 1f;
                e.fightsEveryone = true; e.brawlGroup = "Vangit"; e.huntsBrawlers = false;   // vangit käyvät kaikkien kimppuun (hero, vartijat), eivät toisiaan
                e.idleSprites = s["idle"]; e.idleFrameTime = 1f / 12f;
                e.walkSprites = s["kavely"]; e.walkFrameTime = 1f / 12f;
                e.runSprites = s["juoksu"]; e.runFrameTime = 1f / 24f; e.runSpeedMultiplier = 1.8f; e.runDistance = 6f;
                e.maxHealth = 130; e.moveSpeedX = 3.0f; e.moveSpeedY = 2.4f; e.depthTolerance = 0.4f; e.attackCooldown = 0.7f; e.wakeDistance = 12f;
                e.comboChance = 0.55f; e.comboWindupScale = 0.6f; e.comboRecoverFraction = 0.6f; e.attackRange = 2.3f;
                // J = jabi (osuma kuvassa 4), S = takasuora (osuma 4), K = koukku (osuma 5, kaataa)
                e.punchSprites = s["isku1"]; e.punchImpactFrame = 3; e.windupTime = 3 * F; e.punchRecoverTime = (s["isku1"].Length - 3) * F; e.punchActiveTime = 0.12f; e.punchDamage = 9;
                e.punch2Sprites = s["isku2"]; e.punch2ImpactFrame = 3; e.punch2Damage = 12; e.punch2Knockdown = false; e.punch2Chance = 0.35f;
                e.punch3Sprites = s["isku3"]; e.punch3ImpactFrame = 4; e.punch3Damage = 15; e.punch3Knockdown = true; e.punch3Chance = 0.25f; e.punch3IsPunch = true;
                e.punch3WindupTime = 4 * F; e.punch3RecoverTime = (s["isku3"].Length - 4) * F; e.punch3Reach = 2.6f;
                // alt-iskut: 0 etupotku (osuma 5), 1 roundhouse (osuma 5, kaataa)
                e.altAttackSprites = s["potku1"]; e.altImpactFrame = 4; e.altAnimationFrameTime = F; e.altDamage = 14; e.altReach = 3.2f;
                e.altChance = 0.35f;
                e.altVariants = new[]
                {
                    new Enemy.AltVariant { name = "Etupotku", sprites = s["potku1"], impactFrame = 4, frameTime = F, damage = 14, reach = 3.3f, knockdown = false, weight = 1f, lungeSpeed = 0f },
                    new Enemy.AltVariant { name = "Roundhouse", sprites = s["potku2"], impactFrame = 4, frameTime = F, damage = 17, reach = 3.2f, knockdown = true, knockSpeed = 7f, knockUp = 4f, weight = 0.8f, lungeSpeed = 0f },
                };
                // numero = alt-variantti: 0 etupotku, 1 roundhouse
                e.combos = new[] { "JS", "JSK", "JK", "J", "S0", "JS1", "K1" };
                e.hurtSprites = s["osuma"]; e.hurtTime = 0.3f;
                e.knockdownSprites = s["kaatuminen"]; e.airFrameTime = 0.1f; e.thrownStartFrames = new[] { 0, 1, 2 }; e.thrownLoopFrames = new[] { 3, 4, 5 }; e.thrownLandFrames = new[] { 6, 7, 8, 9 }; e.thrownFrameTime = 0.11f; e.getUpSprites = s["nousu"]; e.getUpTime = 1.0f;   // heitettynä: pyörähtää ilmassa (kuten vartija)
                e.hurtSounds = gasp; e.hurtVolume = 0.95f;
                e.blockSprites = s["suojaus"].Skip(1).ToArray(); e.blockChance = 0.25f;   // suojaus: kuvat 2–5
                go.transform.position = pos;
                EditorUtility.SetDirty(e);
                return e;
            }

            // (alue, osuudet kävelyalueen leveydestä, syvyys): Ricot D- ja E-osastolla, ruokalassa, työpajassa ja kuntopihalla
            var plan = new (string area, float[] fr, float[] depth)[]
            {
                ("Vankilan D-osasto", new[] { 0.58f }, new[] { 0.5f }),
                ("Vankilan E-osasto", new[] { 0.58f }, new[] { 0.5f }),
                ("Vankilan ruokala", new[] { 0.42f, 0.65f }, new[] { 0.4f, 0.55f }),
                ("Vankilan työpaja", new[] { 0.35f }, new[] { 0.5f }),
                ("Vankilan kuntopiha", new[] { 0.55f }, new[] { 0.5f }),
            };
            var report = new List<string>();
            foreach (var (an, fr, depth) in plan)
            {
                var a = areas.FirstOrDefault(x => x.areaName == an);
                if (a == null) { report.Add(an + ": ei löytynyt"); continue; }
                var grp = new GameObject("Ricot: " + an); grp.transform.SetParent(root.transform, false);
                for (int i = 0; i < fr.Length; i++)
                {
                    float x = Mathf.Lerp(a.walkMinX, a.walkMaxX, fr[i]);
                    float yMax = a.MaxDepthAtX(x, a.maxDepthY), yMin = a.MinDepthAtX(x, a.minDepthY);
                    float y = Mathf.Clamp(Mathf.Lerp(yMax, yMin, depth[i]), yMin + 0.05f, yMax - 0.05f);
                    MakeRico(grp.transform, FreeSpot(a, scene, new Vector3(x, y, 0f)));   // ei päällekkäin laatikoiden, pöytien eikä toisten vihujen kanssa
                }
                report.Add(an + " " + fr.Length);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info(count + " Ricoa (130 elämää) lisätty: " + string.Join(", ", report) + ".\nIskut: jabi, takasuora, koukku (kaataa); potkut: etupotku, roundhouse (kaataa). Kombot JS, JSK, JK, J, S0, JS1, K1. Osuma, kaatuminen ja nousu mukana.\nPuuttuu: suojaus, omat äänet (nyt big thug gasp). Vangit tappelevat vartijoiden kanssa. Ajamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
