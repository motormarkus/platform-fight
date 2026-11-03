using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Kuun laboratorion pomotaistelu: Kuurobotti-pomo tulee käytävältä (lähes voittamaton), Koe-olento on lukittuna oven takana (jytisee ja karjuu).
/// Kun pelaaja kävelee oven kohdalle, kysytään "Vapauta olio"; vapautettu olio käy robottien (ja pelaajan) kimppuun. Komponentti BeastCage.
/// Valikko 333 rakentaa tämän laboratorion (valikko 327) kaksi ensimmäistä ruutua käyttäen; korvaa valikon 332 yksinäisen alienin.</summary>
public static partial class BeatEmUpSetup
{
    // käytäväkuvan (muuri_kaytava_pano, 4721 px) kohtia: olion varoitusovi, pomon ilmestymiskohta ja laukaisu (käytävään tultaessa)
    const float LabCorridorPx = 330f, LabBeastDoorPx = 3829f, LabBossTriggerPx = 950f;
    static readonly Vector2 LabOpenCenterPx = new Vector2(3832f, 500f), LabOpenSizePx = new Vector2(455f, 385f);   // avoin oviaukko käytäväpanossa (px)

    /// <summary>Laboratorion robottipomo: täysin oma vihollinen (ei Kuurobotti): omat kuvat robotti_pomo_*.png ja äänet Resources/Sfx/Robottipomo. Liikkeet: jabi, risti, liukutaklaus, erikoisisku.</summary>
    static Enemy MakeRobotBoss(Transform parent, Vector3 pos, out string missing)
    {
        missing = null;
        var sp = new System.Collections.Generic.Dictionary<string, Sprite[]>();
        foreach (var n in new[] { "idle", "kavely", "lyonti1", "lyonti2", "osuma", "kaatuminen", "maahan", "nousu", "liukutaklaus", "erikoisisku" })
        {
            var a = LoadWalkerSprites("robotti_pomo_" + n);
            if (a == null || a.Length == 0) { missing = "Puuttuu tai ei leikkaudu: robotti_pomo_" + n + ".png (Assets/Sprites/Viholliset). Aja Tools/RobotAssets/make_robot_boss_sheets.py, ...sheets2.py ja ...sheets3.py."; return null; }
            sp[n] = a;
        }
        AudioClip Clip(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Sfx/Robottipomo/" + n + ".wav");
        const float F = 1f / 30f;
        var go = new GameObject("Robottipomo");
        go.transform.SetParent(parent, false);
        var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
        var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
        var e = go.AddComponent<Enemy>();
        e.body = visual; e.shadow = shadow; visual.sprite = sp["idle"][0];
        e.displayName = "Robottipomo";
        e.isBoss = true; e.huntsBrawlers = true;
        e.visualScale = 1.7f; e.attackArtScale = e.knockArtScale = e.walkArtScale = 1f;   // koko vain visualScalessa (1,7 = sama kuin iskussa ja kävelyssä ennen: 1,3 x 1,3); tilakohtaiset kertoimet kertoisivat sen uudestaan ja idle jäi pienemmäksi
        e.idleSprites = sp["idle"]; e.idleFrameTime = 0.12f;
        e.walkSprites = sp["kavely"]; e.walkFrameTime = 0.083f;
        e.runSprites = sp["kavely"]; e.runFrameTime = 0.083f; e.runSpeedMultiplier = 1f; e.runDistance = 999f;   // ei juokse
        e.maxHealth = 420; e.moveSpeedX = 3.4f; e.moveSpeedY = 2.4f; e.attackRange = 2.4f; e.attackCooldown = 0.5f; e.wakeDistance = 40f;
        var j = sp["lyonti1"]; var s2 = sp["lyonti2"];
        e.punchSprites = j; e.punchImpactFrame = 4; e.windupTime = 4 * F * 2f; e.punchRecoverTime = (j.Length - 4) * F * 2f; e.punchDamage = 11;
        e.punch2Sprites = s2; e.punch2ImpactFrame = 4; e.punch2Damage = 13; e.punch2Knockdown = false; e.punch2Chance = 0.45f;
        e.punch3Chance = 0f;
        e.combos = new[] { "JS", "J" }; e.comboChance = 0.6f; e.comboWindupScale = 0.6f; e.comboRecoverFraction = 0.6f;
        e.hurtSprites = sp["osuma"]; e.hurtTime = 0.35f;
        e.knockdownSprites = sp["kaatuminen"]; e.airFrameTime = 0.06f;
        e.landSprites = sp["maahan"]; e.landFrameTime = 0.1f;
        e.getUpSprites = sp["nousu"]; e.getUpTime = sp["nousu"].Length / 14f;
        e.damagedIdleSprites = sp["idle"]; e.damagedFraction = 0.3f; e.damagedIdleFrameTime = 0.12f;   // vaurioitunut tila (alle 30 % elämistä) = kipinät ja savu (DamageSmoke); idle pysyy samana
        go.AddComponent<DamageSmoke>();
        var brain = go.AddComponent<MongolBrain>();
        brain.moves = new[]
        {
            new MongolBrain.Move { name = "liukutaklaus", sprites = sp["liukutaklaus"], frameTime = 0.07f, hitFrom = 5, hitTo = 14, reach = 3.2f, minRange = 2.8f, maxRange = 10f, weight = 1.8f, cooldown = 4f, damage = 32, knockdown = true, moveFrom = 5, moveTo = 14, moveSpeed = 17f, sound = Clip("liuku"), soundVolume = 0.9f },
            // erikoisisku: lataus (kuvat 0-11), lyönti (12-30; ojennettu 14-27), palautuminen; osumaikkuna 15-22, pieni ponnistus eteenpäin 13-17
            new MongolBrain.Move { name = "erikoisisku", sprites = sp["erikoisisku"], frameTime = 0.07f, hitFrom = 15, hitTo = 22, reach = 3.6f, minRange = 1.5f, maxRange = 4.2f, weight = 1.2f, cooldown = 8f, damage = 30, knockdown = true, moveFrom = 13, moveTo = 17, moveSpeed = 4f, sound = Clip("erikoisisku_lataus"), hitSound = Clip("erikoisisku_isku"), soundVolume = 0.9f },
        };
        go.transform.position = pos;
        EditorUtility.SetDirty(e); EditorUtility.SetDirty(brain);
        return e;
    }

    [MenuItem("Beat em up/333. Kuun laboratorio: robottipomo ja olion ovi", false, 333)]
    static void AddLabBossAndBeast()
    {
        if (PlayModeBlocked()) return;
        var moonArt = LoadMoonRobotArt(out string missingArt);
        if (moonArt == null) { Info("Puuttuu Kuurobotin kuvat: " + missingArt); return; }
        var alien = LoadAlienSheets(out string missingAlien);
        if (alien == null) { Info("Puuttuu tai ei leikkaudu: " + missingAlien + " (Assets/Sprites/Viholliset). Aja Tools/AlienAssets/make_alien_sheets*.py."); return; }
        if (!File.Exists(WallScenePath)) { Info("Aja ensin valikko 327 (Kiinan muuri)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(WallScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(WallScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var lab = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Muurin käytävä" && a.gameObject.scene == scene);
            if (lab == null) { Info("Käytävän aluetta ei löytynyt: aja valikko 327."); return; }
            {   // alueiden pitää olla nykyisillä paikoillaan (valikko 327/330), muuten laatikot ja robotit päätyvät toisten alueiden päälle
                foreach (var (an, ax) in new[] { ("Muurin käytävä", WallCorX0), ("Muurin olion huone", WallBeastX0), ("Muurin käytävä 2", WallCor2X0), ("Muurin laboratorio", WallLabX0) })
                {
                    var ar = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == an && a.gameObject.scene == scene);
                    if (ar == null || ar.walkMinX < ax || ar.walkMinX > ax + 10f)
                    { Info("Alue \"" + an + "\" on vanhalla paikalla tai puuttuu (odotettu x " + ax.ToString("0") + (ar != null ? ", nyt " + ar.walkMinX.ToString("0") : "") + "). Aja ensin valikko 330 (tai 327), sitten tämä uudelleen. Mitään ei muutettu."); return; }
                }
            }
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Koe-olento (kuu)" || g.name == "Laboratorion pomo ja olio").ToArray()) Undo.DestroyObjectImmediate(old);
            var root = new GameObject("Laboratorion pomo ja olio");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Laboratorion pomo ja olio");
            float ppu = 941f / (2f * CamHalf);
            Vector3 At(float px, float depth)
            {
                float x = WallCorX0 + px / ppu; float yMax = lab.MaxDepthAtX(x, lab.maxDepthY), yMin = lab.MinDepthAtX(x, lab.minDepthY);
                return new Vector3(x, Mathf.Clamp(Mathf.Lerp(yMax, yMin, depth), yMin + 0.05f, yMax - 0.05f), 0f);
            }
            var beast = MakeAlien(root.transform, At(LabBeastDoorPx, 0.15f), alien);
            beast.maxHealth = 800;   // peto kestää: ei kaadu helposti
            beast.undying = true;   // peto ei kuole: energian loppuessa kaatuu, nousee ja juoksee ruudulta (AlienBrain)
            var boss = MakeRobotBoss(root.transform, At(LabCorridorPx, 0.08f), out string missingBoss);
            if (boss == null) { Info(missingBoss); return; }
            var door = new GameObject("Olion ovi");
            door.transform.SetParent(root.transform, false);
            var dp = At(LabBeastDoorPx, 0f); door.transform.position = new Vector3(dp.x, lab.MaxDepthAtX(dp.x, lab.maxDepthY), 0f);
            var cage = door.AddComponent<BeastCage>();
            cage.here = lab; cage.boss = boss; cage.beast = beast; cage.bossTriggerX = WallCorX0 + LabBossTriggerPx / ppu;
            {   // oven lehdet: peittävät avoimen oviaukon (olion huone näkyy takana) ja nousevat ylös vapautuksessa
                var leafSp = ImportShipSprite("Assets/Sprites/Taustat/muuri_kaytava_ovilehdet.png", ppu, false, SpriteAlignment.Center);
                if (leafSp != null)
                {
                    float top = CamY + CamHalf;
                    Vector2 oc = new Vector2(WallCorX0 + LabOpenCenterPx.x / ppu, top - LabOpenCenterPx.y / ppu), os = new Vector2(LabOpenSizePx.x / ppu, LabOpenSizePx.y / ppu);
                    var leaf = new GameObject("Olion oven lehdet").AddComponent<SpriteRenderer>();
                    leaf.transform.SetParent(root.transform, false); leaf.sprite = leafSp; leaf.sortingOrder = -9990;
                    leaf.transform.position = new Vector3(oc.x, oc.y, 0f);
                    float sw = leafSp.rect.width / leafSp.pixelsPerUnit, sh = leafSp.rect.height / leafSp.pixelsPerUnit;   // kuvassa lehdet täyttävät 1123 x 1035 px 1254 x 1254:stä
                    leaf.transform.localScale = new Vector3(os.x * 1.03f / (sw * 1123f / 1254f), os.y * 1.03f / (sh * 1035f / 1254f), 1f);
                    cage.doorLeaf = leaf; cage.openingCenter = oc; cage.openingSize = os;
                }
            }
            foreach (var rn in new[] { "Muurin käytävä", "Muurin käytävä 2" })
            {   // ikkunan takahuoneessa (pedon huone) vilahtaa olion varjo
                var rootGo = scene.GetRootGameObjects().FirstOrDefault(g => g.name == rn);
                var par = rootGo != null ? rootGo.GetComponentsInChildren<ParallaxLayer>(true).FirstOrDefault(pl => pl.name == "Ikkunan takahuone") : null;
                if (par == null) continue;
                foreach (var old in par.GetComponentsInChildren<WindowBeast>(true).ToArray()) Object.DestroyImmediate(old.gameObject);
                var wb = new GameObject("Ikkunan peto").AddComponent<WindowBeast>();
                wb.transform.SetParent(par.transform, false);
                float wpx = par.GetComponent<SpriteRenderer>().sprite.rect.width;   // ikkunakuvan leveys; ikkunasta näkyy kuvan kohta 2400-3100 px
                wb.walkSprites = alien["kavely"];
                wb.fromX = (2420f - wpx * 0.5f) / ppu; wb.toX = (3080f - wpx * 0.5f) / ppu;
                wb.footY = (470f - 790f) / ppu;   // jalat huoneen lattialla (kuvan rivi 790), ylempi vartalo näkyy ikkunassa
                wb.scale = 1.5f;
            }
            var roomDoorGo = Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(d => d.name == "Käytävä: olion huoneeseen" && d.gameObject.scene == scene);
            if (roomDoorGo != null) { cage.roomDoor = roomDoorGo.gameObject; roomDoorGo.gameObject.SetActive(false); }
            {   // kun robottipomo on voitettu, neljä kuurobottia ilmestyy; peto raatelee nekin (fightsEveryone)
                var rr = new Enemy[4];
                for (int i = 0; i < rr.Length; i++)
                {
                    rr[i] = MakeMoonRobot(moonArt, root.transform, At(LabCorridorPx + 200f * i, 0.4f), "Laboratorion kuurobotti " + (i + 1));
                    rr[i].huntsBrawlers = true; rr[i].gameObject.SetActive(false); EditorUtility.SetDirty(rr[i]);
                }
                cage.reinforcements = rr;
            }
            {   // lisää kuurobotteja: neljä olion huoneessa ja neljä viimeisellä käytävällä (käytävä 2); yksin ei kannata tapella petoa vastaan, koska se juoksee kuitenkin karkuun
                var beastRoomA = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Muurin olion huone" && a.gameObject.scene == scene);
                var cor2A = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Muurin käytävä 2" && a.gameObject.scene == scene);
                Vector3 AtIn(Area a, float x0, float px, float depth)
                {
                    float x = x0 + px / ppu;
                    return new Vector3(x, Mathf.Lerp(a.MaxDepthAtX(x, a.maxDepthY), a.MinDepthAtX(x, a.minDepthY), depth), 0f);
                }
                int nr = 0;
                void Squad(Area a, float x0, string label, float[] pxs, float[] ds)
                {
                    if (a == null) return;
                    for (int i = 0; i < pxs.Length; i++)
                    {
                        var r = MakeMoonRobot(moonArt, root.transform, AtIn(a, x0, pxs[i], ds[i]), label + " " + (i + 1));
                        r.huntsBrawlers = true; EditorUtility.SetDirty(r); nr++;
                    }
                }
                Squad(beastRoomA, WallBeastX0, "Olion huoneen kuurobotti", new[] { 900f, 1700f, 2500f, 3300f }, new[] { 0.35f, 0.6f, 0.3f, 0.55f });
                Squad(cor2A, WallCor2X0, "Käytävän 2 kuurobotti", new[] { 900f, 1700f, 2500f, 3200f }, new[] { 0.55f, 0.3f, 0.6f, 0.35f });
                Debug.Log("Beat em up: lisättiin " + nr + " kuurobottia (olion huone ja käytävä 2).");
            }
            boss.heroDamageMul = cage.lockedBossDamageMul;
            boss.gameObject.SetActive(false); beast.gameObject.SetActive(false);
            foreach (var rr in scene.GetRootGameObjects().Where(g => g.name == "Kuurobotit"))
                foreach (var en in rr.GetComponentsInChildren<Enemy>(true)) { en.huntsBrawlers = true; EditorUtility.SetDirty(en); }
            {   // jerrygallonat käytävälle ja laboratorioon: räjäyttämällä pomo kaatuu ilman olion apua (Jerrycan.robotBossDamage)
                foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Laboratorion jerrygallonat").ToArray()) Undo.DestroyObjectImmediate(old);
                var cans = new GameObject("Laboratorion jerrygallonat");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cans, scene);
                Undo.RegisterCreatedObjectUndo(cans, "Laboratorion jerrygallonat");
                var labBase = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Muurin laboratorio" && a.gameObject.scene == scene);
                int nc = 0;
                void Can(Area a, float x0, string label, float px, float depth)
                {
                    if (a == null) return;
                    float x = x0 + px / ppu;
                    var go = new GameObject(label);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                    go.transform.SetParent(cans.transform, false);
                    go.transform.position = new Vector3(x, Mathf.Lerp(a.MaxDepthAtX(x, a.maxDepthY), a.MinDepthAtX(x, a.minDepthY), depth), 0f);
                    go.AddComponent<Jerrycan>(); nc++;
                }
                float[] cp = { 1250f, 1950f, 2650f, 3300f }, cd = { 0.5f, 0.3f, 0.65f, 0.45f };   // käytävä: pomo tulee takaa, gallonat matkan varrella
                for (int i = 0; i < cp.Length; i++) Can(lab, WallCorX0, "Käytävän gallona " + (i + 1), cp[i], cd[i]);
                float[] lp = { 1900f, 3600f, 5400f, 7000f }, ld = { 0.4f, 0.65f, 0.35f, 0.55f };   // laboratorio
                for (int i = 0; i < lp.Length; i++) Can(labBase, WallLabX0, "Laboratorion gallona " + (i + 1), lp[i], ld[i]);
                Debug.Log("Beat em up: laboratorioon ja käytävälle lisättiin " + nc + " jerrygallonaa.");
            }
            {   // puulaatikot käytävälle ja laboratorioon: energiaa (ruoka) ja staminaa (energiajuoma); malli UccoLaivan laatikosta
                foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Tukikohdan laatikot").ToArray()) Undo.DestroyObjectImmediate(old);
                const string uccoPath = "Assets/Scenes/Areas/UccoLaiva.unity";
                var ucco = File.Exists(uccoPath) ? EditorSceneManager.GetSceneByPath(uccoPath) : default;
                bool openedUcco = false;
                if (File.Exists(uccoPath) && (!ucco.IsValid() || !ucco.isLoaded)) { ucco = EditorSceneManager.OpenScene(uccoPath, OpenSceneMode.Additive); openedUcco = true; }
                var crateT = ucco.IsValid() ? Object.FindObjectsByType<Crate>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c != null && c.gameObject.scene == ucco && c.breakable && c.stackedOn == null && c.gameObject.name.StartsWith("Laatikko")) : null;
                if (crateT == null) Debug.LogWarning("Beat em up: laatikkomallia ei löytynyt UccoLaivasta, tukikohdan laatikoita ei lisätty.");
                else
                {
                    var crates = new GameObject("Tukikohdan laatikot");
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(crates, scene);
                    Undo.RegisterCreatedObjectUndo(crates, "Tukikohdan laatikot");
                    var labBase2 = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Muurin laboratorio" && a.gameObject.scene == scene);
                    int ncr = 0;
                    Crate Box(Area a, float x0, float px, float depth, Crate under)
                    {
                        float x = x0 + px / ppu;
                        var c = Object.Instantiate(crateT.gameObject);
                        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(c, scene);
                        c.transform.SetParent(crates.transform, false);
                        c.name = "Tukikohdan laatikko " + (++ncr) + (under != null ? " (päällä)" : "");
                        c.transform.position = new Vector3(x, Mathf.Lerp(a.MaxDepthAtX(x, a.maxDepthY), a.MinDepthAtX(x, a.minDepthY), depth), 0f);
                        c.SetActive(true);
                        var cr = c.GetComponent<Crate>(); cr.stackedOn = under; cr.energyChance = 0.6f; cr.foodChance = 0.3f; cr.thermosChance = 0f;
                        return cr;
                    }
                    void Pair(Area a, float x0, float px, float depth)
                    {
                        if (a == null) return;
                        var b1 = Box(a, x0, px, depth, null); Box(a, x0, px + 90f, depth, null);
                        Box(a, x0, px + 45f, depth + 0.002f, b1);
                    }
                    foreach (var px in new[] { 700f, 2300f, 3000f }) Pair(lab, WallCorX0, px, 0.15f);   // käytävä
                    foreach (var px in new[] { 1100f, 2700f, 4500f, 6200f, 7600f }) Pair(labBase2, WallLabX0, px, 0.12f);   // laboratorio
                    var beastRoomC = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Muurin olion huone" && a.gameObject.scene == scene);
                    var cor2C = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Muurin käytävä 2" && a.gameObject.scene == scene);
                    foreach (var px in new[] { 1300f, 3000f }) Pair(beastRoomC, WallBeastX0, px, 0.14f);   // olion huone
                    foreach (var px in new[] { 1300f, 3000f }) Pair(cor2C, WallCor2X0, px, 0.14f);   // viimeinen käytävä (käytävä 2)
                    Debug.Log("Beat em up: tukikohtaan lisättiin " + ncr + " puulaatikkoa (60 % energiajuoma, 30 % ruoka).");
                }
                if (openedUcco && ucco.IsValid()) EditorSceneManager.CloseScene(ucco, true);
            }
            EditorUtility.SetDirty(boss); EditorUtility.SetDirty(cage);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Laboratorion pomotaistelu rakennettu.\nRobottipomo (420 elämää, 1,3-kertainen) ilmestyy käytävän aukosta, kun pelaaja ylittää x=" + (WallCorX0 + LabBossTriggerPx / ppu).ToString("0.0") +
                 "; heron iskut tekevät siihen 2 % vahinkoa, kunnes olio on vapautettu.\nKoe-olento on oven takana (x=" + dp.x.ToString("0.0") + "): jytisee ja karjuu. Oven kohdalla kysytään \"Vapauta olio\" / \"Jätä olio rauhaan\"; vapautettu olio käy kaikkien kimppuun.\nAjamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
