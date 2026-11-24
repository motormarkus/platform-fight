using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Auroran uudet kuvasarjat (Tools/AuroraAssets/make_aurora.py): istuva idle, nousu, kävely, kääntyminen tiskillä ja seisova idle (ruudut 512 x 448).
/// Valikko 428 viipaloi sheetit ja päivittää laivan kannen Aurora-turistin (UccoLaiva-scene) ilman koko kannen uudelleenrakennusta.
/// </summary>
public static partial class BeatEmUpSetup
{
    static Sprite[] AuroraSheet(string n)
    {
        string p = FindTexture(n);
        if (p == null) return null;
        SetupAndSlice(p);
        var t2 = (TextureImporter)AssetImporter.GetAtPath(p);
        t2.mipmapEnabled = true; t2.filterMode = FilterMode.Trilinear; t2.mipMapsPreserveCoverage = true; t2.SaveAndReimport();
        return LoadSprites(n).OrderBy(sp => int.TryParse(sp.name.Substring(sp.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
    }

    /// Antaa Auroran kuvasarjat: [istuu, nousu, idle, kävely, kääntyminen]; null jos jokin puuttuu.
    static Sprite[][] LoadAuroraSets()
    {
        var sets = new[] { "turisti_aurora_istuu", "turisti_aurora_nousu", "turisti_aurora_idle", "turisti_aurora_kavely", "turisti_aurora_kaanto" }.Select(AuroraSheet).ToArray();
        return sets.All(x => x != null && x.Length > 0) ? sets : null;
    }

    static void ApplyAurora(Tourist tu, SpriteRenderer sr, Sprite[][] a)
    {
        tu.sitLoop = a[0]; tu.standUp = a[1]; tu.idle = a[2]; tu.walk = a[3]; tu.turn = a[4];
        tu.sitFrameTime = 1f / 6f; tu.standFrameTime = 1f / 12f; tu.idleFrameTime = 0.125f; tu.walkFrameTime = 1f / 12f; tu.turnFrameTime = 0.1f;
        tu.idleBeforeWalk = 0f;                  // nousun jälkeen suoraan kävelyyn (nousu jatkuu askeleeseen)
        tu.standOffsetY = 0f;                    // kaikki ruudut ovat samassa lattiakoordinaatistossa
        tu.idleCellH = 448f; tu.idleFeetPx = new Vector2(317f, 447f);
        if (sr != null && a[0].Length > 0) sr.sprite = a[0][0];
    }

    [MenuItem("Beat em up/428. Aurora: uudet kuvasarjat laivan kannelle", false, 428)]
    static void UpdateAuroraSheets()
    {
        if (PlayModeBlocked()) return;
        var sets = LoadAuroraSets();
        if (sets == null) { Info("Auroran kuvia puuttuu (Assets/Sprites/turisti_aurora_istuu/nousu/idle/kavely/kaanto.png)."); return; }
        const string scenePath = "Assets/Scenes/Areas/UccoLaiva.unity";
        var sc = EditorSceneManager.GetSceneByPath(scenePath); bool opened = false;
        if (!sc.IsValid() || !sc.isLoaded) { sc = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive); opened = true; }
        try
        {
            int n = 0;
            foreach (var tu in Object.FindObjectsByType<Tourist>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (tu == null || tu.gameObject.scene != sc || tu.standUp == null || tu.walkToX == 0f) continue;   // vain Aurora (nousee ja kävelee baarille)
                var sr = tu.GetComponent<SpriteRenderer>();
                // istuva kuva on 4 ruudun (2x) verran ylempänä ruudun alareunasta kuin vanha: jalat samaan kohtaan
                var p = tu.transform.position; p.y += 4f * tu.transform.localScale.x / 100f; tu.transform.position = p;
                ApplyAurora(tu, sr, sets); EditorUtility.SetDirty(tu); n++;
            }
            EditorSceneManager.MarkSceneDirty(sc); EditorSceneManager.SaveScene(sc);
            Info(n == 1 ? "Auroran uudet kuvasarjat asetettu laivan kannelle." : "Aurora-turisteja löytyi " + n + " (odotettiin 1). Tarkista UccoLaiva-scene.");
        }
        finally { if (opened && sc.IsValid()) EditorSceneManager.CloseScene(sc, true); }
    }
}

public static partial class BeatEmUpSetup
{
    /// Komentorivi: 427 (lopputekstien muotokuvat + baarin ovet), 428 (Aurora), 426 (aloitus hytistä).
    public static void RunCreditsAuroraCabinBatch()
    {
        batch = true;
        try
        {
            Step("427 lopputekstit", CreditsPortraits.RunBatch);
            Step("428 Aurora", UpdateAuroraSheets);
            Step("426 aloitus hytistä", StartAtShipCabin);
        }
        finally { batch = false; }
    }
}
