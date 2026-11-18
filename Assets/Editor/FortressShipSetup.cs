using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Aluksen sisätilat (valikko 418): kuusi 5000 x 2400 -kuvaa (output/imagegen/linnake-rahtialus-sisatilat-6x5k-native) kolmeksi alueeksi:
/// "Aluksen cargo" (01 metallilaatikot + 02 verkkoruuma), "Aluksen hytti" (03 oleskelutila + 04 kanttiini + 05 lennonvalvonta), "Aluksen ohjaamo" (06). Ikkunoiden ja rungon reunojen takana avaruus.
/// Ovet: katon aluksen luukku -> cargon vasen pää; cargo -> hytti -> ohjaamo ja takaisin. Alueiden nimessä "Aluksen" -> pomopalkintoja ei anneta (BossPerks).
/// Vihut (kuurobotit cargoon, Gustav hyttiin, Lokki ohjaamoon) lisätään myöhemmin. Vaatii valikon 417 (katto). Ajo uudelleen turvallinen.
/// </summary>
public static partial class BeatEmUpSetup
{
    const float FortShipX0 = 79600f, FortShipImgPx = 5000f, FortShipImgPy = 2400f, FortShipFloorFar = 1520f, FortShipFloorNear = 1960f;

    [MenuItem("Beat em up/418. Linnake: aluksen sisätilat (cargo, hytti, ohjaamo)", false, 418)]
    static void AddFortressShip()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        string srcDir = "output/imagegen/linnake-rahtialus-sisatilat-6x5k-native/";
        string[] srcNames = { "01-metallilaatikot.png", "02-verkkoruuma.png", "03-oleskelutila.png", "04-kanttiini.png", "05-lennonvalvonta.png", "06-ohjaamo.png" };
        for (int i = 0; i < 6; i++)
        {
            string d = T + "alus_sisa_" + (i + 1) + ".png";
            if (!File.Exists(d)) { if (!File.Exists(srcDir + srcNames[i])) { Info("Puuttuu " + srcDir + srcNames[i]); return; } File.Copy(srcDir + srcNames[i], d, true); }
            AssetDatabase.ImportAsset(d, ImportAssetOptions.ForceUpdate);
        }
        if (!File.Exists(T + "muuri_avaruus.png")) { Info("Puuttuu " + T + "muuri_avaruus.png"); return; }

        string scenePath = "Assets/Scenes/Areas/" + (AreaSceneName("Muurin juhlahalli") ?? "Linnake") + ".unity";
        var opened = new System.Collections.Generic.List<UnityEngine.SceneManagement.Scene>();
        var sc = EditorSceneManager.GetSceneByPath(scenePath);
        if (!sc.IsValid() || !sc.isLoaded) { sc = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive); opened.Add(sc); }
        try
        {
            var all = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == sc).ToArray();
            var hall = all.FirstOrDefault(a => a.areaName == "Muurin juhlahalli");
            var roof = all.FirstOrDefault(a => a.areaName == FortRoofAreaName);
            if (hall == null || roof == null) { Info("Katto puuttuu: aja valikko 417 (ja 351, 352)."); return; }
            foreach (var n in new[] { "Aluksen sisätilat", "Alue: Aluksen cargo", "Alue: Aluksen hytti", "Alue: Aluksen ohjaamo" })
                foreach (var o in sc.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);

            float H = 2f * CamHalf, ppu = 1844f / H, W = 1672f / (941f / H), cy = CamY, top = cy + CamHalf;
            float imgW = FortShipImgPx / ppu;                       // yhden kuvan leveys (yksikköä)
            var root = new GameObject("Aluksen sisätilat"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, sc); Undo.RegisterCreatedObjectUndo(root, "Aluksen sisätilat");
            SpriteRenderer Put(string name, Sprite sp, Vector3 pos, int order)
            {
                var sr = new GameObject(name).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = order; sr.transform.position = pos; return sr;
            }
            var sprites = new Sprite[6];
            for (int i = 0; i < 6; i++) { sprites[i] = ImportShipSprite(T + "alus_sisa_" + (i + 1) + ".png", ppu, false, SpriteAlignment.Center); if (sprites[i] == null) { Info("Sisätilakuvan " + (i + 1) + " tuonti epäonnistui."); return; } }
            var space = ImportShipSprite(T + "muuri_avaruus.png", ppu, false, SpriteAlignment.Center);

            // lattia: kuvan (2400 px) rivit 1520-1960; kuvan keskipiste on kameran keskellä (cy)
            float yFar = cy + (FortShipImgPy * 0.5f - FortShipFloorFar) / ppu, yNear = cy + (FortShipImgPy * 0.5f - FortShipFloorNear) / ppu;
            Area MakeArea(string goName, string areaName, float x0, int firstImg, int count)
            {
                for (int i = 0; i < count; i++) Put(areaName + " " + (i + 1), sprites[firstImg + i], new Vector3(x0 + imgW * (i + 0.5f), cy, 0f), -10000);
                var a = ReuseArea(goName);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(a.gameObject, sc);
                float widthU = imgW * count;
                a.areaName = areaName; a.useSidewalk = false;
                a.maxDepthY = yFar; a.minDepthY = yNear;
                a.walkMinX = x0 + 1.5f; a.walkMaxX = x0 + widthU - 1.5f;
                a.camMinX = x0 + W * 0.5f; a.camMaxX = Mathf.Max(a.camMinX, x0 + widthU - W * 0.5f);
                a.useCeiling = true; a.ceilingY = cy + (FortShipImgPy * 0.5f - 330f) / ppu;   // kuvan katto ~330 px ylhäältä
                a.camSize = CamHalf; a.camRiseY = 0f; a.camOffsetY = 0f; a.backScale = 1f; a.depthLimits = new Vector2[0];
                var baseMus = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Musiikki/Lunar Base.mp3"); a.music = baseMus != null ? baseMus : hall.music; a.musicGain = baseMus != null ? 1f : hall.musicGain;   // kuun tukikohdan musiikki (Lunar Base): loppumusiikki toistaiseksi, ei vielä lopullinen
                if (space != null)
                {
                    float sh = space.bounds.size.y, k = Mathf.Max(1f, FortShipImgPy / ppu * 1.02f / sh);
                    var star = Put(areaName + ": avaruus", space, new Vector3(a.camMinX - W * 0.5f + space.bounds.size.x * k * 0.5f, cy, 0f), -10150);
                    star.transform.localScale = new Vector3(k, k, 1f);
                    var sd = star.gameObject.AddComponent<StarDrift>();   // tähdet lentävät ikkunoiden takana jatkuvasti (alus lentää)
                    sd.speed = 5f; sd.minX = a.camMinX - 2f; sd.maxX = a.camMaxX + 2f;
                }
                EditorUtility.SetDirty(a);
                return a;
            }
            float cargoX0 = FortShipX0, cargoW = 2f * imgW, hytX0 = cargoX0 + cargoW + 20f, hytW = 3f * imgW, cockX0 = hytX0 + hytW + 20f;
            var cargo = MakeArea("Alue: Aluksen cargo", "Aluksen cargo", cargoX0, 0, 2);
            var hytti = MakeArea("Alue: Aluksen hytti", "Aluksen hytti", hytX0, 2, 3);
            var cockpit = MakeArea("Alue: Aluksen ohjaamo", "Aluksen ohjaamo", cockX0, 5, 1);

            // ovet
            var doorRoot = sc.GetRootGameObjects().FirstOrDefault(g => g.name == "Muurin ovet") ?? new GameObject("Muurin ovet");
            if (doorRoot.scene != sc) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, sc);
            foreach (var d in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(d => d != null && d.gameObject.scene == sc && d.name.StartsWith("Alus: ")).ToArray()) Undo.DestroyObjectImmediate(d.gameObject);
            float midY(Area a) => Mathf.Lerp(a.maxDepthY, a.minDepthY, 0.45f);
            void Door2(string name, string prompt, Area here, Area target, float x, float spawnX)
            {
                var d = new GameObject(name).AddComponent<Door>();
                d.transform.SetParent(doorRoot.transform, false); d.transform.position = new Vector3(x, here.maxDepthY, 0f);
                d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = new Vector2(spawnX, midY(target)); d.halfWidth = 1.6f; d.maxDistanceFromWall = 2.4f;
            }
            float edge = 70f / ppu;   // ovipilari ~ 70 px kuvan reunasta
            // katon luukku: aluksen perän luukku (kuvan x ~ 820 px, alus 1,3 ×)
            float roofW = 5f * (5120f / ppu), shipWu = (5120f * 1.3f) / ppu, shipLeft = FortRoofX0 + roofW - 200f / ppu - shipWu, hatchX = shipLeft + 820f * 1.3f / ppu;
            Door2("Alus: katolta cargoon", "Astu alukseen", roof, cargo, hatchX, cargo.walkMinX + 3f);
            { var hd = doorRoot.GetComponentsInChildren<Door>().First(d => d.name == "Alus: katolta cargoon"); hd.halfWidth = 4f; hd.maxDistanceFromWall = 50f; }   // luukku löytyy helposti: koko syvyys, +-6 yks.
            Door2("Alus: cargosta katolle", "Poistu alukselta", cargo, roof, cargoX0 + edge, hatchX - 2.8f);
            Door2("Alus: cargosta hyttiin", "Mene hyttiin", cargo, hytti, cargoX0 + cargoW - edge, hytti.walkMinX + 3f);
            Door2("Alus: hytistä cargoon", "Takaisin cargoon", hytti, cargo, hytX0 + edge, cargo.walkMaxX - 3f);
            Door2("Alus: hytistä ohjaamoon", "Mene ohjaamoon", hytti, cockpit, hytX0 + hytW - edge, cockpit.walkMinX + 3f);
            Door2("Alus: ohjaamosta hyttiin", "Takaisin hyttiin", cockpit, hytti, cockX0 + edge, hytti.walkMaxX - 3f);

            EditorSceneManager.MarkSceneDirty(sc); EditorSceneManager.SaveScene(sc);
            Info("Aluksen sisätilat rakennettu: cargo x " + cargoX0.ToString("0") + "–" + (cargoX0 + cargoW).ToString("0") + ", hytti x " + hytX0.ToString("0") + "–" + (hytX0 + hytW).ToString("0") + ", ohjaamo x " + cockX0.ToString("0") + "–" + (cockX0 + imgW).ToString("0")
                + ". Ovet: katon luukku (x " + hatchX.ToString("0.0") + ") -> cargo -> hytti -> ohjaamo ja takaisin. Lattia rivit " + FortShipFloorFar + "-" + FortShipFloorNear + ". Vihuja ei vielä.");
        }
        finally { foreach (var s in opened) if (s.IsValid()) EditorSceneManager.CloseScene(s, true); }
    }
}
