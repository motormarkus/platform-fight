using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Vankilan vartijat (valikko 363): kuvat Assets/Sprites/Viholliset/vartija_*.png (Tools/GuardAssets/make_guard_sheets.py, make_guard_fight.py, make_guard_baton.py) ja
/// Assets/Resources/Vartija/pamppu.png. Vartija aloittaa pampulla; kaatuessaan (tai heitettynä) pamppu lentää kädestä ja hän taistelee sen jälkeen nyrkein (VartijaBrain).
/// Heitettynä näytetään kaatumiskuvat.</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/363. Vankila: vartijat (pamppu, kaatuessa pamppu lentää, sitten nyrkit)", false, 363)]
    static void AddPrisonGuards()
    {
        if (PlayModeBlocked()) return;
        var sheets = new Dictionary<string, Sprite[]>();
        foreach (var n in new[] { "idle", "kavely", "juoksu", "pamppu_idle", "pamppu1", "pamppu2", "suora1", "suora2", "potku", "osuma", "kaatuminen", "maahan", "nousu", "pilli" })
        {
            var sp = LoadWalkerSprites("vartija_" + n);
            if (sp == null || sp.Length == 0) { Info("Puuttuu tai ei leikkaudu: vartija_" + n + ".png (Assets/Sprites/Viholliset). Aja Tools/GuardAssets/make_guard_*.py."); return; }
            sheets[n] = sp;
        }
        const string batonPath = "Assets/Resources/Vartija/pamppu.png";
        var batonSprite = File.Exists(batonPath) ? ImportShipSprite(batonPath, 1250f, false, SpriteAlignment.Center) : null;
        var whistleClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Sfx/Vartija/pilli.wav");
        if (batonSprite == null) { Info("Puuttuu Assets/Resources/Vartija/pamppu.png (Tools/GuardAssets/make_guard_baton.py)."); return; }
        if (!File.Exists(PrisonScenePath)) { Info("Aja ensin valikot 353–362."); return; }
        var scene = EditorSceneManager.GetSceneByPath(PrisonScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(PrisonScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var areas = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == scene).ToArray();
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Vankilan vartijat").ToArray()) Undo.DestroyObjectImmediate(old);
            var root = new GameObject("Vankilan vartijat");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Vankilan vartijat");
            int count = 0;
            const float F = 0.05f;   // kuva-aika (kuvat videosta 2 ruudun välein)

            Enemy MakeGuard(Transform parent, Vector3 pos)
            {
                var go = new GameObject("Vartija " + (++count));
                go.transform.SetParent(parent, false);
                var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
                var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
                var e = go.AddComponent<Enemy>();
                e.body = visual; e.shadow = shadow; visual.sprite = sheets["pamppu_idle"][0];
                e.displayName = "Vartija";
                e.visualScale = GuardScale; e.attackArtScale = 1f; e.knockArtScale = 1f; e.walkArtScale = 1f;   // vartija 15 % isommaksi
                e.idleSprites = sheets["pamppu_idle"]; e.idleFrameTime = 1f / 12f;
                e.walkSprites = sheets["kavely"]; e.walkFrameTime = 1f / 12f;
                e.runSprites = sheets["juoksu"]; e.runFrameTime = 1f / 24f; e.runSpeedMultiplier = 1.9f; e.runDistance = 6f;
                e.maxHealth = 150; e.moveSpeedX = 3.4f; e.moveSpeedY = 2.4f; e.depthTolerance = 0.4f; e.attackCooldown = 0.8f; e.wakeDistance = 13f;
                e.comboChance = 0.55f; e.comboWindupScale = 0.6f; e.comboRecoverFraction = 0.6f;
                e.blockSprites = null; e.blockChance = 0f;
                e.brawlGroup = "Vartijat"; e.huntsBrawlers = true;   // vartijat käyvät vankien (Hector, fightsEveryone) kimppuun
                e.hurtSprites = sheets["osuma"]; e.hurtTime = 0.3f;
                e.knockdownSprites = sheets["kaatuminen"]; e.airFrameTime = 0.05f;   // heitettynä näytetään samat kaatumiskuvat
                e.landSprites = sheets["maahan"]; e.landFrameTime = 0.1f;
                e.getUpSprites = sheets["nousu"]; e.getUpTime = sheets["nousu"].Length / 14f;
                var brain = go.AddComponent<VartijaBrain>();
                brain.batonSprite = batonSprite; brain.hasBaton = true;
                brain.whistleSprites = sheets["pilli"]; brain.whistleClip = whistleClip; brain.canCall = true; brain.callCount = 3;
                // pamppuvaihe: J = pamppu1 (osuma kuvassa 6), S = pamppu2 (kuvassa 6, kaataa); pidempi ulottuvuus ja kovempi isku kuin nyrkeillä
                brain.baton = new VartijaBrain.Loadout
                {
                    idle = sheets["pamppu_idle"], j = sheets["pamppu1"], s = sheets["pamppu2"], k = null,
                    jImpact = 5, sImpact = 5, jDamage = 14, sDamage = 16, sKnockdown = true, frameTime = F, attackRange = 2.6f, combos = new[] { "J", "JS", "JJ" }
                };
                // nyrkkivaihe: J = suora1, S = suora2, K = takajalan etupotku (kaataa)
                brain.fists = new VartijaBrain.Loadout
                {
                    idle = sheets["idle"], j = sheets["suora1"], s = sheets["suora2"], k = sheets["potku"],
                    jImpact = 3, sImpact = 3, kImpact = 4, jDamage = 11, sDamage = 13, kDamage = 17, sKnockdown = false, frameTime = F, attackRange = 2.3f, kReach = 3.0f,
                    combos = new[] { "JS", "JSK", "J", "JJS" }
                };
                brain.slamSprites = sheets["pamppu2"]; brain.slamHitFrom = 5; brain.slamHitTo = 7; brain.slamDamage = 20;
                go.transform.position = pos;
                EditorUtility.SetDirty(e); EditorUtility.SetDirty(brain);
                return e;
            }

            // (alue, osuudet kävelyalueen leveydestä): ovien läheisyys vältetään, eka vartija vasta hallin keskellä
            var plan = new (string area, float[] fr, float[] depth)[]
            {
                ("Vankilan D-osasto", new[] { 0.45f, 0.78f }, new[] { 0.4f, 0.65f }),
                ("Vankilan työpaja", new[] { 0.40f, 0.75f }, new[] { 0.5f, 0.35f }),
                ("Vankilan E-osasto", new[] { 0.45f, 0.78f }, new[] { 0.6f, 0.4f }),
                ("Vankilan ruokala", new[] { 0.30f, 0.55f, 0.80f }, new[] { 0.35f, 0.65f, 0.45f }),
                ("Vankilan kuntopiha", new[] { 0.28f, 0.55f, 0.80f }, new[] { 0.6f, 0.35f, 0.55f }),
                ("Vankilan henkilökunta", new[] { 0.40f, 0.62f, 0.82f }, new[] { 0.5f, 0.35f, 0.6f }),
            };
            var report = new List<string>();
            foreach (var (an, fr, depth) in plan)
            {
                var a = areas.FirstOrDefault(x => x.areaName == an);
                if (a == null) { report.Add(an + ": ei löytynyt"); continue; }
                var grp = new GameObject("Vartijat: " + an); grp.transform.SetParent(root.transform, false);
                for (int i = 0; i < fr.Length; i++)
                {
                    float x = Mathf.Lerp(a.walkMinX, a.walkMaxX, fr[i]);
                    float yMax = a.MaxDepthAtX(x, a.maxDepthY), yMin = a.MinDepthAtX(x, a.minDepthY);
                    float y = Mathf.Clamp(Mathf.Lerp(yMax, yMin, depth[i]), yMin + 0.05f, yMax - 0.05f);
                    MakeGuard(grp.transform, FreeSpot(a, scene, new Vector3(x, y, 0f)));   // ei päällekkäin laatikoiden, pöytien eikä toisten vihujen kanssa
                }
                report.Add(an + " " + fr.Length);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info(count + " vartijaa lisätty vankilaan (150 elämää): " + string.Join(", ", report) + ".\n" +
                 "Äkätessään heron vartija soittaa pilliä ja kutsuu 3 lisävartijaa (ruudun reunoilta, pampulla; vain yksi hälytys kerrallaan, 45 s väli). Alussa pamppu (J pamppu1, S pamppu2 kaataa, yläkautta lyönti kaataa); kaatuessa/heitettynä pamppu lentää kädestä ja sen jälkeen nyrkit (suorat J/S, takajalan etupotku K kaataa). Heitettynä näytetään kaatumiskuvat. Äänet väliaikaisesti Jakuzan.\nAjamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
