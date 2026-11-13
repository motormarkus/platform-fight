using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Vegasin baarin vessat (valikko 381): miesten ja naisten vessa, molemmissa samat kaksi kuvaa (vegas_vessa_pesu: ovi vasemmalla + pesualtaat, vegas_vessa_kopit: kopit).
/// Baarin pään miesten (x ~52 %) ja naisten (x ~64 %) ovi vie sisään, vessan vasen ovi takaisin baariin. Bäkkärin ovi (x ~92 %) jää odottamaan. Aja valikot 376, 378 ja 379 ensin.</summary>
public static partial class BeatEmUpSetup
{
    const float VegasWcX0Men = 106000f, VegasWcX0Women = 107000f;

    [MenuItem("Beat em up/381. Las Vegas: baarin vessat (miehet, naiset)", false, 381)]
    static void AddVegasWc()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        var files = new[] { "vegas_vessa_pesu", "vegas_vessa_kopit" };
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
            var bar = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.gameObject.scene == scene && a.areaName == "Vegasin baari");
            if (bar == null) { Info("Alue \"Vegasin baari\" puuttuu: aja valikko 379."); return; }
            foreach (var n in new[] { "Vegasin miesten vessa", "Vegasin naisten vessa", "Vegasin vessat ovet" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);

            float H = 2f * CamHalf, cy = CamY, top = cy + CamHalf;
            float W = 1672f / (941f / H);
            float ppuImg = 1844f / H, imgW = 5120f / ppuImg;
            float totalW = files.Length * imgW;
            var doorRoot = new GameObject("Vegasin vessat ovet");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, scene);
            Undo.RegisterCreatedObjectUndo(doorRoot, "Vegasin vessat ovet");

            Door MakeDoor(string name, string prompt, Area here, Area target, float x, Vector2 spawn)
            {
                var d = new GameObject(name).AddComponent<Door>();
                d.transform.SetParent(doorRoot.transform, false); d.transform.position = new Vector3(x, here.maxDepthY - 0.3f, 0f);
                d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = spawn;
                d.halfWidth = 1.5f; d.maxDistanceFromWall = 2.4f; d.onGround = true; d.groundHalfDepth = 1.6f;
                return d;
            }

            var areasPath = VegasScenePath.Replace(".unity", ".areas.txt");
            var names = File.Exists(areasPath) ? File.ReadAllLines(areasPath).ToList() : new System.Collections.Generic.List<string>();
            float barBase = VegasBarX0 + 2f * imgW;   // baarin kolmas kuva

            foreach (var (areaName, X0, barDoorFrac, prompt) in new[] { ("Vegasin miesten vessa", VegasWcX0Men, 0.523f, "Mene miesten vessaan"), ("Vegasin naisten vessa", VegasWcX0Women, 0.644f, "Mene naisten vessaan") })
            {
                var root = new GameObject(areaName);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
                Undo.RegisterCreatedObjectUndo(root, areaName);

                var area = ReuseArea("Alue: " + areaName);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(area.gameObject, scene);
                area.areaName = areaName; area.useSidewalk = false;
                area.maxDepthY = top - 0.60f * H; area.minDepthY = top - 0.985f * H;   // seinän alareuna ~61 % korkeudesta
                area.walkMinX = X0 + imgW * 0.04f; area.walkMaxX = X0 + totalW - imgW * 0.04f;
                area.camMinX = X0 + W * 0.5f; area.camMaxX = Mathf.Max(area.camMinX, X0 + totalW - W * 0.5f);
                area.camSize = CamHalf; area.camRiseY = 0f; area.camOffsetY = 0f; area.backScale = 1f; area.depthLimits = new Vector2[0];

                for (int i = 0; i < files.Length; i++)
                {
                    var sp = ImportShipSprite(T + files[i] + ".png", ppuImg, false, SpriteAlignment.Center);
                    if (sp == null) { Info("Kuvan tuonti epäonnistui: " + files[i]); return; }
                    var sr = new GameObject("Vessa " + (i + 1)).AddComponent<SpriteRenderer>();
                    sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10000;
                    sr.transform.position = new Vector3(X0 + imgW * (i + 0.5f), cy, 0f);
                }

                // ovi baarin päädyssä <-> vessan vasen ovi (kuvan 1 x ~15 %)
                float inX = X0 + imgW * 0.150f, barX = barBase + imgW * barDoorFrac;
                float inY = Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.12f), barY = Mathf.Lerp(bar.maxDepthY, bar.minDepthY, 0.12f);
                MakeDoor("Vegas: " + areaName + " ovi sisään", prompt, bar, area, barX, new Vector2(inX + 1.2f, inY));
                MakeDoor("Vegas: " + areaName + " ovi ulos", "Mene takaisin baariin", area, bar, inX, new Vector2(barX, barY));
                EditorUtility.SetDirty(area);
                if (!names.Contains(areaName)) names.Add(areaName);
            }
            File.WriteAllLines(areasPath, names);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Vegasin vessat rakennettu (miehet x " + VegasWcX0Men.ToString("0") + ", naiset x " + VegasWcX0Women.ToString("0") + "): pesuhuone ja kopit, ovet baarin päädyn miesten/naisten oviin. Bäkkärin ovi odottaa vielä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
