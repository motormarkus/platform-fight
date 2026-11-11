using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Vankilan mittakaavakorjaukset olemassa olevaan sceneen (ilman koko vankilan uudelleenrakennusta): vartijat 15 % isommiksi, suojalaatikot pienemmiksi (3,4 -> 2,5 korkeus).
/// Uudet ajot (353–363) käyttävät samoja arvoja suoraan. Valikko on turvallista ajaa uudelleen (ei kerro kahdesti).</summary>
public static partial class BeatEmUpSetup
{
    const float GuardScale = 1.15f;
    /// Vankilassa ei ole suojalaatikoita (ei stealth-tilaa): false = valikot eivät luo niitä ja valikko 367 poistaa olemassa olevat.
    const bool PrisonCrates = false;
    const float RexScale = 1.15f;
    /// Sellien kameran lähennys: hahmot näyttävät isommilta sellissä (kuva-alan alareuna pysyy samana, yläosa rajautuu).
    const float CellZoom = 1.3f;

    [MenuItem("Beat em up/367. Vankila: vartijat 15 % isommiksi, poista laatikot, Rex 15 % isommaksi, sellien kamera lähemmäs, taivas ikkunoihin (nykyiseen sceneen)", false, 367)]
    static void FixPrisonScales()
    {
        if (PlayModeBlocked()) return;
        if (!File.Exists(PrisonScenePath)) { Info("Aja ensin valikko 353."); return; }
        var scene = EditorSceneManager.GetSceneByPath(PrisonScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(PrisonScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            int guards = 0, crates = 0;
            foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (e != null && e.gameObject.scene == scene && e.displayName == "Vartija" && e.visualScale < GuardScale - 0.01f)
                {
                    Undo.RecordObject(e, "Vartija isommaksi"); e.visualScale = GuardScale; EditorUtility.SetDirty(e); guards++;
                }
            foreach (var c in Object.FindObjectsByType<CoverCrate>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (c != null && c.gameObject.scene == scene && !PrisonCrates) { Undo.DestroyObjectImmediate(c.gameObject); crates++; }   // poistetaan suojalaatikot
            int rexes = 0;
            foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (e != null && e.displayName == "Rex" && e.visualScale < RexScale - 0.01f) { Undo.RecordObject(e, "Rex isommaksi"); e.visualScale = RexScale; EditorUtility.SetDirty(e); rexes++; }
            int cells = 0;
            foreach (var ar in Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (ar != null && ar.gameObject.scene == scene && ar.areaName != null && ar.areaName.StartsWith("Vankilan selli") && ar.camSize > CamHalf / CellZoom + 0.01f)
                {
                    float X0 = ar.walkMinX - 1.5f, imgW = ar.walkMaxX - ar.walkMinX + 3f, W = 1672f / (941f / (2f * CamHalf)) / CellZoom;
                    Undo.RecordObject(ar, "Sellin kamera");
                    ar.camSize = CamHalf / CellZoom; ar.camOffsetY = CamHalf / CellZoom - CamHalf; ar.camMinX = X0 + W * 0.5f; ar.camMaxX = Mathf.Max(ar.camMinX, X0 + imgW - W * 0.5f);
                    EditorUtility.SetDirty(ar); cells++;
                }
            // taivas (parallaksi) ikkunoiden taakse: ikkunalasit ovat kuvissa läpinäkyviä, mutta takana ei ollut mitään (pihalla on oma taivas)
            int skies = 0;
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name.StartsWith("Vankilan taivas: ")).ToArray()) Undo.DestroyObjectImmediate(old);
            var skySprite = ImportShipSprite("Assets/Sprites/Taustat/kone_taivas.png", 922f / (2f * CamHalf), false, SpriteAlignment.Center);
            if (skySprite != null)
                foreach (var ar in Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (ar == null || ar.gameObject.scene != scene || ar.areaName == null || ar.areaName == "Vankila" || ar.areaName.Contains("D-osasto") || ar.areaName.Contains("E-osasto")) continue;   // pihalla on oma taivas, käytävillä ei ole ikkunoita
                    var go = new GameObject("Vankilan taivas: " + ar.areaName);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                    Undo.RegisterCreatedObjectUndo(go, "Taivas");
                    var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = skySprite; sr.sortingOrder = -10100;
                    float W = 1672f / (941f / (2f * CamHalf)), left = ar.camMinX - W * 0.5f;
                    go.transform.localScale = new Vector3(1.25f, 1.25f, 1f);
                    go.transform.position = new Vector3(left + skySprite.bounds.size.x * 1.25f * 0.5f, CamY, 0f);
                    var pl = go.AddComponent<ParallaxLayer>(); pl.factor = 0.02f; pl.startCamX = ar.camMinX; pl.minX = ar.camMinX - 2f; pl.maxX = ar.camMaxX + 2f;
                    skies++;
                }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info(skies + " alueelle taivas ikkunoiden taakse (parallaksi). " + cells + " sellin kamera lähemmäs (hahmot isommiksi, zoom " + CellZoom + "). Vartijoita suurennettu " + guards + " (kerroin " + GuardScale + "), suojalaatikoita poistettu " + crates + ", Rexejä isonnettu " + rexes + " (kerroin " + RexScale + ").\nVartijoita voi kutsua pillillä lisää: ne tulevat vartijan kopioina samalla koolla.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
