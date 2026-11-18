using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Valikko 412: kanttiinin Sohvi hanan kohdalle (kuvan x ~4007 px) ja jalat 985 px:n kohtaan, jotta hän ei näytä seisovan tiskin sisässä eivätkä kengät näy tiskin alta. Idempotentti: asettaa absoluuttiset paikat. Siirtää vain olemassa olevaa Sohvia (ei rakenna kanttiinia uudelleen).</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/412. Las Vegas: kanttiinin Sohvi hanan kohdalle ja kengät piiloon", false, 412)]
    static void FixCanteenSohvi()
    {
        if (PlayModeBlocked()) return;
        if (!File.Exists(VegasScenePath)) { Info("Vegas-scene puuttuu."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            float ppu = 1844f / (2f * CamHalf), cy = CamY, X0 = VegasCanteenX0;
            float hx = X0 + 4007f / ppu, hy = cy + (922f - 985f) / ppu;   // hanan kohta (kuvan x ~4007 px), jalat tiskin etuosan takana (985 px)
            int n = 0;
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.gameObject.scene == scene).ToArray();
            foreach (var t in all.Where(t => t.name == "Sohvi (kanttiini)")) { Undo.RecordObject(t, "Sohvi"); t.position = new Vector3(hx, hy, t.position.z); EditorUtility.SetDirty(t); n++; }
            foreach (var t in all.Where(t => t.name == "Kanttiinin tiski (Sohvi)")) { Undo.RecordObject(t, "Sohvin tiski"); t.position = new Vector3(hx, t.position.y, t.position.z); EditorUtility.SetDirty(t); }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Kanttiinin Sohvi siirretty hanan kohdalle ja kengät tiskin taakse (" + n + " kpl).");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
