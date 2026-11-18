using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static partial class BeatEmUpSetup
{
    /// Vaihtaa kaikkien bandi.png-pohjaisten mariachi-bändien (S-Club "Bändi", UccoLaiva "Loipparin bändi") kuvat uuteen ping-pong-animaatioon (bandi_ranta.png).
    public static void RunBandSwapBatch()
    {
        string nbp = FindTexture("bandi_ranta"); SetupAndSlice(nbp);
        var ns = LoadSprites("bandi_ranta").OrderBy(q => int.TryParse(q.name.Substring(q.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        var pp = new System.Collections.Generic.List<Sprite>(ns);
        for (int k = ns.Length - 2; k >= 1; k--) pp.Add(ns[k]);
        foreach (var path in new[] { "Assets/Scenes/SampleScene.unity", "Assets/Scenes/Areas/UccoLaiva.unity" })
        {
            var sc = EditorSceneManager.OpenScene(path, OpenSceneMode.Single); int n = 0;
            foreach (var d in Object.FindObjectsByType<Dancer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (d.sprites == null || d.sprites.Length == 0 || d.sprites[0] == null || !AssetDatabase.GetAssetPath(d.sprites[0]).EndsWith("/bandi.png")) continue;
                d.sprites = pp.ToArray(); d.keepOrder = true; d.frameTime = 0.112f; d.startFrame = 0; d.flipX = false;
                var sr = d.GetComponent<SpriteRenderer>(); if (sr != null) sr.sprite = ns[0];
                EditorUtility.SetDirty(d); n++;
                Debug.Log("[BandSwap] " + path + ": " + d.name);
            }
            EditorSceneManager.MarkAllScenesDirty(); EditorSceneManager.SaveScene(sc);
            Debug.Log("[BandSwap] " + path + " vaihdettu " + n);
        }
    }
}
