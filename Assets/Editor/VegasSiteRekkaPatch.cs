using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Valikko 413: työmaalla oli kaksi Rekkamiestä ("1. laatta" ja "katto"). Rekkamies on miniboss ja esiintyy aina yksin, joten "1. laatta" poistetaan (katon Rekkamies jää).
/// Sama muutos on työmaan rakennusvalikossa (392). Poistaa vain olemassa olevan olion.</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/413. Las Vegas: työmaan toinen Rekkamies pois (vain 1)", false, 413)]
    static void RemoveSecondRekka()
    {
        if (PlayModeBlocked()) return;
        if (!File.Exists(VegasScenePath)) { Info("Vegas-scene puuttuu."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var del = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.gameObject.scene == scene && t.name == "Rekkamies (1. laatta)").ToArray();
            foreach (var t in del) Undo.DestroyObjectImmediate(t.gameObject);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Työmaan \"Rekkamies (1. laatta)\" poistettu (" + del.Length + " kpl). Jäljelle jää katon Rekkamies.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
