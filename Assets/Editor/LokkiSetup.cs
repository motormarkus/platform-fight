using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Lokki (valikko 397): ensiesiintyminen Aavikon juna-asemalla (alue "Juna-asema"): Lokki tulee ruutuun oikealta, lyhyt puhe, taistelu jossa hero ei voi voittaa, hero pieksetään,
/// Lokki sanoo loppurepliikin ja pakenee oikealle (LokkiEncounter). Lokki on nopein kaikista pomoista ja vihuista (7,5 / 5,2; juoksu x2).
/// Kuvat Assets/Sprites/Viholliset/lokki_*.png (Tools/LokkiAssets/make_lokki_sheets.py). Äänet puuttuvat (repliikit tekstinä).
/// </summary>
public static partial class BeatEmUpSetup
{
    const string LokkiAreaName = "Juna-asema";

    [MenuItem("Beat em up/397. Aavikko: Lokki ensiesiintyy juna-asemalla (tulee oikealta, lyhyt puhe, voittamaton, pako)", false, 397)]
    static void AddLokki()
    {
        if (PlayModeBlocked()) return;
        var sheets = new Dictionary<string, Sprite[]>();
        foreach (var n in new[] { "idle", "kavely", "juoksu", "suora1", "suora2", "suora3", "suora4", "potku1", "potku2", "potku3", "sarja1", "sarja2", "yla", "aalto", "suoja", "polvi", "vaisto", "osuma", "kaatuminen", "maahan", "nousu", "heitto" })
        {
            var sp = LoadWalkerSprites("lokki_" + n);
            if (sp == null || sp.Length == 0) { Info("Puuttuu tai ei leikkaudu: lokki_" + n + ".png (Assets/Sprites/Viholliset). Aja Tools/LokkiAssets/make_lokki_sheets.py."); return; }
            sheets[n] = sp;
        }
        const string scenePath = "Assets/Scenes/Areas/Aavikko.unity";
        if (!File.Exists(scenePath)) { Info("Aavikon scenea ei löytynyt."); return; }
        var scene = EditorSceneManager.GetSceneByPath(scenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var area = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a != null && a.gameObject.scene == scene && a.areaName == LokkiAreaName);
            if (area == null) { Info("Aluetta \"" + LokkiAreaName + "\" ei löytynyt Aavikon scenestä (valikko 123)."); return; }
            foreach (var n in new[] { "Lokki", "Lokin ensiesiintyminen" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);

            var go = new GameObject("Lokki");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, "Lokki");
            var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
            var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
            var e = go.AddComponent<Enemy>();
            const float F = 1f / 22f;   // iskukuvien kuva-aika: nopea (kuvat 12 fps -> yli 20 fps)
            e.body = visual; e.shadow = shadow; visual.sprite = sheets["idle"][0];
            e.displayName = "Lokki"; e.isBoss = true;
            e.visualScale = 1f; e.attackArtScale = 1f; e.knockArtScale = 1f; e.walkArtScale = 1f;
            e.idleSprites = sheets["idle"]; e.idleFrameTime = 1f / 12f;
            e.walkSprites = sheets["kavely"]; e.walkFrameTime = 1f / 16f;
            e.runSprites = sheets["juoksu"]; e.runFrameTime = 1f / 24f; e.runSpeedMultiplier = 2.0f; e.runDistance = 3f;
            // nopein kaikista: Koe-olento 5,6 / 4,0, Timppa 4,6 / 3,2
            e.maxHealth = 1500; e.moveSpeedX = 7.5f; e.moveSpeedY = 5.2f; e.depthTolerance = 0.5f; e.attackRange = 2.3f; e.attackCooldown = 0.15f; e.wakeDistance = 99f;
            // suorat: J = suora1 (osuma kuvassa 2), S = suora2 (kuvassa 2), K = suora4 (kaataa; kuvassa 2)
            var j = sheets["suora1"]; var s2 = sheets["suora2"]; var hk = sheets["suora4"];
            e.punchSprites = j; e.punchImpactFrame = 2; e.windupTime = 2 * F; e.punchRecoverTime = (j.Length - 2) * F; e.punchDamage = 14; e.punchActiveTime = 0.12f;
            e.punch2Sprites = s2; e.punch2ImpactFrame = 2; e.punch2Damage = 16; e.punch2Knockdown = false; e.punch2Chance = 0.5f;
            e.punch3Sprites = hk; e.punch3ImpactFrame = 2; e.punch3Damage = 24; e.punch3Knockdown = true; e.punch3Chance = 0.3f; e.punch3WindupTime = 0.15f; e.punch3RecoverTime = 0.2f; e.punch3Reach = 3.0f;
            // etupotku (P, osuma kuvissa 4-5) ja kierto + korkea potku (D, osuma kuvissa ~17-20)
            e.altAttackSprites = sheets["potku1"]; e.altImpactFrame = 4; e.altDamage = 18; e.altChance = 0f; e.altExtraWindup = 0.03f; e.altReach = 3.0f; e.altAnimationFrameTime = F; e.altKnockdown = false;
            e.doubleKickSprites = sheets["potku3"]; e.doubleKickImpactFrame = 17; e.doubleKickDamage = 26; e.doubleKickFrameTime = F; e.doubleKickReach = 3.3f; e.doubleKickLaunchX = 12f; e.doubleKickLaunchUp = 6f; e.doubleKickChance = 0f;
            // kombo noin 10 iskua nopeasti kuten herojen ketju
            e.combos = new[] { "JSJSJJSPKD", "JJSJSPJSKD", "JSJJSJSPSD", "JSJSJSJPKD" }; e.comboChance = 0.9f; e.comboWindupScale = 0.3f; e.comboRecoverFraction = 0.3f; e.comboGap = 0.02f; e.comboArmor = true; e.comboArmorMinLength = 3;
            // ei oikeaa torjuntaa/osumakuvia: heron iskut eivät osu (LokkiBrain näyttää suoja-asennon), kaatumiskuvat varalta idleä
            e.blockChance = 0f; e.maxBlocksInRow = 0;
            e.hurtSprites = sheets["osuma"]; e.hurtTime = 0.3f;
            e.knockdownSprites = sheets["kaatuminen"]; e.airFrameTime = 0.05f;
            e.landSprites = sheets["maahan"]; e.landFrameTime = 0.09f;
            e.getUpSprites = sheets["nousu"]; e.getUpTime = sheets["nousu"].Length / 16f;
            // erikoisliikkeet (LokkiBrain): sarjat, takapotku, kämmeniskun aalto, heilautus, kierto + korkea potku; kuvat joka toinen videon ruutu
            var brain = go.AddComponent<LokkiBrain>();
            brain.globalCooldown = 0.5f; brain.chancePerSec = 1.4f; brain.invulnerable = false; brain.reactive = true; brain.landKnockdown = true;
            brain.fleeMove = new MongolBrain.Move { name = "pako", sprites = sheets["juoksu"], frameTime = 1f / 24f, hitFrom = 99, hitTo = 99, reach = 0f, minRange = 999f, maxRange = 0f, weight = 0f, cooldown = 0f, damage = 0, moveFrom = 0, moveTo = sheets["juoksu"].Length - 1, moveSpeed = 20f };
            // heitto-counter: kuvat (heitto, 8 kuvaa): 0 asento, 1 kurotus kädestä, 2 veto, 3 käännös, 4 selin (heitto), 5-6 matala kurotus jalasta, 7 asento
            Sprite[] H(params int[] idx) => idx.Select(i => sheets["heitto"][i]).ToArray();
            brain.catchHand = new MongolBrain.Move { name = "heitto-käsi", sprites = H(0, 1, 2, 3, 4, 7), frameTime = 0.09f, hitFrom = 99, hitTo = 99, reach = 0f, minRange = 999f, maxRange = 0f, weight = 0f, cooldown = 0f, damage = 0 };
            brain.catchLeg = new MongolBrain.Move { name = "heitto-jalka", sprites = H(0, 5, 6, 3, 4, 7), frameTime = 0.09f, hitFrom = 99, hitTo = 99, reach = 0f, minRange = 999f, maxRange = 0f, weight = 0f, cooldown = 0f, damage = 0 };
            brain.handGrabPos = 1; brain.handThrowPos = 4; brain.legGrabPos = 2; brain.legThrowPos = 4; brain.throwDamage = 26; brain.throwSpeed = 9f; brain.throwUp = 6f;
            brain.dodgeChance = 0.35f; brain.blockChance = 0.25f; brain.catchChance = 0.2f; brain.landChance = 0.2f;
            float ft = 0.045f;
            // puolustus kuten Deaconilla: väistö ylös/alas (syvyys) tai torjunta, heti counter (suora + kaatava lopetus). Väistö: lokki_vaisto (5 kuvaa, syvä painuminen kuvassa 3).
            brain.blockMove = new MongolBrain.Move { name = "torjunta", sprites = sheets["suoja"], frameTime = 0.05f, hitFrom = 99, hitTo = 99, reach = 0f, minRange = 999f, maxRange = 0f, weight = 0f, cooldown = 0f, damage = 0 };
            brain.dodgeMove = new MongolBrain.Move { name = "väistö", sprites = sheets["vaisto"], frameTime = 0.07f, hitFrom = 99, hitTo = 99, reach = 0f, minRange = 999f, maxRange = 0f, weight = 0f, cooldown = 0f, damage = 0 };
            brain.counterJab = new MongolBrain.Move { name = "counter-suora", sprites = sheets["suora2"], frameTime = 0.04f, hitFrom = 2, hitTo = 3, reach = 2.8f, minRange = 0f, maxRange = 99f, weight = 0f, cooldown = 0f, damage = 14, knockdown = false, moveFrom = 0, moveTo = 2, moveSpeed = 9f, impactHold = 0f };
            brain.counterFinishers = new[]
            {
                new MongolBrain.Move { name = "counter-kierto", sprites = sheets["potku3"], frameTime = 0.04f, hitFrom = 17, hitTo = 20, reach = 3.3f, minRange = 0f, maxRange = 99f, weight = 0f, cooldown = 0f, damage = 28, knockdown = true },
                new MongolBrain.Move { name = "counter-takapotku", sprites = sheets["potku2"], frameTime = 0.04f, hitFrom = 4, hitTo = 6, reach = 3.2f, minRange = 0f, maxRange = 99f, weight = 0f, cooldown = 0f, damage = 26, knockdown = true },
                new MongolBrain.Move { name = "counter-kova-suora", sprites = sheets["suora4"], frameTime = 0.04f, hitFrom = 2, hitTo = 3, reach = 3.0f, minRange = 0f, maxRange = 99f, weight = 0f, cooldown = 0f, damage = 26, knockdown = true, moveFrom = 0, moveTo = 2, moveSpeed = 7f, impactHold = 0f },
            };
            brain.moves = new[]
            {
                new MongolBrain.Move { name = "sarja", sprites = sheets["sarja1"], frameTime = ft, hitFrom = 3, hitTo = 15, reach = 2.6f, minRange = 1.2f, maxRange = 2.8f, weight = 1.2f, cooldown = 2.2f, damage = 22, knockdown = false },
                // polvi-isku (kuvat 0-4, polvi ylhäällä kuvissa 3-4)
                new MongolBrain.Move { name = "polvi", sprites = sheets["polvi"], frameTime = ft, hitFrom = 3, hitTo = 4, reach = 2.1f, minRange = 0.6f, maxRange = 2.3f, weight = 1.1f, cooldown = 2f, damage = 18, knockdown = false },
                new MongolBrain.Move { name = "sarja2", sprites = sheets["sarja2"], frameTime = ft, hitFrom = 4, hitTo = 16, reach = 2.6f, minRange = 1.2f, maxRange = 2.8f, weight = 1f, cooldown = 2.4f, damage = 22, knockdown = false },
                new MongolBrain.Move { name = "takapotku", sprites = sheets["potku2"], frameTime = ft, hitFrom = 4, hitTo = 6, reach = 3.1f, minRange = 1.2f, maxRange = 3.4f, weight = 1f, cooldown = 2.8f, damage = 26, knockdown = true },
                new MongolBrain.Move { name = "heilautus", sprites = sheets["yla"], frameTime = ft, hitFrom = 6, hitTo = 9, reach = 2.7f, minRange = 1f, maxRange = 2.9f, weight = 0.9f, cooldown = 3.2f, damage = 24, knockdown = true },
                new MongolBrain.Move { name = "kierto", sprites = sheets["potku3"], frameTime = ft, hitFrom = 17, hitTo = 20, reach = 3.3f, minRange = 1.2f, maxRange = 3.6f, weight = 1f, cooldown = 3.5f, damage = 28, knockdown = true },
                // kämmeniske: valkoinen iskuaalto kuvissa 7-9 (videon ruudut 60-64), ulottuu kauas (3-7 yks.)
                new MongolBrain.Move { name = "aalto", sprites = sheets["aalto"], frameTime = ft, hitFrom = 7, hitTo = 9, reach = 7f, minRange = 3.2f, maxRange = 7.5f, weight = 1.4f, cooldown = 4f, damage = 30, knockdown = true, impactHold = 0.08f },
            };
            go.transform.position = new Vector3(area.walkMaxX + 20f, Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.5f), 0f);
            go.SetActive(false);   // ilmestyy kohtauksessa
            EditorUtility.SetDirty(e); EditorUtility.SetDirty(brain);

            var enc = new GameObject("Lokin ensiesiintyminen");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(enc, scene);
            Undo.RegisterCreatedObjectUndo(enc, "Lokin ensiesiintyminen");
            var le = enc.AddComponent<LokkiEncounter>();
            le.area = area; le.lokki = e;
            float width = area.walkMaxX - area.walkMinX;
            le.triggerX = width > 30f ? area.walkMaxX - 22f : area.walkMinX + width * 0.4f;   // kamera ehtii näyttää oikean reunan ennen Lokin tuloa
            EditorUtility.SetDirty(le);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Lokki lisätty alueelle \"" + LokkiAreaName + "\" (x " + area.walkMinX.ToString("0") + "–" + area.walkMaxX.ToString("0") + ", laukaisu x " + le.triggerX.ToString("0") + "): tulee oikealta, lyhyt puhe (teksti), taistelu (Lokki väistää ylös/alas, torjuu, ottaa kädestä/jalasta kiinni ja heittää tai tekee heti counterin; osumia voi saada, 1500 hp; vetäytyy 25 %:lla; hero ei kuole), 10 iskun kombot, pieksennän jälkeen loppurepliikki ja pako oikealle; hero saa 25 % energiaa. Nopein kaikista (7,5 / 5,2).");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    /// <summary>Lokki (valikko 397).</summary>
    public static void RunLokkiBatch()
    {
        batch = true;
        try { Step("397 Lokki", AddLokki); }
        finally { batch = false; }
    }
}
