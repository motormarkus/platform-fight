using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Linq;

/// <summary>Vegasin taustakuvien saumat: levennetään kuvia 0,3 %, jotta suurten x-arvojen float-tarkkuus ei jätä rakoa. Ei rakenna mitään uudelleen.</summary>
public static class VegasSeamFix
{
    [MenuItem("Beat em up/388. Las Vegas: korjaa taustakuvien saumat", false, 388)]
    static void Fix()
    {
        const string path = "Assets/Scenes/Areas/Vegas.unity";
        var scene = EditorSceneManager.GetSceneByPath(path);
        bool opened = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive); opened = true; }
        int n = 0;
        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (sr.gameObject.scene != scene) continue;
            if (!(sr.name.StartsWith("Katu: panttilainaamo") || sr.name.StartsWith("Katu: baari") || sr.name.StartsWith("Katu: kasino") || sr.name.StartsWith("Baari sisä ") || sr.name.StartsWith("Katto ") || sr.name.StartsWith("Kasino etuala"))) continue;
            sr.transform.localScale = new Vector3(1.003f, 1f, 1f); EditorUtility.SetDirty(sr.transform); n++;
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        if (opened) EditorSceneManager.CloseScene(scene, true);
        EditorUtility.DisplayDialog("Vegas", n + " taustakuvaa levennetty 0,3 %.", "OK");
    }
}
