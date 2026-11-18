using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Linnakkeen katto (valikko 417): viisi 5120 x 1844 -kuvaa peräkkäin (vasen hissi, keskiosa, keskiosa peilattuna, keskiosa, oikea laukaisualusta; taivas läpinäkyvä, taakse avaruus),
/// laukaisualustalla iso alus (5120 x 1707, jalat lattian takareunassa, hero kulkee aina sen edessä). Kenraalin huoneen oikea ovi vie hissille (lukossa kunnes Kenraali on kaatunut), hissistä takaisin huoneeseen.
/// Kuvat: output/imagegen/linnakkeen-katto-3x5k-laaja-laukaisualusta (01, 02, 02b peilattu, 03) ja Assets/Sprites/Props/alus_ulko.png. Vaatii valikot 351 (pomohuone) ja 352. Ajo uudelleen turvallinen.
/// Aluksen sisätilat (cargo, hytti, ohjaamo) ja luukun ovi tulevat myöhemmin.
/// </summary>
public static partial class BeatEmUpSetup
{
    const string FortRoofAreaName = "Linnakkeen katto";
    const float FortRoofX0 = 79320f;

    [MenuItem("Beat em up/417. Linnake: katto (5 kuvaa, laukaisualusta, alus, hissi Kenraalin huoneesta)", false, 417)]
    static void AddFortressRoof()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/", P = "Assets/Sprites/Props/";
        string srcDir = "output/imagegen/linnakkeen-katto-3x5k-laaja-laukaisualusta/";
        var map = new[] { ("01-vasen-hissi.png", "muuri_katto_1.png"), ("02-keskiosa.png", "muuri_katto_2.png"), ("02b-keskiosa-peilattu.png", "muuri_katto_2b.png"), ("03-oikea-laukaisualusta.png", "muuri_katto_3.png") };
        foreach (var (s, d) in map)
        {
            if (!File.Exists(T + d)) { if (!File.Exists(srcDir + s)) { Info("Puuttuu kattokuva " + srcDir + s); return; } File.Copy(srcDir + s, T + d, true); }
            AssetDatabase.ImportAsset(T + d, ImportAssetOptions.ForceUpdate);
        }
        if (!File.Exists(P + "alus_ulko.png")) { Info("Puuttuu " + P + "alus_ulko.png"); return; }
        AssetDatabase.ImportAsset(P + "alus_ulko.png", ImportAssetOptions.ForceUpdate);
        if (!File.Exists(T + "muuri_avaruus.png")) { Info("Puuttuu " + T + "muuri_avaruus.png"); return; }

        string hallScene = AreaSceneName("Muurin juhlahalli");
        if (hallScene == null) { Info("Juhlasalia ei löytynyt: aja valikko 340 tai 330."); return; }
        string scenePath = "Assets/Scenes/Areas/" + hallScene + ".unity";
        var opened = new System.Collections.Generic.List<UnityEngine.SceneManagement.Scene>();
        var sc = EditorSceneManager.GetSceneByPath(scenePath);
        if (!sc.IsValid() || !sc.isLoaded) { sc = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive); opened.Add(sc); }
        try
        {
            var all = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == sc).ToArray();
            var hall = all.FirstOrDefault(a => a.areaName == "Muurin juhlahalli");
            var room = all.FirstOrDefault(a => a.areaName == BossRoom1AreaName);
            if (hall == null || room == null) { Info("Pomohuonetta ei löytynyt: aja valikko 351 ja 352."); return; }
            foreach (var n in new[] { "Linnakkeen katto", "Alue: Linnakkeen katto" })
                foreach (var o in sc.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);

            float H = 2f * CamHalf, ppu = 1844f / H, W = 1672f / (941f / H), cy = CamY, top = cy + CamHalf, bottom = cy - CamHalf;
            float imgW = 5120f / ppu, X0 = FortRoofX0, widthU = 5f * imgW;
            var root = new GameObject("Linnakkeen katto"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, sc); Undo.RegisterCreatedObjectUndo(root, "Linnakkeen katto");
            SpriteRenderer Put(string name, Sprite sp, Vector3 pos, int order)
            {
                var sr = new GameObject(name).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = order; sr.transform.position = pos; return sr;
            }
            Sprite S(string f) => ImportShipSprite(T + f, ppu, false, SpriteAlignment.Center);
            var sL = S("muuri_katto_1.png"); var sM = S("muuri_katto_2.png"); var sMf = S("muuri_katto_2b.png"); var sR = S("muuri_katto_3.png");
            if (sL == null || sM == null || sMf == null || sR == null) { Info("Kattokuvien tuonti epäonnistui."); return; }
            var seq = new[] { sL, sM, sMf, sM, sR };
            for (int i = 0; i < seq.Length; i++) Put("Katto " + (i + 1), seq[i], new Vector3(X0 + imgW * (i + 0.5f), cy, 0f), -10000);

            // alue: lattia kuvan riveillä 1260-1660 (takareuna: hissin ovi ja aluksen jalat)
            var roof = ReuseArea("Alue: Linnakkeen katto");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(roof.gameObject, sc);
            roof.areaName = FortRoofAreaName; roof.useSidewalk = false;
            roof.maxDepthY = top - 1260f / ppu; roof.minDepthY = top - 1660f / ppu;
            roof.walkMinX = X0 + 1.5f; roof.walkMaxX = X0 + widthU - 1.2f;
            roof.camMinX = X0 + W * 0.5f; roof.camMaxX = Mathf.Max(roof.camMinX, X0 + widthU - W * 0.5f);
            roof.camSize = CamHalf; roof.camRiseY = 0f; roof.camOffsetY = 0f; roof.backScale = 1f; roof.depthLimits = new Vector2[0];
            var kuuMus = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Musiikki/Kuu Main.mp3"); roof.music = kuuMus != null ? kuuMus : hall.music; roof.musicGain = kuuMus != null ? 1f : hall.musicGain;   // kuun oma taustamusiikki
            roof.gravityScale = 0.4f; roof.camJumpLift = 0.6f;   // kuun heikko painovoima kuten kuun pinnalla: hypyt korkealle ja kamera seuraa

            // avaruus taivaan taakse (hidas parallaksi)
            var spc = S("muuri_avaruus_2x.png");   // kaksinkertaisen korkea (peilattu ylös): kamera nousee hypyissä kuun heikossa painovoimassa
            if (spc != null)
            {
                float sh = spc.bounds.size.y * 0.5f, k = Mathf.Max(1f, H * 1.02f / sh);
                var star = Put("Avaruus", spc, new Vector3(roof.camMinX - W * 0.5f + spc.bounds.size.x * k * 0.5f, bottom - H * 0.01f + spc.bounds.size.y * k * 0.5f, 0f), -10150);
                star.transform.localScale = new Vector3(k, k, 1f);
                var pl = star.gameObject.AddComponent<ParallaxLayer>();
                pl.factor = 0.02f; pl.startCamX = roof.camMinX; pl.minX = roof.camMinX - 2f; pl.maxX = roof.camMaxX + 2f;
            }

            // alus laukaisualustalla (kuvan 5 alue): jalat lattian takareunassa (maxDepthY), hero kulkee aina edessä
            var ship = ImportShipSprite(P + "alus_ulko.png", ppu, false, SpriteAlignment.Center);
            if (ship != null)
            {
                // alus 1,3-kertaisena (yläosa saa leikkautua: kuun heikossa painovoimassa kamera nousee hypyissä); nokka kuvan oikeassa reunassa, jalat lattian takareunassa
                const float shipScale = 1.3f;
                float shipWu = ship.bounds.size.x * shipScale, shipHu = ship.bounds.size.y * shipScale;
                float shipX = X0 + widthU - 200f / ppu - shipWu * 0.5f, shipY = roof.maxDepthY + 1568f * shipScale / ppu - shipHu * 0.5f;
                var sr = Put("Alus", ship, new Vector3(shipX, shipY, 0f), Mathf.RoundToInt(-roof.maxDepthY * 100f) - 5);
                sr.transform.localScale = new Vector3(shipScale, shipScale, 1f);
            }

            // hissi: Kenraalin huoneen oikea ovi -> hissin ovi (kuvan 1 hissi x 721 px), hissistä takaisin
            var doorRoot = sc.GetRootGameObjects().FirstOrDefault(g => g.name == "Muurin ovet") ?? new GameObject("Muurin ovet");
            if (doorRoot.scene != sc) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, sc);
            float elevX = X0 + 721f / ppu, roomRightX = BossRoom1X0 + BossRoomRightDoorPx / (BossRoomImgH / (2f * CamHalf));
            float midY(Area a) => Mathf.Lerp(a.maxDepthY, a.minDepthY, 0.4f);
            foreach (var d in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(d => d != null && d.gameObject.scene == sc && d.name == "Katto: hissi alas").ToArray()) Undo.DestroyObjectImmediate(d.gameObject);
            var down = new GameObject("Katto: hissi alas").AddComponent<Door>();
            down.transform.SetParent(doorRoot.transform, false); down.transform.position = new Vector3(elevX, roof.maxDepthY, 0f);
            down.prompt = "Hissillä alas"; down.here = roof; down.target = room; down.spawnPoint = new Vector2(roomRightX - 2.8f, midY(room)); down.halfWidth = 1.6f; down.maxDistanceFromWall = 2.2f;
            var fwd = Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(d => d != null && d.gameObject.scene == sc && d.name == "Pomohuone 1: ovi eteenpäin");
            string info = "";
            if (fwd != null)
            {
                fwd.target = roof; fwd.spawnPoint = new Vector2(elevX + 2.8f, midY(roof)); fwd.prompt = "Hissillä katolle";
                fwd.requiredClue = "Pomohuone 1: avain"; fwd.lockedClueText = "Ovi on lukossa.";   // avautuu, kun Kenraali kaatuu (Clues.BossDown)
                EditorUtility.SetDirty(fwd);
            }
            else info = "Pomohuoneen oikeaa ovea ei löytynyt (aja 351). ";
            EditorUtility.SetDirty(roof); EditorUtility.SetDirty(down);
            EditorSceneManager.MarkSceneDirty(sc); EditorSceneManager.SaveScene(sc);
            Info("Linnakkeen katto rakennettu (x " + X0.ToString("0") + "–" + (X0 + widthU).ToString("0") + ", scene " + hallScene + "): 5 kuvaa, avaruus, alus laukaisualustalla ja hissi pomohuoneesta (ovi aukeaa Kenraalin kaaduttua). " + info + "Aluksen luukku ja sisätilat puuttuvat vielä.");
        }
        finally { foreach (var s in opened) if (s.IsValid()) EditorSceneManager.CloseScene(s, true); }
    }
}
