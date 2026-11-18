using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Valikko 405: Vegasin kadulle jalkakäytävän pykälä (Area.useSidewalk), kuten muissa kentissä: jalkakäytävälle noustaan askeleella.
/// Katukiveyksen alareuna on kaikissa katukuvissa (Strip, kappeli, työmaa, konttori, tienpätkä, motelli) ~440 px kuvan alareunasta (1844 korkea),
/// kiveyksen korkeus ~28 px. Takaraja (seinän juuri) lasketaan pykälän verran, ettei jalkakäytävällä seisova ukko nouse seinään.
/// Työmaan/konttorin/motellin ylemmät kerrokset eivät nouse (SiteLevels.AboveStreet). Uudelleenajo antaa saman tuloksen.
/// Aja valikkojen 378, 392, 393 ja 403 jälkeen (ne palauttavat kadun rajat).
/// </summary>
public static partial class BeatEmUpSetup
{
    const float VegasCurbPx = 440f, VegasCurbHeightPx = 28f;

    [MenuItem("Beat em up/405. Las Vegas: jalkakäytävän pykälä kadulle", false, 405)]
    static void AddVegasCurb()
    {
        if (PlayModeBlocked()) return;
        if (!File.Exists(VegasScenePath)) { Info("Vegasin scenea ei löytynyt."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var street = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a != null && a.gameObject.scene == scene && a.areaName == "Vegasin katu");
            if (street == null) { Info("Alue \"Vegasin katu\" puuttuu: aja valikko 378."); return; }
            float H = 2f * CamHalf, cy = CamY, bottom = cy - CamHalf;
            foreach (var csr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (csr.gameObject.scene == scene && csr.gameObject.name == "Katu: hääkappeli") { bottom = csr.bounds.min.y; break; }
            float top = bottom + H, ppu = 1844f / H;
            float h = VegasCurbHeightPx / ppu;
            float baseBack = top - 0.675f * H;   // valikon 378 takaraja (seinän juuri) ilman pykälää

            street.useSidewalk = true;
            street.sidewalkHeight = h;
            street.curbDepthY = bottom + VegasCurbPx / ppu;
            street.maxDepthY = baseBack - h;
            int moved = 0;
            if (street.depthLimits != null)
            {
                var dl = street.depthLimits;
                for (int i = 0; i < dl.Length; i++)
                    if (Mathf.Abs(dl[i].y - baseBack) < 0.02f) { dl[i].y = baseBack - h; moved++; }
                street.depthLimits = dl;
            }
            EditorUtility.SetDirty(street);
            string sites = string.Join(", ", Object.FindObjectsByType<SiteLevels>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(s => s != null && s.gameObject.scene == scene && s.area == street)
                .Select(s => s.gameObject.name + " katu " + s.streetMin.ToString("0.00") + ".." + s.streetMax.ToString("0.00")));
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Vegasin kadulle jalkakäytävän pykälä: reuna y " + street.curbDepthY.ToString("0.00") + " (syvyys " + street.minDepthY.ToString("0.00") + ".." + street.maxDepthY.ToString("0.00")
                 + "), korkeus " + h.ToString("0.00") + "; takarajaa laskettu pykälän verran (" + moved + " rajapistettä). Kerrokset: " + sites + ".");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    /// <summary>Jalkakäytävän pykälä (405).</summary>
    public static void RunVegasCurbBatch()
    {
        batch = true;
        try { Step("405 jalkakäytävän pykälä", AddVegasCurb); }
        finally { batch = false; }
    }
}
