using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Valikko 411: hääkappelin pariskunta lähemmäs vihkijää (Melvin): Tankki-Tony (sulhanen) x 1,575 ja morsian x 1,485 (kuvan leveyksinä), Tony katsoo morsianta (vasemmalle),
/// ja Jakuza (häävieras 2), joka seisoi heidän edessään, siirretään sivuun. Siirtää vain olemassa olevia olioita (ei rakenna kappelia uudelleen). Sama muutos on myös kappelin rakennusvalikossa.</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/411. Las Vegas: hääkappelin pariskunta ja taikuri lähemmäs Melviniä", false, 411)]
    static void MoveChapelCouple()
    {
        if (PlayModeBlocked()) return;
        if (!File.Exists(VegasScenePath)) { Info("Aja ensin valikko 376 (Vegas)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var chapel = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.gameObject.scene == scene && a.areaName == "Vegasin hääkappeli");
            if (chapel == null) { Info("Alue \"Vegasin hääkappeli\" puuttuu."); return; }
            float H = 2f * CamHalf, ppuImg = 1844f / H, imgW = 5120f / ppuImg, X0 = VegasChapelX0;
            Vector3 Spot(float xFrac, float depth)
            {
                float x = X0 + imgW * xFrac;
                float yMax = chapel.MaxDepthAtX(x, chapel.maxDepthY), yMin = chapel.MinDepthAtX(x, chapel.minDepthY);
                return new Vector3(x, Mathf.Clamp(Mathf.Lerp(yMax, yMin, depth), yMin + 0.05f, yMax - 0.05f), 0f);
            }
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.gameObject.scene == scene).ToArray();
            Transform F(string n) => all.FirstOrDefault(t => t.name == n);
            string info = "";
            var groom = F("Tankki-Tony (sulhanen)");
            if (groom != null) { groom.position = Spot(1.575f, 0.14f); var e = groom.GetComponent<Enemy>(); if (e != null) { e.calmFacing = -1; EditorUtility.SetDirty(e); } info += "Tony siirretty ja käännetty morsianta kohti. "; }
            var bride = F("Morsian"); if (bride != null) { bride.position = Spot(1.485f, 0.14f); info += "Morsian siirretty. "; }
            var jak = F("Jakuza (häävieras 2)"); if (jak != null) { jak.position = Spot(1.08f, 0.30f); info += "Jakuza sivuun. "; }
            var mg = F("Taikuri (Vegasin hääkappeli)"); if (mg != null) { mg.position = Spot(1.66f, 0.62f); info += "Taikuri lähemmäs Melviniä, alemmas. "; }
            // morsian katsoo oikealle (Tonya kohti): ChapelBride käyttää flipX=false
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Hääkappeli: " + (info.Length > 0 ? info : "yhtään kohdetta ei löytynyt (aja kappelin valikko)."));
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
