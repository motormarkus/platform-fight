using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Vegasin kasinon aula (valikko 407): kaksi kuvaa vegas_aula_1 (vasen ovi, lasiovet oikeassa reunassa) ja vegas_aula_2 (lasiovet vasemmassa reunassa, oikea ovi).
/// Ovet: kasinon käytävän vasen ovi <-> aulan vasen ovi, showbaarin oikea ovi <-> aulan oikea ovi, kadun kasinon ovi <-> aulan lasiovet (kuvien rajalla).
/// Siirtää kasinon vasemman oven (ennen: katu) ja kadun kasinon oven kohteet aulaan. Aja valikot 376, 378 ja 406 ensin ja tämä viimeisenä (kadun/kasinon valikon uudelleenajo palauttaa ovet suoraan).</summary>
public static partial class BeatEmUpSetup
{
    const float VegasLobbyX0 = 122000f;

    [MenuItem("Beat em up/407. Las Vegas: kasinon aula (kasino, showbaari ja katu)", false, 407)]
    static void AddVegasLobby()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        var files = new[] { "vegas_aula_1", "vegas_aula_2" };
        foreach (var f in files)
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
            var areas = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a.gameObject.scene == scene).ToArray();
            Area Get(string n) => areas.FirstOrDefault(a => a.areaName == n);
            var casino = Get("Vegasin kasino"); var street = Get("Vegasin katu"); var show = Get("Vegasin showbaari");
            if (casino == null || street == null) { Info("Alue \"Vegasin kasino\" tai \"Vegasin katu\" puuttuu: aja valikot 376 ja 378."); return; }
            if (show == null) { Info("Alue \"Vegasin showbaari\" puuttuu: aja valikko 406."); return; }
            foreach (var n in new[] { "Vegasin aula", "Vegasin aula ovet" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);

            float H = 2f * CamHalf, cy = CamY, top = cy + CamHalf, X0 = VegasLobbyX0;
            float W = 1672f / (941f / H);
            float ppuImg = 1844f / H, imgW = 5120f / ppuImg, totalW = files.Length * imgW;
            var root = new GameObject("Vegasin aula");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Vegasin aula");
            var lobby = ReuseArea("Alue: Vegasin aula");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lobby.gameObject, scene);
            lobby.areaName = "Vegasin aula"; lobby.useSidewalk = false;
            lobby.maxDepthY = top - 0.66f * H; lobby.minDepthY = top - 0.985f * H;   // seinän alareuna ~65 % korkeudesta
            lobby.walkMinX = X0 + imgW * 0.04f; lobby.walkMaxX = X0 + totalW - imgW * 0.04f;
            lobby.camMinX = X0 + W * 0.5f; lobby.camMaxX = Mathf.Max(lobby.camMinX, X0 + totalW - W * 0.5f);
            lobby.camSize = CamHalf; lobby.camRiseY = 0f; lobby.camOffsetY = 0f; lobby.backScale = 1f; lobby.depthLimits = new Vector2[0];
            for (int i = 0; i < files.Length; i++)
            {
                var sp = ImportShipSprite(T + files[i] + ".png", ppuImg, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan tuonti epäonnistui: " + files[i]); return; }
                var sr = new GameObject("Aula " + (i + 1)).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10000;
                sr.transform.position = new Vector3(X0 + imgW * (i + 0.5f), cy, 0f);
                sr.transform.localScale = new Vector3(1.003f, 1f, 1f);
            }
            EditorUtility.SetDirty(lobby);

            // lasiovien läpi näkyy Vegasin yö: samat taustatasot kuin kadulla (taivas, hotellit, palmut); aulan kuvat ovat oven aukoissa läpinäkyviä
            {
                var layers = new[] { ("Taivas", "vegas_taivas", -10100, 0.02f, 0), ("Hotellit", "vegas_hotellit", -10050, 0.25f, 1), ("Palmut", "vegas_palmut", -10040, 0.5f, 1) };
                float left = lobby.camMinX - W * 0.5f;
                foreach (var (lname, lfile, order, factor, mirrors) in layers)
                {
                    var lsp = ImportShipSprite(T + lfile + ".png", ppuImg, false, SpriteAlignment.Center);
                    if (lsp == null) { Info("Kuvan tuonti epäonnistui: " + lfile); return; }
                    var lsr = new GameObject("Aula: " + lname).AddComponent<SpriteRenderer>();
                    lsr.transform.SetParent(root.transform, false); lsr.sprite = lsp; lsr.sortingOrder = order;
                    lsr.transform.position = new Vector3(left + lsp.bounds.size.x * 0.5f, cy + (lfile == "vegas_taivas" ? 0f : 0.325f * H), 0f);
                    var pl = lsr.gameObject.AddComponent<ParallaxLayer>();
                    pl.factor = factor; pl.mirrorCopies = mirrors;
                    pl.startCamX = lobby.camMinX; pl.minX = lobby.camMinX - 2f; pl.maxX = lobby.camMaxX + 2f;
                }
            }

            var doorRoot = new GameObject("Vegasin aula ovet");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, scene);
            Undo.RegisterCreatedObjectUndo(doorRoot, "Vegasin aula ovet");
            Door MakeDoor(string name, string prompt, Area here, Area target, float x, Vector2 spawn)
            {
                var d = new GameObject(name).AddComponent<Door>();
                d.transform.SetParent(doorRoot.transform, false); d.transform.position = new Vector3(x, here.maxDepthY - 0.3f, 0f);
                d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = spawn;
                d.halfWidth = 1.6f; d.maxDistanceFromWall = 2.4f; d.onGround = true; d.groundHalfDepth = 1.6f;
                return d;
            }
            Vector2 At(Area a, float x) => new Vector2(x, Mathf.Lerp(a.maxDepthY, a.minDepthY, 0.12f));

            float lobbyLeft = X0 + imgW * 0.107f, lobbyGlass = X0 + imgW * 1.0f, lobbyRight = X0 + imgW * 1.898f;
            float casinoDoor = VegasCasinoX0 + imgW * 0.152f, showDoor = VegasShowbarX0 + imgW * 2.902f, streetDoor = VegasStreetX0 + imgW * 2.25f;

            // kasinon käytävän vasen ovi (ennen: katu) <-> aulan vasen ovi
            var allDoors = Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(d => d.gameObject.scene == scene).ToArray();
            var oldOut = allDoors.FirstOrDefault(d => d.name == "Vegas: kasinon vasen ovi ulos");
            if (oldOut != null) { oldOut.prompt = "Mene aulaan"; oldOut.target = lobby; oldOut.spawnPoint = At(lobby, lobbyLeft + 1.2f); EditorUtility.SetDirty(oldOut); }
            MakeDoor("Vegas: aulan vasen ovi kasinolle", "Mene kasinolle", lobby, casino, lobbyLeft, At(casino, casinoDoor + 1.2f));
            // showbaarin oikea ovi <-> aulan oikea ovi
            MakeDoor("Vegas: showbaarin oikea ovi aulaan", "Mene aulaan", show, lobby, showDoor, At(lobby, lobbyRight - 1.2f));
            MakeDoor("Vegas: aulan oikea ovi showbaariin", "Mene showbaariin", lobby, show, lobbyRight, At(show, showDoor - 1.2f));
            // aulan lasiovet <-> kadun kasinon ulko-ovi
            var oldIn = allDoors.FirstOrDefault(d => d.name == "Vegas: kasinon ulko-ovi sisään");
            if (oldIn != null) { oldIn.prompt = "Mene kasinon aulaan"; oldIn.target = lobby; oldIn.spawnPoint = At(lobby, lobbyGlass + 1.4f); EditorUtility.SetDirty(oldIn); streetDoor = oldIn.transform.position.x; }
            MakeDoor("Vegas: aulan lasiovet ulos", "Mene ulos kadulle", lobby, street, lobbyGlass, At(street, streetDoor));

            var areasPath = VegasScenePath.Replace(".unity", ".areas.txt");
            var names = File.Exists(areasPath) ? File.ReadAllLines(areasPath).ToList() : new System.Collections.Generic.List<string>();
            if (!names.Contains("Vegasin aula")) names.Add("Vegasin aula");
            File.WriteAllLines(areasPath, names);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Aula rakennettu (x " + X0.ToString("0") + "). Kasinon vasen ovi " + (oldOut != null ? "siirretty aulaan" : "EI LÖYTYNYT (aja valikko 376/378)") + ", kadun kasinon ovi " + (oldIn != null ? "siirretty aulan lasiovelle" : "EI LÖYTYNYT") + ", showbaarin oikea ovi <-> aulan oikea ovi.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
