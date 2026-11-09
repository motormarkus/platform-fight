using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Linnakkeen ensimmäinen pomohuone (valikko 351): kaksi kuvaa (muuri_pomohuone_1: ovi vasemmalla, ikkunat läpinäkyviä -> taakse avaruus; muuri_pomohuone_2: tykki/kaukoputki keskellä, ovi oikealla),
/// kummankin koko 2172 x 724. Ovi juhlasalin oikeasta päästä huoneen vasempaan oveen; oikea ovi on toistaiseksi lukossa (seuraava huone myöhemmin). Pomoa ei vielä ole.
/// Rakentuu siihen sceneen, jossa juhlasali on (Linnake tai Muuri, jos ei jaettu).
/// </summary>
public static partial class BeatEmUpSetup
{
    const string BossRoom1AreaName = "Muurin pomohuone 1";
    const float BossRoom1X0 = 79180f, BossRoomImgW = 2172f, BossRoomImgH = 724f, BossRoomLeftDoorPx = 141f, BossRoomRightDoorPx = 2172f + 2009f, HallBossDoorPx = 4f * 1672f + 1530f, BossRoomFloorRow = 560f;

    [MenuItem("Beat em up/351. Linnake: ensimmäinen pomohuone (tausta, ovi salista, oikea ovi lukossa)", false, 351)]
    static void AddFortressBossRoom1()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        foreach (var f in new[] { "muuri_pomohuone_1", "muuri_pomohuone_2", "muuri_avaruus" })
            if (!File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat)."); return; }
        string hallScene = AreaSceneName("Muurin juhlahalli");
        if (hallScene == null) { Info("Juhlasalia ei löytynyt: aja valikko 340 tai 330."); return; }
        string scenePath = "Assets/Scenes/Areas/" + hallScene + ".unity";
        var opened = new List<UnityEngine.SceneManagement.Scene>();
        var sc = EditorSceneManager.GetSceneByPath(scenePath);
        if (!sc.IsValid() || !sc.isLoaded) { sc = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive); opened.Add(sc); }
        try
        {
            var hall = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Muurin juhlahalli" && a.gameObject.scene == sc);
            if (hall == null) { Info("Juhlasalin aluetta ei löytynyt sceneltä " + hallScene + "."); return; }
            foreach (var n in new[] { "Muurin pomohuone 1", "Alue: Muurin pomohuone 1" })
                foreach (var o in sc.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);
            foreach (var d in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(d => d != null && d.gameObject.scene == sc && d.name.StartsWith("Pomohuone 1") || d != null && d.gameObject.scene == sc && d.name == "Juhlahalli: pomohuoneeseen").ToArray())
                Undo.DestroyObjectImmediate(d.gameObject);

            float ppu = BossRoomImgH / (2f * CamHalf);   // huoneen kuva täyttää kameran korkeuden (724 px = 12 yksikköä)
            float H = 2f * CamHalf, W = 1672f / (941f / H), cy = CamY, bottom = cy - CamHalf, top = cy + CamHalf;
            float widthPx = 2f * BossRoomImgW, X0 = BossRoom1X0;
            GameObject NewRoot(string n)
            {
                var g = new GameObject(n);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(g, sc);
                Undo.RegisterCreatedObjectUndo(g, "Pomohuone 1");
                return g;
            }
            var root = NewRoot("Muurin pomohuone 1");
            SpriteRenderer Put(string name, Sprite sp, Vector3 pos, int order)
            {
                var sr = new GameObject(name).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = order; sr.transform.position = pos;
                return sr;
            }
            var s1 = ImportShipSprite(T + "muuri_pomohuone_1.png", ppu, false, SpriteAlignment.Center);
            var s2 = ImportShipSprite(T + "muuri_pomohuone_2.png", ppu, false, SpriteAlignment.Center);
            if (s1 == null || s2 == null) { Info("Pomohuoneen kuvien tuonti epäonnistui."); return; }
            Put("Pomohuone 1 (vasen)", s1, new Vector3(X0 + s1.bounds.size.x * 0.5f, cy, 0f), -10000);
            Put("Pomohuone 1 (oikea)", s2, new Vector3(X0 + s1.bounds.size.x + s2.bounds.size.x * 0.5f, cy, 0f), -10000);

            // alue
            var room = ReuseArea("Alue: Muurin pomohuone 1");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(room.gameObject, sc);
            float widthU = widthPx / ppu;
            room.areaName = BossRoom1AreaName; room.useSidewalk = false;
            room.maxDepthY = top - BossRoomFloorRow / ppu; room.minDepthY = bottom + 0.5f;
            room.walkMinX = X0 + 1.5f; room.walkMaxX = X0 + widthU - 1.2f;
            room.camMinX = X0 + W * 0.5f; room.camMaxX = Mathf.Max(room.camMinX, X0 + widthU - W * 0.5f);
            room.camSize = CamHalf; room.camRiseY = 0f; room.camOffsetY = 0f; room.backScale = 1f; room.depthLimits = new Vector2[0];
            room.music = hall.music; room.musicGain = hall.musicGain;

            // ikkunoiden takana avaruus (vasemman kuvan ikkunat ovat läpinäkyviä); sama kuva kuin juhlasalissa, hidas parallaksi
            var spc = ImportShipSprite(T + "muuri_avaruus.png", ppu, false, SpriteAlignment.Center);
            if (spc != null)
            {
                float sh = spc.bounds.size.y, k = Mathf.Max(1f, H * 1.02f / sh);
                var star = Put("Ikkunoiden avaruus", spc, new Vector3(room.camMinX - W * 0.5f + spc.bounds.size.x * k * 0.5f, cy, 0f), -10150);
                star.transform.localScale = new Vector3(k, k, 1f);
                var pl = star.gameObject.AddComponent<ParallaxLayer>();
                pl.factor = 0.02f; pl.startCamX = room.camMinX; pl.minX = room.camMinX - 2f; pl.maxX = room.camMaxX + 2f;
            }

            // ovet
            var doorRoot = sc.GetRootGameObjects().FirstOrDefault(g => g.name == "Muurin ovet") ?? NewRoot("Muurin ovet");
            Door MakeDoor(string name, string prompt, Area here, Area target, Vector3 pos, Vector2 spawn)
            {
                var d = new GameObject(name).AddComponent<Door>();
                d.transform.SetParent(doorRoot.transform, false); d.transform.position = pos;
                d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = spawn; d.halfWidth = 1.6f; d.maxDistanceFromWall = 2.2f;
                return d;
            }
            float hallDoorX = WallHallX0 + HallBossDoorPx / HallPpu, roomLeftX = X0 + BossRoomLeftDoorPx / ppu, roomRightX = X0 + BossRoomRightDoorPx / ppu;
            float midY(Area a) => Mathf.Lerp(a.maxDepthY, a.minDepthY, 0.4f);
            MakeDoor("Juhlahalli: pomohuoneeseen", "Mene pomohuoneeseen", hall, room, new Vector3(hallDoorX, hall.maxDepthY, 0f), new Vector2(roomLeftX + 2.8f, midY(room)));
            MakeDoor("Pomohuone 1: takaisin saliin", "Takaisin saliin", room, hall, new Vector3(roomLeftX, room.maxDepthY, 0f), new Vector2(hallDoorX - 2.8f, midY(hall)));
            var fwd = MakeDoor("Pomohuone 1: ovi eteenpäin", "Mene eteenpäin", room, room, new Vector3(roomRightX, room.maxDepthY, 0f), new Vector2(roomRightX - 2.8f, midY(room)));
            fwd.requiredClue = "Pomohuone 1: avain"; fwd.lockedClueText = "Ovi on lukossa.";   // seuraava huone tulee myöhemmin

            EditorUtility.SetDirty(room);
            EditorSceneManager.MarkSceneDirty(sc); EditorSceneManager.SaveScene(sc);
            Info("Linnakkeen ensimmäinen pomohuone rakennettu (x " + X0.ToString("0") + "–" + (X0 + widthU).ToString("0") + ", scene " + hallScene + "): taustat, ikkunoiden avaruus ja ovet: salin oikean pään ovi -> huoneen vasen ovi, takaisin, ja oikea ovi lukossa. Pomoa ei ole vielä. Ajamatta Unityssä.");
        }
        finally { foreach (var s in opened) if (s.IsValid()) EditorSceneManager.CloseScene(s, true); }
    }
}
