using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Valikko 402: kaikki Vegasin Tankki-Tonyt (katu, baari, Donin saattue, kanttiini) suoraan scenessä:
/// ilmalennossa vain yksi kuva (kaatumiskuva 4, ei kiertoa) ja elämät -15 % (520 -> 442). Ei tarvitse ajaa Tonyn/väen valikoita uudelleen.
/// </summary>
public static partial class BeatEmUpSetup
{
    const int TonyHealth = 442;

    [MenuItem("Beat em up/402. Las Vegas: Tankki-Tony (ilmalento yhdellä kuvalla, elämät -15 %)", false, 402)]
    static void PatchVegasTonys()
    {
        if (PlayModeBlocked()) return;
        if (!File.Exists(VegasScenePath)) { Info("Vegas-scene puuttuu."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            int n = 0;
            foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x != null && x.gameObject.scene == scene && x.displayName == "Tankki-Tony"))
            {
                Undo.RecordObject(e, "Tony");
                e.thrownLoopFrames = new[] { 4 }; e.airLoopFrames = new[] { 4 };
                e.maxHealth = TonyHealth;
                EditorUtility.SetDirty(e); n++;
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Tankki-Tony päivitetty (" + n + " kpl): ilmalento yhdellä kuvalla, elämät " + TonyHealth + ".");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
