using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Muurin keskitorni, alakerta (valikko 343): kaksoistornin (muurin ruudut 8 ja 9) kaksi lasiovea johtavat samaan saliin.
/// Kuva muuri_torni_ala.png (2823 x 941): vasen ovi px 282, oikea ovi px 2605, portaat ylös keskellä px 1433 (yläkerta myöhemmin, ei ovea vielä).
/// Valikko 327 kutsuu samaa koodia (BuildWallTower).</summary>
public static partial class BeatEmUpSetup
{
    const float WallTowerX0 = 79720f, TowerWallPx = 790f, TowerLeftDoorPx = 282f, TowerRightDoorPx = 2605f, TowerPx = 2823f;
    const float TowerOutLeftDoorPx = 7f * 1672f + 1365f, TowerOutRightDoorPx = 8f * 1672f + 305f;   // muurin ruudut 8 (peili, ovi oikealla) ja 9 (alkutorni, ovi vasemmalla)
    const string TowerAreaName = "Muurin torni (alakerta)", TowerUpAreaName = "Muurin torni (yläkerta)", TowerRoofAreaName = "Muurin torni (katto)";
    // katto (muuri_torni_katto.png 2817 x 941): portaat alas vasemmassa etukulmassa (px ~130), kuppien liekit px 180 / 2642 (rivi 369), lipputanko px 1325 (kärki rivi 113)
    const float WallTowerRoofX0 = 79880f, TowerRoofWallPx = 634f, TowerRoofPx = 2817f, TowerRoofStairsPx = 130f;
    // yläkerta (muuri_torni_yla.png 3137 x 941): vasemmalla portaat alas (px 230), oikealla portaat ylös katolle (px 2945, katto myöhemmin), seinän juuri rivi 605
    const float WallTowerUpX0 = 79800f, TowerUpWallPx = 605f, TowerUpPx = 3137f, TowerUpDownPx = 230f, TowerUpRoofPx = 2945f, TowerStairsPx = 1433f;

    static void BuildWallTower(UnityEngine.SceneManagement.Scene scene)
    {
        const string T = "Assets/Sprites/Taustat/";
        var wall = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Muuri" && a.gameObject.scene == scene);
        if (wall == null) { Debug.LogWarning("Muurin torni: aluetta \"Muuri\" ei löytynyt."); return; }
        foreach (var n in new[] { "Muurin torni (alakerta)", "Alue: Muurin torni (alakerta)", "Muurin torni (yläkerta)", "Alue: Muurin torni (yläkerta)", "Muurin torni (katto)", "Alue: Muurin torni (katto)" })
            foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);
        foreach (var d in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(d => d != null && d.gameObject.scene == scene && d.name.StartsWith("Torni:")).ToArray())
            Undo.DestroyObjectImmediate(d.gameObject);

        float ppu = 941f / (2f * CamHalf), H = 941f / ppu, W = 1672f / ppu;
        float bottom = CamY - CamHalf, top = bottom + H, cy = CamY;
        var root = new GameObject("Muurin torni (alakerta)");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        Undo.RegisterCreatedObjectUndo(root, "Muurin torni");
        var sp = ImportShipSprite(T + "muuri_torni_ala.png", ppu, false, SpriteAlignment.Center);
        if (sp == null) { Debug.LogWarning("Muurin torni: kuvan tuonti epäonnistui."); return; }
        var sr = new GameObject("Tornin alakerta").AddComponent<SpriteRenderer>();
        sr.transform.SetParent(root.transform, false);
        sr.sprite = sp; sr.sortingOrder = -10000;
        sr.transform.position = new Vector3(WallTowerX0 + sp.bounds.size.x * 0.5f, cy, 0f);

        var a = ReuseArea("Alue: Muurin torni (alakerta)");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(a.gameObject, scene);
        a.areaName = TowerAreaName; a.useSidewalk = false;
        a.maxDepthY = top - TowerWallPx / ppu; a.minDepthY = bottom + 0.9f;
        a.walkMinX = WallTowerX0 + 200f / ppu; a.walkMaxX = WallTowerX0 + (TowerPx - 200f) / ppu;
        a.camMinX = WallTowerX0 + W * 0.5f; a.camMaxX = Mathf.Max(a.camMinX, WallTowerX0 + TowerPx / ppu - W * 0.5f);
        a.camSize = CamHalf; a.camRiseY = 0f; a.camOffsetY = 0f; a.backScale = 1f; a.depthLimits = new Vector2[0];
        a.music = wall.music; a.musicGain = wall.musicGain;
        EditorUtility.SetDirty(a);
        {   // ikkunoiden taakse parallaksi (taivas ja vuoret): näkyy, kun ikkunat ovat kuvassa läpinäkyviä (nyt valkoiset, joten ei vielä näy)
            var sky = ImportShipSprite(T + "muuri_far_x2.png", ppu, false, SpriteAlignment.Center);
            if (sky != null)
            {
                float left = a.camMinX - W * 0.5f;
                var ssr = new GameObject("Ikkunoiden taivas").AddComponent<SpriteRenderer>();
                ssr.transform.SetParent(root.transform, false);
                ssr.sprite = sky; ssr.sortingOrder = -10150; ssr.transform.position = new Vector3(left + sky.bounds.size.x * 0.5f, cy, 0f);
                var pl = ssr.gameObject.AddComponent<ParallaxLayer>();
                pl.factor = 0.03f; pl.startCamX = a.camMinX; pl.minX = a.camMinX - 2f; pl.maxX = a.camMaxX + 2f;
            }
        }

        // ---- yläkerta ----
        var upRoot = new GameObject("Muurin torni (yläkerta)");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(upRoot, scene);
        Undo.RegisterCreatedObjectUndo(upRoot, "Muurin torni");
        var upSp = ImportShipSprite(T + "muuri_torni_yla.png", ppu, false, SpriteAlignment.Center);
        Area up = null;
        if (upSp != null)
        {
            var usr = new GameObject("Tornin yläkerta").AddComponent<SpriteRenderer>();
            usr.transform.SetParent(upRoot.transform, false);
            usr.sprite = upSp; usr.sortingOrder = -10000;
            usr.transform.position = new Vector3(WallTowerUpX0 + upSp.bounds.size.x * 0.5f, cy, 0f);
            up = ReuseArea("Alue: Muurin torni (yläkerta)");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(up.gameObject, scene);
            up.areaName = TowerUpAreaName; up.useSidewalk = false;
            up.maxDepthY = top - TowerUpWallPx / ppu; up.minDepthY = bottom + 0.9f;
            up.walkMinX = WallTowerUpX0 + 150f / ppu; up.walkMaxX = WallTowerUpX0 + (TowerUpPx - 150f) / ppu;
            up.camMinX = WallTowerUpX0 + W * 0.5f; up.camMaxX = Mathf.Max(up.camMinX, WallTowerUpX0 + TowerUpPx / ppu - W * 0.5f);
            up.camSize = CamHalf; up.camRiseY = 0f; up.camOffsetY = 0f; up.backScale = 1f; up.depthLimits = new Vector2[0];
            up.music = wall.music; up.musicGain = wall.musicGain;
            EditorUtility.SetDirty(up);
            var sky2 = ImportShipSprite(T + "muuri_far_x2.png", ppu, false, SpriteAlignment.Center);   // ikkunoiden taakse sama taivas
            if (sky2 != null)
            {
                var s2 = new GameObject("Ikkunoiden taivas").AddComponent<SpriteRenderer>();
                s2.transform.SetParent(upRoot.transform, false);
                s2.sprite = sky2; s2.sortingOrder = -10150; s2.transform.position = new Vector3(up.camMinX - W * 0.5f + sky2.bounds.size.x * 0.5f, cy, 0f);
                var pl2 = s2.gameObject.AddComponent<ParallaxLayer>();
                pl2.factor = 0.03f; pl2.startCamX = up.camMinX; pl2.minX = up.camMinX - 2f; pl2.maxX = up.camMaxX + 2f;
            }
        }

        // ---- katto ----
        var roofRoot = new GameObject("Muurin torni (katto)");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(roofRoot, scene);
        Undo.RegisterCreatedObjectUndo(roofRoot, "Muurin torni");
        var roofSp = ImportShipSprite(T + "muuri_torni_katto.png", ppu, false, SpriteAlignment.Center);
        Area roof = null;
        if (roofSp != null)
        {
            var rsr = new GameObject("Tornin katto").AddComponent<SpriteRenderer>();
            rsr.transform.SetParent(roofRoot.transform, false);
            rsr.sprite = roofSp; rsr.sortingOrder = -10000;
            rsr.transform.position = new Vector3(WallTowerRoofX0 + roofSp.bounds.size.x * 0.5f, cy, 0f);
            roof = ReuseArea("Alue: Muurin torni (katto)");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(roof.gameObject, scene);
            roof.areaName = TowerRoofAreaName; roof.useSidewalk = false;
            roof.maxDepthY = top - TowerRoofWallPx / ppu; roof.minDepthY = bottom + 0.9f;
            roof.walkMinX = WallTowerRoofX0 + 110f / ppu; roof.walkMaxX = WallTowerRoofX0 + (TowerRoofPx - 150f) / ppu;
            roof.camMinX = WallTowerRoofX0 + W * 0.5f; roof.camMaxX = Mathf.Max(roof.camMinX, WallTowerRoofX0 + TowerRoofPx / ppu - W * 0.5f);
            roof.camSize = CamHalf; roof.camRiseY = 0f; roof.camOffsetY = 0f; roof.backScale = 1f; roof.depthLimits = new Vector2[0];
            roof.music = wall.music; roof.musicGain = wall.musicGain;
            EditorUtility.SetDirty(roof);
            var sky3 = ImportShipSprite(T + "muuri_far_x2.png", ppu, false, SpriteAlignment.Center);   // katon taivas läpinäkyvän yläosan takana
            if (sky3 != null)
            {
                var s3 = new GameObject("Katon taivas").AddComponent<SpriteRenderer>();
                s3.transform.SetParent(roofRoot.transform, false);
                s3.sprite = sky3; s3.sortingOrder = -10150; s3.transform.position = new Vector3(roof.camMinX - W * 0.5f + sky3.bounds.size.x * 0.5f, cy, 0f);
                var pl3 = s3.gameObject.AddComponent<ParallaxLayer>();
                pl3.factor = 0.03f; pl3.startCamX = roof.camMinX; pl3.minX = roof.camMinX - 2f; pl3.maxX = roof.camMaxX + 2f;
            }
            // soihtuliekit kahteen kuppiin ja lippu tankoon: kuvasarjat Resources/Muuri/Soihtu ja Resources/Muuri/Lippu (jos kansiot tyhjiä, ei näy mitään)
            float RX(float px) => WallTowerRoofX0 + px / ppu;
            float RY(float row) => top - row / ppu;
            foreach (var fx in new[] { 183f, 2674f })
            {
                var f = new GameObject("Soihtuliekki").AddComponent<FrameLoop>();
                f.transform.SetParent(roofRoot.transform, false); f.transform.position = new Vector3(RX(fx), RY(362f), 0f);
                f.folder = "Muuri/Soihtu"; f.fps = 10f; f.worldHeight = 155f / ppu; f.pivot = new Vector2(0.5f, 0.02f); f.sortingOrder = -9990; f.phase = fx > 1000f ? 0.37f : 0f;
            }
            var fl = new GameObject("Lippu").AddComponent<FrameLoop>();
            fl.transform.SetParent(roofRoot.transform, false); fl.transform.position = new Vector3(RX(1328f), RY(138f), 0f);
            fl.folder = "Muuri/Lippu"; fl.fps = 8f; fl.worldHeight = 180f / ppu; fl.pivot = new Vector2(0.01f, 0.97f); fl.sortingOrder = -9990;
        }

        var doorRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Muurin ovet");
        if (doorRoot == null) { doorRoot = new GameObject("Muurin ovet"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, scene); Undo.RegisterCreatedObjectUndo(doorRoot, "Muurin ovet"); }
        Door MakeDoor(string name, string prompt, Area here, Area target, Vector3 pos, Vector2 spawn)
        {
            var d = new GameObject(name).AddComponent<Door>();
            d.transform.SetParent(doorRoot.transform, false); d.transform.position = pos;
            d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = spawn; d.halfWidth = 1.6f; d.maxDistanceFromWall = 2.2f;
            return d;
        }
        float WallX(float px) => WallX0 + px / ppu;
        float TowerX(float px) => WallTowerX0 + px / ppu;
        Vector2 WallSpawn(float px) => new Vector2(WallX(px), Mathf.Lerp(wall.maxDepthY, wall.minDepthY, 0.4f));
        Vector2 TowerSpawn(float px) => new Vector2(TowerX(px), Mathf.Lerp(a.maxDepthY, a.minDepthY, 0.3f));
        MakeDoor("Torni: vasen ovi sisään", "Mene torniin", wall, a, new Vector3(WallX(TowerOutLeftDoorPx), wall.maxDepthY, 0f), TowerSpawn(TowerLeftDoorPx + 130f));
        MakeDoor("Torni: vasen ovi ulos", "Ulos muurille", a, wall, new Vector3(TowerX(TowerLeftDoorPx), a.maxDepthY, 0f), WallSpawn(TowerOutLeftDoorPx - 130f));
        MakeDoor("Torni: oikea ovi sisään", "Mene torniin", wall, a, new Vector3(WallX(TowerOutRightDoorPx), wall.maxDepthY, 0f), TowerSpawn(TowerRightDoorPx - 130f));
        MakeDoor("Torni: oikea ovi ulos", "Ulos muurille", a, wall, new Vector3(TowerX(TowerRightDoorPx), a.maxDepthY, 0f), WallSpawn(TowerOutRightDoorPx + 130f));

        if (up != null)
        {   // keskiportaat ylös yläkerran vasempaan päätyyn (portaat alas), takaisin alas samoista portaista; oikeat portaat ylös katolle tulevat myöhemmin
            float UpX(float px) => WallTowerUpX0 + px / ppu;
            MakeDoor("Torni: portaat ylös", "Portaat ylös", a, up, new Vector3(TowerX(TowerStairsPx), a.maxDepthY, 0f), new Vector2(UpX(TowerUpDownPx + 130f), Mathf.Lerp(up.maxDepthY, up.minDepthY, 0.4f)));
            MakeDoor("Torni: portaat alas", "Portaat alas", up, a, new Vector3(UpX(TowerUpDownPx), up.maxDepthY, 0f), new Vector2(TowerX(TowerStairsPx), Mathf.Lerp(a.maxDepthY, a.minDepthY, 0.3f)));
        }

        if (up != null && roof != null)
        {   // yläkerran oikeat portaat -> katon vasen etukulma (portaat alas); takaisin samasta
            MakeDoor("Torni: portaat katolle", "Portaat katolle", up, roof, new Vector3(WallTowerUpX0 + TowerUpRoofPx / ppu, up.maxDepthY, 0f),
                new Vector2(WallTowerRoofX0 + (TowerRoofStairsPx + 260f) / ppu, Mathf.Lerp(roof.maxDepthY, roof.minDepthY, 0.45f)));
            var dd = MakeDoor("Torni: portaat katolta alas", "Portaat alas", roof, up, new Vector3(WallTowerRoofX0 + TowerRoofStairsPx / ppu, roof.minDepthY, 0f),
                new Vector2(WallTowerUpX0 + (TowerUpRoofPx - 130f) / ppu, Mathf.Lerp(up.maxDepthY, up.minDepthY, 0.4f)));
            dd.atFront = true;
        }

        // muuri jakautuu tornin kohdalta: kävely ja kamera pysähtyvät tornin päätyyn (vasen puoli) tai alkavat sen toiselta puolelta (oikea puoli); yli pääsee vain tornin kautta
        var split = root.AddComponent<AreaSplit>();
        split.area = wall;
        split.leftWalkMax = WallX(TowerOutLeftDoorPx + 100f); split.leftCamMax = WallX(8f * 1672f) - W * 0.5f;
        split.rightWalkMin = WallX(TowerOutRightDoorPx - 100f); split.rightCamMin = WallX(8f * 1672f) + W * 0.5f;

        var areasFile = WallScenePath.Replace(".unity", ".areas.txt");
        var names = File.Exists(areasFile) ? File.ReadAllLines(areasFile).ToList() : new List<string>();
        bool addedNames = false;
        foreach (var nm in new[] { TowerAreaName, TowerUpAreaName, TowerRoofAreaName }) if (!names.Contains(nm)) { names.Add(nm); addedNames = true; }
        if (addedNames) File.WriteAllLines(areasFile, names);
    }

    [MenuItem("Beat em up/343. Muurin keskitorni: ala-, ylä- ja kattokerros", false, 343)]
    static void BuildWallTowerMenu()
    {
        if (PlayModeBlocked()) return;
        if (!File.Exists("Assets/Sprites/Taustat/muuri_torni_ala.png")) { Info("Puuttuu kuva muuri_torni_ala.png (Assets/Sprites/Taustat)."); return; }
        if (!File.Exists(WallScenePath)) { Info("Muuri-scenea ei ole: aja valikko 327 kerran."); return; }
        var opened = new List<UnityEngine.SceneManagement.Scene>();
        try
        {
            var sc = EditorSceneManager.GetSceneByPath(WallScenePath);
            if (!sc.IsValid() || !sc.isLoaded) { sc = EditorSceneManager.OpenScene(WallScenePath, OpenSceneMode.Additive); opened.Add(sc); }
            BuildWallTower(sc);
            EditorSceneManager.MarkSceneDirty(sc); EditorSceneManager.SaveScene(sc);
            Info("Muurin keskitornin alakerta rakennettu (x " + WallTowerX0.ToString("0") + "): muurin kaksoistornin kaksi ovea (vasen ja oikea) vievät saliin. Portaat ylös tulevat myöhemmin.\nAjamatta Unityssä.");
        }
        finally { foreach (var sc in opened) if (sc.IsValid()) EditorSceneManager.CloseScene(sc, true); }
    }
}
