using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Rex (valikko 365): E-osaston yläkerran sellin E13 sisätila (sama sellikuva kuin D-sellit) ja Rex sinne odottamaan.
/// Rex vapautuu, kun hero painaa E hänen vieressään (RexAlly), ja taistelee sen jälkeen vihuja vastaan heron rinnalla (Enemy.ally).
/// Kuvat: Assets/Sprites/Viholliset/rex_*.png (Tools/RexAssets/make_rex_sheets.py ja make_rex_block.py). Hurt/kaatuminen/nousu puuttuvat vielä.</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/365. Vankila: Rex (sellin E13 sisätila, Rex taistelee heron rinnalla)", false, 365)]
    static void AddPrisonRex()
    {
        if (PlayModeBlocked()) return;
        var s = new Dictionary<string, Sprite[]>();
        foreach (var n in new[] { "idle", "kavely", "juoksu", "isku1", "isku2", "isku3", "suojaus", "potku1", "potku2", "potku3", "ylapotku", "matalapotku", "osuma", "kaatuminen", "nousu", "alakoukku" })
        {
            var sp = LoadWalkerSprites("rex_" + n);
            if (sp == null || sp.Length == 0) { Info("Puuttuu tai ei leikkaudu: rex_" + n + ".png (Assets/Sprites/Viholliset). Aja Tools/RexAssets/make_rex_*.py."); return; }
            s[n] = sp;
        }
        const string T = "Assets/Sprites/Taustat/";
        if (!File.Exists(T + "vankila_selli.png")) { Info("Puuttuu vankila_selli.png."); return; }
        if (!File.Exists(PrisonScenePath)) { Info("Aja ensin valikot 353–363."); return; }
        var scene = EditorSceneManager.GetSceneByPath(PrisonScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(PrisonScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var hall = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a != null && a.gameObject.scene == scene && a.areaName == "Vankilan E-osasto");
            if (hall == null) { Info("E-osastoa ei löytynyt: aja valikko 358."); return; }
            var corrDoor = Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x != null && x.gameObject.scene == scene && x.name == "E-osasto: yläselli E13");
            if (corrDoor == null) { Info("E-osaston oven 'yläselli E13' ei löytynyt: aja valikko 358."); return; }
            foreach (var g in scene.GetRootGameObjects().Where(g => g.name == "Vankilan selli E13" || g.name == "Alue: Vankilan selli E13").ToArray()) Undo.DestroyObjectImmediate(g);
            foreach (var dd in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x != null && x.gameObject.scene == scene && x.name == "Selli E13: ulos").ToArray()) Undo.DestroyObjectImmediate(dd.gameObject);

            float H = 2f * CamHalf, cy = CamY, top = cy + CamHalf;
            float ppu = CellImgH / H, imgW = CellImgW / ppu, W = 1672f / (941f / H);
            float X0 = PrisonCellX0 + 20 * PrisonCellStep;   // D-sellien (D1–D8) jälkeen
            var spr = ImportShipSprite(T + "vankila_selli.png", ppu, false, SpriteAlignment.Center);
            if (spr == null) { Info("Sellikuvan tuonti epäonnistui."); return; }
            var root = new GameObject("Vankilan selli E13");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Rex");
            var sr = new GameObject("Selli E13").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(root.transform, false); sr.sprite = spr; sr.sortingOrder = -10000;
            sr.transform.position = new Vector3(X0 + imgW * 0.5f, cy, 0f);

            var cell = ReuseArea("Alue: Vankilan selli E13");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cell.gameObject, scene);
            cell.areaName = "Vankilan selli E13"; cell.useSidewalk = false;
            cell.maxDepthY = top - 0.76f * H; cell.minDepthY = top - 0.985f * H;
            cell.walkMinX = X0 + 1.5f; cell.walkMaxX = X0 + imgW - 1.5f;
            cell.camMinX = X0 + W * 0.5f / CellZoom; cell.camMaxX = Mathf.Max(cell.camMinX, X0 + imgW - W * 0.5f / CellZoom);   // lähikuva: hahmot isommiksi sellissä
            cell.camSize = CamHalf / CellZoom; cell.camRiseY = 0f; cell.camOffsetY = CamHalf / CellZoom - CamHalf;   // kuvan alareuna pysyy paikallaan (lattia) cell.backScale = 1f; cell.depthLimits = new Vector2[0];
            cell.music = hall.music; cell.musicGain = hall.musicGain;

            // ovet: käytävän yläkerran ovi E13 -> sellin sisälle (ovi oikealla), sisältä takaisin käytävälle
            var doorRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Vankilan ovet");
            float inDoorX = X0 + 0.925f * imgW, midCell = Mathf.Lerp(cell.maxDepthY, cell.minDepthY, 0.5f);
            corrDoor.target = cell; corrDoor.spawnPoint = new Vector2(inDoorX - 3.2f, midCell);
            corrDoor.requiredClue = "Vankila: sellin E13 avain"; corrDoor.lockedClueText = "Sellin ovi on lukossa. Avain on valvontahuoneen kaapissa.";   // avain valvontahuoneesta (valikko 375)
            EditorUtility.SetDirty(corrDoor);
            var back = new GameObject("Selli E13: ulos").AddComponent<Door>();
            back.transform.SetParent(doorRoot != null ? doorRoot.transform : root.transform, false); back.transform.position = new Vector3(inDoorX, cell.maxDepthY, 0f);
            back.prompt = "Ulos käytävälle"; back.here = cell; back.target = hall; back.spawnPoint = new Vector2(corrDoor.transform.position.x, corrDoor.transform.position.y); back.halfWidth = 1.4f; back.maxDistanceFromWall = 2.4f;
            EditorUtility.SetDirty(cell);

            // Rex
            const float F = 0.045f;
            var go = new GameObject("Rex");
            go.transform.SetParent(root.transform, false);
            var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
            var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
            var e = go.AddComponent<Enemy>();
            e.body = visual; e.shadow = shadow; visual.sprite = s["idle"][0];
            e.displayName = "Rex";
            e.visualScale = RexScale; e.attackArtScale = 1f; e.knockArtScale = 1f; e.walkArtScale = 1f;
            e.idleSprites = s["idle"]; e.idleFrameTime = 1f / 12f;
            e.walkSprites = s["kavely"]; e.walkFrameTime = 1f / 12f;
            e.runSprites = s["juoksu"]; e.runFrameTime = 1f / 24f; e.runSpeedMultiplier = 1.9f; e.runDistance = 5f;
            e.maxHealth = 520; e.moveSpeedX = 3.6f; e.moveSpeedY = 2.5f; e.depthTolerance = 0.45f; e.attackCooldown = 0.5f; e.wakeDistance = 0f;
            e.comboChance = 0.6f; e.comboWindupScale = 0.6f; e.comboRecoverFraction = 0.6f;
            e.attackRange = 2.4f;
            // J = tupla-jabi, S = jabi, K = takasuora (kaataa); osumakuvat videosta
            e.punchSprites = s["isku1"]; e.punchImpactFrame = 3; e.secondImpactFrame = 5; e.windupTime = 3 * F; e.punchRecoverTime = (s["isku1"].Length - 3) * F; e.punchActiveTime = 0.14f; e.punchDamage = 10;
            e.punch2Sprites = s["isku2"]; e.punch2ImpactFrame = 3; e.punch2Damage = 12; e.punch2Knockdown = false; e.punch2Chance = 0.4f;
            e.punch3Sprites = s["isku3"]; e.punch3ImpactFrame = 3; e.punch3Damage = 22; e.punch3Knockdown = true; e.punch3IsPunch = true; e.punch3Chance = 0.3f; e.punch3WindupTime = 3 * F; e.punch3RecoverTime = (s["isku3"].Length - 3) * F; e.punch3Reach = 3.0f;
            // potkut: vaihtoehtoinen isku (alt), arvotaan korkea potku 1, korkea potku 2 tai etupotku
            e.altChance = 0.35f; e.altExtraWindup = 0.1f;
            e.altVariants = new[]
            {
                new Enemy.AltVariant { name = "Korkea potku 1", sprites = s["potku1"], impactFrame = 4, frameTime = F, damage = 14, reach = 3.4f, knockdown = false, weight = 1f },
                new Enemy.AltVariant { name = "Korkea potku 2", sprites = s["potku2"], impactFrame = 4, frameTime = F, damage = 16, reach = 3.4f, knockdown = true, weight = 1f },
                new Enemy.AltVariant { name = "Lennättävä yläpotku", sprites = s["ylapotku"], impactFrame = 3, frameTime = F, damage = 20, reach = 3.2f, knockdown = true, knockSpeed = 9f, knockUp = 8f, weight = 0.6f },
                new Enemy.AltVariant { name = "Matala potku", sprites = s["matalapotku"], impactFrame = 4, frameTime = F, damage = 11, reach = 3.0f, knockdown = false, weight = 1f },
                new Enemy.AltVariant { name = "Alakoukku", sprites = s["alakoukku"], impactFrame = 4, frameTime = F * 1.2f, damage = 22, reach = 2.5f, knockdown = true, knockSpeed = 6f, knockUp = 10f, weight = 0.8f },
                new Enemy.AltVariant { name = "Etupotku", sprites = s["potku3"], impactFrame = 8, frameTime = F, damage = 18, reach = 3.0f, knockdown = true, weight = 1f },
            };
            // numero = alt-variantti (järjestys altVariants-listassa): 0 korkea potku 1, 1 korkea potku 2, 2 lennättävä yläpotku, 3 matala potku, 4 alakoukku, 5 etupotku
            e.combos = new[] { "JS", "JK", "JSK", "J", "JS2", "S3K", "J32" };   // JS2 = jabit + lennättävä yläpotku päätteeksi, S3K = jabi + matala potku + takasuora, J32 = tupla-jabi + matala potku + lennättävä yläpotku
            e.altAttackSprites = s["potku1"]; e.altImpactFrame = 4; e.altAnimationFrameTime = F; e.altDamage = 14; e.altReach = 3.4f;   // alt-isku käyttöön (variantti arvotaan joka iskulla)
            e.blockSprites = s["suojaus"]; e.blockChance = 0.25f;
            e.hurtSprites = s["osuma"]; e.hurtTime = 0.32f;
            e.knockdownSprites = s["kaatuminen"]; e.airFrameTime = 0.05f;   // heitettynä näytetään samat kaatumiskuvat
            e.getUpSprites = s["nousu"]; e.getUpTime = s["nousu"].Length / 14f;
            // äänet toistaiseksi portsarin: kipu = big thug gasp, huudot = portsarin poke (omat äänet tulevat myöhemmin)
            e.hurtSounds = LoadClips("Assets/Audio/big thug", "gasp"); e.hurtVolume = 0.99f;
            string bvoice = AssetDatabase.GetSubFolders("Assets/Audio").FirstOrDefault(f => Path.GetFileName(f).ToLowerInvariant().StartsWith("portsari"));
            e.tauntSounds = bvoice != null ? LoadClips(bvoice, "poke") : new AudioClip[0];
            e.ally = true; e.fightsEveryone = false; e.huntsBrawlers = false; e.brawlsWithOthers = false; e.joinsFightWhenSquadComes = false;
            var ra = go.AddComponent<RexAlly>(); ra.recruitRange = 2.8f;
            go.transform.position = new Vector3(X0 + imgW * 0.35f, Mathf.Lerp(cell.maxDepthY, cell.minDepthY, 0.5f), 0f);
            EditorUtility.SetDirty(e); EditorUtility.SetDirty(ra);

            var listPath = PrisonScenePath.Replace(".unity", ".areas.txt");
            var names = new List<string>(File.Exists(listPath) ? File.ReadAllLines(listPath) : new string[0]);
            if (!names.Contains("Vankilan selli E13")) names.Add("Vankilan selli E13");
            File.WriteAllLines(listPath, names);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Sellin E13 sisätila ja Rex rakennettu (x " + X0.ToString("0") + "…). Ovi E13 (E-osaston yläkerta) on auki toistaiseksi. Rex odottaa sellissä; E hänen vieressään vapauttaa hänet, jolloin hän taistelee vihuja vastaan heron rinnalla (J tupla-jabi, S jabi, K takasuora, suojaus), seuraa heroa alueelta toiselle ja saa elämäpalkin ruudulle.\nPuuttuu vielä: Rexin omat äänet (nyt portsarin), avaimen hankinta ja Rexin repliikit. Ajamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
