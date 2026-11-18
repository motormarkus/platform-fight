using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Valikko 426: peli alkaa aluksen hytistä Gustavin jälkeen (Gustav poistetaan, ohjaamon ovi vieressä; Lokki ohjaamossa). Testaus. Vaatii valikot 418 ja 419.</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/426. Aloita peli aluksen hytistä Gustavin jälkeen (testaus)", false, 426)]
    static void StartAtShipCabin() { WithLevelScenes(StartAtShipCabinImpl); }

    static void StartAtShipCabinImpl()
    {
        if (PlayModeBlocked()) return;
        const string an = "Aluksen hytti";
        var area = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == an);
        string sceneName = null; UnityEngine.SceneManagement.Scene opened = default;
        if (area == null)
        {
            sceneName = AreaSceneName(an);
            if (sceneName != null)
            {
                opened = EditorSceneManager.OpenScene("Assets/Scenes/Areas/" + sceneName + ".unity", OpenSceneMode.Additive);
                area = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == an);
            }
        }
        if (area == null) { Info("Aluksen hyttiä ei ole: aja valikko 418."); return; }
        var old = GameObject.Find("Aloituskohta");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var go = new GameObject("Aloituskohta");
        var start = go.AddComponent<GameStart>(); start.grantAllPerks = true; start.removeNamed = new[] { "Gustav (alus)" };
        start.position = new Vector2(area.walkMaxX - 8f, Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.45f));
        start.areaName = an; start.sceneName = sceneName ?? System.IO.Path.GetFileNameWithoutExtension(area.gameObject.scene.path);
        if (opened.IsValid()) EditorSceneManager.CloseScene(opened, true);
        Undo.RegisterCreatedObjectUndo(go, "Aloituskohta");
        EditorSceneManager.MarkSceneDirty(go.scene); if (batch) EditorSceneManager.SaveScene(go.scene);
        Info("Peli alkaa nyt aluksen hytistä ohjaamon oven vierestä (Gustav poistettu).");
    }
}
