using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Vegasin Stripin baarin sisätila (valikko 379): kuvat vegas_baari_sisa_1 (ovet vasemmalla, jukebox, sohva, tiski alkaa) ja vegas_baari_sisa_2 (tiski jatkuu, lava) ja vegas_baari_sisa_3 (pääty).
/// Sisääntuloovet vasemmalla vievät kadulle ja kadun baarin ovi tänne. Oikealla on baarin pääty. Aja valikot 376 ja 378 ensin.</summary>
public static partial class BeatEmUpSetup
{
    const float VegasBarX0 = 105000f;

    [MenuItem("Beat em up/379. Las Vegas: baarin sisätila (ovet, tiski, lava)", false, 379)]
    static void AddVegasBar()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        var files = new[] { "vegas_baari_sisa_1", "vegas_baari_sisa_2", "vegas_baari_sisa_3" };
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
            var street = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.gameObject.scene == scene && a.areaName == "Vegasin katu");
            if (street == null) { Info("Alue \"Vegasin katu\" puuttuu: aja valikot 376 ja 378."); return; }
            foreach (var n in new[] { "Vegasin baari", "Vegasin baari ovet" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);

            float H = 2f * CamHalf, cy = CamY, top = cy + CamHalf, X0 = VegasBarX0;
            float W = 1672f / (941f / H);
            float ppuImg = 1844f / H, imgW = 5120f / ppuImg;     // kuvat 5120 x 1844
            float totalW = files.Length * imgW;
            var root = new GameObject("Vegasin baari");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Vegasin baari");

            var area = ReuseArea("Alue: Vegasin baari");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(area.gameObject, scene);
            area.areaName = "Vegasin baari"; area.useSidewalk = false;
            area.maxDepthY = top - 0.575f * H; area.minDepthY = top - 0.985f * H;   // seinän alareuna ~56 % korkeudesta, lattia sen alla
            area.walkMinX = X0 + imgW * 0.04f; area.walkMaxX = X0 + totalW - imgW * 0.03f;
            area.camMinX = X0 + W * 0.5f; area.camMaxX = Mathf.Max(area.camMinX, X0 + totalW - W * 0.5f);
            area.camSize = CamHalf; area.camRiseY = 0f; area.camOffsetY = 0f; area.backScale = 1f; area.depthLimits = new Vector2[0];

            for (int i = 0; i < files.Length; i++)
            {
                var sp = ImportShipSprite(T + files[i] + ".png", ppuImg, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan tuonti epäonnistui: " + files[i]); return; }
                var sr = new GameObject("Baari sisä " + (i + 1)).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10000;
                sr.transform.position = new Vector3(X0 + imgW * (i + 0.5f), cy, 0f);
            }

            // ovet: baarin sisääntuloovet (kuvan 1 vasen reuna, x ~11 %) <-> kadun baarin ovi (levennetyn baarikuvan x ~31 %)
            var doorRoot = new GameObject("Vegasin baari ovet");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, scene);
            Undo.RegisterCreatedObjectUndo(doorRoot, "Vegasin baari ovet");
            {
                float inX = X0 + imgW * 0.113f, outX = VegasStreetX0 + imgW * 1.309f;
                float inY = Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.10f), outY = Mathf.Lerp(street.maxDepthY, street.minDepthY, 0.12f);
                var toStreet = new GameObject("Vegas: baarin ovet ulos").AddComponent<Door>();
                toStreet.transform.SetParent(doorRoot.transform, false); toStreet.transform.position = new Vector3(inX, area.maxDepthY - 0.3f, 0f);
                toStreet.prompt = "Mene ulos kadulle"; toStreet.here = area; toStreet.target = street; toStreet.spawnPoint = new Vector2(outX, outY);
                toStreet.halfWidth = 1.8f; toStreet.maxDistanceFromWall = 2.4f; toStreet.onGround = true; toStreet.groundHalfDepth = 1.6f;
                var toBar = new GameObject("Vegas: kadun baarin ovi sisään").AddComponent<Door>();
                toBar.transform.SetParent(doorRoot.transform, false); toBar.transform.position = new Vector3(outX, street.maxDepthY - 0.3f, 0f);
                toBar.prompt = "Mene baariin"; toBar.here = street; toBar.target = area; toBar.spawnPoint = new Vector2(inX + 1.2f, inY);
                toBar.halfWidth = 1.8f; toBar.maxDistanceFromWall = 2.4f; toBar.onGround = true; toBar.groundHalfDepth = 1.6f;
            }
            EditorUtility.SetDirty(area);

            var areasPath = VegasScenePath.Replace(".unity", ".areas.txt");
            var names = File.Exists(areasPath) ? File.ReadAllLines(areasPath).ToList() : new System.Collections.Generic.List<string>();
            if (!names.Contains("Vegasin baari")) names.Add("Vegasin baari");
            File.WriteAllLines(areasPath, names);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Vegasin baari rakennettu (x " + X0.ToString("0") + "–" + (X0 + totalW).ToString("0") + "): kolme kuvaa, ovet vasemmalla <-> kadun baarin ovi.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
