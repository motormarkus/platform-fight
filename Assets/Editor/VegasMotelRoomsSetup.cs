using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Valikko 404: motellin huoneet ovien taakse (ei vastaanottoa). 8 huonetta: alakerta 101–103 (katutason ovet), yläkerta 201–205 (parvekkeen ovet).
/// Kolme sisustusta vuorotellen (turkoosi, persikka, puupaneeli; kukin 10240x1844 kahtena 5120x1844-palana, Assets/Sprites/Taustat/vegas_motellihuone_*_1/2.png).
/// Huoneen ovi kuvan vasemmassa reunassa = ovi takaisin ulos (kadulle tai parvekkeelle). Huoneet omina alueinaan x 113000 + i * 400.
/// Oven kohdat motellikuvassa (10240 leveä, ovet levennetty: Tools/MotelAssets/widen_doors.py). Aja valikko 403 ensin.
/// </summary>
public static partial class BeatEmUpSetup
{
    const float VegasMotelRoomX0 = 113000f, VegasMotelRoomStep = 400f;

    [MenuItem("Beat em up/404. Las Vegas: motellin huoneet ovien taakse (101–103 alhaalla, 201–205 parvekkeella)", false, 404)]
    static void AddVegasMotelRooms()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        string[] styles = { "turkoosi", "persikka", "puupaneeli" };
        foreach (var s in styles) for (int i = 1; i <= 2; i++)
            if (!File.Exists(T + "vegas_motellihuone_" + s + "_" + i + ".png")) { Info("Puuttuu kuva vegas_motellihuone_" + s + "_" + i + ".png (Assets/Sprites/Taustat)."); return; }
        if (!File.Exists(VegasScenePath)) { Info("Aja ensin valikko 376 (Vegas)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var street = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a != null && a.gameObject.scene == scene && a.areaName == "Vegasin katu");
            var motelRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Vegasin motelli");
            var motel = motelRoot != null ? motelRoot.GetComponent<SiteLevels>() : null;
            if (street == null || motel == null) { Info("Alue \"Vegasin katu\" tai motelli puuttuu: aja valikko 403."); return; }
            foreach (var o in scene.GetRootGameObjects().Where(g => g.name == "Motellin huoneet" || g.name == "Motellin huoneiden ovet" || g.name.StartsWith("Alue: Motellihuone ")).ToArray()) Undo.DestroyObjectImmediate(o);

            float H = 2f * CamHalf, cy = CamY, top = cy + CamHalf;
            float ppu = 1844f / H, imgW = 5120f / ppu, W = 1672f / (941f / H), totalW = 2f * imgW;
            float mx0 = motel.zoneMinX;
            float MX(float px) => mx0 + px / ppu;
            float streetY = motel.streetMax - 0.3f, balconyY = motel.levelY[1];

            var root = new GameObject("Motellin huoneet");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Motellin huoneet");
            var doorRoot = new GameObject("Motellin huoneiden ovet");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, scene);
            Undo.RegisterCreatedObjectUndo(doorRoot, "Motellin ovet");
            Door MakeDoor(string name, string prompt, Area here, Area target, float x, float y, Vector2 spawn, float halfW, float halfD)
            {
                var d = new GameObject(name).AddComponent<Door>();
                d.transform.SetParent(doorRoot.transform, false); d.transform.position = new Vector3(x, y, 0f);
                d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = spawn;
                d.halfWidth = halfW; d.maxDistanceFromWall = 2.4f; d.onGround = true; d.groundHalfDepth = halfD;
                return d;
            }

            // (huonenumero, oven keskikohta motellikuvassa px, kerros 0 = katu / 1 = parveke)
            var rooms = new List<(int no, float px, int lv)>
            {
                (101, 3172f, 0), (102, 4834f, 0), (103, 6338f, 0),
                (201, 1572f, 1), (202, 3140f, 1), (203, 4810f, 1), (204, 6342f, 1), (205, 7818f, 1),
            };
            var sprites = new Dictionary<string, Sprite>();
            foreach (var s in styles) for (int i = 1; i <= 2; i++)
            {
                var sp = ImportShipSprite(T + "vegas_motellihuone_" + s + "_" + i + ".png", ppu, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan tuonti epäonnistui: " + s + " " + i); return; }
                sprites[s + i] = sp;
            }
            for (int r = 0; r < rooms.Count; r++)
            {
                var (no, px, lv) = rooms[r];
                string style = styles[r % styles.Length], areaName = "Motellihuone " + no;
                float X0 = VegasMotelRoomX0 + r * VegasMotelRoomStep;
                var rr = new GameObject(areaName + " (" + style + ")"); rr.transform.SetParent(root.transform, false);
                for (int i = 0; i < 2; i++)
                {
                    var sr = new GameObject(areaName + " " + (i + 1)).AddComponent<SpriteRenderer>();
                    sr.transform.SetParent(rr.transform, false); sr.sprite = sprites[style + (i + 1)]; sr.sortingOrder = -10000;
                    sr.transform.position = new Vector3(X0 + imgW * (i + 0.5f), cy, 0f);
                }
                var area = ReuseArea("Alue: " + areaName);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(area.gameObject, scene);
                area.areaName = areaName; area.useSidewalk = false;
                area.maxDepthY = top - 0.58f * H; area.minDepthY = top - 0.985f * H;   // lattia alkaa jalkalistasta (~54 % kuvan korkeudesta)
                area.walkMinX = X0 + imgW * 0.05f; area.walkMaxX = X0 + totalW - imgW * 0.06f;
                area.camMinX = X0 + W * 0.5f; area.camMaxX = Mathf.Max(area.camMinX, X0 + totalW - W * 0.5f);
                area.camSize = CamHalf; area.camRiseY = 0f; area.camOffsetY = 0f; area.backScale = 1f; area.depthLimits = new Vector2[0];
                area.music = street.music; area.musicGain = street.musicGain;
                EditorUtility.SetDirty(area);

                // ovi: motellin ovi (katu tai parveke) <-> huoneen vasemman reunan ovi (~4,4 % kuvan leveydestä)
                float outX = MX(px), outY = lv == 0 ? streetY : balconyY;
                float inX = X0 + totalW * 0.044f, inY = Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.15f);
                MakeDoor("Motelli: ovi " + no + " sisään", "Mene huoneeseen " + no, street, area, outX, outY, new Vector2(inX + 2.2f, inY), 1.3f, lv == 0 ? 1.0f : 0.6f);
                MakeDoor("Motelli: huoneen " + no + " ovi ulos", lv == 0 ? "Mene ulos kadulle" : "Mene ulos parvekkeelle", area, street, inX, area.maxDepthY - 0.3f,
                         new Vector2(outX, lv == 0 ? motel.streetMax - 0.6f : balconyY - 0.05f), 1.8f, 1.6f);
            }
            var areasPath = VegasScenePath.Replace(".unity", ".areas.txt");   // alueen nimi -> scene (muut työkalut löytävät huoneet)
            var names = File.Exists(areasPath) ? File.ReadAllLines(areasPath).ToList() : new List<string>();
            foreach (var (no, _, _) in rooms) if (!names.Contains("Motellihuone " + no)) names.Add("Motellihuone " + no);
            File.WriteAllLines(areasPath, names);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Motellin huoneet lisätty: 101–103 (katutason ovet) ja 201–205 (parvekkeen ovet), sisustukset vuorotellen turkoosi / persikka / puupaneeli (x " + VegasMotelRoomX0.ToString("0") + "–" + (VegasMotelRoomX0 + (rooms.Count - 1) * VegasMotelRoomStep + totalW).ToString("0") + ").");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    /// <summary>Motellin huoneet (404).</summary>
    public static void RunVegasMotelRoomsBatch()
    {
        batch = true;
        try { Step("404 motellin huoneet", AddVegasMotelRooms); }
        finally { batch = false; }
    }
}
