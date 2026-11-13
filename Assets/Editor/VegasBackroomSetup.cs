using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Vegasin baarin bäkkäri (valikko 387): kuva vegas_bakkari.png (sohva, kitarat, peilipöytä; ovi vasemmalla). Baarin 3. kuvan oikea ovi (x ~92 %) vie sisään, bäkkärin ovi takaisin.
/// Sisällä: pokeripöytä lattialla sekä pelikoneet Turpaan Vaan, Flipper Flapper ja Tähtipyssy seinän vieressä (kopiot pelihallin koneista, valikko 254). Aja valikot 379 ja 253/254 ensin.</summary>
public static partial class BeatEmUpSetup
{
    const float VegasBackX0 = 108000f;

    [MenuItem("Beat em up/387. Las Vegas: bäkkäri (pokeripöytä, Tähtipyssy, Flipper Flapper, Turpaan Vaan)", false, 387)]
    static void AddVegasBackroom()
    {
        if (PlayModeBlocked()) return;
        const string img = "Assets/Sprites/Taustat/vegas_bakkari.png";
        if (!File.Exists(img)) { Info("Puuttuu kuva vegas_bakkari.png (Assets/Sprites/Taustat)."); return; }
        AssetDatabase.ImportAsset(img, ImportAssetOptions.ForceUpdate);
        if (!File.Exists(VegasScenePath)) { Info("Aja ensin valikko 376 (Vegas)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        var opened = new List<UnityEngine.SceneManagement.Scene>();
        try
        {
            var bar = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.gameObject.scene == scene && a.areaName == "Vegasin baari");
            if (bar == null) { Info("Alue \"Vegasin baari\" puuttuu: aja valikko 379."); return; }
            foreach (var n in new[] { "Vegasin bäkkäri", "Vegasin bäkkärin ovet", "Vegasin bäkkärin koneet" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);
            // koneiden mallit: pääscenen Pelihallin pelikoneet
            const string mainPath = "Assets/Scenes/SampleScene.unity";
            var mainScene = EditorSceneManager.GetSceneByPath(mainPath);
            if (File.Exists(mainPath) && (!mainScene.IsValid() || !mainScene.isLoaded)) { mainScene = EditorSceneManager.OpenScene(mainPath, OpenSceneMode.Additive); opened.Add(mainScene); }

            float H = 2f * CamHalf, cy = CamY, top = cy + CamHalf;
            float W = 1672f / (941f / H);
            float ppuImg = 461f / H, imgW = 1280f / ppuImg, X0 = VegasBackX0;
            var root = new GameObject("Vegasin bäkkäri");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Vegasin bäkkäri");
            var area = ReuseArea("Alue: Vegasin bäkkäri");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(area.gameObject, scene);
            area.areaName = "Vegasin bäkkäri"; area.useSidewalk = false;
            area.maxDepthY = top - 0.615f * H; area.minDepthY = top - 0.985f * H;   // seinän alareuna ~61 % korkeudesta
            area.walkMinX = X0 + imgW * 0.04f; area.walkMaxX = X0 + imgW * 0.96f;
            area.camMinX = X0 + W * 0.5f; area.camMaxX = Mathf.Max(area.camMinX, X0 + imgW - W * 0.5f);
            area.camSize = CamHalf; area.camRiseY = 0f; area.camOffsetY = 0f; area.backScale = 1f; area.depthLimits = new Vector2[0];
            var sp = ImportShipSprite(img, ppuImg, false, SpriteAlignment.Center);
            if (sp == null) { Info("Kuvan tuonti epäonnistui: vegas_bakkari"); return; }
            var sr = new GameObject("Bäkkäri").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10000;
            sr.transform.position = new Vector3(X0 + imgW * 0.5f, cy, 0f);

            // ovet: baarin 3. kuvan oikea ovi (x 92 %) <-> bäkkärin vasen ovi (x ~9 %)
            var doorRoot = new GameObject("Vegasin bäkkärin ovet");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, scene);
            Undo.RegisterCreatedObjectUndo(doorRoot, "Vegasin bäkkärin ovet");
            float inX = X0 + imgW * 0.09f, barX = VegasBarX0 + imgW * 2.919f;
            float inY = Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.12f), barY = Mathf.Lerp(bar.maxDepthY, bar.minDepthY, 0.12f);
            Door MakeDoor(string name, string prompt, Area here, Area target, float x, Vector2 spawn)
            {
                var d = new GameObject(name).AddComponent<Door>();
                d.transform.SetParent(doorRoot.transform, false); d.transform.position = new Vector3(x, here.maxDepthY - 0.3f, 0f);
                d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = spawn;
                d.halfWidth = 1.5f; d.maxDistanceFromWall = 2.4f; d.onGround = true; d.groundHalfDepth = 1.6f;
                return d;
            }
            MakeDoor("Vegas: bäkkärin ovi sisään", "Mene bäkkäriin", bar, area, barX, new Vector2(inX + 1.4f, inY));
            MakeDoor("Vegas: bäkkärin ovi ulos", "Mene takaisin baariin", area, bar, inX, new Vector2(barX, barY));
            EditorUtility.SetDirty(area);

            // pelikoneet seinän viereen (sohvan vasemmalle puolelle)
            var mroot = new GameObject("Vegasin bäkkärin koneet");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(mroot, scene);
            Undo.RegisterCreatedObjectUndo(mroot, "Vegasin bäkkärin koneet");
            var report = new List<string>();
            var allT = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var (src, u) in new[] { ("Kone: Turpaan Vaan", 0.20f), ("Kone: Flipper Flapper", 0.29f), ("Kone: Tähtipyssy", 0.38f) })
            {
                var template = allT.FirstOrDefault(t => t != null && t.gameObject.scene != scene && t.name == src && t.parent != null && t.parent.name == "Pelihallin pelikoneet");
                if (template == null) { report.Add(src + ": mallia ei löytynyt (aja valikko 254)"); continue; }
                var go = Object.Instantiate(template.gameObject, mroot.transform);
                go.name = src; go.SetActive(true);
                go.transform.position = new Vector3(X0 + imgW * u, area.maxDepthY - 0.3f, 0f);
                report.Add(src.Replace("Kone: ", ""));
            }
            // pokeripöytä lattialle sohvan eteen
            string pk = PokerWorldTablesSetup.PlaceTableInArea(scene, area, X0 + imgW * 0.64f, Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.55f), "VegasBakkari");

            var areasPath = VegasScenePath.Replace(".unity", ".areas.txt");
            var names = File.Exists(areasPath) ? File.ReadAllLines(areasPath).ToList() : new List<string>();
            if (!names.Contains("Vegasin bäkkäri")) names.Add("Vegasin bäkkäri");
            File.WriteAllLines(areasPath, names);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Vegasin bäkkäri rakennettu (x " + X0.ToString("0") + "): ovet baarin oikeaan päätyyn, koneet: " + string.Join(", ", report) + ". " + pk);
        }
        finally
        {
            foreach (var sc in opened) if (sc.IsValid()) EditorSceneManager.CloseScene(sc, true);
            if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
