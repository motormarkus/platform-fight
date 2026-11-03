using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Mongolisoturi (vihollinen): kuvat Assets/Sprites/Viholliset/mongoli_*.png (Tools/MongolAssets/make_mongol_sheets.py).
/// Valikko 328 lisää heitä Muuri-sceneen: muurille, terminaalin väkeen ja kuuhissin aaltotaisteluun.</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/328. Mongolisoturit: muurille, terminaaliin ja kuuhissin taisteluun", false, 328)]
    static void AddMongols()
    {
        if (PlayModeBlocked()) return;
        var sheets = new Dictionary<string, Sprite[]>();
        foreach (var n in new[] { "idle", "kavely", "juoksu", "suoja", "suora1", "suora2", "potku1", "potku2", "potku3", "hyppypotku", "heitto", "osuma", "kaatuminen", "maahan", "nousu" })
        {
            var sp = LoadWalkerSprites("mongoli_" + n);
            if (sp == null || sp.Length == 0) { Info("Puuttuu tai ei leikkaudu: mongoli_" + n + ".png (Assets/Sprites/Viholliset). Aja Tools/MongolAssets/make_mongol_sheets.py."); return; }
            sheets[n] = sp;
        }
        var rockArr = LoadWalkerSprites("mongoli_kivi");
        if (rockArr == null || rockArr.Length == 0) { Info("Puuttuu mongoli_kivi.png (Tools/MongolAssets/cut_rock.py)."); return; }
        if (!File.Exists(WallScenePath)) { Info("Aja ensin valikko 327 (Kiinan muuri)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(WallScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(WallScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            Area Find(string n) => Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == n && a.gameObject.scene == scene);
            var wall = Find("Muuri"); var term = Find("Muurin terminaali"); var elev = Find("Muurin kuuhissi");
            if (wall == null || term == null || elev == null) { Info("Muurin alueita ei löytynyt: aja valikko 327."); return; }
            foreach (var nm in new[] { "Mongolisoturit", "Kuuhissin taistelu" })
                foreach (var old in scene.GetRootGameObjects().Where(g => g.name == nm).ToArray()) Undo.DestroyObjectImmediate(old);
            var root = new GameObject("Mongolisoturit");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Mongolisoturit");
            float ppu = 941f / (2f * CamHalf);
            const float F = 1f / 30f;
            int count = 0;

            Enemy MakeMongol(Transform parent, Vector3 pos)
            {
                var go = new GameObject("Mongolisoturi " + (++count));
                go.transform.SetParent(parent, false);
                var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
                var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
                var e = go.AddComponent<Enemy>();
                e.body = visual; e.shadow = shadow; visual.sprite = sheets["idle"][0];
                e.displayName = "Mongolisoturi";
                e.visualScale = 1f; e.attackArtScale = 1f; e.knockArtScale = 1f; e.walkArtScale = 1f;
                e.idleSprites = sheets["idle"]; e.idleFrameTime = 0.06f;
                e.walkSprites = sheets["kavely"]; e.walkFrameTime = 0.04f;
                e.runSprites = sheets["juoksu"]; e.runFrameTime = 0.04f; e.runSpeedMultiplier = 1.9f; e.runDistance = 6f;
                e.maxHealth = 170; e.moveSpeedX = 3.8f; e.moveSpeedY = 2.7f; e.attackRange = 2.3f; e.attackCooldown = 0.7f; e.wakeDistance = 14f;
                // äänet: Enemy.cs antaa Mongolisoturille Jakuzan äänet (Resources/Sfx/Jakuza)
                // suorat (J, S) ja etupotku (K)
                var j = sheets["suora1"]; var s = sheets["suora2"]; var k = sheets["potku1"];
                e.punchSprites = j; e.punchImpactFrame = 4; e.windupTime = 4 * F; e.punchRecoverTime = (j.Length - 4) * F; e.punchDamage = 10;
                e.punch2Sprites = s; e.punch2ImpactFrame = 3; e.punch2Damage = 12; e.punch2Knockdown = false; e.punch2Chance = 0.4f;
                e.punch3Sprites = k; e.punch3ImpactFrame = 3; e.punch3Damage = 16; e.punch3Knockdown = true; e.punch3Chance = 0.3f; e.punch3WindupTime = 0.12f; e.punch3RecoverTime = 0.22f; e.punch3Reach = 3.0f;
                e.combos = new[] { "JS", "JSK", "J" }; e.comboChance = 0.6f; e.comboWindupScale = 0.6f; e.comboRecoverFraction = 0.6f;
                e.blockSprites = sheets["suoja"]; e.blockChance = 0.4f; e.maxBlocksInRow = 2; e.blockTime = 0.4f;
                e.hurtSprites = sheets["osuma"]; e.hurtTime = 0.3f;
                e.knockdownSprites = sheets["kaatuminen"]; e.airFrameTime = 0.05f;
                e.landSprites = sheets["maahan"]; e.landFrameTime = 0.07f;
                e.getUpSprites = sheets["nousu"]; e.getUpTime = sheets["nousu"].Length / 15f;
                var brain = go.AddComponent<MongolBrain>();
                brain.rockSprite = rockArr[0]; brain.rockDamage = 13;
                brain.moves = new[]
                {
                    new MongolBrain.Move { name = "vaakapotku", sprites = sheets["potku2"], frameTime = 0.045f, hitFrom = 3, hitTo = 5, reach = 3.3f, minRange = 1.8f, maxRange = 3.6f, weight = 1f, cooldown = 3f, damage = 18 },
                    new MongolBrain.Move { name = "pyöräytyspotku", sprites = sheets["potku3"], frameTime = 0.05f, hitFrom = 6, hitTo = 8, reach = 3.0f, minRange = 1.5f, maxRange = 3.2f, weight = 1f, cooldown = 4f, damage = 23, knockdown = true },
                    new MongolBrain.Move { name = "hyppypotku", sprites = sheets["hyppypotku"], frameTime = 0.05f, hitFrom = 6, hitTo = 10, reach = 2.6f, minRange = 4f, maxRange = 11f, weight = 1f, cooldown = 5f, damage = 21, knockdown = true, moveFrom = 3, moveTo = 9, moveSpeed = 9f },
                    new MongolBrain.Move { name = "kivenheitto", sprites = sheets["heitto"], frameTime = 0.045f, releaseFrame = 9, minRange = 6f, maxRange = 16f, weight = 1f, cooldown = 6f },
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

            // ---- muuri: ryhmä ennen rotkoa ja kolme ryhmää sen jälkeen ----
            var wallGroup = new GameObject("Muurin mongolisoturit"); wallGroup.transform.SetParent(root.transform, false);
            foreach (var (px, d) in new[] { (1672f + 900f, 0.5f), (2f * 1672f + 250f, 0.3f), (3f * 1672f + 500f, 0.7f), (3f * 1672f + 1250f, 0.35f), (4f * 1672f + 700f, 0.6f), (4f * 1672f + 1350f, 0.25f), (5f * 1672f + 450f, 0.5f), (5f * 1672f + 1150f, 0.75f) })
                MakeMongol(wallGroup.transform, At(wall, WallX0, px, d));

            // ---- terminaali: kolme rauhallista asiakasta (liittyvät tappeluun) ----
            var termGroup = new GameObject("Terminaalin mongolisoturit"); termGroup.transform.SetParent(root.transform, false);
            var brawl = scene.GetRootGameObjects().Select(g => g.GetComponent<SalsaBrawl>()).FirstOrDefault(b => b != null && b.area == term);
            var termMongols = new List<Enemy>();
            foreach (var (px, d) in new[] { (880f, 0.6f), (2950f, 0.8f), (3800f, 0.5f) })
            {
                var e = MakeMongol(termGroup.transform, At(term, WallTermX0, px, d));
                if (brawl != null)
                {
                    e.wakeDistance = -1f; e.wakeOnFightRadius = 0f; e.joinsFightWhenSquadComes = false; e.fightsEveryone = true; e.ally = false; e.brawlsWithOthers = false;
                    if (e.calmIdleSprites == null || e.calmIdleSprites.Length == 0) e.calmIdleSprites = e.idleSprites;
                    if (e.calmWalkSprites == null || e.calmWalkSprites.Length == 0) { e.calmWalkSprites = e.walkSprites; e.calmWalkFrameTime = e.walkFrameTime; }
                    float x = e.transform.position.x; e.wanderMinX = x - 520f / ppu; e.wanderMaxX = x + 520f / ppu;
                }
                termMongols.Add(e);
            }
            if (brawl != null) { brawl.crowd = (brawl.crowd ?? new Enemy[0]).Concat(termMongols).ToArray(); EditorUtility.SetDirty(brawl); }

            // ---- kuuhissi: kolme aaltoa (2 + 2 + 2); ovi vasemmalla aukeaa vasta, kun kaikki on kaatunut ----
            var fight = new GameObject("Kuuhissin taistelu");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fight, scene);
            Undo.RegisterCreatedObjectUndo(fight, "Kuuhissin taistelu");
            var ew = fight.AddComponent<EnemyWaves>(); ew.area = elev;
            var waveDefs = new[] { new[] { (600f, 0.4f), (900f, 0.7f) }, new[] { (1500f, 0.3f), (2100f, 0.6f) }, new[] { (650f, 0.55f), (2150f, 0.45f) } };
            var waves = new List<EnemyWaves.Wave>(); var all = new List<Enemy>();
            foreach (var wd in waveDefs)
            {
                var list = new List<Enemy>();
                foreach (var (px, d) in wd)
                {
                    var e = MakeMongol(fight.transform, At(elev, WallElevX0, px, d));
                    e.gameObject.SetActive(false); list.Add(e); all.Add(e);
                }
                waves.Add(new EnemyWaves.Wave { enemies = list.ToArray(), delay = 1.2f });
            }
            ew.waves = waves.ToArray();
            var exitDoor = Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(d => d.gameObject.scene == scene && d.here == elev && d.name.StartsWith("Kuuhissi"));
            string doorInfo = "ovea ei löytynyt";
            if (exitDoor != null) { exitDoor.guards = all.ToArray(); EditorUtility.SetDirty(exitDoor); doorInfo = "hissin vasen ovi lukossa, kunnes kaikki 6 on kaatunut"; }
            EditorUtility.SetDirty(ew);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info(count + " mongolisoturia lisätty Muuri-sceneen (170 elämää): muurille 8, terminaaliin " + termMongols.Count + (brawl != null ? " (liittyvät tappeluun)" : " (tappelua ei löytynyt: tavallisia vihuja)") + ", kuuhissiin 6 (3 aaltoa, " + doorInfo + ").\n" +
                 "Liikkeet: suorat (J, S) ja etupotku (K) kombona, vaakapotku, pyöräytyspotku, hyppypotku ja kivenheitto (väistö hyppäämällä), torjunta 40 %. Äänet: Jakuzan (Enemy.cs).\nAjamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
