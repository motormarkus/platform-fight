using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Valikko 408: kasinon ruletti-pöytä pelattavaksi. Kasinon etualan kuvassa vegas_kasino_etu_vasen ruletti-pöytä on x ~70,6 % kuvasta (kasinon vasemmalta kolmas kuva).
/// Lisää pöydän kohdalle RouletteTableEntry (E avaa ruletin). Aja valikko 376 ensin.</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/408. Las Vegas: kasinon ruletti (pelattava pöytä)", false, 408)]
    static void AddVegasRoulette()
    {
        if (PlayModeBlocked()) return;
        if (!File.Exists(VegasScenePath)) { Info("Aja ensin valikko 376 (Vegas)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var casino = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.gameObject.scene == scene && a.areaName == "Vegasin kasino");
            if (casino == null) { Info("Alue \"Vegasin kasino\" puuttuu: aja valikko 376."); return; }
            foreach (var o in scene.GetRootGameObjects().Where(g => g.name == "Ruletti pöytä").ToArray()) Undo.DestroyObjectImmediate(o);
            float H = 2f * CamHalf, ppuImg = 1844f / H, imgW = 5120f / ppuImg;
            float x = VegasCasinoX0 + 3f * imgW + 0.706f * imgW;                 // kasinon vasemmalta: ovet, avoin lattia, baari, vegas_kasino_etu_vasen (blackjack + ruletti)
            float y = Mathf.Lerp(casino.maxDepthY, casino.minDepthY, 0.40f);
            var go = new GameObject("Ruletti pöytä");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, "Ruletti pöytä");
            go.transform.position = new Vector3(x, y, 0f);
            var entry = go.AddComponent<RouletteTableEntry>(); entry.here = casino; entry.rangeX = 3.4f; entry.rangeY = 3f;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Ruletti-pöytä lisätty kasinoon (x " + x.ToString("0.0") + "): E / toimintanappi pöydän luona avaa ruletin.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
