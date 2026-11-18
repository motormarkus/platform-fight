using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Valikko 420: peli alkaa linnakkeen katolta aluksen luukun edestä (testaus). Vaatii valikon 417 (ja 418 luukulle).</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/420. Aloita peli linnakkeen katolta (aluksen luukun edestä, testaus)", false, 420)]
    static void StartAtFortressRoof() { WithLevelScenes(StartAtFortressRoofImpl); }

    static void StartAtFortressRoofImpl()
    {
        if (PlayModeBlocked()) return;
        var area = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == FortRoofAreaName);
        string sceneName = null; UnityEngine.SceneManagement.Scene opened = default;
        if (area == null)
        {
            sceneName = AreaSceneName(FortRoofAreaName);
            if (sceneName != null)
            {
                opened = EditorSceneManager.OpenScene("Assets/Scenes/Areas/" + sceneName + ".unity", OpenSceneMode.Additive);
                area = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == FortRoofAreaName);
            }
        }
        if (area == null) { Info("Linnakkeen kattoa ei ole: aja valikko 417."); return; }
        var old = GameObject.Find("Aloituskohta");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var go = new GameObject("Aloituskohta");
        var start = go.AddComponent<GameStart>(); start.grantAllPerks = true;
        float ppu = 1844f / (2f * CamHalf);
        float roofW = 5f * (5120f / ppu), shipWu = (5120f * 1.3f) / ppu, shipLeft = FortRoofX0 + roofW - 200f / ppu - shipWu, hatchX = shipLeft + 820f * 1.3f / ppu;
        start.position = new Vector2(hatchX - 15f, Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.4f));
        start.areaName = FortRoofAreaName; start.sceneName = sceneName ?? System.IO.Path.GetFileNameWithoutExtension(area.gameObject.scene.path);
        if (opened.IsValid()) EditorSceneManager.CloseScene(opened, true);
        Undo.RegisterCreatedObjectUndo(go, "Aloituskohta");
        EditorSceneManager.MarkSceneDirty(go.scene); if (batch) EditorSceneManager.SaveScene(go.scene);
        Info("Peli alkaa nyt linnakkeen katolta aluksen luukun edestä (x ≈ " + start.position.x.ToString("0.0") + ", luukku x ≈ " + hatchX.ToString("0.0") + "). Hissi on kaukana vasemmalla.");
    }
}

public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/421. Aloita peli Vegasin rakennustyömaalta (testaus)", false, 421)]
    static void StartAtVegasSite() { WithLevelScenes(StartAtVegasSiteImpl); }

    static void StartAtVegasSiteImpl()
    {
        if (PlayModeBlocked()) return;
        const string areaName = "Vegasin katu";
        var area = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == areaName);
        string sceneName = null; UnityEngine.SceneManagement.Scene opened = default;
        if (area == null)
        {
            sceneName = AreaSceneName(areaName);
            if (sceneName != null)
            {
                opened = EditorSceneManager.OpenScene("Assets/Scenes/Areas/" + sceneName + ".unity", OpenSceneMode.Additive);
                area = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == areaName);
            }
        }
        if (area == null) { Info("Aluetta \"Vegasin katu\" ei ole: aja valikko 378."); return; }
        var old = GameObject.Find("Aloituskohta");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var go = new GameObject("Aloituskohta");
        var start = go.AddComponent<GameStart>(); start.grantAllPerks = true;
        float imgW = 5120f / (1844f / (2f * CamHalf));
        float sx = VegasStreetX0 + 2f * imgW + imgW * VegasChapelOverlapFrac + imgW + 6f;   // työmaan alku (kappelin oikealla puolella)
        start.position = new Vector2(sx, Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.4f));
        start.areaName = areaName; start.sceneName = sceneName ?? System.IO.Path.GetFileNameWithoutExtension(area.gameObject.scene.path);
        if (opened.IsValid()) EditorSceneManager.CloseScene(opened, true);
        Undo.RegisterCreatedObjectUndo(go, "Aloituskohta");
        EditorSceneManager.MarkSceneDirty(go.scene); if (batch) EditorSceneManager.SaveScene(go.scene);
        Info("Peli alkaa nyt Vegasin rakennustyömaan alusta (x ≈ " + sx.ToString("0.0") + "). Aja 392 ensin, jos työmaata ei ole rakennettu.");
    }
}
