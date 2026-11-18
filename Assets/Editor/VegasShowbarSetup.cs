using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Vegasin kasinon showbaari (valikko 406): kolme kuvaa vegas_showbaari_1 (vasen ovi vessoihin), _2 (lava), _3 (tiski + oikea ovi aulaan).
/// Vasen ovi vie showbaarin vessaan (samat kuvat kuin baarin vessoissa, peilikuvina: ovi oikealla). Oikea ovi aulaan odottaa aulan kuvia. Aja valikko 376 ensin.</summary>
public static partial class BeatEmUpSetup
{
    const float VegasShowbarX0 = 120000f, VegasShowWcX0 = 121000f;

    [MenuItem("Beat em up/406. Las Vegas: kasinon showbaari (lava, tiski, vessa peilikuvana)", false, 406)]
    static void AddVegasShowbar()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        var files = new[] { "vegas_showbaari_1", "vegas_showbaari_2", "vegas_showbaari_3" };
        var wcFiles = new[] { "vegas_vessa_peili_1", "vegas_vessa_peili_2" };
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
            foreach (var n in new[] { "Vegasin showbaari", "Vegasin showbaarin vessa", "Vegasin showbaari ovet" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);

            float H = 2f * CamHalf, cy = CamY, top = cy + CamHalf;
            float W = 1672f / (941f / H);
            float ppuImg = 1844f / H, imgW = 5120f / ppuImg;
            var areasPath = VegasScenePath.Replace(".unity", ".areas.txt");
            var names = File.Exists(areasPath) ? File.ReadAllLines(areasPath).ToList() : new System.Collections.Generic.List<string>();

            Area Build(string areaName, string[] imgs, float X0, float wallFrac)
            {
                var root = new GameObject(areaName);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
                Undo.RegisterCreatedObjectUndo(root, areaName);
                float totalW = imgs.Length * imgW;
                var area = ReuseArea("Alue: " + areaName);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(area.gameObject, scene);
                area.areaName = areaName; area.useSidewalk = false;
                area.maxDepthY = top - wallFrac * H; area.minDepthY = top - 0.985f * H;
                area.walkMinX = X0 + imgW * 0.04f; area.walkMaxX = X0 + totalW - imgW * 0.04f;
                area.camMinX = X0 + W * 0.5f; area.camMaxX = Mathf.Max(area.camMinX, X0 + totalW - W * 0.5f);
                area.camSize = CamHalf; area.camRiseY = 0f; area.camOffsetY = 0f; area.backScale = 1f; area.depthLimits = new Vector2[0];
                for (int i = 0; i < imgs.Length; i++)
                {
                    var sp = ImportShipSprite(T + imgs[i] + ".png", ppuImg, false, SpriteAlignment.Center);
                    if (sp == null) { Info("Kuvan tuonti epäonnistui: " + imgs[i]); return null; }
                    var sr = new GameObject("Kuva " + (i + 1)).AddComponent<SpriteRenderer>();
                    sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10000;
                    sr.transform.position = new Vector3(X0 + imgW * (i + 0.5f), cy, 0f);
                    sr.transform.localScale = new Vector3(1.003f, 1f, 1f);
                }
                EditorUtility.SetDirty(area);
                if (!names.Contains(areaName)) names.Add(areaName);
                return area;
            }

            var show = Build("Vegasin showbaari", files, VegasShowbarX0, 0.575f);
            var wc = Build("Vegasin showbaarin vessa", wcFiles, VegasShowWcX0, 0.60f);
            if (show == null || wc == null) return;

            var doorRoot = new GameObject("Vegasin showbaari ovet");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, scene);
            Undo.RegisterCreatedObjectUndo(doorRoot, "Vegasin showbaari ovet");
            Door MakeDoor(string name, string prompt, Area here, Area target, float x, Vector2 spawn)
            {
                var d = new GameObject(name).AddComponent<Door>();
                d.transform.SetParent(doorRoot.transform, false); d.transform.position = new Vector3(x, here.maxDepthY - 0.3f, 0f);
                d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = spawn;
                d.halfWidth = 1.6f; d.maxDistanceFromWall = 2.4f; d.onGround = true; d.groundHalfDepth = 1.6f;
                return d;
            }
            // vasen ovi (kuvan 1 x ~10,7 %) <-> vessan oikea ovi (peilattu: 2 kuvaa, ovi x ~ 2 - 0,150)
            float showX = VegasShowbarX0 + imgW * 0.107f, wcX = VegasShowWcX0 + imgW * 1.850f;
            float showY = Mathf.Lerp(show.maxDepthY, show.minDepthY, 0.12f), wcY = Mathf.Lerp(wc.maxDepthY, wc.minDepthY, 0.12f);
            MakeDoor("Vegas: showbaarin vessan ovi sisään", "Mene vessaan", show, wc, showX, new Vector2(wcX - 1.2f, wcY));
            MakeDoor("Vegas: showbaarin vessan ovi ulos", "Mene takaisin showbaariin", wc, show, wcX, new Vector2(showX + 1.2f, showY));
            // oikea ovi (kuvan 3 x ~91 %) aulaan: odottaa aulan kuvia

            File.WriteAllLines(areasPath, names);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Showbaari rakennettu (x " + VegasShowbarX0.ToString("0") + ") ja vessa peilikuvana (x " + VegasShowWcX0.ToString("0") + "). Vasen ovi -> vessa. Oikea ovi (aula) odottaa aulan kuvia.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
