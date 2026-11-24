using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Valikko 431: WTF-areenan sisätila "WTF-areenan aula" (aula + narikka + WC + baari + ravintola, kolme 5000x1884-kuvaa peräkkäin, x 124000).
/// Ovet: kadun areenan sisäänkäynti <-> aulan vasemman vinoseinän lasiovet (EXIT); WC-ovet -> areenan miesten/naisten vessa (Vegasin vessojen kuvat, x 125000/126000);
/// oikean vinoseinän "SEATING / ARENA" -ovi lukossa, kunnes katsomo ja kehä tehdään. Vinoseinien kohdalla takaraja seuraa lattialinjaa. Aja ensin 430.
/// </summary>
public static partial class BeatEmUpSetup
{
    const float VegasArenaLobbyX0 = 124000f, VegasArenaWcX0Men = 125000f, VegasArenaWcX0Women = 126000f;
    public const string ArenaSeatingClue = "Vegas: areenan katsomo";

    [MenuItem("Beat em up/431. Las Vegas: WTF-areenan aula ja ravintola (ovet kadulle ja vessoihin, katsomon ovi lukossa)", false, 431)]
    static void AddVegasArenaLobby()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        var files = new[] { "vegas_areena_aula", "vegas_areena_ravintola_1", "vegas_areena_ravintola_2" };
        var wcFiles = new[] { "vegas_vessa_pesu", "vegas_vessa_kopit" };
        foreach (var f in files.Concat(wcFiles))
        {
            if (!File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat)."); return; }
            AssetDatabase.ImportAsset(T + f + ".png", ImportAssetOptions.ForceUpdate);
        }
        if (!File.Exists(VegasScenePath)) { Info("Aja ensin valikko 376 (Vegas)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var street = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.gameObject.scene == scene && a.areaName == "Vegasin katu");
            SpriteRenderer entrance = null;
            foreach (var r in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (r.gameObject.scene == scene && r.gameObject.name.StartsWith("Areena 2 sisäänkäynti")) { entrance = r; break; }
            if (street == null || entrance == null) { Info("Vegasin katu tai areenan julkisivu puuttuu: aja valikko 430."); return; }
            foreach (var n in new[] { "WTF-areenan aula", "WTF-areenan miesten vessa", "WTF-areenan naisten vessa", "WTF-areenan ovet" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);

            float H = 2f * CamHalf, cy = CamY, top = cy + CamHalf, X0 = VegasArenaLobbyX0;
            float W = 1672f / (941f / H);
            float ppu = 1884f / H, imgW = 5000f / ppu, totalW = files.Length * imgW;
            float PX(int img, float px) => X0 + img * imgW + px / ppu;   // kuvan pikseli -> maailman x
            float PY(float py) => top - py / ppu;                         // kuvan rivi -> maailman y

            var root = new GameObject("WTF-areenan aula");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "WTF-areenan aula");
            var lobby = ReuseArea("Alue: WTF-areenan aula");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lobby.gameObject, scene);
            lobby.areaName = "WTF-areenan aula"; lobby.useSidewalk = false;
            lobby.maxDepthY = PY(1245f); lobby.minDepthY = top - 0.985f * H;   // takaseinän alareuna rivillä ~1230
            lobby.walkMinX = PX(0, 60f); lobby.walkMaxX = PX(2, 4940f);
            lobby.camMinX = X0 + W * 0.5f; lobby.camMaxX = Mathf.Max(lobby.camMinX, X0 + totalW - W * 0.5f);
            lobby.camSize = CamHalf; lobby.camRiseY = 0f; lobby.camOffsetY = 0f; lobby.backScale = 1f;
            // vinot päätyseinät: takaraja seuraa seinän lattialinjaa (aulan vasen pää, ravintolan oikea pää)
            lobby.depthLimits = new[]
            {
                new Vector2(PX(0, 0f), PY(1560f)), new Vector2(PX(0, 900f), PY(1250f)), new Vector2(PX(0, 960f), PY(1245f)),
                new Vector2(PX(2, 3500f), PY(1245f)), new Vector2(PX(2, 3560f), PY(1250f)), new Vector2(PX(2, 4550f), PY(1420f)), new Vector2(PX(2, 5000f), PY(1580f)),
            };
            for (int i = 0; i < files.Length; i++)
            {
                var sp = ImportShipSprite(T + files[i] + ".png", ppu, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan tuonti epäonnistui: " + files[i]); return; }
                var sr = new GameObject("Areenan aula " + (i + 1) + ": " + files[i]).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10000;
                sr.transform.position = new Vector3(X0 + imgW * (i + 0.5f), cy, 0f);
                sr.transform.localScale = new Vector3(1.003f, 1f, 1f);
            }
            EditorUtility.SetDirty(lobby);

            var doorRoot = new GameObject("WTF-areenan ovet");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, scene);
            Undo.RegisterCreatedObjectUndo(doorRoot, "WTF-areenan ovet");
            Door MakeDoor(string name, string prompt, Area here, Area target, float x, Vector2 spawn)
            {
                var d = new GameObject(name).AddComponent<Door>();
                d.transform.SetParent(doorRoot.transform, false); d.transform.position = new Vector3(x, here.MaxDepthAtX(x, here.maxDepthY) - 0.3f, 0f);
                d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = spawn;
                d.halfWidth = 1.6f; d.maxDistanceFromWall = 2.4f; d.onGround = true; d.groundHalfDepth = 1.6f;
                return d;
            }
            Vector2 At(Area a, float x) => new Vector2(x, Mathf.Lerp(a.MaxDepthAtX(x, a.maxDepthY), a.minDepthY, 0.12f));

            // katu <-> aulan lasiovet (vasen vinoseinä, EXIT)
            float streetDoorX = entrance.transform.position.x, exitX = PX(0, 530f);
            MakeDoor("Vegas: areenan ovet sisään", "Mene WTF-areenaan", street, lobby, streetDoorX, At(lobby, exitX + 2.2f));
            MakeDoor("Vegas: areenan aulasta kadulle", "Mene ulos kadulle", lobby, street, exitX, At(street, streetDoorX));

            // katsomo / areena: oikean vinoseinän yhteinen ovi, lukossa kunnes katsomo ja kehä rakennetaan
            var seat = MakeDoor("Vegas: areenan katsomon ovi", "Mene katsomoon", lobby, null, PX(2, 4210f), Vector2.zero);
            seat.requiredClue = ArenaSeatingClue; seat.lockedClueText = "Katsomon ovet ovat vielä kiinni.";
            // katsomo jo rakennettu (434): ovi auki sinne
            var ringArea = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.gameObject.scene == scene && a.areaName == ArenaRingAreaName);
            if (ringArea != null)
            {
                float rx = ringArea.walkMinX + 4.1f;   // sama kuin 434: oviaukon oikealla puolella
                seat.target = ringArea; seat.requiredClue = ""; seat.spawnPoint = new Vector2(rx, Mathf.Lerp(ringArea.MaxDepthAtX(rx, ringArea.maxDepthY), ringArea.minDepthY, 0.12f));
                var back = Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(d => d.gameObject.scene == scene && d.gameObject.name == "Vegas: katsomosta aulaan");
                if (back != null) { back.target = lobby; back.spawnPoint = At(lobby, seat.transform.position.x); EditorUtility.SetDirty(back); }
            }

            // WC: miesten ja naisten ovi -> vessat (Vegasin vessojen kuvat), vessan vasen ovi takaisin aulaan
            var areasPath = VegasScenePath.Replace(".unity", ".areas.txt");
            var names = File.Exists(areasPath) ? File.ReadAllLines(areasPath).ToList() : new System.Collections.Generic.List<string>();
            float wcPpu = 1844f / H, wcW = 5120f / wcPpu, wcTotal = wcFiles.Length * wcW;
            foreach (var (areaName, wx0, doorPx, prompt) in new[] { ("WTF-areenan miesten vessa", VegasArenaWcX0Men, 3656f, "Mene miesten vessaan"), ("WTF-areenan naisten vessa", VegasArenaWcX0Women, 4172f, "Mene naisten vessaan") })
            {
                var wroot = new GameObject(areaName);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(wroot, scene);
                Undo.RegisterCreatedObjectUndo(wroot, areaName);
                var area = ReuseArea("Alue: " + areaName);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(area.gameObject, scene);
                area.areaName = areaName; area.useSidewalk = false;
                area.maxDepthY = top - 0.60f * H; area.minDepthY = top - 0.985f * H;
                area.walkMinX = wx0 + wcW * 0.04f; area.walkMaxX = wx0 + wcTotal - wcW * 0.04f;
                area.camMinX = wx0 + W * 0.5f; area.camMaxX = Mathf.Max(area.camMinX, wx0 + wcTotal - W * 0.5f);
                area.camSize = CamHalf; area.camRiseY = 0f; area.camOffsetY = 0f; area.backScale = 1f; area.depthLimits = new Vector2[0];
                for (int i = 0; i < wcFiles.Length; i++)
                {
                    var sp = ImportShipSprite(T + wcFiles[i] + ".png", wcPpu, false, SpriteAlignment.Center);
                    if (sp == null) { Info("Kuvan tuonti epäonnistui: " + wcFiles[i]); return; }
                    var sr = new GameObject("Vessa " + (i + 1)).AddComponent<SpriteRenderer>();
                    sr.transform.SetParent(wroot.transform, false); sr.sprite = sp; sr.sortingOrder = -10000;
                    sr.transform.position = new Vector3(wx0 + wcW * (i + 0.5f), cy, 0f);
                }
                float inX = wx0 + wcW * 0.150f, outX = PX(0, doorPx);
                MakeDoor("Vegas: " + areaName + " ovi sisään", prompt, lobby, area, outX, new Vector2(inX + 1.2f, Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.12f)));
                MakeDoor("Vegas: " + areaName + " ovi ulos", "Mene takaisin aulaan", area, lobby, inX, At(lobby, outX));
                EditorUtility.SetDirty(area);
                if (!names.Contains(areaName)) names.Add(areaName);
            }
            if (!names.Contains("WTF-areenan aula")) names.Add("WTF-areenan aula");
            File.WriteAllLines(areasPath, names);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("WTF-areenan aula ja ravintola rakennettu (x " + X0.ToString("0") + "–" + (X0 + totalW).ToString("0") + "): kadun sisäänkäynti <-> aulan lasiovet, WC-ovet vessoihin (x " + VegasArenaWcX0Men.ToString("0") + "/" + VegasArenaWcX0Women.ToString("0") + "), katsomon ovi lukossa (\"" + ArenaSeatingClue + "\").");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    public static void RunVegasArenaLobbyBatch()
    {
        batch = true;
        try { Step("431 areenan aula", AddVegasArenaLobby); }
        finally { batch = false; }
    }
}
