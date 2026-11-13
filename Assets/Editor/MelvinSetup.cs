using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Melvin (Vegasin 2. vihu, valikko 382): greaser ja mikkiständi. Kuvat Assets/Sprites/Viholliset/melvin_*.png (Tools/MelvinAssets).
/// Ständi kädessä: juoksee, taklaa (rynnäkkö, kaataa taaksepäin) tai lyö swingin (kiihtyvä, kaataa kattoon); osuma-ääni arvotaan tynnyrin ja pannun väliltä. Kun Melvin kaatuu, ständi tippuu ja hän jatkaa nyrkein (jabi, takasuora, etupotku).
/// Sijoitus Vegasin kadulle ja baariin. Aja valikot 376, 378 ja 379 ensin.</summary>
public static partial class BeatEmUpSetup
{
    const float MelvinScale = 1.2f;

    [MenuItem("Beat em up/382. Las Vegas: Melvin (mikkiständi: taklaus, swingi; ständittä jabi, takasuora, potku)", false, 382)]
    static void AddVegasMelvin()
    {
        if (PlayModeBlocked()) return;
        var s = new Dictionary<string, Sprite[]>();
        foreach (var n in new[] { "idle", "kavely", "juoksu", "isku1", "isku2", "osuma_standi", "jabi", "takasuora", "potku", "osuma", "kaatuminen", "nousu", "suojaus" })
        {
            var sp = LoadWalkerSprites("melvin_" + n);
            if (sp == null || sp.Length == 0) { Info("Puuttuu tai ei leikkaudu: melvin_" + n + ".png (Assets/Sprites/Viholliset). Aja Tools/MelvinAssets/make_melvin_sheets*.py."); return; }
            s[n] = sp;
        }
        const string standPath = "Assets/Sprites/Viholliset/mikkistandi.png";
        var stand = AssetDatabase.LoadAssetAtPath<Sprite>(standPath);
        if (stand == null)
        {
            var imp = AssetImporter.GetAtPath(standPath) as TextureImporter;
            if (imp != null) { imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single; imp.spritePixelsPerUnit = 200f; imp.SaveAndReimport(); }
            stand = AssetDatabase.LoadAssetAtPath<Sprite>(standPath);
        }
        if (!File.Exists(VegasScenePath)) { Info("Aja ensin valikko 376 (Vegas)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var areas = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == scene).ToArray();
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Melvinit").ToArray()) Undo.DestroyObjectImmediate(old);
            var root = new GameObject("Melvinit");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Melvinit");
            int count = 0;
            const float F = 0.04f;
            var gasp = LoadClips("Assets/Audio/big thug", "gasp");
            var hitSnd = LoadClips("Assets/Resources/Sfx/Barrel", "barrel").Concat(LoadClips("Assets/Resources/Sfx/Pan", "pannu")).ToArray();   // ständin osuma: tynnyri tai pannu

            Enemy MakeMelvin(Transform parent, Vector3 pos)
            {
                var go = new GameObject("Melvin " + (++count));
                go.transform.SetParent(parent, false);
                var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
                var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
                var e = go.AddComponent<Enemy>();
                e.body = visual; e.shadow = shadow; visual.sprite = s["idle"][0];
                e.displayName = "Melvin";
                e.visualScale = MelvinScale; e.attackArtScale = 1f; e.knockArtScale = 1f; e.walkArtScale = 1f;
                e.idleSprites = s["idle"]; e.idleFrameTime = 1f / 12f;
                e.walkSprites = s["kavely"]; e.walkFrameTime = 1f / 18f;
                e.runSprites = s["juoksu"]; e.runFrameTime = 1f / 24f; e.runSpeedMultiplier = 1.9f; e.runDistance = 2.2f;   // juoksee lähes koko ajan
                e.maxHealth = 300; e.moveSpeedX = 3.1f; e.moveSpeedY = 2.0f; e.depthTolerance = 0.45f; e.attackCooldown = 0.5f; e.wakeDistance = 13f;
                e.comboChance = 0.5f; e.comboWindupScale = 0.7f; e.comboRecoverFraction = 0.7f; e.attackRange = 2.2f;
                // ilman ständiä: J jabi, S takasuora, P etupotku
                e.punchSprites = s["jabi"]; e.punchImpactFrame = 4; e.windupTime = 4 * F; e.punchRecoverTime = (s["jabi"].Length - 4) * F; e.punchActiveTime = 0.12f; e.punchDamage = 10;
                e.punch2Sprites = s["takasuora"]; e.punch2ImpactFrame = 4; e.punch2Damage = 14; e.punch2Knockdown = false; e.punch2Chance = 0.5f;
                e.altAttackSprites = s["potku"]; e.altImpactFrame = 10; e.altAnimationFrameTime = F; e.altDamage = 15; e.altReach = 3.0f; e.altChance = 0.3f; e.altKnockdown = true; e.altKnockSpeed = 7f; e.altKnockUp = 4f;
                e.combos = new[] { "JS", "JSP", "JJ" };
                e.hurtSprites = s["osuma"]; e.hurtTime = 0.3f;
                e.knockdownSprites = s["kaatuminen"]; e.airFrameTime = 0.07f;
                e.thrownStartFrames = new[] { 0, 1, 2 }; e.thrownLoopFrames = new[] { 3, 4, 5, 6, 7, 8 }; e.thrownLandFrames = Enumerable.Range(9, s["kaatuminen"].Length - 9).ToArray(); e.thrownFrameTime = 0.07f;
                e.getUpSprites = s["nousu"]; e.getUpTime = 1.3f;
                e.hurtSounds = gasp; e.hurtVolume = 0.95f;   // väliaikaiset äänet
                var g = s["suojaus"];
                e.blockSprites = new[] { g[1], g[2], g[3] }; e.blockChance = 0.3f; e.maxBlocksInRow = 2;
                // ständi: swingi (isku1, osuma kuvassa 11, kaataa kattoon) ja taklaus (isku2, osuma 13, kaataa taaksepäin)
                e.uppercutHeroUp = 24f; e.uppercutLaunchUp = 21f; e.uppercutLaunchX = 1.5f;
                e.startsWithStand = true;
                e.standIdle = s["idle"]; e.standWalk = s["kavely"]; e.standRun = s["juoksu"]; e.standHurt = s["osuma_standi"];
                e.standSwing = s["isku1"]; e.standSwingImpact = 10; e.standSwingDamage = 24; e.standSwingWindup = 10 * 0.05f; e.standSwingRecover = (s["isku1"].Length - 10) * F; e.standSwingStep = 1.5f; e.standSwingReach = 3.0f;
                e.standTackle = s["isku2"]; e.standTackleImpact = 12; e.standTackleDamage = 22; e.standTackleFrameTime = 0.035f; e.standTackleLunge = 10f; e.standTackleLungeTime = 0.4f; e.standTackleReach = 3.2f;
                e.standTackleKnockSpeed = 14f; e.standTackleKnockUp = 3f; e.standChargeRange = 14f; e.standChargeMinRange = 3.5f; e.standTackleNearChance = 0.3f;
                e.standAirSprite = stand; e.standHitSounds = hitSnd;
                go.transform.position = pos;
                EditorUtility.SetDirty(e);
                return e;
            }

            var plan = new (string area, float[] fr, float[] depth)[]
            {
                ("Vegasin katu", new[] { 0.5f, 0.85f }, new[] { 0.7f, 0.4f }),
                ("Vegasin baari", new[] { 0.7f }, new[] { 0.4f }),
            };
            var report = new List<string>();
            foreach (var (an, fr, depth) in plan)
            {
                var a = areas.FirstOrDefault(x => x.areaName == an);
                if (a == null) { report.Add(an + ": ei löytynyt (aja 378/379)"); continue; }
                var grp = new GameObject("Melvinit: " + an); grp.transform.SetParent(root.transform, false);
                for (int i = 0; i < fr.Length; i++)
                {
                    float x = Mathf.Lerp(a.walkMinX, a.walkMaxX, fr[i]);
                    float yMax = a.MaxDepthAtX(x, a.maxDepthY), yMin = a.MinDepthAtX(x, a.minDepthY);
                    float y = Mathf.Clamp(Mathf.Lerp(yMax, yMin, depth[i]), yMin + 0.05f, yMax - 0.05f);
                    MakeMelvin(grp.transform, FreeSpot(a, scene, new Vector3(x, y, 0f)));
                }
                report.Add(an + " " + fr.Length);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info(count + " Melvinia (300 elämää) lisätty: " + string.Join(", ", report) + ".\nStändillä: juoksee, taklaus (rynnäkkö, kaataa taakse), swingi (kiihtyvä, kaataa kattoon), osuma-ääni tynnyri/pannu. Kaatuessa ständi putoaa; sen jälkeen jabi, takasuora ja etupotku (kombot JS, JSP, JJ). Äänet (osuma, kaatuminen) ovat väliaikaisia.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
