using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Gestapo (vihollinen, natsilinnakkeen juhlahalli): kuvat Assets/Sprites/Viholliset/gestapo_*.png (Tools/GestapoAssets/make_gestapo_sheets.py).
/// Valikko 334 lisää heitä juhlahalliin (valikko 327/330 tekee hallin). Liikkeet: suora lyönti, etupotku ja hyppypotku; osuma, kaatuminen, maahan ja nousu.</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/334. Gestapo: juhlahalliin (idle, kävely, juoksu, hyppypotku)", false, 334)]
    static void AddGestapo()
    {
        if (PlayModeBlocked()) return;
        var sheets = new Dictionary<string, Sprite[]>();
        foreach (var n in new[] { "idle", "kavely", "juoksu", "hyppypotku", "suora1", "potku", "osuma_kevyt", "kaatuminen", "maahan", "nousu" })
        {
            var sp = LoadWalkerSprites("gestapo_" + n);
            if (sp == null || sp.Length == 0) { Info("Puuttuu tai ei leikkaudu: gestapo_" + n + ".png (Assets/Sprites/Viholliset). Aja Tools/GestapoAssets/make_gestapo_sheets.py ja make_gestapo_sheets2.py."); return; }
            sheets[n] = sp;
        }
        if (!File.Exists(WallScenePath)) { Info("Aja ensin valikko 327 (Kiinan muuri)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(WallScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(WallScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var hall = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Muurin juhlahalli" && a.gameObject.scene == scene);
            if (hall == null) { Info("Juhlahallia ei löytynyt: aja valikko 330 (tai 327)."); return; }
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Gestapo").ToArray()) Undo.DestroyObjectImmediate(old);
            var root = new GameObject("Gestapo");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Gestapo");
            float ppu = 941f / (2f * CamHalf);
            int count = 0;

            Enemy MakeGestapo(Vector3 pos)
            {
                var go = new GameObject("Gestapo " + (++count));
                go.transform.SetParent(root.transform, false);
                var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
                var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
                var e = go.AddComponent<Enemy>();
                e.body = visual; e.shadow = shadow; visual.sprite = sheets["idle"][0];
                e.displayName = "Gestapo";
                e.visualScale = 1f; e.attackArtScale = 1f; e.knockArtScale = 1f; e.walkArtScale = 1f;
                e.idleSprites = sheets["idle"]; e.idleFrameTime = 0.06f;
                e.walkSprites = sheets["kavely"]; e.walkFrameTime = 0.045f;
                e.runSprites = sheets["juoksu"]; e.runFrameTime = 0.045f; e.runSpeedMultiplier = 1.9f; e.runDistance = 6f;
                e.maxHealth = 190; e.moveSpeedX = 3.8f; e.moveSpeedY = 2.7f; e.attackRange = 2.3f; e.attackCooldown = 0.7f; e.wakeDistance = 14f;
                // suora lyönti (J) ja etupotku (K); ylälyöntiä ei ole
                const float F = 1f / 30f;
                var j = sheets["suora1"]; var k = sheets["potku"];
                e.punchSprites = j; e.punchImpactFrame = 3; e.windupTime = 3 * F * 2f; e.punchRecoverTime = (j.Length - 3) * F * 2f; e.punchDamage = 11;
                e.punch3Sprites = k; e.punch3ImpactFrame = 3; e.punch3Damage = 16; e.punch3Knockdown = true; e.punch3Chance = 0.35f; e.punch3WindupTime = 0.2f; e.punch3RecoverTime = 0.3f; e.punch3Reach = 3.2f;
                e.combos = new[] { "J", "JK", "J" }; e.comboChance = 0.6f; e.comboWindupScale = 0.6f; e.comboRecoverFraction = 0.6f;
                e.hurtSprites = sheets["osuma_kevyt"]; e.hurtTime = 0.3f;
                e.knockdownSprites = sheets["kaatuminen"]; e.airFrameTime = 0.05f;
                e.landSprites = sheets["maahan"]; e.landFrameTime = 0.07f;
                e.getUpSprites = sheets["nousu"]; e.getUpTime = sheets["nousu"].Length / 15f;
                var brain = go.AddComponent<MongolBrain>();
                brain.globalCooldown = 1.2f;
                brain.moves = new[]
                {
                    // kuvat 64 + 3·n videossa: ponnistus indeksi 3–4, jalka ojossa 6–12, lasku 15–18
                    new MongolBrain.Move { name = "hyppypotku", sprites = sheets["hyppypotku"], frameTime = 0.045f, hitFrom = 6, hitTo = 12, reach = 2.8f, minRange = 1.2f, maxRange = 11f, weight = 1f, cooldown = 2.5f, damage = 22, knockdown = true, moveFrom = 3, moveTo = 14, moveSpeed = 8f },
                };
                go.transform.position = pos;
                EditorUtility.SetDirty(e); EditorUtility.SetDirty(brain);
                return e;
            }
            Vector3 At(float px, float depth)
            {
                float x = WallHallX0 + px / HallPpu; float yMax = hall.MaxDepthAtX(x, hall.maxDepthY), yMin = hall.MinDepthAtX(x, hall.minDepthY);
                return new Vector3(x, Mathf.Clamp(Mathf.Lerp(yMax, yMin, depth), yMin + 0.05f, yMax - 0.05f), 0f);
            }
            foreach (var (px, d) in new[] { (1500f, 0.45f), (2700f, 0.7f), (3900f, 0.35f), (5200f, 0.6f), (6500f, 0.4f) }) MakeGestapo(At(px, d));
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info(count + " gestapoa lisätty juhlahalliin (190 elämää). Liikkeet: suora lyönti, etupotku (kaataa) ja hyppypotku (loikka heroa kohti, kaataa); osuma-, kaatumis- ja nousukuvat. Äänet väliaikaisesti Jakuzan.\nAjamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
