using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Don (Vegasin pomo, valikko 383): punapukuinen herra, jota suojelee kaksi Tankki-Tonya. Kuvat Assets/Sprites/Viholliset/don_*.png (Tools/DonAssets/make_don_sheets.py).
/// Iskut: J jabi, S takasuora, K kyynärpää (kaataa, kiihtyvä lataus), 0 etupotku, 1 kierrospotku (kaataa), 2 nilkkapotku (kaataa). Ei suojausta (kuvia ei vielä).
/// Saattue: Tonyt (kopioidaan kentän Tonyista, aja 380 ensin) seisovat Donin ja pelaajan välissä; Don pysyy vartijoiden takana kunnes pelaaja pääsee lähelle tai vartijat kaatuvat.
/// Sijoitus Vegasin baariin. Aja valikot 376, 378, 379 ja 380 ensin.</summary>
public static partial class BeatEmUpSetup
{
    const float DonScale = 1.2f;

    [MenuItem("Beat em up/383. Las Vegas: Don + kaksi Tankki-Tonya vartijoina", false, 383)]
    static void AddVegasDon()
    {
        if (PlayModeBlocked()) return;
        var s = new Dictionary<string, Sprite[]>();
        foreach (var n in new[] { "idle", "kavely", "juoksu", "jabi", "potku", "takasuora", "kierros", "nilkka", "kyynarpaa", "osuma", "kaatuminen", "nousu" })
        {
            var sp = LoadWalkerSprites("don_" + n);
            if (sp == null || sp.Length == 0) { Info("Puuttuu tai ei leikkaudu: don_" + n + ".png (Assets/Sprites/Viholliset). Aja Tools/DonAssets/make_don_sheets.py."); return; }
            s[n] = sp;
        }
        if (!File.Exists(VegasScenePath)) { Info("Aja ensin valikko 376 (Vegas)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var areas = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == scene).ToArray();
            var tonys = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(e => e != null && e.gameObject.scene == scene && e.displayName == "Tankki-Tony" && e.guardOf == null && e.transform.parent != null && !e.transform.parent.name.StartsWith("Donit")).ToArray();
            if (tonys.Length == 0) { Info("Vegasista ei löytynyt Tankki-Tonya malliksi: aja valikko 380 ensin."); return; }
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Donit").ToArray()) Undo.DestroyObjectImmediate(old);
            var root = new GameObject("Donit");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Donit");
            const float F = 0.042f;
            var gasp = LoadClips("Assets/Audio/big thug", "gasp");

            Enemy MakeDon(Transform parent, Vector3 pos)
            {
                var go = new GameObject("Don");
                go.transform.SetParent(parent, false);
                var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
                var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
                var e = go.AddComponent<Enemy>();
                e.body = visual; e.shadow = shadow; visual.sprite = s["idle"][0];
                e.displayName = "Don";
                e.visualScale = DonScale; e.attackArtScale = 1f; e.knockArtScale = 1f; e.walkArtScale = 1f;
                e.idleSprites = s["idle"]; e.idleFrameTime = 1f / 12f;
                e.walkSprites = s["kavely"]; e.walkFrameTime = 1f / 20f;
                e.runSprites = s["juoksu"]; e.runFrameTime = 1f / 24f; e.runSpeedMultiplier = 1.8f; e.runDistance = 5f;
                e.maxHealth = 420; e.moveSpeedX = 2.9f; e.moveSpeedY = 1.9f; e.depthTolerance = 0.45f; e.attackCooldown = 0.45f; e.wakeDistance = 13f;
                e.comboChance = 0.6f; e.comboWindupScale = 0.65f; e.comboRecoverFraction = 0.65f; e.attackRange = 2.3f;
                // J = jabi (osuma kuvassa 8), S = takasuora (osuma 6)
                e.punchSprites = s["jabi"]; e.punchImpactFrame = 7; e.windupTime = 7 * F; e.punchRecoverTime = (s["jabi"].Length - 7) * F; e.punchActiveTime = 0.12f; e.punchDamage = 11;
                e.punch2Sprites = s["takasuora"]; e.punch2ImpactFrame = 5; e.punch2Damage = 15; e.punch2Knockdown = false; e.punch2Chance = 0.4f;
                // K = kyynärpää (osuma kuvassa 21, kaataa): hidas lataus joka kiihtyy iskua kohti (AttackEase)
                int ei = 20; e.punch3Sprites = s["kyynarpaa"]; e.punch3ImpactFrame = ei; e.punch3Damage = 26; e.punch3Knockdown = true; e.punch3Chance = 0.15f; e.punch3IsPunch = true;
                e.punch3WindupTime = 14 * 0.08f + 6 * 0.04f; e.punch3RecoverTime = (s["kyynarpaa"].Length - ei) * F; e.punch3Reach = 3.0f;
                // alt: 0 etupotku (osuma 10), 1 kierrospotku (osuma 11, kaataa), 2 nilkkapotku (osuma 10, kaataa jalat alta)
                e.altAttackSprites = s["potku"]; e.altImpactFrame = 9; e.altAnimationFrameTime = F; e.altDamage = 14; e.altReach = 3.2f; e.altChance = 0.35f;
                e.altVariants = new[]
                {
                    new Enemy.AltVariant { name = "Etupotku", sprites = s["potku"], impactFrame = 9, frameTime = F, damage = 14, reach = 3.3f, knockdown = true, knockSpeed = 6f, knockUp = 3f, weight = 1f, lungeSpeed = 0f },
                    new Enemy.AltVariant { name = "Kierrospotku", sprites = s["kierros"], impactFrame = 10, frameTime = F, damage = 20, reach = 3.4f, knockdown = true, knockSpeed = 8f, knockUp = 4f, weight = 0.8f, lungeSpeed = 0f },
                    new Enemy.AltVariant { name = "Nilkkapotku", sprites = s["nilkka"], impactFrame = 9, frameTime = F, damage = 13, reach = 3.2f, knockdown = true, knockSpeed = 1.5f, knockUp = 2f, weight = 1f, lungeSpeed = 0f },
                };
                // kombot: J jabi, S takasuora, K kyynärpää, 0 etupotku, 1 kierrospotku, 2 nilkkapotku
                e.combos = new[] { "JS", "JSK", "J1", "JS2", "S01", "J0K" };
                e.hurtSprites = s["osuma"]; e.hurtTime = 0.3f;
                e.knockdownSprites = s["kaatuminen"]; e.airFrameTime = 0.07f;
                e.thrownStartFrames = new[] { 0, 1, 2, 3 }; e.thrownLoopFrames = new[] { 4, 5, 6, 7, 8, 9 }; e.thrownLandFrames = Enumerable.Range(10, s["kaatuminen"].Length - 10).ToArray(); e.thrownFrameTime = 0.07f;
                e.getUpSprites = s["nousu"]; e.getUpTime = 1.5f;
                e.hurtSounds = gasp; e.hurtVolume = 0.95f;   // väliaikaiset äänet
                go.transform.position = pos;
                EditorUtility.SetDirty(e);
                return e;
            }

            var a = areas.FirstOrDefault(x => x.areaName == "Vegasin baari");
            if (a == null) { Info("Alue \"Vegasin baari\" puuttuu: aja valikko 379."); return; }
            var grp = new GameObject("Donit: Vegasin baari"); grp.transform.SetParent(root.transform, false);
            float x = Mathf.Lerp(a.walkMinX, a.walkMaxX, 0.85f);
            float yMax = a.MaxDepthAtX(x, a.maxDepthY), yMin = a.MinDepthAtX(x, a.minDepthY);
            float y = Mathf.Clamp(Mathf.Lerp(yMax, yMin, 0.5f), yMin + 0.05f, yMax - 0.05f);
            var don = MakeDon(grp.transform, FreeSpot(a, scene, new Vector3(x, y, 0f)));
            var guards = new Enemy[2];
            for (int i = 0; i < 2; i++)
            {
                var g = Object.Instantiate(tonys[0].gameObject, grp.transform);
                g.name = "Tankki-Tony (vartija " + (i + 1) + ")";
                var ge = g.GetComponent<Enemy>(); ge.guardOf = don; ge.guards = null;
                var gy = Mathf.Clamp(y + (i == 0 ? 0.7f : -0.7f), yMin + 0.05f, yMax - 0.05f);
                g.transform.position = new Vector3(x - 2.4f, gy, 0f);
                guards[i] = ge; EditorUtility.SetDirty(ge);
            }
            don.guards = guards; EditorUtility.SetDirty(don);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Don (420 elämää) ja kaksi Tankki-Tonya vartijoina lisätty Vegasin baarin perälle. Don pysyy vartijoiden takana ~8 yksikön päässä; vartijat taistelevat kun pelaaja tulee lähelle, ja Don osallistuu kun pelaaja pääsee lähelle tai vartijat kaatuvat.\nDonin iskut: jabi, takasuora, kyynärpää (kaataa), etupotku, kierrospotku (kaataa), nilkkapotku (kaataa). Suojaus puuttuu, äänet ovat väliaikaisia.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
