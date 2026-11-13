using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Tankki-Tony (Vegasin 1. vihu, valikko 380). Kuvat Assets/Sprites/Viholliset/tony_*.png (Tools/TonyAssets/make_tony_sheets.py).
/// Iskut: J jabi, S takasuora, K haymaker (kaataa), 0 korkea potku (kaataa), 1 matala potku (viikate, kaataa), U alakoukku; heitto (ote, nosto, ojennus), suojaus, osuma, kaatuminen ja nousu mukana.
/// Raskas ja kestävä, rekkamiehen tasoa (520 elämää, kombohaarniska). Sijoitus Vegasin kadulle ja baariin FreeSpot-apurilla. Aja valikot 376, 378 ja 379 ensin.</summary>
public static partial class BeatEmUpSetup
{
    const float TonyScale = 1.15f, TonyKnockScale = 0.94f;

    [MenuItem("Beat em up/380. Las Vegas: Tankki-Tony (jabi, takasuora, korkea potku, heitto, suojaus)", false, 380)]
    static void AddVegasTony()
    {
        if (PlayModeBlocked()) return;
        var s = new Dictionary<string, Sprite[]>();
        foreach (var n in new[] { "idle", "kavely", "juoksu", "isku1", "isku2", "isku3", "potku", "potku2", "uppercut", "osuma", "kaatuminen", "nousu", "heitto", "suojaus" })
        {
            var sp = LoadWalkerSprites("tony_" + n);
            if (sp == null || sp.Length == 0) { Info("Puuttuu tai ei leikkaudu: tony_" + n + ".png (Assets/Sprites/Viholliset). Aja Tools/TonyAssets/make_tony_sheets.py."); return; }
            s[n] = sp;
        }
        if (!File.Exists(VegasScenePath)) { Info("Aja ensin valikko 376 (Vegas)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var areas = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == scene).ToArray();
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Tankki-Tonyt").ToArray()) Undo.DestroyObjectImmediate(old);
            var root = new GameObject("Tankki-Tonyt");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Tankki-Tonyt");
            int count = 0;
            const float F = 0.045f;
            var gasp = LoadClips("Assets/Audio/big thug", "gasp");

            Enemy MakeTony(Transform parent, Vector3 pos)
            {
                var go = new GameObject("Tankki-Tony " + (++count));
                go.transform.SetParent(parent, false);
                var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
                var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
                var e = go.AddComponent<Enemy>();
                e.body = visual; e.shadow = shadow; visual.sprite = s["idle"][0];
                e.displayName = "Tankki-Tony"; e.bigBody = true;   // iso vihu: Rocco heittää kuperkeikalla
                e.visualScale = TonyScale; e.attackArtScale = 1f; e.knockArtScale = TonyKnockScale; e.walkArtScale = 1f;
                e.idleSprites = s["idle"]; e.idleFrameTime = 1f / 12f;
                e.walkSprites = s["kavely"]; e.walkFrameTime = 1f / 12f;
                e.runSprites = s["juoksu"]; e.runFrameTime = 1f / 24f; e.runSpeedMultiplier = 1.7f; e.runDistance = 6f;
                e.maxHealth = 520; e.moveSpeedX = 2.7f; e.moveSpeedY = 1.8f; e.depthTolerance = 0.45f; e.attackCooldown = 0.5f;   // rekkamiehen taso (520 elämää, sama nopeus ja tahti) e.wakeDistance = 13f;
                e.comboChance = 0.55f; e.comboArmor = true; e.comboWindupScale = 0.65f; e.comboRecoverFraction = 0.65f; e.attackRange = 2.4f;   // kombo alkaa jabista; muut liikkeet satunnaisesti, ja alakoukku, haymaker ja matala potku tulevat usein avauksena yksinään
                // J = jabi (osuma kuvassa 8), S = takasuora (osuma 5)
                e.punchSprites = s["isku1"]; e.punchImpactFrame = 7; e.windupTime = 7 * F; e.punchRecoverTime = (s["isku1"].Length - 7) * F; e.punchActiveTime = 0.12f; e.punchDamage = 11;
                e.punch2Sprites = s["isku2"]; e.punch2ImpactFrame = 4; e.punch2Damage = 15; e.punch2Knockdown = false; e.punch2Chance = 0.45f;
                // K = haymaker (osuma kuvassa 11, kaataa): lataus hidas, kiihtyy iskua kohti (AttackEase), ei kuva-ajan lyhennystä
                e.punch3Sprites = s["isku3"]; e.punch3ImpactFrame = 10; e.punch3Damage = 24; e.punch3Knockdown = true; e.punch3Chance = 0.14f; e.punch3IsPunch = true;
                e.punch3WindupTime = 10 * 0.06f; e.punch3RecoverTime = (s["isku3"].Length - 10) * F; e.punch3Reach = 3.0f;
                // alt-isku 0: korkea potku (osuma kuvassa 8, kaataa)
                e.altAttackSprites = s["potku"]; e.altImpactFrame = 7; e.altAnimationFrameTime = F; e.altDamage = 17; e.altReach = 3.3f; e.altChance = 0.3f;   // alt = matala potku (paino 1,2) tai korkea potku (0,5)
                e.altVariants = new[]
                {
                    new Enemy.AltVariant { name = "Korkea potku", sprites = s["potku"], impactFrame = 7, frameTime = F, damage = 17, reach = 3.4f, knockdown = true, knockSpeed = 7f, knockUp = 4f, weight = 0.5f, lungeSpeed = 0f },
                    // matala potku (viikate): 33 kuvaa (3 ensimmäistä kahdesti = hitaampi alku), kääntyminen selin, osuma kuvassa 14, kaataa jalat alta: lentää vain vähän (knockSpeed 1,2, knockUp 2), kaatuu lähes paikalleen parilla ilmakuvalla; kuva-aika 0,037 s ja loppu on pitkä kääntyminen takaisin
                    new Enemy.AltVariant { name = "Matala potku", sprites = s["potku2"], impactFrame = 13, frameTime = 0.037f, damage = 15, reach = 3.3f, knockdown = true, knockSpeed = 1.2f, knockUp = 2f, weight = 1.2f, lungeSpeed = 0f },
                };
                // U = alakoukku (osuma kuvassa 15): lataus kiihtyy (AttackEase), Tony liukuu latauksen lopussa eteen; hero lentää kattoon (24), muut ruudun yläreunaan (21), kevyet yli ruudun
                e.uppercutSprites = s["uppercut"]; e.uppercutImpactFrame = 14; e.uppercutDamage = 26; e.uppercutChance = 0.14f; e.uppercutReach = 2.7f;
                e.uppercutWindupTime = 14 * 0.04f; e.uppercutRecoverTime = (s["uppercut"].Length - 14) * 0.04f; e.uppercutHeroUp = 24f; e.uppercutLaunchUp = 21f; e.uppercutLaunchX = 1.5f; e.uppercutStep = 2.8f;
                // kombot: J jabi, S takasuora, K haymaker, 0 korkea potku, 1 matala potku, U alakoukku
                e.combos = new[] { "JS1", "JSJK", "JJU", "JS01", "JS0U" };
                // heitto: kuvat 2–10 (1-pohjainen): kurotus, ote, nosto (kädet ylös), ojennus eteen
                e.grabSprites = new[] { s["heitto"][1], s["heitto"][2], s["heitto"][3], s["heitto"][4], s["heitto"][5], s["heitto"][6], s["heitto"][7], s["heitto"][8], s["heitto"][9] };
                e.grabsHero = true; e.grabChance = 0.5f; e.grabVsBlockChance = 0.9f; e.grabWhenCloseTime = 0.7f;   // heittää herkästi: puolet hyökkäyksistä, lähes aina kun hero torjuu, ja kun hero on lähellä 0,7 s e.grabRange = 2.0f; e.grabReachTime = 0.3f; e.grabLiftTime = 0.8f;
                e.slideThrow = true; e.throwForward = true; e.throwDamage = 22; e.throwSpeed = 12f; e.throwUp = 7f;   // lentää kauas ja liukuu perille, mutta lyhyemmälle kuin Tomin heitto (16 / 9)
                e.hurtSprites = s["osuma"]; e.hurtTime = 0.3f;
                e.knockdownSprites = s["kaatuminen"]; e.airFrameTime = 0.1f; e.thrownStartFrames = new[] { 0, 1, 2 }; e.thrownLoopFrames = new[] { 3, 4, 5 }; e.thrownLandFrames = new[] { 6, 7, 8, 9, 10, 11 }; e.thrownFrameTime = 0.11f;
                e.getUpSprites = s["nousu"]; e.getUpTime = 1.2f;
                e.hurtSounds = gasp; e.hurtVolume = 0.95f;   // väliaikaiset äänet
                var g = s["suojaus"];
                e.blockSprites = new[] { g[1], g[2], g[3] }; e.blockChance = 0.35f; e.maxBlocksInRow = 2;
                go.transform.position = pos;
                EditorUtility.SetDirty(e);
                return e;
            }

            // (alue, osuudet kävelyalueen leveydestä, syvyys): kadulla ja baarissa
            var plan = new (string area, float[] fr, float[] depth)[]
            {
                ("Vegasin katu", new[] { 0.3f, 0.7f }, new[] { 0.5f, 0.35f }),
                ("Vegasin baari", new[] { 0.45f }, new[] { 0.5f }),
            };
            var report = new List<string>();
            foreach (var (an, fr, depth) in plan)
            {
                var a = areas.FirstOrDefault(x => x.areaName == an);
                if (a == null) { report.Add(an + ": ei löytynyt (aja 378/379)"); continue; }
                var grp = new GameObject("Tankki-Tonyt: " + an); grp.transform.SetParent(root.transform, false);
                for (int i = 0; i < fr.Length; i++)
                {
                    float x = Mathf.Lerp(a.walkMinX, a.walkMaxX, fr[i]);
                    float yMax = a.MaxDepthAtX(x, a.maxDepthY), yMin = a.MinDepthAtX(x, a.minDepthY);
                    float y = Mathf.Clamp(Mathf.Lerp(yMax, yMin, depth[i]), yMin + 0.05f, yMax - 0.05f);
                    MakeTony(grp.transform, FreeSpot(a, scene, new Vector3(x, y, 0f)));
                }
                report.Add(an + " " + fr.Length);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info(count + " Tankki-Tonya (520 elämää) lisätty: " + string.Join(", ", report) + ".\nIskut: jabi J, takasuora S, haymaker K (kaataa), korkea potku 0 (kaataa), matala potku 1 (viikate, kaataa), alakoukku U (kattoon asti, liukuu eteen); haymaker K (kaataa, hidas lataus joka kiihtyy); kombot JS, J, S0, JS0, 0, K, JK, JSK. Heitto 50 % (torjuvaa heroa 90 %, lähellä oleva 0,7 s), suojaus 35 %, osuma, kaatuminen ja nousu mukana. Äänet ovat väliaikaisia.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
