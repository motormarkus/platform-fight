using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Valikko 403: työmaakonttorin jälkeen yksi tienpätkä ja sitten Vegasin motelli (alue "Vegasin katu" jatkuu). Aja ensin valikot 392 ja 393 (konttorin oikea reuna).
/// Motelli 10240x3688 neljänä 5120x1844-palana (katu vasen/oikea, yläosa vasen/oikea): kaksikerroksinen, parveke (ovet) ja ulkoportaat oikealla (katu -> parveke, diagonaaliset).
/// Tienpätkä: vegas_tie_patka.png (5120x1844, vain katu; reunat täsmäävät konttoriin ja motelliin). Motellin ovet/sisätilat ja viholliset tulevat myöhemmin.
/// Huom: valikon 393 uudelleenajo palauttaa kadun rajat; aja sitten 403 uudelleen (ja 401: saumat).
/// </summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/403. Las Vegas: tienpätkä + motelli konttorin jälkeen (2 kerrosta, ulkoportaat parvekkeelle)", false, 403)]
    static void AddVegasMotel()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        string[] files = { "vegas_motelli_katu1", "vegas_motelli_katu2", "vegas_motelli_yla1", "vegas_motelli_yla2", "vegas_tie_patka" };
        foreach (var f in files)
            if (!File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat)."); return; }
        if (!File.Exists(VegasScenePath)) { Info("Aja ensin valikko 376 (Vegas)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var street = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a != null && a.gameObject.scene == scene && a.areaName == "Vegasin katu");
            var site = Object.FindObjectsByType<SiteLevels>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(s => s != null && s.gameObject.scene == scene && s.gameObject.name == "Rakennustyömaa");
            var officeRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Työmaakonttori");
            var office = officeRoot != null ? officeRoot.GetComponent<SiteLevels>() : null;
            if (street == null || site == null || office == null) { Info("Alue \"Vegasin katu\", työmaa tai konttori puuttuu: aja valikot 378, 392 ja 393."); return; }
            foreach (var o in scene.GetRootGameObjects().Where(g => g.name == "Vegasin motelli").ToArray()) Undo.DestroyObjectImmediate(o);

            float H = 2f * CamHalf, cy = CamY, bottom = cy - CamHalf;
            foreach (var csr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))   // kadun kuvan todellinen alareuna
                if (csr.gameObject.scene == scene && csr.gameObject.name == "Katu: hääkappeli") { bottom = csr.bounds.min.y; cy = bottom + CamHalf; break; }
            float ppuFull = 1844f / H;
            float imgW = 5120f / ppuFull, W = 1672f / (941f / H);
            float officeEnd = office.zoneMaxX;                 // konttorin oikea reuna = tienpätkän alku
            float mx0 = officeEnd + imgW, mx1 = mx0 + 2f * imgW;   // motelli: tienpätkän jälkeen
            float XW(float px) => mx0 + px / ppuFull;
            float YW(float py) => bottom + (3688f - py) / ppuFull;

            var root = new GameObject("Vegasin motelli");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Vegasin motelli");
            // (tiedosto, x: tienpätkä -0,5 / motellin vasen 0 / oikea 1, rivi 0 = yläosa / 1 = katu)
            var parts = new[] { ("vegas_tie_patka", -1, 1, "Tienpätkä"), (files[0], 0, 1, "Motelli katu 1"), (files[1], 1, 1, "Motelli katu 2"), (files[2], 0, 0, "Motelli yläosa 1"), (files[3], 1, 0, "Motelli yläosa 2") };
            int k = 0;
            foreach (var (f, cx, row, nm) in parts)
            {
                var sp = ImportShipSprite(T + f + ".png", ppuFull, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan tuonti epäonnistui: " + f); return; }
                var sr = new GameObject(nm + ": " + f).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10000 + (++k);
                sr.transform.position = new Vector3(mx0 + imgW * (cx + 0.5f), cy + (row == 0 ? H : 0f), 0f);
            }

            // kerrokset: katu + parveke (ovet); portaat oikealla (alhaalla oikealla, ylhäällä vasemmalla)
            float streetMax = site.streetMax, streetMin = site.streetMin;
            float[] levelY = { streetMax, YW(1770f + 25f) };
            float topY = levelY[1] + 0.4f;
            float Cam(float feetY) => feetY + 0.19f * H - cy;
            var sl = root.AddComponent<SiteLevels>();
            sl.area = street; sl.levelY = levelY; sl.streetMin = streetMin; sl.streetMax = streetMax; sl.bandHalf = 0.28f;
            sl.levelCamOffset = new[] { 0f, Cam(levelY[1]) };
            sl.zoneMinX = mx0; sl.zoneMaxX = mx1; sl.siteMinX = mx0; sl.siteMaxX = mx1;
            sl.levelMinX = new[] { 0f, XW(1300f) }; sl.levelMaxX = new[] { 0f, XW(9150f) };
            sl.edgeDrop = false;   // parvekkeessa kaide: ei putoamista
            sl.climbSpeed = 4.5f;
            sl.ladders = new[] { new SiteLevels.Ladder { x = XW(9930f), topX = XW(9110f), useTop = true, lo = 0, hi = 1 } };

            // katu jatkuu: kävelyalue, kamera, syvyysrajat, taustatasot
            street.walkMaxX = mx1 - 1.5f;
            street.camMaxX = Mathf.Max(street.camMinX, mx1 - W * 0.5f);
            float baseLimit = street.MaxDepthAtX(site.siteMaxX, street.maxDepthY);   // kadun takaraja ennen työmaata/konttoria
            var dl = street.depthLimits.Where(v => v.x <= officeEnd + 0.01f).ToList();
            dl.Add(new Vector2(officeEnd + 2.5f, baseLimit));       // konttorin jälkeen takaraja takaisin kadun tasolle
            dl.Add(new Vector2(mx0 + 2.5f, baseLimit));             // tienpätkä
            dl.Add(new Vector2(mx0 + 5f, topY)); dl.Add(new Vector2(mx1, topY));   // motelli: parvekkeen korkeus sallittu
            street.depthLimits = dl.ToArray();
            street.depthLimitsExceedMax = true;
            EditorUtility.SetDirty(street);
            foreach (var pl in Object.FindObjectsByType<ParallaxLayer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (pl.gameObject.scene != scene) continue;
                if (pl.name.StartsWith("Katu: ") || pl.name == "Työmaa: korkea taivas") { pl.maxX = street.camMaxX + 2f; EditorUtility.SetDirty(pl); }
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Tienpätkä (x " + officeEnd.ToString("0") + "–" + mx0.ToString("0") + ") ja motelli (x " + mx0.ToString("0") + "–" + mx1.ToString("0") + ") lisätty kadun jatkoksi: kaksikerroksinen, parveke ja ulkoportaat oikealla (katu -> parveke). Kävelyalue päättyy x " + street.walkMaxX.ToString("0") + ".");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    /// <summary>Motelli: 403 + saumat (401).</summary>
    public static void RunVegasMotelBatch()
    {
        batch = true;
        try
        {
            Step("403 motelli", AddVegasMotel);
            Step("401 saumat", FixVegasSeams);
        }
        finally { batch = false; }
    }
}
