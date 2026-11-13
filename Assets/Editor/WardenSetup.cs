using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Vankilanjohtaja Warden: huone (valikko 370) ja pomo (valikko 371).
/// Huone: kaksi kuvaa (Assets/Sprites/Taustat/vankila_johtaja_1.png vasen, vankila_johtaja_2.png oikea; ikkunat läpinäkyviä, taivasparallaksi). Ovi: henkilökunnan tilojen yläkerran puuovet ("Henkilökunta: Wardenin huone"),
/// nyt ilman avainta (avainta ei voi vielä saada mistään).
/// Pomo: kuvat Assets/Sprites/Viholliset/warden_*.png (Tools/PrisonAssets/make_warden_*.py). Liikkeet: J jabi, S takasuora, K haymaker (kaataa), alt: 0 etupotku, 1 roundhouse (kaataa),
/// 2 uppercut-hyppy (lataus, hyppy, ei katkea: hyperArmor); heitto (nosto hitaasti, ojennus kiihtyy; lentää matalalla eteenpäin); torjunta.</summary>
public static partial class BeatEmUpSetup
{
    const float PrisonWardenX0 = 95000f;

    [MenuItem("Beat em up/370. Vankila: Wardenin huone (kaksi kuvaa, ovi henkilökunnan yläkerrasta)", false, 370)]
    static void AddWardenRoom()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        foreach (var f in new[] { "vankila_johtaja_1", "vankila_johtaja_2", "kone_taivas" })
        {
            if (!File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat)."); return; }
            AssetDatabase.ImportAsset(T + f + ".png", ImportAssetOptions.ForceUpdate);
        }
        if (!File.Exists(PrisonScenePath)) { Info("Aja ensin valikot 353–362."); return; }
        var scene = EditorSceneManager.GetSceneByPath(PrisonScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(PrisonScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var areas = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == scene).ToArray();
            var staff = areas.FirstOrDefault(a => a.areaName == "Vankilan henkilökunta");
            if (staff == null) { Info("Henkilökunnan tiloja ei löytynyt: aja valikko 362."); return; }
            var staffDoor = Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x != null && x.gameObject.scene == scene && x.name == "Henkilökunta: Wardenin huone");
            if (staffDoor == null) { Info("Wardenin oven paikkaa ei löytynyt (\"Henkilökunta: Wardenin huone\"): aja valikko 362."); return; }
            foreach (var n in new[] { "Vankilan Wardenin huone", "Alue: Vankilan Wardenin huone" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);
            foreach (var dd in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x != null && x.gameObject.scene == scene && x.name.StartsWith("Wardenin huone:")).ToArray())
                Undo.DestroyObjectImmediate(dd.gameObject);

            float H = 2f * CamHalf, cy = CamY, top = cy + CamHalf, X0 = PrisonWardenX0;
            float ppu = 922f / H, W = 1672f / (941f / H);       // ppu: 2000 px:n mittakaava (kuten muut vankilan alueet)
            float ppuImg = 1844f / H, imgW = 5120f / ppuImg;    // kuvat 5120 x 1844
            float totalW = 2f * imgW;
            float wx(float px) => X0 + px * 1.28f / ppu;        // px = kuvakoordinaatti (2000 px:n mittakaava, kuvat peräkkäin)
            var root = new GameObject("Vankilan Wardenin huone");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Vankila");
            for (int i = 0; i < 2; i++)
            {
                var sp = ImportShipSprite(T + "vankila_johtaja_" + (i + 1) + ".png", ppuImg, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan tuonti epäonnistui (johtajan huone " + (i + 1) + ")."); return; }
                var sr = new GameObject("Johtajan huone " + (i + 1)).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10000;
                sr.transform.position = new Vector3(X0 + imgW * (i + 0.5f), cy, 0f);
            }
            var room = ReuseArea("Alue: Vankilan Wardenin huone");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(room.gameObject, scene);
            room.areaName = "Vankilan Wardenin huone"; room.useSidewalk = false;
            room.maxDepthY = top - 0.78f * H; room.minDepthY = top - 0.985f * H;   // lattia alkaa seinän jalkalistan alta (~0,73 kuvan korkeudesta); työpöydän ja nojatuolien etupuolella
            room.walkMinX = X0 + 1.5f; room.walkMaxX = X0 + totalW - 1.5f;
            room.camMinX = X0 + W * 0.5f; room.camMaxX = Mathf.Max(room.camMinX, X0 + totalW - W * 0.5f);
            room.camSize = CamHalf; room.camRiseY = 0f; room.camOffsetY = 0f; room.backScale = 1f; room.depthLimits = new Vector2[0];
            room.music = staff.music; room.musicGain = staff.musicGain;

            // ikkunat läpinäkyviä: taivasparallaksi
            {
                var ssp = ImportShipSprite(T + "kone_taivas.png", ppu, false, SpriteAlignment.Center);
                if (ssp != null)
                {
                    float scale = 1.25f, left = room.camMinX - W * 0.5f;
                    var sr = new GameObject("Johtajan huoneen taivas").AddComponent<SpriteRenderer>();
                    sr.transform.SetParent(root.transform, false); sr.sprite = ssp; sr.sortingOrder = -10100;
                    sr.transform.position = new Vector3(left + ssp.bounds.size.x * scale * 0.5f, cy, 0f);
                    sr.transform.localScale = new Vector3(scale, scale, 1f);
                    var pl = sr.gameObject.AddComponent<ParallaxLayer>();
                    pl.factor = 0.02f; pl.startCamX = room.camMinX; pl.minX = room.camMinX - 2f; pl.maxX = room.camMaxX + 2f;
                }
            }

            var doorRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Vankilan ovet");
            if (doorRoot == null) { doorRoot = new GameObject("Vankilan ovet"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, scene); }
            Door MakeDoor(string name, string prompt, Area here, Area target, Vector3 pos, Vector2 spawn)
            {
                var d = new GameObject(name).AddComponent<Door>();
                d.transform.SetParent(doorRoot.transform, false); d.transform.position = pos;
                d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = spawn; d.halfWidth = 2.4f; d.maxDistanceFromWall = 2.4f;
                d.onGround = true; d.groundHalfDepth = 1.6f;
                return d;
            }
            float midRoom = Mathf.Lerp(room.maxDepthY, room.minDepthY, 0.5f), doorY = room.maxDepthY - 0.3f, inX = wx(160f);
            var tf = scene.GetRootGameObjects().Select(g => g.GetComponent<TwoFloorCorridor>()).FirstOrDefault(c => c != null && c.area == staff);
            float midUp = tf != null ? 0.5f * (tf.upperMin + tf.upperMax) : Mathf.Lerp(staff.maxDepthY, staff.minDepthY, 0.5f);
            // henkilökunnan yläkerran puuovet -> huoneeseen (ei avainta); huoneen ovi -> takaisin yläkertaan
            Undo.RecordObject(staffDoor, "Wardenin ovi");
            staffDoor.target = room; staffDoor.requiredClue = ""; staffDoor.lockedClueText = ""; staffDoor.prompt = "Mene Wardenin huoneeseen"; staffDoor.spawnPoint = new Vector2(inX + 3.4f, midRoom);
            EditorUtility.SetDirty(staffDoor);
            MakeDoor("Wardenin huone: ovi ulos", "Mene ulos", room, staff, new Vector3(inX, doorY, 0f), new Vector2(staffDoor.transform.position.x - 3.4f, midUp));

            var listPath = PrisonScenePath.Replace(".unity", ".areas.txt");
            var names = new List<string>(File.Exists(listPath) ? File.ReadAllLines(listPath) : new string[0]);
            if (!names.Contains("Vankilan Wardenin huone")) names.Add("Vankilan Wardenin huone");
            File.WriteAllLines(listPath, names);
            EditorUtility.SetDirty(room);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Wardenin huone rakennettu (x " + X0.ToString("0") + "–" + (X0 + totalW).ToString("0") + "): henkilökunnan yläkerran puuovet vievät huoneeseen (ei avainta) ja huoneen vasen kaksoisovi takaisin.\nLattia: työpöydän ja nojatuolien edessä. Säädä tarvittaessa room.maxDepthY/minDepthY. Seuraavaksi valikko 371 (Warden pomoksi). Ajamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    [MenuItem("Beat em up/371. Vankila: Warden pomoksi (jabi, takasuora, haymaker, potkut, uppercut-hyppy, heitto, torjunta)", false, 371)]
    static void AddWardenBoss()
    {
        if (PlayModeBlocked()) return;
        var s = new Dictionary<string, Sprite[]>();
        foreach (var n in new[] { "idle", "kavely", "juoksu", "isku1", "isku2", "isku3", "potku1", "potku2", "osuma", "kaatuminen", "nousu", "suojaus", "uppercut", "heitto" })
        {
            var sp = LoadWalkerSprites("warden_" + n);
            if (sp == null || sp.Length == 0) { Info("Puuttuu tai ei leikkaudu: warden_" + n + ".png (Assets/Sprites/Viholliset). Aja Tools/PrisonAssets/make_warden_*.py."); return; }
            s[n] = sp;
        }
        if (!File.Exists(PrisonScenePath)) { Info("Aja ensin valikot 353–370."); return; }
        var scene = EditorSceneManager.GetSceneByPath(PrisonScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(PrisonScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var room = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a != null && a.gameObject.scene == scene && a.areaName == "Vankilan Wardenin huone");
            if (room == null) { Info("Wardenin huonetta ei löytynyt: aja ensin valikko 370."); return; }
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Warden").ToArray()) Undo.DestroyObjectImmediate(old);
            var go = new GameObject("Warden");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, "Warden");
            var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
            var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
            var e = go.AddComponent<Enemy>();
            const float F = 0.05f;   // iskukuvien kuva-aika
            e.body = visual; e.shadow = shadow; visual.sprite = s["idle"][0];
            e.displayName = "Warden"; e.isBoss = true; e.bigBody = true;   // iso vihu: Rocco heittää kuperkeikalla
            e.visualScale = 1.0f; e.attackArtScale = 1f; e.knockArtScale = 1f; e.walkArtScale = 1f;
            e.idleSprites = s["idle"]; e.idleFrameTime = 1f / 12f;
            e.walkSprites = s["kavely"]; e.walkFrameTime = 1f / 12f;
            e.runSprites = s["juoksu"]; e.runFrameTime = 1f / 24f; e.runSpeedMultiplier = 2f; e.runDistance = 5f;
            e.maxHealth = 1000; e.moveSpeedX = 4.0f; e.moveSpeedY = 2.8f; e.depthTolerance = 0.5f; e.attackRange = 2.4f; e.attackCooldown = 0.15f; e.wakeDistance = 16f;
            e.comboArmor = true; e.holdGround = true; e.counterAfterHits = 1;   // kombo menee loppuun (tavallinen isku ei katkaise), vuoroa odottava lyö nopeammin, vastaa jo ensimmäiseen osumaan
            e.comboChance = 0.88f; e.comboWindupScale = 0.5f; e.comboRecoverFraction = 0.8f;
            // J = jabi (osuma kuvassa 5), S = takasuora (osuma 4), K = haymaker (osuma 8, kaataa)
            e.punchSprites = s["isku1"]; e.punchImpactFrame = 4; e.windupTime = 4 * F; e.punchRecoverTime = (s["isku1"].Length - 4) * F; e.punchActiveTime = 0.15f; e.punchDamage = 18;
            e.punch2Sprites = s["isku2"]; e.punch2ImpactFrame = 3; e.punch2Damage = 22; e.punch2Knockdown = false; e.punch2Chance = 0.5f;
            e.punch3Sprites = s["isku3"]; e.punch3ImpactFrame = 7; e.punch3Damage = 34; e.punch3Knockdown = true; e.punch3Chance = 0.35f; e.punch3IsPunch = true;
            e.punch3WindupTime = 7 * F; e.punch3RecoverTime = (s["isku3"].Length - 7) * F; e.punch3Reach = 3.0f;
            // alt-iskut (numero kombossa): 0 etupotku, 1 roundhouse (kaataa), 2 uppercut-hyppy (lataus, hyppy; ei katkea)
            e.altAttackSprites = s["potku1"]; e.altImpactFrame = 5; e.altAnimationFrameTime = F; e.altDamage = 16; e.altReach = 3.3f; e.altChance = 0.5f; e.altExtraWindup = 0.05f;
            e.altVariants = new[]
            {
                new Enemy.AltVariant { name = "Etupotku", sprites = s["potku1"], impactFrame = 5, frameTime = F, damage = 20, reach = 3.4f, knockdown = false, weight = 1f, lungeSpeed = 0f },
                new Enemy.AltVariant { name = "Roundhouse", sprites = s["potku2"], impactFrame = 7, frameTime = F, damage = 26, reach = 3.4f, knockdown = true, knockSpeed = 7f, knockUp = 4f, weight = 0.8f, lungeSpeed = 0f },
                // uppercut: 38 kuvaa, lataus kuvat 0–9, ponnistus 10–18 (efektit), osuma kuvassa 13, huippu 19–25, lasku 26–37; kuvat liikkuvat itse oikealle (kiinteä kamera), joten syöksyä vain vähän
                new Enemy.AltVariant { name = "Uppercut", sprites = s["uppercut"], impactFrame = 13, frameTime = 0.035f, damage = 38, reach = 2.9f, knockdown = true, knockSpeed = 6f, knockUp = 7f, weight = 0.9f, lungeSpeed = 1.5f, lungeTime = 0.3f, jumpHeight = 1.3f, hyperArmor = true },
            };
            // 0 etupotku, 1 roundhouse, 2 uppercut
            e.combos = new[] { "JS", "JSK", "JSJSK", "JSK2", "S0", "JS1", "K1", "2", "JS2", "S2" };
            // heitto: kurotus, ote, nosto hitaasti (kädet ylös), ojennus kiihtyen eteen; ei katkea; lentää matalalla ja lujaa eteenpäin
            e.grabSprites = new[] { s["heitto"][1], s["heitto"][2], s["heitto"][3], s["heitto"][4], s["heitto"][5], s["heitto"][6], s["heitto"][7], s["heitto"][8] };   // kuvat 2–9 (1-pohjainen)
            e.grabsHero = true; e.grabChance = 0.4f; e.grabRange = 2.0f; e.grabReachTime = 0.35f; e.grabLiftTime = 1.0f;
            e.slideThrow = true; e.throwForward = true; e.throwDamage = 30; e.grabArmor = true;
            // torjunta
            var g = s["suojaus"];
            e.blockSprites = new[] { g[0], g[1], g[2], g[3], g[3], g[2], g[1] };
            e.blockTime = 0.45f; e.blockChance = 0.5f; e.maxBlocksInRow = 5; e.guardWhenHurt = true;
            e.hurtSprites = s["osuma"]; e.hurtTime = 0.2f;
            e.knockdownSprites = s["kaatuminen"]; e.airFrameTime = 0.1f; e.thrownStartFrames = new[] { 0, 1, 2 }; e.thrownLoopFrames = new[] { 3, 4, 5 }; e.thrownLandFrames = new[] { 6, 7, 8, 9 }; e.thrownFrameTime = 0.11f;
            e.getUpSprites = s["nousu"]; e.getUpTime = 1.1f;
            e.hurtSounds = LoadClips("Assets/Audio/big thug", "gasp"); e.hurtVolume = 0.95f;   // väliaikaiset äänet
            float x = Mathf.Lerp(room.walkMinX, room.walkMaxX, 0.55f), y = Mathf.Lerp(room.maxDepthY, room.minDepthY, 0.45f);
            go.transform.position = new Vector3(x, y, 0f);
            EditorUtility.SetDirty(e);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Warden lisätty huoneeseen (pomo, 1000 elämää): jabi J, takasuora S, haymaker K (kaataa), etupotku 0, roundhouse 1 (kaataa), uppercut-hyppy 2 (ei katkea), heitto, torjunta 40 % (max 4), nurkkapurkaus (haymaker + uppercut). Kombot JS, JSK, JSJSK, JSK2, S0, JS1, K1, 2, JS2, S2; kombohaarniska, nopeampi (hyökkäysväli 0,4 s), kovempi isku, heitto 40 %.\nÄänet väliaikaisesti big thugin. Ajamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
