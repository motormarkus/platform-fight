using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Nokkamies (Foreman, valikko 395): työmaakonttorin yläkerran toimiston pomo (Vegas, alue "Työmaakonttorin toimisto"). Kuvat Assets/Sprites/Viholliset/nokkamies_*.png (Tools/NokkamiesAssets/make_nokkamies_sheets.py).
/// Vaikeus: 700 elämää, painostava (lyhyet tauot, torjunta 25 %, max 2 peräkkäin), raivo alle 45 %:ssa. Liikkeet: suorat J/S/K (K = kovin suora, kaataa), kombot, etupotku P ja suora+korkea potku D,
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
        foreach (var n in new[] { "idle", "kavely", "juoksu", "suora1", "suora2", "suora3", "suora4", "potku1", "potku2", "potku3", "syoksy", "syoksy2", "osuma", "kaatuminen", "maahan", "nousu", "suoja", "pilli" })
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
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Nokkamies" || g.name == "Nokkamiehen apurit (mallit)" || g.name == "Toimiston kaasupullot").ToArray()) Undo.DestroyObjectImmediate(old);
            var go = new GameObject("Nokkamies");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, "Nokkamies");
            var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
            var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
            var e = go.AddComponent<Enemy>();
            const float F = 1f / 14f;   // iskukuvien kuva-aika (kuvat videosta joka toinen ruutu 24 fps -> 12 fps, hieman nopeammin)
            e.body = visual; e.shadow = shadow; visual.sprite = sheets["idle"][0];
            e.displayName = "Nokkamies"; e.isBoss = true;
            e.visualScale = 1.2f; e.attackArtScale = 1f;   // kuvat 600 px -> 720 px (Tony/Don-koko) e.knockArtScale = 1f; e.walkArtScale = 1f;
            e.idleSprites = sheets["idle"]; e.idleFrameTime = 1f / 12f;
            e.walkSprites = sheets["kavely"]; e.walkFrameTime = 1f / 12f;
            e.runSprites = sheets["juoksu"]; e.runFrameTime = 1f / 12f; e.runSpeedMultiplier = 2.0f; e.runDistance = 5f;
            // vähemmän elämää, mutta painostaa koko ajan: nopea, lyhyet tauot, harvemmin torjuntaa (jatkuva torjunta oli tylsää)
            e.maxHealth = 700; e.moveSpeedX = 3.9f; e.moveSpeedY = 2.7f; e.depthTolerance = 0.45f; e.attackRange = 2.6f; e.attackCooldown = 0.32f; e.wakeDistance = 18f;
            e.runSpeedMultiplier = 2.2f; e.runDistance = 4f;
            // suorat: J = suora1 (osuma kuvassa 3), S = suora2 (kuvassa 2), K = suora4 (kuvassa 2, kaataa)
            var j = sheets["suora1"]; var s2 = sheets["suora2"]; var hk = sheets["suora4"];
            e.punchSprites = j; e.punchImpactFrame = 3; e.windupTime = 3 * F; e.punchRecoverTime = (j.Length - 3) * F; e.punchDamage = 17; e.punchActiveTime = 0.17f;
            e.punch2Sprites = s2; e.punch2ImpactFrame = 2; e.punch2Damage = 19; e.punch2Knockdown = false; e.punch2Chance = 0.45f;
            e.punch3Sprites = hk; e.punch3ImpactFrame = 2; e.punch3Damage = 28; e.punch3Knockdown = true; e.punch3Chance = 0.3f; e.punch3WindupTime = 0.28f; e.punch3RecoverTime = 0.35f; e.punch3Reach = 3.0f;
            // potkut kombojen osina: P = etupotku (alt, ei kaada), D = suora + korkea potku (kaataa; potku kuvissa 6-8)
            e.altAttackSprites = sheets["potku1"]; e.altImpactFrame = 3; e.altDamage = 20; e.altChance = 0f; e.altExtraWindup = 0.05f; e.altReach = 2.9f; e.altAnimationFrameTime = F; e.altKnockdown = false;
            e.doubleKickSprites = sheets["potku3"]; e.doubleKickImpactFrame = 7; e.doubleKickDamage = 26; e.doubleKickFrameTime = F; e.doubleKickReach = 3.1f; e.doubleKickLaunchX = 12f; e.doubleKickLaunchUp = 6f; e.doubleKickChance = 0f;
            e.combos = new[] { "JSK", "JSP", "JSPD", "JJSPK", "JSJSPD", "PJSK", "JSJSK" }; e.comboChance = 0.9f; e.comboArmor = true; e.comboArmorMinLength = 4;
            // torjunta: kädet ylös ja kasvot peittoon (suojakuvat 0-3)
            var g = sheets["suoja"];
            e.blockSprites = new[] { g[0], g[1], g[2], g[3], g[2], g[1] };
            e.blockTime = 0.35f; e.blockChance = 0.25f; e.maxBlocksInRow = 2; e.guardWhenHurt = true;
            e.hurtSprites = sheets["osuma"]; e.hurtTime = 0.3f;
            e.knockdownSprites = sheets["kaatuminen"]; e.airFrameTime = 0.05f;
            e.landSprites = sheets["maahan"]; e.landFrameTime = 0.09f;
            e.getUpSprites = sheets["nousu"]; e.getUpTime = sheets["nousu"].Length / 14f;
            // erikoisliikkeet (MongolBrain) + omat mekaniikat (NokkamiesBrain): kaasupullon potku, pilli (apurit), rynnäkkö seinään
            var brain = go.AddComponent<NokkamiesBrain>();
            brain.globalCooldown = 0.6f; brain.chancePerSec = 1.6f;
            brain.rageBelow = 0.45f; brain.rageScale = 0.5f; brain.rageSpeed = 1.2f;   // alle 45 %: raivo (erikoisliikkeet tuplasti useammin, nopeampi)
            int syN = sheets["syoksy"].Length, sy2N = sheets["syoksy2"].Length;
            brain.moves = new[]
            {
                // syöksy (syöksylyöntivideo): kuvat 0-12 vauhdinotto, 13-20 syöksy, 21-27 nyrkki ojossa, 28- toipuminen; osumaikkuna 14-25, liuku 13-22
                new MongolBrain.Move { name = "syöksy", sprites = sheets["syoksy"], frameTime = 0.055f, hitFrom = 14, hitTo = Mathf.Min(25, syN - 1), reach = 2.9f, minRange = 4.5f, maxRange = 12f, weight = 0.8f, cooldown = 3.2f, damage = 28, knockdown = true, moveFrom = 13, moveTo = 22, moveSpeed = 10f },
                // iskusyöksy (hyökkäysvideon loppu): käsien levitys, loikka eteen ja suora (osuma kuvissa 5-9)
                new MongolBrain.Move { name = "iskusyöksy", sprites = sheets["syoksy2"], frameTime = 0.06f, hitFrom = 5, hitTo = Mathf.Min(9, sy2N - 1), reach = 2.8f, minRange = 3f, maxRange = 8f, weight = 1.2f, cooldown = 2.8f, damage = 24, knockdown = true, moveFrom = 4, moveTo = 10, moveSpeed = 9f },
                // takapotku (kuvat 78-106, osuma 4-5) ja kolmas suora lähietäisyydellä
                new MongolBrain.Move { name = "takapotku", sprites = sheets["potku2"], frameTime = 0.06f, hitFrom = 4, hitTo = 5, reach = 3.0f, minRange = 1.3f, maxRange = 3.3f, weight = 1.0f, cooldown = 2.2f, damage = 22, knockdown = true },
                new MongolBrain.Move { name = "suora3", sprites = sheets["suora3"], frameTime = 0.06f, hitFrom = 2, hitTo = 3, reach = 2.4f, minRange = 1.2f, maxRange = 2.6f, weight = 1f, cooldown = 1.5f, damage = 17, knockdown = false, impactHold = 0.15f },
            };
            // 1) kaasupullot: etupotku (potku1, osuma kuvassa 3) lähettää pullon; kävely pullon taakse
            brain.kickMove = new MongolBrain.Move { name = "pullopotku", sprites = sheets["potku1"], frameTime = F, hitFrom = 3, hitTo = 4, reach = 2.6f, minRange = 0f, maxRange = 99f, damage = 18, knockdown = false };
            brain.walkSprites = sheets["kavely"]; brain.walkFrameTime = 1f / 12f;
            // 2) pilli (oma video: pilli liivin taskusta, vihellys, takaisin taskuun; 29 kuvaa, ~1,5 s): ääni ja apurit, kun pilli on suulla (kuva 10)
            var pilli = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Sfx/Vartija/pilli.wav");
            brain.whistleMove = new MongolBrain.Move { name = "pilli", sprites = sheets["pilli"], frameTime = 0.05f, hitFrom = 99, hitTo = 99, releaseFrame = 0 };
            brain.whistleFrame = 10;
            brain.whistleClip = pilli;
            brain.crewSpawnX = room.walkMaxX - 1.2f;
            // 3) rynnäkkö: syöksyn vauhdinotto (kuvat 0-12), juoksu seinään, törmäys -> pökerrys (osumakuvat edestakaisin)
            var sy = sheets["syoksy"];
            brain.chargeWindup = sy.Take(Mathf.Min(13, sy.Length)).ToArray();
            brain.chargeMove = new MongolBrain.Move { name = "rynnäkkö", sprites = sheets["juoksu"], frameTime = 0.05f, hitFrom = 99, hitTo = 99 };
            brain.stunMove = new MongolBrain.Move { name = "pökerrys", sprites = sheets["osuma"], frameTime = 0.12f, hitFrom = 99, hitTo = 99, releaseFrame = 0 };
            // apurimallit (ei aktiivisia) työmaan vihuista + kaasupullomalli ja 4 pulloa toimistoon
            var site = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "Rakennustyömaan viholliset");
            var gasSite = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "Rakennustyömaan kaasupullot");
            var tplRoot = new GameObject("Nokkamiehen apurit (mallit)");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(tplRoot, scene);
            Undo.RegisterCreatedObjectUndo(tplRoot, "Nokkamiehen apurit");
            var crewT = new List<GameObject>();
            if (site != null)
                foreach (var prefix in new[] { "Kovis", "Mainari" })   // ei Rekkamiestä: hän on miniboss ja esiintyy aina yksin
                {
                    var src = site.GetComponentsInChildren<Enemy>(true).FirstOrDefault(c => c.name.StartsWith(prefix));
                    if (src == null) continue;
                    var c2 = Object.Instantiate(src.gameObject, tplRoot.transform); c2.name = "Apuri: " + prefix;
                    var ce = c2.GetComponent<Enemy>(); ce.guardOf = null; ce.guards = null; ce.wakeDistance = 99f;
                    c2.SetActive(false); crewT.Add(c2);
                }
            brain.crewTemplates = crewT.ToArray();
            int nGas = 0;
            var gasSrc = gasSite != null ? gasSite.GetComponentInChildren<GasCylinder>(true) : null;
            if (gasSrc != null)
            {
                var gt = Object.Instantiate(gasSrc.gameObject, tplRoot.transform); gt.name = "Kaasupullo (malli)"; gt.SetActive(false);
                brain.cylinderTemplate = gt;
                var gasRoot = new GameObject("Toimiston kaasupullot");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gasRoot, scene);
                Undo.RegisterCreatedObjectUndo(gasRoot, "Toimiston kaasupullot");
                foreach (var (fx, fd) in new[] { (0.12f, 0.75f), (0.3f, 0.3f), (0.62f, 0.7f), (0.85f, 0.35f) })
                {
                    var gc = Object.Instantiate(gasSrc.gameObject, gasRoot.transform); gc.name = "Toimiston kaasupullo " + (++nGas);
                    gc.transform.position = new Vector3(Mathf.Lerp(room.walkMinX + 1f, room.walkMaxX - 1f, fx), Mathf.Lerp(room.minDepthY, room.maxDepthY, fd), 0f);
                }
            }
            float x = Mathf.Lerp(room.walkMinX, room.walkMaxX, 0.42f), y = Mathf.Lerp(room.maxDepthY, room.minDepthY, 0.5f);
            go.transform.position = new Vector3(x, y, 0f);
            EditorUtility.SetDirty(e); EditorUtility.SetDirty(brain);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Nokkamies lisätty työmaakonttorin yläkertaan (pomo, 700 elämää, raivo alle 45 %). Uutta: potkii kaasupulloja (" + nGas + " toimistossa), pilli kutsuu apurit (" + crewT.Count + " mallia), rynnäkkö seinään + pökerrys. Lisäksi: suorat J/S/K (K kaataa), kombot, etupotku, suora + korkea potku, syöksy juoksusta, iskusyöksy, takapotku, torjunta 25 % (max 2 peräkkäin). Äänet väliaikaisesti Gestapon.");
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
