using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Valikko 429: poistaa ohuet pystyviivat vierekkäisten taustakuvien välistä (Linnake: loppuranta, rantabaari, alus, katto).
/// Suurilla x:illä (n. 80 000) floatin tarkkuus on alle kuvapikselin, joten tarkasti vierekkäin asetetuissa kuvissa voi näkyä rako.
/// Jokainen kuva, jonka oikeaan reunaan toinen samanlainen kuva liittyy, levennetään 0,03 yksikköä (n. 2–5 kuvapikseliä), jolloin kuvat limittyvät.
/// Ajo uudelleen turvallinen (käsitellyt merkitään nimen loppuun " ·").
/// </summary>
public static partial class BeatEmUpSetup
{
    const string SeamMark = " ·";

    [MenuItem("Beat em up/429. Taustojen saumat pois (loppuranta, rantabaari, alus)", false, 429)]
    static void FixBackgroundSeams()
    {
        if (PlayModeBlocked()) return;
        string scenePath = "Assets/Scenes/Areas/" + (AreaSceneName("Muurin juhlahalli") ?? "Linnake") + ".unity";
        var sc = EditorSceneManager.GetSceneByPath(scenePath); bool opened = false;
        if (!sc.IsValid() || !sc.isLoaded) { sc = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive); opened = true; }
        try
        {
            var all = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(r => r != null && r.gameObject.scene == sc && r.sprite != null && r.GetComponent<ParallaxRight>() == null && r.GetComponent<ParallaxLayer>() == null)
                .ToArray();
            int n = 0;
            foreach (var r in all)
            {
                if (r.name.EndsWith(SeamMark)) continue;
                var b = r.bounds;
                if (b.size.x < 3f) continue;   // vain isot taustapalat
                bool joined = all.Any(o => o != r && o.sortingOrder == r.sortingOrder && Mathf.Abs(o.bounds.min.x - b.max.x) < 0.06f
                                         && Mathf.Abs(o.bounds.center.y - b.center.y) < 0.5f && Mathf.Abs(o.bounds.size.y - b.size.y) < 0.5f);
                if (!joined) continue;
                Undo.RecordObject(r.transform, "Sauma");
                var s = r.transform.localScale; float k = (b.size.x + 0.03f) / b.size.x;
                s.x *= k; r.transform.localScale = s;
                r.transform.position += new Vector3(0.015f, 0f, 0f);   // levennys oikealle: vasen reuna pysyy paikallaan
                Undo.RecordObject(r.gameObject, "Sauma"); r.gameObject.name += SeamMark; n++;
            }
            EditorSceneManager.MarkSceneDirty(sc); EditorSceneManager.SaveScene(sc);
            Info("Taustojen saumat: " + n + " kuvaa limitetty (Linnake-scene).");
        }
        finally { if (opened && sc.IsValid()) EditorSceneManager.CloseScene(sc, true); }
    }
}
