using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Valikko 400: Vegasin taustamusiikki (Assets/Audio/Musiikki/Vegas.mp3) kaikille Vegas-scenen alueille. Hääkappelin oma musiikki (Vegaswedding) jää ennalleen.
/// Taso tasataan ajonaikaisesti MusicPlayer.MusicRms-taulukon mukaan (Vegas -18,9 dBFS).
/// </summary>
public static partial class BeatEmUpSetup
{
    const string VegasMusicPath = "Assets/Audio/Musiikki/Vegas.mp3";

    [MenuItem("Beat em up/400. Vegas: taustamusiikki (kappeli pitää omansa)", false, 400)]
    static void AddVegasMusic()
    {
        if (PlayModeBlocked()) return;
        const string scenePath = "Assets/Scenes/Areas/Vegas.unity";
        if (!File.Exists(scenePath)) { Info("Vegasin scenea ei löytynyt."); return; }
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(VegasMusicPath);
        if (clip == null) { Info("Musiikkia ei löytynyt: " + VegasMusicPath); return; }
        var scene = EditorSceneManager.GetSceneByPath(scenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            int n = 0; var skipped = new System.Collections.Generic.List<string>();
            foreach (var a in Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == scene))
            {
                if (a.music != null && a.music != clip) { skipped.Add(a.areaName + " (" + a.music.name + ")"); continue; }   // kappeli (Vegaswedding) ym. oma musiikki
                Undo.RecordObject(a, "Vegasin musiikki");
                a.music = clip; a.musicGain = 1f; EditorUtility.SetDirty(a); n++;
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Vegasin musiikki " + n + " alueelle. Oma musiikki pidetty: " + (skipped.Count > 0 ? string.Join(", ", skipped) : "-"));
        }
        finally
        {
            if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
        }
    }

    public static void RunVegasMusicBatch()
    {
        batch = true;
        try { Step("400 Vegasin musiikki", AddVegasMusic); }
        finally { batch = false; }
    }
}
