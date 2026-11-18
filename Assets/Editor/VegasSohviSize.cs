using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Valikko 390: Sohvi (kasinon baari) kokoon 1,15 suoraan Vegas-scenessä (ei koko erän ajoa).</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/390. Las Vegas: Sohvi kokoon 1,35, tiski vyötäröllä (kasinon baari)", false, 390)]
    static void VegasSohviSize()
    {
        if (PlayModeBlocked()) return;
        if (!File.Exists(VegasScenePath)) { Info("Vegas-scene puuttuu."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.gameObject.scene == scene && t.name == "Sohvi (kasinon baari)").ToArray();
            
            float hyAbs = (CamY + CamHalf) - 0.62f * (2f * CamHalf);   // jalat tiskin etuseinän taakse: ei seiso tiskin päällä
            foreach (var t in all) { Undo.RecordObject(t, "Sohvi koko"); t.localScale = Vector3.one * 1.35f; var pp = t.position; t.position = new Vector3(pp.x, hyAbs, pp.z); EditorUtility.SetDirty(t); }
            // baarin (Vegasin baari) Sohvi: korkeus 0,15 ruudusta (oli 0,185, liian iso), absoluuttinen koko
            int barCount = 0;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.gameObject.scene == scene && t.name == "Vegasin baarin Sohvi"))
            {
                var bsr = t.GetComponentInChildren<SpriteRenderer>();
                if (bsr == null || bsr.sprite == null) continue;
                Undo.RecordObject(t, "Baarin Sohvi koko"); t.localScale = Vector3.one * (0.15f * 2f * CamHalf / bsr.sprite.bounds.size.y); EditorUtility.SetDirty(t); barCount++;
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Sohvi (kasinon baari) kokoon 1,35, tiski vyötäröllä (" + all.Length + " kpl); baarin Sohvi pienempi (" + barCount + " kpl).");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
