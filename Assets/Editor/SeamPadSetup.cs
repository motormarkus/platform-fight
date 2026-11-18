using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Valikko 401: Vegasin taustakuvien saumat umpeen. Vierekkäiset isot taustakuvat (katu, työmaa, konttori, kanttiini, toimisto, sisätilaparallaksit)
/// levennetään SeamPadExtend yksikköä kummastakin reunasta, jolloin ne limittyvät eikä väliin jää 1 px rakoa (float-tarkkuus suurilla x:illä).
/// Ajetaan rakennusvalikoiden (376–399) jälkeen; uudelleenajo antaa saman tuloksen (SeamPad muistaa alkuperäisen skaalan).
/// </summary>
public static partial class BeatEmUpSetup
{
    const float SeamPadExtend = 0.025f;   // ~4 px (ppu 154) kummallekin puolelle; float-virhe x ≈ 112 000: 0,008

    [MenuItem("Beat em up/401. Vegas: taustakuvien saumat umpeen (katu, työmaa, konttori, kanttiini)", false, 401)]
    static void FixVegasSeams()
    {
        if (PlayModeBlocked()) return;
        if (!File.Exists(VegasScenePath)) { Info("Vegasin scenea ei löytynyt."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var srs = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(r => r != null && r.gameObject.scene == scene && r.sprite != null && r.sprite.rect.width >= 1500f && r.sprite.rect.height >= 500f
                            && r.GetComponentInParent<Enemy>() == null && r.GetComponentInParent<PlayerController>() == null)
                .ToList();
            // alkuperäinen skaala takaisin ennen mittausta
            foreach (var r in srs)
            {
                var pad = r.GetComponent<SeamPad>();
                if (pad != null) { var s = r.transform.localScale; s.x = pad.baseScaleX; r.transform.localScale = s; }
            }
            var b = srs.Select(r => r.bounds).ToList();
            var touch = new bool[srs.Count];
            for (int i = 0; i < srs.Count; i++)
                for (int j = 0; j < srs.Count; j++)
                {
                    if (i == j) continue;
                    if (Mathf.Abs(b[i].max.x - b[j].min.x) > 0.06f) continue;   // i:n oikea reuna = j:n vasen reuna
                    float ov = Mathf.Min(b[i].max.y, b[j].max.y) - Mathf.Max(b[i].min.y, b[j].min.y);
                    if (ov < 0.5f * Mathf.Min(b[i].size.y, b[j].size.y)) continue;
                    touch[i] = touch[j] = true;
                }
            int n = 0;
            for (int i = 0; i < srs.Count; i++)
            {
                var r = srs[i]; var pad = r.GetComponent<SeamPad>();
                if (!touch[i]) { if (pad != null) Object.DestroyImmediate(pad); continue; }
                if (pad == null) pad = r.gameObject.AddComponent<SeamPad>();
                var s = r.transform.localScale; pad.baseScaleX = s.x;
                float w = b[i].size.x;
                s.x = pad.baseScaleX * (w + 2f * SeamPadExtend) / w;
                r.transform.localScale = s;
                EditorUtility.SetDirty(r.transform); EditorUtility.SetDirty(pad); n++;
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Saumat umpeen: " + n + " taustakuvaa levennetty " + SeamPadExtend + " yksikköä kummastakin reunasta (" + srs.Count + " isoa kuvaa tutkittu).");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    /// <summary>Viimeisimmät korjaukset: 390, 393, 394, 395, 396, 400, 401, 402.</summary>
    public static void RunVegasFixBatch()
    {
        batch = true;
        try
        {
            Step("390 Sohvin koko", VegasSohviSize);
            Step("393 konttori", AddVegasOffice);
            Step("394 kanttiini ja toimisto", AddVegasCanteen);
            Step("395 Nokkamies", AddNokkamies);
            Step("396 kanttiinin väki", AddVegasCanteenCrowd);
            Step("400 Vegasin musiikki", AddVegasMusic);
            Step("401 saumat", FixVegasSeams);
            Step("402 Tankki-Tony", PatchVegasTonys);
        }
        finally { batch = false; }
    }
}
